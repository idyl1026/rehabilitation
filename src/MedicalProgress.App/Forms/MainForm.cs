using System.Diagnostics;
using System.Text;
using MedicalProgress.App.Helpers;
using MedicalProgress.App.Models;
using MedicalProgress.App.Services;

namespace MedicalProgress.App.Forms;

public class MainForm : Form
{
    // ── 服务 ─────────────────────────────────────────────────
    private readonly DatabaseService           _databaseService  = new();
    private readonly GeneratorService          _generatorService = new();
    private readonly PatientFolderService      _folderService    = new();
    private readonly KnowledgeCardMatchService _matchService     = new();

    // ── 控件 ─────────────────────────────────────────────────
    private ListBox  lstPatients       = null!;
    private TextBox  txtSearch         = null!;
    private Label    lblPatientInfo    = null!;
    private Label    lblPatientBadge   = null!;
    private Label    lblStatusDark     = null!;
    private Label    lblStatusTime     = null!;
    private Label    lblPatientCount   = null!;
    private ListView lvProgressHistory = null!;
    private CheckBox chkShowDischarged = null!;
    private ListBox  lstKnowledge      = null!;
    private Label    lblKnowledgeStatus = null!;
    private System.Windows.Forms.Timer _clockTimer = null!;

    // ── 状态 ─────────────────────────────────────────────────
    private Patient?             _currentPatient         = null;
    private List<ProgressRecord> _currentProgressRecords = new();
    private List<Patient>        _allPatients            = new();

    public MainForm()
    {
        InitializeComponent();
        LoadPatients();
        StartClock();
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
        MedStyleHelper.ApplyWindowStyle(this);

        // 底部状态栏先加，DockStyle.Bottom 需在 Fill 之前
        var statusBar = BuildStatusBar();
        Controls.Add(statusBar);
        Controls.Add(BuildBody());
        Controls.Add(BuildHeader());
    }

    // ── Header ────────────────────────────────────────────────

    private Control BuildHeader()
    {
        var header = new Panel
        {
            Dock      = DockStyle.Top,
            Height    = 48,
            BackColor = MedStyleHelper.HeaderBg
        };

        // 左侧医院 logo 区
        var logoPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "logo.png");
        if (File.Exists(logoPath))
        {
            try
            {
                var pb = new PictureBox
                {
                    Image    = Image.FromFile(logoPath),
                    SizeMode = PictureBoxSizeMode.Zoom,
                    Width    = 280, Height = 44,
                    Left     = 10, Top = 2,
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

        // 中间标题
        var lblTitle = new Label
        {
            Text      = "病程助手",
            Font      = MedStyleHelper.FontTitle,
            ForeColor = Color.White,
            AutoSize  = true,
            Top       = 12
        };
        header.Controls.Add(lblTitle);
        header.Resize += (_, _) => lblTitle.Left = (header.Width - lblTitle.PreferredWidth) / 2;

        // 右侧版本+时间
        var lblRight = new Label
        {
            Text      = "v1.0  |  全科医生·张医生",
            Font      = MedStyleHelper.FontSmall,
            ForeColor = Color.FromArgb(160, 200, 255),
            AutoSize  = true,
            Top       = 14
        };
        header.Controls.Add(lblRight);
        header.Resize += (_, _) => lblRight.Left = header.Width - lblRight.PreferredWidth - 16;

        return header;
    }

    private static Control BuildTextLogo()
    {
        var p = new Panel { Width = 320, Height = 48, Left = 10, Top = 0, BackColor = Color.Transparent };
        p.Controls.Add(new Label
        {
            Text      = "安徽医科大学第二附属医院",
            Font      = new Font("Microsoft YaHei UI", 11f, FontStyle.Bold),
            ForeColor = Color.FromArgb(160, 200, 255),
            AutoSize  = true, Left = 0, Top = 5
        });
        p.Controls.Add(new Label
        {
            Text      = "The 2nd Affiliated Hospital of Anhui Medical University",
            Font      = new Font("Microsoft YaHei UI", 7.5f),
            ForeColor = Color.FromArgb(120, 160, 210),
            AutoSize  = true, Left = 0, Top = 30
        });
        return p;
    }

    // ── 状态栏 ────────────────────────────────────────────────

    private Panel BuildStatusBar()
    {
        var bar = new Panel
        {
            Dock      = DockStyle.Bottom,
            Height    = 28,
            BackColor = MedStyleHelper.StatusBarBg
        };

        var flow = new FlowLayoutPanel
        {
            Dock          = DockStyle.Fill,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents  = false,
            BackColor     = Color.Transparent,
            Padding       = new Padding(8, 4, 0, 0)
        };

        Label MakeStatusLabel(string text, int width = 0)
        {
            var lbl = new Label
            {
                Text      = text,
                Font      = MedStyleHelper.FontSmall,
                ForeColor = Color.FromArgb(180, 200, 220),
                AutoSize  = width == 0,
                Width     = width > 0 ? width : 0
            };
            return lbl;
        }

        var dbLabel   = MakeStatusLabel("SQLite 本地数据库");
        var sep1      = MakeStatusLabel("  |  ");
        sep1.ForeColor = Color.FromArgb(60, 80, 100);
        var userLabel  = MakeStatusLabel("全科医生·张医生");
        var sep2       = MakeStatusLabel("  |  ");
        sep2.ForeColor = Color.FromArgb(60, 80, 100);
        lblStatusTime  = MakeStatusLabel(DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"), 180);
        lblStatusDark  = MakeStatusLabel("就绪", 200);

        flow.Controls.AddRange(new Control[] { dbLabel, sep1, userLabel, sep2, lblStatusTime, lblStatusDark });
        bar.Controls.Add(flow);
        return bar;
    }

    private void StartClock()
    {
        _clockTimer = new System.Windows.Forms.Timer { Interval = 1000 };
        _clockTimer.Tick += (_, _) =>
        {
            if (lblStatusTime != null && !lblStatusTime.IsDisposed)
                lblStatusTime.Text = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
        };
        _clockTimer.Start();
        FormClosed += (_, _) => _clockTimer.Stop();
    }

    // ── Body（三栏布局） ──────────────────────────────────────

    private Control BuildBody()
    {
        var outer = new TableLayoutPanel
        {
            Dock        = DockStyle.Fill,
            ColumnCount = 3,
            RowCount    = 1,
            BackColor   = MedStyleHelper.ContentBg
        };
        outer.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 240));
        outer.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,  100));
        outer.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 280));

        outer.Controls.Add(BuildPatientPanel(),   0, 0);
        outer.Controls.Add(BuildWorkPanel(),      1, 0);
        outer.Controls.Add(BuildKnowledgePanel(), 2, 0);

        return outer;
    }

    // ── 左栏：患者工作台 ─────────────────────────────────────

    private Control BuildPatientPanel()
    {
        var panel = new Panel
        {
            Dock      = DockStyle.Fill,
            BackColor = MedStyleHelper.SidebarBg,
            Padding   = new Padding(8, 8, 4, 8)
        };

        var card = MedStyleHelper.CreateCard();
        card.Dock    = DockStyle.Fill;
        card.Padding = new Padding(0);

        // 标题行
        var titleBar = new Panel
        {
            Dock      = DockStyle.Top,
            Height    = 40,
            BackColor = Color.White,
            Padding   = new Padding(12, 8, 8, 0)
        };
        var lblTitle = new Label
        {
            Text      = "患者工作台",
            Font      = MedStyleHelper.FontBold,
            ForeColor = MedStyleHelper.TextDark,
            Dock      = DockStyle.Left,
            Width     = 100,
            TextAlign = ContentAlignment.MiddleLeft
        };
        lblPatientCount = new Label
        {
            Text      = "",
            Font      = MedStyleHelper.FontSmall,
            ForeColor = Color.White,
            BackColor = MedStyleHelper.PrimaryBlue,
            Dock      = DockStyle.Right,
            Width     = 36,
            TextAlign = ContentAlignment.MiddleCenter
        };
        titleBar.Controls.Add(lblTitle);
        titleBar.Controls.Add(lblPatientCount);
        titleBar.Controls.Add(new Panel { Dock = DockStyle.Bottom, Height = 1, BackColor = MedStyleHelper.BorderColor });

        // 搜索框
        var searchBar = new Panel
        {
            Dock      = DockStyle.Top,
            Height    = 38,
            BackColor = Color.White,
            Padding   = new Padding(8, 5, 8, 0)
        };
        txtSearch = MedStyleHelper.CreateSearchBox("搜索患者...");
        txtSearch.Dock         = DockStyle.Fill;
        txtSearch.TextChanged += (_, _) => ApplyPatientFilter();
        searchBar.Controls.Add(txtSearch);

        // 显示已出院
        chkShowDischarged = new CheckBox
        {
            Dock      = DockStyle.Top,
            Height    = 26,
            Text      = "显示已出院患者",
            Font      = MedStyleHelper.FontSmall,
            ForeColor = MedStyleHelper.TextGray,
            BackColor = Color.White,
            Padding   = new Padding(12, 4, 0, 0)
        };
        chkShowDischarged.CheckedChanged += (_, _) => LoadPatients();
        var sepChk = new Panel { Dock = DockStyle.Top, Height = 1, BackColor = MedStyleHelper.BorderColor };

        // 患者列表
        lstPatients = new ListBox
        {
            Dock           = DockStyle.Fill,
            Font           = MedStyleHelper.FontBody,
            IntegralHeight = false,
            BorderStyle    = BorderStyle.None,
            BackColor      = Color.White,
            ItemHeight     = 56,
            DrawMode       = DrawMode.OwnerDrawFixed
        };
        lstPatients.DrawItem             += LstPatients_DrawItem;
        lstPatients.SelectedIndexChanged += (_, _) => SelectCurrentPatient();
        lstPatients.DoubleClick          += BtnEditPatient_Click;

        // 底部按钮区（2x2）
        var btnGrid = new TableLayoutPanel
        {
            Dock        = DockStyle.Bottom,
            Height      = 76,
            ColumnCount = 2,
            RowCount    = 2,
            BackColor   = Color.White,
            Padding     = new Padding(8, 6, 8, 8)
        };
        btnGrid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
        btnGrid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
        btnGrid.RowStyles.Add(new RowStyle(SizeType.Percent, 50));
        btnGrid.RowStyles.Add(new RowStyle(SizeType.Percent, 50));
        btnGrid.Controls.Add(new Panel { Dock = DockStyle.Top, Height = 1, BackColor = MedStyleHelper.BorderColor }, 0, 0);

        var btnNew    = MedStyleHelper.CreatePrimaryBtn("新建患者");
        var btnDel    = MedStyleHelper.CreateDangerBtn("删除患者");
        var btnFolder = MedStyleHelper.CreateSecondaryBtn("打开目录");
        var btnKb     = MedStyleHelper.CreateSecondaryBtn("知识库");
        foreach (var b in new[] { btnNew, btnDel, btnFolder, btnKb })
        { b.Dock = DockStyle.Fill; b.Height = 30; b.Margin = new Padding(2); }

        btnNew.Click    += BtnNewPatient_Click;
        btnDel.Click    += BtnDelete_Click;
        btnFolder.Click += BtnViewFolder_Click;
        btnKb.Click     += (_, _) => new KnowledgeBaseForm().ShowDialog(this);

        btnGrid.Controls.Add(btnNew,    0, 0);
        btnGrid.Controls.Add(btnDel,    1, 0);
        btnGrid.Controls.Add(btnFolder, 0, 1);
        btnGrid.Controls.Add(btnKb,     1, 1);

        // 组装
        var sepBtnTop = new Panel { Dock = DockStyle.Bottom, Height = 1, BackColor = MedStyleHelper.BorderColor };
        card.Controls.Add(lstPatients);
        card.Controls.Add(sepChk);
        card.Controls.Add(chkShowDischarged);
        card.Controls.Add(searchBar);
        card.Controls.Add(titleBar);
        card.Controls.Add(sepBtnTop);
        card.Controls.Add(btnGrid);

        panel.Controls.Add(card);
        return panel;
    }

    private void LstPatients_DrawItem(object? sender, DrawItemEventArgs e)
    {
        if (e.Index < 0 || e.Index >= lstPatients.Items.Count) return;
        var selected = (e.State & DrawItemState.Selected) == DrawItemState.Selected;
        var bg = selected ? MedStyleHelper.LightBlue : (e.Index % 2 == 0 ? Color.White : Color.FromArgb(248, 250, 253));
        e.Graphics.FillRectangle(new SolidBrush(bg), e.Bounds);

        var patient = lstPatients.Items[e.Index] as Patient;
        if (patient == null) return;

        var x = e.Bounds.X + 8;
        var y = e.Bounds.Y + 4;
        var w = e.Bounds.Width - 16;

        // 左侧床号 Badge
        var bedText  = string.IsNullOrWhiteSpace(patient.BedNumber) ? "?" : patient.BedNumber;
        var badgeRect = new Rectangle(x, y + 4, 32, 22);
        MedStyleHelper.FillRoundRect(e.Graphics, badgeRect, 4, MedStyleHelper.PrimaryBlue);
        using var wBrush = new SolidBrush(Color.White);
        e.Graphics.DrawString(bedText, MedStyleHelper.FontSmall, wBrush,
            new RectangleF(badgeRect.X, badgeRect.Y, badgeRect.Width, badgeRect.Height),
            new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center });

        // 姓名
        using var nameBrush = new SolidBrush(selected ? MedStyleHelper.PrimaryBlue : MedStyleHelper.TextDark);
        e.Graphics.DrawString(patient.Name,
            MedStyleHelper.FontBold, nameBrush,
            new RectangleF(x + 38, y + 2, w - 80, 20));

        // 诊断（小字灰色）
        var diag = patient.Diagnosis.Length > 16 ? patient.Diagnosis[..16] + "..." : patient.Diagnosis;
        using var gBrush = new SolidBrush(MedStyleHelper.TextGray);
        e.Graphics.DrawString(diag, MedStyleHelper.FontSmall, gBrush,
            new RectangleF(x + 38, y + 24, w - 80, 18));

        // 右侧状态 Badge
        var statusColor = patient.IsDischarged ? MedStyleHelper.TextGray : MedStyleHelper.SuccessGreen;
        var statusText  = patient.IsDischarged ? "出院" : "在院";
        var statusRect  = new Rectangle(e.Bounds.Right - 40, y + 14, 34, 18);
        MedStyleHelper.FillRoundRect(e.Graphics, statusRect, 4, statusColor);
        e.Graphics.DrawString(statusText, MedStyleHelper.FontSmall, wBrush,
            new RectangleF(statusRect.X, statusRect.Y, statusRect.Width, statusRect.Height),
            new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center });

        // 底线
        using var pen = new Pen(MedStyleHelper.BorderColor);
        e.Graphics.DrawLine(pen, e.Bounds.Left, e.Bounds.Bottom - 1, e.Bounds.Right, e.Bounds.Bottom - 1);
    }

    // ── 中栏：病程工作区 ─────────────────────────────────────

    private Control BuildWorkPanel()
    {
        var panel = new Panel
        {
            Dock      = DockStyle.Fill,
            BackColor = MedStyleHelper.ContentBg,
            Padding   = new Padding(6, 8, 6, 8)
        };

        var outer = MedStyleHelper.CreateCard();
        outer.Dock    = DockStyle.Fill;
        outer.Padding = new Padding(0);

        // 患者信息卡（浅蓝背景）
        var infoBar = new Panel
        {
            Dock      = DockStyle.Top,
            Height    = 52,
            BackColor = MedStyleHelper.LightBlue,
            Padding   = new Padding(14, 0, 14, 0)
        };
        lblPatientInfo = new Label
        {
            Dock      = DockStyle.Fill,
            Font      = MedStyleHelper.FontBold,
            ForeColor = MedStyleHelper.PrimaryBlue,
            TextAlign = ContentAlignment.MiddleLeft,
            Text      = "请从左侧选择或新建患者"
        };
        lblPatientBadge = new Label
        {
            Dock      = DockStyle.Right,
            Width     = 60,
            Font      = MedStyleHelper.FontSmall,
            ForeColor = Color.White,
            BackColor = MedStyleHelper.TextGray,
            TextAlign = ContentAlignment.MiddleCenter,
            Text      = "未选择"
        };
        infoBar.Controls.Add(lblPatientInfo);
        infoBar.Controls.Add(lblPatientBadge);
        infoBar.Controls.Add(new Panel { Dock = DockStyle.Bottom, Height = 1, BackColor = MedStyleHelper.BorderColor });

        // 快捷操作按钮行（4个大按钮）
        var toolbar = BuildWorkToolbar();

        // 病程历史记录区
        var histPanel = BuildHistoryPanel();

        outer.Controls.Add(histPanel);
        outer.Controls.Add(toolbar);
        outer.Controls.Add(infoBar);

        panel.Controls.Add(outer);
        return panel;
    }

    private Control BuildWorkToolbar()
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

        var btnFirst     = MedStyleHelper.CreateSecondaryBtn("患者档案", 110);
        var btnNew       = MedStyleHelper.CreatePrimaryBtn("新建病程记录", 130);
        var btnBrowse    = MedStyleHelper.CreateSecondaryBtn("联合浏览", 100);
        var btnDischarge = MedStyleHelper.CreateDangerBtn("出院归档", 100);

        btnFirst.Click     += BtnGenerateProgress_Click;
        btnNew.Click       += BtnNewProgressRecord_Click;
        btnBrowse.Click    += BtnCombinedBrowse_Click;
        btnDischarge.Click += BtnDischarge_Click;

        flow.Controls.AddRange(new Control[] { btnFirst, btnNew, btnBrowse, btnDischarge });

        bar.Controls.Add(flow);
        bar.Controls.Add(new Panel { Dock = DockStyle.Bottom, Height = 1, BackColor = MedStyleHelper.BorderColor });
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

        var titleBar = new Panel { Dock = DockStyle.Top, Height = 32, BackColor = Color.Transparent };
        titleBar.Controls.Add(new Label
        {
            Text      = "病程历史记录（双击编辑）",
            Font      = MedStyleHelper.FontBold,
            ForeColor = MedStyleHelper.TextDark,
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
            Font          = MedStyleHelper.FontBody,
            BorderStyle   = BorderStyle.None,
            BackColor     = Color.White
        };
        lvProgressHistory.Columns.Add("日期",   160);
        lvProgressHistory.Columns.Add("类型",   140);
        lvProgressHistory.Columns.Add("摘要",   700);

        // 交替行颜色
        lvProgressHistory.OwnerDraw = true;
        lvProgressHistory.DrawColumnHeader += (_, e) => e.DrawDefault = true;
        lvProgressHistory.DrawSubItem      += (_, e) => e.DrawDefault = true;
        lvProgressHistory.DrawItem         += (s, e) =>
        {
            var bg = e.ItemIndex % 2 == 0 ? Color.White : Color.FromArgb(248, 250, 253);
            e.Item.BackColor = bg;
            e.DrawDefault = true;
        };

        lvProgressHistory.DoubleClick      += LvProgressHistory_DoubleClick;

        // 右键菜单
        var ctxMenu   = new ContextMenuStrip { Font = MedStyleHelper.FontBody };
        var ctxDelete = new ToolStripMenuItem("删除选中记录") { ForeColor = MedStyleHelper.DangerRed };
        ctxDelete.Click += BtnDeleteSelectedRecords_Click;
        ctxMenu.Items.Add(ctxDelete);
        lvProgressHistory.ContextMenuStrip = ctxMenu;

        panel.Controls.Add(lvProgressHistory);
        panel.Controls.Add(titleBar);
        return panel;
    }

    // ── 右栏：智能推荐 ───────────────────────────────────────

    private Control BuildKnowledgePanel()
    {
        var panel = new Panel
        {
            Dock      = DockStyle.Fill,
            BackColor = MedStyleHelper.SidebarBg,
            Padding   = new Padding(4, 8, 8, 8)
        };

        var card = MedStyleHelper.CreateCard();
        card.Dock    = DockStyle.Fill;
        card.Padding = new Padding(0);

        // 标题行
        var titleBar = new Panel { Dock = DockStyle.Top, Height = 50, BackColor = Color.White, Padding = new Padding(12, 8, 8, 0) };
        var lblTitle = new Label
        {
            Text      = "智能推荐",
            Font      = MedStyleHelper.FontBold,
            ForeColor = MedStyleHelper.TextDark,
            Dock      = DockStyle.Left,
            Width     = 120,
            TextAlign = ContentAlignment.TopLeft
        };
        lblKnowledgeStatus = new Label
        {
            Text      = "根据当前患者自动匹配",
            Font      = MedStyleHelper.FontSmall,
            ForeColor = MedStyleHelper.TextGray,
            Dock      = DockStyle.Bottom,
            Height    = 18,
            TextAlign = ContentAlignment.MiddleLeft
        };
        titleBar.Controls.Add(lblTitle);
        titleBar.Controls.Add(lblKnowledgeStatus);
        titleBar.Controls.Add(new Panel { Dock = DockStyle.Bottom, Height = 1, BackColor = MedStyleHelper.BorderColor });

        lstKnowledge = new ListBox
        {
            Dock           = DockStyle.Fill,
            Font           = MedStyleHelper.FontSmall,
            IntegralHeight = false,
            BorderStyle    = BorderStyle.None,
            BackColor      = Color.White,
            ItemHeight     = 66,
            DrawMode       = DrawMode.OwnerDrawFixed
        };
        lstKnowledge.DrawItem   += LstKnowledge_DrawItem;
        lstKnowledge.DoubleClick += LstKnowledge_DoubleClick;

        card.Controls.Add(lstKnowledge);
        card.Controls.Add(titleBar);

        panel.Controls.Add(card);
        return panel;
    }

    private void LstKnowledge_DrawItem(object? sender, DrawItemEventArgs e)
    {
        if (e.Index < 0 || e.Index >= lstKnowledge.Items.Count) return;
        var selected = (e.State & DrawItemState.Selected) == DrawItemState.Selected;
        e.Graphics.FillRectangle(new SolidBrush(selected ? MedStyleHelper.LightBlue : Color.White), e.Bounds);

        if (lstKnowledge.Items[e.Index] is not KnowledgeTemplate tpl) return;

        var x = e.Bounds.X + 10;
        var y = e.Bounds.Y + 5;
        var w = e.Bounds.Width - 16;

        // 蓝色标题
        e.Graphics.DrawString(
            tpl.Title.Length > 24 ? tpl.Title[..24] + "..." : tpl.Title,
            MedStyleHelper.FontBold,
            new SolidBrush(MedStyleHelper.PrimaryBlue),
            new RectangleF(x, y, w - 60, 20));

        // 分类 Badge（右上角）
        var catText = tpl.Category?.Name ?? tpl.TemplateType ?? "";
        if (catText.Length > 6) catText = catText[..6];
        var catRect = new Rectangle(e.Bounds.Right - 56, y, 48, 18);
        MedStyleHelper.FillRoundRect(e.Graphics, catRect, 4, MedStyleHelper.LightBlue);
        e.Graphics.DrawString(catText, MedStyleHelper.FontSmall,
            new SolidBrush(MedStyleHelper.PrimaryBlue),
            new RectangleF(catRect.X, catRect.Y, catRect.Width, catRect.Height),
            new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center });

        // 灰色预览（2行）
        var preview = (tpl.Summary.Length > 0 ? tpl.Summary : tpl.Content).Replace("\n", " ").Replace("\r", "");
        if (preview.Length > 80) preview = preview[..80] + "...";
        e.Graphics.DrawString(preview, MedStyleHelper.FontSmall,
            new SolidBrush(MedStyleHelper.TextGray),
            new RectangleF(x, y + 24, w, 34));

        using var pen = new Pen(MedStyleHelper.BorderColor);
        e.Graphics.DrawLine(pen, e.Bounds.Left, e.Bounds.Bottom - 1, e.Bounds.Right, e.Bounds.Bottom - 1);
    }

    private void LstKnowledge_DoubleClick(object? sender, EventArgs e)
    {
        new KnowledgeBaseForm().ShowDialog(this);
    }

    // ═══════════════════════════════════════════════════════════
    //  患者管理逻辑（原样保留）
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
        lblPatientCount.Text      = list.Count.ToString();
    }

    private async void SelectCurrentPatient()
    {
        if (lstPatients.SelectedItem is not Patient patient) return;

        _currentPatient = patient;
        lblPatientInfo.Text = $"患者：{patient.Name}    {patient.Gender}，{patient.Age} 岁    " +
                              $"住院号：{patient.MedicalRecordNumber}    床号：{patient.BedNumber}    " +
                              $"诊断：{patient.Diagnosis}";

        // 更新状态 Badge
        lblPatientBadge.Text      = patient.IsDischarged ? "出院" : "在院";
        lblPatientBadge.BackColor = patient.IsDischarged ? MedStyleHelper.TextGray : MedStyleHelper.SuccessGreen;

        _currentProgressRecords = await _databaseService.GetPatientProgressRecordsAsync(patient.Id);
        LoadProgressHistory();
        SetStatus($"已选择患者：{patient.Name}");

        _ = LoadKnowledgeRecommendationsAsync(patient);
    }

    private async Task LoadKnowledgeRecommendationsAsync(Patient patient)
    {
        try
        {
            lblKnowledgeStatus.Text = "正在匹配知识卡片...";
            var ctx     = $"{patient.Diagnosis} {patient.ChiefComplaint} {patient.PhysicalExam}";
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
                item.ForeColor = MedStyleHelper.DangerRed;
            lvProgressHistory.Items.Add(item);
        }
    }

    private void SetStatus(string text)
    {
        if (lblStatusDark != null && !lblStatusDark.IsDisposed)
            lblStatusDark.Text = text;
    }

    // ═══════════════════════════════════════════════════════════
    //  按钮事件处理（保留原有业务逻辑）
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
        lblPatientInfo.Text       = "请从左侧选择或新建患者";
        lblPatientBadge.Text      = "未选择";
        lblPatientBadge.BackColor = MedStyleHelper.TextGray;
        lblKnowledgeStatus.Text   = "根据当前患者自动匹配";
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
        SetStatus("患者档案已更新");
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
        SetStatus(record.HasDuplicate ? "新病程已保存，重复内容已标红" : "新病程已保存");
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
        SetStatus("病程修改已保存");
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
        SetStatus($"已删除 {toDelete.Count} 条病程记录");
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
}
