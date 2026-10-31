using System;
namespace AlgorithmLab.Core.Models
{
    public sealed class BenchmarkOptions
    {
        public int WarmupCount { get; }
        public int MeasuredCount { get; }
        public int TimeoutSeconds { get; }
        public BenchmarkOptions(int warmupCount = 1, int measuredCount = 5, int timeoutSeconds = 30)
        {
            CaseRules.Require((warmupCount == 1 || warmupCount == 3) && measuredCount >= 5 && measuredCount <= 30 && timeoutSeconds >= 10 && timeoutSeconds <= 120, "Invalid benchmark options.");
            WarmupCount = warmupCount;
            MeasuredCount = measuredCount;
            TimeoutSeconds = timeoutSeconds;
        }
    }
}
