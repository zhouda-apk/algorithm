using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using AlgorithmLab.Core.Contracts;
using AlgorithmLab.Core.Models;
namespace AlgorithmLab.Core.Services
{
    public sealed class BenchmarkService
    {
        private readonly AlgorithmCatalog catalog;
        private readonly IBenchmarkRepository repository;
        private readonly ITestCaseRepository cases;
        private readonly Action<Exception> log;
        private int busy;
        public BenchmarkService(AlgorithmCatalog catalog, IBenchmarkRepository repository, ITestCaseRepository cases, Action<Exception> log = null)
        { this.catalog = catalog ?? throw new ArgumentNullException(nameof(catalog)); this.repository = repository ?? throw new ArgumentNullException(nameof(repository)); this.cases = cases ?? throw new ArgumentNullException(nameof(cases)); this.log = log ?? (e => Trace.TraceError(e.ToString())); }
        private void Enter() { if (Interlocked.CompareExchange(ref busy, 1, 0) != 0) throw new AlgorithmLabException(ErrorKind.Conflict, "已有測試工作進行中。"); }
        private void Log(Exception e) { try { log(e); } catch { /* Logging cannot hide the original result. */ } }
        private void Report(IProgress<BenchmarkProgress> progress, string stage, int done, int total)
        { if (progress != null) try { progress.Report(new BenchmarkProgress(stage, done, total)); } catch (Exception e) { Log(e); } }
        public BenchmarkRun Run(BenchmarkRequest request, CancellationToken cancellationToken = default(CancellationToken), IProgress<BenchmarkProgress> progress = null)
        {
            if (request == null) throw new ArgumentNullException(nameof(request)); Enter();
            try
            {
                var algorithm = catalog.GetById(request.AlgorithmId); var input = request.Input; var options = request.Options;
                var original = IntArrayCodec.Decode(input.InputDataText, input.DataCount, input.InputHash);
                if (input.TestCaseId.HasValue) {
                    var latest = cases.GetById(input.TestCaseId.Value);
                    if (latest.Revision != input.CaseRevision || new TestCaseSnapshot(latest).InputHash != input.InputHash) throw new AlgorithmLabException(ErrorKind.Conflict, "案例版次或資料已改變，請重新選取。");
                }
                var expected = SortResultValidator.CreateExpected(original);
                var environment = EnvironmentInfoCollector.Collect();
                var run = new BenchmarkRun(Guid.NewGuid(), input, algorithm.Descriptor, options, environment, DateTime.UtcNow);
                repository.CreateRunning(run); // Must succeed before invoking Sort.
                var samples = new List<BenchmarkSample>(); SortMetrics metrics = null; BenchmarkStatistics statistics = null;
                var status = BenchmarkStatus.Completed; string errorCode = null, errorMessage = null, preview = null;
                using (var linked = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken))
                {
                    linked.CancelAfter(TimeSpan.FromSeconds(options.TimeoutSeconds));
                    try
                    {
                        Report(progress, "Preparing", 0, options.MeasuredCount); linked.Token.ThrowIfCancellationRequested();
                        Report(progress, "Warmup", 0, options.MeasuredCount);
                        for (int i = 0; i < options.WarmupCount; i++) {
                            var work = (int[])original.Clone(); algorithm.Sort(work, new SortExecutionContext(SortExecutionMode.Timing, linked.Token)); SortResultValidator.Validate(expected, work);
                        }
                        int[] last = null;
                        for (int i = 1; i <= options.MeasuredCount; i++) {
                            linked.Token.ThrowIfCancellationRequested();
                            var work = (int[])original.Clone(); var context = new SortExecutionContext(SortExecutionMode.Timing, linked.Token);
                            long start = Stopwatch.GetTimestamp();
                            algorithm.Sort(work, context);
                            long elapsed = Stopwatch.GetTimestamp() - start;
                            try { SortResultValidator.Validate(expected, work); }
                            catch (AlgorithmLabException) { samples.Add(new BenchmarkSample(i, elapsed, false)); throw; }
                            samples.Add(new BenchmarkSample(i, elapsed, true)); last = work;
                            Report(progress, "Measuring", i, options.MeasuredCount);
                        }
                        Report(progress, "Analysis", samples.Count, options.MeasuredCount);
                        var analysisData = (int[])original.Clone(); var analysis = new SortExecutionContext(SortExecutionMode.Analysis, linked.Token);
                        algorithm.Sort(analysisData, analysis); SortResultValidator.Validate(expected, analysisData);
                        metrics = new SortMetrics(analysis.Metrics.ComparisonCount, analysis.Metrics.SwapCount, analysis.Metrics.WriteCount);
                        if (IntArrayCodec.ComputeHash(IntArrayCodec.Encode(input.Data)) != input.InputHash) throw new AlgorithmLabException(ErrorKind.SortValidationFailure, "Input snapshot changed.");
                        statistics = BenchmarkStatisticsCalculator.Calculate(samples, environment.StopwatchFrequency);
                        preview = string.Join(",", last.Take(20));
                        Report(progress, "Saving", samples.Count, options.MeasuredCount); linked.Token.ThrowIfCancellationRequested();
                    }
                    catch (OperationCanceledException) when (linked.IsCancellationRequested) {
                        status = BenchmarkStatus.Cancelled; errorCode = cancellationToken.IsCancellationRequested ? "USER_CANCELLED" : "TIMEOUT"; errorMessage = "測試已取消或超時。"; statistics = null;
                    }
                    catch (Exception e) {
                        status = BenchmarkStatus.Failed; errorCode = e is AlgorithmLabException && ((AlgorithmLabException)e).Kind == ErrorKind.SortValidationFailure ? "SORT_INVALID" : "UNEXPECTED";
                        errorMessage = e.Message.Substring(0, Math.Min(1000, e.Message.Length)); statistics = null; Log(e);
                    }
                }
                var finished = DateTime.UtcNow; if (finished < run.StartedAtUtc) finished = run.StartedAtUtc;
                var finalization = new PendingRunFinalization(run.RunId, status, finished, samples, statistics, metrics, preview, errorCode, errorMessage);
                var saved = Save(finalization); Report(progress, "Finished", samples.Count, options.MeasuredCount); return saved;
            }
            finally { Volatile.Write(ref busy, 0); }
        }
        private BenchmarkRun Save(PendingRunFinalization finalization)
        {
            try { return repository.FinalizeRun(finalization); }
            catch (AlgorithmLabException e) when (e.Kind == ErrorKind.Persistence) { Log(e); throw new BenchmarkPersistenceException(finalization, e); }
        }
        public BenchmarkRun RetrySave(PendingRunFinalization finalization)
        {
            if (finalization == null) throw new ArgumentNullException(nameof(finalization)); Enter();
            try { return Save(finalization); } finally { Volatile.Write(ref busy, 0); }
        }
    }
}
