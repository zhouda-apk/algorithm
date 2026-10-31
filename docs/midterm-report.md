# Algorithm Lab 期中成果報告素材

2026-10-30。四人小組，姓名／班級／學號由組員於繳交前填入。

## 動機與目標

建立 C# 排序實驗工具，讓使用者保存可重現的整數案例，驗證演算法答案，觀察執行時間與操作次數。期中提供主控台原型與 WinForms 草圖，期末延續相同 Core／儲存介面擴充視窗及資料庫。

## 已完成原型

案例 CRUD；Random／Sorted／Reversed／FewUnique 與手動輸入；Bubble／Selection／Insertion；預熱、五次計時及獨立 Analysis；JSON 原子保存／備份／復原；取消／逾時／失敗狀態；歷史篩選、分頁與完整樣本。可操作細節見 README，未完成跨機簽核不等同已正式交付課程。

## OOP 與技術應用

| 概念／技術 | 實際檔案與方法 | 用途 |
| --- | --- | --- |
| 封裝、建構子及屬性 | Core/Models/TestCase.cs，建構子、Data、ToDraft | 驗證不變條件，外部取得陣列副本 |
| 自訂類別及關係 | TestCaseService、BenchmarkRun、BenchmarkService | 案例、測試結果與執行流程各負其責 |
| 介面、多型 | Core/Contracts/ISortAlgorithm.cs、BenchmarkService.Run | 同一流程執行不同排序 |
| 抽象類別、繼承 | SortAlgorithmBase、三個 SortCore | 共用比較／交換／寫入與取消檢查 |
| 儲存介面 | ITestCaseRepository／IBenchmarkRepository、兩個 JSON Repository | 期末可替換 SQLite，核心不引用 JSON |
| 型態轉換、條件 | ManualDataParser.Parse、ConsoleInputReader.Integer | Int32 解析、缺值／溢位／選項驗證 |
| 迴圈、陣列與集合 | 三種 SortCore、DataGenerator.Generate、歷史清單 | 排序、資料生成、保存及查詢 |
| 字串／日期方法 | TestCaseService.Search、UTC 時間、Invariant 格式 | 名稱查詢、時間保存與可重現格式 |
| 例外處理 | AlgorithmLabException、BenchmarkPersistenceException | 不以空資料或 0 ms 掩蓋失敗 |

依賴及類別圖見 class-diagram.md 與 wireframes/class-diagram.png。以上路徑位於對應 AlgorithmLab.Core／Data 專案，均有實際原始碼，可由組員開啟說明。

## 測量與真實證據

同一份 Random／N=1000／範圍 -10000～10000／seed=42，分別以三種排序測量。每個 Run 使用新副本、一次預熱、五個正式樣本與一次獨立計數。原案例改名為「期中展示案例」後測量，刪除後歷史仍保留。

摘要：[實測結果表](evidence/results-table.md)。完整輸入、InputHash、原始 ticks、Frequency、版本與環境：[JSON](evidence/demo-store.json)。操作逐字紀錄：[文字](evidence/demo-transcript.txt)。簡報第 6 頁由這份 JSON 產生，不使用示意時間。

結果只代表這台機器與這份輸入；五次時間的波動與單一種子的限制都應在報告說明。比較、交換與寫入為一次 Analysis，不能只根據交換次數宣稱演算法最有效率。

## 驗證與交付

71 項自動測試通過。隔離的 Windows console 已實測 CTRL_C_EVENT 後保存 Cancelled／USER_CANCELLED，沒有正式成績。複製到新的中文／空白目錄後，可建立新 JSON、重開、執行三種排序、刪除案例再查歷史。

這些證據來自同一台 Windows／帳號；另一台電腦、上台終端與真實鍵盤彩排、組員身分仍待 acceptance-checklist.md 簽核。候選包含 Runtime、原始碼、文件、圖檔、簡報及雜湊清單；不代為上傳或提交作業。

## 期末延續

先完整匯入期中 JSON 至 SQLite，再增加 WinForms、Quick／Merge、批次矩陣、圖表及 CSV。動畫只在必做完成且有餘裕時加入。草圖是期末規劃，不能列作期中已實作的視窗。
