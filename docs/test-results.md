# 開發驗證紀錄

## 2026-10-30：Phase 5（最新狀態，RC1）

產品來源沿用 Phase 4；Release x64 再次建置成功，完整 Runner 再跑 **71 passed、0 failed、退出碼 0**。日誌：artifacts/validation/automated-tests.txt。以下整合驗證全部使用獨立路徑，不讀寫預設個人資料。

| 項目 | 實際結果／證據 |
| --- | --- |
| Windows 原生取消事件 | ConsoleCancelProbe 配置自己的 console，在子程式 Warmup 階段送出 CTRL_C_EVENT；子程式保存 Cancelled／USER_CANCELLED、median=null，正常退出。artifacts/validation/console-cancel-* |
| 全新 Release 複製目錄 | 中文／空白路徑、建立及改名案例、退出重開、三種排序、刪除原案例、錯誤缺值輸入、歷史查詢；正常退出 |
| 真實成果資料 | docs/evidence/demo-store.json：同一 Random1000／seed42／InputHash，三個 Completed Run，每個五個樣本；刪除案例後 testCaseId=null，快照保留 |
| 完整操作證據 | docs/evidence/demo-transcript.txt、results-table.md；不是手填的時間數據 |
| UI 與展示素材 | 三張草圖 PNG、三頁 PDF、類別圖 PNG、八頁 PPTX／PDF；目視檢查草圖及結果表；圖內區別規劃／實測 |
| RC1 完整性及解壓後試跑 | verify-midterm-package.ps1 核對 ZIP 與逐檔 SHA-256、必要文件及排除項；新 TEMP 建立三案例與一個 Completed／五樣本 Run、重開歷史及備份。實際結果寫入 artifacts/validation/package-verification.txt；腳本失敗時不能視為通過 |

Windows 原生事件證據涵蓋產品的 CancelKeyPress 路徑，與純 CancellationToken 注入不同；仍不能取代上台終端／實體鍵盤驗收。新目錄及 ZIP 試跑均是**同機同帳號**；E-03 另一台 Windows／另一帳號、磁碟滿載／實際 ACL 拒絕、投影彩排仍未執行。Phase 5 外部簽核請看 acceptance-checklist.md。

封裝來源是目前工作區快照，未建立 commit 或上傳。validation 目錄的 ZIP 驗證紀錄產生於封裝之後，保存於 ZIP 外部；ZIP 內 validation 則是封裝當時的證據快照。

## 2026-10-30：Phase 4

Windows .NET Framework 4.7.2、C# 7.3、x64。Release Runner：**71 passed、0 failed、退出碼 0**（儲存 20＋案例／主控台 15＋排序 16＋Benchmark 20）。Debug／Release slnx、Release sln 建置成功。

新增 20 項驗證包括：奇偶中位數、母體標準差、零 ticks／指定 Frequency 與錯誤樣本；環境指紋；Running 先保存；七次排序呼叫的模式與原始副本一致；一次獨立 Analysis 計數；重開讀取；過期案例／未知演算法拒絕；初始保存失敗不排序；預先取消、兩個樣本後取消、實際 10 秒合作式逾時；預熱／正式樣本／Analysis 三種答案錯誤；未預期例外診斷；最終保存前取消；保存失敗與原結果重試、不重複且不重跑排序；遺留 Running 處理；執行期間刪除案例仍保留歷史快照；篩選／23 筆分頁；服務重入拒絕及進度回呼錯誤隔離；三種演算法跨主控台行程測量與刪除後歷史；展示案例及未確認不建立 Run。

成功 Run 實測只有五個正式樣本，不包含預熱與 Analysis，操作數為一次 Analysis，並以已保存 JSON 重開查詢。錯誤樣本可作診斷但不產生 Completed 聚合成績。保存失敗後重試相同 RunId 不再呼叫 Sort；相同最終內容重試後整個檔案 bytes 不變，不增加文件版次。

所有檔案測試仍使用獨立 TEMP 目錄並清理，不讀寫個人正式 JSON。真實鍵盤 Ctrl+C、不同組員電腦、長時間展示負載與介面草圖／簡報仍列 Phase 5 人工驗收；本階段自動測試透過 CancellationToken 驗證取消及實際期限驗證逾時，不將程式事件接線等同已完成鍵盤人工測試。

## 2026-10-30：Phase 3

Windows .NET Framework 4.7.2、C# 7.3、x64。Release Runner：**51 passed、0 failed、退出碼 0**（20 項儲存＋15 項案例／主控台＋16 項排序）。Debug／Release slnx、Release sln 建置通過。

新增 16 項排序測試涵蓋：Catalog 描述與註冊順序／factory；未知、重複或不一致註冊拒絕；null 與 mode 驗證；空陣列／單值；已排序、反向、相同值、重複、負數及 Int32 極值；metrics-1 的兩組指定次數；Bubble 提前結束、反向交換數及 Selection 固定比較公式；多種種子與規模的獨立 Array.Sort 答案；5,000 筆資料；元素遺失／替換／重複拒絕；快照與正確答案副本隔離；輔助陣列寫入及同索引交換；預先取消；實際排序有進展後取消；context 隔離／重用拒絕；64 位元計數。

執行中取消測試在獨立背景執行緒執行各實際排序、使用 50,000 筆反向工作副本，觀察到元素改變後才要求取消；Timing／Analysis 六種組合均驗證拋 OperationCanceledException，且在等待上限內結束，不使用 Thread.Abort。50,000 筆僅用於核心取消測試，不改變期中案例上限 5,000。

核心演算法不引用 Console、Data 或 JSON，不在 Sort 呼叫中執行 I/O。測試原始資料與工作副本分開；測試資料不寫到個人正式檔案。正式時間與 Run 保存尚未實作，這些驗證不代表效能實驗已完成。

## 2026-10-30：Phase 2

Windows .NET Framework 4.7.2、C# 7.3、x64。Release Runner：**35 passed、0 failed、退出碼 0**（20 項儲存＋15 項案例／主控台）。Debug／Release slnx、Release sln 均建置成功。

新增驗證包括：混合分隔手動輸入與 Int32 極值；缺值／溢位拒絕；固定及自選種子重現；Sorted／Reversed 同數值多重集合；四分布上下界與最大 5000 筆；陣列防禦性副本；無效候選不寫檔；CRUD 重開與重名；版次衝突；明確替換資料與快照不變；重複 Id／建立時間竄改拒絕；案例更新刪除保留歷史；更新／刪除儲存失敗整批回復。

主控台測試實際啟動獨立 ConsoleApp1.exe 行程並傳入鍵盤輸入，驗證中文案例的新增、搜尋、檢視、只改名稱、取消及刪除；另驗證自動生成、指定種子、明確替換為手動資料、自選種子預覽後取消，以及錯誤輸入／EOF 不保存草稿。各行程透過 --data-path 使用獨立 TEMP 資料，結束後釋放並清理，未讀寫預設個人 JSON。

歷史保留測試使用明確測試用 Run／樣本，不代表正式 Benchmark 已實作。上述操作與 UTF-8 中文主控台往返均已實測；產品選單仍不提供排序或歷史測試執行功能。

## 2026-10-30：Phase 1

Windows、.NET Framework 4.7.2、C# 7.3、x64。Phase 0 下方紀錄為當時歷史狀態；目前 Runner 已註冊實際儲存測試。

| 驗證 | 結果 |
| --- | --- |
| slnx Debug／Release x64 | 建置成功，退出碼 0 |
| 傳統 sln Release x64 | 建置成功，退出碼 0 |
| Release 儲存測試 | 20 passed、0 failed，Runner 退出碼 0 |
| 主控台整合檢查 | 首次初始化、重開不重設、未知版本拒絕且原檔不變、B 確認復原成功；成功退出碼 0，拒絕退出碼 1 |
| 測試資料隔離 | 全部使用獨立 TEMP 目錄並清理；未讀寫預設個人 store.json |

20 項測試範圍：初始化／重開；案例與完整 Run／樣本及 64 位元計數往返；前版備份；提交前失敗；提交後觀察到錯誤時核對落盤結果；callback／驗證失敗；重複 Id／壞 Hash／孤立參照；錯誤樣本／狀態；損毀保留與明確復原；主檔遺失但有備份；未知版本／缺必要欄位；20 MiB 讀寫限制；外部修改衝突；遺留暫存；同程式與跨行程 Mutex；Dispose 後拒絕操作；尾隨 JSON／非 UTF-8；重入 callback；2,000,000 物件圖限制；實際 Windows sharing violation。

故障注入只透過 Tests 可見的 internal IFileCommitter 測試邊界，不提供正式產品故障切換功能。損毀主檔復原時先保留 `.corrupt.*`，不以壞檔覆蓋有效備份。JSON 讀取器之外另驗證只有一個完整根物件，避免內建讀取器忽略尾隨內容。

Phase 1 通過的是儲存基礎；不代表案例服務、Repository、排序或 Benchmark 功能已完成。其他組員電腦、磁碟滿載／ACL 等更多人工環境條件仍需在期中整合驗收時確認。

## 2026-10-30：Phase 0

環境：Windows 上的 Visual Studio Community 2026，MSBuild 18.10.1.42706，.NET Framework 4.7.2 參考組件；C# 7.3；由 WSL 呼叫 Windows PowerShell／MSBuild。

| 項目 | 結果 | 證據 |
| --- | --- | --- |
| slnx／Debug x64 | 通過 | scripts/build.ps1 -Configuration Debug，退出碼 0，四專案輸出完整 |
| slnx／Release x64 | 通過 | scripts/build.ps1 -Configuration Release，退出碼 0 |
| 傳統 sln／Release x64 | 通過 | 同腳本加 -SolutionFormat sln，退出碼 0 |
| 輸出架構 | 通過 | Debug／Release 的四個 DLL／EXE 共八份，PE Machine 皆為 0x8664 |
| 主控台啟動 | 通過 | Release ConsoleApp1.exe 輸出 Phase 0 提示，退出碼 0 |
| 測試骨架啟動 | 符合規格 | Release AlgorithmLab.Tests.exe 明確顯示尚無產品測試，退出碼 2 |
| 專案依賴與 Compile 清單 | 通過 | Console→Core／Data，Data→Core，Tests→Core／Data，Core 無專案依賴；每個 Compile 檔案存在；四專案指定 net472 與 C# 7.3 |
| 忽略規則 | 通過 | .vs、bin、obj、個人 store.json 被忽略；Tests/Fixtures/store.json 可追蹤 |
| 已追蹤產物清理 | 完成 | 七個 .vs／obj 檔案取消 Git 追蹤，檔案仍留在工作目錄 |

SLNX 已明確將 C# 專案平台映射到 x64，避免使用其預設 AnyCPU 對應。格式依據：[Microsoft SolutionPersistence 維護者說明](https://github.com/microsoft/vs-solutionpersistence/issues/115)。兩份方案的專案與配置需同步維護。

本次沒有安裝 NuGet／SQLite，沒有變更永久 Windows 環境變數，沒有建立使用者正式 store.json，也沒有提交或推送 Git commit。既有使用者變更保留。

## 目前尚未驗證的範圍

- 另一台 Windows／另一帳號的 Release 保存、重讀與備份，以及其他組員的來源建置。
- 上台終端實體鍵盤 Ctrl+C、投影及完整口頭彩排。
- 真實磁碟滿載／ACL 拒絕；不將故障注入與 sharing violation 等同這兩項。
- SQLite 遷移、實際 WinForms、進階排序及圖表：期末 Phase 6 起，尚未實作。

各 Phase 下的敘述是當時的歷史結果；最新狀態以頁首及封版簽核清單為準。未註冊測試時仍須退出碼 2，禁止以空集合回報成功。
