using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using MedicalProgress.App.Models;
using MedicalProgress.App.Services;
using MedicalProgress.App.Data;
using Microsoft.EntityFrameworkCore;

namespace MedicalProgress.App.Forms;

public class NewProgressRecordForm : Form
{
    private readonly Patient _patient;
    private readonly List<ProgressRecord> _records;
    private readonly ProgressRecord? _existingRecord;
    private readonly bool _isEditMode;
    private readonly string? _initialContent;

    private readonly ClinicalTextNormalizeService _normalizeService = new();
    private readonly ClinicalDocumentSectionParserService _sectionParserService = new();
    private readonly ExamResultParserService _examParserService = new();
    private readonly ClinicalChangeAnalyzerService _analyzerService = new();
    private readonly GeneratorService _generatorService = new();
    private readonly DatabaseService  _databaseService  = new();
    private readonly BM25Service      _bm25Service      = new();

    private List<KnowledgeTemplate> _knowledgeTemplates   = [];
    private List<KnowledgeTemplate> _recommendedTemplates  = [];
    private ListBox lstRecommendations = null!;
    private readonly System.Windows.Forms.Timer _recommendTimer = new() { Interval = 1200 };

    private Label lblPatientInfo = null!;
    private DateTimePicker dtpRecordDate = null!;
    private ComboBox cmbRecordType = null!;
    private TextBox txtSubjective = null!;
    private TextBox txtPhysicalExam = null!;
    private TextBox txtDoctorName = null!;
    private TextBox txtLabResults = null!;
    private TextBox txtExamResults = null!;
    private TextBox txtOrderChanges = null!;
    private TextBox txtAnalysis = null!;
    private TextBox txtDuplicateSources = null!;
    private RichTextBox txtPreview = null!;
    private SplitContainer mainSplit = null!;

    // 自动检查防抖计时器
    private readonly System.Windows.Forms.Timer _checkTimer = new() { Interval = 700 };

    [DllImport("user32.dll")]
    private static extern bool SendMessage(IntPtr hWnd, int msg, bool wParam, int lParam);
    [DllImport("user32.dll")]
    private static extern IntPtr SendMessage(IntPtr hWnd, int msg, IntPtr wParam, ref Point lParam);
    private const int WM_SETREDRAW    = 0x000B;
    private const int EM_GETSCROLLPOS = 0x04DD;
    private const int EM_SETSCROLLPOS = 0x04DE;

    private ParsedClinicalDocument _firstProgress = new();
    private string _firstProgressRawText = string.Empty;

    public ProgressRecord? Record { get; private set; }

    // 新建模式（可选传入预填内容）
    public NewProgressRecordForm(Patient patient, List<ProgressRecord> records, string? initialContent = null)
    {
        _patient = patient;
        _records = records;
        _isEditMode = false;
        _initialContent = initialContent;
        InitializeComponent();
        LoadFirstProgressContext();
        FillDefaults();
    }

    // 编辑模式
    public NewProgressRecordForm(Patient patient, ProgressRecord existingRecord, List<ProgressRecord> allRecords)
    {
        _patient = patient;
        _existingRecord = existingRecord;
        _isEditMode = true;
        _records = allRecords.Where(r => r.Id != existingRecord.Id).ToList();
        InitializeComponent();
        LoadFirstProgressContext();
        FillDefaults();
    }

    private void InitializeComponent()
    {
        Text = _isEditMode ? "修改病程记录" : "新建病程记录";
        Size = new Size(1440, 920);
        StartPosition = FormStartPosition.CenterParent;
        BackColor = Color.FromArgb(245, 247, 250);
        MinimizeBox = false;

        // ── 顶部患者信息 + 按钮行 ──────────────────────────────
        var topPanel = new Panel
        {
            Dock      = DockStyle.Top,
            Height    = 98,
            BackColor = Color.White,
            Padding   = new Padding(14)
        };

        lblPatientInfo = new Label
        {
            Dock      = DockStyle.Top,
            Height    = 30,
            Font      = new Font("Microsoft YaHei UI", 10, FontStyle.Bold),
            ForeColor = Color.FromArgb(35, 35, 35)
        };

        var topInputs = new FlowLayoutPanel
        {
            Dock          = DockStyle.Fill,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents  = false,
            Padding       = new Padding(0, 10, 0, 0)
        };

        dtpRecordDate = new DateTimePicker
        {
            Width        = 180,
            Format       = DateTimePickerFormat.Custom,
            CustomFormat = "yyyy-MM-dd HH:mm",
            Value        = DateTime.Now,
            Font         = new Font("Microsoft YaHei UI", 9),
            Margin       = new Padding(0, 4, 12, 0)
        };

        cmbRecordType = new ComboBox
        {
            Width         = 180,
            DropDownStyle = ComboBoxStyle.DropDownList,
            Font          = new Font("Microsoft YaHei UI", 9),
            Margin        = new Padding(0, 4, 12, 0)
        };
        cmbRecordType.Items.AddRange(new object[] { "主任医师查房记录", "副主任医师查房记录", "主治查房", "病程记录" });
        cmbRecordType.SelectedIndex = 0;

        var btnAnalyze  = CreateButton("生成分析",   Color.FromArgb(111, 66,  193));
        var btnPreview  = CreateButton("生成预览",   Color.FromArgb(0,   123, 215));
        var btnBrowse   = CreateButton("联合浏览",   Color.FromArgb(23,  162, 184));
        var btnTemplate = CreateButton("套用模板",   Color.FromArgb(96,  108, 118));
        var btnSave     = CreateButton(_isEditMode ? "保存修改" : "保存病程", Color.FromArgb(40, 167, 69));
        var btnCancel   = CreateButton("取消",       Color.FromArgb(108, 117, 125));

        btnAnalyze.Click  += (_, _) => GenerateAnalysis();
        btnPreview.Click  += (_, _) => BuildPreview();
        btnBrowse.Click   += (_, _) => OpenCombinedBrowse();
        btnTemplate.Click += BtnUseTemplate_Click;
        btnSave.Click     += BtnSave_Click;
        btnCancel.Click   += (_, _) => DialogResult = DialogResult.Cancel;

        txtDoctorName = new TextBox
        {
            Width       = 130,
            Font        = new Font("Microsoft YaHei UI", 9),
            PlaceholderText = "查房医师姓名",
            Margin      = new Padding(0, 4, 12, 0)
        };

        topInputs.Controls.Add(new Label { Text = "记录日期", AutoSize = true, Height = 30, TextAlign = ContentAlignment.MiddleLeft, Margin = new Padding(0, 8, 8, 0) });
        topInputs.Controls.Add(dtpRecordDate);
        topInputs.Controls.Add(new Label { Text = "记录类型", AutoSize = true, Height = 30, TextAlign = ContentAlignment.MiddleLeft, Margin = new Padding(0, 8, 8, 0) });
        topInputs.Controls.Add(cmbRecordType);
        topInputs.Controls.Add(new Label { Text = "查房医师", AutoSize = true, Height = 30, TextAlign = ContentAlignment.MiddleLeft, Margin = new Padding(0, 8, 8, 0) });
        topInputs.Controls.Add(txtDoctorName);
        topInputs.Controls.Add(btnAnalyze);
        topInputs.Controls.Add(btnPreview);
        topInputs.Controls.Add(btnBrowse);
        topInputs.Controls.Add(btnTemplate);
        topInputs.Controls.Add(btnSave);
        topInputs.Controls.Add(btnCancel);

        topPanel.Controls.Add(topInputs);
        topPanel.Controls.Add(lblPatientInfo);

        // ── 主体左右分割 ──────────────────────────────────────
        mainSplit = new SplitContainer
        {
            Dock          = DockStyle.Fill,
            Orientation   = Orientation.Vertical,
            Panel1MinSize = 160,
            Panel2MinSize = 60,   // Load 后再设为真实值
            BackColor     = Color.FromArgb(230, 235, 240)
        };

        // 左侧：输入字段
        var inputPanel = new TableLayoutPanel
        {
            Dock        = DockStyle.Fill,
            ColumnCount = 1,
            RowCount    = 6,
            BackColor   = Color.White,
            Padding     = new Padding(12)
        };
        inputPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 17));
        inputPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 20));
        inputPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 14));
        inputPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 14));
        inputPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 11));
        inputPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 24));

        txtSubjective  = CreateMultilineTextBox();
        txtPhysicalExam = CreateMultilineTextBox();
        txtLabResults  = CreateMultilineTextBox();
        txtExamResults = CreateMultilineTextBox();
        txtOrderChanges = CreateMultilineTextBox();
        txtAnalysis    = CreateMultilineTextBox();

        txtDuplicateSources           = CreateMultilineTextBox();
        txtDuplicateSources.ReadOnly  = true;
        txtDuplicateSources.BackColor = Color.FromArgb(255, 245, 245);

        txtPreview = CreatePreviewTextBox();

        inputPanel.Controls.Add(WrapWithTitle("本次病情变化 / 主观症状", txtSubjective), 0, 0);
        inputPanel.Controls.Add(WrapWithTitle("本次查体", txtPhysicalExam), 0, 1);
        inputPanel.Controls.Add(WrapWithTitle("检验结果（血尿便等化验）", txtLabResults), 0, 2);
        inputPanel.Controls.Add(WrapWithTitle("检查结果（影像、心电图等）", txtExamResults), 0, 3);
        inputPanel.Controls.Add(WrapWithTitle("医嘱 / 治疗变化", txtOrderChanges), 0, 4);
        inputPanel.Controls.Add(WrapWithTitle("自动分析草稿", txtAnalysis), 0, 5);

        mainSplit.Panel1.Controls.Add(inputPanel);

        // ── 推荐模板面板（Panel1 底部）──────────────────────────
        lstRecommendations = new ListBox
        {
            Dock        = DockStyle.Fill,
            Font        = new Font("Microsoft YaHei UI", 8.5f),
            BorderStyle = BorderStyle.None,
            BackColor   = Color.FromArgb(250, 252, 255),
            ItemHeight  = 38,
            DrawMode    = DrawMode.OwnerDrawFixed
        };
        lstRecommendations.DrawItem    += LstRecommendations_DrawItem;
        lstRecommendations.DoubleClick += LstRecommendations_DoubleClick;

        var pnlRecommend = new Panel
        {
            Dock      = DockStyle.Bottom,
            Height    = 168,
            BackColor = Color.FromArgb(248, 252, 255)
        };
        var lblRec = new Label
        {
            Text      = "推荐模板（相关度排序，双击插入预览）",
            Dock      = DockStyle.Top,
            Height    = 24,
            Font      = new Font("Microsoft YaHei UI", 8.5f, FontStyle.Bold),
            ForeColor = Color.FromArgb(21, 101, 192),
            BackColor = Color.FromArgb(225, 240, 255),
            TextAlign = ContentAlignment.MiddleLeft,
            Padding   = new Padding(8, 0, 0, 0)
        };
        var sepRec = new Panel { Dock = DockStyle.Top, Height = 1, BackColor = Color.FromArgb(180, 215, 245) };
        pnlRecommend.Controls.Add(lstRecommendations);
        pnlRecommend.Controls.Add(sepRec);
        pnlRecommend.Controls.Add(lblRec);
        mainSplit.Panel1.Controls.Add(pnlRecommend);   // Dock.Bottom → 添加顺序在 inputPanel 后，优先占底部

        // 右侧：预览 + 重复提示（可拖拽分割）
        var rightSplit = new SplitContainer
        {
            Dock          = DockStyle.Fill,
            Orientation   = Orientation.Horizontal,
            Panel1MinSize = 200,
            Panel2MinSize = 60,
            BackColor     = Color.FromArgb(220, 235, 220)
        };
        rightSplit.Panel1.Controls.Add(WrapWithTitle("病程记录（重复内容自动标红，占位符标橙）", txtPreview));
        rightSplit.Panel2.Controls.Add(WrapWithTitle("重复来源提示", txtDuplicateSources));
        Load += (_, _) =>
        {
            mainSplit.Panel2MinSize    = 400;
            mainSplit.SplitterDistance = Math.Max(160, Math.Min(240, mainSplit.Width - 400));
            rightSplit.SplitterDistance = Math.Max(200, (int)(rightSplit.Height * 0.82));
            _ = LoadKnowledgeTemplatesAsync();
        };

        mainSplit.Panel2.Controls.Add(rightSplit);

        Controls.Add(mainSplit);
        Controls.Add(topPanel);

        // 自动检查防抖；窗体关闭时立即停止计时器，防止访问已销毁控件
        _checkTimer.Tick += (_, _) => { _checkTimer.Stop(); RunFullCheck(); };
        txtPreview.TextChanged += OnPreviewTextChanged;

        // 推荐刷新防抖（任意输入字段变化后 1.2s 重排序）
        _recommendTimer.Tick += (_, _) => { _recommendTimer.Stop(); RefreshRecommendations(); };
        foreach (var tb in new TextBox[] { txtSubjective, txtPhysicalExam, txtLabResults, txtExamResults, txtOrderChanges })
            tb.TextChanged += (_, _) => { _recommendTimer.Stop(); _recommendTimer.Start(); };

        FormClosed += (_, _) =>
        {
            _checkTimer.Stop();
            _recommendTimer.Stop();
            txtPreview.TextChanged -= OnPreviewTextChanged;
        };
    }

    // ─────────────────────────────────────────────────────────
    //  BM25 推荐模板
    // ─────────────────────────────────────────────────────────

    private async Task LoadKnowledgeTemplatesAsync()
    {
        try
        {
            using var context = new AppDbContext();
            _knowledgeTemplates = await context.KnowledgeTemplates
                .Where(t => t.IsActive)
                .ToListAsync();
            RefreshRecommendations();
        }
        catch { /* 知识库不可用时静默忽略 */ }
    }

    private void RefreshRecommendations()
    {
        if (IsDisposed || lstRecommendations == null || lstRecommendations.IsDisposed) return;
        if (_knowledgeTemplates.Count == 0) return;

        var query = BuildRecommendQuery();
        if (string.IsNullOrWhiteSpace(query)) return;

        var ranked = _bm25Service.Rank(query, _knowledgeTemplates, topN: 5);
        _recommendedTemplates = ranked.Select(x => x.Template).ToList();

        lstRecommendations.BeginUpdate();
        lstRecommendations.Items.Clear();
        foreach (var (t, score) in ranked)
            lstRecommendations.Items.Add(new RecommendItem(t, score));
        lstRecommendations.EndUpdate();
    }

    private string BuildRecommendQuery()
    {
        var parts = new List<string>();
        if (!string.IsNullOrWhiteSpace(_patient.Diagnosis))   parts.Add(_patient.Diagnosis);
        if (!string.IsNullOrWhiteSpace(txtSubjective.Text))   parts.Add(txtSubjective.Text);
        if (!string.IsNullOrWhiteSpace(txtPhysicalExam.Text)) parts.Add(txtPhysicalExam.Text);
        if (!string.IsNullOrWhiteSpace(txtLabResults.Text))   parts.Add(txtLabResults.Text);
        if (!string.IsNullOrWhiteSpace(txtExamResults.Text))  parts.Add(txtExamResults.Text);
        return string.Join(" ", parts);
    }

    private void LstRecommendations_DrawItem(object? sender, DrawItemEventArgs e)
    {
        if (e.Index < 0 || e.Index >= lstRecommendations.Items.Count) return;
        var item = (RecommendItem)lstRecommendations.Items[e.Index];

        e.DrawBackground();
        var isSelected = (e.State & DrawItemState.Selected) != 0;
        var bgColor = isSelected ? Color.FromArgb(210, 232, 255) : (e.Index % 2 == 0 ? Color.White : Color.FromArgb(248, 252, 255));
        using var bgBrush = new SolidBrush(bgColor);
        e.Graphics.FillRectangle(bgBrush, e.Bounds);

        var titleFont   = new Font("Microsoft YaHei UI", 8.5f, FontStyle.Bold);
        var subFont     = new Font("Microsoft YaHei UI", 7.5f);
        var titleColor  = isSelected ? Color.FromArgb(10, 60, 140) : Color.FromArgb(30, 30, 30);
        var subColor    = Color.FromArgb(100, 120, 150);
        var scoreColor  = Color.FromArgb(21, 101, 192);
        var scoreStr    = $"{item.Score:F1}分";
        var scoreSize   = e.Graphics.MeasureString(scoreStr, subFont);

        using var titleBrush = new SolidBrush(titleColor);
        using var subBrush   = new SolidBrush(subColor);
        using var scoreBrush = new SolidBrush(scoreColor);

        var titleRect = new RectangleF(e.Bounds.X + 8, e.Bounds.Y + 3, e.Bounds.Width - scoreSize.Width - 20, 20);
        var subRect   = new RectangleF(e.Bounds.X + 8, e.Bounds.Y + 21, e.Bounds.Width - 16, 14);

        e.Graphics.DrawString(item.Template.Title, titleFont, titleBrush, titleRect, new StringFormat { Trimming = StringTrimming.EllipsisCharacter });
        e.Graphics.DrawString($"{item.Template.TemplateType}  ·  {item.Template.Category?.Name ?? item.Template.Keywords}", subFont, subBrush, subRect, new StringFormat { Trimming = StringTrimming.EllipsisCharacter });
        e.Graphics.DrawString(scoreStr, subFont, scoreBrush, e.Bounds.Right - scoreSize.Width - 6, e.Bounds.Y + 3);

        titleFont.Dispose();
        subFont.Dispose();
    }

    private void LstRecommendations_DoubleClick(object? sender, EventArgs e)
    {
        var idx = lstRecommendations.SelectedIndex;
        if (idx < 0 || idx >= _recommendedTemplates.Count) return;
        var template = _recommendedTemplates[idx];
        txtPreview.Focus();
        txtPreview.SelectedText = (txtPreview.SelectionStart > 0 ? "\n\n" : "") + template.Content;
        RunFullCheck();
    }

    // 推荐列表项包装
    private sealed record RecommendItem(KnowledgeTemplate Template, float Score)
    {
        public override string ToString() => Template.Title;
    }

    // ─────────────────────────────────────────────────────────
    //  套用模板（知识库）
    // ─────────────────────────────────────────────────────────

    private void BtnUseTemplate_Click(object? sender, EventArgs e)
    {
        using var kb = new KnowledgeBaseForm();
        if (kb.ShowDialog(this) == DialogResult.OK && !string.IsNullOrWhiteSpace(kb.SelectedTemplateContent))
        {
            // 在光标处插入（不替换全文）
            txtPreview.Focus();
            txtPreview.SelectedText = kb.SelectedTemplateContent;
            RunFullCheck();
        }
    }

    // ─────────────────────────────────────────────────────────
    //  数据填充
    // ─────────────────────────────────────────────────────────

    private void LoadFirstProgressContext()
    {
        var firstRecord = _records
            .OrderBy(r => r.RecordDate)
            .FirstOrDefault(r => r.RecordType.Contains("首次") || r.Content.Contains("首次病程"));
        firstRecord ??= _records.OrderBy(r => r.RecordDate).FirstOrDefault();

        _firstProgressRawText = firstRecord?.Content ?? BuildPatientArchiveText();
        var normalized = _normalizeService.Normalize(_firstProgressRawText);
        _firstProgress = _sectionParserService.Parse(normalized);
    }

    private void FillDefaults()
    {
        lblPatientInfo.Text = $"患者：{_patient.Name}  性别：{_patient.Gender}  年龄：{_patient.Age}岁  " +
                              $"住院号：{_patient.MedicalRecordNumber}  诊断：{_patient.Diagnosis}";

        if (_isEditMode && _existingRecord != null)
        {
            dtpRecordDate.Value = _existingRecord.RecordDate;
            var typeItems = cmbRecordType.Items.Cast<object>().Select(o => o.ToString()).ToList();
            var typeIdx = typeItems.IndexOf(_existingRecord.RecordType);
            if (typeIdx >= 0) cmbRecordType.SelectedIndex = typeIdx;
            else { cmbRecordType.Items.Add(_existingRecord.RecordType); cmbRecordType.SelectedIndex = cmbRecordType.Items.Count - 1; }

            // 从已有内容解析结构化字段（保留已有内容）
            ParseExistingContentIntoFields(_existingRecord.Content);

            txtPreview.Text = _existingRecord.Content;
            RunFullCheck();
        }
        else
        {
            txtSubjective.Text   = string.IsNullOrWhiteSpace(_patient.ChiefComplaint) ? _firstProgress.ChiefComplaint : _patient.ChiefComplaint;
            txtPhysicalExam.Text = FirstNonEmpty(_firstProgress.PhysicalExam, _patient.PhysicalExam);
            txtLabResults.Text   = string.Empty;
            txtExamResults.Text  = string.Empty;
            txtOrderChanges.Text = string.Empty;

            if (!string.IsNullOrWhiteSpace(_initialContent))
            {
                txtPreview.Text = _initialContent;
                RunFullCheck();
            }
            else
            {
                GenerateAnalysis();
                BuildPreview();
            }
        }
    }

    // 从已有病程内容解析各字段
    private void ParseExistingContentIntoFields(string content)
    {
        // 查房医师
        var doctorMatch = System.Text.RegularExpressions.Regex.Match(content, @"记录医师：(.+)");
        if (doctorMatch.Success) txtDoctorName.Text = doctorMatch.Groups[1].Value.Trim();

        var subjective = ExtractSection(content, "主观症状：", "客观体征：", "检验结果：", "检查结果：", "评估分析：", "诊疗计划：");
        var physExam   = ExtractSection(content, "客观体征：", "检验结果：", "检查结果：", "医嘱变化：", "评估分析：", "诊疗计划：");
        var labRes     = ExtractSection(content, "检验结果：", "检查结果：", "医嘱变化：", "评估分析：", "诊疗计划：");
        var examRes    = ExtractSection(content, "检查结果：", "医嘱变化：", "评估分析：", "诊疗计划：");
        var orders     = ExtractSection(content, "医嘱变化：", "评估分析：", "诊疗计划：");
        var analysis   = ExtractSection(content, "评估分析：", "诊疗计划：");

        if (!string.IsNullOrWhiteSpace(subjective)) txtSubjective.Text   = subjective;
        else txtSubjective.Text = FirstNonEmpty(_firstProgress.ChiefComplaint, _patient.ChiefComplaint);

        if (!string.IsNullOrWhiteSpace(physExam)) txtPhysicalExam.Text = physExam;
        else txtPhysicalExam.Text = FirstNonEmpty(_firstProgress.PhysicalExam, _patient.PhysicalExam);

        txtLabResults.Text   = labRes;
        txtExamResults.Text  = examRes;
        txtOrderChanges.Text = orders;
        if (!string.IsNullOrWhiteSpace(analysis)) txtAnalysis.Text = analysis;
    }

    private static string ExtractSection(string content, string startMarker, params string[] endMarkers)
    {
        var startIdx = content.IndexOf(startMarker, StringComparison.Ordinal);
        if (startIdx < 0) return string.Empty;
        startIdx += startMarker.Length;

        var endIdx = content.Length;
        foreach (var end in endMarkers)
        {
            var idx = content.IndexOf(end, startIdx, StringComparison.Ordinal);
            if (idx >= 0 && idx < endIdx) endIdx = idx;
        }

        return content[startIdx..endIdx].Trim();
    }

    // ─────────────────────────────────────────────────────────
    //  生成 / 预览
    // ─────────────────────────────────────────────────────────

    private void GenerateAnalysis()
    {
        var combined   = $"主观症状：{txtSubjective.Text}\n客观体征：{txtPhysicalExam.Text}\n检验结果：{txtLabResults.Text}\n检查结果：{txtExamResults.Text}\n医嘱变化：{txtOrderChanges.Text}";
        var normalized = _normalizeService.Normalize(combined);
        var examResults = _examParserService.Parse(normalized, _patient.Id);
        var doc = new ParsedClinicalDocument
        {
            PatientName         = _patient.Name,
            Gender              = _patient.Gender,
            Age                 = _patient.Age.ToString(),
            MedicalRecordNumber = _patient.MedicalRecordNumber,
            Department          = _patient.Department,
            BedNumber           = _patient.BedNumber,
            ChiefComplaint      = txtSubjective.Text.Trim(),
            PhysicalExam        = txtPhysicalExam.Text.Trim(),
            AuxiliaryExam       = txtExamResults.Text.Trim(),
            TreatmentPlan       = txtOrderChanges.Text.Trim()
        };

        var suggestions = _analyzerService.Analyze(doc, examResults, normalized);
        txtAnalysis.Text = RefineAnalysisDraft(_analyzerService.BuildAnalysisDraft(suggestions));
    }

    private void OnPreviewTextChanged(object? sender, EventArgs e)
    {
        _checkTimer.Stop();
        _checkTimer.Start();
    }

    private void BuildPreview()
    {
        if (string.IsNullOrWhiteSpace(txtAnalysis.Text)) GenerateAnalysis();

        var physicalExamText = AdjustPhysicalExamIfRepeated(txtPhysicalExam.Text);

        // 断开事件避免 Text= 触发计时器（BuildPreview 末尾直接调用 RunFullCheck）
        _checkTimer.Stop();
        txtPreview.TextChanged -= OnPreviewTextChanged;
        var doctorLine = string.IsNullOrWhiteSpace(txtDoctorName.Text)
            ? string.Empty : $"\n记录医师：{txtDoctorName.Text.Trim()}";
        txtPreview.Text = $"""
{dtpRecordDate.Value:yyyy-MM-dd HH:mm}  {cmbRecordType.Text}{doctorLine}

患者{_patient.Name}，{_patient.Gender}，{_patient.Age}岁，住院号{_patient.MedicalRecordNumber}。

主观症状：{EmptyAsNone(txtSubjective.Text)}
客观体征：{EmptyAsNone(physicalExamText)}
检验结果：{EmptyAsNone(txtLabResults.Text)}
检查结果：{EmptyAsNone(txtExamResults.Text)}
医嘱变化：{EmptyAsNone(txtOrderChanges.Text)}
评估分析：{EmptyAsNone(txtAnalysis.Text)}
诊疗计划：继续结合患者病情变化及检查结果调整治疗方案，动态观察疗效及不良反应。
""";
        txtPreview.TextChanged += OnPreviewTextChanged;
        RunFullCheck();
    }

    // ─────────────────────────────────────────────────────────
    //  自动检查：重复（红） + 占位符（橙）
    // ─────────────────────────────────────────────────────────

    private void RunFullCheck()
    {
        if (IsDisposed || txtPreview == null || txtPreview.IsDisposed) return;
        if (string.IsNullOrEmpty(txtPreview.Text)) return;

        var origStart = txtPreview.SelectionStart;
        var origLen   = txtPreview.SelectionLength;
        var text      = txtPreview.Text;

        // 保存当前滚动位置（EM_GETSCROLLPOS 是 RichTextBox 专有消息）
        var scrollPos = new Point();
        SendMessage(txtPreview.Handle, EM_GETSCROLLPOS, IntPtr.Zero, ref scrollPos);

        // 冻结重绘，批量改色后一次性刷新
        SendMessage(txtPreview.Handle, WM_SETREDRAW, false, 0);
        try
        {
            // 1. 全部重置为黑色
            txtPreview.SelectAll();
            txtPreview.SelectionColor = Color.Black;

            // 2. 占位符（橙色）
            foreach (Match m in new Regex(@"【[^】]{0,20}】").Matches(text))
            {
                txtPreview.Select(m.Index, m.Length);
                txtPreview.SelectionColor = Color.FromArgb(200, 100, 0);
            }

            // 3. 重复内容（红色）—— 覆盖橙色
            var historyContents = GetHistoryContentsForPreview();
            var duplicateTexts  = _generatorService.GetDuplicateTexts(text, historyContents);
            foreach (var dup in duplicateTexts)
            {
                var pos = 0;
                while (pos < text.Length)
                {
                    var idx = text.IndexOf(dup, pos, StringComparison.Ordinal);
                    if (idx < 0) break;
                    txtPreview.Select(idx, dup.Length);
                    txtPreview.SelectionColor = Color.FromArgb(200, 0, 0);
                    pos = idx + dup.Length;
                }
            }

            UpdateDuplicateSources(duplicateTexts);

            // 恢复光标位置和选区（不触发滚动）
            var safeStart = Math.Min(origStart, txtPreview.TextLength);
            txtPreview.Select(safeStart, Math.Min(origLen, txtPreview.TextLength - safeStart));
        }
        finally
        {
            // 恢复滚动位置，再开启重绘
            SendMessage(txtPreview.Handle, EM_SETSCROLLPOS, IntPtr.Zero, ref scrollPos);
            SendMessage(txtPreview.Handle, WM_SETREDRAW, true, 0);
            txtPreview.Refresh();
            txtPreview.ClearUndo();
        }
    }

    // ─────────────────────────────────────────────────────────
    //  保存
    // ─────────────────────────────────────────────────────────

    private void BtnSave_Click(object? sender, EventArgs e)
    {
        if (string.IsNullOrWhiteSpace(txtPreview.Text))
        {
            MessageBox.Show("病程正文不能为空。", _isEditMode ? "修改病程" : "新建病程", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        var historyContents = GetHistoryContentsForPreview();
        var hasDuplicates   = _generatorService.HasDuplicates(txtPreview.Text, historyContents);
        var duplicateReport = hasDuplicates ? _generatorService.CheckDuplicates(txtPreview.Text, historyContents) : string.Empty;
        if (hasDuplicates) RunFullCheck();

        if (_isEditMode && _existingRecord != null)
        {
            _existingRecord.RecordDate   = dtpRecordDate.Value;
            _existingRecord.RecordType   = cmbRecordType.Text;
            _existingRecord.Content      = txtPreview.Text;
            _existingRecord.Summary      = txtPreview.Text.Length > 100 ? txtPreview.Text[..100] + "..." : txtPreview.Text;
            _existingRecord.HasDuplicate = hasDuplicates;
            _existingRecord.DuplicateInfo= duplicateReport;
            Record = _existingRecord;
        }
        else
        {
            Record = new ProgressRecord
            {
                PatientId    = _patient.Id,
                RecordDate   = dtpRecordDate.Value,
                RecordType   = cmbRecordType.Text,
                Content      = txtPreview.Text,
                Summary      = txtPreview.Text.Length > 100 ? txtPreview.Text[..100] + "..." : txtPreview.Text,
                HasDuplicate = hasDuplicates,
                DuplicateInfo= duplicateReport
            };
        }

        DialogResult = DialogResult.OK;
    }

    // ─────────────────────────────────────────────────────────
    //  联合浏览（从本窗体打开）
    // ─────────────────────────────────────────────────────────

    private void OpenCombinedBrowse()
    {
        BuildPreview();
        // 把当前草稿也加进去让联合浏览可以看到
        var allRecords = new List<ProgressRecord>(_records);
        if (!string.IsNullOrWhiteSpace(txtPreview.Text))
        {
            allRecords.Add(new ProgressRecord
            {
                RecordDate = dtpRecordDate.Value,
                RecordType = cmbRecordType.Text + "（草稿）",
                Content    = txtPreview.Text
            });
        }
        using var form = new CombinedBrowseForm(_patient, allRecords, _databaseService);
        form.ShowDialog(this);
    }

    // ─────────────────────────────────────────────────────────
    //  辅助方法
    // ─────────────────────────────────────────────────────────

    private void UpdateDuplicateSources(List<string> duplicateTexts)
    {
        if (duplicateTexts.Count == 0)
        {
            txtDuplicateSources.Text = "未检测到明显重复。";
            return;
        }

        var lines = new List<string>();
        foreach (var dup in duplicateTexts)
        {
            var sourceRecords = _records
                .Where(r => r.Content.Contains(dup, StringComparison.Ordinal))
                .OrderBy(r => r.RecordDate)
                .ToList();

            if (sourceRecords.Count == 0 && _firstProgressRawText.Contains(dup, StringComparison.Ordinal))
            {
                lines.Add($"与首程资料重复：{Shorten(dup)}");
                continue;
            }

            foreach (var source in sourceRecords)
                lines.Add($"与 {source.RecordDate:yyyy-MM-dd HH:mm} {source.RecordType} 重复：{Shorten(dup)}");
        }

        txtDuplicateSources.Text = string.Join(Environment.NewLine, lines.Distinct());
    }

    private List<string> GetHistoryContentsForPreview()
    {
        var currentText = txtPreview.Text;
        var history = _records
            .Select(r => r.Content)
            .Where(c => !string.Equals(c, currentText, StringComparison.Ordinal))
            .ToList();

        if (!history.Any(c => string.Equals(c, _firstProgressRawText, StringComparison.Ordinal))
            && !string.IsNullOrWhiteSpace(_firstProgressRawText)
            && !string.Equals(_firstProgressRawText, currentText, StringComparison.Ordinal))
        {
            history.Add(_firstProgressRawText);
        }

        return history;
    }

    private string AdjustPhysicalExamIfRepeated(string physicalExam)
    {
        if (string.IsNullOrWhiteSpace(physicalExam)) return physicalExam;

        var normalizedExam = NormalizeForLocalCompare(physicalExam);
        if (normalizedExam.Length < 20) return physicalExam.Trim();

        var repeated = GetHistoryContentsForPreview()
            .Any(c => NormalizeForLocalCompare(c).Contains(normalizedExam, StringComparison.Ordinal));

        if (!repeated) return physicalExam.Trim();

        var focus = BuildPhysicalExamFocus(physicalExam);
        return string.IsNullOrWhiteSpace(focus)
            ? "查体情况较前相仿，未见明确新发阳性体征；继续动态观察。"
            : $"查体较前总体相仿，本次重点记录：{focus}";
    }

    private static string BuildPhysicalExamFocus(string physicalExam)
    {
        var keywords = new[] { "Brunnstrom", "Ashworth", "肌力", "肌张力", "痉挛", "平衡", "步态", "吞咽", "疼痛", "水肿" };
        var segments = physicalExam
            .Replace("\r", "\n")
            .Split(new[] { '\n', '。', '；', ';' }, StringSplitOptions.RemoveEmptyEntries)
            .Select(s => s.Trim())
            .Where(s => s.Length > 0 && keywords.Any(k => s.Contains(k, StringComparison.OrdinalIgnoreCase)))
            .Take(3)
            .ToList();
        return string.Join("；", segments);
    }

    private string RefineAnalysisDraft(string analysis)
    {
        if (string.IsNullOrWhiteSpace(analysis)) return analysis;

        var existingText = string.Join("\n", new[]
        {
            txtSubjective.Text, txtPhysicalExam.Text, txtExamResults.Text, txtOrderChanges.Text
        }.Where(s => !string.IsNullOrWhiteSpace(s)));

        var existingNormalized = NormalizeForLocalCompare(existingText);
        var sentences = analysis
            .Replace("\r", "")
            .Split(new[] { '。', '；', ';', '\n' }, StringSplitOptions.RemoveEmptyEntries)
            .Select(s => s.Trim())
            .Where(s => s.Length > 0)
            .Where(s => !existingNormalized.Contains(NormalizeForLocalCompare(s), StringComparison.Ordinal))
            .Distinct()
            .Take(5)
            .ToList();

        var knowledgeHint = FindKnowledgeHint(existingText);
        if (!string.IsNullOrWhiteSpace(knowledgeHint)) sentences.Add(knowledgeHint);

        if (sentences.Count == 0)
            return "患者目前情况：结合本次资料，重点关注病情变化趋势、治疗耐受情况及功能恢复情况。";

        return string.Join("。", sentences.Distinct()).TrimEnd('。') + "。";
    }

    private static string NormalizeForLocalCompare(string value) =>
        new(value.Where(ch => !char.IsWhiteSpace(ch) && !char.IsPunctuation(ch)).ToArray());

    private static string FindKnowledgeHint(string clinicalText)
    {
        try
        {
            using var context = new AppDbContext();
            var templates = context.KnowledgeTemplates
                .AsNoTracking()
                .Where(t => t.IsActive)
                .OrderByDescending(t => t.LastUsedAt)
                .Take(80)
                .ToList();

            var matched = templates.FirstOrDefault(t =>
                SplitKeywords(t.Keywords).Any(k => clinicalText.Contains(k, StringComparison.OrdinalIgnoreCase))
                || (!string.IsNullOrWhiteSpace(t.Title) && clinicalText.Contains(t.Title, StringComparison.OrdinalIgnoreCase)));

            return matched == null
                ? string.Empty
                : $"结合知识库\"{matched.Title}\"提示，后续分析以功能变化、风险控制和治疗反应为重点";
        }
        catch { return string.Empty; }
    }

    private static IEnumerable<string> SplitKeywords(string keywords) =>
        keywords.Split(new[] { ',', '，', ';', '；', ' ' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(k => k.Trim())
                .Where(k => k.Length >= 2);

    private string BuildPatientArchiveText() =>
        $"""
患者档案
姓名：{_patient.Name}
性别：{_patient.Gender}
年龄：{_patient.Age}岁
住院号：{_patient.MedicalRecordNumber}
诊断：{_patient.Diagnosis}
主诉：{_patient.ChiefComplaint}
现病史：{_patient.History}
查体：{_patient.PhysicalExam}
辅助检查：{_patient.AuxiliaryExam}
诊疗计划：{_patient.TreatmentPlan}
""";

    private static string FirstNonEmpty(params string[] values) =>
        values.FirstOrDefault(v => !string.IsNullOrWhiteSpace(v)) ?? string.Empty;

    private static string EmptyAsNone(string value) =>
        string.IsNullOrWhiteSpace(value) ? "无特殊补充。" : value.Trim();

    private static string Shorten(string value)
    {
        var compact = value.Replace("\r", " ").Replace("\n", " ").Trim();
        return compact.Length > 80 ? compact[..80] + "..." : compact;
    }

    // ─────────────────────────────────────────────────────────
    //  UI 工厂方法
    // ─────────────────────────────────────────────────────────

    private static Button CreateButton(string text, Color color)
    {
        var b = new Button
        {
            Text      = text,
            Width     = 100,
            Height    = 34,
            BackColor = color,
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            Font      = new Font("Microsoft YaHei UI", 9),
            Margin    = new Padding(0, 2, 6, 0)
        };
        b.FlatAppearance.BorderSize = 0;
        return b;
    }

    private static TextBox CreateMultilineTextBox() => new()
    {
        Dock        = DockStyle.Fill,
        Multiline   = true,
        ScrollBars  = ScrollBars.Vertical,
        WordWrap    = true,
        Font        = new Font("Microsoft YaHei UI", 9),
        BorderStyle = BorderStyle.FixedSingle
    };

    private static RichTextBox CreatePreviewTextBox() => new()
    {
        Dock        = DockStyle.Fill,
        ScrollBars  = RichTextBoxScrollBars.Vertical,
        WordWrap    = true,
        Font        = new Font("Microsoft YaHei UI", 10),
        BorderStyle = BorderStyle.FixedSingle,
        BackColor   = Color.White,
        DetectUrls  = false
    };

    private static Control WrapWithTitle(string title, Control content)
    {
        var panel = new Panel { Dock = DockStyle.Fill, BackColor = Color.White, Padding = new Padding(0, 0, 0, 8) };
        panel.Controls.Add(content);
        panel.Controls.Add(new Label
        {
            Text      = title,
            Dock      = DockStyle.Top,
            Height    = 22,
            Font      = new Font("Microsoft YaHei UI", 9, FontStyle.Bold),
            ForeColor = Color.FromArgb(45, 45, 45)
        });
        return panel;
    }
}
