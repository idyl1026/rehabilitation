using System.Diagnostics;
using System.Text;
using MedicalProgress.App.Helpers;
using MedicalProgress.App.Models;
using MedicalProgress.App.Services;

namespace MedicalProgress.App.Forms;

public class MainForm : Form
{
    // ── 服务 ─────────────────────────────────────────────────
    private readonly DatabaseService        _databaseService      = new();
    private readonly GeneratorService       _generatorService     = new();
    private readonly PatientFolderService   _folderService        = new();
    private readonly KnowledgeCardMatchService _matchService      = new();

    // ── 控件 ─────────────────────────────────────────────────
    private ListBox   lstPatients        = null!;
    private TextBox   txtSearch          = null!;
    private Label     lblPatientInfo     = null!;
    private Label     lblStatus          = null!;
    private Label     lblPatientCount    = null!;
    private ListView  lvProgressHistory  = null!;
    private CheckBox  chkShowDischarged  = null!;
    private ListBox   lstKnowledge       = null!;
    private Label     lblKnowledgeStatus = null!;

    // ── 状态 ─────────────────────────────────────────────────
    private Patient?             _currentPatient         = null;
    private List<ProgressRecord> _currentProgressRecords = new();
    private List<Patient>        _allPatients            = new();

    public MainForm()
    {
        InitializeComponent();
        LoadPatients();
    }

    // ═══════════════════════════════════════════════════════════
    //  构建 UI
    // ═══════════════════════════════════════════════════════════

    private void InitializeComponent()
    {
        Text          = "病程助手 v1.0";
        Size          = new Size(1500, 900);
        MinimumSize   = new Size(1200, 720);
        StartPosition = FormStartPosition.CenterScreen;
        AppleStyleHelper.ApplyFormStyle(this);

        Controls.Add(BuildBody());
        Controls.Add(BuildHeader());
    }

    // ── Header ────────────────────────────────────────────────

    private Control BuildHeader()
    {
        var header = new Panel
        {
            Dock      = DockStyle.Top,
            Height    = 56,
            BackColor = AppleStyleHelper.HeaderDark
        };

        var lblTitle = new Label
        {
            Text      = "病 程 助 手",
            Font      = new Font("Segoe UI", 18f, FontStyle.Bold),
            ForeColor = Color.White,
            AutoSize  = true,
            Top       = 12
        };
        header.Controls.Add(lblTitle);
        header.Resize += (_, _) => lblTitle.Left = (header.Width - lblTitle.PreferredWidth) / 2;

        // Logo（左侧）
        var logoPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "logo.png");
        if (File.Exists(logoPath))
        {
            try
            {
                var pb = new PictureBox
                {
                    Image    = Image.FromFile(logoPath),
                    SizeMode = PictureBoxSizeMode.Zoom,
                    Width    = 280,
                    Height   = 46,
                    Left     = 10,
                    Top      = 5,
                    BackColor= Color.Transparent
                };
                header.Controls.Add(pb);
            }
            catch { header.Controls.Add(BuildTextLogo()); }
        }
        else
        {
            header.Controls.Add(BuildTextLogo());
        }

        // 版本号（右侧）
        var lblVer = new Label
        {
            Text      = "v1.0    作者：刘奕  liuyi@ahmu.edu.cn",
            Font      = new Font("Segoe UI", 8.5f),
            ForeColor = Color.FromArgb(160, 160, 175),
            AutoSize  = true,
            Top       = 18
        };
        header.Controls.Add(lblVer);
        header.Resize += (_, _) => lblVer.Left = header.Width - lblVer.PreferredWidth - 16;

        return header;
    }

    private static Control BuildTextLogo()
    {
        var p = new Panel { Width = 320, Height = 50, Left = 10, Top = 3, BackColor = Color.Transparent };
        p.Controls.Add(new Label
        {
            Text      = "安徽医科大学第二附属医院",
            Font      = new Font("Segoe UI", 12f, FontStyle.Bold),
            ForeColor = Color.FromArgb(160, 200, 255),
            AutoSize  = true,
            Left      = 0,
            Top       = 4
        });
        p.Controls.Add(new Label
        {
            Text      = "The 2nd Affiliated Hospital of Anhui Medical Univ.",
            Font      = new Font("Segoe UI", 7.5f),
            ForeColor = Color.FromArgb(120, 160, 210),
            AutoSize  = true,
            Left      = 0,
            Top       = 32
        });
        return p;
    }

    // ── Body（三栏布局） ──────────────────────────────────────

    private Control BuildBody()
    {
        // 三栏：左（患者列表）| 中（工作区）| 右（知识推荐）
        var outer = new TableLayoutPanel
        {
            Dock        = DockStyle.Fill,
            ColumnCount = 3,
            RowCount    = 1,
            BackColor   = AppleStyleHelper.BgGray
        };
        outer.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 230));
        outer.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,  100));
        outer.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 260));

        outer.Controls.Add(BuildPatientPanel(),   0, 0);
        outer.Controls.Add(BuildWorkPanel(),      1, 0);
        outer.Controls.Add(BuildKnowledgePanel(), 2, 0);

        return outer;
    }

    // ── 左栏：患者列表 ────────────────────────────────────────

    private Control BuildPatientPanel()
    {
        var panel = new Panel
        {
            Dock      = DockStyle.Fill,
            BackColor = AppleStyleHelper.BgGray,
            Padding   = new Padding(10, 10, 6, 10)
        };

        var card = AppleStyleHelper.CreateCard();
        card.Dock    = DockStyle.Fill;
        card.Padding = new Padding(0);

        // 标题行
        var titleBar = new Panel { Dock = DockStyle.Top, Height = 40, BackColor = Color.White, Padding = new Padding(12, 8, 8, 0) };
        var lblTitle = AppleStyleHelper.CreateTitle("患者列表");
        lblTitle.Dock      = DockStyle.Left;
        lblTitle.TextAlign = ContentAlignment.MiddleLeft;
        lblTitle.Width     = 100;
        lblPatientCount = AppleStyleHelper.CreateLabel("");
        lblPatientCount.Dock      = DockStyle.Right;
        lblPatientCount.TextAlign = ContentAlignment.MiddleRight;
        lblPatientCount.Width     = 50;
        titleBar.Controls.Add(lblTitle);
        titleBar.Controls.Add(lblPatientCount);

        // 搜索框
        var searchBar = new Panel { Dock = DockStyle.Top, Height = 38, BackColor = Color.White, Padding = new Padding(8, 5, 8, 0) };
        txtSearch = AppleStyleHelper.CreateTextField();
        txtSearch.Dock            = DockStyle.Fill;
        txtSearch.PlaceholderText = "搜索患者…";
        txtSearch.TextChanged    += (_, _) => ApplyPatientFilter();
        searchBar.Controls.Add(txtSearch);

        // 显示已出院
        chkShowDischarged = new CheckBox
        {
            Dock      = DockStyle.Top,
            Height    = 26,
            Text      = "显示已出院患者",
            Font      = AppleStyleHelper.FontSmall,
            ForeColor = AppleStyleHelper.DisabledGray,
            BackColor = Color.White,
            Padding   = new Padding(12, 4, 0, 0)
        };
        chkShowDischarged.CheckedChanged += (_, _) => LoadPatients();

        // 分隔线
        var sep = new Panel { Dock = DockStyle.Top, Height = 1, BackColor = AppleStyleHelper.CardBorder };

        // 患者列表
        lstPatients = new ListBox
        {
            Dock           = DockStyle.Fill,
            Font           = AppleStyleHelper.FontBody,
            IntegralHeight = false,
            BorderStyle    = BorderStyle.None,
            BackColor      = Color.White,
            ItemHeight     = 28,
            DrawMode       = DrawMode.OwnerDrawFixed
        };
        lstPatients.DrawItem             += LstPatients_DrawItem;
        lstPatients.SelectedIndexChanged += (_, _) => SelectCurrentPatient();
        lstPatients.DoubleClick          += BtnEditPatient_Click;

        // 底部按钮
        var btnGrid = new TableLayoutPanel
        {
            Dock        = DockStyle.Bottom,
            Height      = 80,
            ColumnCount = 2,
            RowCount    = 2,
            BackColor   = Color.White,
            Padding     = new Padding(8, 6, 8, 8),
            CellBorderStyle = TableLayoutPanelCellBorderStyle.None
        };
        btnGrid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
        btnGrid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
        btnGrid.RowStyles.Add(new RowStyle(SizeType.Percent, 50));
        btnGrid.RowStyles.Add(new RowStyle(SizeType.Percent, 50));

        var btnNew    = AppleStyleHelper.CreatePrimaryButton("新建患者");
        var btnDel    = AppleStyleHelper.CreateDangerButton("删除患者");
        var btnFolder = AppleStyleHelper.CreateSecondaryButton("打开目录");
        var btnKb     = AppleStyleHelper.CreateSecondaryButton("知识库");

        foreach (var b in new[] { btnNew, btnDel, btnFolder, btnKb })
        {
            b.Dock   = DockStyle.Fill;
            b.Height = 30;
            b.Margin = new Padding(2);
        }

        btnNew.Click    += BtnNewPatient_Click;
        btnDel.Click    += BtnDelete_Click;
        btnFolder.Click += BtnViewFolder_Click;
        btnKb.Click     += (_, _) => new KnowledgeBaseForm().ShowDialog(this);

        btnGrid.Controls.Add(btnNew,    0, 0);
        btnGrid.Controls.Add(btnDel,    1, 0);
        btnGrid.Controls.Add(btnFolder, 0, 1);
        btnGrid.Controls.Add(btnKb,     1, 1);

        card.Controls.Add(lstPatients);
        card.Controls.Add(sep);
        card.Controls.Add(chkShowDischarged);
        card.Controls.Add(searchBar);
        card.Controls.Add(titleBar);
        card.Controls.Add(btnGrid);

        panel.Controls.Add(card);
        return panel;
    }

    private void LstPatients_DrawItem(object? sender, DrawItemEventArgs e)
    {
        if (e.Index < 0 || e.Index >= lstPatients.Items.Count) return;
        var selected = (e.State & DrawItemState.Selected) == DrawItemState.Selected;
        var bg = selected ? AppleStyleHelper.SelectionBg : (e.Index % 2 == 0 ? Color.White : Color.FromArgb(250, 250, 252));
        e.Graphics.FillRectangle(new SolidBrush(bg), e.Bounds);

        var patient = lstPatients.Items[e.Index] as Patient;
        var name    = patient?.Name ?? lstPatients.Items[e.Index]?.ToString() ?? "";
        var color   = selected ? AppleStyleHelper.PrimaryBlue : AppleStyleHelper.HeaderDark;
        using var brush = new SolidBrush(color);
        e.Graphics.DrawString(name, AppleStyleHelper.FontBody, brush, e.Bounds.X + 12, e.Bounds.Y + 5);

        // 底部细线
        using var pen = new Pen(Color.FromArgb(240, 240, 242));
        e.Graphics.DrawLine(pen, e.Bounds.Left, e.Bounds.Bottom - 1, e.Bounds.Right, e.Bounds.Bottom - 1);
    }

    // ── 中栏：工作区 ──────────────────────────────────────────

    private Control BuildWorkPanel()
    {
        var panel = new Panel
        {
            Dock      = DockStyle.Fill,
            BackColor = AppleStyleHelper.BgGray,
            Padding   = new Padding(6, 10, 6, 10)
        };

        var outer = AppleStyleHelper.CreateCard();
        outer.Dock    = DockStyle.Fill;
        outer.Padding = new Padding(0);

        // 患者信息栏
        var infoBar = new Panel
        {
            Dock      = DockStyle.Top,
            Height    = 44,
            BackColor = Color.FromArgb(232, 240, 254),
            Padding   = new Padding(14, 0, 14, 0)
        };
        lblPatientInfo = new Label
        {
            Dock      = DockStyle.Fill,
            Font      = AppleStyleHelper.FontBold,
            ForeColor = AppleStyleHelper.PrimaryBlue,
            TextAlign = ContentAlignment.MiddleLeft,
            Text      = "请从左侧选择或新建患者"
        };
        infoBar.Controls.Add(lblPatientInfo);

        // 工具栏
        var toolbar = BuildToolbar();

        // 历史记录
        var histPanel = BuildHistoryPanel();

        // 状态栏
        lblStatus = new Label
        {
            Dock      = DockStyle.Bottom,
            Height    = 28,
            Text      = "就绪",
            Font      = AppleStyleHelper.FontSmall,
            ForeColor = AppleStyleHelper.DisabledGray,
            TextAlign = ContentAlignment.MiddleLeft,
            Padding   = new Padding(14, 0, 0, 0),
            BackColor = Color.FromArgb(250, 250, 252)
        };

        outer.Controls.Add(histPanel);
        outer.Controls.Add(lblStatus);
        outer.Controls.Add(toolbar);
        outer.Controls.Add(infoBar);

        panel.Controls.Add(outer);
        return panel;
    }

    private Control BuildToolbar()
    {
        var bar = new Panel
        {
            Dock      = DockStyle.Top,
            Height    = 52,
            BackColor = Color.White,
            Padding   = new Padding(12, 9, 12, 9)
        };

        var flow = new FlowLayoutPanel
        {
            Dock          = DockStyle.Fill,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents  = false,
            BackColor     = Color.Transparent
        };

        var btnFirst     = AppleStyleHelper.CreateSecondaryButton("患者档案",     110);
        var btnNew       = AppleStyleHelper.CreatePrimaryButton("新建病程记录",  130);
        var btnBrowse    = AppleStyleHelper.CreateSecondaryButton("联合浏览",     100);
        var btnDischarge = AppleStyleHelper.CreateDangerButton("出院归档",       100);

        btnFirst.Click     += BtnGenerateProgress_Click;
        btnNew.Click       += BtnNewProgressRecord_Click;
        btnBrowse.Click    += BtnCombinedBrowse_Click;
        btnDischarge.Click += BtnDischarge_Click;

        flow.Controls.AddRange(new Control[] { btnFirst, btnNew, btnBrowse, btnDischarge });

        var sep = new Panel { Dock = DockStyle.Bottom, Height = 1, BackColor = AppleStyleHelper.CardBorder };
        bar.Controls.Add(flow);
        bar.Controls.Add(sep);
        return bar;
    }

    private Control BuildHistoryPanel()
    {
        var panel = new Panel
        {
            Dock      = DockStyle.Fill,
            BackColor = Color.White,
            Padding   = new Padding(12, 6, 12, 6)
        };

        var titleBar = new Panel { Dock = DockStyle.Top, Height = 30, BackColor = Color.Transparent };
        titleBar.Controls.Add(new Label
        {
            Text      = "病程历史记录（双击编辑）",
            Font      = AppleStyleHelper.FontBold,
            ForeColor = AppleStyleHelper.HeaderDark,
            Dock      = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleLeft
        });

        lvProgressHistory = new ListView
        {
            Dock          = DockStyle.Fill,
            View          = View.Details,
            FullRowSelect = true,
            GridLines     = false,
            MultiSelect   = true,
            Font          = AppleStyleHelper.FontBody,
            BorderStyle   = BorderStyle.None,
            BackColor     = Color.White
        };
        lvProgressHistory.Columns.Add("日期",   160);
        lvProgressHistory.Columns.Add("类型",   140);
        lvProgressHistory.Columns.Add("摘要",   700);
        lvProgressHistory.DoubleClick += LvProgressHistory_DoubleClick;

        // 右键菜单
        var ctxMenu   = new ContextMenuStrip { Font = AppleStyleHelper.FontBody };
        var ctxDelete = new ToolStripMenuItem("删除选中记录") { ForeColor = AppleStyleHelper.DangerRed };
        ctxDelete.Click += BtnDeleteSelectedRecords_Click;
        ctxMenu.Items.Add(ctxDelete);
        lvProgressHistory.ContextMenuStrip = ctxMenu;

        panel.Controls.Add(lvProgressHistory);
        panel.Controls.Add(titleBar);
        return panel;
    }

    // ── 右栏：知识卡片推荐 ────────────────────────────────────

    private Control BuildKnowledgePanel()
    {
        var panel = new Panel
        {
            Dock      = DockStyle.Fill,
            BackColor = AppleStyleHelper.BgGray,
            Padding   = new Padding(6, 10, 10, 10)
        };

        var card = AppleStyleHelper.CreateCard();
        card.Dock    = DockStyle.Fill;
        card.Padding = new Padding(0);

        var titleBar = new Panel { Dock = DockStyle.Top, Height = 40, BackColor = Color.White, Padding = new Padding(12, 8, 8, 0) };
        var lblTitle = AppleStyleHelper.CreateTitle("相关知识");
        lblTitle.Dock      = DockStyle.Left;
        lblTitle.TextAlign = ContentAlignment.MiddleLeft;
        lblTitle.Width     = 120;
        titleBar.Controls.Add(lblTitle);

        lblKnowledgeStatus = AppleStyleHelper.CreateLabel("选择患者后自动推荐");
        lblKnowledgeStatus.Dock      = DockStyle.Top;
        lblKnowledgeStatus.Height    = 24;
        lblKnowledgeStatus.TextAlign = ContentAlignment.MiddleLeft;
        lblKnowledgeStatus.Padding   = new Padding(12, 4, 0, 0);
        lblKnowledgeStatus.BackColor = Color.White;

        lstKnowledge = new ListBox
        {
            Dock           = DockStyle.Fill,
            Font           = AppleStyleHelper.FontSmall,
            IntegralHeight = false,
            BorderStyle    = BorderStyle.None,
            BackColor      = Color.White,
            ItemHeight     = 52,
            DrawMode       = DrawMode.OwnerDrawFixed
        };
        lstKnowledge.DrawItem  += LstKnowledge_DrawItem;
        lstKnowledge.DoubleClick += LstKnowledge_DoubleClick;

        var sep = new Panel { Dock = DockStyle.Top, Height = 1, BackColor = AppleStyleHelper.CardBorder };

        card.Controls.Add(lstKnowledge);
        card.Controls.Add(sep);
        card.Controls.Add(lblKnowledgeStatus);
        card.Controls.Add(titleBar);

        panel.Controls.Add(card);
        return panel;
    }

    private void LstKnowledge_DrawItem(object? sender, DrawItemEventArgs e)
    {
        if (e.Index < 0 || e.Index >= lstKnowledge.Items.Count) return;
        var selected = (e.State & DrawItemState.Selected) == DrawItemState.Selected;
        e.Graphics.FillRectangle(new SolidBrush(selected ? AppleStyleHelper.SelectionBg : Color.White), e.Bounds);

        if (lstKnowledge.Items[e.Index] is not KnowledgeTemplate tpl) return;

        var x = e.Bounds.X + 10;
        var y = e.Bounds.Y + 4;
        var w = e.Bounds.Width - 14;

        using var titleFont   = new Font("Segoe UI", 9.5f, FontStyle.Bold);
        using var previewFont = new Font("Segoe UI", 8.5f);

        e.Graphics.DrawString(tpl.Title.Length > 22 ? tpl.Title[..22] + "…" : tpl.Title,
            titleFont, new SolidBrush(AppleStyleHelper.PrimaryBlue),
            new RectangleF(x, y, w, 20));

        var preview = (tpl.Summary.Length > 0 ? tpl.Summary : tpl.Content).Replace("\n", " ");
        if (preview.Length > 60) preview = preview[..60] + "…";
        e.Graphics.DrawString(preview, previewFont, new SolidBrush(Color.FromArgb(90, 90, 100)),
            new RectangleF(x, y + 22, w, 22));

        using var pen = new Pen(Color.FromArgb(240, 240, 242));
        e.Graphics.DrawLine(pen, e.Bounds.Left, e.Bounds.Bottom - 1, e.Bounds.Right, e.Bounds.Bottom - 1);
    }

    private void LstKnowledge_DoubleClick(object? sender, EventArgs e)
    {
        // 双击知识卡片：打开知识库并定位到该条
        new KnowledgeBaseForm().ShowDialog(this);
    }

    // ═══════════════════════════════════════════════════════════
    //  患者管理逻辑
    // ═══════════════════════════════════════════════════════════

    private async void LoadPatients()
    {
        try
        {
            _allPatients = await _databaseService.GetAllPatientsAsync();
            if (!chkShowDischarged.Checked)
                _allPatients = _allPatients.Where(p => !p.IsDischarged).ToList();
            ApplyPatientFilter();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"加载患者失败：{ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void ApplyPatientFilter()
    {
        var search = txtSearch?.Text.Trim() ?? "";
        var list = string.IsNullOrWhiteSpace(search)
            ? _allPatients
            : _allPatients.Where(p =>
                p.Name.Contains(search, StringComparison.OrdinalIgnoreCase) ||
                (p.MedicalRecordNumber?.Contains(search) ?? false) ||
                (p.BedNumber?.Contains(search) ?? false)).ToList();

        lstPatients.DataSource    = null;
        lstPatients.DisplayMember = nameof(Patient.Name);
        lstPatients.DataSource    = list;
        lblPatientCount.Text      = $"({list.Count})";
    }

    private async void SelectCurrentPatient()
    {
        if (lstPatients.SelectedItem is not Patient patient) return;

        _currentPatient = patient;
        lblPatientInfo.Text = $"患者：{patient.Name}    {patient.Gender}，{patient.Age} 岁    " +
                              $"住院号：{patient.MedicalRecordNumber}    床号：{patient.BedNumber}    " +
                              $"诊断：{patient.Diagnosis}";

        _currentProgressRecords = await _databaseService.GetPatientProgressRecordsAsync(patient.Id);
        LoadProgressHistory();
        SetStatus("已选择患者，可生成或新建病程记录", AppleStyleHelper.PrimaryBlue);

        // 异步加载知识卡片推荐
        _ = LoadKnowledgeRecommendationsAsync(patient);
    }

    private async Task LoadKnowledgeRecommendationsAsync(Patient patient)
    {
        try
        {
            lblKnowledgeStatus.Text = "正在匹配知识卡片…";
            var ctx = $"{patient.Diagnosis} {patient.ChiefComplaint} {patient.PhysicalExam}";
            var matched = await _matchService.MatchAsync(ctx, topN: 8);

            lstKnowledge.Items.Clear();
            foreach (var tpl in matched)
                lstKnowledge.Items.Add(tpl);

            lblKnowledgeStatus.Text = matched.Count > 0
                ? $"匹配到 {matched.Count} 条相关知识"
                : "暂无匹配知识（知识库可能为空）";
        }
        catch
        {
            lblKnowledgeStatus.Text = "推荐失败";
        }
    }

    private void LoadProgressHistory()
    {
        lvProgressHistory.Items.Clear();
        foreach (var record in _currentProgressRecords.OrderByDescending(r => r.RecordDate))
        {
            var item = new ListViewItem(record.RecordDate.ToString("yyyy-MM-dd HH:mm"));
            item.SubItems.Add(record.RecordType);
            item.SubItems.Add(record.GetShortSummary().Replace("\r", " ").Replace("\n", " "));
            item.Tag = record;
            if (record.HasDuplicate)
                item.ForeColor = AppleStyleHelper.DangerRed;
            lvProgressHistory.Items.Add(item);
        }
    }

    private void SetStatus(string text, Color? color = null)
    {
        lblStatus.Text      = text;
        lblStatus.ForeColor = color ?? AppleStyleHelper.DisabledGray;
    }

    // ═══════════════════════════════════════════════════════════
    //  按钮事件处理
    // ═══════════════════════════════════════════════════════════

    private async void BtnNewPatient_Click(object? sender, EventArgs e)
    {
        using var form = new PatientForm();
        if (form.ShowDialog(this) != DialogResult.OK || form.Patient == null) return;

        var patient = await _databaseService.CreatePatientAsync(form.Patient);
        await _folderService.SaveDangAnAsync(patient);
        LoadPatients();
    }

    private async void BtnEditPatient_Click(object? sender, EventArgs e)
    {
        if (_currentPatient == null) return;

        using var form = new PatientForm(_currentPatient);
        if (form.ShowDialog(this) != DialogResult.OK || form.Patient == null) return;

        await _databaseService.UpdatePatientAsync(form.Patient);
        await _folderService.SaveDangAnAsync(form.Patient);
        lblPatientInfo.Text = $"患者：{form.Patient.Name}    {form.Patient.Gender}，{form.Patient.Age} 岁    " +
                              $"住院号：{form.Patient.MedicalRecordNumber}    诊断：{form.Patient.Diagnosis}";
        LoadPatients();
    }

    private async void BtnDelete_Click(object? sender, EventArgs e)
    {
        if (!EnsurePatientSelected()) return;
        if (MessageBox.Show($"确定删除患者 {_currentPatient!.Name}？", "确认删除",
            MessageBoxButtons.YesNo, MessageBoxIcon.Warning) != DialogResult.Yes) return;

        await _databaseService.DeletePatientAsync(_currentPatient.Id);
        _currentPatient = null;
        _currentProgressRecords.Clear();
        lvProgressHistory.Items.Clear();
        lstKnowledge.Items.Clear();
        lblPatientInfo.Text     = "请从左侧选择或新建患者";
        lblKnowledgeStatus.Text = "选择患者后自动推荐";
        SetStatus("就绪");
        LoadPatients();
    }

    private async void BtnGenerateProgress_Click(object? sender, EventArgs e)
    {
        if (_currentPatient == null)
        {
            MessageBox.Show("请先从左侧选择患者。", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        using var form = new PatientForm(_currentPatient);
        if (form.ShowDialog(this) != DialogResult.OK || form.Patient == null) return;

        await _databaseService.UpdatePatientAsync(form.Patient);
        await _folderService.SaveDangAnAsync(form.Patient);
        _currentPatient = form.Patient;
        lblPatientInfo.Text = $"患者：{form.Patient.Name}    {form.Patient.Gender}，{form.Patient.Age} 岁    " +
                              $"住院号：{form.Patient.MedicalRecordNumber}    诊断：{form.Patient.Diagnosis}";
        LoadPatients();
        SetStatus("患者档案已更新", AppleStyleHelper.PrimaryBlue);
    }

    private async void BtnNewProgressRecord_Click(object? sender, EventArgs e)
    {
        if (!EnsurePatientSelected()) return;
        if (_currentPatient!.IsDischarged)
        {
            MessageBox.Show("该患者已出院，不能新建病程记录。", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        using var form = new NewProgressRecordForm(_currentPatient, _currentProgressRecords);
        if (form.ShowDialog(this) != DialogResult.OK || form.Record == null) return;

        var record = form.Record;
        await _databaseService.CreateProgressRecordAsync(record);
        _currentProgressRecords.Add(record);
        await _folderService.SaveProgressRecordAsync(_currentPatient, record);
        LoadProgressHistory();
        SetStatus(record.HasDuplicate ? "新病程已保存，重复内容已标红" : "新病程已保存",
            record.HasDuplicate ? AppleStyleHelper.DangerRed : AppleStyleHelper.PrimaryBlue);
    }

    private async void LvProgressHistory_DoubleClick(object? sender, EventArgs e)
    {
        if (_currentPatient == null || lvProgressHistory.SelectedItems.Count == 0) return;
        if (lvProgressHistory.SelectedItems[0].Tag is not ProgressRecord record) return;

        using var form = new NewProgressRecordForm(_currentPatient, record, _currentProgressRecords);
        if (form.ShowDialog(this) != DialogResult.OK || form.Record == null) return;

        await _databaseService.UpdateProgressRecordAsync(form.Record);
        await _folderService.SaveProgressRecordAsync(_currentPatient, form.Record);

        var idx = _currentProgressRecords.FindIndex(r => r.Id == form.Record.Id);
        if (idx >= 0) _currentProgressRecords[idx] = form.Record;

        LoadProgressHistory();
        SetStatus("病程修改已保存", AppleStyleHelper.PrimaryBlue);
    }

    private async void BtnDischarge_Click(object? sender, EventArgs e)
    {
        if (!EnsurePatientSelected()) return;
        using var form = new DischargeForm(_currentPatient!, _currentProgressRecords);
        if (form.ShowDialog(this) == DialogResult.OK)
        {
            await _databaseService.UpdatePatientAsync(_currentPatient!);
            LoadPatients();
        }
    }

    private void BtnViewFolder_Click(object? sender, EventArgs e)
    {
        if (!EnsurePatientSelected()) return;
        var folder = _folderService.GetPatientFolder(_currentPatient!);
        if (Directory.Exists(folder))
            Process.Start(new ProcessStartInfo(folder) { UseShellExecute = true });
    }

    private void BtnCombinedBrowse_Click(object? sender, EventArgs e)
    {
        if (!EnsurePatientSelected()) return;
        if (_currentProgressRecords.Count == 0)
        {
            MessageBox.Show("该患者暂无病程记录。", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }
        using var form = new CombinedBrowseForm(_currentPatient!, _currentProgressRecords, _databaseService);
        form.ShowDialog(this);
    }

    private async void BtnDeleteSelectedRecords_Click(object? sender, EventArgs e)
    {
        if (lvProgressHistory.SelectedItems.Count == 0) return;

        var count = lvProgressHistory.SelectedItems.Count;
        if (MessageBox.Show($"确定删除选中的 {count} 条病程记录？此操作不可撤销。", "确认删除",
            MessageBoxButtons.YesNo, MessageBoxIcon.Warning) != DialogResult.Yes) return;

        var toDelete = lvProgressHistory.SelectedItems
            .Cast<ListViewItem>()
            .Select(i => i.Tag as ProgressRecord)
            .Where(r => r != null)
            .ToList();

        foreach (var record in toDelete)
        {
            await _databaseService.DeleteProgressRecordAsync(record!.Id);
            _currentProgressRecords.RemoveAll(r => r.Id == record.Id);
        }

        LoadProgressHistory();
        SetStatus($"已删除 {toDelete.Count} 条病程记录", AppleStyleHelper.DangerRed);
    }

    // ═══════════════════════════════════════════════════════════
    //  辅助方法
    // ═══════════════════════════════════════════════════════════

    private bool EnsurePatientSelected()
    {
        if (_currentPatient != null) return true;
        MessageBox.Show("请先选择患者。", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
        return false;
    }

    private static string EmptyAsPending(string? value, string fallback = "待补充。") =>
        string.IsNullOrWhiteSpace(value) ? fallback : value.Trim();

    private static string SummarizeForSentence(string value)
    {
        if (string.IsNullOrWhiteSpace(value) || value == "暂无。") return "待补充";
        var compact = value.Replace("\r", " ").Replace("\n", " ").Trim();
        return compact.Length > 120 ? compact[..120] + "..." : compact;
    }
}
