"""Optional renderer: Pillow, reportlab, python-pptx and Windows JhengHei; no app dependency."""
from pathlib import Path
import json
from PIL import Image, ImageDraw, ImageFont
from pptx import Presentation
from pptx.util import Inches, Pt
from pptx.dml.color import RGBColor
from reportlab.pdfgen import canvas
from reportlab.lib.utils import ImageReader

ROOT = Path(__file__).resolve().parents[1]
OUT = ROOT / 'docs' / 'wireframes'
SLIDES = ROOT / 'docs' / 'presentation'
OUT.mkdir(parents=True, exist_ok=True)
SLIDES.mkdir(parents=True, exist_ok=True)
FONT = 'C:/Windows/Fonts/msjh.ttc'
BOLD = 'C:/Windows/Fonts/msjhbd.ttc'
NAVY, BLUE, GREY = '#172b4d', '#235ad1', '#667085'


def font(size, bold=False):
    return ImageFont.truetype(BOLD if bold and Path(BOLD).exists() else FONT, size)


def words(draw, value, xy, size=24, color=NAVY, width=1300, bold=False):
    x, y = xy
    line = ''
    for char in value:
        if char == '\n' or draw.textlength(line + char, font=font(size, bold)) > width:
            draw.text((x, y), line, font=font(size, bold), fill=color)
            line = '' if char == '\n' else char
            y += size + 12
        else:
            line += char
    draw.text((x, y), line, font=font(size, bold), fill=color)
    return y + size + 12


def panel(draw, box, title=None):
    draw.rounded_rectangle(box, radius=12, fill='white', outline='#cbd5e1', width=2)
    if title:
        words(draw, title, (box[0] + 20, box[1] + 15), 25, bold=True)


def control(draw, box, label, active=False):
    draw.rounded_rectangle(box, radius=7, fill=BLUE if active else '#f8fafc', outline='#aab6ca', width=2)
    words(draw, label, (box[0] + 12, box[1] + 9), 22, 'white' if active else NAVY, box[2] - box[0] - 20)


def wireframe(kind):
    image = Image.new('RGB', (1600, 1000), '#eef2f7')
    draw = ImageDraw.Draw(image)
    titles = {'main': 'MainForm｜案例管理', 'editor': 'CaseEditorDialog｜新增／編輯案例', 'benchmark': 'BenchmarkControl｜執行測試頁'}
    words(draw, titles[kind], (45, 28), 38, bold=True)
    words(draw, '期末 WinForms 規劃草圖｜示意資料，並非已完成視窗或實測數據', (45, 87), 23, GREY)
    panel(draw, (35, 145, 1135, 910))
    draw.rectangle((36, 146, 1134, 201), fill=NAVY)
    words(draw, 'Algorithm Lab', (58, 155), 27, 'white', bold=True)
    panel(draw, (1160, 145, 1565, 910), '控制項與事件')
    if kind == 'main':
        for x, label in [(60, '案例管理'), (280, '執行測試'), (500, '歷史紀錄'), (720, '比較分析')]:
            control(draw, (x, 225, x + 195, 277), label, x == 60)
        words(draw, '名稱搜尋', (65, 310), 23)
        control(draw, (190, 302, 560, 352), 'txtKeyword')
        control(draw, (580, 302, 800, 352), '分布：全部 ▼')
        control(draw, (825, 302, 1050, 352), '查詢 btnSearch', True)
        draw.rectangle((65, 385, 1090, 625), outline='#aab6ca', width=2)
        draw.rectangle((66, 386, 1089, 435), fill='#e3eafa')
        words(draw, '名稱                  筆數        分布          版次       更新時間', (82, 397), 24)
        for i, line in enumerate(['示例案例 A          1000        Random          1          示意', '示例案例 B          1000        Reversed         2          示意']):
            words(draw, line, (82, 457 + i * 62), 23)
        for x, label in [(65, '新增'), (280, '修改'), (495, '刪除'), (710, '使用此案例測試')]:
            control(draw, (x, 660, x + 195, 716), label, x == 65)
        words(draw, '選取資料預覽：3, -1, 8, …（前 20 筆／共 1000 筆）', (65, 755), 24)
        words(draw, '狀態：就緒　｜　儲存位置：由系統設定取得', (65, 845), 22, GREY)
        notes = ['btnSearch.Click\n查詢並刷新摘要', 'dgvTestCases.SelectionChanged\n載入選取案例／無選取停用編輯', 'btnAdd／btnEdit.Click\n開啟案例對話框', 'btnDelete.Click\n確認後刪除，保留歷史', 'btnUseCase.Click\n切換測試頁並核對版次', 'MainForm.FormClosing\n取消工作、等待保存結束']
    elif kind == 'editor':
        for y, title, box, value in [(245, '案例名稱', (230, 235, 1050, 288), 'txtName：示例案例'), (330, '資料來源', (230, 320, 590, 373), 'cmbInputMode：自動 ▼'), (415, '分布', (230, 405, 590, 458), 'cmbDistribution：Random ▼')]:
            words(draw, title, (75, y), 25)
            control(draw, box, value)
        for x, label in [(75, '數量：1000'), (340, '最小：-10000'), (605, '最大：10000'), (870, '種子：42')]:
            control(draw, (x, 495, x + 235, 548), label)
        control(draw, (75, 578, 425, 632), '產生／重新產生預覽', True)
        panel(draw, (75, 660, 1070, 765))
        words(draw, '手動輸入區／資料預覽（依來源切換）', (95, 677), 24)
        words(draw, '3, -1, 8, …　實際數量：1000（示意）', (95, 718), 22, GREY)
        words(draw, 'ErrorProvider：顯示欄位驗證錯誤', (75, 792), 23, '#ae402c')
        control(draw, (680, 837, 865, 889), '儲存 btnSave', True)
        control(draw, (885, 837, 1070, 889), '取消 btnCancel')
        notes = ['cmbInputMode.SelectedIndexChanged\n切換自動／手動欄位', 'cmbDistribution.SelectedIndexChanged\n更新說明，不隱含重新生成', 'btnGeneratePreview.Click\n僅生成預覽，尚未保存', '輸入欄位.Validating\nErrorProvider 顯示錯誤', 'btnSave.Click\n核對預覽參數後才保存', 'btnCancel.Click\n放棄未保存資料']
    else:
        control(draw, (70, 245, 560, 300), '案例：示例案例 A ▼')
        control(draw, (590, 245, 1080, 300), '演算法：Bubble Sort ▼')
        words(draw, '版次：1　N：1000　輸入雜湊：開始前由快照取得', (70, 335), 24)
        words(draw, '預熱：1 次　正式：5 次　上限：30 秒', (70, 390), 26)
        control(draw, (70, 460, 380, 520), '開始 btnRun', True)
        control(draw, (415, 460, 725, 520), '取消 btnCancel')
        control(draw, (70, 560, 1080, 620), '階段與進度：完成 3／5 個正式樣本（示意）')
        words(draw, '排序驗證：—　中位數：— ms', (70, 670), 28)
        words(draw, '比較：—　交換：—　寫入：—', (70, 725), 26)
        words(draw, '原始／結果前 20 筆資料預覽', (70, 785), 24)
        words(draw, '狀態：就緒／預熱／測量／分析／保存／完成', (70, 850), 23, GREY)
        notes = ['btnRun.Click\n驗證後啟動背景工作', 'btnCancel.Click\n只提出取消要求', 'IProgress 更新\n安全派送到 UI 執行緒', '工作期間停用開始與輸入\n避免重複執行', '結束處恢復控制項\n取消／錯誤也必須恢復', 'Benchmark 期間不播放動畫\n畫面更新放在計時外']
    y = 215
    for item in notes:
        y = words(draw, item, (1180, y), 20, width=360) + 23
    words(draw, '期中繳交草圖；期末才實作視窗。', (45, 945), 24, GREY)
    image.save(OUT / (kind + '.png'))
    return image


def class_figure():
    image = Image.new('RGB', (1600, 900), '#f4f7fb')
    d = ImageDraw.Draw(image)
    words(d, '核心類別與物件關係', (45, 30), 40, bold=True)
    boxes = [((45, 130, 520, 355), 'TestCase｜封裝', 'Id／Name／Revision／Data\n建構時驗證不變條件\nData 回傳副本；ToDraft(name)'),
             ((45, 410, 520, 635), 'TestCaseService', 'Create／Search／Update／Delete\nCreateSnapshot(id, revision)\n依賴 ITestCaseRepository'),
             ((45, 690, 520, 865), 'JsonTestCaseRepository', '實作 ITestCaseRepository\n與測試 Repository 共用 JsonDataStore'),
             ((615, 130, 1545, 310), 'ISortAlgorithm｜介面', 'Descriptor\nSort(int[] data, SortExecutionContext context)'),
             ((615, 385, 1545, 565), 'SortAlgorithmBase｜抽象類別', '共用 Sort 入口、比較／交換／寫入 helper\nSortCore 交由子類別實作；context 保存計數與取消 token'),
             ((615, 645, 1545, 835), 'BubbleSort／SelectionSort／InsertionSort', '繼承基底類別並實作排序策略\nBenchmarkService 透過 ISortAlgorithm 執行：多型')]
    for box, title, body in boxes:
        panel(d, box, title)
        words(d, body, (box[0] + 20, box[1] + 65), 24, width=box[2] - box[0] - 40)
    for x, y1, y2 in [(280, 355, 410), (280, 635, 690), (1080, 310, 385), (1080, 565, 645)]:
        d.line((x, y1, x, y2 - 12), fill=BLUE, width=4)
        d.polygon([(x - 9, y2 - 18), (x + 9, y2 - 18), (x, y2 - 3)], fill=BLUE)
    image.save(OUT / 'class-diagram.png')
    return image


def save_pdf(images, path, title):
    w, h = images[0].size
    pdf = canvas.Canvas(str(path), pagesize=(960, 960 * h / w))
    pdf.setTitle(title)
    for image in images:
        pdf.setPageSize((960, 960 * image.height / image.width))
        pdf.drawImage(ImageReader(image), 0, 0, width=960, height=960 * image.height / image.width)
        pdf.showPage()
    pdf.save()


def slide(index, title, body, picture=None, results=None):
    im = Image.new('RGB', (1600, 900), 'white'); d = ImageDraw.Draw(im)
    d.rectangle((0, 0, 1600, 15), fill=BLUE)
    words(d, 'ALGORITHM LAB  /  期中進度報告', (55, 45), 22, BLUE)
    words(d, title, (55, 105), 47, bold=True)
    y = 205
    if results:
        columns = [70, 460, 780, 1040, 1300]
        for x, heading in zip(columns, ['演算法', '中位數 ms', '比較次數', '交換次數', '寫入次數']):
            words(d, heading, (x, 240), 26, BLUE, bold=True, width=270)
        for row_index, row in enumerate(results):
            values = [row['algorithmId'], f"{row['medianMs']:.6f}", str(row['comparisonCount']), str(row['swapCount']), str(row['writeCount'])]
            for x, value in zip(columns, values):
                words(d, value, (x, 310 + row_index * 90), 31, width=280)
        y = 620
    if picture:
        thumb = picture.copy(); thumb.thumbnail((1450, 535))
        im.paste(thumb, ((1600 - thumb.width) // 2, 205))
        y = 760
    for line in body:
        y = words(d, '• ' + line, (70, y), 30 if not picture and not results else 23, width=1450) + 22
    words(d, f'2026-10-30｜期中主控台 + JSON｜{index:02d} / 08', (55, 850), 21, GREY)
    im.save(SLIDES / f'slide-{index:02d}.png')
    return im


def main():
    images = [wireframe(x) for x in ['main', 'editor', 'benchmark']]
    save_pdf(images, OUT / 'winforms-wireframes.pdf', 'WinForms planning wireframes')
    classes = class_figure()
    data = json.loads((ROOT / 'docs/evidence/demo-store.json').read_text(encoding='utf-8'))
    rows = sorted(data['benchmarkRuns'], key=lambda r: ['bubble', 'selection', 'insertion'].index(r['algorithmId']))
    assert len(rows) == 3 and len({r['inputHash'] for r in rows}) == 1
    pages = [
        slide(1, '演算法效能比較與視覺化平台', ['以 C# 物件導向設計，建立可保存與重現的排序實驗。', '四人小組：A 整合／B 儲存／C 演算法／D 驗證與文件', '組員姓名、班級與學號請在上台前填入。']),
        slide(2, '從排序原理走到可操作實驗', ['觀察資料規模與分布如何影響時間及操作次數。', '期中：主控台、案例 CRUD、三種排序、JSON 與歷史。', '期末：WinForms、SQLite 遷移、進階排序、批次及圖表。', '排序動畫保留為時間充裕時的選配。']),
        slide(3, '兩項完整核心流程', ['案例管理：生成／手動輸入 → 預覽 → 確認保存。', '排序測試：選案例與演算法 → 預熱／測量／分析 → 保存。', '可重開查詢；刪除原案例仍保留測試快照。', '錯誤輸入、取消及保存失敗都有明確處理。']),
        slide(4, '封裝、介面、繼承與多型', ['Core 不依賴 UI 或 JSON；資料存取實作可在期末替換。'], picture=classes),
        slide(5, '公平且可追溯的測量方式', ['同一份原始資料，每次排序都使用新副本。', '1 次預熱 + 5 次正式 Timing + 1 次獨立 Analysis。', '排序之外的複製、驗證、畫面與 I/O 不計入時間。', '結果逐項比對；報告中位數並保留全部樣本。', '操作數：比較、交換、寫入，採一致的 metrics-1 規則。']),
        slide(6, '同一輸入的三種排序：本機實測', [f"N=1000，Random，seed=42；同一 InputHash：{rows[0]['inputHash'][:24]}…", 'Release x64；五次正式時間中位數；數值來源 docs/evidence/demo-store.json。', '這是本機特定資料的結果，不能代表所有機器或分布。'], results=rows),
        slide(7, '期末 WinForms 草圖與事件', ['草圖為期末規劃，示意數據不作效能證據。', '新增／編輯 Click、選取 SelectionChanged、輸入 Validating。'], picture=images[0]),
        slide(8, '交付、驗收與期末延續', ['71 項自動測試通過，已驗證隔離 Windows Ctrl+C 事件。', 'Release、原始碼、資料格式、圖檔、簡報與操作文件已備。', '待組員完成另一台實體電腦／帳號的啟動與展示簽核。', '期末沿用 Core／Repository，先遷移 SQLite 再完成視窗。'])
    ]
    save_pdf(pages, SLIDES / 'midterm-briefing.pdf', 'Algorithm Lab midterm briefing')
    deck = Presentation(); deck.slide_width = Inches(16); deck.slide_height = Inches(9)
    for i, page in enumerate(pages, 1):
        s = deck.slides.add_slide(deck.slide_layouts[6])
        s.shapes.add_picture(str(SLIDES / f'slide-{i:02d}.png'), 0, 0, width=deck.slide_width, height=deck.slide_height)
        s.notes_slide.notes_text_frame.text = '請依 docs/demo-script.md 彩排。此頁為高解析圖像版，原始排版可由 scripts/render-midterm-assets.py 重建。'
    # Editable identity field, without inventing personal names or student IDs.
    identity = deck.slides[0].shapes.add_textbox(Inches(.7), Inches(7.4), Inches(14.5), Inches(.65))
    frame = identity.text_frame; frame.text = '組員／班級／學號：請填入'
    for run in frame.paragraphs[0].runs:
        run.font.name = 'Microsoft JhengHei'; run.font.size = Pt(23); run.font.color.rgb = RGBColor.from_string('235AD1')
    deck.save(SLIDES / 'midterm-briefing.pptx')
    (ROOT / 'docs/evidence/results-table.md').write_text(
        '# 本機實測摘要\n\n來源：demo-store.json；三個 Completed Run，皆為同一輸入、Random／N=1000／seed=42／Release x64。時間是各 Run 的五個正式樣本中位數。\n\n'
        '| Algorithm | Median ms | Comparisons | Swaps | Writes |\n| --- | --- | --- | --- | --- |\n' +
        ''.join(f"| {r['algorithmId']} | {r['medianMs']:.6f} | {r['comparisonCount']} | {r['swapCount']} | {r['writeCount']} |\n" for r in rows) +
        '\n完整環境、timer ticks、Frequency、Hash 與版本保留於原始 JSON；不可直接將此單一案例的排行推廣至所有資料。\n', encoding='utf-8')
    print('Created 3 UI PNGs, class diagram, wireframe PDF, 8-slide PDF/PPTX, real-results table.')


if __name__ == '__main__':
    main()
