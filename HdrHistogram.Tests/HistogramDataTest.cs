using System;

namespace HdrHistogram.Tests
{
    using NUnit.Framework;

    [TestFixture]
    public class HistogramDataTest {

        const long HighestTrackableValue = 3600L * 1000 * 1000; // 1 hour in usec units
        const int NumberOfSignificantValueDigits = 3; // Maintain at least 3 decimal points of accuracy
        static Histogram histogram;
        static Histogram rawHistogram;
        static Histogram postCorrectedHistogram;

        [TestFixtureSetUp]
        public void TestFixtureSetUp()
        {
            histogram = new Histogram(HighestTrackableValue, NumberOfSignificantValueDigits);
            rawHistogram = new Histogram(HighestTrackableValue, NumberOfSignificantValueDigits);
            // Log hypothetical scenario: 100 seconds of "perfect" 1msec results, sampled
            // 100 times per second (10,000 results), followed by a 100 second pause with
            // a single (100 second) recorded result. Recording is done indicating an expected
            // interval between samples of 10 msec:
            for (int i = 0; i < 10000; i++) {
                histogram.RecordValueWithExpectedInterval(1000 /* 1 msec */, 10000 /* 10 msec expected interval */);
                rawHistogram.RecordValue(1000 /* 1 msec */);
            }
            histogram.RecordValueWithExpectedInterval(100000000L /* 100 sec */, 10000 /* 10 msec expected interval */);
            rawHistogram.RecordValue(100000000L /* 100 sec */);

            postCorrectedHistogram = rawHistogram.CopyCorrectedForCoordinatedOmission(10000 /* 10 msec expected interval */);
        } 

        [Test]
        public void TestPreVsPostCorrectionValues()  {
            // Loop both ways (one would be enough, but good practice just for fun:

            Assert.AreEqual(histogram.GetTotalCount(), postCorrectedHistogram.GetTotalCount(), "pre and post corrected count totals ");

            // The following comparison loops would have worked in a perfect accuracy world, but since post
            // correction is done based on the value extracted from the bucket, and the during-recording is done
            // based on the actual (not pixelized) value, there will be subtle differences due to roundoffs:

            //        foreach(HistogramIterationValue v in histogram.GetHistogramData().allValues()) {
            //            long preCorrectedCount = v.GetCountAtValueIteratedTo();
            //            long postCorrectedCount = postCorrectedHistogram.GetHistogramData().GetCountAtValue(v.GetValueIteratedTo());
            //            Assert.AreEqual("pre and post corrected count at value " + v.GetValueIteratedTo(),
            //                    preCorrectedCount, postCorrectedCount);
            //        }
            //
            //        foreach(HistogramIterationValue v in postCorrectedHistogram.GetHistogramData().allValues()) {
            //            long preCorrectedCount = v.GetCountAtValueIteratedTo();
            //            long postCorrectedCount = histogram.GetHistogramData().GetCountAtValue(v.GetValueIteratedTo());
            //            Assert.AreEqual("pre and post corrected count at value " + v.GetValueIteratedTo(),
            //                    preCorrectedCount, postCorrectedCount);
            //        }

        }

        [Test]
        public void TestGetTotalCount() {
            // The overflow value should count in the total count:
            Assert.AreEqual(10001L, rawHistogram.GetHistogramData().GetTotalCount(), "Raw total count is 10,001");
            Assert.AreEqual(20000L, histogram.GetHistogramData().GetTotalCount(), "Total count is 20,000");
        }

        [Test]
        public void TestGetMaxValue() {
            Assert.True(
                    histogram.ValuesAreEquivalent(100L * 1000 * 1000,
                            histogram.GetHistogramData().GetMaxValue()));
        }

        [Test]
        public void TestGetMinValue() {
            Assert.True(
                    histogram.ValuesAreEquivalent(1000,
                            histogram.GetHistogramData().GetMinValue()));
        }

        [Test]
        public void TestGetMean() {
            const double ExpectedRawMean = ((10000.0 * 1000) + (1.0 * 100000000))/10001;
            const double ExpectedMean = (1000.0 + 50000000.0)/2;
            // We expect to see the mean to be accurate to ~3 decimal points (~0.1%):
            Assert.AreEqual(ExpectedRawMean, rawHistogram.GetHistogramData().GetMean(), ExpectedRawMean * 0.001, 
                string.Format("Raw mean is {0} +/- 0.1%", ExpectedRawMean));
            Assert.AreEqual(ExpectedMean, histogram.GetHistogramData().GetMean(), ExpectedMean * 0.001, 
                string.Format("Mean is {0} +/- 0.1%", ExpectedMean));
        }

        [Test]
        public void TestGetStdDeviation() {
            const double ExpectedRawMean = ((10000.0 * 1000) + (1.0 * 100000000))/10001;
            double expectedRawStdDev =
                Math.Sqrt(((10000.0 * Math.Pow((1000.0 - ExpectedRawMean), 2)) 
                + Math.Pow((100000000.0 - ExpectedRawMean), 2)) / 10001);

            const double ExpectedMean = (1000.0 + 50000000.0)/2;
            double expectedSquareDeviationSum = 10000 * Math.Pow((1000.0 - ExpectedMean), 2);
            for (long value = 10000; value <= 100000000; value += 10000) {
                expectedSquareDeviationSum += Math.Pow((value - ExpectedMean), 2);
            }
            double expectedStdDev = Math.Sqrt(expectedSquareDeviationSum / 20000);

            // We expect to see the standard deviations to be accurate to ~3 decimal points (~0.1%):
            Assert.AreEqual(expectedRawStdDev, rawHistogram.GetHistogramData().GetStdDeviation(), expectedRawStdDev * 0.001, 
                "Raw standard deviation is " + expectedRawStdDev + " +/- 0.1%");
            Assert.AreEqual(expectedStdDev, histogram.GetHistogramData().GetStdDeviation(), expectedStdDev * 0.001,
                string.Format("Standard deviation is {0} +/- 0.1%", expectedStdDev));
        }

        [Test]
        public void TestGetValueAtPercentile() {
            Assert.AreEqual(1000.0,
                    (double) rawHistogram.GetHistogramData().GetValueAtPercentile(30.0), 1000.0 * 0.001,
                    "raw 30%'ile is 1 msec +/- 0.1%");
            Assert.AreEqual(1000.0,
                    (double) rawHistogram.GetHistogramData().GetValueAtPercentile(99.0), 1000.0 * 0.001,
                    "raw 99%'ile is 1 msec +/- 0.1%");
            Assert.AreEqual(1000.0,
                    (double) rawHistogram.GetHistogramData().GetValueAtPercentile(99.99), 1000.0 * 0.001
                    , "raw 99.99%'ile is 1 msec +/- 0.1%");
            Assert.AreEqual(100000000.0,
                    (double) rawHistogram.GetHistogramData().GetValueAtPercentile(99.999), 100000000.0 * 0.001,
                    "raw 99.999%'ile is 100 sec +/- 0.1%");
            Assert.AreEqual(100000000.0,
                    (double) rawHistogram.GetHistogramData().GetValueAtPercentile(100.0), 100000000.0 * 0.001,
                    "raw 100%'ile is 100 sec +/- 0.1%");

            Assert.AreEqual(1000.0,
                    (double) histogram.GetHistogramData().GetValueAtPercentile(30.0), 1000.0 * 0.001,
                    "30%'ile is 1 msec +/- 0.1%");
            Assert.AreEqual(1000.0,
                    (double) histogram.GetHistogramData().GetValueAtPercentile(50.0), 1000.0 * 0.001,
                    "50%'ile is 1 msec +/- 0.1%");
            Assert.AreEqual(50000000.0,
                    (double) histogram.GetHistogramData().GetValueAtPercentile(75.0), 50000000.0 * 0.001,
                    "75%'ile is 50 sec +/- 0.1%");
            Assert.AreEqual(80000000.0,
                    (double) histogram.GetHistogramData().GetValueAtPercentile(90.0), 80000000.0 * 0.001,
                    "90%'ile is 80 sec +/- 0.1%");
            Assert.AreEqual(98000000.0,
                    (double) histogram.GetHistogramData().GetValueAtPercentile(99.0), 98000000.0 * 0.001,
                    "99%'ile is 98 sec +/- 0.1%");
            Assert.AreEqual(100000000.0,
                    (double) histogram.GetHistogramData().GetValueAtPercentile(99.999), 100000000.0 * 0.001,
                    "99.999%'ile is 100 sec +/- 0.1%");
            Assert.AreEqual(100000000.0,
                    (double) histogram.GetHistogramData().GetValueAtPercentile(100.0), 100000000.0 * 0.001,
                    "100%'ile is 100 sec +/- 0.1%");
        }

        [Test]
        public void TestGetPercentileAtOrBelowValue() {
            Assert.AreEqual(99.99, rawHistogram.GetHistogramData().GetPercentileAtOrBelowValue(5000), 0.0001, 
                "Raw percentile at or below 5 msec is 99.99% +/- 0.0001");
            Assert.AreEqual(50.0, histogram.GetHistogramData().GetPercentileAtOrBelowValue(5000), 0.0001, 
                "Percentile at or below 5 msec is 50% +/- 0.0001%");
            Assert.AreEqual(100.0, histogram.GetHistogramData().GetPercentileAtOrBelowValue(100000000L), 0.0001, 
                "Percentile at or below 100 sec is 100% +/- 0.0001%");
        }

        [Test]
        public void TestGetCountBetweenValues() {
            Assert.AreEqual(10000, rawHistogram.GetHistogramData().GetCountBetweenValues(1000L, 1000L), 
                "Count of raw values between 1 msec and 1 msec is 1");
            Assert.AreEqual(1, rawHistogram.GetHistogramData().GetCountBetweenValues(5000L, 150000000L), 
                "Count of raw values between 5 msec and 150 sec is 1");
            Assert.AreEqual(10000, histogram.GetHistogramData().GetCountBetweenValues(5000L, 150000000L), 
                "Count of values between 5 msec and 150 sec is 10,000");
        }

        [Test]
        public void TestGetCountAtValue() {
            Assert.AreEqual(0, rawHistogram.GetHistogramData().GetCountBetweenValues(10000L, 10010L), 
                    "Count of raw values at 10 msec is 0");
            Assert.AreEqual(1, histogram.GetHistogramData().GetCountBetweenValues(10000L, 10010L), 
                "Count of values at 10 msec is 0");
            Assert.AreEqual(10000, rawHistogram.GetHistogramData().GetCountAtValue(1000L), 
                    "Count of raw values at 1 msec is 10,000");
            Assert.AreEqual(10000, histogram.GetHistogramData().GetCountAtValue(1000L), 
                    "Count of values at 1 msec is 10,000");
        }

        [Test]
        public void TestPercentiles() {
            Assert.Ignore("Not Implemented");
        }

        [Test]
        public void TestLinearBucketValues() {
            int index = 0;
            // Note that using linear buckets should work "as expected" as long as the number of linear buckets
            // is lower than the resolution level determined by largestValueWithSingleUnitResolution
            // (2000 in this case). Above that count, some of the linear buckets can end up rounded up in size
            // (to the nearest local resolution unit level), which can result in a smaller number of buckets that
            // expected covering the range.

            // Iterate raw data using linear buckets of 100 msec each.
            foreach (HistogramIterationValue v in rawHistogram.GetHistogramData().LinearBucketValues(100000)) 
            {
                long countAddedInThisBucket = v.GetCountAddedInThisIterationStep();
                if (index == 0) {
                    Assert.AreEqual(10000, countAddedInThisBucket, 
                        "Raw Linear 100 msec bucket # 0 added a count of 10000");
                } else if (index == 999) {
                    Assert.AreEqual(1, countAddedInThisBucket, 
                        "Raw Linear 100 msec bucket # 999 added a count of 1");
                } else {
                    Assert.AreEqual(0, countAddedInThisBucket , 
                        string.Format("Raw Linear 100 msec bucket # {0} added a count of 0", index));
                }
                index++;
            }
            Assert.AreEqual(1000, index - 1);

            index = 0;
            long totalAddedCounts = 0;
            // Iterate data using linear buckets of 1 sec each.
            foreach (HistogramIterationValue v in rawHistogram.GetHistogramData().LinearBucketValues(10000))
            {
                long countAddedInThisBucket = v.GetCountAddedInThisIterationStep();
                if (index == 0) {
                    Assert.AreEqual(10001, countAddedInThisBucket, 
                            string.Format("Linear 1 sec bucket # 0 [{0}..{1}] added a count of 10001", 
                                v.GetValueIteratedFrom(), v.GetValueIteratedTo()));
                }
                // Because value resolution is low enough (3 digits) that multiple linear buckets will end up
                // residing in a single value-equivalent range, some linear buckets will have counts of 2 or
                // more, and some will have 0 (when the first bucket in the equivalent range was the one that
                // got the total count bump).
                // However, we can still verify the sum of counts added in all the buckets...
                totalAddedCounts += v.GetCountAddedInThisIterationStep();
                index++;
            }
            Assert.AreEqual(10001, index, 
                "There should be 10001 linear buckets of size 10001 usec between 0 and 1 sec.");
            Assert.AreEqual(20000, totalAddedCounts, 
                "Total added counts should be 20000");

        }

        [Test]
        public void TestLogarithmicBucketValues() {
            int index = 0;
            // Iterate raw data using logarithmic buckets starting at 10 msec.
            foreach (HistogramIterationValue v in rawHistogram.GetHistogramData().LogarithmicBucketValues(10000, 2)) {
                long countAddedInThisBucket = v.GetCountAddedInThisIterationStep();
                if (index == 0) {
                    Assert.AreEqual(10000, countAddedInThisBucket, 
                        "Raw Logarithmic 10 msec bucket # 0 added a count of 10000");
                } else if (index == 14) {
                    Assert.AreEqual(1, countAddedInThisBucket, 
                        "Raw Logarithmic 10 msec bucket # 14 added a count of 1");
                } else {
                    Assert.AreEqual(0, countAddedInThisBucket, 
                        "Raw Logarithmic 100 msec bucket # " + index + " added a count of 0");
                }
                index++;
            }
            Assert.AreEqual(14, index - 1);

            index = 0;
            long totalAddedCounts = 0;
            // Iterate data using linear buckets of 1 sec each.
            foreach (HistogramIterationValue v in rawHistogram.GetHistogramData().LogarithmicBucketValues(10000, 2))
            {
                long countAddedInThisBucket = v.GetCountAddedInThisIterationStep();
                if (index == 0) {
                    Assert.AreEqual(10001, countAddedInThisBucket, 
                        string.Format("Logarithmic 10 msec bucket # 0 [{0}..{1}] added a count of 10001", 
                            v.GetValueIteratedFrom(), v.GetValueIteratedTo()));
                }
                totalAddedCounts += v.GetCountAddedInThisIterationStep();
                index++;
            }
            Assert.AreEqual(14, index - 1, 
                "There should be 14 Logarithmic buckets of size 10001 usec between 0 and 1 sec.");
            Assert.AreEqual(20000, totalAddedCounts, 
                "Total added counts should be 20000");
        }

        [Test]
        public void TestRecordedValues() {
            int index = 0;
            // Iterate raw data by stepping through every value that has a count recorded:
            foreach(HistogramIterationValue v in rawHistogram.GetHistogramData().RecordedValues()) {
                long countAddedInThisBucket = v.GetCountAddedInThisIterationStep();
                if (index == 0) {
                    Assert.AreEqual(10000, countAddedInThisBucket, 
                        "Raw recorded value bucket # 0 added a count of 10000");
                } else {
                    Assert.AreEqual(1, countAddedInThisBucket, 
                        string.Format("Raw recorded value bucket # {0} added a count of 1", index));
                }
                index++;
            }
            Assert.AreEqual(2, index);

            index = 0;
            long totalAddedCounts = 0;
            // Iterate data using linear buckets of 1 sec each.
            foreach(HistogramIterationValue v in histogram.GetHistogramData().RecordedValues()) {
                long countAddedInThisBucket = v.GetCountAddedInThisIterationStep();
                if (index == 0) {
                    Assert.AreEqual(10000, countAddedInThisBucket, 
                        string.Format("Recorded bucket # 0 [{0}..{1}] added a count of 10000", 
                            v.GetValueIteratedFrom(), v.GetValueIteratedTo()));
                }
                Assert.True(v.GetCountAtValueIteratedTo() != 0,
                        string.Format("The count in recorded bucket #{0} is not 0", index));
                Assert.AreEqual(v.GetCountAtValueIteratedTo(), v.GetCountAddedInThisIterationStep(), 
                        string.Format("The count in recorded bucket #{0} is exactly the amount added since the last iteration ", 
                            index));
                totalAddedCounts += v.GetCountAddedInThisIterationStep();
                index++;
            }
            Assert.AreEqual(20000, totalAddedCounts, "Total added counts should be 20000");
        }

        [Test]
        public void TestAllValues()
        {
            int index = 0;
            long latestValueAtIndex = 0;
            // Iterate raw data by stepping through every value that ahs a count recorded:
            foreach (HistogramIterationValue v in rawHistogram.GetHistogramData().AllValues())
            {
                long countAddedInThisBucket = v.GetCountAddedInThisIterationStep();
                if (index == 1000)
                {
                    Assert.AreEqual(10000,
                            countAddedInThisBucket, 
                            "Raw allValues bucket # 0 added a count of 10000");
                }
                else if (histogram.ValuesAreEquivalent(v.GetValueIteratedTo(), 100000000))
                {
                    Assert.AreEqual(1, countAddedInThisBucket, 
                        string.Format("Raw allValues value bucket # {0} added a count of 1", index));
                }
                else
                {
                    Assert.AreEqual(0, countAddedInThisBucket, 
                            string.Format("Raw allValues value bucket # {0} added a count of 0", index));
                }
                latestValueAtIndex = v.GetValueIteratedTo();
                index++;
            }
            Assert.AreEqual(1,
                    rawHistogram.GetHistogramData().GetCountAtValue(latestValueAtIndex), 
                    "Count at latest value iterated to is 1");

            index = 0;
            long totalAddedCounts = 0;
            // Iterate data using linear buckets of 1 sec each.
            foreach (HistogramIterationValue v in histogram.GetHistogramData().AllValues())
            {
                long countAddedInThisBucket = v.GetCountAddedInThisIterationStep();
                if (index == 1000)
                {
                    Assert.AreEqual(10000, countAddedInThisBucket, 
                        string.Format("AllValues bucket # 0 [{0}..{1}] added a count of 10000", 
                            v.GetValueIteratedFrom(), v.GetValueIteratedTo()));
                }
                Assert.AreEqual(v.GetCountAtValueIteratedTo(), v.GetCountAddedInThisIterationStep(), 
                    string.Format("The count in AllValues bucket #{0} is exactly the amount added since the last iteration ", 
                        index));
                totalAddedCounts += v.GetCountAddedInThisIterationStep();
                index++;
            }
            Assert.AreEqual(20000, totalAddedCounts, "Total added counts should be 20000");
        }
    }
}
