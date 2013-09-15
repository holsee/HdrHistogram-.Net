namespace HdrHistogram
{
    using System.Collections.Generic;
    using System.IO;

    public class HistogramData
    {
        public HistogramData(AbstractHistogram abstractHistogram)
        {
            throw new System.NotImplementedException();
        }

        public IEnumerable<HistogramIterationValue> RecordedValues()
        {
            throw new System.NotImplementedException();
        }

        public void OutputPercentileDistribution(TextWriter @out, double d, double x  = 0)
        {
            throw new System.NotImplementedException();
        }

        public long GetTotalCount()
        {
            throw new System.NotImplementedException();
        }

        public long GetMaxValue()
        {
            throw new System.NotImplementedException();
        }

        public long GetMinValue()
        {
            throw new System.NotImplementedException();
        }

        public long GetCountAtValue(long testValueLevel)
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

        public int GetCountBetweenValues(long p0, long p1)
        {
            throw new System.NotImplementedException();
        }

        public IEnumerable<HistogramIterationValue> LinearBucketValues(long p0)
        {
            throw new System.NotImplementedException();
        }

        public IEnumerable<HistogramIterationValue> LogarithmicBucketValues(int p0, int p1)
        {
            throw new System.NotImplementedException();
        }

        public IEnumerable<HistogramIterationValue> AllValues()
        {
            throw new System.NotImplementedException();
        }
    }
}