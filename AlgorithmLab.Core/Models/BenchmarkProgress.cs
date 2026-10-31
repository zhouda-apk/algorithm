using System;
namespace AlgorithmLab.Core.Models
{
    public sealed class BenchmarkProgress
    {
        public string Stage { get; }
        public int CompletedSampleCount { get; }
        public int TotalSampleCount { get; }
        public BenchmarkProgress(string stage, int completedSampleCount, int totalSampleCount)
        {

            Stage = stage;
            CompletedSampleCount = completedSampleCount;
            TotalSampleCount = totalSampleCount;
        }
    }
}
