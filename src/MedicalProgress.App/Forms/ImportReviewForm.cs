using System.ComponentModel;
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
        Text = "Import and review clinical documents";
        Size = new Size(1180, 760);
        StartPosition = FormStartPosition.CenterParent;
        BackColor = Color.FromArgb(245, 247, 250);
        MinimizeBox = false;

        var topPanel = new Panel
        {
            Dock = DockStyle.Top,
            Height = 88,
            BackColor = Color.White,
            Padding = new Padding(12)
        };

        var patientName = _patient == null ? "No patient selected" : $"{_patient.Name} / {_patient.MedicalRecordNumber}";
        var lblTitle = new Label
        {
            Text = $"Document import review - {patientName}",
            Dock = DockStyle.Top,
            Height = 28,
            Font = new Font("Microsoft YaHei UI", 11, FontStyle.Bold),
            ForeColor = Color.FromArgb(35, 35, 35)
        };

        var buttonPanel = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = false,
            Padding = new Padding(0, 8, 0, 0)
        };

        btnPaste = CreateButton("Paste text", Color.FromArgb(0, 123, 215));
        btnPaste.Click += BtnPaste_Click;

        btnOpenFile = CreateButton("Open file", Color.FromArgb(40, 167, 69));
        btnOpenFile.Click += BtnOpenFile_Click;

        btnAnalyze = CreateButton("Generate analysis", Color.FromArgb(111, 66, 193));
        btnAnalyze.Enabled = false;
        btnAnalyze.Click += BtnAnalyze_Click;

        btnSave = CreateButton("Save reviewed data", Color.FromArgb(108, 117, 125));
        btnSave.Enabled = false;
        btnSave.Click += BtnSave_Click;

        btnClose = CreateButton("Close", Color.FromArgb(90, 90, 90));
        btnClose.Click += (_, _) => Close();

        lblStatus = new Label
        {
            AutoSize = false,
            Width = 450,
            Height = 36,
            TextAlign = ContentAlignment.MiddleLeft,
            Font = new Font("Microsoft YaHei UI", 9),
            ForeColor = Color.FromArgb(80, 80, 80),
            Margin = new Padding(12, 3, 0, 0)
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
            Dock = DockStyle.Fill,
            Orientation = Orientation.Horizontal,
            SplitterDistance = 300,
            BackColor = Color.FromArgb(230, 235, 240)
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

        splitText.Panel1.Controls.Add(WrapWithTitle("Original text", txtRawText));
        splitText.Panel2.Controls.Add(WrapWithTitle("Normalized text", txtNormalizedText));

        gridResults = new DataGridView
        {
            Dock = DockStyle.Fill,
            AutoGenerateColumns = false,
            AllowUserToAddRows = true,
            AllowUserToDeleteRows = true,
            BackgroundColor = Color.White,
            BorderStyle = BorderStyle.FixedSingle,
            RowHeadersWidth = 32,
            Font = new Font("Microsoft YaHei UI", 9),
            EditMode = DataGridViewEditMode.EditOnEnter
        };

        AddGridColumns();
        gridResults.DataSource = _results;

        splitMain.Panel1.Controls.Add(splitText);
        txtAnalysis = CreateTextBox();
        splitBottom.Panel1.Controls.Add(WrapWithTitle("Structured exam results", gridResults));
        splitBottom.Panel2.Controls.Add(WrapWithTitle("Analysis draft", txtAnalysis));
        splitMain.Panel2.Controls.Add(splitBottom);

        Controls.Add(splitMain);
        Controls.Add(topPanel);
    }

    private static Button CreateButton(string text, Color color)
    {
        var button = new Button
        {
            Text = text,
            Width = 150,
            Height = 34,
            BackColor = color,
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            Margin = new Padding(0, 3, 8, 3),
            Font = new Font("Microsoft YaHei UI", 9)
        };
        button.FlatAppearance.BorderSize = 0;
        return button;
    }

    private static TextBox CreateTextBox()
    {
        return new TextBox
        {
            Dock = DockStyle.Fill,
            Multiline = true,
            ScrollBars = ScrollBars.Both,
            WordWrap = true,
            Font = new Font("Consolas", 10),
            BorderStyle = BorderStyle.FixedSingle
        };
    }

    private static Control WrapWithTitle(string title, Control content)
    {
        var panel = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = Color.White,
            Padding = new Padding(10)
        };

        var label = new Label
        {
            Text = title,
            Dock = DockStyle.Top,
            Height = 28,
            Font = new Font("Microsoft YaHei UI", 10, FontStyle.Bold),
            ForeColor = Color.FromArgb(45, 45, 45)
        };

        panel.Controls.Add(content);
        panel.Controls.Add(label);
        return panel;
    }

    private void AddGridColumns()
    {
        gridResults.Columns.Add(CreateTextColumn(nameof(StructuredExamResult.ExamDate), "Exam date", 95));
        gridResults.Columns.Add(CreateTextColumn(nameof(StructuredExamResult.ExamType), "Type", 80));
        gridResults.Columns.Add(CreateTextColumn(nameof(StructuredExamResult.ReportName), "Report", 120));
        gridResults.Columns.Add(CreateTextColumn(nameof(StructuredExamResult.ItemName), "Item", 140));
        gridResults.Columns.Add(CreateTextColumn(nameof(StructuredExamResult.ResultValue), "Result", 90));
        gridResults.Columns.Add(CreateTextColumn(nameof(StructuredExamResult.Unit), "Unit", 70));
        gridResults.Columns.Add(CreateTextColumn(nameof(StructuredExamResult.ReferenceRange), "Reference", 110));
        gridResults.Columns.Add(CreateTextColumn(nameof(StructuredExamResult.AbnormalFlag), "Flag", 80));
        gridResults.Columns.Add(CreateTextColumn(nameof(StructuredExamResult.Conclusion), "Conclusion", 220));
        gridResults.Columns.Add(CreateTextColumn(nameof(StructuredExamResult.RawLine), "Raw line", 280));
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
