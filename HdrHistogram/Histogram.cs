﻿namespace HdrHistogram
{
    using System;
    using System.Collections.Generic;

    public class Histogram : AbstractHistogram
    {
        public Histogram(long highestTrackableValue, int numberOfSignificantValueDigits)
            : base(highestTrackableValue, numberOfSignificantValueDigits)
        {
        }

        public string MedianEquivalentValue(int p0)
        {
            throw new NotImplementedException();
        }

        public override long GetCountAtIndex(int index)
        {
            throw new NotImplementedException();
        }

        public override void IncrementCountAtIndex(int index)
        {
            throw new NotImplementedException();
        }

        public override void AddToCountAtIndex(int index, long value)
        {
            throw new NotImplementedException();
        }

        public override long GetTotalCount()
        {
            throw new NotImplementedException();
        }

        public override void SetTotalCount(long totalCount)
        {
            throw new NotImplementedException();
        }

        public override void IncrementTotalCount()
        {
            throw new NotImplementedException();
        }

        public override void AddToTotalCount(long value)
        {
            throw new NotImplementedException();
        }

        public override void ClearCounts()
        {
            throw new NotImplementedException();
        }

        public override AbstractHistogram Copy()
        {
            throw new NotImplementedException();
        }
    
        public override AbstractHistogram CopyCorrectedForCoordinatedOmission(long expectedIntervalBetweenValueSamples)
        {
            throw new NotImplementedException();
        }

        public override int GetEstimatedFootprintInBytes()
        {
            throw new NotImplementedException();
        }

        public string LowestEquivalentValue(int i)
        {
            throw new NotImplementedException();
        }
    }
}