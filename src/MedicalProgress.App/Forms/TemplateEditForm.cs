using System.Windows.Forms;
using Microsoft.EntityFrameworkCore;
using MedicalProgress.App.Models;
using MedicalProgress.App.Data;

namespace MedicalProgress.App.Forms;

public class TemplateEditForm : Form
{
    public KnowledgeTemplate? Template { get; private set; }
    public bool IsNew { get; private set; }

    private TextBox txtTitle = null!;
    private ComboBox cmbTemplateType = null!;
    private ComboBox cmbCategory = null!;
    private TextBox txtKeywords = null!;
    private TextBox txtContent = null!;
    private Label lblWordCount = null!;
    private Button btnSave = null!;
    private Button btnCancel = null!;

    private int _subjectId;
    private List<DiseaseCategory> _categories = new List<DiseaseCategory>();

    public TemplateEditForm(int subjectId, KnowledgeTemplate? existingTemplate = null)
    {
        _subjectId = subjectId;
        Template = existingTemplate;
        IsNew = existingTemplate == null;
        InitializeComponent();
        LoadCategories();
        
        if (!IsNew && Template != null)
        {
            LoadTemplate();
        }
    }

    private void InitializeComponent()
    {
        this.Text = IsNew ? "➕ 创建新模板" : "✏️ 编辑模板";
        this.Size = new System.Drawing.Size(850, 650);
        this.StartPosition = FormStartPosition.CenterParent;
        this.FormBorderStyle = FormBorderStyle.FixedDialog;
        this.MaximizeBox = false;
        this.MinimizeBox = false;
        this.BackColor = Color.FromArgb(240, 244, 248);

        var pnlHeader = new Panel
        {
            Dock = DockStyle.Top,
            Height = 65,
            BackColor = IsNew ? Color.FromArgb(40, 167, 69) : Color.FromArgb(0, 123, 215)
        };

        var lblTitle = new Label
        {
            Text = IsNew ? "➕ 创建新模板" : "✏️ 编辑模板",
            Font = new Font("微软雅黑", 18, FontStyle.Bold),
            ForeColor = Color.White,
            TextAlign = ContentAlignment.MiddleCenter,
            Dock = DockStyle.Fill
        };
        pnlHeader.Controls.Add(lblTitle);

        var pnlTop = new Panel
        {
            Dock = DockStyle.Top,
            Height = 180,
            BackColor = Color.White,
            Padding = new Padding(20)
        };

        var row1 = new TableLayoutPanel
        {
            Dock = DockStyle.Top,
            Height = 40,
            ColumnCount = 4,
            Padding = new Padding(0, 5, 0, 5)
        };
        row1.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 80));
        row1.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
        row1.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 80));
        row1.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));

        var lblTitleLabel = new Label
        {
            Text = "标题：",
            TextAlign = ContentAlignment.MiddleRight,
            Dock = DockStyle.Fill,
            Font = new Font("微软雅黑", 10)
        };

        txtTitle = new TextBox
        {
            Dock = DockStyle.Fill,
            Font = new Font("微软雅黑", 10),
            BorderStyle = BorderStyle.FixedSingle,
            BackColor = Color.White
        };

        var lblTypeLabel = new Label
        {
            Text = "类型：",
            TextAlign = ContentAlignment.MiddleRight,
            Dock = DockStyle.Fill,
            Font = new Font("微软雅黑", 10)
        };

        cmbTemplateType = new ComboBox
        {
            Dock = DockStyle.Fill,
            DropDownStyle = ComboBoxStyle.DropDownList,
            Font = new Font("微软雅黑", 10),
            FlatStyle = FlatStyle.Flat,
            BackColor = Color.White
        };
        cmbTemplateType.Items.AddRange(new[] { "首次病程", "日常病程", "出院小结", "会诊记录", "鉴别诊断", "治疗方案" });
        cmbTemplateType.SelectedIndex = 0;

        row1.Controls.Add(lblTitleLabel, 0, 0);
        row1.Controls.Add(txtTitle, 1, 0);
        row1.Controls.Add(lblTypeLabel, 2, 0);
        row1.Controls.Add(cmbTemplateType, 3, 0);

        var row2 = new TableLayoutPanel
        {
            Dock = DockStyle.Top,
            Height = 40,
            ColumnCount = 4,
            Padding = new Padding(0, 5, 0, 5)
        };
        row2.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 80));
        row2.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
        row2.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 80));
        row2.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));

        var lblCategoryLabel = new Label
        {
            Text = "分类：",
            TextAlign = ContentAlignment.MiddleRight,
            Dock = DockStyle.Fill,
            Font = new Font("微软雅黑", 10)
        };

        cmbCategory = new ComboBox
        {
            Dock = DockStyle.Fill,
            DropDownStyle = ComboBoxStyle.DropDownList,
            Font = new Font("微软雅黑", 10),
            FlatStyle = FlatStyle.Flat,
            BackColor = Color.White
        };

        var lblKeywordsLabel = new Label
        {
            Text = "关键词：",
            TextAlign = ContentAlignment.MiddleRight,
            Dock = DockStyle.Fill,
            Font = new Font("微软雅黑", 10)
        };

        txtKeywords = new TextBox
        {
            Dock = DockStyle.Fill,
            Font = new Font("微软雅黑", 10),
            BorderStyle = BorderStyle.FixedSingle,
            BackColor = Color.White
        };

        row2.Controls.Add(lblCategoryLabel, 0, 0);
        row2.Controls.Add(cmbCategory, 1, 0);
        row2.Controls.Add(lblKeywordsLabel, 2, 0);
        row2.Controls.Add(txtKeywords, 3, 0);

        var lblContentLabel = new Label
        {
            Text = "📝 模板内容：",
            Dock = DockStyle.Top,
            Height = 30,
            TextAlign = ContentAlignment.BottomLeft,
            Font = new Font("微软雅黑", 11, FontStyle.Bold),
            ForeColor = Color.FromArgb(50, 50, 50),
            Padding = new Padding(0, 10, 0, 5)
        };

        lblWordCount = new Label
        {
            Text = "字数：0",
            Dock = DockStyle.Top,
            Height = 25,
            TextAlign = ContentAlignment.MiddleRight,
            Font = new Font("微软雅黑", 9),
            ForeColor = Color.FromArgb(100, 100, 100),
            Padding = new Padding(0, 0, 20, 0)
        };

        pnlTop.Controls.Add(lblContentLabel);
        pnlTop.Controls.Add(row1);
        pnlTop.Controls.Add(row2);
        pnlTop.Controls.Add(lblWordCount);

        var pnlContent = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = Color.White,
            Padding = new Padding(20, 0, 20, 0)
        };

        txtContent = new TextBox
        {
            Multiline = true,
            ScrollBars = ScrollBars.Both,
            Dock = DockStyle.Fill,
            Font = new Font("微软雅黑", 11),
            WordWrap = true,
            AcceptsReturn = true,
            AcceptsTab = true,
            BorderStyle = BorderStyle.FixedSingle,
            BackColor = Color.White,
            Padding = new Padding(10)
        };
        txtContent.TextChanged += TxtContent_TextChanged;

        pnlContent.Controls.Add(txtContent);

        var pnlBottom = new Panel
        {
            Dock = DockStyle.Bottom,
            Height = 60,
            BackColor = Color.White,
            Padding = new Padding(20, 10, 20, 15)
        };

        btnSave = new Button
        {
            Text = "💾 保存",
            Left = 580,
            Top = 10,
            Width = 130,
            Height = 40,
            Font = new Font("微软雅黑", 10),
            BackColor = Color.FromArgb(40, 167, 69),
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat
        };
        btnSave.FlatAppearance.BorderSize = 0;
        btnSave.FlatAppearance.MouseOverBackColor = ControlPaint.Dark(Color.FromArgb(40, 167, 69), 0.1f);
        btnSave.Click += BtnSave_Click;

        btnCancel = new Button
        {
            Text = "❌ 取消",
            Left = 720,
            Top = 10,
            Width = 100,
            Height = 40,
            Font = new Font("微软雅黑", 10),
            BackColor = Color.FromArgb(108, 117, 125),
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat
        };
        btnCancel.FlatAppearance.BorderSize = 0;
        btnCancel.FlatAppearance.MouseOverBackColor = ControlPaint.Dark(Color.FromArgb(108, 117, 125), 0.1f);
        btnCancel.Click += (s, e) => { this.DialogResult = DialogResult.Cancel; this.Close(); };

        pnlBottom.Controls.Add(btnSave);
        pnlBottom.Controls.Add(btnCancel);

        this.Controls.Add(pnlContent);
        this.Controls.Add(pnlTop);
        this.Controls.Add(pnlBottom);
        this.Controls.Add(pnlHeader);
    }

    private async void LoadCategories()
    {
        try
        {
            using var context = new AppDbContext();
            _categories = await context.DiseaseCategories
                .Where(c => c.SubjectId == _subjectId)
                .OrderBy(c => c.Name)
                .ToListAsync();

            cmbCategory.Items.Clear();
            cmbCategory.Items.Add("未分类");
            foreach (var cat in _categories)
            {
                cmbCategory.Items.Add(cat.Name);
            }
            cmbCategory.SelectedIndex = 0;
        }
        catch (Exception ex)
        {
            MessageBox.Show($"加载分类失败：{ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void LoadTemplate()
    {
        txtTitle.Text = Template!.Title;
        cmbTemplateType.SelectedItem = Template.TemplateType;
        txtKeywords.Text = Template.Keywords;
        txtContent.Text = Template.Content;
        
        if (Template.CategoryId != 0)
        {
            var category = _categories.FirstOrDefault(c => c.Id == Template.CategoryId);
            if (category != null && cmbCategory.Items.Contains(category.Name))
            {
                cmbCategory.SelectedItem = category.Name;
            }
        }
    }

    private void TxtContent_TextChanged(object? sender, EventArgs e)
    {
        lblWordCount.Text = $"字数：{txtContent.Text.Length}";
    }

    private async void BtnSave_Click(object? sender, EventArgs e)
    {
        if (string.IsNullOrWhiteSpace(txtTitle.Text))
        {
            MessageBox.Show("请输入标题", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            txtTitle.Focus();
            return;
        }

        if (string.IsNullOrWhiteSpace(txtContent.Text))
        {
            MessageBox.Show("请输入模板内容", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            txtContent.Focus();
            return;
        }

        try
        {
            using var context = new AppDbContext();

            int? categoryId = null;
            if (cmbCategory.SelectedIndex > 0)
            {
                var category = _categories.FirstOrDefault(c => c.Name == cmbCategory.SelectedItem?.ToString());
                categoryId = category?.Id;
            }

            if (IsNew)
            {
                Template = new KnowledgeTemplate
                {
                    SubjectId = _subjectId,
                    CategoryId = categoryId ?? 0,
                    Title = txtTitle.Text.Trim(),
                    TemplateType = cmbTemplateType.SelectedItem?.ToString() ?? "日常病程",
                    Content = txtContent.Text,
                    Summary = GenerateSummary(txtContent.Text),
                    Keywords = txtKeywords.Text.Trim(),
                    IsActive = true,
                    CreatedAt = DateTime.Now,
                    LastUsedAt = DateTime.Now
                };

                context.KnowledgeTemplates.Add(Template);
            }
            else
            {
                var existing = await context.KnowledgeTemplates.FindAsync(Template!.Id);
                if (existing != null)
                {
                    existing.Title = txtTitle.Text.Trim();
                    existing.TemplateType = cmbTemplateType.SelectedItem?.ToString() ?? "日常病程";
                    existing.Content = txtContent.Text;
                    existing.Summary = GenerateSummary(txtContent.Text);
                    existing.Keywords = txtKeywords.Text.Trim();
                    existing.CategoryId = categoryId ?? 0;
                    
                    Template = existing;
                }
            }

            await context.SaveChangesAsync();

            this.DialogResult = DialogResult.OK;
            this.Close();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"保存失败：{ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private string GenerateSummary(string content)
    {
        if (string.IsNullOrWhiteSpace(content))
            return "";
        
        var cleanText = content.Replace("\n", " ").Replace("\r", " ").Trim();
        if (cleanText.Length > 200)
            return cleanText.Substring(0, 200) + "...";
        
        return cleanText;
    }
}
