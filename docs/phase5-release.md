# Phase 5：期中整合與 RC1 交付

2026-10-30。主控台功能維持 Phase 4 的成熟管線；本階段完成整合證據、期末視窗草圖、期中報告、展示簡報與可重建的封版流程。**RC1 是本機驗證候選版，另一環境及課堂彩排尚未簽核；不宣稱所有 Phase 5 驗收已完成。**

## 交付內容

| 路徑 | 用途 |
| --- | --- |
| artifacts/AlgorithmLab-Midterm-2026-10-30-rc1.zip | 完整候選包；解壓後執行 runtime/Start-Demo.cmd |
| 同名 .zip.sha256.txt | ZIP 傳輸完整性校驗 |
| ZIP 內 runtime | Release x64 exe、Core／Data DLL、config、展示啟動器 |
| ZIP 內 source | 四專案、兩份方案、完整原始碼、腳本、文件與素材，不含 bin／obj／.vs／個人資料 |
| ZIP 內 validation | 71 項測試、OS Ctrl+C、全新複製目錄證據 |
| ZIP 內 manifest.json | 逐檔相對路徑、大小及 SHA-256，不包含 manifest 自身 |
| docs/wireframes | 三張標示控制項／事件的 PNG、三頁 PDF、類別圖 PNG |
| docs/presentation | 八頁 PPTX／PDF 及逐頁 PNG |
| docs/demo-script.md | 5～7 分鐘示範、取消備案與彩排清單 |
| docs/data-format-and-recovery.md | JSON v1、路徑、備份與明確復原流程 |
| docs/midterm-report.md | 動機、功能、OOP、真實實測及期末延續 |
| docs/acceptance-checklist.md | 已驗證項目、跨機／課堂／身分待填欄位 |

簡報主要為高解析圖像版，第 1 頁有可編輯的姓名／班級／學號欄位；其他排版變更請修改產生腳本後重建。產生器使用 Windows Microsoft JhengHei、Pillow、reportlab 與 python-pptx，**僅用於文件**，不是主程式的執行相依。已輸出 PDF／PNG，可不安裝 Python 直接閱讀。

## 重建與驗證

在 Windows PowerShell、repository 根目錄執行：

```powershell
powershell.exe -NoProfile -ExecutionPolicy Bypass -File .\scripts\build.ps1 -Configuration Release
.\AlgorithmLab.Tests\bin\x64\Release\AlgorithmLab.Tests.exe
powershell.exe -NoProfile -ExecutionPolicy Bypass -File .\scripts\verify-console-cancel.ps1 -Executable .\ConsoleApp1\bin\x64\Release\ConsoleApp1.exe -EvidenceDirectory .\artifacts\validation
```

Runner 應為 71 passed、0 failed；需要保存測試日誌時將完整 stdout 另存 `artifacts/validation/automated-tests.txt`。全新目錄整合摘要見 `fresh-directory-summary.txt`，可依示範腳本在獨立 `--data-path` 重做。取消探針會自行配置隔離主控台，不向使用者目前的 console 廣播事件。

具備驗證證據及文件後封裝；既有版號不覆寫，後續重新交付應使用新的 ReleaseId：

```powershell
powershell.exe -NoProfile -ExecutionPolicy Bypass -File .\scripts\package-midterm.ps1
powershell.exe -NoProfile -ExecutionPolicy Bypass -File .\scripts\verify-midterm-package.ps1 -ZipPath .\artifacts\AlgorithmLab-Midterm-2026-10-30-rc1.zip -EvidenceDirectory .\artifacts\validation
```

package 預設先建置 Release；`-SkipBuild` 只供剛完成建置且產品來源未變動時使用。產生器不自動判定舊日誌是否適用；若修改產品來源，須重跑測試、取消與整合證據後再建立新版。`-ReleaseId AlgorithmLab-Midterm-2026-10-31-rc2` 可保留舊候選版，但使用者須同步文件中的候選路徑。

verify 會核對 ZIP 雜湊及每個 manifest 項目，解壓至本機 TEMP 中文／空白路徑，建立全新資料、保存一個 Completed／五樣本 Run、重開查歷史及確認備份；成功後清理 TEMP，輸出 package-verification.txt 與操作逐字紀錄。這份解壓後驗證在封包建立後產生，保存在外部 artifacts/validation，ZIP 中的 validation 是封裝當下快照。

若需重建圖檔，使用已安裝上述套件的 Windows Python 執行 `python scripts/render-midterm-assets.py`。結果頁僅讀取明確生成的 `docs/evidence/demo-store.json`，不讀取個人資料；該 JSON 保存三個真實 Run，不是可任意改數字的示意表。

## 驗證邊界與交接

本機已重跑 71 項測試、三種排序同輸入測量、CRUD／重開／刪除後歷史、隔離 Windows Ctrl+C 事件。Release 複製到全新目錄的測試來自**同一台機器與帳號**。磁碟滿載／實際 ACL 拒絕、第二台 Windows／另一帳號、上台終端 Ctrl+C 與投影仍需人工驗收，不能用既有故障注入或複製目錄替代。

組員應依 acceptance-checklist.md 完成另一環境簽核、填入身分並彩排，再依課程方式提交。未建立 commit、tag、遠端倉庫或代為上傳。期末工作從 Phase 6 的 SQLite 遷移開始；期中目前仍以 JSON 保存。
