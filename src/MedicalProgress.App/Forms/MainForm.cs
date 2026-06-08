using System.Diagnostics;
using System.Text;
using MedicalProgress.App.Models;
using MedicalProgress.App.Services;

namespace MedicalProgress.App.Forms;

public class MainForm : Form
{
    // ── 颜色主题 ──────────────────────────────────────────────
    private static readonly Color ClrHeaderBg    = Color.FromArgb(10,  90, 160);
    private static readonly Color ClrHeaderText  = Color.White;
    private static readonly Color ClrPanelBlue   = Color.FromArgb(232, 245, 253);
    private static readonly Color ClrPanelGreen  = Color.FromArgb(232, 248, 240);
    private static readonly Color ClrInfoBar     = Color.FromArgb(213, 237, 255);
    private static readonly Color ClrStatusBar   = Color.FromArgb(245, 250, 253);
    private static readonly Color ClrBtnBlue     = Color.FromArgb(21, 101, 192);
    private static readonly Color ClrBtnGreen    = Color.FromArgb(27, 130, 80);
    private static readonly Color ClrBtnRed      = Color.FromArgb(198, 40,  40);
    private static readonly Color ClrBtnTeal     = Color.FromArgb(0,  137, 123);
    private static readonly Color ClrBtnPurple   = Color.FromArgb(100, 55, 175);
    private static readonly Color ClrBtnGray     = Color.FromArgb(96, 108, 118);
    private static readonly Font  FontMain       = new("Microsoft YaHei UI", 9);
    private static readonly Font  FontBold       = new("Microsoft YaHei UI", 9,  FontStyle.Bold);
    private static readonly Font  FontLarge      = new("Microsoft YaHei UI", 10, FontStyle.Bold);

    // ── 服务 ─────────────────────────────────────────────────
    private readonly DatabaseService      _databaseService  = new();
    private readonly GeneratorService     _generatorService = new();
    private readonly PatientFolderService _folderService    = new();

    // ── 控件 ─────────────────────────────────────────────────
    private ListBox  lstPatients       = null!;
    private Label    lblPatientInfo    = null!;
    private Label    lblStatus         = null!;
    private Label    lblPatientCount   = null!;
    private ListView lvProgressHistory = null!;
    private CheckBox chkShowDischarged = null!;

    // ── 状态 ─────────────────────────────────────────────────
    private Patient?             _currentPatient         = null;
    private List<ProgressRecord> _currentProgressRecords = new();

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
        Text            = "病程助手 v1.0";
        Size            = new Size(1440, 880);
        MinimumSize     = new Size(1200, 720);
        StartPosition   = FormStartPosition.CenterScreen;
        BackColor       = Color.FromArgb(245, 249, 253);

        var header = BuildHeader();
        var body   = BuildBody();

        Controls.Add(body);
        Controls.Add(header);
    }

    // ── Header ────────────────────────────────────────────────

    private Control BuildHeader()
    {
        var header = new Panel
        {
            Dock      = DockStyle.Top,
            Height    = 76,
            BackColor = ClrHeaderBg
        };

        var logoPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "logo.png");
        Control logoCtrl;
        if (File.Exists(logoPath))
        {
            try
            {
                var pb = new PictureBox
                {
                    Image    = Image.FromFile(logoPath),
                    SizeMode = PictureBoxSizeMode.Zoom,
                    Width    = 300,
                    Height   = 66,
                    Left     = 10,
                    Top      = 5,
                    BackColor= Color.Transparent
                };
                logoCtrl = pb;
            }
            catch { logoCtrl = BuildTextLogo(); }
        }
        else
        {
            logoCtrl = BuildTextLogo();
        }

        var lblTitle = new Label
        {
            Text      = "病 程 助 手",
            Font      = new Font("Microsoft YaHei UI", 22, FontStyle.Bold),
            ForeColor = ClrHeaderText,
            AutoSize  = true,
            Top       = 18
        };
        header.Controls.Add(lblTitle);
        header.Resize += (_, _) => lblTitle.Left = (header.Width - lblTitle.PreferredWidth) / 2;

        var lblInfo = new Label
        {
            Text      = "v1.0    作者：刘奕\nliuyi@ahmu.edu.cn",
            Font      = new Font("Microsoft YaHei UI", 8.5f),
            ForeColor = Color.FromArgb(180, 220, 255),
            AutoSize  = true,
            TextAlign = ContentAlignment.TopRight,
            Top       = 18
        };
        header.Controls.Add(lblInfo);
        header.Resize += (_, _) => lblInfo.Left = header.Width - lblInfo.PreferredWidth - 20;

        header.Controls.Add(logoCtrl);
        return header;
    }

    private static Control BuildTextLogo()
    {
        var p = new Panel
        {
            Width    = 320,
            Height   = 66,
            Left     = 10,
            Top      = 5,
            BackColor= Color.Transparent
        };
        p.Controls.Add(new Label
        {
            Text      = "安徽医科大学第二附属医院",
            Font      = new Font("Microsoft YaHei UI", 13, FontStyle.Bold),
            ForeColor = Color.FromArgb(160, 215, 255),
            AutoSize  = true,
            Left      = 0,
            Top       = 6
        });
        p.Controls.Add(new Label
        {
            Text      = "The 2nd Affiliated Hospital of Anhui Medical Univ.",
            Font      = new Font("Arial", 8),
            ForeColor = Color.FromArgb(130, 185, 230),
            AutoSize  = true,
            Left      = 0,
            Top       = 40
        });
        return p;
    }

    // ── Body ─────────────────────────────────────────────────

    private Control BuildBody()
    {
        var splitter = new SplitContainer
        {
            Dock             = DockStyle.Fill,
            SplitterDistance = 218,
            Panel1MinSize    = 180,
            BackColor        = Color.FromArgb(200, 220, 240)
        };
        splitter.SplitterMoved += (_, _) =>
        {
            if (splitter.SplitterDistance > 280) splitter.SplitterDistance = 280;
        };

        splitter.Panel1.Controls.Add(BuildPatientPanel());
        splitter.Panel2.Controls.Add(BuildWorkPanel());

        return splitter;
    }

    // ── 患者列表面板 ──────────────────────────────────────────

    private Control BuildPatientPanel()
    {
        var panel = new Panel { Dock = DockStyle.Fill, BackColor = ClrPanelBlue, Padding = new Padding(8) };

        var titleRow = new Panel { Dock = DockStyle.Top, Height = 34, BackColor = Color.Transparent };
        var lblTitle = new Label
        {
            Text      = "患者列表",
            Font      = FontLarge,
            ForeColor = ClrBtnBlue,
            AutoSize  = true,
            Left      = 2,
            Top       = 7
        };
        lblPatientCount = new Label
        {
            Text      = "",
            Font      = FontMain,
            ForeColor = Color.FromArgb(100, 130, 160),
            AutoSize  = true,
            Top       = 9
        };
        titleRow.Controls.AddRange(new Control[] { lblTitle, lblPatientCount });
        lblTitle.SizeChanged += (_, _) => lblPatientCount.Left = lblTitle.Right + 6;

        chkShowDischarged = new CheckBox
        {
            Dock      = DockStyle.Top,
            Height    = 26,
            Text      = "显示已出院患者",
            Font      = FontMain,
            ForeColor = Color.FromArgb(80, 110, 140),
            BackColor = Color.Transparent
        };
        chkShowDischarged.CheckedChanged += (_, _) => LoadPatients();

        var sep = new Panel { Dock = DockStyle.Top, Height = 1, BackColor = Color.FromArgb(195, 220, 240) };

        lstPatients = new ListBox
        {
            Dock            = DockStyle.Fill,
            Font            = FontMain,
            IntegralHeight  = false,
            BorderStyle     = BorderStyle.None,
            BackColor       = Color.FromArgb(245, 251, 255),
            ItemHeight      = 26,
            DrawMode        = DrawMode.OwnerDrawFixed
        };
        lstPatients.DrawItem             += LstPatients_DrawItem;
        lstPatients.SelectedIndexChanged += (_, _) => SelectCurrentPatient();
        lstPatients.DoubleClick          += BtnEditPatient_Click;

        // 2×2 按钮网格
        var btnGrid = new TableLayoutPanel
        {
            Dock        = DockStyle.Bottom,
            Height      = 92,
            ColumnCount = 2,
            RowCount    = 2,
            BackColor   = Color.Transparent,
            Padding     = new Padding(0, 6, 0, 0),
            CellBorderStyle = TableLayoutPanelCellBorderStyle.None
        };
        btnGrid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
        btnGrid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
        btnGrid.RowStyles.Add(new RowStyle(SizeType.Percent, 50));
        btnGrid.RowStyles.Add(new RowStyle(SizeType.Percent, 50));

        var btnNew    = MakeGridBtn("新建患者", ClrBtnBlue);
        var btnDel    = MakeGridBtn("删除患者", ClrBtnRed);
        var btnFolder = MakeGridBtn("打开目录", ClrBtnTeal);
        var btnKb     = MakeGridBtn("知识库",   ClrBtnGray);

        btnNew.Click    += BtnNewPatient_Click;
        btnDel.Click    += BtnDelete_Click;
        btnFolder.Click += BtnViewFolder_Click;
        btnKb.Click     += (_, _) => new KnowledgeBaseForm().ShowDialog(this);

        btnGrid.Controls.Add(btnNew,    0, 0);
        btnGrid.Controls.Add(btnDel,    1, 0);
        btnGrid.Controls.Add(btnFolder, 0, 1);
        btnGrid.Controls.Add(btnKb,     1, 1);

        var bottom = new Panel { Dock = DockStyle.Bottom, Height = 4, BackColor = ClrBtnGreen };

        panel.Controls.Add(lstPatients);
        panel.Controls.Add(sep);
        panel.Controls.Add(chkShowDischarged);
        panel.Controls.Add(titleRow);
        panel.Controls.Add(btnGrid);
        panel.Controls.Add(bottom);
        return panel;
    }

    private void LstPatients_DrawItem(object? sender, DrawItemEventArgs e)
    {
        if (e.Index < 0 || e.Index >= lstPatients.Items.Count) return;
        var selected = (e.State & DrawItemState.Selected) == DrawItemState.Selected;
        var bg = selected
            ? ClrBtnBlue
            : e.Index % 2 == 0 ? Color.FromArgb(245, 251, 255) : Color.FromArgb(232, 244, 254);
        e.Graphics.FillRectangle(new SolidBrush(bg), e.Bounds);

        // 正确取患者姓名
        var patient = lstPatients.Items[e.Index] as Patient;
        var text = patient?.Name ?? lstPatients.Items[e.Index]?.ToString() ?? "";

        using var brush = new SolidBrush(selected ? Color.White : Color.FromArgb(30, 50, 80));
        e.Graphics.DrawString(text, FontMain, brush, e.Bounds.X + 6, e.Bounds.Y + 5);
    }

    private static Button MakeGridBtn(string text, Color color)
    {
        var b = new Button
        {
            Text      = text,
            Dock      = DockStyle.Fill,
            BackColor = color,
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            Font      = FontMain,
            Margin    = new Padding(2)
        };
        b.FlatAppearance.BorderSize = 0;
        return b;
    }

    // ── 工作区面板 ────────────────────────────────────────────

    private Control BuildWorkPanel()
    {
        var outer = new Panel { Dock = DockStyle.Fill, BackColor = Color.White };

        // 患者信息栏
        var infoBar = new Panel
        {
            Dock      = DockStyle.Top,
            Height    = 46,
            BackColor = ClrInfoBar,
            Padding   = new Padding(12, 0, 12, 0)
        };
        lblPatientInfo = new Label
        {
            Dock      = DockStyle.Fill,
            Font      = FontBold,
            ForeColor = Color.FromArgb(20, 60, 110),
            TextAlign = ContentAlignment.MiddleLeft,
            Text      = "请从左侧选择或新建患者"
        };
        infoBar.Controls.Add(lblPatientInfo);

        // 工具栏
        var toolbar = BuildToolbar();

        // 历史记录占满其余空间
        var histPanel = BuildHistoryPanel();

        // 状态栏
        lblStatus = new Label
        {
            Dock      = DockStyle.Bottom,
            Height    = 28,
            Text      = "就绪",
            Font      = FontMain,
            ForeColor = Color.FromArgb(80, 100, 120),
            TextAlign = ContentAlignment.MiddleLeft,
            Padding   = new Padding(12, 0, 0, 0),
            BackColor = ClrStatusBar
        };

        outer.Controls.Add(histPanel);
        outer.Controls.Add(lblStatus);
        outer.Controls.Add(toolbar);
        outer.Controls.Add(infoBar);
        return outer;
    }

    private Control BuildToolbar()
    {
        var bar = new Panel
        {
            Dock      = DockStyle.Top,
            Height    = 52,
            BackColor = Color.FromArgb(250, 253, 255),
            Padding   = new Padding(8, 8, 8, 8)
        };

        var flow = new FlowLayoutPanel
        {
            Dock          = DockStyle.Fill,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents  = false,
            BackColor     = Color.Transparent
        };

        var btnFirst    = MakeToolBtn("患者档案",     ClrBtnBlue,   110);
        var btnNew      = MakeToolBtn("新建病程记录", ClrBtnGreen,  120);
        var btnBrowse   = MakeToolBtn("联合浏览",     ClrBtnTeal,   100);
        var btnDischarge= MakeToolBtn("出院归档",     ClrBtnRed,    100);

        btnFirst.Click    += BtnGenerateProgress_Click;
        btnNew.Click      += BtnNewProgressRecord_Click;
        btnBrowse.Click   += BtnCombinedBrowse_Click;
        btnDischarge.Click+= BtnDischarge_Click;

        flow.Controls.AddRange(new Control[] { btnFirst, btnNew, btnBrowse, btnDischarge });

        var sepLine = new Panel { Dock = DockStyle.Bottom, Height = 1, BackColor = Color.FromArgb(200, 220, 235) };

        bar.Controls.Add(flow);
        bar.Controls.Add(sepLine);
        return bar;
    }

    private static Button MakeToolBtn(string text, Color color, int width)
    {
        var b = new Button
        {
            Text      = text,
            Width     = width,
            Height    = 34,
            BackColor = color,
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            Font      = FontMain,
            Margin    = new Padding(0, 0, 8, 0)
        };
        b.FlatAppearance.BorderSize = 0;
        return b;
    }

    // 历史记录面板（全高）
    private Control BuildHistoryPanel()
    {
        var panel = new Panel { Dock = DockStyle.Fill, BackColor = ClrPanelGreen, Padding = new Padding(8, 4, 8, 4) };

        var titleBar = new Panel { Dock = DockStyle.Top, Height = 30, BackColor = Color.Transparent };
        titleBar.Controls.Add(new Label
        {
            Text      = "病程历史记录（双击编辑）",
            Font      = FontBold,
            ForeColor = Color.FromArgb(20, 100, 50),
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
            Font          = FontMain,
            BorderStyle   = BorderStyle.None,
            BackColor     = Color.FromArgb(244, 252, 246),
        };
        lvProgressHistory.Columns.Add("日期",   160);
        lvProgressHistory.Columns.Add("类型",   160);
        lvProgressHistory.Columns.Add("摘要",   800);
        lvProgressHistory.DoubleClick += LvProgressHistory_DoubleClick;

        // 右键菜单
        var ctxMenu = new ContextMenuStrip { Font = FontMain };
        var ctxDelete = new ToolStripMenuItem("删除选中记录") { ForeColor = ClrBtnRed };
        ctxDelete.Click += BtnDeleteSelectedRecords_Click;
        ctxMenu.Items.Add(ctxDelete);
        lvProgressHistory.ContextMenuStrip = ctxMenu;

        panel.Controls.Add(lvProgressHistory);
        panel.Controls.Add(titleBar);
        return panel;
    }

    // ═══════════════════════════════════════════════════════════
    //  患者管理逻辑
    // ═══════════════════════════════════════════════════════════

    private async void LoadPatients()
    {
        try
        {
            var patients = await _databaseService.GetAllPatientsAsync();
            if (!chkShowDischarged.Checked)
                patients = patients.Where(p => !p.IsDischarged).ToList();

            lstPatients.DataSource    = null;
            lstPatients.DisplayMember = nameof(Patient.Name);
            lstPatients.DataSource    = patients;
            lblPatientCount.Text      = $"({patients.Count})";
        }
        catch (Exception ex)
        {
            MessageBox.Show($"加载患者失败：{ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
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
        SetStatus("已选择患者，可生成或新建病程记录", ClrBtnBlue);
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
                item.ForeColor = Color.FromArgb(180, 40, 40);
            lvProgressHistory.Items.Add(item);
        }
    }

    private void SetStatus(string text, Color? color = null)
    {
        lblStatus.Text      = text;
        lblStatus.ForeColor = color ?? Color.FromArgb(80, 100, 120);
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
        lblPatientInfo.Text = "请从左侧选择或新建患者";
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
        SetStatus("患者档案已更新", ClrBtnGreen);
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
            record.HasDuplicate ? ClrBtnRed : ClrBtnGreen);
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
        SetStatus("病程修改已保存", ClrBtnGreen);
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
        SetStatus($"已删除 {toDelete.Count} 条病程记录", ClrBtnRed);
    }

    // ═══════════════════════════════════════════════════════════
    //  首次病程录生成
    // ═══════════════════════════════════════════════════════════

    private string BuildInitialProgressRecordText(Patient patient)
    {
        var diagnosis    = EmptyAsPending(patient.Diagnosis);
        var chiefComplaint = EmptyAsPending(patient.ChiefComplaint);
        var history      = EmptyAsPending(patient.History);
        var physicalExam = EmptyAsPending(patient.PhysicalExam,
            "待补充生命体征、专科查体及康复功能评定。若无新查体，请补充首程查体。 ");
        var auxiliaryExam = string.IsNullOrWhiteSpace(patient.AuxiliaryExam) ? "暂无。" : patient.AuxiliaryExam.Trim();
        var treatmentPlan = EmptyAsPending(patient.TreatmentPlan,
            "完善相关检查，结合病情予以综合康复治疗，动态评估功能恢复及安全风险。");

        return $"""
{DateTime.Now:yyyy-MM-dd HH:mm}  首次病程记录

一、病例特点：
1. 主诉：{chiefComplaint}

2. 现病史：
{history}

3. 查体及专科情况：
{physicalExam}

4. 辅助检查：
{auxiliaryExam}

二、诊断及鉴别诊断：
1. 初步诊断：
{diagnosis}

2. 诊断依据：
（1）病史：患者因"{chiefComplaint}"入院。
（2）查体：{SummarizeForSentence(physicalExam)}
（3）辅助检查：{SummarizeForSentence(auxiliaryExam)}
（4）结合患者病史、查体及辅助检查，目前诊断如上。

3. 鉴别诊断：
需结合起病形式、神经系统查体、影像学及实验室检查，与相关神经系统疾病、骨关节疾病及其他可导致功能障碍的疾病相鉴别。

三、诊疗计划：
1. 完善血常规、尿常规、肝肾功能、电解质、凝血功能、心电图及影像学等相关检查。
2. 完善康复评定，包括肌力、肌张力、Brunnstrom分期、改良Ashworth评分、平衡、步态、ADL、吞咽、言语及认知等功能评定。
3. 治疗计划：{treatmentPlan}
4. 动态观察病情变化、治疗耐受情况及康复疗效，必要时调整康复治疗强度和医嘱。
""";
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
