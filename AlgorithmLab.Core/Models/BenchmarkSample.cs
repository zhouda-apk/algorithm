using System;
namespace AlgorithmLab.Core.Models
{
    public sealed class BenchmarkSample
    {
        public int SampleIndex { get; }
        public long ElapsedTimerTicks { get; }
        public bool IsValid { get; }
        public BenchmarkSample(int sampleIndex, long elapsedTimerTicks, bool isValid)
        {
            if (sampleIndex < 1 || elapsedTimerTicks < 0) throw new ArgumentOutOfRangeException(nameof(sampleIndex));
            SampleIndex = sampleIndex;
            ElapsedTimerTicks = elapsedTimerTicks;
            IsValid = isValid;
        }
    }
}
