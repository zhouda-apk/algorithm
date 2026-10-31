using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using AlgorithmLab.Core.Models;
using AlgorithmLab.Data.FileStorage;

namespace AlgorithmLab.Tests.FileStorageTests
{
    internal static class StorageTests
    {
        private static void Assert(bool value) { if (!value) throw new Exception("Assertion failed"); }
        private static void Error(Action action, ErrorKind kind)
        {
            try { action(); } catch (AlgorithmLabException e) { Assert(e.Kind == kind); return; }
            throw new Exception("Expected " + kind);
        }
        private static TestCaseDto Case()
        {
            var now = DateTime.UtcNow.ToString("o", CultureInfo.InvariantCulture);
            return new TestCaseDto { Id = Guid.NewGuid().ToString("D"), Name = "中文\\引號\"測試", Revision = 1,
                Distribution = "Manual", DataCount = 3, Data = new[] { int.MinValue, 0, int.MaxValue },
                MinValue = int.MinValue, MaxValue = int.MaxValue, CreatedAtUtc = now, UpdatedAtUtc = now };
        }
        internal static BenchmarkRunDto Run(TestCaseDto c)
        {
            var now = DateTime.UtcNow.ToString("o", CultureInfo.InvariantCulture);
            const string input = "-2147483648,0,2147483647";
            return new BenchmarkRunDto { Id = Guid.NewGuid().ToString("D"), TestCaseId = c.Id, CaseRevision = 1, CaseName = c.Name,
                Distribution = "Manual", DataCount = 3, MinValue = int.MinValue, MaxValue = int.MaxValue,
                InputDataText = input, InputFormatVersion = "intcsv-1", InputHash = DocumentValidator.Hash(System.Text.Encoding.UTF8.GetBytes(input)),
                AlgorithmId = "bubble", AlgorithmVersion = "1.0", MetricsVersion = "metrics-1", WarmupCount = 1, RequestedSampleCount = 5, TimeoutSeconds = 30,
                Status = "Completed", StartedAtUtc = now, FinishedAtUtc = now, StopwatchFrequency = 1000, IsHighResolution = true,
                MedianMs = 3, MeanMs = 3, MinMs = 1, MaxMs = 5, PopulationStdDevMs = Math.Sqrt(2), ComparisonCount = 2, SwapCount = 0, WriteCount = 0,
                OutputPreviewText = input, AppVersion = "1.0", BuildConfiguration = "Release", ProcessArchitecture = "x64", ClrVersion = "4", OsVersion = "Windows",
                MachineName = "test", CpuModel = "Unknown", EnvironmentKey = new string('a', 64), ProcessorCount = 1,
                Samples = Enumerable.Range(1, 5).Select(i => new BenchmarkSampleDto { SampleIndex = i, ElapsedTimerTicks = i, IsValid = true }).ToList() };
        }
        private sealed class FaultCommitter : IFileCommitter
        {
            internal bool Enabled;
            internal bool After;
            public void Publish(string temp, string destination, string backup)
            {
                if (Enabled && !After) throw new IOException("Test publication failure");
                new AtomicFileCommitter().Publish(temp, destination, backup);
                if (Enabled && After) throw new IOException("Test error after publication");
            }
        }
        internal static int Execute()
        {
            int failed = 0, passed = 0;
            Action<string, Action<string>> test = (name, action) => {
                var directory = Path.Combine(Path.GetTempPath(), "AlgorithmLab-Tests-" + Guid.NewGuid().ToString("N")); Directory.CreateDirectory(directory);
                try { action(Path.Combine(directory, "store.json")); passed++; Console.WriteLine("PASS " + name); }
                catch (Exception e) { failed++; Console.WriteLine("FAIL " + name + ": " + e); }
                finally { Directory.Delete(directory, true); }
            };
            test("initialize/reopen/version", p => {
                using (var s = new JsonDataStore(p)) { Assert(s.Initialize().DocumentRevision == 1); }
                using (var s = new JsonDataStore(p)) { Assert(s.Initialize().DocumentRevision == 1); Assert(s.Load().FileFormatVersion == 1); }
            });
            test("case+history+64bit+null roundtrip and detached reads", p => {
                using (var s = new JsonDataStore(p)) {
                    s.Initialize(); var c = Case(); var r = Run(c); r.ComparisonCount = 9007199254740993L;
                    var saved = s.Commit(d => { d.TestCases.Add(c); d.BenchmarkRuns.Add(r); }); saved.TestCases[0].Data[0] = 12; c.Data[0] = 99;
                    Assert(s.Load().TestCases[0].Data[0] == int.MinValue);
                }
                using (var s = new JsonDataStore(p)) { var d = s.Initialize(); Assert(d.TestCases[0].Seed == null && d.BenchmarkRuns[0].ComparisonCount == 9007199254740993L); Assert(d.BenchmarkRuns[0].Samples.Count == 5); }
            });
            test("backup is previous revision", p => {
                using (var s = new JsonDataStore(p)) { s.Initialize(); s.Commit(d => d.TestCases.Add(Case())); Assert(s.InspectBackup().DocumentRevision == 1); Assert(s.Load().DocumentRevision == 2); }
            });
            test("publication failure preserves disk and cache", p => {
                var io = new FaultCommitter(); using (var s = new JsonDataStore(p, io)) { s.Initialize(); var before = File.ReadAllBytes(p); io.Enabled = true;
                    Error(() => s.Commit(d => d.TestCases.Add(Case())), ErrorKind.Persistence); Assert(before.SequenceEqual(File.ReadAllBytes(p))); Assert(s.Load().TestCases.Count == 0);
                    io.Enabled = false; Assert(s.Commit(d => d.TestCases.Add(Case())).DocumentRevision == 2);
                }
                Assert(Directory.GetFiles(Path.GetDirectoryName(p), "*.tmp").Length == 0);
            });
            test("post-publication error reconciles intended bytes", p => {
                var io = new FaultCommitter(); using (var s = new JsonDataStore(p, io)) { s.Initialize(); io.Enabled = true; io.After = true; Assert(s.Commit(d => d.TestCases.Add(Case())).DocumentRevision == 2); Assert(s.Load().TestCases.Count == 1); }
            });
            test("callback/validation failure leaves prior state", p => {
                using (var s = new JsonDataStore(p)) { s.Initialize();
                    try { s.Commit(d => { d.TestCases.Add(Case()); throw new InvalidOperationException(); }); } catch (InvalidOperationException) { }
                    Error(() => s.Commit(d => d.TestCases.Add(null)), ErrorKind.DataIntegrity); Assert(s.Load().DocumentRevision == 1);
                }
            });
            test("duplicate IDs, bad hash, orphan references rejected", p => {
                using (var s = new JsonDataStore(p)) { s.Initialize(); var c = Case();
                    Error(() => s.Commit(d => { d.TestCases.Add(c); d.TestCases.Add(c); }), ErrorKind.DataIntegrity);
                    Error(() => s.Commit(d => d.BenchmarkRuns.Add(Run(c))), ErrorKind.DataIntegrity);
                    Error(() => s.Commit(d => { d.TestCases.Add(c); var r = Run(c); r.InputHash = "bad"; d.BenchmarkRuns.Add(r); }), ErrorKind.DataIntegrity);
                    Assert(s.Load().DocumentRevision == 1);
                }
            });
            test("invalid samples/status/partial aggregates rejected", p => {
                using (var s = new JsonDataStore(p)) { s.Initialize(); var c = Case();
                    Error(() => s.Commit(d => { d.TestCases.Add(c); var r = Run(c); r.Samples[1].SampleIndex = 1; d.BenchmarkRuns.Add(r); }), ErrorKind.DataIntegrity);
                    Error(() => s.Commit(d => { d.TestCases.Add(c); var r = Run(c); r.Status = "Failed"; d.BenchmarkRuns.Add(r); }), ErrorKind.DataIntegrity);
                    Assert(s.Load().TestCases.Count == 0);
                }
            });
            test("corrupt main preserved and explicit backup recovery", p => {
                using (var s = new JsonDataStore(p)) { s.Initialize(); s.Commit(d => d.TestCases.Add(Case())); s.Commit(d => d.TestCases.Clear()); }
                File.WriteAllText(p, "corrupt");
                using (var s = new JsonDataStore(p)) { Error(() => s.Initialize(), ErrorKind.DataIntegrity); Assert(File.ReadAllText(p) == "corrupt"); Assert(s.InspectBackup().TestCases.Count == 1); Assert(s.RestoreBackup().TestCases.Count == 1); }
                Assert(Directory.GetFiles(Path.GetDirectoryName(p), "*.corrupt.*").Length == 1);
            });
            test("missing main with backup must not reset", p => {
                using (var s = new JsonDataStore(p)) { s.Initialize(); s.Commit(d => d.TestCases.Add(Case())); } File.Delete(p);
                using (var s = new JsonDataStore(p)) { Error(() => s.Initialize(), ErrorKind.DataIntegrity); Assert(!File.Exists(p)); s.RestoreBackup(); }
            });
            test("unknown version/missing required field preserved", p => {
                using (var s = new JsonDataStore(p)) { s.Initialize(); }
                var valid = File.ReadAllText(p); File.WriteAllText(p, valid.Replace("\"fileFormatVersion\":1", "\"fileFormatVersion\":2"));
                using (var s = new JsonDataStore(p)) { Error(() => s.Initialize(), ErrorKind.DataIntegrity); }
                File.WriteAllText(p, "{}"); using (var s = new JsonDataStore(p)) { Error(() => s.Initialize(), ErrorKind.DataIntegrity); Assert(File.ReadAllText(p) == "{}"); }
            });
            test("oversize read/write preserve prior file", p => {
                using (var s = new JsonDataStore(p)) { s.Initialize(); var before = File.ReadAllBytes(p);
                    Error(() => s.Commit(d => { var c = Case(); var r = Run(c); r.CpuModel = new string('x', JsonDataStore.MaximumFileBytes); d.TestCases.Add(c); d.BenchmarkRuns.Add(r); }), ErrorKind.Validation);
                    Assert(before.SequenceEqual(File.ReadAllBytes(p)));
                }
                using (var f = File.OpenWrite(p)) f.SetLength(JsonDataStore.MaximumFileBytes + 1L);
                using (var s = new JsonDataStore(p)) { Error(() => s.Initialize(), ErrorKind.DataIntegrity); }
            });
            test("outside file change requires reload", p => {
                using (var s = new JsonDataStore(p)) { s.Initialize(); File.AppendAllText(p, " "); Error(() => s.Commit(d => { }), ErrorKind.Conflict); s.Load(); Assert(s.Commit(d => { }).DocumentRevision == 2); }
            });
            test("abandoned temp requires recovery", p => {
                File.WriteAllText(p + ".interrupted.tmp", "{}"); using (var s = new JsonDataStore(p)) { Error(() => s.Initialize(), ErrorKind.DataIntegrity); Assert(!File.Exists(p)); }
            });
            test("same-process and cross-process single instance", p => {
                using (var s = new JsonDataStore(p)) {
                    s.Initialize(); Error(() => { using (var second = new JsonDataStore(p)) { } }, ErrorKind.Conflict);
                    using (var child = Process.Start(new ProcessStartInfo { FileName = typeof(StorageTests).Assembly.Location, Arguments = "--try-store \"" + p + "\"", UseShellExecute = false, CreateNoWindow = true })) {
                        Assert(child.WaitForExit(10000)); Assert(child.ExitCode == 3);
                    }
                }
                using (var s = new JsonDataStore(p)) Assert(s.Initialize().DocumentRevision == 1);
            });
            test("disposed store rejects use", p => { var s = new JsonDataStore(p); s.Dispose(); try { s.Load(); } catch (ObjectDisposedException) { return; } throw new Exception("Expected ObjectDisposedException"); });
            test("trailing content and non-UTF8 rejected", p => {
                using (var s = new JsonDataStore(p)) { s.Initialize(); }
                var text = File.ReadAllText(p); File.WriteAllText(p, text + "{}");
                using (var s = new JsonDataStore(p)) { Error(() => s.Initialize(), ErrorKind.DataIntegrity); }
                File.WriteAllText(p, text, System.Text.Encoding.Unicode);
                using (var s = new JsonDataStore(p)) { Error(() => s.Initialize(), ErrorKind.DataIntegrity); }
            });
            test("reentrant callback rejected without a partial commit", p => {
                using (var s = new JsonDataStore(p)) { s.Initialize(); Error(() => s.Commit(d => s.Commit(inner => { })), ErrorKind.Conflict); Assert(s.Load().DocumentRevision == 1); }
            });
            test("object graph limit is a recoverable write error", p => {
                using (var s = new JsonDataStore(p)) { s.Initialize();
                    Error(() => s.Commit(d => {
                        for (int i = 0; i < 2100; i++) { var c = Case(); c.Data = new int[1000]; c.DataCount = 1000; c.MinValue = c.MaxValue = 0; d.TestCases.Add(c); }
                    }), ErrorKind.Validation);
                    Assert(s.Load().DocumentRevision == 1);
                }
            });
            test("real Windows sharing violation preserves old file", p => {
                using (var s = new JsonDataStore(p)) { s.Initialize(); var old = File.ReadAllBytes(p);
                    using (var held = new FileStream(p, FileMode.Open, FileAccess.Read, FileShare.Read))
                        Error(() => s.Commit(d => d.TestCases.Add(Case())), ErrorKind.Persistence);
                    Assert(old.SequenceEqual(File.ReadAllBytes(p))); Assert(s.Load().TestCases.Count == 0);
                }
            });
            Console.WriteLine("Storage tests: " + passed + " passed; " + failed + " failed."); return failed;
        }
    }
}
