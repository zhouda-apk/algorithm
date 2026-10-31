using System;
namespace AlgorithmLab.Core.Models
{
    public sealed class SortMetrics
    {
        public long ComparisonCount { get; private set; }
        public long SwapCount { get; private set; }
        public long WriteCount { get; private set; }
        public SortMetrics(long comparisonCount = 0, long swapCount = 0, long writeCount = 0)
        {
            if (comparisonCount < 0 || swapCount < 0 || writeCount < 0) throw new ArgumentOutOfRangeException(nameof(comparisonCount), "Counts must be nonnegative.");
            ComparisonCount = comparisonCount; SwapCount = swapCount; WriteCount = writeCount;
        }
        internal void Compare() { ComparisonCount = checked(ComparisonCount + 1); }
        internal void Swap() { SwapCount = checked(SwapCount + 1); WriteCount = checked(WriteCount + 2); }
        internal void Write() { WriteCount = checked(WriteCount + 1); }
    }
}
