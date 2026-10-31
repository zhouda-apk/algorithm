# Algorithm Lab

C# 演算法效能比較與視覺化平台。目標 .NET Framework 4.7.2、C# 7.3、Debug／Release x64。

目前完成 Phase 0～4 的主控台功能，以及 Phase 5 的本機整合、WinForms 草圖、報告、簡報及 RC1 封版素材。主控台可完成「新增案例→排序測試→保存結果→重開查詢」。**另一台 Windows／帳號及上台終端彩排仍待組員簽核**；詳見 [封版驗收](docs/acceptance-checklist.md)。

## 期中展示與 RC1

- [Phase 5 交付說明](docs/phase5-release.md)：繳交包、重建步驟及驗證範圍。
- [八頁簡報 PPTX](docs/presentation/midterm-briefing.pptx)／[PDF](docs/presentation/midterm-briefing.pdf)：第 1 頁身分欄位可編輯，其他內容為圖像版；完整排版可由產生腳本修改重建。
- [三頁 WinForms 草圖 PDF](docs/wireframes/winforms-wireframes.pdf)／[控制項與事件](docs/winforms-wireframes.md)。草圖是期末規劃，期中以主控台操作。
- [示範腳本](docs/demo-script.md)、[期中報告素材](docs/midterm-report.md)、[資料格式與復原](docs/data-format-and-recovery.md)。
- [RC1 ZIP](artifacts/AlgorithmLab-Midterm-2026-10-30-rc1.zip)：完整解壓後執行 `runtime/Start-Demo.cmd`，示範資料保存於 `%LOCALAPPDATA%\AlgorithmLab-MidtermDemo\store.json`。此目錄由啟動器指定；直接啟動 exe 仍使用一般 AlgorithmLab 路徑。

RC1 的 Runtime 只需要相容的 Windows x64／.NET Framework 4.7.2 或後續相容 4.x 執行環境，不需 Visual Studio。ZIP 內另有 source、validation、README.txt 及逐檔 SHA-256 manifest；原始碼重建仍需下列開發環境。artifacts 是本機交付輸出，受 Git 忽略。

## 開啟與建置

Windows Visual Studio 安裝「.NET 桌面開發」及 4.7.2 Targeting Pack 後，開啟 `ConsoleApp1.slnx`。若版本不支援 slnx，開啟內容相同的 `ConsoleApp1.sln`。將 ConsoleApp1 設為啟始專案，選 Debug 或 Release、平台 x64。

在 Windows PowerShell 的專案根目錄執行：

```powershell
powershell.exe -NoProfile -ExecutionPolicy Bypass -File .\scripts\build.ps1 -Configuration Debug
powershell.exe -NoProfile -ExecutionPolicy Bypass -File .\scripts\build.ps1 -Configuration Release
```

ExecutionPolicy 只作用於這次 PowerShell 行程。腳本從 Visual Studio Installer 找 MSBuild，暫時使用系統層 PATHEXT，結束時還原，不修改永久設定。不要以 Linux dotnet 建置傳統 Framework 專案。

主程式輸出：`ConsoleApp1/bin/x64/Release/ConsoleApp1.exe`。
測試入口：`AlgorithmLab.Tests/bin/x64/Release/AlgorithmLab.Tests.exe`；目前執行 71 項測試（20 項儲存＋15 項案例／主控台＋16 項排序＋20 項 Benchmark）。請先建置完整方案；測試會啟動同組態的 ConsoleApp1.exe，且包含一次實際 10 秒逾時驗證。退出碼 0＝全部通過、1＝有失敗；未來若無測試應回傳 2。

## Phase 4 測試與歷史操作

| 主選單 | 操作 |
| --- | --- |
| 1 | 原有案例管理 |
| 2 | 選案例、演算法，確認案例版次／筆數／輸入雜湊及預設測量設定後輸入 Y 執行 |
| 3 | 按演算法、狀態、案例 GUID 篩選歷史；空白不篩選，預設每頁 20 筆，N 下一頁、序號看細節、0 返回 |
| 4 | 明確確認後新增三個展示案例：Random／Sorted／Reversed，各 1000 筆、範圍 -10000～10000、種子 42；啟動時不自動建立 |
| 0 | 結束 |

每個 Run 先保存 Running，再對相同原始資料的新副本執行 1 次預熱、5 次正式計時及 1 次獨立 Analysis。僅 Sort 呼叫在 Stopwatch 計時區間內；資料複製、答案驗證、進度與檔案 I/O 在計時之外。畫面顯示正確性、中位數／平均／最小／最大／母體標準差，以及一次 Analysis 的比較／交換／寫入數，時間至少六位小數。

Ctrl+C 在測試期間提出取消要求，逾時預設 30 秒；服務保存 Cancelled，並以 USER_CANCELLED／TIMEOUT 區分原因。錯誤答案為 Failed／SORT_INVALID，其他未預期錯誤為 Failed／UNEXPECTED；非完成紀錄沒有正式時間聚合成績。原始樣本可留作診斷，只有 Completed 才視為正式成績。

只有最終保存成功才顯示「紀錄已儲存」。若儲存失敗，可選擇重試同一 RunId 的原結果，重試不重新排序；若放棄，留下的 Running 下次啟動標為 Interrupted。重試內容相同不重複插入或改版次，內容不同回報 Conflict。

歷史細節保留原始快照、樣本 ticks／毫秒、操作數、設定、App／CLR／OS／機器／CPU 識別與計時頻率。案例改名、重新生成或刪除不改舊輸入。Debug 或附加偵錯器紀錄會標示，請以 Release、未附加偵錯器環境做正式比較。

錯誤的完整例外會透過診斷回呼寫入資料目錄下 `logs/UTC日期.log`；若檔案記錄不可用則退回 Trace。CPU 欄位目前取 Windows PROCESSOR_IDENTIFIER，無法取得時為 Unknown。一般成功流程不建立診斷檔；進度回呼或 logger 自身錯誤不能把已保存成功結果改成失敗。

目前尚無圖表、CSV、WinForms 或批次矩陣；這些仍是期末階段。Phase 5 已用隔離 Windows 主控台的實際 CTRL_C_EVENT 驗證 CancelKeyPress 接線及取消保存；實體鍵盤、不同組員電腦及投影彩排仍待人工驗收。

## Phase 3 排序核心

Core/Contracts 的 ISortAlgorithm 統一 Descriptor 與 Sort；三個排序實作繼承 SortAlgorithmBase，共用資料比較、交換、寫入與取消檢查。排序呼叫只修改傳入陣列，不存檔、不顯示畫面。呼叫方先從 TestCaseSnapshot.Data 取得副本，再執行排序。

每次 Sort 建立新的 SortExecutionContext：Timing 不累計操作數；Analysis 累計 long 型別的比較／交換／寫入數。context 是一次性物件，重用會拋 InvalidOperationException；同一演算法實例可用新 context 多次呼叫，不累加前次統計。

AlgorithmCatalog 在組合入口接受 `IEnumerable<Func<ISortAlgorithm>>` 註冊，GetAll 保留註冊順序，GetById 使用穩定且區分大小寫的 Id（bubble、selection、insertion）。未知 Id 拒絕執行；重複 Id、缺少描述或改變已註冊描述的 factory 都會回報錯誤。Program 已完成三種演算法註冊，UI 不依名稱判斷演算法。

SortResultValidator.CreateExpected 先複製原始資料並以 Array.Sort 產生正確答案；Validate(expected, actual) 比較長度與每個元素。BenchmarkService 已將這些操作放在計時範圍外。只檢查遞增順序不足以發現元素遺失／重複，驗證器會拒絕這些輸出。

取消使用 CancellationToken；排序入口、出口、每輪主要迴圈及資料操作 helper 都檢查取消，頻率至少符合每 1,024 次迭代的規格。取消拋 OperationCanceledException，可能留下部分排序的工作副本；原始快照保持不變，取消結果不得作正式成績。排序核心沒有計時器、背景工作或資料庫相依。

## Phase 2 案例操作

啟動後選「1 案例管理」，再使用下列編號：

| 編號 | 操作 |
| --- | --- |
| 1 | 列出案例，顯示序號、ID、名稱、筆數、分布、版次及本機更新時間 |
| 2 | 以名稱關鍵字及可選分布搜尋；關鍵字空白表示全部 |
| 3 | 依清單序號檢視；預覽最多 20 筆，另顯示總數、種子及範圍 |
| 4 | 新增：名稱→自動／手動→預覽→Y 才保存 |
| 5 | 修改：選案例→只改名稱或重新生成／替換資料→預覽→Y 確認 |
| 6 | 刪除：選案例並確認；舊歷史輸入與樣本保留 |
| 0 | 返回主選單；主選單 0 結束 |

自動模式支援 Random、Sorted、Reversed、FewUnique，N 為 1～5000；生成範圍 -1000000～1000000，種子空白時選定一次並顯示／保存。相同參數與種子在同一執行環境可重現資料。

手動模式接受半形逗號、空白與多行整數，輸入空白行結束。允許 Int32 極值及重複值，拒絕 `1,,2`、尾逗號、小數或溢位。輸入 `:q` 取消目前操作；沒有確認保存、輸入結束或錯誤輸入都不寫入候選案例。EOF 正常退出，沒有未完成輸入迴圈。

「只改名稱」保留陣列、分布及種子；只有明確選替換資料才改變輸入。每次成功修改版次加一；舊版次操作會回報衝突。名稱允許重複，實際操作以 GUID 辨識。所有儲存由共用 JsonDataStore 原子提交。

Main 只負責建立 Store、Repository、Service 與選單；Core 領域物件不引用 JSON 或主控台。清單對使用端只回傳摘要；目前 JSON 仍需整份讀取與驗證，並非資料庫式分頁。

## Phase 1 操作及資料保存

直接啟動 ConsoleApp1.exe 會使用 `%LOCALAPPDATA%\AlgorithmLab\store.json`；首次建立空文件，重開驗證並讀取原文件。可用 `--data-path 完整路徑` 指定獨立資料檔供展示或測試，不修改預設個人資料。

有錯誤時顯示原因；可按 R 重試。只有有效備份才顯示其版次／UTC 時間及 B 復原選項；B 是明確復原確認，可能回到較舊資料。損毀主檔會另存 `.corrupt.*`，備份不被損毀內容取代。無輸入或選擇結束時退出碼為 1。

`AlgorithmLab.Data.FileStorage.JsonDataStore` 提供 Initialize、Load、Commit、InspectBackup、RestoreBackup。Commit 接受只修改候選文件的 callback；不得在 callback 內重入 Store 或執行其他 I/O。Load／Commit 的回傳值均為獨立副本，不能藉改動回傳 DTO 修改正式快取。案例與 Run Repository 已共用同一 Store。

DTO 只存在 Data/FileStorage；Core 已有案例、草稿、摘要、快照與 ITestCaseRepository，Data/Repositories 負責 DTO 對應。文件版本仍為 1，完整必要欄位、快照 Hash、UTF-8、20 MiB 與物件圖上限都會驗證。每次提交先寫暫存、flush、讀回驗證，再替換主檔並保留上一版；正式測量不在 Commit 內執行。

Store 生命週期持有單實例 Mutex；建立與 Dispose 必須在同一個執行緒，讀寫操作可由背景執行緒呼叫，由內部鎖序列化。應以 using 釋放。測試只使用獨立 TEMP 目錄，故障注入元件為 internal，沒有產品用的「模擬成功／失敗」開關。

## 專案邊界

| 專案 | 責任 | 專案參考 |
| --- | --- | --- |
| ConsoleApp1 | 主控台與依賴組合入口 | Core、Data |
| AlgorithmLab.Core | 模型、排序、服務及 Repository 介面 | 無 |
| AlgorithmLab.Data | 期中 JSON、期末 SQLite 儲存實作 | Core |
| AlgorithmLab.Tests | 行為驗證入口 | Core、Data |

期中不用 SQLite 或任何 NuGet 套件。四專案採傳統 csproj，新增 C# 檔案必須納入 Compile；專案參考與方案變更由角色 A 整合。兩份方案必須同步更新。

## 協作入口

- [開發計畫書](開發計畫書.md)：完整範圍與驗收規則。
- [共用契約](docs/contracts.md)：Phase 0 凍結的名稱、資料型別與操作語意。
- [分工與交接](docs/division-of-work.md)：A～D 責任及文件所有權。
- [需求追蹤](docs/requirements-traceability.md)：需求對應階段與驗收。
- [架構圖](docs/class-diagram.md)：現有專案與後續型別關係。
- [驗證紀錄](docs/test-results.md)：實際驗證證據與尚未執行項目。
- [環境檢查報告](環境檢查報告.md)：Windows 工具及 PATHEXT 注意事項。

四人以同一 repository 及本機副本協作；尚未設定遠端位址時，不假設已有 GitHub 專案，也不由程式自動推送。每次交接附建置結果、異動範圍及剩餘工作。`.vs`、bin、obj 與個人資料不提交，測試範例集中在 Tests/Fixtures。
