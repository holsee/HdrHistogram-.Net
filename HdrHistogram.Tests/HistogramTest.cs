namespace HdrHistogram.Tests
{
    using System;

    using NUnit.Framework;

    [TestFixture]
    public class HistogramTest
    {
        const long HighestTrackableValue = 3600L * 1000 * 1000; // e.g. for 1 hr in usec units
        const int NumberOfSignificantValueDigits = 3;
        const long TestValueLevel = 4;

        [Test]
        public void TestConstructionArgumentRanges() {
            bool thrown = false;
            Histogram histogram = null;

            try {
                // This should throw:
                histogram = new Histogram(1, NumberOfSignificantValueDigits);
            } catch (ArgumentException e) {
                thrown = true;
            }
            Assert.True(thrown);
            Assert.AreEqual(histogram, null);

            thrown = false;
            try {
                // This should throw:
                histogram = new Histogram(HighestTrackableValue, 6);
            } catch (ArgumentException e) {
                thrown = true;
            }
            Assert.True(thrown);
            Assert.AreEqual(histogram, null);

            thrown = false;
            try {
                // This should throw:
                histogram = new Histogram(HighestTrackableValue, -1);
            } catch (ArgumentException e) {
                thrown = true;
            }
            Assert.True(thrown);
            Assert.AreEqual(histogram, null);
        }

        [Test]
        public void TestConstructionArgumentGets() {
            Histogram histogram = new Histogram(HighestTrackableValue, NumberOfSignificantValueDigits);
            Assert.AreEqual(HighestTrackableValue, histogram.GetHighestTrackableValue());
            Assert.AreEqual(NumberOfSignificantValueDigits, histogram.GetNumberOfSignificantValueDigits());
        }

        [Test]
        public void TestGetEstimatedFootprintInBytes() {
            Histogram histogram = new Histogram(HighestTrackableValue, NumberOfSignificantValueDigits);
            /*
            *     largestValueWithSingleUnitResolution = 2 * (10 ^ numberOfSignificantValueDigits);
            *     subBucketSize = roundedUpToNearestPowerOf2(largestValueWithSingleUnitResolution);

            *     expectedHistogramFootprintInBytes = 512 +
            *          ({primitive type size} / 2) *
            *          (log2RoundedUp((highestTrackableValue) / subBucketSize) + 2) *
            *          subBucketSize
            */
            long largestValueWithSingleUnitResolution = 2 * (long) Math.Pow(10, NumberOfSignificantValueDigits);
            int subBucketCountMagnitude = (int) Math.Ceiling(Math.Log(largestValueWithSingleUnitResolution)/Math.Log(2));
            int subBucketSize = (int) Math.Pow(2, (subBucketCountMagnitude));

            long expectedSize = 512 +
                    ((8 *
                     ((long)(
                            Math.Ceiling(Math.Log(HighestTrackableValue / subBucketSize) / Math.Log(2))
                           + 2)) *
                        (1 << (64 - Long.NumberOfLeadingZeros(2 * (long) Math.Pow(10, NumberOfSignificantValueDigits))))
                     ) / 2);
            Assert.AreEqual(expectedSize, histogram.GetEstimatedFootprintInBytes());
        }

        [Test]
        public void TestRecordValue() {
            var histogram = new Histogram(HighestTrackableValue, NumberOfSignificantValueDigits);
            histogram.RecordValue(TestValueLevel);
            Assert.AreEqual(1L, histogram.GetHistogramData().GetCountAtValue(TestValueLevel));
            Assert.AreEqual(1L, histogram.GetHistogramData().GetTotalCount());
        }

        [Test]
        public void TestRecordValue_Overflow_ShouldThrowException() {
            var histogram = new Histogram(HighestTrackableValue, NumberOfSignificantValueDigits);
            Assert.Throws<IndexOutOfRangeException>(() => histogram.RecordValue(HighestTrackableValue * 3));
        }

        [Test]
        public void TestRecordValueWithExpectedInterval() {
            var histogram = new Histogram(HighestTrackableValue, NumberOfSignificantValueDigits);
            histogram.RecordValueWithExpectedInterval(TestValueLevel, TestValueLevel/4);
            var rawHistogram = new Histogram(HighestTrackableValue, NumberOfSignificantValueDigits);
            rawHistogram.RecordValue(TestValueLevel);
            // The data will include corrected samples:
            Assert.AreEqual(1L, histogram.GetHistogramData().GetCountAtValue((TestValueLevel * 1 )/4));
            Assert.AreEqual(1L, histogram.GetHistogramData().GetCountAtValue((TestValueLevel * 2 )/4));
            Assert.AreEqual(1L, histogram.GetHistogramData().GetCountAtValue((TestValueLevel * 3 )/4));
            Assert.AreEqual(1L, histogram.GetHistogramData().GetCountAtValue((TestValueLevel * 4 )/4));
            Assert.AreEqual(4L, histogram.GetHistogramData().GetTotalCount());
            // But the raw data will not:
            Assert.AreEqual(0L, rawHistogram.GetHistogramData().GetCountAtValue((TestValueLevel * 1 )/4));
            Assert.AreEqual(0L, rawHistogram.GetHistogramData().GetCountAtValue((TestValueLevel * 2 )/4));
            Assert.AreEqual(0L, rawHistogram.GetHistogramData().GetCountAtValue((TestValueLevel * 3 )/4));
            Assert.AreEqual(1L, rawHistogram.GetHistogramData().GetCountAtValue((TestValueLevel * 4 )/4));
            Assert.AreEqual(1L, rawHistogram.GetHistogramData().GetTotalCount());
        }

        [Test]
        public void TestReset() {
            Histogram histogram = new Histogram(HighestTrackableValue, NumberOfSignificantValueDigits);
            histogram.RecordValue(TestValueLevel);
            histogram.Reset();
            Assert.AreEqual(0L, histogram.GetHistogramData().GetCountAtValue(TestValueLevel));
            Assert.AreEqual(0L, histogram.GetHistogramData().GetTotalCount());
        }

        [Test]
        public void TestAdd() {
            Histogram histogram = new Histogram(HighestTrackableValue, NumberOfSignificantValueDigits);
            Histogram other = new Histogram(HighestTrackableValue, NumberOfSignificantValueDigits);
            histogram.RecordValue(TestValueLevel);
            other.RecordValue(TestValueLevel);
            histogram.Add(other);
            Assert.AreEqual(2L, histogram.GetHistogramData().GetCountAtValue(TestValueLevel));
            Assert.AreEqual(2L, histogram.GetHistogramData().GetTotalCount());
            Histogram incompatibleOther = new Histogram(HighestTrackableValue * 2, NumberOfSignificantValueDigits);
            bool thrown = false;
            try {
                // This should throw:
                histogram.Add(incompatibleOther);
            } catch (ArgumentException e) {
                thrown = true;
            }
            Assert.True(thrown);
        }


        [Test]
        public void TestSizeOfEquivalentValueRange() {
            Histogram histogram = new Histogram(HighestTrackableValue, NumberOfSignificantValueDigits);
            Assert.AreEqual("Size of equivalent range for value 1 is 1",
                    1, histogram.SizeOfEquivalentValueRange(1));
            Assert.AreEqual("Size of equivalent range for value 2500 is 2",
                    2, histogram.SizeOfEquivalentValueRange(2500));
            Assert.AreEqual("Size of equivalent range for value 8191 is 4",
                    4, histogram.SizeOfEquivalentValueRange(8191));
            Assert.AreEqual("Size of equivalent range for value 8192 is 8",
                    8, histogram.SizeOfEquivalentValueRange(8192));
            Assert.AreEqual("Size of equivalent range for value 10000 is 8",
                    8, histogram.SizeOfEquivalentValueRange(10000));
        }

        [Test]
        public void TestLowestEquivalentValue() {
            Histogram histogram = new Histogram(HighestTrackableValue, NumberOfSignificantValueDigits);
            Assert.AreEqual("The lowest equivalent value to 10007 is 10000",
                    10000, histogram.LowestEquivalentValue(10007));
            Assert.AreEqual("The lowest equivalent value to 10009 is 10008",
                    10008, histogram.LowestEquivalentValue(10009));
        }

        [Test]
        public void TestHighestEquivalentValue() {
            Histogram histogram = new Histogram(HighestTrackableValue, NumberOfSignificantValueDigits);
            Assert.AreEqual("The highest equivalent value to 8180 is 8183",
                    8183, histogram.HighestEquivalentValue(8180));
            Assert.AreEqual("The highest equivalent value to 8187 is 8191",
                    8191, histogram.HighestEquivalentValue(8191));
            Assert.AreEqual("The highest equivalent value to 8193 is 8199",
                    8199, histogram.HighestEquivalentValue(8193));
            Assert.AreEqual("The highest equivalent value to 9995 is 9999",
                    9999, histogram.HighestEquivalentValue(9995));
            Assert.AreEqual("The highest equivalent value to 10007 is 10007",
                    10007, histogram.HighestEquivalentValue(10007));
            Assert.AreEqual("The highest equivalent value to 10008 is 10015",
                    10015, histogram.HighestEquivalentValue(10008));
        }

        [Test]
        public void TestMedianEquivalentValue() {
            Histogram histogram = new Histogram(HighestTrackableValue, NumberOfSignificantValueDigits);
            Assert.AreEqual("The median equivalent value to 4 is 4",
                    4, histogram.MedianEquivalentValue(4));
            Assert.AreEqual("The median equivalent value to 5 is 5",
                    5, histogram.MedianEquivalentValue(5));
            Assert.AreEqual("The median equivalent value to 4000 is 4001",
                    4001, histogram.MedianEquivalentValue(4000));
            Assert.AreEqual("The median equivalent value to 8000 is 8002",
                    8002, histogram.MedianEquivalentValue(8000));
            Assert.AreEqual("The median equivalent value to 10007 is 10004",
                    10004, histogram.MedianEquivalentValue(10007));
        }

        [Test]
        public void TestNextNonEquivalentValue() {
            Histogram histogram = new Histogram(HighestTrackableValue, NumberOfSignificantValueDigits);
            Assert.AreNotSame(null, histogram);
        }

        #region Helpers

        void TestAbstractSerialization(AbstractHistogram histogram) {

            histogram.RecordValue(TestValueLevel);
            histogram.RecordValue(TestValueLevel * 10);
            histogram.RecordValueWithExpectedInterval(histogram.GetHighestTrackableValue() - 1, 31);

            //TODO: Convert to .NET Streams or evaluate better approach to testing serialization

            //ByteArrayOutputStream bos = new ByteArrayOutputStream();
            //ObjectOutput @out = null;
            //ByteArrayInputStream bis = null;
            //ObjectInput @in = null;
            //AbstractHistogram newHistogram = null;

            //try {
            //    @out = new ObjectOutputStream(bos);
            //    @out.writeObject(histogram);

            //    Deflater compresser = new Deflater();
            //    compresser.setInput(bos.toByteArray());
            //    compresser.finish();

            //    byte [] compressedOutput = new byte[1024*1024];
            //    int compressedDataLength = compresser.deflate(compressedOutput);

            //    Console.WriteLine(
            //        "Serialized form of {0} with highestTrackableValue = {1}\n and a numberOfSignificantValueDigits =" + 
            //        " {2} is {3} bytes long. Compressed form is {4} bytes long.", 
            //        histogram.GetType(), histogram.GetHighestTrackableValue(), histogram.GetNumberOfSignificantValueDigits(), 
            //        bos.toByteArray().length, compressedDataLength);

            //    Console.WriteLine("   (estimated footprint was {0} bytes)", histogram.GetEstimatedFootprintInBytes());

            //    bis = new ByteArrayInputStream(bos.toByteArray());
            //    @in = new ObjectInputStream(bis);
            //    newHistogram = (AbstractHistogram) in.readObject();
            //} finally {
            //    if (@out != null) @out.close();
            //    bos.close();
            //    if (@in !=null) @in.close();
            //    if (bis != null) bis.close();
            //}

            //Assert.NotNull(newHistogram);
            //assertEqual(histogram, newHistogram);
        }

        private void assertEqual(AbstractHistogram expectedHistogram, AbstractHistogram actualHistogram)
        {
            Assert.AreEqual(expectedHistogram, actualHistogram);
            Assert.AreEqual(
                    expectedHistogram.GetHistogramData().GetCountAtValue(TestValueLevel),
                    actualHistogram.GetHistogramData().GetCountAtValue(TestValueLevel));
            Assert.AreEqual(
                    expectedHistogram.GetHistogramData().GetCountAtValue(TestValueLevel * 10),
                    actualHistogram.GetHistogramData().GetCountAtValue(TestValueLevel * 10));
            Assert.AreEqual(
                    expectedHistogram.GetHistogramData().GetTotalCount(),
                    actualHistogram.GetHistogramData().GetTotalCount());
        }

        #endregion

        [Test]
        public void TestSerialization() {
            Histogram histogram = new Histogram(HighestTrackableValue, 3);
            TestAbstractSerialization(histogram);
            IntHistogram intHistogram = new IntHistogram(HighestTrackableValue, 3);
            TestAbstractSerialization(intHistogram);
            ShortHistogram shortHistogram = new ShortHistogram(HighestTrackableValue, 3);
            TestAbstractSerialization(shortHistogram);
            histogram = new Histogram(HighestTrackableValue, 2);
            TestAbstractSerialization(histogram);
            intHistogram = new IntHistogram(HighestTrackableValue, 2);
            TestAbstractSerialization(intHistogram);
            shortHistogram = new ShortHistogram(HighestTrackableValue, 2);
            TestAbstractSerialization(shortHistogram);
        }

        [Test]
        public void TestOverflow() {
            ShortHistogram histogram = new ShortHistogram(HighestTrackableValue, 2);
            histogram.RecordValue(TestValueLevel);
            histogram.RecordValue(TestValueLevel * 10);
            Assert.False(histogram.HasOverflowed());
            // This should overflow a ShortHistogram:
            histogram.RecordValueWithExpectedInterval(histogram.GetHighestTrackableValue() - 1, 500);
            Assert.True(histogram.HasOverflowed());
            Console.WriteLine("Histogram percentile output should show overflow:");
            histogram.GetHistogramData().OutputPercentileDistribution(Console.Out, 5, 100.0);
        }
    
        [Test]
        public void TestCopy() {
            Histogram histogram = new Histogram(HighestTrackableValue, NumberOfSignificantValueDigits);
            histogram.RecordValue(TestValueLevel);
            histogram.RecordValue(TestValueLevel * 10);
            histogram.RecordValueWithExpectedInterval(histogram.GetHighestTrackableValue() - 1, 31);
        
            assertEqual(histogram, histogram.Copy());
  
            IntHistogram intHistogram = new IntHistogram(HighestTrackableValue, NumberOfSignificantValueDigits);
            intHistogram.RecordValue(TestValueLevel);
            intHistogram.RecordValue(TestValueLevel * 10);
            intHistogram.RecordValueWithExpectedInterval(intHistogram.GetHighestTrackableValue() - 1, 31);
        
            assertEqual(intHistogram, intHistogram.Copy());
  
            ShortHistogram shortHistogram = new ShortHistogram(HighestTrackableValue, NumberOfSignificantValueDigits);
            shortHistogram.RecordValue(TestValueLevel);
            shortHistogram.RecordValue(TestValueLevel * 10);
            shortHistogram.RecordValueWithExpectedInterval(shortHistogram.GetHighestTrackableValue() - 1, 31);
        
            assertEqual(shortHistogram, shortHistogram.Copy());
  
            AtomicHistogram atomicHistogram = new AtomicHistogram(HighestTrackableValue, NumberOfSignificantValueDigits);
            atomicHistogram.RecordValue(TestValueLevel);
            atomicHistogram.RecordValue(TestValueLevel * 10);
            atomicHistogram.RecordValueWithExpectedInterval(atomicHistogram.GetHighestTrackableValue() - 1, 31);
        
            assertEqual(atomicHistogram, atomicHistogram.Copy());
  
            SynchronizedHistogram syncHistogram = new SynchronizedHistogram(HighestTrackableValue, NumberOfSignificantValueDigits);
            syncHistogram.RecordValue(TestValueLevel);
            syncHistogram.RecordValue(TestValueLevel * 10);
            syncHistogram.RecordValueWithExpectedInterval(syncHistogram.GetHighestTrackableValue() - 1, 31);
        
            assertEqual(syncHistogram, syncHistogram.Copy());
        } 
    }
}