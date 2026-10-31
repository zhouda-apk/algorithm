using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
using AlgorithmLab.Core.Contracts;
using AlgorithmLab.Core.Models;
using AlgorithmLab.Core.Services;
namespace ConsoleApp1.Presentation
{
    internal sealed class BenchmarkMenu
    {
        private readonly TestCaseService cases;
        private readonly BenchmarkService benchmark;
        private readonly IBenchmarkRepository history;
        private readonly AlgorithmCatalog catalog;
        private readonly ConsoleInputReader reader;
        private readonly TextWriter output;
        private sealed class ProgressSink : IProgress<BenchmarkProgress>
        {
            private readonly TextWriter output;
            internal ProgressSink(TextWriter output) { this.output = output; }
            public void Report(BenchmarkProgress p) { output.WriteLine("階段：" + p.Stage + "；正式樣本 " + p.CompletedSampleCount + "/" + p.TotalSampleCount); }
        }
        internal BenchmarkMenu(TestCaseService cases, BenchmarkService benchmark, IBenchmarkRepository history, AlgorithmCatalog catalog, TextReader input, TextWriter output)
        { this.cases = cases; this.benchmark = benchmark; this.history = history; this.catalog = catalog; reader = new ConsoleInputReader(input, output); this.output = output; }
        internal void RunTest()
        {
            var list = cases.Search();
            if (list.Count == 0) { output.WriteLine("尚無案例，請先到案例管理新增。"); return; }
            for (int i = 0; i < list.Count; i++) output.WriteLine((i + 1) + ". " + list[i].Name + "｜" + list[i].DataCount + " 筆｜版次 " + list[i].Revision);
            int selected = reader.Integer("案例序號：");
            if (selected < 1 || selected > list.Count) throw new AlgorithmLabException(ErrorKind.Validation, "案例序號無效。");
            var c = list[selected - 1]; var algorithms = catalog.GetAll();
            for (int i = 0; i < algorithms.Count; i++) output.WriteLine((i + 1) + ". " + algorithms[i].Name);
            int choice = reader.Integer("演算法序號：");
            if (choice < 1 || choice > algorithms.Count) throw new AlgorithmLabException(ErrorKind.Validation, "演算法序號無效。");
            var snapshot = cases.CreateSnapshot(c.Id, c.Revision); var options = new BenchmarkOptions();
            output.WriteLine(snapshot.CaseName + "｜版次 " + snapshot.CaseRevision + "｜N=" + snapshot.DataCount + "｜" + algorithms[choice - 1].Name);
            output.WriteLine("輸入雜湊：" + snapshot.InputHash);
            output.WriteLine("預熱 1 次、正式 5 次、獨立操作分析 1 次、上限 30 秒；Ctrl+C 取消。");
            if (!reader.Confirm("開始？Y/N：")) return;
            using (var cts = new CancellationTokenSource()) {
                ConsoleCancelEventHandler cancel = (sender, e) => { e.Cancel = true; try { cts.Cancel(); } catch (ObjectDisposedException) { } };
                Console.CancelKeyPress += cancel;
                try {
                    BenchmarkRun result;
                    try { result = benchmark.Run(new BenchmarkRequest(snapshot, algorithms[choice - 1].Id, options), cts.Token, new ProgressSink(output)); }
                    catch (BenchmarkPersistenceException saveError) {
                        output.WriteLine(saveError.Message);
                        var pending = saveError.PendingRunFinalization;
                        while (true) {
                            if (!reader.Confirm("只重試保存原結果，不重新排序？Y/N：")) return;
                            try { result = benchmark.RetrySave(pending); break; }
                            catch (BenchmarkPersistenceException retryError) { output.WriteLine(retryError.Message); }
                        }
                    }
                    output.WriteLine("紀錄已儲存。"); Show(result);
                }
                finally { Console.CancelKeyPress -= cancel; }
            }
        }
        private static string Number(double value) { return value.ToString("F6", CultureInfo.InvariantCulture); }
        private void Show(BenchmarkRun run)
        {
            output.WriteLine("RunId：" + run.RunId + "；狀態：" + run.Status);
            output.WriteLine("案例：" + run.Input.CaseName + "｜版次 " + run.Input.CaseRevision + "｜" + run.Algorithm.Name + " " + run.Algorithm.Version + "｜N=" + run.Input.DataCount);
            output.WriteLine("輸入雜湊：" + run.Input.InputHash);
            output.WriteLine("原始前 20 筆：" + string.Join(",", run.Input.Data.Take(20)));
            output.WriteLine("結果前 20 筆：" + (run.OutputPreviewText ?? "未完成"));
            if (run.Status == BenchmarkStatus.Completed) {
                output.WriteLine("排序驗證：通過；中位數 " + Number(run.Statistics.MedianMs) + " ms；最小 " + Number(run.Statistics.MinMs) + "；最大 " + Number(run.Statistics.MaxMs));
                output.WriteLine("平均 " + Number(run.Statistics.MeanMs) + " ms；母體標準差 " + Number(run.Statistics.PopulationStdDevMs));
                output.WriteLine("獨立 Analysis：比較 " + run.Metrics.ComparisonCount + "／交換 " + run.Metrics.SwapCount + "／寫入 " + run.Metrics.WriteCount);
            }
            else output.WriteLine("未完成，無正式聚合成績。原因：" + run.ErrorCode + " " + run.ErrorMessage);
            var env = run.Environment;
            output.WriteLine("環境：" + env.BuildConfiguration + " " + env.ProcessArchitecture + "｜App " + env.AppVersion + "｜CLR " + env.ClrVersion + "｜OS " + env.OsVersion);
            output.WriteLine("電腦 " + env.MachineName + "｜CPU " + env.CpuModel + "｜邏輯處理器 " + env.ProcessorCount + "｜EnvironmentKey " + env.EnvironmentKey);
            output.WriteLine("計時頻率 " + env.StopwatchFrequency + "｜高解析 " + env.IsHighResolution + "｜Debugger " + env.DebuggerAttached + "｜" + run.MetricsVersion);
            if (env.BuildConfiguration != "Release" || env.DebuggerAttached) output.WriteLine("此紀錄為 Debug 或附加偵錯器環境，請勿列為正式效能實驗。");
            output.WriteLine("開始 " + run.StartedAtUtc.ToLocalTime().ToString("o") + "；結束 " + (run.FinishedAtUtc.HasValue ? run.FinishedAtUtc.Value.ToLocalTime().ToString("o") : "尚未結束"));
            foreach (var sample in run.Samples) output.WriteLine("樣本 " + sample.SampleIndex + "：ticks=" + sample.ElapsedTimerTicks + "，" + Number(sample.ElapsedTimerTicks * 1000.0 / env.StopwatchFrequency) + " ms，valid=" + sample.IsValid + (sample.ElapsedTimerTicks == 0 ? "（低於本次量測可分辨範圍）" : ""));
        }
        internal void History()
        {
            string algorithm = reader.Read("演算法 Id（bubble/selection/insertion，空白全部）：").Trim();
            if (algorithm.Length == 0) algorithm = null;
            else catalog.GetById(algorithm);
            string statusText = reader.Read("狀態（Running/Completed/Cancelled/Failed/Interrupted，空白全部）：").Trim();
            BenchmarkStatus parsed; BenchmarkStatus? status = null;
            if (statusText.Length > 0) {
                if (!Enum.TryParse(statusText, true, out parsed) || !Enum.IsDefined(typeof(BenchmarkStatus), parsed)) throw new AlgorithmLabException(ErrorKind.Validation, "狀態無效。");
                status = parsed;
            }
            string caseText = reader.Read("案例 GUID（空白全部，已刪案例可在全部歷史查詢）：").Trim(); Guid caseId; Guid? filter = null;
            if (caseText.Length > 0) { if (!Guid.TryParse(caseText, out caseId)) throw new AlgorithmLabException(ErrorKind.Validation, "案例 GUID 無效。"); filter = caseId; }
            int offset = 0;
            while (true) {
                var page = history.GetHistory(new BenchmarkHistoryQuery(filter, algorithm, status, offset));
                if (page.Count == 0) { output.WriteLine("沒有更多紀錄。"); return; }
                for (int i = 0; i < page.Count; i++) output.WriteLine((i + 1) + ". " + page[i].RunId + "｜" + page[i].Input.CaseName + "｜" + page[i].Algorithm.Id + "｜N=" + page[i].Input.DataCount + "｜" + page[i].Status + "｜" + (page[i].Statistics == null ? "未完成" : Number(page[i].Statistics.MedianMs) + " ms") + "｜" + page[i].StartedAtUtc.ToLocalTime().ToString("o"));
                var option = reader.Read("輸入序號看細節／N 下一頁／0 返回：").Trim();
                if (option == "0") return;
                if (string.Equals(option, "N", StringComparison.OrdinalIgnoreCase)) { offset += 20; continue; }
                int index; if (!int.TryParse(option, out index) || index < 1 || index > page.Count) throw new AlgorithmLabException(ErrorKind.Validation, "紀錄序號無效。");
                Show(history.GetDetails(page[index - 1].RunId));
            }
        }
    }
}
