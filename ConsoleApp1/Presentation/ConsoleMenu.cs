using System;
using System.IO;
using System.Linq;
using System.Text;
using AlgorithmLab.Core.Models;
using AlgorithmLab.Core.Services;
namespace ConsoleApp1.Presentation
{
    internal sealed class ConsoleMenu
    {
        private readonly TestCaseService service;
        private readonly DataGenerator generator = new DataGenerator();
        private readonly ConsoleInputReader reader;
        private readonly TextWriter output;
        private readonly BenchmarkMenu benchmarks;
        internal ConsoleMenu(TestCaseService service, TextReader input, TextWriter output, BenchmarkMenu benchmarks)
        { this.service = service; reader = new ConsoleInputReader(input, output); this.output = output; this.benchmarks = benchmarks; }
        internal void Run()
        {
            try
            {
                while (true)
                {
                    output.WriteLine("\n主選單：1 案例管理／2 執行測試／3 歷史紀錄／4 建立展示案例／0 結束（:q 取消）");
                    var choice = reader.Read("選項：").Trim();
                    if (choice == "0") return;
                    try {
                        if (choice == "1") Manage();
                        else if (choice == "2") benchmarks.RunTest();
                        else if (choice == "3") benchmarks.History();
                        else if (choice == "4") DemonstrationCases();
                        else output.WriteLine("無效選項。");
                    }
                    catch (AlgorithmLabException e) { output.WriteLine("操作未完成 [" + e.Kind + "]：" + e.Message); }
                    catch (OperationCanceledException) { output.WriteLine("已取消。"); }
                }
            }
            catch (EndOfStreamException) { output.WriteLine("輸入結束，正常離開。"); }
            catch (OperationCanceledException) { output.WriteLine("已離開。"); }
        }
        private void DemonstrationCases()
        {
            if (!reader.Confirm("建立 Random／Sorted／Reversed 各 1000 筆、範圍 -10000～10000、種子 42 的展示案例？Y/N：")) return;
            foreach (var distribution in new[] { DataDistribution.Random, DataDistribution.Sorted, DataDistribution.Reversed }) {
                var value = service.Create(generator.Generate(distribution, 1000, -10000, 10000, 42).ToDraft("展示-" + distribution));
                output.WriteLine("已新增展示案例：" + value.Name + "，ID " + value.Id);
            }
        }
        private void Manage()
        {
            while (true)
            {
                try
                {
                    output.WriteLine("\n案例管理：1 列出／2 搜尋／3 檢視／4 新增／5 修改／6 刪除／0 返回");
                    switch (reader.Read("選項：").Trim())
                    {
                        case "0": return;
                        case "1": List(); break;
                        case "2": Search(); break;
                        case "3": Show(Select()); break;
                        case "4": Create(); break;
                        case "5": Edit(); break;
                        case "6": Delete(); break;
                        default: output.WriteLine("無效選項。"); break;
                    }
                }
                catch (AlgorithmLabException e) { output.WriteLine("操作未完成 [" + e.Kind + "]：" + e.Message); }
                catch (OperationCanceledException) { output.WriteLine("已取消，資料未修改。"); }
            }
        }
        private void Print(System.Collections.Generic.IReadOnlyList<TestCaseSummary> cases)
        {
            if (cases.Count == 0) { output.WriteLine("沒有符合的案例。"); return; }
            for (int i = 0; i < cases.Count; i++)
            {
                var c = cases[i];
                output.WriteLine((i + 1) + ". " + c.Name + "｜" + c.Id + "｜" + c.DataCount + " 筆｜" + c.Distribution + "｜版次 " + c.Revision + "｜" + c.UpdatedAtUtc.ToLocalTime().ToString("yyyy-MM-dd HH:mm:ss"));
            }
        }
        private void List() { Print(service.Search()); }
        private void Search()
        {
            var word = reader.Read("名稱關鍵字（空白代表全部）：");
            int choice = reader.Integer("分布：0 全部／1 Random／2 Sorted／3 Reversed／4 FewUnique／5 Manual：");
            if (choice < 0 || choice > 5) throw new AlgorithmLabException(ErrorKind.Validation, "分布選項無效。");
            Print(service.Search(word, choice == 0 ? (DataDistribution?)null : (DataDistribution)(choice - 1)));
        }
        private TestCase Select()
        {
            var list = service.Search(); Print(list);
            if (list.Count == 0) throw new OperationCanceledException();
            int index = reader.Integer("案例序號：");
            if (index < 1 || index > list.Count) throw new AlgorithmLabException(ErrorKind.Validation, "案例序號無效。");
            var summary = list[index - 1]; var value = service.GetDetails(summary.Id);
            if (value.Revision != summary.Revision) throw new AlgorithmLabException(ErrorKind.Conflict, "清單版次已改變，請重新選取。");
            return value;
        }
        private void Preview(TestCaseDraft draft)
        {
            var data = draft.Data;
            output.WriteLine("名稱：" + draft.Name + "；分布：" + draft.Distribution + "；筆數：" + data.Length + "；範圍：" + draft.MinValue + "～" + draft.MaxValue);
            output.WriteLine("種子：" + (draft.Seed.HasValue ? draft.Seed.Value.ToString(System.Globalization.CultureInfo.InvariantCulture) : "無（手動）"));
            output.WriteLine("資料：" + string.Join(", ", data.Take(20)) + (data.Length > 20 ? " …（僅前 20 筆）" : ""));
        }
        private void Show(TestCase value)
        {
            output.WriteLine("ID：" + value.Id + "；版次：" + value.Revision);
            output.WriteLine("建立：" + value.CreatedAtUtc.ToLocalTime().ToString("o"));
            Preview(value.ToDraft(value.Name));
        }
        private TestCaseDraft ReadDraft(string name)
        {
            int source = reader.Integer("資料來源：1 自動生成／2 手動：");
            if (source == 2)
            {
                output.WriteLine("輸入整數，可用半形逗號、空白或換行分隔；空白行結束，:q 取消：");
                var text = new StringBuilder();
                while (true)
                {
                    var line = reader.Read(""); if (string.IsNullOrWhiteSpace(line)) break;
                    if (text.Length + line.Length > 100000) throw new AlgorithmLabException(ErrorKind.Validation, "輸入文字過長。");
                    text.AppendLine(line);
                }
                return TestCaseDraft.Manual(name, ManualDataParser.Parse(text.ToString()));
            }
            if (source != 1) throw new AlgorithmLabException(ErrorKind.Validation, "資料來源無效。");
            int distribution = reader.Integer("分布：1 Random／2 Sorted／3 Reversed／4 FewUnique：");
            if (distribution < 1 || distribution > 4) throw new AlgorithmLabException(ErrorKind.Validation, "分布選項無效。");
            int n = reader.Integer("數量（1～5000）："); int min = reader.Integer("最小值："); int max = reader.Integer("最大值：");
            string seedText = reader.Read("種子（空白自動選定）：").Trim(); int seed = 0;
            if (seedText.Length > 0 && !int.TryParse(seedText, System.Globalization.NumberStyles.AllowLeadingSign, System.Globalization.CultureInfo.InvariantCulture, out seed))
                throw new AlgorithmLabException(ErrorKind.Validation, "種子必須是 Int32 整數。");
            int? selectedSeed = seedText.Length == 0 ? (int?)null : seed;
            return generator.Generate((DataDistribution)(distribution - 1), n, min, max, selectedSeed).ToDraft(name);
        }
        private void Create()
        {
            var name = reader.Read("案例名稱："); var draft = ReadDraft(name); Preview(draft);
            if (!reader.Confirm("儲存？Y/N：")) { output.WriteLine("已取消，資料未修改。"); return; }
            var saved = service.Create(draft); output.WriteLine("已新增：" + saved.Name + "；ID " + saved.Id + "；版次 " + saved.Revision);
        }
        private void Edit()
        {
            var old = Select(); Show(old);
            int mode = reader.Integer("1 只改名稱／2 重新生成或替換資料：");
            var name = reader.Read("名稱（空白沿用）："); if (string.IsNullOrWhiteSpace(name)) name = old.Name;
            TestCaseDraft draft;
            if (mode == 1) draft = old.ToDraft(name);
            else if (mode == 2) draft = ReadDraft(name);
            else throw new AlgorithmLabException(ErrorKind.Validation, "修改選項無效。");
            Preview(draft);
            if (!reader.Confirm("確認修改？Y/N：")) { output.WriteLine("已取消，資料未修改。"); return; }
            var saved = service.Update(old.Id, old.Revision, draft); output.WriteLine("已修改：" + saved.Name + "；版次 " + saved.Revision);
        }
        private void Delete()
        {
            var old = Select(); Show(old); output.WriteLine("刪除案例後，舊測試的輸入快照與歷史紀錄仍保留。");
            if (!reader.Confirm("確認刪除？Y/N：")) { output.WriteLine("已取消，資料未修改。"); return; }
            service.Delete(old.Id, old.Revision); output.WriteLine("已刪除：" + old.Name);
        }
    }
}
