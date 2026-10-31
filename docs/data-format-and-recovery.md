# 本機資料格式、備份與復原

## 資料位置

一般啟動：`%LOCALAPPDATA%\AlgorithmLab\store.json`。
封版包的 Start-Demo.cmd：`%LOCALAPPDATA%\AlgorithmLab-MidtermDemo\store.json`。
可透過 `ConsoleApp1.exe --data-path 完整路徑` 選擇其他獨立檔案。路徑顯示在啟動畫面，與目前工作目錄無關。

## 文件內容

| 根欄位 | 內容 |
| --- | --- |
| fileFormatVersion | 固定格式版本 1，未知版本拒絕寫入 |
| documentRevision | 每次成功完整提交加一，與單一案例 Revision 分開 |
| savedAtUtc | UTC、round-trip ISO 8601 字串 |
| testCases | 當前案例、實際整數陣列、分布、種子、生成版本、範圍與版次 |
| benchmarkRuns | 測試輸入快照、演算法／環境／設定、狀態、正式樣本及統計 |

UTF-8、不含 BOM，最大 20 MiB、物件圖最多 2,000,000 項。所有必要欄位都要存在，未取得結果為 null，不能用 0 偽裝未完成。GUID 使用 D 格式、列舉為名稱字串、計數為整數、布林為 true／false。

Run 的 inputDataText 以 invariant 整數及半形逗號組成；inputHash 是其 UTF-8 SHA-256。案例刪除後，Run.testCaseId 為 null，但 caseName、caseRevision、inputDataText、inputHash 與 samples 保留。samples 只含正式測量，不含預熱與 Analysis。

完整欄位以 contracts.md 及 FileStorage DTO 為準；本文件沒有提供可直接覆蓋個人資料的範本。不要以文字編輯器隨意刪欄位、改 Hash 或把未知版本改成 1。

## 正常保存

程式在共享鎖內建立候選副本，驗證後寫同目錄暫存檔、flush、讀回核對，最後替換主檔；store.json.bak 保存前一次成功版本。只有成功後才回報保存。單實例 Mutex 防止另一入口同時修改相同資料。

## 複製與復原

1. 備份或轉移資料前，先正常結束所有開啟該檔案的程式。
2. 複製主檔及其 .bak 到另一個備份目錄；不要只複製 exe 而以為資料也在 exe 旁。
3. 主檔無法讀取時，程式保留原檔並顯示錯誤；有效備份會顯示版次與 UTC 時間。
4. 選 R 只重試讀取，選 B 才明確復原該備份；備份可能較舊，較新的操作可能不在其中。
5. 復原先另存 `.corrupt.*` 原檔，再發布有效內容；不以壞檔覆蓋有效備份。沒有有效備份時結束程式，保留檔案供檢查，不清空重建。
6. 遺留 Running 於下次啟動轉 Interrupted，不自行重跑；保存失敗的計算結果可在當次選擇重試，不重新排序。

期末會依 SourceHash 驗證並整批匯入 SQLite；本期不做雙寫或雙向同步。當前案例／歷史格式維持版本 1。
