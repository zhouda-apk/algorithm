using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Json;
using AlgorithmLab.Core.Algorithms;
using AlgorithmLab.Core.Contracts;
using AlgorithmLab.Core.Models;
using AlgorithmLab.Core.Services;
using AlgorithmLab.Data.FileStorage;
namespace AlgorithmLab.Data.Repositories
{
    public sealed class JsonBenchmarkRepository : IBenchmarkRepository
    {
        private readonly JsonDataStore store;
        private sealed class AlreadyFinalized : Exception { }
        public JsonBenchmarkRepository(JsonDataStore store) { this.store = store ?? throw new ArgumentNullException(nameof(store)); }
        private static string Time(DateTime value) { return value.ToString("o", CultureInfo.InvariantCulture); }
        private static DateTime ParseTime(string value) { return DateTime.ParseExact(value, "o", CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind); }
        private static BenchmarkRunDto Find(StoreDocument document, Guid id)
        {
            var value = document.BenchmarkRuns.SingleOrDefault(r => Guid.Parse(r.Id) == id);
            if (value == null) throw new AlgorithmLabException(ErrorKind.NotFound, "找不到測試紀錄。"); return value;
        }
        private static AlgorithmDescriptor Descriptor(string id, string version)
        {
            AlgorithmDescriptor known = id == "bubble" ? new BubbleSort().Descriptor : id == "selection" ? new SelectionSort().Descriptor : id == "insertion" ? new InsertionSort().Descriptor : null;
            return known != null && known.Version == version ? known : new AlgorithmDescriptor(id, id, version, "Unknown historical metadata", false, false);
        }
        internal static BenchmarkRun FromDto(BenchmarkRunDto d)
        {
            var input = new TestCaseSnapshot(d.TestCaseId == null ? (Guid?)null : Guid.Parse(d.TestCaseId), d.CaseRevision, d.CaseName,
                (DataDistribution)Enum.Parse(typeof(DataDistribution), d.Distribution), IntArrayCodec.Decode(d.InputDataText, d.DataCount, d.InputHash), d.Seed, d.GeneratorVersion, d.MinValue, d.MaxValue);
            var options = new BenchmarkOptions(d.WarmupCount, d.RequestedSampleCount, d.TimeoutSeconds);
            var env = new EnvironmentInfo(d.AppVersion, d.BuildConfiguration, d.ProcessArchitecture, d.ClrVersion, d.OsVersion, d.MachineName, d.CpuModel, d.EnvironmentKey, d.ProcessorCount, d.DebuggerAttached, d.IsHighResolution, d.StopwatchFrequency);
            PendingRunFinalization finalization = null;
            if (d.Status != "Running") {
                var stats = d.MedianMs.HasValue ? new BenchmarkStatistics(d.MedianMs.Value, d.MeanMs.Value, d.MinMs.Value, d.MaxMs.Value, d.PopulationStdDevMs.Value) : null;
                var metrics = d.ComparisonCount.HasValue ? new SortMetrics(d.ComparisonCount.Value, d.SwapCount.Value, d.WriteCount.Value) : null;
                finalization = new PendingRunFinalization(Guid.Parse(d.Id), (BenchmarkStatus)Enum.Parse(typeof(BenchmarkStatus), d.Status), ParseTime(d.FinishedAtUtc),
                    d.Samples.Select(s => new BenchmarkSample(s.SampleIndex, s.ElapsedTimerTicks, s.IsValid)), stats, metrics, d.OutputPreviewText, d.ErrorCode, d.ErrorMessage);
            }
            return new BenchmarkRun(Guid.Parse(d.Id), input, Descriptor(d.AlgorithmId, d.AlgorithmVersion), options, env, ParseTime(d.StartedAtUtc), finalization);
        }
        internal static BenchmarkRunDto ToDto(BenchmarkRun r)
        {
            var i = r.Input; var e = r.Environment; var s = r.Statistics; var m = r.Metrics;
            return new BenchmarkRunDto {
                Id = r.RunId.ToString("D"), TestCaseId = i.TestCaseId.HasValue ? i.TestCaseId.Value.ToString("D") : null, CaseRevision = i.CaseRevision, CaseName = i.CaseName,
                Distribution = i.Distribution.ToString(), DataCount = i.DataCount, Seed = i.Seed, GeneratorVersion = i.GeneratorVersion, MinValue = i.MinValue, MaxValue = i.MaxValue,
                InputDataText = i.InputDataText, InputFormatVersion = i.InputFormatVersion, InputHash = i.InputHash,
                AlgorithmId = r.Algorithm.Id, AlgorithmVersion = r.Algorithm.Version, MetricsVersion = r.MetricsVersion,
                WarmupCount = r.Options.WarmupCount, RequestedSampleCount = r.Options.MeasuredCount, TimeoutSeconds = r.Options.TimeoutSeconds,
                Status = r.Status.ToString(), StartedAtUtc = Time(r.StartedAtUtc), FinishedAtUtc = r.FinishedAtUtc.HasValue ? Time(r.FinishedAtUtc.Value) : null,
                StopwatchFrequency = e.StopwatchFrequency, IsHighResolution = e.IsHighResolution,
                AppVersion = e.AppVersion, BuildConfiguration = e.BuildConfiguration, ProcessArchitecture = e.ProcessArchitecture, ClrVersion = e.ClrVersion,
                OsVersion = e.OsVersion, MachineName = e.MachineName, CpuModel = e.CpuModel, EnvironmentKey = e.EnvironmentKey, ProcessorCount = e.ProcessorCount, DebuggerAttached = e.DebuggerAttached,
                MedianMs = s == null ? (double?)null : s.MedianMs, MeanMs = s == null ? (double?)null : s.MeanMs, MinMs = s == null ? (double?)null : s.MinMs, MaxMs = s == null ? (double?)null : s.MaxMs, PopulationStdDevMs = s == null ? (double?)null : s.PopulationStdDevMs,
                ComparisonCount = m == null ? (long?)null : m.ComparisonCount, SwapCount = m == null ? (long?)null : m.SwapCount, WriteCount = m == null ? (long?)null : m.WriteCount,
                OutputPreviewText = r.OutputPreviewText, ErrorCode = r.ErrorCode, ErrorMessage = r.ErrorMessage,
                Samples = r.Samples.Select(x => new BenchmarkSampleDto { SampleIndex = x.SampleIndex, ElapsedTimerTicks = x.ElapsedTimerTicks, IsValid = x.IsValid }).ToList()
            };
        }
        private static byte[] Encode(BenchmarkRunDto value)
        { using (var stream = new MemoryStream()) { new DataContractJsonSerializer(typeof(BenchmarkRunDto)).WriteObject(stream, value); return stream.ToArray(); } }
        public void CreateRunning(BenchmarkRun run)
        {
            if (run == null) throw new ArgumentNullException(nameof(run));
            if (run.Status != BenchmarkStatus.Running) throw new AlgorithmLabException(ErrorKind.Validation, "New run must be Running.");
            store.Commit(d => {
                if (d.BenchmarkRuns.Any(r => Guid.Parse(r.Id) == run.RunId)) throw new AlgorithmLabException(ErrorKind.Conflict, "Run Id already exists.");
                if (run.Input.TestCaseId.HasValue) {
                    var c = d.TestCases.SingleOrDefault(x => Guid.Parse(x.Id) == run.Input.TestCaseId.Value);
                    if (c == null) throw new AlgorithmLabException(ErrorKind.NotFound, "案例已刪除。");
                    if (c.Revision != run.Input.CaseRevision || IntArrayCodec.ComputeHash(IntArrayCodec.Encode(c.Data)) != run.Input.InputHash) throw new AlgorithmLabException(ErrorKind.Conflict, "案例已變更。");
                }
                d.BenchmarkRuns.Add(ToDto(run));
            });
        }
        public BenchmarkRun FinalizeRun(PendingRunFinalization finalization)
        {
            if (finalization == null) throw new ArgumentNullException(nameof(finalization));
            BenchmarkRun result = null;
            try {
                store.Commit(d => {
                    var old = Find(d, finalization.RunId); var updated = FromDto(old).With(finalization); var proposed = ToDto(updated);
                    if (old.Status != "Running") {
                        if (Encode(old).SequenceEqual(Encode(proposed))) { result = FromDto(old); throw new AlreadyFinalized(); }
                        throw new AlgorithmLabException(ErrorKind.Conflict, "Final run content cannot be overwritten.");
                    }
                    d.BenchmarkRuns[d.BenchmarkRuns.IndexOf(old)] = proposed; result = updated;
                });
            }
            catch (AlreadyFinalized) { }
            return result;
        }
        public BenchmarkRun GetDetails(Guid runId) { return FromDto(Find(store.Load(), runId)); }
        public IReadOnlyList<BenchmarkRun> GetHistory(BenchmarkHistoryQuery query)
        {
            if (query == null) throw new ArgumentNullException(nameof(query));
            return store.Load().BenchmarkRuns.Where(d => (!query.TestCaseId.HasValue || d.TestCaseId != null && Guid.Parse(d.TestCaseId) == query.TestCaseId.Value)
                && (query.AlgorithmId == null || d.AlgorithmId == query.AlgorithmId) && (!query.Status.HasValue || d.Status == query.Status.Value.ToString()))
                .OrderByDescending(d => ParseTime(d.StartedAtUtc)).ThenBy(d => Guid.Parse(d.Id)).Skip(query.Offset).Take(query.Limit).Select(FromDto).ToList().AsReadOnly();
        }
        public int MarkInterruptedRuns(DateTime utcNow)
        {
            if (utcNow.Kind != DateTimeKind.Utc) throw new ArgumentException("UTC required.", nameof(utcNow));
            if (!store.Load().BenchmarkRuns.Any(r => r.Status == "Running")) return 0;
            int count = 0;
            store.Commit(d => {
                foreach (var dto in d.BenchmarkRuns.Where(r => r.Status == "Running").ToList()) {
                    var old = FromDto(dto); var time = utcNow < old.StartedAtUtc ? old.StartedAtUtc : utcNow;
                    var done = new PendingRunFinalization(old.RunId, BenchmarkStatus.Interrupted, time, old.Samples, errorCode: "INTERRUPTED", errorMessage: "上次程式結束前未完成保存。");
                    d.BenchmarkRuns[d.BenchmarkRuns.IndexOf(dto)] = ToDto(old.With(done)); count++;
                }
            }); return count;
        }
    }
}
