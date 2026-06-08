using System.Text;
using System.Text.RegularExpressions;
using MedicalProgress.App.Helpers;
using MedicalProgress.App.Models;
using MedicalProgress.App.Services;

namespace MedicalProgress.App.Forms;

public class CombinedBrowseForm : Form
{
    private static readonly Color ClrBtnBlue   = MedStyleHelper.PrimaryBlue;
    private static readonly Color ClrBtnGreen  = MedStyleHelper.SuccessGreen;
    private static readonly Color ClrBtnAmber  = MedStyleHelper.WarningOrange;
    private static readonly Color ClrBtnOrange = Color.FromArgb(210, 95, 20);
    private static readonly Color ClrBtnGray   = MedStyleHelper.TextGray;
    private static readonly Font  FontMain     = MedStyleHelper.FontSmall;
    private static readonly Font  FontBold     = MedStyleHelper.FontBold;

    private readonly Patient         _patient;
    private readonly List<ProgressRecord> _records;
    private readonly GeneratorService    _generatorService = new();
    private readonly DatabaseService     _databaseService;

    private CheckedListBox clbRecords  = null!;
    private RichTextBox    txtCombined = null!;
    private Label          lblStatus   = null!;

    public CombinedBrowseForm(Patient patient, List<ProgressRecord> records, DatabaseService databaseService)
    {
        _patient         = patient;
        _records         = records.OrderBy(r => r.RecordDate).ToList();
        _databaseService = databaseService;
        InitializeComponent();
        Load += (_, _) =>
        {
            SelectAll(true);
            clbRecords.ItemCheck += ClbItemCheck;
            RefreshCombinedText();
        };
    }

    private void InitializeComponent()
    {
        Text          = $"{_patient.Name} — 联合病程浏览";
        Size          = new Size(1300, 860);
        MinimumSize   = new Size(900, 600);
        WindowState   = FormWindowState.Maximized;
        StartPosition = FormStartPosition.CenterParent;
        BackColor     = MedStyleHelper.ContentBg;

        // ── 顶部工具栏 ───────────────────────────────────────
        // 顶部 Header
        var header = MedStyleHelper.CreateHeader($"联合病程浏览  |  {_patient.Name}");

        var toolbar = new Panel
        {
            Dock      = DockStyle.Top,
            Height    = 52,
            BackColor = Color.White,
            Padding   = new Padding(10, 8, 10, 8)
        };

        var flow = new FlowLayoutPanel
        {
            Dock          = DockStyle.Fill,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents  = false,
            BackColor     = Color.Transparent
        };

        var btnAll     = MakeBtn("全部勾选", ClrBtnBlue,   90);
        var btnNone    = MakeBtn("全部取消", ClrBtnGray,   90);
        var btnRefresh = MakeBtn("刷新合并", ClrBtnGreen,  90);
        var btnCheck   = MakeBtn("检查高亮", ClrBtnAmber,  90);
        var btnSave    = MakeBtn("保存记录", ClrBtnOrange, 90);
        var btnCopy    = MakeBtn("复制全文", ClrBtnGray,   90);
        var btnClose   = MakeBtn("关闭",     ClrBtnGray,   70);

        btnAll.Click     += (_, _) => { SelectAll(true);  RefreshCombinedText(); };
        btnNone.Click    += (_, _) => { SelectAll(false); RefreshCombinedText(); };
        btnRefresh.Click += (_, _) => RefreshCombinedText();
        btnCheck.Click   += (_, _) => RunCheck();
        btnSave.Click    += BtnSave_Click;
        btnCopy.Click    += (_, _) => CopyText();
        btnClose.Click   += (_, _) => Close();

        var lblHint = new Label
        {
            Text      = "检查高亮：红=重复  橙=占位符未填    保存：勾选恰好一条记录后点击",
            Font      = new Font("Microsoft YaHei UI", 8),
            ForeColor = Color.FromArgb(120, 140, 160),
            AutoSize  = true,
            Margin    = new Padding(12, 10, 0, 0)
        };

        flow.Controls.AddRange(new Control[] { btnAll, btnNone, btnRefresh, btnCheck, btnSave, btnCopy, btnClose, lblHint });

        var sep = new Panel { Dock = DockStyle.Bottom, Height = 1, BackColor = Color.FromArgb(200, 220, 235) };
        toolbar.Controls.Add(flow);
        toolbar.Controls.Add(sep);

        // ── 状态栏 ───────────────────────────────────────────
        lblStatus = new Label
        {
            Dock      = DockStyle.Bottom,
            Height    = 28,
            Text      = "就绪",
            Font      = FontMain,
            ForeColor = Color.FromArgb(180, 200, 220),
            TextAlign = ContentAlignment.MiddleLeft,
            Padding   = new Padding(10, 0, 0, 0),
            BackColor = MedStyleHelper.StatusBarBg
        };

        // ── 勾选列表 ─────────────────────────────────────────
        clbRecords = new CheckedListBox
        {
            Dock         = DockStyle.Fill,
            CheckOnClick = true,
            Font         = FontMain,
            BorderStyle  = BorderStyle.None,
            BackColor    = MedStyleHelper.SidebarBg
        };
        // ItemCheck 在 Load 后挂载，避免 Items.Add 触发 BeginInvoke 时句柄尚未创建

        // ── 主体左右分割 ─────────────────────────────────────
        var splitter = new SplitContainer
        {
            Dock          = DockStyle.Fill,
            Panel1MinSize = 160,
            BackColor     = MedStyleHelper.BorderColor
        };

        // 左：记录列表
        var leftPanel = new Panel { Dock = DockStyle.Fill, BackColor = MedStyleHelper.SidebarBg, Padding = new Padding(6) };
        leftPanel.Controls.Add(clbRecords);
        leftPanel.Controls.Add(new Label
        {
            Text      = "选择要浏览的病程",
            Dock      = DockStyle.Top,
            Height    = 28,
            Font      = FontBold,
            ForeColor = ClrBtnBlue,
            TextAlign = ContentAlignment.MiddleLeft
        });

        // 右：可编辑预览
        var rightPanel = new Panel { Dock = DockStyle.Fill, BackColor = Color.White, Padding = new Padding(6) };
        txtCombined = new RichTextBox
        {
            Dock        = DockStyle.Fill,
            ReadOnly    = false,
            Font        = MedStyleHelper.FontBody,
            ScrollBars  = RichTextBoxScrollBars.Vertical,
            WordWrap    = true,
            DetectUrls  = false,
            BorderStyle = BorderStyle.None,
            BackColor   = Color.White
        };
        rightPanel.Controls.Add(txtCombined);
        rightPanel.Controls.Add(new Label
        {
            Text      = "合并内容预览（可直接编辑，勾选单条后点【保存记录】写回）",
            Dock      = DockStyle.Top,
            Height    = 28,
            Font      = FontBold,
            ForeColor = ClrBtnGreen,
            TextAlign = ContentAlignment.MiddleLeft
        });

        splitter.Panel1.Controls.Add(leftPanel);
        splitter.Panel2.Controls.Add(rightPanel);

        Controls.Add(splitter);
        Controls.Add(lblStatus);
        Controls.Add(toolbar);
        Controls.Add(header);

        // 填充记录列表 — 存格式化字符串，避免 OwnerDraw 问题；索引与 _records 一一对应
        foreach (var r in _records)
            clbRecords.Items.Add($"{r.RecordDate:yyyy-MM-dd HH:mm}  {r.RecordType}", false);

        // 设置分割位置（窗口 Load 后执行，此时实际宽度已确定）
        Load += (_, _) => splitter.SplitterDistance = Math.Max(160, Math.Min(220, splitter.Width - 200));
    }

    private void ClbItemCheck(object? sender, ItemCheckEventArgs e)
    {
        BeginInvoke(RefreshCombinedText);
    }

    // ─────────────────────────────────────────────────────────
    //  全选 / 全不选
    // ─────────────────────────────────────────────────────────

    private void SelectAll(bool check)
    {
        for (int i = 0; i < clbRecords.Items.Count; i++)
            clbRecords.SetItemChecked(i, check);
    }

    // ─────────────────────────────────────────────────────────
    //  刷新合并文本
    // ─────────────────────────────────────────────────────────

    private void RefreshCombinedText()
    {
        var sb = new StringBuilder();
        sb.AppendLine($"患者：{_patient.Name}  {_patient.Gender}  {_patient.Age}岁  住院号：{_patient.MedicalRecordNumber}");
        sb.AppendLine(new string('═', 60));
        sb.AppendLine();

        var checkedRecords = GetCheckedRecords();
        foreach (var r in checkedRecords)
        {
            sb.AppendLine($"【{r.RecordDate:yyyy-MM-dd HH:mm}  {r.RecordType}】");
            sb.AppendLine(r.Content.Trim());
            sb.AppendLine();
            sb.AppendLine(new string('─', 60));
            sb.AppendLine();
        }

        txtCombined.Text = sb.ToString();

        lblStatus.Text      = $"共 {checkedRecords.Count} 条记录，{txtCombined.Text.Length} 字符";
        lblStatus.ForeColor = Color.FromArgb(80, 100, 120);
    }

    // ─────────────────────────────────────────────────────────
    //  检查高亮
    // ─────────────────────────────────────────────────────────

    private void RunCheck()
    {
        var checkedRecords = GetCheckedRecords();
        if (checkedRecords.Count == 0)
        {
            lblStatus.Text = "请先勾选至少一条病程记录。";
            return;
        }

        RefreshCombinedText();

        // 1. 全部重置黑色
        txtCombined.SelectAll();
        txtCombined.SelectionColor = Color.Black;

        // 2. 占位符（橙色）
        var placeholderRe = new Regex(@"【[^】]{0,20}】");
        foreach (Match m in placeholderRe.Matches(txtCombined.Text))
        {
            txtCombined.Select(m.Index, m.Length);
            txtCombined.SelectionColor = Color.FromArgb(200, 100, 0);
        }

        // 3. 跨记录重复（红色）
        var duplicateSet = new HashSet<string>(StringComparer.Ordinal);
        for (int i = 0; i < checkedRecords.Count; i++)
        {
            var current = checkedRecords[i].Content;
            var history = checkedRecords.Where((_, j) => j != i).Select(r => r.Content).ToList();
            var dups    = _generatorService.GetDuplicateTexts(current, history);
            foreach (var d in dups) duplicateSet.Add(d);
        }

        foreach (var dup in duplicateSet)
        {
            if (string.IsNullOrWhiteSpace(dup)) continue;
            var start = 0;
            while (start < txtCombined.TextLength)
            {
                var idx = txtCombined.Text.IndexOf(dup, start, StringComparison.Ordinal);
                if (idx < 0) break;
                txtCombined.Select(idx, dup.Length);
                txtCombined.SelectionColor = Color.FromArgb(200, 0, 0);
                start = idx + dup.Length;
            }
        }

        txtCombined.Select(0, 0);

        lblStatus.Text      = duplicateSet.Count > 0
            ? $"发现 {duplicateSet.Count} 处重复（红色），占位符已标橙"
            : "未检测到明显重复，占位符已标橙";
        lblStatus.ForeColor = duplicateSet.Count > 0 ? MedStyleHelper.DangerRed : MedStyleHelper.SuccessGreen;
    }

    // ─────────────────────────────────────────────────────────
    //  保存：将预览框中对应记录的内容写回数据库
    // ─────────────────────────────────────────────────────────

    private async void BtnSave_Click(object? sender, EventArgs e)
    {
        var checkedRecords = GetCheckedRecords();
        if (checkedRecords.Count != 1)
        {
            lblStatus.Text      = checkedRecords.Count == 0 ? "请先勾选一条记录再保存" : "保存时请只勾选一条记录";
            lblStatus.ForeColor = MedStyleHelper.DangerRed;
            return;
        }

        var record = checkedRecords[0];
        var text   = txtCombined.Text;

        // 定位该记录在预览文本中的起止位置
        var header       = $"【{record.RecordDate:yyyy-MM-dd HH:mm}  {record.RecordType}】";
        var headerIdx    = text.IndexOf(header, StringComparison.Ordinal);
        var contentStart = headerIdx >= 0 ? headerIdx + header.Length : 0;

        // 跳过标题行后的换行
        while (contentStart < text.Length && (text[contentStart] == '\r' || text[contentStart] == '\n'))
            contentStart++;

        // 找到下一个分隔线
        var sep     = new string('─', 60);
        var sepIdx  = text.IndexOf(sep, contentStart, StringComparison.Ordinal);
        var content = (sepIdx > contentStart ? text[contentStart..sepIdx] : text[contentStart..]).Trim();

        record.Content = content;
        await _databaseService.UpdateProgressRecordAsync(record);

        lblStatus.Text      = $"已保存：{record.RecordDate:yyyy-MM-dd HH:mm}  {record.RecordType}";
        lblStatus.ForeColor = MedStyleHelper.SuccessGreen;
    }

    private void CopyText()
    {
        if (!string.IsNullOrWhiteSpace(txtCombined.Text))
            Clipboard.SetText(txtCombined.Text);
        lblStatus.Text      = "已复制到剪贴板";
        lblStatus.ForeColor = MedStyleHelper.SuccessGreen;
    }

    private List<ProgressRecord> GetCheckedRecords()
    {
        var result = new List<ProgressRecord>();
        for (int i = 0; i < clbRecords.Items.Count; i++)
            if (clbRecords.GetItemChecked(i))
                result.Add(_records[i]);
        return result;
    }

    private static Button MakeBtn(string text, Color color, int width)
    {
        var b = new Button
        {
            Text      = text,
            Width     = width,
            Height    = 34,
            BackColor = color,
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            Font      = FontMain,
            Margin    = new Padding(0, 0, 6, 0)
        };
        b.FlatAppearance.BorderSize = 0;
        return b;
    }
}
