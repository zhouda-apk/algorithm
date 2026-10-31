using System;
using System.Text;
using AlgorithmLab.Core.Models;
using AlgorithmLab.Data.FileStorage;
using AlgorithmLab.Tests.FileStorageTests;
namespace AlgorithmLab.Tests
{
    internal static class TestRunner
    {
        private static int Main(string[] args)
        {
            Console.OutputEncoding = Encoding.UTF8;
            if (args.Length == 2 && args[0] == "--try-store")
            {
                try { using (var s = new JsonDataStore(args[1])) { return 0; } }
                catch (AlgorithmLabException e) when (e.Kind == ErrorKind.Conflict) { return 3; }
            }
            var failures = StorageTests.Execute();
            failures += ServiceTests.CaseTests.Execute();
            failures += AlgorithmTests.SortTests.Execute();
            failures += ServiceTests.BenchmarkTests.Execute();
            return failures == 0 ? 0 : 1;
        }
    }
}
