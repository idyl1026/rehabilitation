using System.Text;
using MedicalProgress.App.Models;
using MedicalProgress.App.Services;

namespace MedicalProgress.App.Forms;

public class PatientTimelineForm : Form
{
    private readonly Patient _patient;
    private readonly List<ProgressRecord> _records;
    private readonly DatabaseService _databaseService;
    private readonly PatientFolderService _folderService;
    private readonly GeneratorService _generatorService = new();
    private readonly WordExportService _wordExportService = new();

    private ListView lvRecords = null!;
    private RichTextBox txtViewer = null!;
    private Label lblStatus = null!;

    public PatientTimelineForm(Patient patient, List<ProgressRecord> records, DatabaseService databaseService, PatientFolderService folderService)
    {
        _patient = patient;
        _records = records;
        _databaseService = databaseService;
        _folderService = folderService;
        InitializeComponent();
        LoadRecords();
        BuildCombinedView();
    }

    private void InitializeComponent()
    {
        Text = $"{_patient.Name} - 联合浏览";
        Size = new Size(1180, 780);
        StartPosition = FormStartPosition.CenterParent;
        BackColor = Color.White;

        var top = new FlowLayoutPanel
        {
            Dock = DockStyle.Top,
            Height = 56,
            Padding = new Padding(12),
            BackColor = Color.FromArgb(245, 247, 250),
            WrapContents = false
        };

        var btnCombined = CreateButton("联合浏览", Color.FromArgb(0, 123, 215));
        btnCombined.Click += (_, _) => BuildCombinedView();
        var btnDuplicate = CreateButton("检测重复", Color.FromArgb(255, 193, 7));
        btnDuplicate.Click += (_, _) => CheckDuplicatesInCurrentView();
        var btnExportWord = CreateButton("导出Word", Color.FromArgb(40, 167, 69));
        btnExportWord.Click += async (_, _) => await ExportWordAsync();
        var btnClose = CreateButton("关闭", Color.FromArgb(108, 117, 125));
        btnClose.Click += (_, _) => Close();

        lblStatus = new Label
        {
            AutoSize = true,
            Font = new Font("Microsoft YaHei UI", 9),
            Margin = new Padding(10, 10, 0, 0),
            ForeColor = Color.FromArgb(108, 117, 125)
        };

        top.Controls.Add(btnCombined);
        top.Controls.Add(btnDuplicate);
        top.Controls.Add(btnExportWord);
        top.Controls.Add(btnClose);
        top.Controls.Add(lblStatus);

        var split = new SplitContainer
        {
            Dock = DockStyle.Fill,
            SplitterDistance = 260,
            BackColor = Color.FromArgb(230, 235, 240)
        };

        lvRecords = new ListView
        {
            Dock = DockStyle.Fill,
            View = View.Details,
            FullRowSelect = true,
            GridLines = true,
            Font = new Font("Microsoft YaHei UI", 9)
        };
        lvRecords.Columns.Add("日期", 135);
        lvRecords.Columns.Add("类型", 150);
        lvRecords.Columns.Add("提示", 70);
        lvRecords.SelectedIndexChanged += (_, _) => ShowSelectedRecord();
        lvRecords.DoubleClick += LvRecords_DoubleClick;

        txtViewer = new RichTextBox
        {
            Dock = DockStyle.Fill,
            ReadOnly = true,
            BorderStyle = BorderStyle.None,
            Font = new Font("Microsoft YaHei UI", 10),
            ScrollBars = RichTextBoxScrollBars.Both,
            WordWrap = true,
            DetectUrls = false
        };

        split.Panel1.Controls.Add(lvRecords);
        split.Panel2.Controls.Add(txtViewer);
        Controls.Add(split);
        Controls.Add(top);
    }

    private static Button CreateButton(string text, Color color) => new()
    {
        Text = text,
        Width = 110,
        Height = 32,
        BackColor = color,
        ForeColor = Color.White,
        FlatStyle = FlatStyle.Flat,
        Font = new Font("Microsoft YaHei UI", 9),
        Margin = new Padding(0, 0, 8, 0)
    };

    private void LoadRecords()
    {
        lvRecords.Items.Clear();
        foreach (var record in _records.OrderBy(r => r.RecordDate))
        {
            var item = new ListViewItem(record.RecordDate.ToString("yyyy-MM-dd HH:mm"));
            item.SubItems.Add(record.RecordType);
            item.SubItems.Add(record.HasDuplicate ? "重复" : string.Empty);
            item.Tag = record;
            lvRecords.Items.Add(item);
        }

        lblStatus.Text = _records.Count == 0 ? "暂无病程记录" : $"共 {_records.Count} 条病程，双击可修改";
    }

    private void BuildCombinedView()
    {
        txtViewer.ReadOnly = false;
        txtViewer.Text = BuildTimelineText();
        txtViewer.SelectAll();
        txtViewer.SelectionColor = Color.Black;
        txtViewer.ReadOnly = true;
        lblStatus.Text = "正在联合浏览首次病程录和病程记录";
    }

    private string BuildTimelineText()
    {
        var sb = new StringBuilder();
        sb.AppendLine($"患者：{_patient.Name}  {_patient.Gender}  {_patient.Age}岁");
        sb.AppendLine($"住院号：{_patient.MedicalRecordNumber}  床号：{_patient.BedNumber}  科室：{_patient.Department}");
        sb.AppendLine($"医生：{_patient.AttendingDoctor}");
        sb.AppendLine($"入院诊断：{_patient.Diagnosis}");
        sb.AppendLine(new string('=', 80));
        sb.AppendLine();

        var firstRecord = FindFirstProgressRecord();
        sb.AppendLine(firstRecord == null ? "首程资料（来自患者档案）" : $"{firstRecord.RecordDate:yyyy-MM-dd HH:mm}  首次病程录");
        sb.AppendLine(new string('-', 80));
        sb.AppendLine(firstRecord?.Content ?? BuildPatientArchiveText());
        sb.AppendLine();
        sb.AppendLine(new string('=', 80));
        sb.AppendLine();

        foreach (var record in _records.OrderBy(r => r.RecordDate).Where(r => firstRecord == null || r.Id != firstRecord.Id))
        {
            sb.AppendLine($"{record.RecordDate:yyyy-MM-dd HH:mm}  {record.RecordType}");
            sb.AppendLine(new string('-', 80));
            sb.AppendLine(record.Content);
            sb.AppendLine();
            sb.AppendLine(new string('=', 80));
            sb.AppendLine();
        }

        if (_records.Count == 0)
            sb.AppendLine("暂无病程记录。");

        return sb.ToString();
    }

    private ProgressRecord? FindFirstProgressRecord()
    {
        return _records
            .OrderBy(r => r.RecordDate)
            .FirstOrDefault(r => r.RecordType.Contains("首次", StringComparison.Ordinal)
                || r.RecordType.Contains("首程", StringComparison.Ordinal)
                || r.Content.Contains("首次病程", StringComparison.Ordinal)
                || r.Content.Contains("首次病程录", StringComparison.Ordinal));
    }

    private string BuildPatientArchiveText()
    {
        return $"""
主诉：{EmptyAsNone(_patient.ChiefComplaint)}

现病史：
{EmptyAsNone(_patient.History)}

查体：
{EmptyAsNone(_patient.PhysicalExam)}

辅助检查：
{EmptyAsNone(_patient.AuxiliaryExam)}

诊疗计划：
{EmptyAsNone(_patient.TreatmentPlan)}
""";
    }

    private static string EmptyAsNone(string value)
    {
        return string.IsNullOrWhiteSpace(value) ? "暂无。" : value.Trim();
    }

    private void ShowSelectedRecord()
    {
        if (lvRecords.SelectedItems.Count == 0 || lvRecords.SelectedItems[0].Tag is not ProgressRecord record)
            return;

        txtViewer.ReadOnly = false;
        txtViewer.Text = record.Content;
        txtViewer.SelectAll();
        txtViewer.SelectionColor = Color.Black;
        txtViewer.ReadOnly = true;
        lblStatus.Text = $"当前查看：{record.RecordDate:yyyy-MM-dd HH:mm} {record.RecordType}，双击左侧条目可修改";
    }

    private async void LvRecords_DoubleClick(object? sender, EventArgs e)
    {
        if (lvRecords.SelectedItems.Count == 0 || lvRecords.SelectedItems[0].Tag is not ProgressRecord record)
            return;

        using var form = new ProgressRecordEditForm(_patient, record, _records);
        if (form.ShowDialog(this) != DialogResult.OK)
            return;

        await _databaseService.UpdateProgressRecordAsync(record);
        await _folderService.SaveProgressRecordAsync(_patient, record);
        LoadRecords();
        SelectRecord(record);
        ShowSelectedRecord();
        lblStatus.Text = "病程修改已保存";
    }

    private void SelectRecord(ProgressRecord record)
    {
        foreach (ListViewItem item in lvRecords.Items)
        {
            if (ReferenceEquals(item.Tag, record))
            {
                item.Selected = true;
                item.EnsureVisible();
                break;
            }
        }
    }

    private void CheckDuplicatesInCurrentView()
    {
        if (lvRecords.SelectedItems.Count == 0 || lvRecords.SelectedItems[0].Tag is not ProgressRecord selected)
        {
            CheckDuplicatesForCombinedView();
            return;
        }

        var history = _records
            .Where(r => !ReferenceEquals(r, selected) && r.Id != selected.Id)
            .Select(r => r.Content)
            .ToList();
        var firstRecord = FindFirstProgressRecord();
        if (firstRecord == null)
            history.Add(BuildPatientArchiveText());
        var duplicateTexts = _generatorService.GetDuplicateTexts(selected.Content, history);
        MarkDuplicateTexts(duplicateTexts);
        lblStatus.Text = duplicateTexts.Count > 0
            ? $"当前病程检测到 {duplicateTexts.Count} 处可能重复，已标红"
            : "当前病程未检测到明显重复";
    }

    private void CheckDuplicatesForCombinedView()
    {
        BuildCombinedView();
        lvRecords.SelectedItems.Clear();
        var duplicateTexts = new List<string>();
        var firstRecord = FindFirstProgressRecord();
        var firstText = firstRecord?.Content ?? BuildPatientArchiveText();
        foreach (var record in _records)
        {
            var history = _records
                .Where(r => !ReferenceEquals(r, record) && r.Id != record.Id)
                .Select(r => r.Content)
                .ToList();
            if (firstRecord == null)
                history.Add(firstText);
            duplicateTexts.AddRange(_generatorService.GetDuplicateTexts(record.Content, history));
        }

        if (_records.Count > 0)
            duplicateTexts.AddRange(_generatorService.GetDuplicateTexts(firstText, _records.Select(r => r.Content).ToList()));

        MarkDuplicateTexts(duplicateTexts.Distinct().ToList());
        lblStatus.Text = duplicateTexts.Count > 0
            ? $"联合浏览检测到 {duplicateTexts.Distinct().Count()} 处可能重复，已标红"
            : "联合浏览未检测到明显重复";
    }

    private void MarkDuplicateTexts(List<string> duplicateTexts)
    {
        txtViewer.ReadOnly = false;
        txtViewer.SelectAll();
        txtViewer.SelectionColor = Color.Black;

        foreach (var duplicateText in duplicateTexts)
        {
            var start = 0;
            while (start < txtViewer.TextLength)
            {
                var index = txtViewer.Text.IndexOf(duplicateText, start, StringComparison.Ordinal);
                if (index < 0)
                    break;
                txtViewer.Select(index, duplicateText.Length);
                txtViewer.SelectionColor = Color.Red;
                start = index + duplicateText.Length;
            }
        }

        txtViewer.Select(0, 0);
        txtViewer.ReadOnly = true;
    }

    private async Task ExportWordAsync()
    {
        try
        {
            if (string.IsNullOrWhiteSpace(txtViewer.Text))
                BuildCombinedView();

            var folderPath = _folderService.GetPatientFolder(_patient);
            var filePath = await _wordExportService.ExportCombinedTimelineAsync(_patient, txtViewer.Text, folderPath);
            lblStatus.Text = $"已导出Word：{Path.GetFileName(filePath)}";
            MessageBox.Show($"已导出Word文件：\n{filePath}", "导出完成", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"导出Word失败：{ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }
}
