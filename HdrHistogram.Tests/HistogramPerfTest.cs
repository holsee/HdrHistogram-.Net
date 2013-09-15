namespace HdrHistogram.Tests
{
    using System;
    using System.Threading;

    using NUnit.Framework;

    public class HistogramPerfTest
    {
        /// <summary>
        /// TODO: check equivalence to System.nanoTime();
        /// </summary>
        private long GetSystemNanoTime
        {
            get
            {
                return DateTime.Now.ToFileTimeUtc() * 100;
            }
        }
        /// <summary>
        /// TODO: Long.numberOfLeadingZeros(long);
        /// </summary>
        private static class Long
        {
            public static long NumberOfLeadingZeros(long i)
            {
                throw new NotImplementedException();
            }
        }

        const long HighestTrackableValue = 3600L * 1000 * 1000; // e.g. for 1 hr in usec units
        const int NumberOfSignificantValueDigits = 3;
        const long TestValueLevel = 12340;
        const long WarmupLoopLength = 50000;
        const long RawtimingLoopCount = 400000000L;
        const long SynchronizedTimingLoopCount = 40000000L; // 1/10th the regular count.
        const long AtomicTimingLoopCount = 80000000L; // 1/5th the regular count.

        static void RecordLoopWithExpectedInterval(AbstractHistogram histogram, long loopCount, long expectedInterval)
        {
            for (long i = 0; i < loopCount; i++)
                histogram.RecordValueWithExpectedInterval(TestValueLevel + (i & 0x8000), expectedInterval);
        }

        static long LeadingZerosSpeedLoop(long loopCount)
        {
            long sum = 0;
            for (long i = 0; i < loopCount; i++)
            {
                // long val = testValueLevel + (i & 0x8000);
                long val = TestValueLevel;
                sum += Long.NumberOfLeadingZeros(val);
                sum += Long.NumberOfLeadingZeros(val);
                sum += Long.NumberOfLeadingZeros(val);
                sum += Long.NumberOfLeadingZeros(val);
                sum += Long.NumberOfLeadingZeros(val);
                sum += Long.NumberOfLeadingZeros(val);
                sum += Long.NumberOfLeadingZeros(val);
                sum += Long.NumberOfLeadingZeros(val);
            }
            return sum;
        }

        private void TestRawRecordingSpeedAtExpectedInterval(string label, AbstractHistogram histogram, long expectedInterval, long timingLoopCount) {
            Console.WriteLine("\nTiming recording speed with expectedInterval = {0} :", expectedInterval);
            // Warm up:
            long startTime = GetSystemNanoTime;
            RecordLoopWithExpectedInterval(histogram, WarmupLoopLength, expectedInterval);
            long endTime = GetSystemNanoTime;
            long deltaUsec = (endTime - startTime) / 1000L;
            long rate = 1000000 * WarmupLoopLength / deltaUsec;
            Console.WriteLine(label + "Warmup: " + WarmupLoopLength + " value recordings completed in " +
                    deltaUsec + " usec, rate = " + rate + " value recording calls per sec.");
            histogram.Reset();
        
            // Wait a bit to make sure compiler had a cache to do it's stuff:
            Thread.Sleep(1000);

            startTime = GetSystemNanoTime;
            RecordLoopWithExpectedInterval(histogram, timingLoopCount, expectedInterval);
            endTime = GetSystemNanoTime;

            deltaUsec = (endTime - startTime) / 1000L;
            rate = 1000000 * timingLoopCount / deltaUsec;
            Console.WriteLine("{0}Hot code timing:", label);
            Console.WriteLine("{0}{1} value recordings completed in {2} usec, rate = {3} value recording calls per sec.", 
                label, timingLoopCount, deltaUsec, rate);
            rate = 1000000 * histogram.GetHistogramData().GetTotalCount() / deltaUsec;
            Console.WriteLine("{0}{1} raw recorded entries completed in {2} usec, rate = {3} recorded values per sec.", 
                label, histogram.GetHistogramData().GetTotalCount(), deltaUsec, rate);
        }

        [Test]
        public void TestRawRecordingSpeed()
        {
            AbstractHistogram histogram;
            histogram = new Histogram(HighestTrackableValue, NumberOfSignificantValueDigits);
            Console.WriteLine("\n\nTiming Histogram:");
            this.TestRawRecordingSpeedAtExpectedInterval("Histogram: ", histogram, 1000000000, RawtimingLoopCount);
        }

        [Test]
        public void TestRawSyncronizedRecordingSpeed()
        {
            AbstractHistogram histogram;
            histogram = new SynchronizedHistogram(HighestTrackableValue, NumberOfSignificantValueDigits);
            Console.WriteLine("\n\nTiming SynchronizedHistogram:");
            this.TestRawRecordingSpeedAtExpectedInterval("SynchronizedHistogram: ", histogram, 1000000000, SynchronizedTimingLoopCount);
        }

        [Test]
        public void TestRawAtomicRecordingSpeed()
        {
            AbstractHistogram histogram;
            histogram = new AtomicHistogram(HighestTrackableValue, NumberOfSignificantValueDigits);
            Console.WriteLine("\n\nTiming AtomicHistogram:");
            this.TestRawRecordingSpeedAtExpectedInterval("AtomicHistogram: ", histogram, 1000000000, AtomicTimingLoopCount);
        }

        #region stand-a-lone executable

        private void TestLeadingZerosSpeed()
        {
            Console.WriteLine("\nTiming LeadingZerosSpeed :");
            long startTime = GetSystemNanoTime;
            LeadingZerosSpeedLoop(WarmupLoopLength);
            long endTime = GetSystemNanoTime;
            long deltaUsec = (endTime - startTime) / 1000L;
            long rate = 1000000 * WarmupLoopLength / deltaUsec;
            Console.WriteLine("Warmup:\n" + WarmupLoopLength + " Leading Zero loops completed in " +
                    deltaUsec + " usec, rate = " + rate + " value recording calls per sec.");
            // Wait a bit to make sure compiler had a cache to do it's stuff:
            Thread.Sleep(1000);

            startTime = GetSystemNanoTime;
            LeadingZerosSpeedLoop(RawtimingLoopCount);
            endTime = GetSystemNanoTime;
            deltaUsec = (endTime - startTime) / 1000L;
            rate = 1000000 * RawtimingLoopCount / deltaUsec;
            Console.WriteLine("Hot code timing:");
            Console.WriteLine("{0} Leading Zero loops completed in {1} usec, rate = {2} value recording calls per sec.",
                RawtimingLoopCount, deltaUsec, rate);
        }

        public static void Main(String[] args)
        {
            try
            {
                var test = new HistogramPerfTest();
                test.TestLeadingZerosSpeed();
                Thread.Sleep(1000000);
            }
            catch (Exception e)
            {
                Console.WriteLine("Exception: " + e);
            }
        }

        #endregion
    }
}

