using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using AlgorithmLab.Core.Algorithms;
using AlgorithmLab.Core.Contracts;
using AlgorithmLab.Core.Models;
using AlgorithmLab.Core.Services;
namespace AlgorithmLab.Tests.AlgorithmTests
{
    internal static class SortTests
    {
        private static void Assert(bool value) { if (!value) throw new Exception("Assertion failed"); }
        private static void Throws<T>(Action action) where T : Exception
        { try { action(); } catch (T) { return; } throw new Exception("Expected " + typeof(T).Name); }
        private static void Error(Action action, ErrorKind kind)
        { try { action(); } catch (AlgorithmLabException e) { Assert(e.Kind == kind); return; } throw new Exception("Expected " + kind); }
        private static ISortAlgorithm[] Algorithms() { return new ISortAlgorithm[] { new BubbleSort(), new SelectionSort(), new InsertionSort() }; }
        private static SortExecutionContext Context(SortExecutionMode mode = SortExecutionMode.Analysis) { return new SortExecutionContext(mode); }
        private static void Metrics(SortExecutionContext context, long comparisons, long swaps, long writes)
        { Assert(context.Metrics.ComparisonCount == comparisons && context.Metrics.SwapCount == swaps && context.Metrics.WriteCount == writes); }
        private sealed class HelperProbe : SortAlgorithmBase
        {
            public override AlgorithmDescriptor Descriptor { get { return new AlgorithmDescriptor("probe", "Probe", "1.0", "test only", false, false); } }
            protected override void SortCore(int[] data, SortExecutionContext context)
            {
                var auxiliary = new int[1]; Assert(CompareValues(int.MinValue, int.MaxValue, context) < 0);
                WriteValue(auxiliary, 0, 11, context); WriteValue(data, 0, auxiliary[0], context);
                SwapValues(data, 0, 1, context); SwapValues(data, 1, 1, context);
            }
        }
        internal static int Execute()
        {
            int passed = 0, failed = 0;
            Action<string, Action> test = (name, action) => {
                try { action(); passed++; Console.WriteLine("PASS " + name); }
                catch (Exception e) { failed++; Console.WriteLine("FAIL " + name + ": " + e); }
            };
            test("catalog descriptors, order and fresh strategies", () => {
                var catalog = new AlgorithmCatalog(new Func<ISortAlgorithm>[] { () => new BubbleSort(), () => new SelectionSort(), () => new InsertionSort() });
                Assert(catalog.GetAll().Select(d => d.Id).SequenceEqual(new[] { "bubble", "selection", "insertion" }));
                Assert(catalog.GetAll().All(d => d.Version == "1.0" && d.IsMidtermAlgorithm));
                Assert(catalog.GetAll()[0].IsStable && !catalog.GetAll()[1].IsStable && catalog.GetAll()[2].IsStable);
                Assert(!ReferenceEquals(catalog.GetById("bubble"), catalog.GetById("bubble")));
                foreach (var d in catalog.GetAll()) { var data = new[] { 2, 1 }; catalog.GetById(d.Id).Sort(data, Context()); Assert(data.SequenceEqual(new[] { 1, 2 })); }
            });
            test("catalog rejects missing/duplicate/mutating registrations", () => {
                Throws<ArgumentNullException>(() => new AlgorithmCatalog(null));
                Error(() => new AlgorithmCatalog(new Func<ISortAlgorithm>[] { () => null }), ErrorKind.Validation);
                Error(() => new AlgorithmCatalog(new Func<ISortAlgorithm>[] { null }), ErrorKind.Validation);
                Error(() => new AlgorithmCatalog(new Func<ISortAlgorithm>[] { () => new BubbleSort(), () => new BubbleSort() }), ErrorKind.Validation);
                var catalog = new AlgorithmCatalog(new Func<ISortAlgorithm>[] { () => new BubbleSort() });
                Error(() => catalog.GetById("BUBBLE"), ErrorKind.NotFound); Error(() => catalog.GetById("quick"), ErrorKind.NotFound); Error(() => catalog.GetById(null), ErrorKind.Validation);
                int calls = 0; var changed = new AlgorithmCatalog(new Func<ISortAlgorithm>[] { () => ++calls == 1 ? (ISortAlgorithm)new BubbleSort() : new SelectionSort() });
                Error(() => changed.GetById("bubble"), ErrorKind.Validation);
            });
            test("null guards and invalid execution modes", () => {
                foreach (var algorithm in Algorithms()) {
                    Throws<ArgumentNullException>(() => algorithm.Sort(null, Context()));
                    Throws<ArgumentNullException>(() => algorithm.Sort(new int[0], null));
                }
                Throws<ArgumentOutOfRangeException>(() => new SortExecutionContext((SortExecutionMode)99));
            });
            test("empty and single element do not count operations", () => {
                foreach (var algorithm in Algorithms()) foreach (var data in new[] { new int[0], new[] { int.MinValue } }) {
                    var context = Context(); var original = (int[])data.Clone(); algorithm.Sort(data, context); Assert(data.SequenceEqual(original)); Metrics(context, 0, 0, 0);
                }
            });
            test("sorted/reversed/equal/duplicate/negative/extreme inputs", () => {
                var inputs = new[] { new[] { 1, 2, 3, 4 }, new[] { 4, 3, 2, 1 }, new[] { 7, 7, 7, 7 }, new[] { 2, 1, 2, 0, 1 },
                    new[] { -1, -5, 0, -5, 2 }, new[] { int.MaxValue, int.MinValue, 0, int.MaxValue, int.MinValue } };
                foreach (var algorithm in Algorithms()) foreach (var original in inputs) {
                    var data = (int[])original.Clone(); var expected = (int[])original.Clone(); Array.Sort(expected);
                    algorithm.Sort(data, Context()); Assert(data.SequenceEqual(expected)); SortResultValidator.Validate(expected, data);
                }
            });
            test("metrics-1 fixed examples", () => {
                var algorithms = Algorithms();
                long[][] unsorted = { new long[] { 3, 2, 4 }, new long[] { 3, 2, 4 }, new long[] { 3, 0, 4 } };
                long[][] sorted = { new long[] { 2, 0, 0 }, new long[] { 3, 0, 0 }, new long[] { 2, 0, 2 } };
                for (int i = 0; i < algorithms.Length; i++) {
                    var a = Context(); algorithms[i].Sort(new[] { 3, 1, 2 }, a); Metrics(a, unsorted[i][0], unsorted[i][1], unsorted[i][2]);
                    var b = Context(); algorithms[i].Sort(new[] { 1, 2, 3 }, b); Metrics(b, sorted[i][0], sorted[i][1], sorted[i][2]);
                }
            });
            test("best/reversed/duplicate behavior and selection formula", () => {
                const int n = 100; var increasing = Enumerable.Range(0, n).ToArray(); var decreasing = increasing.Reverse().ToArray();
                var a = Context(); new BubbleSort().Sort((int[])increasing.Clone(), a); Metrics(a, n - 1, 0, 0);
                var b = Context(); new BubbleSort().Sort(decreasing, b); Metrics(b, n * (n - 1) / 2, n * (n - 1) / 2, n * (n - 1));
                foreach (var input in new[] { increasing, increasing.Reverse().ToArray(), Enumerable.Repeat(2, n).ToArray() }) {
                    var c = Context(); new SelectionSort().Sort((int[])input.Clone(), c); Assert(c.Metrics.ComparisonCount == (long)n * (n - 1) / 2);
                }
                var d = Context(); new InsertionSort().Sort(Enumerable.Repeat(2, n).ToArray(), d); Metrics(d, n - 1, 0, n - 1);
            });
            test("seeded oracle and Timing/Analysis equivalence", () => {
                foreach (var algorithm in Algorithms()) foreach (int seed in new[] { 0, 1, 42, 123, 2026, int.MinValue }) foreach (int length in new[] { 2, 3, 10, 100, 257 }) {
                    var random = new Random(seed); var source = Enumerable.Range(0, length).Select(i => random.Next(-20, 21)).ToArray();
                    var expected = (int[])source.Clone(); Array.Sort(expected); var timed = (int[])source.Clone(); var analyzed = (int[])source.Clone();
                    var timing = Context(SortExecutionMode.Timing); var analysis = Context(); algorithm.Sort(timed, timing); algorithm.Sort(analyzed, analysis);
                    Assert(expected.SequenceEqual(timed) && expected.SequenceEqual(analyzed)); Metrics(timing, 0, 0, 0); Assert(analysis.Metrics.ComparisonCount > 0);
                }
            });
            test("maximum midterm data size", () => {
                var random = new Random(42); var input = Enumerable.Range(0, 5000).Select(i => random.Next(-10000, 10001)).ToArray(); var expected = (int[])input.Clone(); Array.Sort(expected);
                foreach (var algorithm in Algorithms()) { var work = (int[])input.Clone(); algorithm.Sort(work, Context()); Assert(work.SequenceEqual(expected)); }
            });
            test("complete answer validation rejects lost/extra/replaced elements", () => {
                var expected = new[] { 1, 2, 2, 3 };
                foreach (var wrong in new[] { new[] { 1, 2, 3 }, new[] { 1, 1, 2, 3 }, new[] { 1, 2, 2, 4 }, new[] { 1, 2, 3, 2 }, new[] { 1, 2, 2, 3, 3 } })
                    Error(() => SortResultValidator.Validate(expected, wrong), ErrorKind.SortValidationFailure);
                Throws<ArgumentNullException>(() => SortResultValidator.CreateExpected(null));
                Throws<ArgumentNullException>(() => SortResultValidator.Validate(null, new int[0]));
                Throws<ArgumentNullException>(() => SortResultValidator.Validate(new int[0], null));
                Throws<ArgumentException>(() => SortResultValidator.Validate(new[] { 2, 1 }, new[] { 2, 1 }));
                SortResultValidator.Validate(new int[0], new int[0]);
            });
            test("snapshot and oracle do not mutate persisted input", () => {
                var draft = TestCaseDraft.Manual("snapshot", new[] { 3, 1, 2 }); var now = DateTime.UtcNow;
                var snapshot = new TestCaseSnapshot(new TestCase(Guid.NewGuid(), 1, draft, now, now)); var hash = snapshot.InputHash;
                var source = snapshot.Data; var expected = SortResultValidator.CreateExpected(source); Assert(source.SequenceEqual(new[] { 3, 1, 2 }));
                foreach (var algorithm in Algorithms()) { var work = snapshot.Data; algorithm.Sort(work, Context()); SortResultValidator.Validate(expected, work); }
                Assert(snapshot.Data.SequenceEqual(new[] { 3, 1, 2 }) && snapshot.InputHash == hash);
            });
            test("helpers count auxiliary writes and ignore self-swap", () => {
                var context = Context(); var data = new[] { 2, 1 }; new HelperProbe().Sort(data, context); Metrics(context, 1, 1, 4); Assert(data.SequenceEqual(new[] { 1, 11 }));
                var timing = Context(SortExecutionMode.Timing); new HelperProbe().Sort(new[] { 2, 1 }, timing); Metrics(timing, 0, 0, 0);
            });
            test("pre-cancellation leaves data and metrics unchanged", () => {
                using (var cts = new CancellationTokenSource()) {
                    cts.Cancel();
                    foreach (var algorithm in Algorithms()) foreach (var mode in new[] { SortExecutionMode.Timing, SortExecutionMode.Analysis }) {
                        var data = new[] { 3, 1, 2 }; var context = new SortExecutionContext(mode, cts.Token);
                        Throws<OperationCanceledException>(() => algorithm.Sort(data, context)); Assert(data.SequenceEqual(new[] { 3, 1, 2 })); Metrics(context, 0, 0, 0);
                    }
                }
            });
            test("cancel each real algorithm after observable progress", () => {
                foreach (var algorithm in Algorithms()) foreach (var mode in new[] { SortExecutionMode.Timing, SortExecutionMode.Analysis }) {
                    using (var cts = new CancellationTokenSource()) {
                        var data = Enumerable.Range(1, 50000).Reverse().ToArray(); Exception failure = null;
                        var finished = new ManualResetEventSlim();
                        var worker = new Thread(() => { try { algorithm.Sort(data, new SortExecutionContext(mode, cts.Token)); } catch (Exception e) { failure = e; } finally { finished.Set(); } }) { IsBackground = true };
                        worker.Start();
                        try {
                            Assert(SpinWait.SpinUntil(() => Volatile.Read(ref data[0]) != 50000 || finished.IsSet, 5000));
                            Assert(!finished.IsSet); cts.Cancel(); Assert(finished.Wait(5000)); worker.Join(); Assert(failure is OperationCanceledException);
                        }
                        finally { cts.Cancel(); if (finished.IsSet) { worker.Join(); finished.Dispose(); } }
                    }
                }
            });
            test("fresh contexts isolate repeated calls and reject reuse", () => {
                foreach (var algorithm in Algorithms()) {
                    var first = Context(); algorithm.Sort(new[] { 3, 1, 2 }, first); var comparisons = first.Metrics.ComparisonCount;
                    var second = Context(); algorithm.Sort(new[] { 3, 1, 2 }, second); Assert(second.Metrics.ComparisonCount == comparisons && first.Metrics.ComparisonCount == comparisons);
                    var data = new[] { 3, 1, 2 }; Throws<InvalidOperationException>(() => algorithm.Sort(data, first)); Assert(data.SequenceEqual(new[] { 3, 1, 2 }));
                }
            });
            test("metrics preserve 64-bit values and reject negative counts", () => {
                var metrics = new SortMetrics(9007199254740993L, 2147483648L, 4294967296L);
                Assert(metrics.ComparisonCount == 9007199254740993L && metrics.SwapCount == 2147483648L && metrics.WriteCount == 4294967296L);
                Throws<ArgumentOutOfRangeException>(() => new SortMetrics(-1));
            });
            Console.WriteLine("Sort tests: " + passed + " passed; " + failed + " failed."); return failed;
        }
    }
}
