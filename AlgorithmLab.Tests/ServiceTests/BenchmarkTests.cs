using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using AlgorithmLab.Core.Algorithms;
using AlgorithmLab.Core.Contracts;
using AlgorithmLab.Core.Models;
using AlgorithmLab.Core.Services;
using AlgorithmLab.Data.FileStorage;
using AlgorithmLab.Data.Repositories;
namespace AlgorithmLab.Tests.ServiceTests
{
    internal static class BenchmarkTests
    {
        private static void Assert(bool ok) { if (!ok) throw new Exception("Assertion failed"); }
        private static void Error(Action action, ErrorKind kind) { try { action(); } catch (AlgorithmLabException e) { Assert(e.Kind == kind); return; } throw new Exception("Expected " + kind); }
        private sealed class InlineProgress : IProgress<BenchmarkProgress>
        { internal Action<BenchmarkProgress> Action; public void Report(BenchmarkProgress p) { Action(p); } }
        private sealed class CountingSort : ISortAlgorithm
        {
            public AlgorithmDescriptor Descriptor { get { return new BubbleSort().Descriptor; } }
            internal int Calls;
            internal readonly List<SortExecutionMode> Modes = new List<SortExecutionMode>();
            internal readonly List<int[]> Inputs = new List<int[]>();
            internal Action<int[], SortExecutionContext> Behavior;
            public void Sort(int[] data, SortExecutionContext context)
            { Calls++; Modes.Add(context.Mode); Inputs.Add((int[])data.Clone()); if (Behavior == null) new BubbleSort().Sort(data, context); else Behavior(data, context); }
        }
        private sealed class FaultWriter : IFileCommitter
        {
            internal bool Fail;
            public void Publish(string temp, string target, string backup) { if (Fail) throw new IOException("Injected write failure"); new AtomicFileCommitter().Publish(temp, target, backup); }
        }
        private sealed class Fixture : IDisposable
        {
            internal readonly JsonDataStore Store;
            internal readonly JsonTestCaseRepository Cases;
            internal readonly TestCaseService CaseService;
            internal readonly TestCase Case;
            internal readonly JsonBenchmarkRepository Runs;
            internal readonly CountingSort Algorithm = new CountingSort();
            internal readonly BenchmarkService Service;
            internal readonly List<Exception> Logs = new List<Exception>();
            internal Fixture(string path, FaultWriter writer = null)
            {
                Store = writer == null ? new JsonDataStore(path) : new JsonDataStore(path, writer); Store.Initialize();
                Cases = new JsonTestCaseRepository(Store); CaseService = new TestCaseService(Cases); Case = CaseService.Create(TestCaseDraft.Manual("研究案例", new[] { 3, 1, 2 }));
                Runs = new JsonBenchmarkRepository(Store);
                Service = new BenchmarkService(new AlgorithmCatalog(new Func<ISortAlgorithm>[] { () => Algorithm }), Runs, Cases, e => Logs.Add(e));
            }
            internal BenchmarkRequest Request(BenchmarkOptions options = null) { return new BenchmarkRequest(CaseService.CreateSnapshot(Case.Id, Case.Revision), "bubble", options ?? new BenchmarkOptions()); }
            internal BenchmarkRun Running() { return new BenchmarkRun(Guid.NewGuid(), CaseService.CreateSnapshot(Case.Id, Case.Revision), new BubbleSort().Descriptor, new BenchmarkOptions(), EnvironmentInfoCollector.Collect(), DateTime.UtcNow); }
            public void Dispose() { Store.Dispose(); }
        }
        private static string Cli(string path, string input)
        {
            var config = new DirectoryInfo(AppDomain.CurrentDomain.BaseDirectory).Name;
            var exe = Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, @"..\..\..\..\ConsoleApp1\bin\x64", config, "ConsoleApp1.exe"));
            using (var child = Process.Start(new ProcessStartInfo(exe, "--data-path \"" + path + "\"") { UseShellExecute = false, CreateNoWindow = true,
                RedirectStandardInput = true, RedirectStandardOutput = true, RedirectStandardError = true, StandardOutputEncoding = Encoding.UTF8, StandardErrorEncoding = Encoding.UTF8 })) {
                var bytes = new UTF8Encoding(false).GetBytes(input); child.StandardInput.BaseStream.Write(bytes, 0, bytes.Length); child.StandardInput.Close();
                var text = child.StandardOutput.ReadToEndAsync(); var errors = child.StandardError.ReadToEndAsync();
                if (!child.WaitForExit(15000)) { child.Kill(); child.WaitForExit(); throw new Exception("CLI timeout"); }
                Assert(child.ExitCode == 0 && errors.GetAwaiter().GetResult().Length == 0); return text.GetAwaiter().GetResult();
            }
        }
        internal static int Execute()
        {
            int passed = 0, failed = 0;
            Action<string, Action<string>> test = (name, action) => {
                var dir = Path.Combine(Path.GetTempPath(), "AlgorithmLab-Benchmark-" + Guid.NewGuid().ToString("N")); Directory.CreateDirectory(dir);
                try { action(Path.Combine(dir, "store.json")); passed++; Console.WriteLine("PASS " + name); }
                catch (Exception e) { failed++; Console.WriteLine("FAIL " + name + ": " + e); }
                finally { Directory.Delete(dir, true); }
            };
            test("statistics odd/even/zero/frequency and invalid samples", p => {
                var data = Enumerable.Range(1, 5).Select(i => new BenchmarkSample(i, i, true)); var stats = BenchmarkStatisticsCalculator.Calculate(data, 1000);
                Assert(stats.MedianMs == 3 && stats.MeanMs == 3 && stats.MinMs == 1 && stats.MaxMs == 5 && Math.Abs(stats.PopulationStdDevMs - Math.Sqrt(2)) < 1e-10);
                Assert(BenchmarkStatisticsCalculator.Calculate(data.Take(4), 1000).MedianMs == 2.5);
                Assert(BenchmarkStatisticsCalculator.Calculate(new[] { new BenchmarkSample(1, 0, true) }, 10000000).MedianMs == 0);
                Error(() => BenchmarkStatisticsCalculator.Calculate(new BenchmarkSample[0], 1000), ErrorKind.Validation);
                Error(() => BenchmarkStatisticsCalculator.Calculate(new[] { new BenchmarkSample(1, 1, false) }, 1000), ErrorKind.Validation);
                Error(() => BenchmarkStatisticsCalculator.Calculate(data, 0), ErrorKind.Validation);
            });
            test("environment fingerprint stable and architecture recorded", p => {
                var a = EnvironmentInfoCollector.Collect(); var b = EnvironmentInfoCollector.Collect();
                Assert(a.EnvironmentKey == b.EnvironmentKey && a.EnvironmentKey.Length == 64 && a.ProcessArchitecture == "x64" && a.StopwatchFrequency == Stopwatch.Frequency && a.ProcessorCount > 0);
            });
            test("success uses original clones and separate Analysis counts", p => {
                using (var f = new Fixture(p)) {
                    bool runningObserved = false;
                    f.Algorithm.Behavior = (data, context) => { runningObserved = f.Runs.GetHistory(new BenchmarkHistoryQuery()).Single().Status == BenchmarkStatus.Running; new BubbleSort().Sort(data, context); };
                    var result = f.Service.Run(f.Request());
                    Assert(runningObserved && result.Status == BenchmarkStatus.Completed && result.Samples.Count == 5 && result.Samples.All(s => s.IsValid));
                    Assert(f.Algorithm.Calls == 7 && f.Algorithm.Modes.Take(6).All(m => m == SortExecutionMode.Timing) && f.Algorithm.Modes.Last() == SortExecutionMode.Analysis);
                    Assert(f.Algorithm.Inputs.All(a => a.SequenceEqual(new[] { 3, 1, 2 })) && f.CaseService.GetDetails(f.Case.Id).Data.SequenceEqual(new[] { 3, 1, 2 }));
                    Assert(result.Metrics.ComparisonCount == 3 && result.Metrics.SwapCount == 2 && result.Metrics.WriteCount == 4 && result.OutputPreviewText == "1,2,3");
                    var decoded = f.Runs.GetDetails(result.RunId); Assert(decoded.Samples.Count == 5 && decoded.Input.InputHash == result.Input.InputHash && decoded.Environment.EnvironmentKey == result.Environment.EnvironmentKey);
                }
                using (var s = new JsonDataStore(p)) { s.Initialize(); Assert(new JsonBenchmarkRepository(s).GetHistory(new BenchmarkHistoryQuery()).Single().Status == BenchmarkStatus.Completed); }
            });
            test("stale input and unknown algorithm do not create Running", p => {
                using (var f = new Fixture(p)) { var request = f.Request(); f.CaseService.Update(f.Case.Id, 1, f.Case.ToDraft("changed"));
                    Error(() => f.Service.Run(request), ErrorKind.Conflict);
                    Error(() => f.Service.Run(new BenchmarkRequest(request.Input, "quick", new BenchmarkOptions())), ErrorKind.NotFound);
                    Assert(f.Algorithm.Calls == 0 && f.Runs.GetHistory(new BenchmarkHistoryQuery()).Count == 0);
                }
            });
            test("initial save failure does not sort", p => {
                var io = new FaultWriter(); using (var f = new Fixture(p, io)) { io.Fail = true; Error(() => f.Service.Run(f.Request()), ErrorKind.Persistence); Assert(f.Algorithm.Calls == 0 && f.Runs.GetHistory(new BenchmarkHistoryQuery()).Count == 0); }
            });
            test("pre-cancelled token persists Cancelled with no samples", p => {
                using (var f = new Fixture(p)) using (var cts = new CancellationTokenSource()) { cts.Cancel(); var run = f.Service.Run(f.Request(), cts.Token);
                    Assert(run.Status == BenchmarkStatus.Cancelled && run.ErrorCode == "USER_CANCELLED" && run.Statistics == null && run.Samples.Count == 0 && f.Algorithm.Calls == 0); }
            });
            test("cancellation after two samples preserves partial measurements", p => {
                using (var f = new Fixture(p)) using (var cts = new CancellationTokenSource()) {
                    var run = f.Service.Run(f.Request(), cts.Token, new InlineProgress { Action = progress => { if (progress.Stage == "Measuring" && progress.CompletedSampleCount == 2) cts.Cancel(); } });
                    Assert(run.Status == BenchmarkStatus.Cancelled && run.Samples.Count == 2 && run.Statistics == null && run.Metrics == null && f.Algorithm.Calls == 3);
                }
            });
            test("cooperative timeout records TIMEOUT", p => {
                using (var f = new Fixture(p)) {
                    f.Algorithm.Behavior = (data, context) => { if (!context.CancellationToken.WaitHandle.WaitOne(15000)) throw new Exception("Deadline did not fire"); context.CancellationToken.ThrowIfCancellationRequested(); };
                    var run = f.Service.Run(f.Request(new BenchmarkOptions(1, 5, 10)));
                    Assert(run.Status == BenchmarkStatus.Cancelled && run.ErrorCode == "TIMEOUT" && run.Statistics == null && f.Algorithm.Calls == 1);
                }
            });
            test("warmup invalid output is Failed without formal samples", p => {
                using (var f = new Fixture(p)) { f.Algorithm.Behavior = (data, context) => { data[0] = 9; Array.Sort(data); };
                    var run = f.Service.Run(f.Request()); Assert(run.Status == BenchmarkStatus.Failed && run.ErrorCode == "SORT_INVALID" && run.Samples.Count == 0 && run.Statistics == null && f.Logs.Count > 0); }
            });
            test("invalid measured sample retained as invalid, no aggregate", p => {
                using (var f = new Fixture(p)) { f.Algorithm.Behavior = (data, context) => { if (f.Algorithm.Calls == 3) data[0] = 9; new BubbleSort().Sort(data, context); };
                    var run = f.Service.Run(f.Request()); Assert(run.Status == BenchmarkStatus.Failed && run.ErrorCode == "SORT_INVALID" && run.Samples.Count == 2 && !run.Samples[1].IsValid && run.Statistics == null); }
            });
            test("Analysis invalid output does not publish completed times", p => {
                using (var f = new Fixture(p)) { f.Algorithm.Behavior = (data, context) => { if (context.Mode == SortExecutionMode.Analysis) data[0] = 9; new BubbleSort().Sort(data, context); };
                    var run = f.Service.Run(f.Request()); Assert(run.Status == BenchmarkStatus.Failed && run.Samples.Count == 5 && run.Metrics == null && run.Statistics == null); }
            });
            test("unexpected exception is saved and diagnosed", p => {
                using (var f = new Fixture(p)) { f.Algorithm.Behavior = (data, context) => { throw new InvalidOperationException("test failure"); };
                    var run = f.Service.Run(f.Request()); Assert(run.Status == BenchmarkStatus.Failed && run.ErrorCode == "UNEXPECTED" && f.Logs.Any(e => e is InvalidOperationException)); }
            });
            test("saving boundary cancellation removes aggregate score", p => {
                using (var f = new Fixture(p)) using (var cts = new CancellationTokenSource()) {
                    var run = f.Service.Run(f.Request(), cts.Token, new InlineProgress { Action = progress => { if (progress.Stage == "Saving") cts.Cancel(); } });
                    Assert(run.Status == BenchmarkStatus.Cancelled && run.Samples.Count == 5 && run.Statistics == null);
                }
            });
            test("save failure retry uses same RunId and never sorts again", p => {
                var io = new FaultWriter(); using (var f = new Fixture(p, io)) {
                    PendingRunFinalization pending = null;
                    try { f.Service.Run(f.Request(), progress: new InlineProgress { Action = progress => { if (progress.Stage == "Saving") io.Fail = true; } }); }
                    catch (BenchmarkPersistenceException e) { pending = e.PendingRunFinalization; }
                    Assert(pending != null && f.Algorithm.Calls == 7 && f.Runs.GetDetails(pending.RunId).Status == BenchmarkStatus.Running);
                    io.Fail = false; var result = f.Service.RetrySave(pending); Assert(result.Status == BenchmarkStatus.Completed && f.Algorithm.Calls == 7 && result.Samples.Count == 5);
                    var bytes = File.ReadAllBytes(p); f.Service.RetrySave(pending); Assert(bytes.SequenceEqual(File.ReadAllBytes(p)));
                    var wrong = new PendingRunFinalization(pending.RunId, BenchmarkStatus.Failed, pending.FinishedAtUtc, pending.Samples, errorCode: "OTHER");
                    Error(() => f.Runs.FinalizeRun(wrong), ErrorKind.Conflict);
                }
            });
            test("interrupted startup preserves other final records", p => {
                Guid runningId;
                using (var f = new Fixture(p)) { f.Service.Run(f.Request()); var pending = f.Running(); runningId = pending.RunId; f.Runs.CreateRunning(pending); }
                using (var s = new JsonDataStore(p)) { s.Initialize(); var repo = new JsonBenchmarkRepository(s); Assert(repo.MarkInterruptedRuns(DateTime.UtcNow) == 1); Assert(repo.MarkInterruptedRuns(DateTime.UtcNow) == 0);
                    Assert(repo.GetDetails(runningId).Status == BenchmarkStatus.Interrupted && repo.GetHistory(new BenchmarkHistoryQuery(status: BenchmarkStatus.Completed)).Count == 1); }
            });
            test("finalization respects deleted case links and frozen snapshot", p => {
                using (var f = new Fixture(p)) {
                    var result = f.Service.Run(f.Request(), progress: new InlineProgress { Action = progress => { if (progress.Stage == "Saving") f.CaseService.Delete(f.Case.Id, 1); } });
                    Assert(result.Status == BenchmarkStatus.Completed && result.Input.TestCaseId == null && result.Input.CaseName == "研究案例" && result.Input.Data.SequenceEqual(new[] { 3, 1, 2 }));
                }
            });
            test("history filters and pagination are stable", p => {
                using (var f = new Fixture(p)) { for (int i = 0; i < 23; i++) f.Runs.CreateRunning(f.Running());
                    var a = f.Runs.GetHistory(new BenchmarkHistoryQuery(limit: 20)); var b = f.Runs.GetHistory(new BenchmarkHistoryQuery(offset: 20)); Assert(a.Count == 20 && b.Count == 3 && a.Concat(b).Select(r => r.RunId).Distinct().Count() == 23);
                    Assert(f.Runs.GetHistory(new BenchmarkHistoryQuery(f.Case.Id, "bubble", BenchmarkStatus.Running)).Count == 20);
                    Assert(f.Runs.GetHistory(new BenchmarkHistoryQuery(algorithmId: "insertion")).Count == 0);
                }
            });
            test("parallel/reentrant run is rejected and observers cannot corrupt success", p => {
                using (var f = new Fixture(p)) { var request = f.Request(); f.Algorithm.Behavior = (data, context) => { Error(() => f.Service.Run(request), ErrorKind.Conflict); new BubbleSort().Sort(data, context); };
                    var result = f.Service.Run(request, progress: new InlineProgress { Action = progress => { throw new InvalidOperationException("observer failure"); } }); Assert(result.Status == BenchmarkStatus.Completed && f.Logs.Count > 0);
                }
            });
            test("CLI runs three algorithms, reopens history and retains deleted cases", p => {
                Assert(Cli(p, "1\n4\n上台測試\n2\n3 1 2\n\nY\n0\n2\n1\n1\nY\n2\n1\n2\nY\n2\n1\n3\nY\n0\n").Contains("排序驗證：通過"));
                using (var store = new JsonDataStore(p)) { var d = store.Initialize(); Assert(d.BenchmarkRuns.Count == 3 && d.BenchmarkRuns.All(r => r.Status == "Completed" && r.Samples.Count == 5)); }
                Assert(Cli(p, "1\n6\n1\nY\n0\n3\n\n\n\n1\n0\n0\n").Contains("上台測試"));
                using (var store = new JsonDataStore(p)) { var d = store.Initialize(); Assert(d.TestCases.Count == 0 && d.BenchmarkRuns.All(r => r.TestCaseId == null)); }
            });
            test("CLI explicit demo cases and unconfirmed run do not create results", p => {
                Assert(Cli(p, "4\nY\n2\n1\n1\nN\n0\n").Contains("已新增展示案例"));
                using (var store = new JsonDataStore(p)) { var d = store.Initialize(); Assert(d.TestCases.Count == 3 && d.BenchmarkRuns.Count == 0 && d.TestCases.All(c => c.Seed == 42 && c.DataCount == 1000)); }
            });
            Console.WriteLine("Benchmark tests: " + passed + " passed; " + failed + " failed."); return failed;
        }
    }
}
