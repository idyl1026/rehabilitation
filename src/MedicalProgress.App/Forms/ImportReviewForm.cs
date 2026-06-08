using System.ComponentModel;
using MedicalProgress.App.Helpers;
using MedicalProgress.App.Models;
using MedicalProgress.App.Services;

namespace MedicalProgress.App.Forms;

public class ImportReviewForm : Form
{
    private readonly Patient? _patient;
    private readonly DatabaseService _databaseService;
    private readonly DocumentImportService _importService;
    private readonly ClinicalChangeAnalyzerService _analyzerService;

    private ImportedDocument? _document;
    private ParsedClinicalDocument? _clinicalDocument;
    private BindingList<StructuredExamResult> _results = new();

    private TextBox txtRawText = null!;
    private TextBox txtNormalizedText = null!;
    private DataGridView gridResults = null!;
    private TextBox txtAnalysis = null!;
    private Label lblStatus = null!;
    private Button btnPaste = null!;
    private Button btnOpenFile = null!;
    private Button btnAnalyze = null!;
    private Button btnSave = null!;
    private Button btnClose = null!;

    public ImportReviewForm(Patient? patient, DatabaseService databaseService)
    {
        _patient = patient;
        _databaseService = databaseService;
        _importService = new DocumentImportService();
        _analyzerService = new ClinicalChangeAnalyzerService();

        InitializeComponent();
    }

    private void InitializeComponent()
    {
        Text = "检查资料导入与解析";
        Size = new Size(1300, 820);
        StartPosition = FormStartPosition.CenterParent;
        BackColor = MedStyleHelper.ContentBg;
        MinimizeBox = false;

        var patientName = _patient == null ? "未选择患者" : $"{_patient.Name} / {_patient.MedicalRecordNumber}";
        var header = MedStyleHelper.CreateHeader("检查资料导入与解析", patientName);

        var topPanel = new Panel
        {
            Dock = DockStyle.Top,
            Height = 52,
            BackColor = Color.White,
            Padding   = new Padding(10, 8, 10, 8)
        };

        var lblTitle = new Label
        {
            Text = $"患者：{patientName}",
            Dock = DockStyle.Top,
            Height = 0,
            Visible = false
        };

        var buttonPanel = new FlowLayoutPanel
        {
            Dock          = DockStyle.Fill,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents  = false,
            BackColor     = Color.Transparent
        };

        btnPaste = MedStyleHelper.CreateSecondaryBtn("粘贴文本", 90);
        btnPaste.Click += BtnPaste_Click;

        btnOpenFile = MedStyleHelper.CreateSecondaryBtn("打开文件", 90);
        btnOpenFile.Click += BtnOpenFile_Click;

        btnAnalyze = MedStyleHelper.CreatePrimaryBtn("生成分析", 100);
        btnAnalyze.Enabled = false;
        btnAnalyze.Click += BtnAnalyze_Click;

        btnSave = MedStyleHelper.CreateSuccessBtn("保存到档案", 110);
        btnSave.Enabled = false;
        btnSave.Click += BtnSave_Click;

        btnClose = MedStyleHelper.CreateSecondaryBtn("关闭", 80);
        btnClose.Click += (_, _) => Close();

        lblStatus = new Label
        {
            AutoSize  = false,
            Width     = 400,
            Height    = 32,
            TextAlign = ContentAlignment.MiddleLeft,
            Font      = MedStyleHelper.FontSmall,
            ForeColor = MedStyleHelper.TextGray,
            Margin    = new Padding(12, 2, 0, 0)
        };

        buttonPanel.Controls.Add(btnPaste);
        buttonPanel.Controls.Add(btnOpenFile);
        buttonPanel.Controls.Add(btnAnalyze);
        buttonPanel.Controls.Add(btnSave);
        buttonPanel.Controls.Add(btnClose);
        buttonPanel.Controls.Add(lblStatus);

        topPanel.Controls.Add(buttonPanel);
        topPanel.Controls.Add(lblTitle);

        var splitMain = new SplitContainer
        {
            Dock             = DockStyle.Fill,
            Orientation      = Orientation.Horizontal,
            SplitterDistance = 300,
            BackColor        = MedStyleHelper.BorderColor
        };

        var splitBottom = new SplitContainer
        {
            Dock = DockStyle.Fill,
            Orientation = Orientation.Vertical,
            SplitterDistance = 760
        };

        var splitText = new SplitContainer
        {
            Dock = DockStyle.Fill,
            Orientation = Orientation.Vertical,
            SplitterDistance = 560
        };

        txtRawText = CreateTextBox();
        txtNormalizedText = CreateTextBox();

        splitText.Panel1.Controls.Add(WrapWithTitle("原始文本", txtRawText));
        splitText.Panel2.Controls.Add(WrapWithTitle("清洗后文本", txtNormalizedText));

        gridResults = new DataGridView
        {
            Dock = DockStyle.Fill,
            AutoGenerateColumns = false,
            AllowUserToAddRows = true,
            AllowUserToDeleteRows = true,
            BackgroundColor = Color.White,
            BorderStyle = BorderStyle.FixedSingle,
            RowHeadersWidth = 32,
            Font = MedStyleHelper.FontSmall,
            EditMode = DataGridViewEditMode.EditOnEnter
        };

        AddGridColumns();
        gridResults.DataSource = _results;

        splitMain.Panel1.Controls.Add(splitText);
        txtAnalysis = CreateTextBox();
        splitBottom.Panel1.Controls.Add(WrapWithTitle("结构化数据", gridResults));
        splitBottom.Panel2.Controls.Add(WrapWithTitle("病情分析草稿", txtAnalysis));
        splitMain.Panel2.Controls.Add(splitBottom);

        Controls.Add(splitMain);
        Controls.Add(topPanel);
        Controls.Add(header);
    }

    private static TextBox CreateTextBox()
    {
        return new TextBox
        {
            Dock        = DockStyle.Fill,
            Multiline   = true,
            ScrollBars  = ScrollBars.Both,
            WordWrap    = true,
            Font        = MedStyleHelper.FontSmall,
            BorderStyle = BorderStyle.FixedSingle
        };
    }

    private static Control WrapWithTitle(string title, Control content)
    {
        var panel = new Panel
        {
            Dock      = DockStyle.Fill,
            BackColor = Color.White,
            Padding   = new Padding(8)
        };

        var label = new Label
        {
            Text      = title,
            Dock      = DockStyle.Top,
            Height    = 26,
            Font      = MedStyleHelper.FontBold,
            ForeColor = MedStyleHelper.TextDark
        };

        panel.Controls.Add(content);
        panel.Controls.Add(label);
        return panel;
    }

    private void AddGridColumns()
    {
        gridResults.Columns.Add(CreateTextColumn(nameof(StructuredExamResult.ExamDate), "日期", 95));
        gridResults.Columns.Add(CreateTextColumn(nameof(StructuredExamResult.ExamType), "检查类型", 80));
        gridResults.Columns.Add(CreateTextColumn(nameof(StructuredExamResult.ReportName), "报告", 120));
        gridResults.Columns.Add(CreateTextColumn(nameof(StructuredExamResult.ItemName), "项目", 140));
        gridResults.Columns.Add(CreateTextColumn(nameof(StructuredExamResult.ResultValue), "结果", 90));
        gridResults.Columns.Add(CreateTextColumn(nameof(StructuredExamResult.Unit), "单位", 70));
        gridResults.Columns.Add(CreateTextColumn(nameof(StructuredExamResult.ReferenceRange), "参考范围", 110));
        gridResults.Columns.Add(CreateTextColumn(nameof(StructuredExamResult.AbnormalFlag), "状态", 80));
        gridResults.Columns.Add(CreateTextColumn(nameof(StructuredExamResult.Conclusion), "结论", 220));
        gridResults.Columns.Add(CreateTextColumn(nameof(StructuredExamResult.RawLine), "原始行", 280));
    }

    private static DataGridViewTextBoxColumn CreateTextColumn(string propertyName, string header, int width)
    {
        return new DataGridViewTextBoxColumn
        {
            DataPropertyName = propertyName,
            HeaderText = header,
            Width = width
        };
    }

    private void BtnPaste_Click(object? sender, EventArgs e)
    {
        if (!Clipboard.ContainsText())
        {
            MessageBox.Show("Clipboard does not contain text.", "Import", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        var preview = _importService.ImportFromText(Clipboard.GetText(), _patient?.Id);
        LoadPreview(preview);
    }

    private async void BtnOpenFile_Click(object? sender, EventArgs e)
    {
        using var dialog = new OpenFileDialog
        {
            Title = "Open clinical document",
            Filter = "Supported files|*.txt;*.docx;*.pdf|Text files|*.txt|Word documents|*.docx|PDF files|*.pdf|All files|*.*"
        };

        if (dialog.ShowDialog(this) != DialogResult.OK)
            return;

        try
        {
            var preview = await _importService.ImportFromFileAsync(dialog.FileName, _patient?.Id);
            LoadPreview(preview);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Import failed: {ex.Message}", "Import", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private async void BtnSave_Click(object? sender, EventArgs e)
    {
        if (_document == null)
            return;

        try
        {
            gridResults.EndEdit();
            var reviewedResults = _results
                .Where(r => !string.IsNullOrWhiteSpace(r.ItemName) || !string.IsNullOrWhiteSpace(r.Conclusion))
                .ToList();

            await _databaseService.SaveImportedDocumentAsync(_document, reviewedResults);
            lblStatus.Text = $"Saved: {reviewedResults.Count} structured rows.";
            btnSave.Enabled = false;
            DialogResult = DialogResult.OK;
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Save failed: {ex.Message}", "Import", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void LoadPreview(ImportPreview preview)
    {
        _document = preview.Document;
        _clinicalDocument = preview.ClinicalDocument;
        txtRawText.Text = preview.Document.RawText;
        txtNormalizedText.Text = preview.Document.NormalizedText;
        _results = new BindingList<StructuredExamResult>(preview.ExamResults);
        gridResults.DataSource = _results;
        GenerateAnalysisDraft();
        btnAnalyze.Enabled = true;
        btnSave.Enabled = true;
        lblStatus.Text = $"Detected: {preview.Document.DocumentType}, rows: {preview.ExamResults.Count}. Please review before saving.";
    }

    private void BtnAnalyze_Click(object? sender, EventArgs e)
    {
        GenerateAnalysisDraft();
    }

    private void GenerateAnalysisDraft()
    {
        if (_document == null || _clinicalDocument == null)
        {
            txtAnalysis.Text = string.Empty;
            return;
        }

        gridResults.EndEdit();
        var suggestions = _analyzerService.Analyze(_clinicalDocument, _results, _document.NormalizedText);
        txtAnalysis.Text = _analyzerService.BuildAnalysisDraft(suggestions);
    }
}
