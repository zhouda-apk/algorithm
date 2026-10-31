# Phase 0 共用契約基準

契約版本：1.3，更新日期：2026-10-30。依開發計畫書第 6～10 節，保留 Phase 0 的型別與儲存語意。

本文件是四位開發者共用的型別與簽章規格；Phase 0 只建立可建置專案，模型與介面的 C# 宣告依 Phase 1～4 逐步加入，不建立具有假資料或 NotImplementedException 的產品服務。修改契約需由 A 同步更新本文件、計畫書與所有使用端。

2026-10-30 Phase 1 實作狀態：Data/FileStorage 已建立文件／案例／Run／樣本 DTO、文件驗證器及 JsonDataStore；Core 已建立 ErrorKind／AlgorithmLabException。領域模型、Repository 與案例／測試服務仍待後續階段，不將 DTO 當成 Core 領域模型。

2026-10-30 Phase 2：TestCase／Draft／Summary／Snapshot、DataDistribution、ITestCaseRepository、DataGenerator、ManualDataParser、IntArrayCodec、TestCaseService 及 JsonTestCaseRepository 已實作。Benchmark／排序相關型別與服務仍待 Phase 3、4。

2026-10-30 Phase 3：AlgorithmDescriptor、SortExecutionMode、SortExecutionContext、SortMetrics、ISortAlgorithm、SortAlgorithmBase、BubbleSort、SelectionSort、InsertionSort、AlgorithmCatalog 及 SortResultValidator 已實作。Benchmark 管線與結果 Repository 留待 Phase 4。

2026-10-30 Phase 4：BenchmarkOptions／Request／Sample／Statistics／Progress／HistoryQuery／Run／Status、EnvironmentInfo、PendingRunFinalization、BenchmarkPersistenceException、IBenchmarkRepository、統計計算、環境收集、BenchmarkService 及 JsonBenchmarkRepository 已實作，主控台已組合測試與歷史功能。

BenchmarkService 建構接受 catalog、IBenchmarkRepository、ITestCaseRepository 及可選 Action<Exception> 診斷回呼；Run 除驗證 request 外，也核對目前案例版次與輸入 Hash。相同服務同時只允許一個 Run／RetrySave，重入為 Conflict。Run 使用外部 token 與期限連結；進度回呼都在計時外，其自身錯誤只記診斷，不改測試結果。取消只由 token 提出。

BenchmarkOptions 預設 1／5／30；為後續擴充可接受預熱 1 或 3、正式 5～30、期限 10～120 秒，目前 CLI 固定預設值。BenchmarkRun 以唯讀屬性組合輸入、演算法、設定、環境及最終資料；PendingRunFinalization 僅代表待保存的候選結果，不代表已成功保存。Run／RetrySave 只有 Repository 成功後才回傳正式結果。

BenchmarkStatisticsCalculator.Calculate(IEnumerable<BenchmarkSample>, long frequency) 接受非空有效正式樣本；使用 timer ticks 換算毫秒，不依賴目前機器的 Frequency。EnvironmentKey 依 AppVersion、BuildConfiguration、ProcessArchitecture、CLR、OS、MachineName、ProcessorCount、CPU 識別、DebuggerAttached、Frequency、IsHighResolution 順序，以每欄「Invariant 字元長度:內容」串接後作 UTF-8 SHA-256；此固定形式避免欄位邊界混淆。

TestCaseSnapshot 已新增可指定歷史案例 Id／版次的建構入口，Id 可 null 且保留舊版次。從 JSON 還原時先以 IntArrayCodec 驗證資料，仍保留目前 5000 筆期中限制；期末批次規模再於對應階段擴充。JSON 的持久化演算法資訊仍為 Id／Version，歷史描述只對目前已知版本補顯示文字；未知歷史版本不以目前描述推斷複雜度或穩定性。

JsonBenchmarkRepository.FinalizeRun 的相同重試不寫入第二次，任意不同的最終內容為 Conflict；案例參照若已在執行期間刪除，保存時沿用持久化快照的 null Id，不能重新掛回舊案例。MarkInterruptedRuns 一次提交所有遺留 Running，不重跑排序、不產生正式統計。

排序呼叫固定為 `Sort(int[] data, SortExecutionContext context)`。context 建構接受 mode 及可選 CancellationToken，每次建立零計數；同一 context 不可重用或並行分享，重用拋 InvalidOperationException，null 引數先拋 ArgumentNullException。三種演算法本身不持有計數狀態。

AlgorithmCatalog 建構接受 `IEnumerable<Func<ISortAlgorithm>>`，由組合入口註冊；GetAll 回傳唯讀描述清單並保留註冊順序，GetById 使用 Ordinal Id 比對並經 factory 取得策略。null／空白 Id 為 Validation，未註冊 Id 為 NotFound，重複或不一致描述為 Validation。

SortResultValidator.CreateExpected(original) 回傳排序好的獨立副本；Validate(expected, actual) 接收已排序的正確答案與實際結果，長度或任一值不符時為 SortValidationFailure。正確答案未排序為 ArgumentException，null 引數為 ArgumentNullException；驗證沒有 context，不影響操作數。

本次補齊生成操作的精確形狀：`DataGenerator.Generate(DataDistribution distribution, int count, int minValue, int maxValue, int? seed = null)` 回傳唯讀 GeneratedData，含分布、資料副本、筆數、已選定種子、gen-1 版本及範圍；其 ToDraft(name) 建立可保存草稿。ManualDataParser.Parse(text) 回傳已解析 int[]，TestCaseDraft.Manual(name, data) 由實際數值計算極值。TestCase.ToDraft(name) 保留其原資料與生成參數，供改名使用。

目前 TestCaseSnapshot 由已驗證 TestCase 建立並計算文字／Hash；期末無案例 Id 的批次來源建構入口留待批次階段擴充。IntArrayCodec.Decode(text, expectedCount, inputHash) 驗證筆數、標準十進位格式與雜湊後才回傳整數陣列。

## 命名與所有權

- 目標固定 .NET Framework 4.7.2、C# 7.3；主要方案僅 Debug／Release x64。
- namespace 採專案名加資料夾：AlgorithmLab.Core.Models、Contracts、Algorithms、Services；AlgorithmLab.Data.FileStorage、Repositories；ConsoleApp1.Presentation。
- 一個公開型別一個同名檔案。Core/Contracts 只放需要替換或測試的邊界；不建立另一套 UI 模型來取代領域資料。
- 字串／GUID／列舉轉 JSON 的 DTO 位於 Data/FileStorage，Core 模型不帶序列化屬性。DTO 與領域物件須明確轉換與驗證。
- 公開讀取陣列回傳副本；清單回傳唯讀集合或獨立副本，不公開可修改的儲存快取。

## 固定值

| 項目 | 值 |
| --- | --- |
| DataDistribution | Random、Sorted、Reversed、FewUnique、Manual |
| BenchmarkStatus | Running、Completed、Cancelled、Failed、Interrupted |
| SortExecutionMode | Timing、Analysis |
| AlgorithmId | bubble、selection、insertion；期末 quick、merge |
| 初始版本 | AlgorithmVersion＝1.0、MetricsVersion＝metrics-1、GeneratorVersion＝gen-1、InputFormatVersion＝intcsv-1 |
| 期中限制 | N＝1～5,000；生成範圍 -1,000,000～1,000,000；名字 Trim 後 1～50 字元 |
| 期中測量 | 1 次預熱、5 次正式時間、1 次獨立 Analysis、30 秒整個排序流程上限 |
| 檔案格式 | fileFormatVersion＝1，20 MiB 上限，2,000,000 物件圖項目上限 |
| 正式主檔 | LocalApplicationData/AlgorithmLab/store.json，備份 store.json.bak |

列舉保存在 JSON 時使用上述名稱字串，不保存數字序號。CancellationToken 用於排序取消；取消檢查不列入比較次數。

## 領域模型形狀

下表指定公開讀取資料及其型別。Phase 2 建構與修改時必須依計畫書驗證不變條件；不能用任意 public setter 讓外部改壞狀態。未取得的統計用 nullable 表示，不以零代替。

| 型別 | 固定資料與責任 |
| --- | --- |
| TestCase | Guid Id；string Name；int Revision；DataDistribution Distribution；int DataCount（由 Data 長度取得）；int? Seed；string GeneratorVersion（Manual 可 null）；int MinValue／MaxValue；DateTime CreatedAtUtc／UpdatedAtUtc；int[] Data（防禦性副本） |
| TestCaseDraft | string Name；DataDistribution Distribution；int? Seed；string GeneratorVersion；int MinValue／MaxValue；int[] Data。它是已解析／生成、但尚未驗證保存的候選資料；不含新 Id 或 Revision |
| TestCaseSummary | Id、Name、Revision、Distribution、DataCount、UpdatedAtUtc；型別同 TestCase，不含完整 Data |
| TestCaseSnapshot | Guid? TestCaseId；int? CaseRevision；string CaseName；分布、數量、種子、生成版本與範圍；int[] Data 副本；string InputDataText／InputFormatVersion／InputHash。期中由現有案例建立；期末批次可無案例 Id |
| AlgorithmDescriptor | string Id／Name／Version／TimeComplexityDescription；bool IsStable／IsMidtermAlgorithm |
| BenchmarkOptions | int WarmupCount／MeasuredCount／TimeoutSeconds；有效值依期中／期末限制驗證 |
| BenchmarkRequest | TestCaseSnapshot Input；string AlgorithmId；BenchmarkOptions Options |
| SortExecutionContext | SortExecutionMode Mode；CancellationToken CancellationToken；SortMetrics Metrics；每次 Sort 獨立建立 |
| SortMetrics | long ComparisonCount／SwapCount／WriteCount；計數由排序 helper 更新，Timing 不遞增 |
| BenchmarkSample | int SampleIndex（起始 1）；long ElapsedTimerTicks（非負）；bool IsValid |
| BenchmarkStatistics | double MedianMs／MeanMs／MinMs／MaxMs／PopulationStdDevMs；有效正式樣本統計 |
| EnvironmentInfo | string AppVersion／BuildConfiguration／ProcessArchitecture／ClrVersion／OsVersion／MachineName／CpuModel／EnvironmentKey；int ProcessorCount；bool DebuggerAttached／IsHighResolution；long StopwatchFrequency |
| BenchmarkRun | Guid RunId；TestCaseSnapshot Input；AlgorithmDescriptor Algorithm；string MetricsVersion；BenchmarkOptions Options；EnvironmentInfo Environment；BenchmarkStatus Status；DateTime StartedAtUtc；DateTime? FinishedAtUtc；唯讀 BenchmarkSample 集合 Samples；BenchmarkStatistics Statistics 可 null；SortMetrics Metrics 可 null；string OutputPreviewText／ErrorCode／ErrorMessage 可 null |
| PendingRunFinalization | Guid RunId；BenchmarkStatus TargetStatus（不能是 Running）；DateTime FinishedAtUtc；完整樣本集合；可空 Statistics／Metrics／OutputPreviewText／ErrorCode／ErrorMessage；僅用於原 Run 重試保存 |
| BenchmarkProgress | string Stage；int CompletedSampleCount／TotalSampleCount；階段名稱固定 Preparing、Warmup、Measuring、Analysis、Saving、Finished |
| BenchmarkHistoryQuery | Guid? TestCaseId；string AlgorithmId（null 不篩）；BenchmarkStatus? Status；int Offset（≥0）；int Limit（1～100，預設 20） |

BenchmarkRun 的組合模型在 Data DTO 中依計畫書第 8.8 節展平；RunId→id、Input→快照欄位、Algorithm.Id→algorithmId、統計／環境→各自欄位，Samples→samples。未來 SQLite 使用同一映射，避免資料格式跟隨 UI 變動。

## Repository 簽章

此處以「名稱：參數 → 回傳」定義簽章；實際介面置於 Core/Contracts。

| 操作 | 簽章與行為 |
| --- | --- |
| ITestCaseRepository.GetSummaries | 無參數 → IReadOnlyList<TestCaseSummary>；UpdatedAtUtc 降冪、Id 升冪 |
| ITestCaseRepository.GetById | Guid id → TestCase；不存在拋 NotFound 類型錯誤，不回傳空物件 |
| ITestCaseRepository.Add | TestCase testCase → void；拒絕重複 Id，保存完整案例 |
| ITestCaseRepository.Update | TestCase replacement, int expectedRevision → void；replacement.Id 定位，原版次須等於 expectedRevision、新版次＝舊＋1；保存全份資料 |
| ITestCaseRepository.Delete | Guid id, int expectedRevision → void；原子刪除案例並將歷史 Run.TestCaseId 清為 null，快照不變 |
| IBenchmarkRepository.CreateRunning | BenchmarkRun run → void；只接受完整輸入及 Running 狀態，拒絕重複 RunId |
| IBenchmarkRepository.FinalizeRun | PendingRunFinalization finalization → BenchmarkRun；原子保存整批樣本與最終狀態；未知 RunId 為 NotFound；相同最終內容重試可直接讀回，不同內容為 Conflict |
| IBenchmarkRepository.GetHistory | BenchmarkHistoryQuery query → IReadOnlyList<BenchmarkRun>；StartedAtUtc 降冪、RunId 升冪，再套 Offset／Limit |
| IBenchmarkRepository.GetDetails | Guid runId → BenchmarkRun；不存在拋 NotFound |
| IBenchmarkRepository.MarkInterruptedRuns | DateTime utcNow → int；將遺留 Running 全部標為 Interrupted，完成時間設 utcNow，回傳變更數量 |

兩介面由 B 實作；JSON 版本共用一個 JsonDataStore。Repository 操作成功前不得變更對外可見快取；同一操作只作一次完整文件提交。未來 SQLite 必須符合相同契約。

## 排序與服務邊界

| 操作 | 簽章／責任 |
| --- | --- |
| ISortAlgorithm.Descriptor | AlgorithmDescriptor 唯讀屬性 |
| ISortAlgorithm.Sort | int[] data, SortExecutionContext context → void；原地排序；無 I/O；null 引數拋 ArgumentNullException，取消拋 OperationCanceledException |
| TestCaseService.Create | TestCaseDraft draft → TestCase；驗證、給 Id／Revision＝1／UTC 時間、保存 |
| TestCaseService.Search | string keyword, DataDistribution? distribution → IReadOnlyList<TestCaseSummary>；空字串查全部，名稱 OrdinalIgnoreCase 子字串 |
| TestCaseService.GetDetails | Guid id → TestCase |
| TestCaseService.Update | Guid id, int expectedRevision, TestCaseDraft draft → TestCase；服務產生下一版次後交 Repository |
| TestCaseService.Delete | Guid id, int expectedRevision → void；呈現層先確認 |
| TestCaseService.CreateSnapshot | Guid id, int expectedRevision → TestCaseSnapshot；讀最新資料核對版次，凍結副本／文字／Hash |
| BenchmarkService.Run | BenchmarkRequest request, CancellationToken cancellationToken, IProgress<BenchmarkProgress> progress（可 null） → BenchmarkRun；同步，保存成功才返回正式結果 |
| BenchmarkService.RetrySave | PendingRunFinalization finalization → BenchmarkRun；不重新排序 |

正式的服務實作分配至 Phase 2～4。Phase 1 可先建立儲存所需模型／DTO／介面，D 用相同 Repository 契約設計後續驗證，不另外改變操作名稱。

錯誤由 Core 的 AlgorithmLabException 搭配 ErrorKind 表達：Validation、NotFound、Conflict、Persistence、DataIntegrity、SortValidationFailure。最終保存失敗使用 BenchmarkPersistenceException，攜帶 PendingRunFinalization。排序取消使用 OperationCanceledException，由測試服務轉換 Cancelled，ErrorCode 為 USER_CANCELLED 或 TIMEOUT。

## JSON 文件與安全保存基準

根欄位固定為 fileFormatVersion（int＝1）、documentRevision（long≥1）、savedAtUtc（ISO 8601 UTC 字串）、testCases（陣列）、benchmarkRuns（陣列）。DTO 成員 lowerCamelCase，Guid 使用 D 格式，列舉為名稱、布林為 JSON boolean、計數為整數。

TestCase DTO 欄位固定為 id、name、revision、distribution、dataCount、seed、generatorVersion、minValue、maxValue、createdAtUtc、updatedAtUtc、data。每個 Run 的 samples 只包含正式測量，保存 sampleIndex、elapsedTimerTicks、isValid；不包含預熱及 Analysis。

InputDataText 是 invariant 十進位整數以半形逗號連接，無空白、BOM、換行；InputHash 為該文字 UTF-8 bytes 的 SHA-256 小寫十六進位。案例 Data 是 JSON 整數陣列；Run 保留獨立快照文字，刪除案例不遺失輸入。

寫入順序固定：共享鎖→候選文件→驗證→documentRevision＋1→同目錄暫存→flush／關閉→讀回驗證→替換主檔並保留備份→發布記憶體狀態。第一次建立使用完整檔案搬移；不先刪主檔，不自動以空文件覆蓋壞檔。復原、大小限制與遷移細節沿用計畫書第 8 節。

## 統計與驗收基準

- Timing 與 Analysis 執行相同排序策略；時間與計數分開，所有測量使用相同原始資料的新副本。
- 比較次數只計資料值比較。交換兩個不同索引計一次交換、兩次寫入；同索引不計。主資料／Merge 資料輔助陣列寫入計數，索引 stack 與局部變數不計。
- Bubble 使用整輪無交換提前結束；Selection 不做同索引交換；Insertion 採位移且最後寫回 key 計一次寫入。
- `[3,1,2]` 的比較／交換／寫入：Bubble＝3／2／4、Selection＝3／2／4、Insertion＝3／0／4。
- `[1,2,3]` 對應 Bubble＝2／0／0、Selection＝3／0／0、Insertion＝2／0／2。
- 毫秒＝timer ticks×1000.0÷Frequency。中位數偶數取中間兩值平均；標準差分母為 N。未完成 Run 不產生正式聚合成績。
- 任何新演算法或統計規則變更都更新對應版本；不更改既有歷史資料的定義。

期末的 SQLite、遷移、WinForms、批次與動畫均保持在各自後續階段。Phase 0 不為其加入套件或假的可執行功能。
