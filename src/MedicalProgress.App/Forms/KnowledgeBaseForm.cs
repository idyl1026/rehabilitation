using Microsoft.EntityFrameworkCore;
using MedicalProgress.App.Data;
using MedicalProgress.App.Helpers;
using MedicalProgress.App.Models;
using MedicalProgress.App.Services;

namespace MedicalProgress.App.Forms;

public class KnowledgeBaseForm : Form
{
    private readonly BatchImportService _batchImportService = new();

    private ComboBox cmbSubjects = null!;
    private ListView lvTemplates = null!;
    private TextBox txtPreview = null!;
    private TextBox txtSearch = null!;
    private Label lblStatus = null!;
    private Button btnEditTemplate = null!;
    private Button btnDeleteTemplate = null!;
    private Button btnDeleteSubject = null!;

    private List<Subject> _subjects = new();
    private List<KnowledgeTemplate> _currentTemplates = new();
    private Subject? _currentSubject;

    public string? SelectedTemplateContent { get; private set; }

    public KnowledgeBaseForm()
    {
        InitializeComponent();
        LoadSubjects();
    }

    private void InitializeComponent()
    {
        Text = "知识库管理";
        Size = new Size(1180, 820);
        StartPosition = FormStartPosition.CenterParent;
        BackColor = Color.FromArgb(245, 247, 250);
        MinimumSize = new Size(1050, 720);

        var header = new Label
        {
            Dock = DockStyle.Top,
            Height = 58,
            Text = "知识库管理",
            TextAlign = ContentAlignment.MiddleCenter,
            Font = new Font("Microsoft YaHei UI", 16, FontStyle.Bold),
            BackColor = Color.FromArgb(0, 123, 215),
            ForeColor = Color.White
        };

        var top = new TableLayoutPanel
        {
            Dock = DockStyle.Top,
            Height = 100,
            ColumnCount = 1,
            RowCount = 2,
            Padding = new Padding(12),
            BackColor = Color.White
        };
        top.RowStyles.Add(new RowStyle(SizeType.Absolute, 36));
        top.RowStyles.Add(new RowStyle(SizeType.Absolute, 42));

        var selectorRow = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = false
        };
        // 隐藏字段（内部逻辑使用，不显示）
        cmbSubjects = new ComboBox { Visible = false, Width = 0 };
        selectorRow.Controls.Add(new Label { Text = "搜索", AutoSize = true, Margin = new Padding(0, 8, 8, 0), Font = new Font("Microsoft YaHei UI", 10) });
        txtSearch = new TextBox { Width = 540, Font = new Font("Microsoft YaHei UI", 10), Margin = new Padding(0, 3, 8, 0) };
        txtSearch.TextChanged += (_, _) => ApplySearchFilter();
        selectorRow.Controls.Add(txtSearch);
        selectorRow.Controls.Add(new Label
        {
            Text = "多词空格分隔，模糊匹配标题/关键词/内容",
            AutoSize = true,
            Margin = new Padding(0, 10, 0, 0),
            Font = new Font("Microsoft YaHei UI", 8),
            ForeColor = Color.FromArgb(130, 130, 130)
        });

        var buttonRow = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = false
        };
        btnDeleteSubject = new Button { Visible = false }; // 保留字段，隐藏按钮
        var btnNewTemplate = CreateButton("新建模板", Color.FromArgb(40, 167, 69), 100);
        btnNewTemplate.Click += BtnNewTemplate_Click;
        btnEditTemplate = CreateButton("编辑模板", Color.FromArgb(0, 123, 215), 100);
        btnEditTemplate.Click += BtnEditTemplate_Click;
        btnEditTemplate.Enabled = false;
        btnDeleteTemplate = CreateButton("删除模板", Color.FromArgb(220, 53, 69), 100);
        btnDeleteTemplate.Click += BtnDeleteTemplate_Click;
        btnDeleteTemplate.Enabled = false;
        var btnImportTemplates = CreateButton("导入模板", Color.FromArgb(111, 66, 193), 100);
        btnImportTemplates.Click += BtnImportTemplates_Click;
        var btnExportSample = CreateButton("导出示例", Color.FromArgb(23, 162, 184), 100);
        btnExportSample.Click += BtnExportSample_Click;
        var btnRefresh = CreateButton("刷新", Color.FromArgb(108, 117, 125), 80);
        btnRefresh.Click += (_, _) => LoadSubjects();
        var btnExtractBook = AppleStyleHelper.CreatePrimaryButton("📖 提取知识点", 120);
        btnExtractBook.Click += (_, _) =>
        {
            var subjectId = _currentSubject?.Id ?? 0;
            new BookTextExtractForm(subjectId).ShowDialog(this);
            _ = LoadTemplatesAsync();
        };
        buttonRow.Controls.AddRange(new Control[]
        {
            btnNewTemplate, btnEditTemplate, btnDeleteTemplate,
            btnImportTemplates, btnExportSample, btnRefresh, btnExtractBook
        });

        top.Controls.Add(selectorRow, 0, 0);
        top.Controls.Add(buttonRow, 0, 1);

        var main = new SplitContainer
        {
            Dock = DockStyle.Fill,
            SplitterDistance = 470,
            BackColor = Color.FromArgb(230, 235, 240)
        };

        lvTemplates = new ListView
        {
            View = View.Details,
            FullRowSelect = true,
            GridLines = true,
            MultiSelect = true,
            Dock = DockStyle.Fill,
            Font = new Font("Microsoft YaHei UI", 9),
            BorderStyle = BorderStyle.FixedSingle,
            BackColor = Color.White
        };
        lvTemplates.Columns.Add("标题", 200);
        lvTemplates.Columns.Add("类型", 90);
        lvTemplates.Columns.Add("学科", 100);
        lvTemplates.Columns.Add("分类", 100);
        lvTemplates.Columns.Add("关键词", 150);
        lvTemplates.SelectedIndexChanged += LvTemplates_SelectedIndexChanged;
        lvTemplates.DoubleClick += (_, _) => UseSelectedTemplate();

        txtPreview = new TextBox
        {
            Multiline = true,
            ScrollBars = ScrollBars.Both,
            Dock = DockStyle.Fill,
            ReadOnly = true,
            Font = new Font("Microsoft YaHei UI", 10),
            WordWrap = true,
            BorderStyle = BorderStyle.FixedSingle,
            BackColor = Color.White
        };

        main.Panel1.Controls.Add(lvTemplates);
        main.Panel2.Controls.Add(txtPreview);

        var bottom = new FlowLayoutPanel
        {
            Dock = DockStyle.Bottom,
            Height = 62,
            Padding = new Padding(12),
            BackColor = Color.White,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = false
        };
        lblStatus = new Label
        {
            Text = "就绪  |  双击模板可套用到病程编辑器",
            Width = 680,
            Height = 32,
            TextAlign = ContentAlignment.MiddleLeft,
            Font = new Font("Microsoft YaHei UI", 9),
            ForeColor = Color.FromArgb(100, 100, 100)
        };
        var btnClose = CreateButton("关闭", Color.FromArgb(108, 117, 125), 85);
        btnClose.Click += (_, _) => { DialogResult = DialogResult.Cancel; Close(); };
        bottom.Controls.Add(lblStatus);
        bottom.Controls.Add(btnClose);

        Controls.Add(main);
        Controls.Add(bottom);
        Controls.Add(top);
        Controls.Add(header);
    }

    private static Button CreateButton(string text, Color color, int width)
    {
        var button = new Button
        {
            Text = text,
            Width = width,
            Height = 32,
            BackColor = color,
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            Font = new Font("Microsoft YaHei UI", 9),
            Margin = new Padding(0, 0, 8, 0)
        };
        button.FlatAppearance.BorderSize = 0;
        return button;
    }

    private async void LoadSubjects()
    {
        try
        {
            await EnsureDefaultSubjectsAsync();
            using var context = new AppDbContext();
            _subjects = await context.Subjects
                .Where(s => s.IsActive)
                .OrderBy(s => s.Name)
                .ToListAsync();

            var previousId = _currentSubject?.Id;
            cmbSubjects.Items.Clear();
            foreach (var subject in _subjects)
                cmbSubjects.Items.Add($"{subject.Name} ({subject.DocumentCount})");

            if (_subjects.Count == 0)
            {
                _currentSubject = null;
                lvTemplates.Items.Clear();
                txtPreview.Clear();
                UpdateStatus("暂无学科数据");
                return;
            }

            // 自动选择默认学科（仅用于新建/导入时）
            var idx = _subjects.FindIndex(s => s.Name == "康复医学科");
            _currentSubject = _subjects[idx >= 0 ? idx : 0];

            await LoadTemplatesAsync();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"加载学科失败：{ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private static async Task EnsureDefaultSubjectsAsync()
    {
        using var context = new AppDbContext();
        if (await context.Subjects.AnyAsync(s => s.IsActive))
            return;

        var defaults = new[] { "康复医学科", "神经内科", "骨科", "内科", "外科" };
        foreach (var name in defaults)
        {
            context.Subjects.Add(new Subject
            {
                Name = name,
                Description = "系统默认学科，可继续新增或导入资料扩展知识库。",
                CreatedAt = DateTime.Now,
                LastUpdatedAt = DateTime.Now,
                IsActive = true
            });
        }
        await context.SaveChangesAsync();
    }

    private async void CmbSubjects_SelectedIndexChanged(object? sender, EventArgs e)
    {
        if (cmbSubjects.SelectedIndex < 0 || cmbSubjects.SelectedIndex >= _subjects.Count)
            return;

        _currentSubject = _subjects[cmbSubjects.SelectedIndex];
        btnDeleteSubject.Enabled = _currentSubject != null;
        await LoadTemplatesAsync();
    }

    private async Task LoadTemplatesAsync()
    {
        using var context = new AppDbContext();
        // 加载所有学科的模板
        _currentTemplates = await context.KnowledgeTemplates
            .Include(t => t.Category)
            .Include(t => t.Subject)
            .Where(t => t.IsActive)
            .OrderByDescending(t => t.LastUsedAt)
            .ThenByDescending(t => t.UseCount)
            .ToListAsync();

        ApplySearchFilter();
        UpdateStatus($"共 {_currentTemplates.Count} 条模板  |  双击模板可套用到病程编辑器");
    }

    private void ApplySearchFilter()
    {
        var search = txtSearch.Text.Trim();

        List<KnowledgeTemplate> filtered;
        if (string.IsNullOrWhiteSpace(search))
        {
            filtered = _currentTemplates;
        }
        else
        {
            // 多词模糊搜索：空格分隔多个关键词，全部命中才显示，按相关度排序
            var tokens = search.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            filtered = _currentTemplates
                .Select(t => (Template: t, Score: GetFuzzyScore(t, tokens)))
                .Where(x => x.Score > 0)
                .OrderByDescending(x => x.Score)
                .ThenByDescending(x => x.Template.UseCount)
                .Select(x => x.Template)
                .ToList();
        }

        lvTemplates.Items.Clear();
        foreach (var template in filtered)
        {
            var item = new ListViewItem(template.Title);
            item.SubItems.Add(template.TemplateType);
            item.SubItems.Add(template.Subject?.Name ?? "—");
            item.SubItems.Add(template.Category?.Name ?? "未分类");
            item.SubItems.Add(template.Keywords.Length > 24 ? template.Keywords[..24] + "..." : template.Keywords);
            item.Tag = template;
            lvTemplates.Items.Add(item);
        }

        var hint = string.IsNullOrWhiteSpace(search)
            ? $"显示全部 {filtered.Count} 条"
            : $"匹配 {filtered.Count} 条（共 {_currentTemplates.Count} 条）";
        UpdateStatus($"{hint}  |  双击模板可套用到病程编辑器");
    }

    // 计算模糊匹配分值：所有词必须命中，标题权重最高
    private static int GetFuzzyScore(KnowledgeTemplate t, string[] tokens)
    {
        var score = 0;
        foreach (var token in tokens)
        {
            var inTitle = t.Title.Contains(token, StringComparison.OrdinalIgnoreCase);
            var inKeywords = t.Keywords.Contains(token, StringComparison.OrdinalIgnoreCase);
            var inSummary = t.Summary.Contains(token, StringComparison.OrdinalIgnoreCase);
            var inContent = t.Content.Contains(token, StringComparison.OrdinalIgnoreCase);

            // 任意词未命中 → 整条不返回
            if (!inTitle && !inKeywords && !inSummary && !inContent)
                return 0;

            if (inTitle) score += 8;
            if (inKeywords) score += 4;
            if (inSummary) score += 2;
            if (inContent) score += 1;
        }
        return score;
    }

    private void LvTemplates_SelectedIndexChanged(object? sender, EventArgs e)
    {
        var count = lvTemplates.SelectedItems.Count;
        var hasSingle = count == 1 && lvTemplates.SelectedItems[0].Tag is KnowledgeTemplate;
        var hasAny = count > 0;

        btnEditTemplate.Enabled = hasSingle;
        btnDeleteTemplate.Enabled = hasAny;
        if (count > 1)
            btnDeleteTemplate.Text = $"删除模板({count})";
        else
            btnDeleteTemplate.Text = "删除模板";

        txtPreview.Text = hasSingle && lvTemplates.SelectedItems[0].Tag is KnowledgeTemplate template
            ? template.Content
            : string.Empty;
    }

    private async void BtnNewSubject_Click(object? sender, EventArgs e)
    {
        var name = Prompt("新建学科", "请输入学科名称：");
        if (string.IsNullOrWhiteSpace(name))
            return;

        try
        {
            using var context = new AppDbContext();
            if (await context.Subjects.AnyAsync(s => s.Name == name.Trim() && s.IsActive))
            {
                MessageBox.Show("该学科已经存在。", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            context.Subjects.Add(new Subject
            {
                Name = name.Trim(),
                Description = "手动创建的知识库学科。",
                CreatedAt = DateTime.Now,
                LastUpdatedAt = DateTime.Now,
                IsActive = true
            });
            await context.SaveChangesAsync();
            LoadSubjects();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"新建学科失败：{ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void BtnNewTemplate_Click(object? sender, EventArgs e)
    {
        if (!EnsureSubjectSelected())
            return;

        using var form = new TemplateEditForm(_currentSubject!.Id);
        if (form.ShowDialog(this) == DialogResult.OK)
            _ = LoadTemplatesAsync();
    }

    private void BtnEditTemplate_Click(object? sender, EventArgs e)
    {
        if (!EnsureSubjectSelected() || lvTemplates.SelectedItems.Count == 0 || lvTemplates.SelectedItems[0].Tag is not KnowledgeTemplate template)
            return;

        using var form = new TemplateEditForm(_currentSubject!.Id, template);
        if (form.ShowDialog(this) == DialogResult.OK)
            _ = LoadTemplatesAsync();
    }

    private async void BtnDeleteSubject_Click(object? sender, EventArgs e)
    {
        if (_currentSubject == null) return;
        var msg = $"确定删除学科「{_currentSubject.Name}」及其所有模板？此操作不可恢复。";
        if (MessageBox.Show(msg, "确认删除学科", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) != DialogResult.Yes)
            return;

        using var context = new AppDbContext();
        var subject = await context.Subjects.FindAsync(_currentSubject.Id);
        if (subject != null)
        {
            subject.IsActive = false;
            var templates = context.KnowledgeTemplates.Where(t => t.SubjectId == subject.Id);
            await templates.ForEachAsync(t => t.IsActive = false);
            await context.SaveChangesAsync();
        }
        _currentSubject = null;
        LoadSubjects();
    }

    private async void BtnDeleteTemplate_Click(object? sender, EventArgs e)
    {
        var selected = lvTemplates.SelectedItems
            .Cast<ListViewItem>()
            .Select(item => item.Tag as KnowledgeTemplate)
            .Where(t => t != null)
            .Cast<KnowledgeTemplate>()
            .ToList();

        if (selected.Count == 0) return;

        var msg = selected.Count == 1
            ? $"确定删除「{selected[0].Title}」？"
            : $"确定删除选中的 {selected.Count} 条模板？";
        if (MessageBox.Show(msg, "确认删除", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) != DialogResult.Yes)
            return;

        using var context = new AppDbContext();
        foreach (var t in selected)
        {
            var existing = await context.KnowledgeTemplates.FindAsync(t.Id);
            if (existing != null) existing.IsActive = false;
        }
        await context.SaveChangesAsync();
        await LoadTemplatesAsync();
    }

    private async void BtnImportTemplates_Click(object? sender, EventArgs e)
    {
        if (!EnsureSubjectSelected())
            return;

        using var dialog = new OpenFileDialog
        {
            Title = "选择模板文件（Excel / JSON / CSV）",
            Filter = "模板文件|*.xlsx;*.json;*.csv|Excel 文件 (*.xlsx)|*.xlsx|JSON 文件 (*.json)|*.json|CSV 文件 (*.csv)|*.csv",
            FilterIndex = 1
        };
        if (dialog.ShowDialog(this) != DialogResult.OK)
            return;

        var filePath = dialog.FileName;
        var ext = Path.GetExtension(filePath).ToLowerInvariant();

        try
        {
            UpdateStatus("正在导入...");
            BatchImportResult result;
            if (ext == ".xlsx")
                result = await _batchImportService.ImportFromXlsxAsync(filePath, _currentSubject!.Id);
            else if (ext == ".json")
                result = await _batchImportService.ImportFromJsonAsync(filePath, _currentSubject!.Id);
            else
                result = await _batchImportService.ImportFromCsvAsync(filePath, _currentSubject!.Id);

            // 更新学科文档数量
            using (var context = new AppDbContext())
            {
                var subject = await context.Subjects.FindAsync(_currentSubject!.Id);
                if (subject != null)
                {
                    subject.LastUpdatedAt = DateTime.Now;
                    subject.DocumentCount = await context.KnowledgeTemplates.CountAsync(t => t.SubjectId == _currentSubject.Id && t.IsActive);
                    await context.SaveChangesAsync();
                }
            }

            LoadSubjects();

            var lines = new List<string> { $"成功导入 {result.SuccessCount} 条模板" };
            if (result.FailedCount > 0)
                lines.Add($"失败 {result.FailedCount} 条");
            if (!string.IsNullOrWhiteSpace(result.ErrorMessage))
                lines.Add($"错误：{result.ErrorMessage}");
            if (result.FailedTitles.Count > 0)
                lines.Add("失败条目：\n" + string.Join("\n", result.FailedTitles.Take(5)));

            MessageBox.Show(string.Join("\n", lines), "导入结果", MessageBoxButtons.OK,
                result.SuccessCount > 0 ? MessageBoxIcon.Information : MessageBoxIcon.Warning);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"导入失败：{ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            UpdateStatus("导入失败");
        }
    }

    private void BtnExportSample_Click(object? sender, EventArgs e)
    {
        using var dialog = new SaveFileDialog
        {
            Title = "导出模板示例文件",
            Filter = "Excel 文件 (*.xlsx)|*.xlsx|JSON 文件 (*.json)|*.json|CSV 文件 (*.csv)|*.csv",
            FileName = "病程模板导入示例",
            FilterIndex = 1
        };
        if (dialog.ShowDialog(this) != DialogResult.OK)
            return;

        try
        {
            var ext = Path.GetExtension(dialog.FileName).ToLowerInvariant();
            if (ext == ".xlsx")
            {
                _batchImportService.CreateSampleXlsx(dialog.FileName);
            }
            else
            {
                var content = ext == ".csv"
                    ? _batchImportService.GetSampleCsvContent()
                    : _batchImportService.GetSampleJsonContent();
                File.WriteAllText(dialog.FileName, content, System.Text.Encoding.UTF8);
            }
            MessageBox.Show($"示例文件已导出到：\n{dialog.FileName}\n\n可用 Excel 编辑后直接导入。",
                "导出成功", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"导出失败：{ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void UseSelectedTemplate()
    {
        if (lvTemplates.SelectedItems.Count == 0 || lvTemplates.SelectedItems[0].Tag is not KnowledgeTemplate template)
            return;

        SelectedTemplateContent = template.Content;

        // 异步更新使用计数，不阻塞 UI
        _ = Task.Run(async () =>
        {
            using var context = new AppDbContext();
            var existing = await context.KnowledgeTemplates.FindAsync(template.Id);
            if (existing == null) return;
            existing.UseCount++;
            existing.LastUsedAt = DateTime.Now;
            await context.SaveChangesAsync();
        });

        DialogResult = DialogResult.OK;
        Close();
    }

    private bool EnsureSubjectSelected()
    {
        if (_currentSubject != null)
            return true;
        MessageBox.Show("请先选择或新建学科。", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
        return false;
    }

    private void UpdateStatus(string text)
    {
        lblStatus.Text = text;
    }

    private static string? Prompt(string title, string label, string defaultValue = "")
    {
        using var form = new Form
        {
            Text = title,
            Size = new Size(460, 170),
            StartPosition = FormStartPosition.CenterParent,
            FormBorderStyle = FormBorderStyle.FixedDialog,
            MaximizeBox = false,
            MinimizeBox = false
        };
        var lbl = new Label { Text = label, Left = 16, Top = 18, Width = 410, Height = 24 };
        var txt = new TextBox { Left = 16, Top = 48, Width = 410, Text = defaultValue };
        var ok = new Button { Text = "确定", Left = 246, Top = 86, Width = 82, DialogResult = DialogResult.OK };
        var cancel = new Button { Text = "取消", Left = 344, Top = 86, Width = 82, DialogResult = DialogResult.Cancel };
        form.Controls.AddRange(new Control[] { lbl, txt, ok, cancel });
        form.AcceptButton = ok;
        form.CancelButton = cancel;
        return form.ShowDialog() == DialogResult.OK ? txt.Text : null;
    }
}
