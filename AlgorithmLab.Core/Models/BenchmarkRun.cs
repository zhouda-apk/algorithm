using System;
using System.Collections.Generic;
namespace AlgorithmLab.Core.Models
{
    public sealed class BenchmarkRun
    {
        public Guid RunId { get; }
        public TestCaseSnapshot Input { get; }
        public AlgorithmDescriptor Algorithm { get; }
        public string MetricsVersion { get { return "metrics-1"; } }
        public BenchmarkOptions Options { get; }
        public EnvironmentInfo Environment { get; }
        public DateTime StartedAtUtc { get; }
        public PendingRunFinalization Finalization { get; }
        public BenchmarkStatus Status { get { return Finalization == null ? BenchmarkStatus.Running : Finalization.TargetStatus; } }
        public DateTime? FinishedAtUtc { get { return Finalization == null ? (DateTime?)null : Finalization.FinishedAtUtc; } }
        public IReadOnlyList<BenchmarkSample> Samples { get { return Finalization == null ? new List<BenchmarkSample>().AsReadOnly() : Finalization.Samples; } }
        public BenchmarkStatistics Statistics { get { return Finalization == null ? null : Finalization.Statistics; } }
        public SortMetrics Metrics { get { return Finalization == null ? null : Finalization.Metrics; } }
        public string OutputPreviewText { get { return Finalization == null ? null : Finalization.OutputPreviewText; } }
        public string ErrorCode { get { return Finalization == null ? null : Finalization.ErrorCode; } }
        public string ErrorMessage { get { return Finalization == null ? null : Finalization.ErrorMessage; } }
        public BenchmarkRun(Guid runId, TestCaseSnapshot input, AlgorithmDescriptor algorithm, BenchmarkOptions options, EnvironmentInfo environment, DateTime startedAtUtc, PendingRunFinalization finalization = null)
        {
            CaseRules.Require(runId != Guid.Empty && input != null && algorithm != null && options != null && environment != null && startedAtUtc.Kind == DateTimeKind.Utc, "Invalid run.");
            if (finalization != null) {
                CaseRules.Require(finalization.RunId == runId && finalization.FinishedAtUtc >= startedAtUtc, "Invalid finalization identity/time.");
                CaseRules.Require(finalization.Samples.Count <= options.MeasuredCount, "Too many samples.");
                if (finalization.TargetStatus == BenchmarkStatus.Completed) CaseRules.Require(finalization.Samples.Count == options.MeasuredCount, "Missing completed samples.");
            }
            RunId = runId; Input = input; Algorithm = algorithm; Options = options; Environment = environment; StartedAtUtc = startedAtUtc; Finalization = finalization;
        }
        public BenchmarkRun With(PendingRunFinalization finalization) { return new BenchmarkRun(RunId, Input, Algorithm, Options, Environment, StartedAtUtc, finalization); }
    }
}
