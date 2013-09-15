namespace HdrHistogram
{
    using System;
    using System.Collections.Generic;

    public class Histogram : AbstractHistogram
    {
        public Histogram(long highestTrackableValue, int numberOfSignificantValueDigits)
        {
            throw new System.NotImplementedException();
        }

        public string MedianEquivalentValue(int p0)
        {
            throw new NotImplementedException();
        }
    }
    
    public class AtomicHistogram : AbstractHistogram
    {
        public AtomicHistogram(object highestTrackableValue, object numberOfSignificantValueDigits)
        {
            throw new NotImplementedException();
        }
    }

    public class SynchronizedHistogram : AbstractHistogram
    {
        public SynchronizedHistogram(object highestTrackableValue, object numberOfSignificantValueDigits)
        {
            throw new NotImplementedException();
        }
    }
  
}