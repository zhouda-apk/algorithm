using System.Linq;
using System;
namespace AlgorithmLab.Core.Models
{
    public sealed class BenchmarkStatistics
    {
        public double MedianMs { get; }
        public double MeanMs { get; }
        public double MinMs { get; }
        public double MaxMs { get; }
        public double PopulationStdDevMs { get; }
        public BenchmarkStatistics(double medianMs, double meanMs, double minMs, double maxMs, double populationStdDevMs)
        {
            if (new[] { medianMs, meanMs, minMs, maxMs, populationStdDevMs }.Any(v => double.IsNaN(v) || double.IsInfinity(v) || v < 0) || minMs > medianMs || medianMs > maxMs || meanMs < minMs || meanMs > maxMs) throw new ArgumentException("Invalid statistics.");
            MedianMs = medianMs;
            MeanMs = meanMs;
            MinMs = minMs;
            MaxMs = maxMs;
            PopulationStdDevMs = populationStdDevMs;
        }
    }
}
