using MedicalProgress.App.Models;
using MedicalProgress.App.Services;

namespace MedicalProgress.App.Forms;

public class ProgressRecordEditForm : Form
{
    private readonly Patient _patient;
    private readonly List<ProgressRecord> _allRecords;
    private readonly ClinicalTextNormalizeService _normalizeService = new();
    private readonly ClinicalDocumentSectionParserService _sectionParserService = new();
    private readonly ExamResultParserService _examParserService = new();
    private readonly ClinicalChangeAnalyzerService _analyzerService = new();
    private readonly GeneratorService _generatorService = new();

    private DateTimePicker dtpRecordDate = null!;
    private ComboBox cmbRecordType = null!;
    private TextBox txtOrderChanges = null!;
    private RichTextBox txtContent = null!;
    private Label lblStatus = null!;

    public ProgressRecord Record { get; }

    public ProgressRecordEditForm(Patient patient, ProgressRecord record, List<ProgressRecord> allRecords)
    {
        _patient = patient;
        Record = record;
        _allRecords = allRecords;
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        Text = "修改病程记录";
        Size = new Size(1120, 800);
        StartPosition = FormStartPosition.CenterParent;
        BackColor = Color.FromArgb(245, 247, 250);

        var top = new FlowLayoutPanel
        {
            Dock = DockStyle.Top,
            Height = 58,
            Padding = new Padding(12),
            BackColor = Color.White,
            WrapContents = false
        };

        dtpRecordDate = new DateTimePicker
        {
            Width = 180,
            Format = DateTimePickerFormat.Custom,
            CustomFormat = "yyyy-MM-dd HH:mm",
            Value = Record.RecordDate,
            Margin = new Padding(0, 4, 12, 0)
        };

        cmbRecordType = new ComboBox
        {
            Width = 190,
            DropDownStyle = ComboBoxStyle.DropDown,
            Text = Record.RecordType,
            Margin = new Padding(0, 4, 12, 0),
            Font = new Font("Microsoft YaHei UI", 9)
        };
        cmbRecordType.Items.AddRange(new object[] { "首次病程记录", "主任医师查房记录", "副主任医师查房记录", "主治查房", "病程记录" });

        var btnAnalysis = CreateButton("重新生成分析", Color.FromArgb(111, 66, 193), 130);
        btnAnalysis.Click += (_, _) => RegenerateAnalysis();
        var btnDuplicate = CreateButton("检测重复", Color.FromArgb(255, 193, 7), 110);
        btnDuplicate.Click += (_, _) => HighlightDuplicates();
        var btnSave = CreateButton("保存修改", Color.FromArgb(40, 167, 69), 110);
        btnSave.Click += (_, _) => SaveAndClose();
        var btnCancel = CreateButton("取消", Color.FromArgb(108, 117, 125), 90);
        btnCancel.Click += (_, _) => DialogResult = DialogResult.Cancel;

        top.Controls.Add(new Label { Text = "日期", AutoSize = true, Margin = new Padding(0, 9, 8, 0) });
        top.Controls.Add(dtpRecordDate);
        top.Controls.Add(new Label { Text = "类型", AutoSize = true, Margin = new Padding(0, 9, 8, 0) });
        top.Controls.Add(cmbRecordType);
        top.Controls.Add(btnAnalysis);
        top.Controls.Add(btnDuplicate);
        top.Controls.Add(btnSave);
        top.Controls.Add(btnCancel);

        var orderPanel = new Panel { Dock = DockStyle.Top, Height = 96, Padding = new Padding(12), BackColor = Color.White };
        var orderLabel = new Label
        {
            Dock = DockStyle.Top,
            Height = 24,
            Text = "本次医嘱 / 治疗变动（重新生成分析时会纳入判断）",
            Font = new Font("Microsoft YaHei UI", 9, FontStyle.Bold)
        };
        txtOrderChanges = new TextBox
        {
            Dock = DockStyle.Fill,
            Multiline = true,
            ScrollBars = ScrollBars.Vertical,
            Font = new Font("Microsoft YaHei UI", 9),
            BorderStyle = BorderStyle.FixedSingle
        };
        orderPanel.Controls.Add(txtOrderChanges);
        orderPanel.Controls.Add(orderLabel);

        lblStatus = new Label
        {
            Dock = DockStyle.Bottom,
            Height = 28,
            Text = "可手动修改正文，也可输入医嘱变动后重新生成分析",
            ForeColor = Color.FromArgb(108, 117, 125),
            TextAlign = ContentAlignment.MiddleLeft,
            Padding = new Padding(12, 0, 0, 0)
        };

        txtContent = new RichTextBox
        {
            Dock = DockStyle.Fill,
            Text = Record.Content,
            Font = new Font("Microsoft YaHei UI", 10),
            ScrollBars = RichTextBoxScrollBars.Both,
            WordWrap = true,
            DetectUrls = false,
            BorderStyle = BorderStyle.FixedSingle,
            BackColor = Color.White
        };

        Controls.Add(txtContent);
        Controls.Add(lblStatus);
        Controls.Add(orderPanel);
        Controls.Add(top);
    }

    private static Button CreateButton(string text, Color color, int width) => new()
    {
        Text = text,
        Width = width,
        Height = 32,
        BackColor = color,
        ForeColor = Color.White,
        FlatStyle = FlatStyle.Flat,
        Font = new Font("Microsoft YaHei UI", 9),
        Margin = new Padding(0, 2, 8, 0)
    };

    private void RegenerateAnalysis()
    {
        var combined = txtContent.Text;
        if (!string.IsNullOrWhiteSpace(txtOrderChanges.Text))
            combined += Environment.NewLine + "医嘱 / 治疗变动：" + txtOrderChanges.Text.Trim();

        var normalized = _normalizeService.Normalize(combined);
        var parsed = _sectionParserService.Parse(normalized);
        parsed.PatientName = _patient.Name;
        parsed.Gender = _patient.Gender;
        parsed.Age = _patient.Age.ToString();
        parsed.MedicalRecordNumber = _patient.MedicalRecordNumber;
        parsed.Department = _patient.Department;
        parsed.BedNumber = _patient.BedNumber;
        parsed.TreatmentPlan = string.Join(Environment.NewLine, new[] { parsed.TreatmentPlan, txtOrderChanges.Text.Trim() }.Where(s => !string.IsNullOrWhiteSpace(s)));

        var examResults = _examParserService.Parse(normalized, _patient.Id);
        var suggestions = _analyzerService.Analyze(parsed, examResults, normalized);
        var analysis = _analyzerService.BuildAnalysisDraft(suggestions);
        if (!string.IsNullOrWhiteSpace(txtOrderChanges.Text))
            analysis = analysis.TrimEnd() + Environment.NewLine + "医嘱变动提示：" + txtOrderChanges.Text.Trim();

        txtContent.Text = ReplaceOrAppendAnalysis(txtContent.Text, analysis);
        lblStatus.Text = "已根据当前正文和医嘱变动重新生成分析";
        lblStatus.ForeColor = Color.FromArgb(40, 167, 69);
    }

    private static string ReplaceOrAppendAnalysis(string content, string analysis)
    {
        var labels = new[] { "评估分析：", "分析指导：", "评估分析:" };
        foreach (var label in labels)
        {
            var index = content.IndexOf(label, StringComparison.Ordinal);
            if (index < 0)
                continue;

            var nextPlanIndex = content.IndexOf("诊疗计划", index, StringComparison.Ordinal);
            if (nextPlanIndex < 0)
                nextPlanIndex = content.IndexOf("下一步诊疗计划", index, StringComparison.Ordinal);
            if (nextPlanIndex < 0)
                nextPlanIndex = content.IndexOf("\n", index + label.Length, StringComparison.Ordinal);

            if (nextPlanIndex > index)
                return content[..(index + label.Length)] + analysis + Environment.NewLine + content[nextPlanIndex..].TrimStart();

            return content[..(index + label.Length)] + analysis;
        }

        return content.TrimEnd() + Environment.NewLine + $"评估分析：{analysis}" + Environment.NewLine;
    }

    private int HighlightDuplicates()
    {
        var history = _allRecords
            .Where(r => !ReferenceEquals(r, Record) && r.Id != Record.Id)
            .Select(r => r.Content)
            .ToList();
        var duplicateTexts = _generatorService.GetDuplicateTexts(txtContent.Text, history);

        txtContent.SelectAll();
        txtContent.SelectionColor = Color.Black;
        foreach (var duplicateText in duplicateTexts)
        {
            var start = 0;
            while (start < txtContent.TextLength)
            {
                var index = txtContent.Text.IndexOf(duplicateText, start, StringComparison.Ordinal);
                if (index < 0)
                    break;
                txtContent.Select(index, duplicateText.Length);
                txtContent.SelectionColor = Color.Red;
                start = index + duplicateText.Length;
            }
        }

        txtContent.Select(0, 0);
        lblStatus.Text = duplicateTexts.Count > 0 ? $"检测到 {duplicateTexts.Count} 处可能重复，已标红" : "未检测到明显重复";
        lblStatus.ForeColor = duplicateTexts.Count > 0 ? Color.FromArgb(220, 53, 69) : Color.FromArgb(40, 167, 69);
        return duplicateTexts.Count;
    }

    private void SaveAndClose()
    {
        Record.RecordDate = dtpRecordDate.Value;
        Record.RecordType = string.IsNullOrWhiteSpace(cmbRecordType.Text) ? Record.RecordType : cmbRecordType.Text.Trim();
        Record.Content = txtContent.Text;
        Record.Summary = Record.Content.Length > 100 ? Record.Content[..100] + "..." : Record.Content;
        var duplicateCount = HighlightDuplicates();
        Record.HasDuplicate = duplicateCount > 0;
        DialogResult = DialogResult.OK;
    }
}
