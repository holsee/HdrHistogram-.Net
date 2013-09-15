namespace HdrHistogram
{
    using System;
    using System.Collections.Generic;
    using System.IO;

    public abstract class AbstractHistogram
    {
        public void RecordValueWithExpectedInterval(long p0, int p1)
        {
            throw new System.NotImplementedException();
        }

        public void RecordValue(long p0)
        {
            throw new System.NotImplementedException();
        }

        public Histogram CopyCorrectedForCoordinatedOmission(int i)
        {
            throw new System.NotImplementedException();
        }

        public bool ValuesAreEquivalent(long l, object getMaxValue)
        {
            throw new System.NotImplementedException();
        }

        public double GetMean()
        {
            throw new System.NotImplementedException();
        }

        public double GetStdDeviation()
        {
            throw new System.NotImplementedException();
        }

        public double GetValueAtPercentile(double p0)
        {
            throw new System.NotImplementedException();
        }

        public double GetPercentileAtOrBelowValue(long i)
        {
            throw new System.NotImplementedException();
        }

        public double GetCountBetweenValues(long p0, long p1)
        {
            throw new System.NotImplementedException();
        }

        public int GetCountAtValue(long l)
        {
            throw new System.NotImplementedException();
        }

        public IEnumerable<HistogramIterationValue> LinearBucketValues(int i)
        {
            throw new System.NotImplementedException();
        }

        public IEnumerable<HistogramIterationValue> LogarithmicBucketValues(int i, int i1)
        {
            throw new System.NotImplementedException();
        }

        public IEnumerable<HistogramIterationValue> RecordedValues()
        {
            throw new System.NotImplementedException();
        }

        public IEnumerable<HistogramIterationValue> AllValues()
        {
            throw new System.NotImplementedException();
        }

        public void RecordValueWithExpectedInterval(long l, long expectedInterval)
        {
            throw new NotImplementedException();
        }

        public AbstractHistogram GetHistogramData()
        {
            throw new NotImplementedException();
        }

        public long GetTotalCount()
        {
            throw new NotImplementedException();
        }

        public void Reset()
        {
            throw new NotImplementedException();
        }

        public long GetMaxValue()
        {
            throw new NotImplementedException();
        }

        public long GetMinValue()
        {
            throw new System.NotImplementedException();
        }

        public long GetHighestTrackableValue()
        {
            throw new NotImplementedException();
        }

        public int GetNumberOfSignificantValueDigits()
        {
            throw new NotImplementedException();
        }

        public long GetEstimatedFootprintInBytes()
        {
            throw new NotImplementedException();
        }

        public void Add(Histogram other)
        {
            throw new NotImplementedException();
        }

        public string SizeOfEquivalentValueRange(int p0)
        {
            throw new NotImplementedException();
        }

        public string LowestEquivalentValue(int p0)
        {
            throw new NotImplementedException();
        }

        public string HighestEquivalentValue(int p0)
        {
            throw new NotImplementedException();
        }

        public bool HasOverflowed()
        {
            throw new System.NotImplementedException();
        }

        public AbstractHistogram Copy()
        {
            throw new NotImplementedException();
        }

        public void OutputPercentileDistribution(TextWriter @out, double i, double d)
        {
            throw new NotImplementedException();
        }

        public void OutputPercentileDistribution(TextWriter @out, double i)
        {
            throw new NotImplementedException();
        }
    }
}