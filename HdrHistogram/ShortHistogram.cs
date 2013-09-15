namespace HdrHistogram
{
    public class ShortHistogram : AbstractHistogram
    {
        public ShortHistogram(long highestTrackableValue, int numberOfSignificantValueDigits)
            : base(highestTrackableValue, numberOfSignificantValueDigits)
        {
        }

        public override long GetCountAtIndex(int index)
        {
            throw new System.NotImplementedException();
        }

        public override void IncrementCountAtIndex(int index)
        {
            throw new System.NotImplementedException();
        }

        public override void AddToCountAtIndex(int index, long value)
        {
            throw new System.NotImplementedException();
        }

        public override long GetTotalCount()
        {
            throw new System.NotImplementedException();
        }

        public override void SetTotalCount(long totalCount)
        {
            throw new System.NotImplementedException();
        }

        public override void IncrementTotalCount()
        {
            throw new System.NotImplementedException();
        }

        public override void AddToTotalCount(long value)
        {
            throw new System.NotImplementedException();
        }

        public override void ClearCounts()
        {
            throw new System.NotImplementedException();
        }

        public override AbstractHistogram Copy()
        {
            throw new System.NotImplementedException();
        }

        public override AbstractHistogram CopyCorrectedForCoordinatedOmission(long expectedIntervalBetweenValueSamples)
        {
            throw new System.NotImplementedException();
        }

        public override int GetEstimatedFootprintInBytes()
        {
            throw new System.NotImplementedException();
        }
    }
}