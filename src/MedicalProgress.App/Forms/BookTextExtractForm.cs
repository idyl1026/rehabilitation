using MedicalProgress.App.Data;
using MedicalProgress.App.Helpers;
using MedicalProgress.App.Models;
using MedicalProgress.App.Services;

namespace MedicalProgress.App.Forms;

/// <summary>
/// 从书本/教材文本中提取核心知识点并导入知识库
/// </summary>
public class BookTextExtractForm : Form
{
    private readonly KnowledgePointExtractorService _extractor = new();
    private readonly int _defaultSubjectId;

    private TextBox txtInput   = null!;
    private ListBox lstResults = null!;
    private Label   lblStatus  = null!;
    private Button  btnExtract = null!;
    private Button  btnImport  = null!;

    // 提取出的知识点（全部）
    private List<KnowledgePoint> _extracted = new();

    public BookTextExtractForm(int defaultSubjectId = 0)
    {
        _defaultSubjectId = defaultSubjectId;
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        Text            = "从书本提取知识点";
        Size            = new Size(1100, 720);
        MinimumSize     = new Size(900, 600);
        StartPosition   = FormStartPosition.CenterParent;
        MedStyleHelper.ApplyWindowStyle(this);

        // ── 顶部标题栏 ────────────────────────────────────────────
        var header = MedStyleHelper.CreateHeader("教材知识点提取");

        // ── 底部状态栏 ────────────────────────────────────────────
        var footer = new Panel
        {
            Dock      = DockStyle.Bottom,
            Height    = 56,
            BackColor = Color.White,
            Padding   = new Padding(12, 8, 12, 8)
        };
        lblStatus = new Label
        {
            Text      = "请粘贴教材原文，然后点击【提取核心知识点】",
            Font      = MedStyleHelper.FontBody,
            ForeColor = MedStyleHelper.DisabledGray,
            AutoSize  = false,
            Dock      = DockStyle.Left,
            Width     = 520,
            TextAlign = ContentAlignment.MiddleLeft
        };
        btnImport = MedStyleHelper.CreatePrimaryButton("✅ 导入选中到知识库", 180);
        btnImport.Dock    = DockStyle.Right;
        btnImport.Height  = 40;
        btnImport.Enabled = false;
        btnImport.Click  += BtnImport_Click;

        var btnCancel = MedStyleHelper.CreateSecondaryButton("取消", 80);
        btnCancel.Dock   = DockStyle.Right;
        btnCancel.Height = 40;
        btnCancel.Margin = new Padding(0, 0, 8, 0);
        btnCancel.Click += (_, _) => Close();

        footer.Controls.Add(lblStatus);
        footer.Controls.Add(btnImport);
        footer.Controls.Add(btnCancel);

        // ── 主体：左右分割 ────────────────────────────────────────
        var split = new SplitContainer
        {
            Dock             = DockStyle.Fill,
            SplitterDistance = 500,
            Panel1MinSize    = 350,
            BackColor        = MedStyleHelper.BgGray
        };

        // 左侧：输入区域
        split.Panel1.Padding = new Padding(12);
        var lblInputTitle = new Label
        {
            Text      = "粘贴教材原文",
            Font      = MedStyleHelper.FontBold,
            ForeColor = MedStyleHelper.HeaderDark,
            Dock      = DockStyle.Top,
            Height    = 28,
            TextAlign = ContentAlignment.MiddleLeft
        };
        txtInput = new TextBox
        {
            Dock        = DockStyle.Fill,
            Multiline   = true,
            ScrollBars  = ScrollBars.Vertical,
            Font        = MedStyleHelper.FontBody,
            BackColor   = Color.White,
            BorderStyle = BorderStyle.FixedSingle,
            WordWrap    = true,
            PlaceholderText = "在此粘贴教材/书本的章节文字，支持多段落……"
        };

        btnExtract = MedStyleHelper.CreatePrimaryButton("🔍 提取核心知识点", 180);
        btnExtract.Dock    = DockStyle.Bottom;
        btnExtract.Height  = 40;
        btnExtract.Margin  = new Padding(0, 8, 0, 0);
        btnExtract.Click  += BtnExtract_Click;

        split.Panel1.Controls.Add(txtInput);
        split.Panel1.Controls.Add(btnExtract);
        split.Panel1.Controls.Add(lblInputTitle);

        // 右侧：结果列表
        split.Panel2.Padding = new Padding(12);
        var lblResultTitle = new Label
        {
            Text      = "提取结果（支持多选）",
            Font      = MedStyleHelper.FontBold,
            ForeColor = MedStyleHelper.HeaderDark,
            Dock      = DockStyle.Top,
            Height    = 28,
            TextAlign = ContentAlignment.MiddleLeft
        };

        lstResults = new ListBox
        {
            Dock           = DockStyle.Fill,
            Font           = MedStyleHelper.FontBody,
            BackColor      = Color.White,
            BorderStyle    = BorderStyle.FixedSingle,
            SelectionMode  = SelectionMode.MultiExtended,
            IntegralHeight = false,
            DrawMode       = DrawMode.OwnerDrawVariable
        };
        lstResults.MeasureItem     += LstResults_MeasureItem;
        lstResults.DrawItem        += LstResults_DrawItem;
        lstResults.SelectedIndexChanged += (_, _) =>
            btnImport.Enabled = lstResults.SelectedIndices.Count > 0;

        split.Panel2.Controls.Add(lstResults);
        split.Panel2.Controls.Add(lblResultTitle);

        Controls.Add(split);
        Controls.Add(footer);
        Controls.Add(header);
    }

    // ── 提取逻辑 ──────────────────────────────────────────────────

    private async void BtnExtract_Click(object? sender, EventArgs e)
    {
        var text = txtInput.Text.Trim();
        if (string.IsNullOrWhiteSpace(text))
        {
            MessageBox.Show("请先粘贴教材原文。", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        btnExtract.Enabled = false;
        lblStatus.Text     = "正在提取知识点，请稍候……";
        lstResults.Items.Clear();
        _extracted.Clear();
        btnImport.Enabled  = false;

        try
        {
            _extracted = await Task.Run(() => _extractor.Extract(text));

            lstResults.Items.Clear();
            foreach (var kp in _extracted)
                lstResults.Items.Add(kp);

            lblStatus.Text = _extracted.Count > 0
                ? $"共提取到 {_extracted.Count} 条知识点，可多选后导入"
                : "未提取到有效知识点（得分均低于阈值）";
        }
        catch (Exception ex)
        {
            MessageBox.Show($"提取失败：{ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            lblStatus.Text = "提取失败";
        }
        finally
        {
            btnExtract.Enabled = true;
        }
    }

    // ── 导入逻辑 ──────────────────────────────────────────────────

    private async void BtnImport_Click(object? sender, EventArgs e)
    {
        var selected = lstResults.SelectedItems
            .Cast<KnowledgePoint>()
            .ToList();

        if (selected.Count == 0) return;

        // 选择目标学科
        int subjectId = await PickSubjectAsync();
        if (subjectId <= 0) return;

        try
        {
            btnImport.Enabled = false;
            lblStatus.Text    = "正在导入……";

            int count = 0;
            using var context = new AppDbContext();
            foreach (var kp in selected)
            {
                context.KnowledgeTemplates.Add(new KnowledgeTemplate
                {
                    SubjectId    = subjectId,
                    Title        = kp.Title,
                    Content      = kp.Content,
                    Keywords     = string.Join(", ", kp.Keywords),
                    Summary      = kp.Content.Length > 200 ? kp.Content[..200] : kp.Content,
                    TemplateType = "知识点",
                    CreatedAt    = DateTime.Now,
                    IsActive     = true
                });
                count++;
            }
            await context.SaveChangesAsync();

            lblStatus.Text = $"已成功导入 {count} 条知识点到知识库";
            MessageBox.Show($"成功导入 {count} 条知识点！", "导入成功", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"导入失败：{ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            lblStatus.Text = "导入失败";
        }
        finally
        {
            btnImport.Enabled = lstResults.SelectedIndices.Count > 0;
        }
    }

    private async Task<int> PickSubjectAsync()
    {
        using var context = new AppDbContext();
        var subjects = await Microsoft.EntityFrameworkCore.EntityFrameworkQueryableExtensions
            .ToListAsync(context.Subjects.Where(s => s.IsActive).OrderBy(s => s.Name));

        if (subjects.Count == 0)
        {
            MessageBox.Show("知识库中暂无学科，请先在知识库管理中新建学科。", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return 0;
        }

        // 如果只有一个学科，直接使用
        if (subjects.Count == 1) return subjects[0].Id;

        // 弹出选择对话框
        using var dlg = new Form
        {
            Text            = "选择目标学科",
            Size            = new Size(320, 200),
            StartPosition   = FormStartPosition.CenterParent,
            FormBorderStyle = FormBorderStyle.FixedDialog,
            MaximizeBox     = false,
            MinimizeBox     = false,
            BackColor       = MedStyleHelper.BgGray
        };
        var lbl = new Label { Text = "请选择导入到哪个学科：", Left = 16, Top = 16, Width = 280, AutoSize = false, Height = 24 };
        var cmb = new ComboBox
        {
            Left          = 16,
            Top           = 46,
            Width         = 272,
            DropDownStyle = ComboBoxStyle.DropDownList,
            Font          = MedStyleHelper.FontBody
        };
        foreach (var s in subjects) cmb.Items.Add(s.Name);
        // 默认选康复医学科
        var defIdx = subjects.FindIndex(s => s.Name == "康复医学科");
        cmb.SelectedIndex = defIdx >= 0 ? defIdx : 0;

        var ok     = MedStyleHelper.CreatePrimaryButton("确定", 90);
        ok.Left    = 16; ok.Top = 110; ok.DialogResult = DialogResult.OK;
        var cancel = MedStyleHelper.CreateSecondaryButton("取消", 80);
        cancel.Left = 120; cancel.Top = 110; cancel.DialogResult = DialogResult.Cancel;

        dlg.Controls.AddRange(new Control[] { lbl, cmb, ok, cancel });
        dlg.AcceptButton = ok;
        dlg.CancelButton = cancel;

        if (dlg.ShowDialog(this) != DialogResult.OK) return 0;
        return subjects[cmb.SelectedIndex].Id;
    }

    // ── 自定义绘制列表项（显示标题+内容预览+得分）─────────────────

    private void LstResults_MeasureItem(object? sender, MeasureItemEventArgs e)
    {
        e.ItemHeight = 60;
    }

    private void LstResults_DrawItem(object? sender, DrawItemEventArgs e)
    {
        if (e.Index < 0 || e.Index >= lstResults.Items.Count) return;
        if (lstResults.Items[e.Index] is not KnowledgePoint kp) return;

        var selected = (e.State & DrawItemState.Selected) == DrawItemState.Selected;
        var bg = selected ? MedStyleHelper.SelectionBg : Color.White;
        e.Graphics.FillRectangle(new SolidBrush(bg), e.Bounds);

        // 标题
        using var titleFont = new Font("Segoe UI", 10f, FontStyle.Bold);
        using var bodyFont  = new Font("Segoe UI", 9f);
        using var scoreFont = new Font("Segoe UI", 8.5f);

        var titleColor = MedStyleHelper.HeaderDark;
        var bodyColor  = Color.FromArgb(80, 80, 90);
        var scoreColor = MedStyleHelper.PrimaryBlue;

        var x   = e.Bounds.X + 8;
        var y   = e.Bounds.Y + 4;
        var w   = e.Bounds.Width - 80;

        e.Graphics.DrawString(kp.Title, titleFont, new SolidBrush(titleColor),
            new RectangleF(x, y, w, 20));

        var preview = kp.Content.Replace("\n", " ").Replace("\r", "");
        if (preview.Length > 80) preview = preview[..80] + "…";
        e.Graphics.DrawString(preview, bodyFont, new SolidBrush(bodyColor),
            new RectangleF(x, y + 22, w, 18));

        // 得分标签（右侧）
        var scoreText = $"分:{kp.Score:F1}";
        e.Graphics.DrawString(scoreText, scoreFont, new SolidBrush(scoreColor),
            new RectangleF(e.Bounds.Right - 70, y + 8, 64, 20));

        // 分隔线
        using var pen = new Pen(Color.FromArgb(240, 240, 240));
        e.Graphics.DrawLine(pen, e.Bounds.Left, e.Bounds.Bottom - 1, e.Bounds.Right, e.Bounds.Bottom - 1);
    }
}
