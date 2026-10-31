using System;
using System.Collections.Generic;
using System.Linq;
namespace AlgorithmLab.Core.Models
{
    public sealed class PendingRunFinalization
    {
        public Guid RunId { get; }
        public BenchmarkStatus TargetStatus { get; }
        public DateTime FinishedAtUtc { get; }
        public IReadOnlyList<BenchmarkSample> Samples { get; }
        public BenchmarkStatistics Statistics { get; }
        public SortMetrics Metrics { get; }
        public string OutputPreviewText { get; }
        public string ErrorCode { get; }
        public string ErrorMessage { get; }
        public PendingRunFinalization(Guid runId, BenchmarkStatus targetStatus, DateTime finishedAtUtc, IEnumerable<BenchmarkSample> samples,
            BenchmarkStatistics statistics = null, SortMetrics metrics = null, string outputPreviewText = null, string errorCode = null, string errorMessage = null)
        {
            CaseRules.Require(runId != Guid.Empty && Enum.IsDefined(typeof(BenchmarkStatus), targetStatus) && targetStatus != BenchmarkStatus.Running && finishedAtUtc.Kind == DateTimeKind.Utc, "Invalid finalization.");
            if (samples == null) throw new ArgumentNullException(nameof(samples));
            RunId = runId; TargetStatus = targetStatus; FinishedAtUtc = finishedAtUtc; Samples = samples.ToList().AsReadOnly();
            CaseRules.Require(Samples.All(s => s != null), "Invalid sample.");
            CaseRules.Require(targetStatus == BenchmarkStatus.Completed ? statistics != null && metrics != null && outputPreviewText != null && errorCode == null && errorMessage == null : statistics == null, "Invalid final statistics.");
            Statistics = statistics; Metrics = metrics == null ? null : new SortMetrics(metrics.ComparisonCount, metrics.SwapCount, metrics.WriteCount);
            OutputPreviewText = outputPreviewText; ErrorCode = errorCode; ErrorMessage = errorMessage;
        }
    }
}
