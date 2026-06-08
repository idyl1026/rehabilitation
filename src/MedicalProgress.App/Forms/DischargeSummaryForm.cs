using System.Text;
using System.Text.RegularExpressions;
using MedicalProgress.App.Models;

namespace MedicalProgress.App.Forms;

/// <summary>
/// 出院小结窗体：从患者基本信息和全部病程自动提取各模块内容，支持直接编辑和复制全文。
/// </summary>
public class DischargeSummaryForm : Form
{
    private static readonly Font  FontMain  = new("Microsoft YaHei UI", 10);
    private static readonly Font  FontBold  = new("Microsoft YaHei UI", 10, FontStyle.Bold);
    private static readonly Font  FontSmall = new("Microsoft YaHei UI", 9);
    private static readonly Color ClrBlue   = Color.FromArgb(21, 101, 192);
    private static readonly Color ClrGreen  = Color.FromArgb(27, 130, 80);
    private static readonly Color ClrGray   = Color.FromArgb(96, 108, 118);

    private readonly Patient              _patient;
    private readonly List<ProgressRecord> _records;

    private RichTextBox txtAdmissionDiagnosis  = null!;
    private RichTextBox txtDischargeDiagnosis  = null!;
    private RichTextBox txtAdmissionPhysExam   = null!;
    private RichTextBox txtLabResults          = null!;
    private RichTextBox txtImagingResults      = null!;
    private RichTextBox txtDischargePhysExam   = null!;
    private RichTextBox txtDischargeMeds       = null!;
    private RichTextBox txtDischargeNotes      = null!;
    private Label       lblStatus              = null!;

    public DischargeSummaryForm(Patient patient, List<ProgressRecord> records)
    {
        _patient = patient;
        _records = records.OrderBy(r => r.RecordDate).ToList();
        InitializeComponent();
        Load += (_, _) => PopulateFromRecords();
    }

    private void InitializeComponent()
    {
        Text          = $"{_patient.Name} — 出院小结";
        Size          = new Size(1100, 860);
        MinimumSize   = new Size(700, 500);
        WindowState   = FormWindowState.Maximized;
        StartPosition = FormStartPosition.CenterParent;
        BackColor     = Color.FromArgb(245, 249, 253);

        // ── 顶部工具栏 ───────────────────────────────────────
        var toolbar = new Panel { Dock = DockStyle.Top, Height = 52, BackColor = Color.White, Padding = new Padding(10, 8, 10, 8) };
        var flow    = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.LeftToRight, WrapContents = false, BackColor = Color.Transparent };

        var lblInfo = new Label
        {
            Text      = $"{_patient.Name}  {_patient.Gender}  {_patient.Age}岁  住院号：{_patient.MedicalRecordNumber}  入院：{_patient.AdmissionDate:yyyy-MM-dd}",
            Font      = FontSmall,
            ForeColor = ClrBlue,
            AutoSize  = true,
            Margin    = new Padding(0, 8, 20, 0)
        };
        var btnRefresh = MakeBtn("重新提取", ClrGreen, 90);
        var btnCopy    = MakeBtn("复制全文", ClrGray,  90);
        var btnClose   = MakeBtn("关闭",     ClrGray,  70);

        btnRefresh.Click += (_, _) => PopulateFromRecords();
        btnCopy.Click    += (_, _) => CopyAll();
        btnClose.Click   += (_, _) => Close();

        flow.Controls.AddRange(new Control[] { lblInfo, btnRefresh, btnCopy, btnClose });
        var toolSep = new Panel { Dock = DockStyle.Bottom, Height = 1, BackColor = Color.FromArgb(200, 220, 235) };
        toolbar.Controls.Add(flow);
        toolbar.Controls.Add(toolSep);

        // ── 状态栏 ───────────────────────────────────────────
        lblStatus = new Label
        {
            Dock      = DockStyle.Bottom,
            Height    = 26,
            Text      = "正在加载…",
            Font      = FontSmall,
            ForeColor = ClrGreen,
            TextAlign = ContentAlignment.MiddleLeft,
            Padding   = new Padding(10, 0, 0, 0),
            BackColor = Color.FromArgb(245, 250, 253)
        };

        // ── 主体滚动区（Dock=Top 叠加，自动全宽，无需 Resize 处理）────
        var scroll = new Panel
        {
            Dock       = DockStyle.Fill,
            AutoScroll = true,
            Padding    = new Padding(16, 8, 16, 8),
            BackColor  = Color.FromArgb(245, 249, 253)
        };

        // 用 Dock=Top 叠加时，控件按"后加先显"排列，所以倒序添加 ↓
        txtDischargeNotes     = Section(scroll, "出院注意事项",                       120);
        txtDischargeMeds      = Section(scroll, "出院带药",                           120);
        txtDischargePhysExam  = Section(scroll, "出院查体（默认同最后一次病程查体）", 120);
        txtImagingResults     = Section(scroll, "检查结果（影像、心电图、彩超等）",   150);
        txtLabResults         = Section(scroll, "检验结果（血尿便等化验）",            180);
        txtAdmissionPhysExam  = Section(scroll, "入院查体",                           120);
        txtDischargeDiagnosis = Section(scroll, "出院诊断（默认同入院诊断）",          58);
        txtAdmissionDiagnosis = Section(scroll, "入院诊断",                            58);

        Controls.Add(scroll);
        Controls.Add(lblStatus);
        Controls.Add(toolbar);
    }

    // 创建一个区段：标题 Label + 可编辑 RichTextBox，整体 Dock=Top 插入父容器
    private static RichTextBox Section(Panel parent, string title, int boxHeight)
    {
        var section = new Panel
        {
            Dock      = DockStyle.Top,
            Height    = 28 + boxHeight + 10,   // label + box + bottom gap
            BackColor = Color.Transparent
        };

        var lbl = new Label
        {
            Text      = title,
            Dock      = DockStyle.Top,
            Height    = 26,
            Font      = FontBold,
            ForeColor = ClrBlue,
            TextAlign = ContentAlignment.MiddleLeft
        };

        var box = new RichTextBox
        {
            Dock        = DockStyle.Fill,
            Font        = FontMain,
            WordWrap    = true,
            ScrollBars  = RichTextBoxScrollBars.Vertical,
            DetectUrls  = false,
            BorderStyle = BorderStyle.FixedSingle,
            BackColor   = Color.White
        };

        section.Controls.Add(box);
        section.Controls.Add(lbl);
        parent.Controls.Add(section);
        return box;
    }

    // ─────────────────────────────────────────────────────────
    //  从病程记录提取并填充各模块
    // ─────────────────────────────────────────────────────────

    private void PopulateFromRecords()
    {
        var firstRecord = _records.FirstOrDefault();
        var lastRecord  = _records.LastOrDefault();

        // 入院诊断
        txtAdmissionDiagnosis.Text = _patient.Diagnosis;

        // 出院诊断（默认同入院诊断）
        txtDischargeDiagnosis.Text = !string.IsNullOrWhiteSpace(_patient.DischargeDiagnosis)
            ? _patient.DischargeDiagnosis : _patient.Diagnosis;

        // 入院查体：优先用建档时的 PhysicalExam，其次提取首次病程
        if (!string.IsNullOrWhiteSpace(_patient.PhysicalExam))
            txtAdmissionPhysExam.Text = _patient.PhysicalExam;
        else if (firstRecord != null)
            txtAdmissionPhysExam.Text = ExtractPhysExam(firstRecord.Content);

        // 检验 + 检查结果：汇总所有病程
        var (labSb, imagingSb) = AggregateExamResults();
        txtLabResults.Text     = labSb.ToString().Trim();
        txtImagingResults.Text = imagingSb.ToString().Trim();

        // 出院查体：最后一条病程，若为空则默认同入院查体
        if (lastRecord != null)
        {
            var lastPhysExam = ExtractPhysExam(lastRecord.Content);
            txtDischargePhysExam.Text = string.IsNullOrWhiteSpace(lastPhysExam)
                ? txtAdmissionPhysExam.Text : lastPhysExam;
        }

        // 出院带药：优先 Patient.DischargeOrders，否则提取最后病程医嘱变化
        if (!string.IsNullOrWhiteSpace(_patient.DischargeOrders))
        {
            txtDischargeMeds.Text = _patient.DischargeOrders;
        }
        else if (lastRecord != null)
        {
            var orders = ExtractSection(lastRecord.Content, "医嘱变化：", "评估分析：", "诊疗计划：");
            txtDischargeMeds.Text = string.IsNullOrWhiteSpace(orders) ? "（请填写出院带药）" : orders;
        }
        else
        {
            txtDischargeMeds.Text = "（请填写出院带药）";
        }

        // 出院注意事项（预置模板）
        txtDischargeNotes.Text =
            "1. 注意休息，避免劳累，保持规律作息\r\n" +
            "2. 合理饮食，低盐低脂，忌辛辣刺激\r\n" +
            "3. 按时服药，不得自行停药或调整剂量\r\n" +
            "4. 定期门诊复查，携带本次出院小结\r\n" +
            "5. 如出现病情变化或不适，及时就诊";

        lblStatus.Text      = $"已从 {_records.Count} 条病程记录自动提取，可直接编辑各栏内容";
        lblStatus.ForeColor = ClrGreen;
    }

    // ─────────────────────────────────────────────────────────
    //  提取查体
    // ─────────────────────────────────────────────────────────

    private static string ExtractPhysExam(string content)
    {
        var v = ExtractSection(content, "客观体征：", "检验结果：", "检查结果：", "医嘱变化：", "评估分析：", "诊疗计划：");
        if (!string.IsNullOrWhiteSpace(v)) return v;
        v = ExtractBetweenHeadings(content, "三、体格检查");
        if (!string.IsNullOrWhiteSpace(v)) return v;
        v = ExtractBetweenHeadings(content, "三、查体情况");
        if (!string.IsNullOrWhiteSpace(v)) return v;
        return ExtractSection(content, "查体：", "辅助检查：", "检查结果：", "诊疗计划：");
    }

    // ─────────────────────────────────────────────────────────
    //  汇总检查结果，按关键字分流为检验 / 检查
    // ─────────────────────────────────────────────────────────

    private static readonly string[] ImagingKeywords =
    {
        "CT", "MRI", "X线", "胸片", "心电图", "彩超", "超声", "B超",
        "心脏彩超", "腹部彩超", "颈动脉", "下肢静脉", "冠脉CTA",
        "造影", "内镜", "胃镜", "肠镜", "支气管镜", "病理"
    };

    private (StringBuilder lab, StringBuilder imaging) AggregateExamResults()
    {
        var lab     = new StringBuilder();
        var imaging = new StringBuilder();
        var seen    = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        if (!string.IsNullOrWhiteSpace(_patient.AuxiliaryExam))
            Classify(_patient.AuxiliaryExam, lab, imaging, seen);

        foreach (var r in _records)
        {
            // 支持新格式（检验结果：/ 检查结果：）和旧格式（检查结果：）
            var labRaw     = ExtractSection(r.Content, "检验结果：", "检查结果：", "医嘱变化：", "评估分析：", "诊疗计划：");
            var imagingRaw = ExtractSection(r.Content, "检查结果：", "医嘱变化：",  "评估分析：", "诊疗计划：");

            if (!string.IsNullOrWhiteSpace(labRaw))
                AppendLines(labRaw, lab, seen);
            if (!string.IsNullOrWhiteSpace(imagingRaw))
                AppendLines(imagingRaw, imaging, seen);

            // 若两个新标签都找不到，fallback 到旧格式并自动分类
            if (string.IsNullOrWhiteSpace(labRaw) && string.IsNullOrWhiteSpace(imagingRaw))
            {
                var old = ExtractBetweenHeadings(r.Content, "四、辅助检查");
                if (!string.IsNullOrWhiteSpace(old))
                    Classify(old, lab, imaging, seen);
            }
        }

        return (lab, imaging);
    }

    private static void Classify(string text, StringBuilder lab, StringBuilder imaging, HashSet<string> seen)
    {
        foreach (var line in text.Split('\n', StringSplitOptions.RemoveEmptyEntries))
        {
            var t = line.Trim();
            if (string.IsNullOrWhiteSpace(t) || !seen.Add(t)) continue;
            if (ImagingKeywords.Any(k => t.Contains(k, StringComparison.OrdinalIgnoreCase)))
                imaging.AppendLine(t);
            else
                lab.AppendLine(t);
        }
    }

    private static void AppendLines(string text, StringBuilder sb, HashSet<string> seen)
    {
        foreach (var line in text.Split('\n', StringSplitOptions.RemoveEmptyEntries))
        {
            var t = line.Trim();
            if (!string.IsNullOrWhiteSpace(t) && seen.Add(t))
                sb.AppendLine(t);
        }
    }

    // ─────────────────────────────────────────────────────────
    //  复制全文
    // ─────────────────────────────────────────────────────────

    private void CopyAll()
    {
        var dischargeDate = _patient.DischargeDate ?? DateTime.Now;
        var hospitalDays  = (int)(dischargeDate - _patient.AdmissionDate).TotalDays + 1;

        var sb = new StringBuilder();
        sb.AppendLine("出  院  小  结");
        sb.AppendLine(new string('═', 50));
        sb.AppendLine($"姓名：{_patient.Name}    性别：{_patient.Gender}    年龄：{_patient.Age}岁");
        sb.AppendLine($"住院号：{_patient.MedicalRecordNumber}    科室：{_patient.Department}    床号：{_patient.BedNumber}");
        sb.AppendLine($"入院日期：{_patient.AdmissionDate:yyyy年MM月dd日}    出院日期：{dischargeDate:yyyy年MM月dd日}    住院天数：{hospitalDays}天");
        sb.AppendLine(new string('─', 50));
        sb.AppendLine();
        AppendBlock(sb, "入院诊断",    txtAdmissionDiagnosis.Text);
        AppendBlock(sb, "出院诊断",    txtDischargeDiagnosis.Text);
        AppendBlock(sb, "入院查体",    txtAdmissionPhysExam.Text);
        AppendBlock(sb, "检验结果",    txtLabResults.Text);
        AppendBlock(sb, "检查结果",    txtImagingResults.Text);
        AppendBlock(sb, "出院查体",    txtDischargePhysExam.Text);
        AppendBlock(sb, "出院带药",    txtDischargeMeds.Text);
        AppendBlock(sb, "出院注意事项", txtDischargeNotes.Text);

        Clipboard.SetText(sb.ToString());
        lblStatus.Text      = "已复制到剪贴板";
        lblStatus.ForeColor = Color.FromArgb(0, 137, 123);
    }

    private static void AppendBlock(StringBuilder sb, string title, string content)
    {
        sb.AppendLine($"【{title}】");
        sb.AppendLine(string.IsNullOrWhiteSpace(content) ? "（未填写）" : content.Trim());
        sb.AppendLine();
    }

    // ─────────────────────────────────────────────────────────
    //  静态文本提取工具
    // ─────────────────────────────────────────────────────────

    private static string ExtractSection(string content, string startMarker, params string[] endMarkers)
    {
        var si = content.IndexOf(startMarker, StringComparison.Ordinal);
        if (si < 0) return string.Empty;
        si += startMarker.Length;
        var ei = content.Length;
        foreach (var end in endMarkers)
        {
            var idx = content.IndexOf(end, si, StringComparison.Ordinal);
            if (idx >= 0 && idx < ei) ei = idx;
        }
        return content[si..ei].Trim();
    }

    private static string ExtractBetweenHeadings(string content, string startHeading)
    {
        var si = content.IndexOf(startHeading, StringComparison.Ordinal);
        if (si < 0) return string.Empty;
        si += startHeading.Length;
        var next = Regex.Match(content[si..], @"\n[一二三四五六七八九十]+[、.．]");
        var ei   = next.Success ? si + next.Index : content.Length;
        return content[si..ei].Trim();
    }

    private static Button MakeBtn(string text, Color color, int width)
    {
        var b = new Button { Text = text, Width = width, Height = 34, BackColor = color, ForeColor = Color.White, FlatStyle = FlatStyle.Flat, Font = FontSmall, Margin = new Padding(0, 0, 6, 0) };
        b.FlatAppearance.BorderSize = 0;
        return b;
    }
}
