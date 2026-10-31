# 期中封版驗收與簽核

2026-10-30，RC1。工程交付及本機驗證已備；**跨機／另一帳號驗收尚未完成，因此不是所有人機環境都已通過的正式簽核版本**。

| 項目 | 狀態 | 證據／下一步 |
| --- | --- | --- |
| Windows Release x64 建置 | 通過 | build.ps1、本機 MSBuild；測試紀錄 |
| 71 項自動測試 | 通過 | artifacts/validation/automated-tests.txt |
| CRUD／重開／三種排序／歷史 | 通過 | docs/evidence/demo-transcript.txt、demo-store.json |
| 原始快照／樣本／環境保存 | 通過 | 三個 Completed Run，InputHash 相同、每個五個正式樣本 |
| 取消與失敗不報假成功 | 通過 | 單元測試及隔離 Windows Ctrl+C 事件，Cancelled／USER_CANCELLED／null 成績 |
| 乾淨解壓目錄與中文／空白路徑 | 本機通過 | 複製 Release 後的新資料完整流程；同一台 Windows／同一帳號，不能當作跨機 |
| 三張視窗草圖、PDF、類別圖 | 已產出 | docs/wireframes，圖內標示控制項／事件與規劃用途 |
| 簡報、示範腳本、資料格式說明 | 已產出 | docs/presentation、demo-script.md、data-format-and-recovery.md |
| 原始碼／Runtime／驗證證據封版包 | 已產出候選包 | artifacts/AlgorithmLab-Midterm-2026-10-30-rc1.zip、manifest.json、SHA256 |
| 另一台實體電腦或另一帳號 | **待組員驗收** | 下表填寫實際資料，不使用本機複製目錄冒充 |
| 上台電腦、實體鍵盤 Ctrl+C 與投影彩排 | **待人工確認** | 確認終端沒有攔截 Ctrl+C、中文顯示、資料路徑及時間預算 |
| 組員姓名、班級、學號、課程上傳方式 | **待組員填妥** | 簡報第 1 頁有可編輯欄位；不虛構身分或代為提交 |

## 另一環境的驗收紀錄（未執行）

| 欄位 | 實際填寫 |
| --- | --- |
| 驗收者／角色 | 待填 |
| 日期、電腦／帳號識別 | 待填，不填密碼或憑證 |
| Windows／.NET Framework | 待填 |
| 解壓後 runtime/Start-Demo.cmd 可啟動 | 待測 |
| 可新增／修改／刪除案例 | 待測 |
| 關閉重開後資料仍存在 | 待測 |
| 三種排序均通過並保存五個正式樣本 | 待測 |
| 刪除原案例後歷史仍可查 | 待測 |
| Ctrl+C 後正常返回且狀態正確 | 待測，小案例若已完成須重新選大案例 |
| 中文／投影／草圖及 PDF 顯示 | 待測 |
| 結論、錯誤與處置 | 待填 |

上述人工項目完成後，將 RC1 複製為正式提交版本或更新候選版號；不要在未知結果前勾選通過。此檔是交接簽核清單，不是已取得其他電腦測試結果的證明。
