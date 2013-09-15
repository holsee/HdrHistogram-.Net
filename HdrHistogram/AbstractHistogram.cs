namespace HdrHistogram
{
    using System;
    using System.Diagnostics;

    using HdrHistogram.CSharp.Atomic;
    using HdrHistogram.GoodieBag;

    public abstract class AbstractHistogramBase
    {
        protected static readonly AtomicLong constructionIdentityCount = new AtomicLong(0);

        // "Cold" accessed fields. Not used in the recording code path:
        protected long identityCount;

        protected long highestTrackableValue;
        protected int numberOfSignificantValueDigits;

        protected int bucketCount;
        protected int subBucketCount;
        protected int countsArrayLength;

        protected HistogramData histogramData;
    }


    /**
     * <h3>A High Dynamic Range (HDR) Histogram</h3>
     * <p>
     * AbstractHistogram supports the recording and analyzing sampled data value counts across a configurable integer value
     * range with configurable value precision within the range. Value precision is expressed as the number of significant
     * digits in the value recording, and provides control over value quantization behavior across the value range and the
     * subsequent value resolution at any given level.
     * <p>
     * For example, a Histogram could be configured to track the counts of observed integer values between 0 and
     * 3,600,000,000 while maintaining a value precision of 3 significant digits across that range. Value quantization
     * within the range will thus be no larger than 1/1,000th (or 0.1%) of any value. This example Histogram could
     * be used to track and analyze the counts of observed response times ranging between 1 microsecond and 1 hour
     * in magnitude, while maintaining a value resolution of 1 microsecond up to 1 millisecond, a resolution of
     * 1 millisecond (or better) up to one second, and a resolution of 1 second (or better) up to 1,000 seconds. At it's
     * maximum tracked value (1 hour), it would still maintain a resolution of 3.6 seconds (or better).
     * <p>
     * See package description for {@link org.HdrHistogram} for details.
     */
    public abstract class AbstractHistogram : AbstractHistogramBase
    {
        // "Hot" accessed fields (used in the the value recording code path) are bunched here, such
        // that they will have a good chance of ending up in the same cache line as the totalCounts and
        // counts array reference fields that subclass implementations will typically add.
        int subBucketHalfCountMagnitude;
        int subBucketHalfCount;
        long subBucketMask;
        // Sub-classes will typically add a totalCount field and a counts array field, which will likely be laid out
        // right around here due to the subclass layout rules in most practical JVM implementations.

        // Abstract, counts-type dependent methods to be provided by subclass implementations:

        public abstract long GetCountAtIndex(int index);

        public abstract void IncrementCountAtIndex(int index);

        public abstract void AddToCountAtIndex(int index, long value);

        public abstract long GetTotalCount();

        public abstract void SetTotalCount(long totalCount);

        public abstract void IncrementTotalCount();

        public abstract void AddToTotalCount(long value);

        public abstract void ClearCounts();

        /// <summary>
        /// Create a copy of this historgram, complete with data and everything.
        /// </summary>
        /// <returns></returns>
        public abstract AbstractHistogram Copy();

        /// <summary>
        /// Get a copy of this histogram, corrected for coordinated omission.
        /// 
        /// To compensate for the loss of sampled values when a recorded value is larger than the expected
        /// interval between value samples, the new histogram will include an auto-generated additional series of
        /// decreasingly-smaller (down to the expectedIntervalBetweenValueSamples) value records for each count found
        /// in the current histogram that is larger than the expectedIntervalBetweenValueSamples.
        ///
        /// Note: This is a post-correction method, as opposed to the at-recording correction method provided
        /// by {@link #RecordValueWithExpectedInterval(long, long) RecordValueWithExpectedInterval}. The two
        /// methods are mutually exclusive, and only one of the two should be be used on a given data set to correct
        /// for the same coordinated omission issue.
        /// 
        /// See notes in the description of the Histogram calls for an illustration of why this corrective behavior is
        /// important.
        /// </summary>
        /// <exception cref="IndexOutOfRangeException"></exception>
        /// <param name="expectedIntervalBetweenValueSamples">
        /// If expectedIntervalBetweenValueSamples is larger than 0, add auto-generated value records as appropriate if value is larger than expectedIntervalBetweenValueSamples
        /// </param>
        /// <returns></returns>
        public abstract AbstractHistogram CopyCorrectedForCoordinatedOmission(long expectedIntervalBetweenValueSamples);

        /// <summary>
        /// Provide a (conservatively high) estimate of the Histogram's total footprint in bytes
        /// </summary>
        /// <returns> 
        /// a (conservatively high) estimate of the Histogram's total footprint in bytes
        /// </returns>
        public abstract int GetEstimatedFootprintInBytes();

        /**
         * Construct a Histogram given the Highest value to be tracked and a number of significant decimal digits
         *
         * @param highestTrackableValue The highest value to be tracked by the histogram. Must be a positive
         *                              integer that is >= 2.
         * @param numberOfSignificantValueDigits The number of significant decimal digits to which the histogram will
         *                                       maintain value resolution and separation. Must be a non-negative
         *                                       integer between 0 and 5.
         */
        protected AbstractHistogram(long highestTrackableValue, int numberOfSignificantValueDigits) 
        {
            // Verify argument validity
            if (highestTrackableValue < 2)
                throw new ArgumentException("highestTrackableValue must be >= 2", "highestTrackableValue");
            if ((numberOfSignificantValueDigits < 0) || (numberOfSignificantValueDigits > 5))
                throw new ArgumentException("numberOfSignificantValueDigits must be between 0 and 6", "numberOfSignificantValueDigits");

            identityCount = constructionIdentityCount.Increment();
            this.Init(highestTrackableValue, numberOfSignificantValueDigits, 0);
        }

        private void Init(long highestTrackableValue, int numberOfSignificantValueDigits, long totalCount) 
        {
            this.highestTrackableValue = highestTrackableValue;
            this.numberOfSignificantValueDigits = numberOfSignificantValueDigits;

            long largestValueWithSingleUnitResolution = 2 * (long) Math.Pow(10, numberOfSignificantValueDigits);

            // We need to maintain power-of-two subBucketCount (for clean direct indexing) that is large enough to
            // provide unit resolution to at least largestValueWithSingleUnitResolution. So figure out
            // largestValueWithSingleUnitResolution's nearest power-of-two (rounded up), and use that:
            int subBucketCountMagnitude = (int) Math.Ceiling(Math.Log(largestValueWithSingleUnitResolution)/Math.Log(2));
            subBucketHalfCountMagnitude = ((subBucketCountMagnitude > 1) ? subBucketCountMagnitude : 1) - 1;
            subBucketCount = (int) Math.Pow(2, (subBucketHalfCountMagnitude + 1));
            subBucketHalfCount = subBucketCount / 2;
            subBucketMask = subBucketCount - 1;

            // determine exponent range needed to support the trackable value with no overflow:
            long trackableValue = subBucketCount - 1;
            int bucketsNeeded = 1;
            while (trackableValue < highestTrackableValue) {
                trackableValue <<= 1;
                bucketsNeeded++;
            }
            this.bucketCount = bucketsNeeded;

            countsArrayLength = (bucketCount + 1) * (subBucketCount / 2);

            SetTotalCount(totalCount);

            histogramData = new HistogramData(this);
        }

        /**
         * get the configured numberOfSignificantValueDigits
         * @return numberOfSignificantValueDigits
         */
        public int GetNumberOfSignificantValueDigits() {
            return numberOfSignificantValueDigits;
        }

        /**
         * get the configured highestTrackableValue
         * @return highestTrackableValue
         */
        public long GetHighestTrackableValue() {
            return highestTrackableValue;
        }

        private int CountsArrayIndex(int bucketIndex, int subBucketIndex) {
            Debug.Assert(subBucketIndex < subBucketCount);
            Debug.Assert(bucketIndex < bucketCount);
            Debug.Assert(bucketIndex == 0 || (subBucketIndex >= subBucketHalfCount));
            
            // Calculate the index for the first entry in the bucket:
            // (The following is the equivalent of ((bucketIndex + 1) * subBucketHalfCount) ):
            int bucketBaseIndex = (bucketIndex + 1) << subBucketHalfCountMagnitude;
            // Calculate the offset in the bucket:
            int offsetInBucket = subBucketIndex - subBucketHalfCount;
            // The following is the equivalent of ((subBucketIndex  - subBucketHalfCount) + bucketBaseIndex;
            return bucketBaseIndex + offsetInBucket;
        }

        private long GetCountAt(int bucketIndex, int subBucketIndex) {
            return GetCountAtIndex(CountsArrayIndex(bucketIndex, subBucketIndex));
        }

        private static void ArrayAdd(AbstractHistogram toHistogram, AbstractHistogram fromHistogram) {
            if (fromHistogram.countsArrayLength != toHistogram.countsArrayLength) 
                throw new IndexOutOfRangeException("fromHistogram.countsArrayLength != toHistogram.countsArrayLength");
            
            for (int i = 0; i < fromHistogram.countsArrayLength; i++)
                toHistogram.AddToCountAtIndex(i, fromHistogram.GetCountAtIndex(i));
        }

        private int GetBucketIndex(long value) {
            int pow2Ceiling = 64 - Long.NumberOfLeadingZeros(value | subBucketMask); // smallest power of 2 containing value
            return  pow2Ceiling - (subBucketHalfCountMagnitude + 1);
        }

        private int GetSubBucketIndex(long value, int bucketIndex) {
            return  (int)(value >> bucketIndex);
        }

        private void RecordCountAtValue(long count, long value) {
            // Dissect the value into bucket and sub-bucket parts, and derive index into counts array:
            int bucketIndex = GetBucketIndex(value);
            int subBucketIndex = GetSubBucketIndex(value, bucketIndex);
            int countsIndex = CountsArrayIndex(bucketIndex, subBucketIndex);
            AddToCountAtIndex(countsIndex, count);
            AddToTotalCount(count);
        }

        private void RecordSingleValue(long value) {
            // Dissect the value into bucket and sub-bucket parts, and derive index into counts array:
            int bucketIndex = GetBucketIndex(value);
            int subBucketIndex = GetSubBucketIndex(value, bucketIndex);
            int countsIndex = CountsArrayIndex(bucketIndex, subBucketIndex);
            IncrementCountAtIndex(countsIndex);
            IncrementTotalCount();
        }


        private void RecordValueWithCountAndExpectedInterval(long value, long count, long expectedIntervalBetweenValueSamples) 
        {
            RecordCountAtValue(count, value);
            if (expectedIntervalBetweenValueSamples <=0)
                return;
            for (long missingValue = value - expectedIntervalBetweenValueSamples;
                 missingValue >= expectedIntervalBetweenValueSamples;
                 missingValue -= expectedIntervalBetweenValueSamples) {
                RecordCountAtValue(count, missingValue);
            }
        }

        /**
         * Record a value in the histogram.
         * <p>
         * To compensate for the loss of sampled values when a recorded value is larger than the expected
         * interval between value samples, Histogram will auto-generate an additional series of decreasingly-smaller
         * (down to the expectedIntervalBetweenValueSamples) value records.
         * <p>
         * Note: This is a at-recording correction method, as opposed to the post-recording correction method provided
         * by {@link #copyCorrectedForCoordinatedOmission(long) getHistogramCorrectedForCoordinatedOmission}.
         * The two methods are mutually exclusive, and only one of the two should be be used on a given data set to correct
         * for the same coordinated omission issue.
         * <p>
         * See notes in the description of the Histogram calls for an illustration of why this corrective behavior is
         * important.
         *
         * @param value The value to record
         * @param expectedIntervalBetweenValueSamples If expectedIntervalBetweenValueSamples is larger than 0, add
         *                                           auto-generated value records as appropriate if value is larger
         *                                           than expectedIntervalBetweenValueSamples
         * @throws ArrayIndexOutOfBoundsException
         */
        public void RecordValueWithExpectedInterval(long value, long expectedIntervalBetweenValueSamples) {
            RecordValueWithCountAndExpectedInterval(value, 1, expectedIntervalBetweenValueSamples);
        }

        /**
         * @deprecated
         *
         * Record a value in the histogram. This deprecated method has identical behavior to
         * <b><code>RecordValueWithExpectedInterval()</code></b>. It was renamed to avoid ambiguity.
         *
         * @param value The value to record
         * @param expectedIntervalBetweenValueSamples If expectedIntervalBetweenValueSamples is larger than 0, add
         *                                           auto-generated value records as appropriate if value is larger
         *                                           than expectedIntervalBetweenValueSamples
         * @throws ArrayIndexOutOfBoundsException
         */
        public void RecordValue(long value, long expectedIntervalBetweenValueSamples) {
            RecordValueWithExpectedInterval(value, expectedIntervalBetweenValueSamples);
        }


        /**
         * Record a value in the histogram (adding to the value's current count)
         *
         * @param value The value to be recorded
         * @param count The number of occurrences of this value to record
         * @throws ArrayIndexOutOfBoundsException
         */
        public void RecordValueWithCount(long value, long count) {
            RecordCountAtValue(count, value);
        }

        /**
         * Record a value in the histogram
         *
         * @param value The value to be recorded
         * @throws ArrayIndexOutOfBoundsException
         */
        public void RecordValue( long value) {
            RecordSingleValue(value);
        }

        /**
         * Reset the contents and stats of this histogram
         */
        public void Reset() {
            ClearCounts();
        }

        /**
         * Add the contents of another histogram to this one
         *
         * @param fromHistogram The other histogram. highestTrackableValue and largestValueWithSingleUnitResolution must match.
         */
        public void Add(AbstractHistogram fromHistogram) {
            if ((highestTrackableValue != fromHistogram.highestTrackableValue) ||
                    (numberOfSignificantValueDigits != fromHistogram.numberOfSignificantValueDigits) ||
                    (bucketCount != fromHistogram.bucketCount) ||
                    (subBucketCount != fromHistogram.subBucketCount))
                throw new ArgumentException("Cannot add histograms with incompatible ranges", "fromHistogram");

            ArrayAdd(this, fromHistogram);
            SetTotalCount(GetTotalCount() + fromHistogram.GetTotalCount());
        }

        /**
         * Add the contents of another histogram to this one, while correcting the incoming data for coordinated omission.
         * <p>
         * To compensate for the loss of sampled values when a recorded value is larger than the expected
         * interval between value samples, the values added will include an auto-generated additional series of
         * decreasingly-smaller (down to the expectedIntervalBetweenValueSamples) value records for each count found
         * in the current histogram that is larger than the expectedIntervalBetweenValueSamples.
         *
         * Note: This is a post-recording correction method, as opposed to the at-recording correction method provided
         * by {@link #RecordValueWithExpectedInterval(long, long) RecordValueWithExpectedInterval}. The two
         * methods are mutually exclusive, and only one of the two should be be used on a given data set to correct
         * for the same coordinated omission issue.
         * by
         * <p>
         * See notes in the description of the Histogram calls for an illustration of why this corrective behavior is
         * important.
         *
         * @param fromHistogram The other histogram. highestTrackableValue and largestValueWithSingleUnitResolution must match.
         * @param expectedIntervalBetweenValueSamples If expectedIntervalBetweenValueSamples is larger than 0, add
         *                                           auto-generated value records as appropriate if value is larger
         *                                           than expectedIntervalBetweenValueSamples
         * @throws ArrayIndexOutOfBoundsException
         */
        public void AddWhileCorrectingForCoordinatedOmission(AbstractHistogram fromHistogram, long expectedIntervalBetweenValueSamples) {
            AbstractHistogram toHistogram = this;

            foreach (HistogramIterationValue v in fromHistogram.GetHistogramData().RecordedValues()) {
                toHistogram.RecordValueWithCountAndExpectedInterval(v.GetValueIteratedTo(),
                        v.GetCountAtValueIteratedTo(), expectedIntervalBetweenValueSamples);
            }
        }

        /**
         * Determine if this histogram had any of it's value counts overflow.
         * Since counts are kept in fixed integer form with potentially limited range (e.g. int and short), a
         * specific value range count could potentially overflow, leading to an inaccurate and misleading histogram
         * representation. This method accurately determines whether or not an overflow condition has happened in an
         * IntHistogram or ShortHistogram.
         *
         * @return True if this histogram has had a count value overflow.
         */
        public bool HasOverflowed() {
            // On overflow, the totalCount accumulated counter will (always) not match the total of counts
            long totalCounted = 0;
            for (int i = 0; i < countsArrayLength; i++) {
                totalCounted += GetCountAtIndex(i);
            }
            return (totalCounted != GetTotalCount());
        }

        /**
         * Determine if this histogram is equivalent to another.
         *
         * @param other the other histogram to compare to
         * @return True if this histogram are equivalent with the other.
         */
        public bool Equals(Object other){
            if ( this == other ) return true;
            if ( !(other is AbstractHistogram) ) return false;
            var that = (AbstractHistogram)other;
            if ((highestTrackableValue != that.highestTrackableValue) ||
                    (numberOfSignificantValueDigits != that.numberOfSignificantValueDigits))
                return false;
            if (countsArrayLength != that.countsArrayLength)
                return false;
            if (GetTotalCount() != that.GetTotalCount())
                return false;
            return true;
        }

        /**
         * Provide access to the histogram's data set.
         * @return a {@link HistogramData} that can be used to query stats and iterate through the default (corrected)
         * data set.
         */
        public HistogramData GetHistogramData() {
            return histogramData;
        }

        /**
         * Get the size (in value units) of the range of values that are equivalent to the given value within the
         * histogram's resolution. Where "equivalent" means that value samples recorded for any two
         * equivalent values are counted in a common total count.
         *
         * @param value The given value
         * @return The lowest value that is equivalent to the given value within the histogram's resolution.
         */
        public long SizeOfEquivalentValueRange(long value) {
            int bucketIndex = GetBucketIndex(value);
            int subBucketIndex = GetSubBucketIndex(value, bucketIndex);
            long distanceToNextValue =
                    (1 << ((subBucketIndex >= subBucketCount) ? (bucketIndex + 1) : bucketIndex));
            return distanceToNextValue;
        }

        /**
         * Get the lowest value that is equivalent to the given value within the histogram's resolution.
         * Where "equivalent" means that value samples recorded for any two
         * equivalent values are counted in a common total count.
         *
         * @param value The given value
         * @return The lowest value that is equivalent to the given value within the histogram's resolution.
         */
        public long lowestEquivalentValue(long value) {
            int bucketIndex = GetBucketIndex(value);
            int subBucketIndex = GetSubBucketIndex(value, bucketIndex);
            long thisValueBaseLevel = subBucketIndex << bucketIndex;
            return thisValueBaseLevel;
        }

        /**
         * Get the highest value that is equivalent to the given value within the histogram's resolution.
         * Where "equivalent" means that value samples recorded for any two
         * equivalent values are counted in a common total count.
         *
         * @param value The given value
         * @return The highest value that is equivalent to the given value within the histogram's resolution.
         */
        public long HighestEquivalentValue(long value) {
            return NextNonEquivalentValue(value) - 1;
        }

        /**
         * Get a value that lies in the middle (rounded up) of the range of values equivalent the given value.
         * Where "equivalent" means that value samples recorded for any two
         * equivalent values are counted in a common total count.
         *
         * @param value The given value
         * @return The value lies in the middle (rounded up) of the range of values equivalent the given value.
         */
        public long MedianEquivalentValue(long value) {
            return (lowestEquivalentValue(value) + (SizeOfEquivalentValueRange(value) >> 1));
        }

        /**
         * Get the next value that is not equivalent to the given value within the histogram's resolution.
         * Where "equivalent" means that value samples recorded for any two
         * equivalent values are counted in a common total count.
         *
         * @param value The given value
         * @return The next value that is not equivalent to the given value within the histogram's resolution.
         */
        public long NextNonEquivalentValue(long value) {
            return lowestEquivalentValue(value) + SizeOfEquivalentValueRange(value);
        }

        /**
         * Determine if two values are equivalent with the histogram's resolution.
         * Where "equivalent" means that value samples recorded for any two
         * equivalent values are counted in a common total count.
         *
         * @param value1 first value to compare
         * @param value2 second value to compare
         * @return True if values are equivalent with the histogram's resolution.
         */
        public bool ValuesAreEquivalent(long value1, long value2) {
            return (lowestEquivalentValue(value1) == lowestEquivalentValue(value2));
        }

        private static long serialVersionUID = 42L;

        //TODO: WriteObject & ReadObject
        //private void WriteObject(ObjectOutputStream o)
        //{
        //    o.writeLong(highestTrackableValue);
        //    o.writeInt(numberOfSignificantValueDigits);
        //    o.writeLong(getTotalCount()); // Needed because overflow situations may lead this to differ from counts totals
        //}

        //private void ReadObject(ObjectInputStream o) {
        //    long highestTrackableValue = o.ReadLong();
        //    int numberOfSignificantValueDigits = o.ReadInt();
        //    long totalCount = o.ReadLong();
        //    Init(highestTrackableValue, numberOfSignificantValueDigits, totalCount);
        //}

    }
}