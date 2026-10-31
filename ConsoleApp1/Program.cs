using System;
using System.Text;
using AlgorithmLab.Core.Models;
using AlgorithmLab.Data.FileStorage;
namespace ConsoleApp1
{
    internal class Program
    {
        private static int Main(string[] args)
        {
            Console.OutputEncoding = Encoding.UTF8;
            Console.InputEncoding = new UTF8Encoding(false);
            if (args.Length != 0 && !(args.Length == 2 && args[0] == "--data-path"))
            { Console.WriteLine("使用方式：ConsoleApp1.exe [--data-path 完整JSON路徑]"); return 1; }
            try
            {
                using (var store = new JsonDataStore(args.Length == 2 ? args[1] : null))
                {
                    while (true)
                    {
                        try
                        {
                            var document = store.Initialize();
                            Console.WriteLine("Algorithm Lab - Phase 4");
                            var startupEnvironment = AlgorithmLab.Core.Services.EnvironmentInfoCollector.Collect();
                            Console.WriteLine("版本 " + startupEnvironment.AppVersion + "｜" + startupEnvironment.BuildConfiguration + " " + startupEnvironment.ProcessArchitecture + "｜Debugger " + startupEnvironment.DebuggerAttached);
                            Console.WriteLine("JSON：" + store.FilePath);
                            Console.WriteLine("文件版本：" + document.FileFormatVersion + "；保存版次：" + document.DocumentRevision);
                            Console.WriteLine("案例：" + document.TestCases.Count + "；歷史紀錄：" + document.BenchmarkRuns.Count);
                            var repository = new AlgorithmLab.Data.Repositories.JsonTestCaseRepository(store);
                            var service = new AlgorithmLab.Core.Services.TestCaseService(repository);
                            var runs = new AlgorithmLab.Data.Repositories.JsonBenchmarkRepository(store);
                            int interrupted = runs.MarkInterruptedRuns(DateTime.UtcNow);
                            if (interrupted > 0) Console.WriteLine("已將 " + interrupted + " 個未完成工作標記 Interrupted。");
                            var catalog = new AlgorithmLab.Core.Services.AlgorithmCatalog(new Func<AlgorithmLab.Core.Contracts.ISortAlgorithm>[] {
                                () => new AlgorithmLab.Core.Algorithms.BubbleSort(), () => new AlgorithmLab.Core.Algorithms.SelectionSort(), () => new AlgorithmLab.Core.Algorithms.InsertionSort() });
                            var benchmark = new AlgorithmLab.Core.Services.BenchmarkService(catalog, runs, repository,
                                error => Diagnostics.FileDiagnosticLog.Write(System.IO.Path.GetDirectoryName(store.FilePath), error));
                            var menu = new Presentation.BenchmarkMenu(service, benchmark, runs, catalog, Console.In, Console.Out);
                            new Presentation.ConsoleMenu(service, Console.In, Console.Out, menu).Run();
                            return 0;
                        }
                        catch (AlgorithmLabException e)
                        {
                            Console.WriteLine("儲存無法啟動：" + e.Message);
                            bool canRestore = false;
                            try { var backup = store.InspectBackup(); canRestore = true; Console.WriteLine("可用備份：版次 " + backup.DocumentRevision + "，時間 " + backup.SavedAtUtc); }
                            catch (AlgorithmLabException) { }
                            Console.WriteLine(canRestore ? "R 重試／B 復原上述備份（可能失去較新的操作）／其他或 Enter 結束" : "R 重試／其他或 Enter 結束");
                            var choice = Console.ReadLine();
                            if (string.Equals(choice, "R", StringComparison.OrdinalIgnoreCase)) continue;
                            if (canRestore && string.Equals(choice, "B", StringComparison.OrdinalIgnoreCase))
                            {
                                try { store.RestoreBackup(); continue; }
                                catch (AlgorithmLabException restoreError) { Console.WriteLine("復原失敗：" + restoreError.Message); }
                            }
                            return 1;
                        }
                    }
                }
            }
            catch (AlgorithmLabException e) { Console.WriteLine(e.Message); return 1; }
            catch (ArgumentException e) { Console.WriteLine("路徑無效：" + e.Message); return 1; }
            catch (System.IO.IOException e) { Console.WriteLine("檔案操作失敗：" + e.Message); return 1; }
            catch (UnauthorizedAccessException e) { Console.WriteLine("沒有存取權限：" + e.Message); return 1; }
        }
    }
}
