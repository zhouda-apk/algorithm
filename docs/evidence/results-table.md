# 本機實測摘要

來源：demo-store.json；三個 Completed Run，皆為同一輸入、Random／N=1000／seed=42／Release x64。時間是各 Run 的五個正式樣本中位數。

| Algorithm | Median ms | Comparisons | Swaps | Writes |
| --- | --- | --- | --- | --- |
| bubble | 3.655100 | 499364 | 242785 | 485570 |
| selection | 2.031700 | 499500 | 991 | 1982 |
| insertion | 1.341200 | 243777 | 0 | 243784 |

完整環境、timer ticks、Frequency、Hash 與版本保留於原始 JSON；不可直接將此單一案例的排行推廣至所有資料。
