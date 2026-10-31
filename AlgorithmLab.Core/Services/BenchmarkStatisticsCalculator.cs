using System;
using System.Collections.Generic;
using System.Linq;
using AlgorithmLab.Core.Models;
namespace AlgorithmLab.Core.Services
{
    public static class BenchmarkStatisticsCalculator
    {
        public static BenchmarkStatistics Calculate(IEnumerable<BenchmarkSample> samples, long frequency)
        {
            if (samples == null) throw new ArgumentNullException(nameof(samples));
            var list = samples.ToList(); CaseRules.Require(frequency > 0 && list.Count > 0 && list.All(s => s != null && s.IsValid), "Valid samples and positive frequency are required.");
            var values = list.Select(s => s.ElapsedTimerTicks * 1000.0 / frequency).OrderBy(x => x).ToArray();
            double mean = values.Average(); int mid = values.Length / 2;
            double median = values.Length % 2 == 0 ? (values[mid - 1] + values[mid]) / 2 : values[mid];
            return new BenchmarkStatistics(median, mean, values[0], values[values.Length - 1], Math.Sqrt(values.Select(v => (v - mean) * (v - mean)).Average()));
        }
    }
}
