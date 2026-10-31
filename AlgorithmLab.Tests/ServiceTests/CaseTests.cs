using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using AlgorithmLab.Core.Models;
using AlgorithmLab.Core.Services;
using AlgorithmLab.Data.FileStorage;
using AlgorithmLab.Data.Repositories;
using AlgorithmLab.Tests.FileStorageTests;
namespace AlgorithmLab.Tests.ServiceTests
{
    internal static class CaseTests
    {
        private static void Assert(bool condition) { if (!condition) throw new Exception("Assertion failed"); }
        private static void Error(Action action, ErrorKind kind)
        {
            try { action(); } catch (AlgorithmLabException e) { Assert(e.Kind == kind); return; }
            throw new Exception("Expected " + kind);
        }
        private static TestCaseService Service(JsonDataStore s) { return new TestCaseService(new JsonTestCaseRepository(s)); }
        private static TestCaseDraft Draft(string name = "Alpha") { return TestCaseDraft.Manual(name, new[] { int.MinValue, 0, int.MaxValue }); }
        private sealed class FailPublish : IFileCommitter
        {
            internal bool Fail;
            public void Publish(string temp, string target, string backup)
            { if (Fail) throw new IOException("Injected persistence failure"); new AtomicFileCommitter().Publish(temp, target, backup); }
        }
        private static string Cli(string path, string input)
        {
            var config = new DirectoryInfo(AppDomain.CurrentDomain.BaseDirectory).Name;
            var exe = Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, @"..\..\..\..\ConsoleApp1\bin\x64", config, "ConsoleApp1.exe"));
            using (var child = Process.Start(new ProcessStartInfo(exe, "--data-path \"" + path + "\"") {
                UseShellExecute = false, CreateNoWindow = true, RedirectStandardInput = true, RedirectStandardOutput = true, RedirectStandardError = true,
                StandardOutputEncoding = Encoding.UTF8, StandardErrorEncoding = Encoding.UTF8 }))
            {
                // Write UTF-8 bytes directly: PowerShell/OEM defaults must not corrupt Chinese input.
                var bytes = new UTF8Encoding(false).GetBytes(input); child.StandardInput.BaseStream.Write(bytes, 0, bytes.Length); child.StandardInput.Close();
                var text = child.StandardOutput.ReadToEndAsync(); var errors = child.StandardError.ReadToEndAsync();
                if (!child.WaitForExit(15000)) { child.Kill(); child.WaitForExit(); throw new Exception("CLI timeout"); }
                Assert(child.ExitCode == 0); Assert(errors.GetAwaiter().GetResult().Length == 0); return text.GetAwaiter().GetResult();
            }
        }
        internal static int Execute()
        {
            int passed = 0, failed = 0;
            Action<string, Action<string>> test = (name, action) => {
                var dir = Path.Combine(Path.GetTempPath(), "AlgorithmLab-Cases-" + Guid.NewGuid().ToString("N")); Directory.CreateDirectory(dir);
                try { action(Path.Combine(dir, "store.json")); passed++; Console.WriteLine("PASS " + name); }
                catch (Exception e) { failed++; Console.WriteLine("FAIL " + name + ": " + e); }
                finally { Directory.Delete(dir, true); }
            };
            test("manual parser mixed delimiters/extremes and missing values", p => {
                Assert(ManualDataParser.Parse(" -2147483648, +2  2\n2147483647 ").SequenceEqual(new[] { int.MinValue, 2, 2, int.MaxValue }));
                foreach (var text in new[] { "", " ", "1,,2", "1,", ",1", "1, \n,2", "1.5", "2147483648", "hello", "1 2x" }) Error(() => ManualDataParser.Parse(text), ErrorKind.Validation);
                Error(() => ManualDataParser.Parse(string.Join(" ", Enumerable.Repeat("1", 5001))), ErrorKind.Validation);
            });
            test("generation repeatability and identical multiset", p => {
                var g = new DataGenerator(); var a = g.Generate(DataDistribution.Random, 1000, -50, 100, 42);
                Assert(a.Data.SequenceEqual(g.Generate(DataDistribution.Random, 1000, -50, 100, 42).Data));
                Assert(a.Data.OrderBy(x => x).SequenceEqual(g.Generate(DataDistribution.Sorted, 1000, -50, 100, 42).Data));
                Assert(a.Data.OrderByDescending(x => x).SequenceEqual(g.Generate(DataDistribution.Reversed, 1000, -50, 100, 42).Data));
                var chosen = g.Generate(DataDistribution.Random, 100, -10, 10);
                Assert(chosen.Data.SequenceEqual(g.Generate(chosen.Distribution, chosen.DataCount, chosen.MinValue, chosen.MaxValue, chosen.Seed).Data));
                Assert(g.Generate(DataDistribution.Random, 1, 0, 1, int.MinValue).DataCount == 1);
            });
            test("generation boundaries and invalid parameters", p => {
                var g = new DataGenerator();
                foreach (var d in new[] { DataDistribution.Random, DataDistribution.Sorted, DataDistribution.Reversed, DataDistribution.FewUnique }) {
                    Assert(g.Generate(d, 5000, 1000000, 1000000, 1).Data.All(v => v == 1000000));
                    var data = g.Generate(d, 5000, -1000000, -999995, 1); Assert(data.Data.All(v => v >= -1000000 && v <= -999995));
                }
                Assert(g.Generate(DataDistribution.FewUnique, 5000, 100, 1000, 2).Data.All(v => v >= 100 && v <= 109));
                Error(() => g.Generate(DataDistribution.Manual, 2, 0, 2), ErrorKind.Validation);
                Error(() => g.Generate((DataDistribution)99, 2, 0, 2), ErrorKind.Validation);
                Error(() => g.Generate(DataDistribution.Random, 0, 0, 2), ErrorKind.Validation);
                Error(() => g.Generate(DataDistribution.Random, 5001, 0, 2), ErrorKind.Validation);
                Error(() => g.Generate(DataDistribution.Random, 2, 3, 2), ErrorKind.Validation);
                Error(() => g.Generate(DataDistribution.Random, 2, int.MinValue, 2), ErrorKind.Validation);
            });
            test("domain and draft defensive copies", p => {
                var array = new[] { 3, 1, 2 }; var draft = TestCaseDraft.Manual(" Test ", array); array[0] = 99; draft.Data[0] = 88;
                using (var s = new JsonDataStore(p)) { s.Initialize(); var value = Service(s).Create(draft); Assert(value.Name == "Test" && value.Data[0] == 3); value.Data[0] = 77; Assert(value.Data[0] == 3); }
                var generated = new DataGenerator().Generate(DataDistribution.Sorted, 10, 0, 2, 42); var original = generated.Data; generated.Data[0] = 999; Assert(generated.Data.SequenceEqual(original));
            });
            test("validation fails before any write", p => {
                using (var s = new JsonDataStore(p)) { s.Initialize(); var service = Service(s); var original = File.ReadAllBytes(p);
                    foreach (var name in new[] { "", "  ", "a\nb", new string('x', 51) }) Error(() => service.Create(Draft(name)), ErrorKind.Validation);
                    Error(() => service.Create(null), ErrorKind.Validation);
                    Error(() => TestCaseDraft.Manual("bad", new int[0]), ErrorKind.Validation);
                    Error(() => TestCaseDraft.Manual("bad", new int[5001]), ErrorKind.Validation);
                    Error(() => service.Create(new TestCaseDraft("bad", DataDistribution.Sorted, new[] { 2, 1 }, 42, "gen-1", 1, 2)), ErrorKind.Validation);
                    Error(() => service.Create(new TestCaseDraft("bad", DataDistribution.Manual, new[] { 1 }, 42, null, 1, 1)), ErrorKind.Validation);
                    Assert(original.SequenceEqual(File.ReadAllBytes(p)));
                }
            });
            test("CRUD persists across reopen and names may repeat", p => {
                Guid id;
                using (var s = new JsonDataStore(p)) { s.Initialize(); var service = Service(s); var value = service.Create(Draft()); id = value.Id; service.Create(Draft());
                    Assert(service.Search(" aLP ", DataDistribution.Manual).Count == 2); Assert(service.Search(null, DataDistribution.Random).Count == 0);
                    var updated = service.Update(id, 1, value.ToDraft("Renamed")); Assert(updated.Revision == 2 && updated.Id == id && updated.CreatedAtUtc == value.CreatedAtUtc && updated.Data.SequenceEqual(value.Data));
                }
                using (var s = new JsonDataStore(p)) { s.Initialize(); var service = Service(s); var value = service.GetDetails(id); Assert(value.Name == "Renamed" && value.Revision == 2); service.Delete(id, 2); Error(() => service.GetDetails(id), ErrorKind.NotFound); Assert(service.Search().Count == 1); }
            });
            test("seed and generated values persisted intact", p => {
                Guid id; var data = new DataGenerator().Generate(DataDistribution.FewUnique, 1000, -20, 30);
                using (var s = new JsonDataStore(p)) { s.Initialize(); id = Service(s).Create(data.ToDraft("generated")).Id; }
                using (var s = new JsonDataStore(p)) { s.Initialize(); var c = Service(s).GetDetails(id); Assert(c.Seed == data.Seed && c.GeneratorVersion == "gen-1" && c.Data.SequenceEqual(data.Data)); }
            });
            test("stale edits/deletes/snapshots and unknown IDs", p => {
                using (var s = new JsonDataStore(p)) { s.Initialize(); var service = Service(s); var c = service.Create(Draft()); service.Update(c.Id, 1, c.ToDraft("new")); var bytes = File.ReadAllBytes(p);
                    Error(() => service.Update(c.Id, 1, c.ToDraft("stale")), ErrorKind.Conflict);
                    Error(() => service.Delete(c.Id, 1), ErrorKind.Conflict); Error(() => service.CreateSnapshot(c.Id, 1), ErrorKind.Conflict);
                    Error(() => service.Delete(Guid.NewGuid(), 1), ErrorKind.NotFound); Error(() => service.GetDetails(Guid.NewGuid()), ErrorKind.NotFound);
                    Assert(bytes.SequenceEqual(File.ReadAllBytes(p)));
                }
            });
            test("explicit replacement and immutable snapshot", p => {
                using (var s = new JsonDataStore(p)) { s.Initialize(); var service = Service(s); var c = service.Create(Draft()); var snapshot = service.CreateSnapshot(c.Id, 1);
                    var encoded = snapshot.InputDataText; var hash = snapshot.InputHash; snapshot.Data[0] = 9;
                    service.Update(c.Id, 1, TestCaseDraft.Manual("replaced", new[] { 5, 3 })); service.Delete(c.Id, 2);
                    Assert(snapshot.CaseRevision == 1 && snapshot.CaseName == "Alpha" && snapshot.Data[0] == int.MinValue);
                    Assert(IntArrayCodec.Decode(encoded, 3, hash).SequenceEqual(new[] { int.MinValue, 0, int.MaxValue }));
                    Error(() => IntArrayCodec.Decode(encoded, 2, hash), ErrorKind.DataIntegrity); Error(() => IntArrayCodec.Decode(encoded, 3, "bad"), ErrorKind.DataIntegrity);
                    Error(() => IntArrayCodec.Decode("+1", 1, IntArrayCodec.ComputeHash("+1")), ErrorKind.DataIntegrity);
                }
            });
            test("repository rejects duplicate ID and timestamp/revision tampering", p => {
                using (var s = new JsonDataStore(p)) { s.Initialize(); var repo = new JsonTestCaseRepository(s); var c = Service(s).Create(Draft()); var before = File.ReadAllBytes(p);
                    Error(() => repo.Add(c), ErrorKind.Conflict);
                    Error(() => repo.Update(c, 1), ErrorKind.Validation);
                    var future = c.CreatedAtUtc.AddMinutes(1); var tampered = new TestCase(c.Id, 2, c.ToDraft("tampered"), future, future);
                    Error(() => repo.Update(tampered, 1), ErrorKind.Validation); Assert(before.SequenceEqual(File.ReadAllBytes(p)));
                }
            });
            test("rename and delete preserve archived input and samples", p => {
                using (var s = new JsonDataStore(p)) { s.Initialize(); var service = Service(s); var c = service.Create(Draft());
                    s.Commit(d => d.BenchmarkRuns.Add(StorageTests.Run(d.TestCases[0]))); var old = s.Load().BenchmarkRuns[0];
                    service.Update(c.Id, 1, TestCaseDraft.Manual("new input", new[] { 9, 8 }));
                    service.Delete(c.Id, 2); var saved = s.Load().BenchmarkRuns.Single();
                    Assert(saved.TestCaseId == null && saved.CaseName == "Alpha" && saved.CaseRevision == 1 && saved.InputHash == old.InputHash && saved.InputDataText == old.InputDataText && saved.Samples.Count == 5);
                }
            });
            test("failed update/delete atomically preserve arrays and references", p => {
                var io = new FailPublish();
                using (var s = new JsonDataStore(p, io)) { s.Initialize(); var service = Service(s); var c = service.Create(Draft()); s.Commit(d => d.BenchmarkRuns.Add(StorageTests.Run(d.TestCases[0])));
                    var before = File.ReadAllBytes(p); io.Fail = true;
                    Error(() => service.Update(c.Id, 1, TestCaseDraft.Manual("fail", new[] { 8 })), ErrorKind.Persistence);
                    Error(() => service.Delete(c.Id, 1), ErrorKind.Persistence);
                    Assert(before.SequenceEqual(File.ReadAllBytes(p))); var unchanged = service.GetDetails(c.Id); Assert(unchanged.Revision == 1 && unchanged.Data.SequenceEqual(c.Data)); Assert(s.Load().BenchmarkRuns[0].TestCaseId != null);
                }
            });
            test("CLI create/search/detail/rename/cancel/delete across processes", p => {
                Assert(Cli(p, "1\n4\n示範案例\n2\n3,1,2\n\nY\n0\n0\n").Contains("已新增"));
                Assert(Cli(p, "1\n5\n1\n1\n改名案例\nY\n0\n0\n").Contains("版次 2"));
                Assert(Cli(p, "1\n2\n改名\n5\n3\n1\n0\n0\n").Contains("改名案例"));
                var old = File.ReadAllBytes(p); Assert(Cli(p, "1\n6\n1\nN\n0\n0\n").Contains("已取消")); Assert(old.SequenceEqual(File.ReadAllBytes(p)));
                Assert(Cli(p, "1\n6\n1\nY\n0\n0\n").Contains("已刪除"));
                using (var s = new JsonDataStore(p)) Assert(s.Initialize().TestCases.Count == 0);
            });
            test("CLI malformed data and EOF never save a draft", p => {
                Assert(Cli(p, "1\n4\ninvalid\n2\n1,,2\n\n0\n0\n").Contains("操作未完成"));
                Assert(Cli(p, "1\n4\nunfinished\n2\n1 2\n").Contains("輸入結束"));
                using (var s = new JsonDataStore(p)) Assert(s.Initialize().DocumentRevision == 1);
            });
            test("CLI generated preview, explicit replacement and cancellation", p => {
                Assert(Cli(p, "1\n4\n自動\n1\n3\n4\n-2\n2\n42\nY\n0\n0\n").Contains("種子：42"));
                using (var s = new JsonDataStore(p)) { var c = s.Initialize().TestCases.Single(); Assert(c.Distribution == "Reversed" && c.DataCount == 4 && c.Seed == 42 && c.Data.SequenceEqual(c.Data.OrderByDescending(v => v))); }
                Assert(Cli(p, "1\n5\n1\n2\n\n2\n7 7 3\n\nY\n0\n0\n").Contains("版次 2"));
                using (var s = new JsonDataStore(p)) { var c = s.Initialize().TestCases.Single(); Assert(c.Distribution == "Manual" && c.Seed == null && c.Name == "自動" && c.Data.SequenceEqual(new[] { 7, 7, 3 })); }
                var before = File.ReadAllBytes(p);
                Assert(Cli(p, "1\n5\n1\n2\n\n1\n4\n50\n-10\n0\n\nN\n0\n0\n").Contains("已取消"));
                Assert(before.SequenceEqual(File.ReadAllBytes(p)));
            });
            Console.WriteLine("Case tests: " + passed + " passed; " + failed + " failed."); return failed;
        }
    }
}
