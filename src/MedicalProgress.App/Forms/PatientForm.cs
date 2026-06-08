using MedicalProgress.App.Helpers;
using MedicalProgress.App.Models;
using MedicalProgress.App.Services;

namespace MedicalProgress.App.Forms;

public class PatientForm : Form
{
    public Patient? Patient { get; private set; }

    private readonly PatientFolderService _folderService    = new();
    private readonly Patient?             _existingPatient;

    // 基本信息
    private TextBox       txtName                = null!;
    private ComboBox      cmbGender              = null!;
    private NumericUpDown numAge                 = null!;
    private TextBox       txtBedNumber           = null!;
    private TextBox       txtMedicalRecordNumber = null!;
    private TextBox       txtDepartment          = null!;
    private TextBox       txtAttendingDoctor     = null!;
    private TextBox       txtDiagnosis           = null!;
    private DateTimePicker dtpAdmissionDate      = null!;
    private TextBox       txtChiefComplaint      = null!;
    private TextBox       txtHistory             = null!;
    private TextBox       txtPhysicalExam        = null!;
    private TextBox       txtAuxiliaryExam       = null!;
    private TextBox       txtTreatmentPlan       = null!;

    public PatientForm() : this(null) { }

    public PatientForm(Patient? patient)
    {
        _existingPatient = patient;
        InitializeComponent();
        if (patient != null) LoadPatient(patient);
    }

    private void InitializeComponent()
    {
        Text          = _existingPatient == null ? "新建患者档案" : "修改患者档案";
        Size          = new Size(1100, 860);
        StartPosition = FormStartPosition.CenterParent;
        MinimumSize   = new Size(900, 720);
        MedStyleHelper.ApplyWindowStyle(this);

        // Header
        var headerSubtitle = _existingPatient == null ? ""
            : $"{_existingPatient.Name}  |  住院号 {_existingPatient.MedicalRecordNumber}  |  床号 {_existingPatient.BedNumber}";
        var header = MedStyleHelper.CreateHeader(Text, headerSubtitle);
        header.Height = 48;

        // Footer 按钮行
        var footer = BuildFooter();

        // Body（左右双栏）
        var body = BuildBody();

        Controls.Add(body);
        Controls.Add(footer);
        Controls.Add(header);
    }

    // ── Body 左右双栏 ─────────────────────────────────────────

    private Control BuildBody()
    {
        var split = new TableLayoutPanel
        {
            Dock        = DockStyle.Fill,
            ColumnCount = 2,
            RowCount    = 1,
            BackColor   = MedStyleHelper.ContentBg,
            Padding     = new Padding(12)
        };
        split.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 460));
        split.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));

        split.Controls.Add(BuildLeftPanel(), 0, 0);
        split.Controls.Add(BuildRightPanel(), 1, 0);

        return split;
    }

    // ── 左侧：基本信息 ────────────────────────────────────────

    private Control BuildLeftPanel()
    {
        var panel = new Panel
        {
            Dock      = DockStyle.Fill,
            BackColor = MedStyleHelper.ContentBg,
            Padding   = new Padding(0, 0, 6, 0)
        };

        var scroll = new Panel { Dock = DockStyle.Fill, AutoScroll = true };
        var inner  = new TableLayoutPanel
        {
            Dock        = DockStyle.Top,
            AutoSize    = true,
            ColumnCount = 1,
            BackColor   = MedStyleHelper.ContentBg
        };

        // 基本信息卡
        var cardBasic = MedStyleHelper.CreateCard("基本信息");
        cardBasic.Dock    = DockStyle.Top;
        cardBasic.Height  = 140;
        cardBasic.Padding = new Padding(12, 44, 12, 8);

        var basicGrid = new TableLayoutPanel
        {
            Dock        = DockStyle.Fill,
            ColumnCount = 4,
            RowCount    = 2,
            BackColor   = Color.White
        };
        for (int i = 0; i < 4; i++)
            basicGrid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25));
        basicGrid.RowStyles.Add(new RowStyle(SizeType.Absolute, 40));
        basicGrid.RowStyles.Add(new RowStyle(SizeType.Absolute, 40));

        txtName = MakeTextBox(150);
        cmbGender = new ComboBox { Font = MedStyleHelper.FontBody, Dock = DockStyle.Fill, DropDownStyle = ComboBoxStyle.DropDownList };
        cmbGender.Items.AddRange(new[] { "男", "女" });
        cmbGender.SelectedIndex = 0;
        numAge = new NumericUpDown { Font = MedStyleHelper.FontBody, Dock = DockStyle.Fill, Minimum = 0, Maximum = 150, Value = 50 };
        txtBedNumber           = MakeTextBox(80);
        txtMedicalRecordNumber = MakeTextBox(130);

        basicGrid.Controls.Add(WrapField("姓名", txtName),                0, 0);
        basicGrid.Controls.Add(WrapField("性别", cmbGender),              1, 0);
        basicGrid.Controls.Add(WrapField("年龄", numAge),                 2, 0);
        basicGrid.Controls.Add(WrapField("床号", txtBedNumber),           3, 0);
        var mrRow = new Panel { Dock = DockStyle.Fill };
        mrRow.Controls.Add(WrapField("住院号", txtMedicalRecordNumber));
        basicGrid.Controls.Add(mrRow, 0, 1);

        cardBasic.Controls.Add(basicGrid);

        // 住院信息卡
        var cardAdmission = MedStyleHelper.CreateCard("本次住院信息");
        cardAdmission.Dock    = DockStyle.Top;
        cardAdmission.Height  = 100;
        cardAdmission.Padding = new Padding(12, 44, 12, 8);
        cardAdmission.Margin  = new Padding(0, 8, 0, 0);

        var admGrid = new TableLayoutPanel
        {
            Dock        = DockStyle.Fill,
            ColumnCount = 3,
            RowCount    = 1,
            BackColor   = Color.White
        };
        admGrid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 34));
        admGrid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 34));
        admGrid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 32));
        admGrid.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

        txtDepartment     = MakeTextBox(120);
        txtAttendingDoctor = MakeTextBox(160);
        dtpAdmissionDate  = new DateTimePicker
        {
            Format       = DateTimePickerFormat.Custom,
            CustomFormat = "yyyy-MM-dd",
            Font         = MedStyleHelper.FontBody,
            Dock         = DockStyle.Fill
        };

        admGrid.Controls.Add(WrapField("科室", txtDepartment),        0, 0);
        admGrid.Controls.Add(WrapField("主治医师", txtAttendingDoctor), 1, 0);
        admGrid.Controls.Add(WrapField("入院日期", dtpAdmissionDate),   2, 0);
        cardAdmission.Controls.Add(admGrid);

        // 诊断卡
        txtDiagnosis = MakeMultilineTextBox(72);
        var cardDiag = WrapCard("入院诊断", txtDiagnosis, 120);

        // 主诉/现病史/既往史
        txtChiefComplaint = MakeMultilineTextBox(72);
        var cardCC        = WrapCard("主诉", txtChiefComplaint, 120);

        txtHistory    = MakeMultilineTextBox(110);
        var cardHist  = WrapCard("现病史", txtHistory, 158);

        txtPhysicalExam  = MakeMultilineTextBox(110);
        var cardPE       = WrapCard("查体 / 康复评定", txtPhysicalExam, 158);

        txtAuxiliaryExam = MakeMultilineTextBox(90);
        var cardAux      = WrapCard("辅助检查", txtAuxiliaryExam, 138);

        txtTreatmentPlan = MakeMultilineTextBox(90);
        var cardPlan     = WrapCard("诊疗计划", txtTreatmentPlan, 138);

        inner.Controls.Add(cardBasic);
        inner.Controls.Add(Spacer(8));
        inner.Controls.Add(cardAdmission);
        inner.Controls.Add(Spacer(8));
        inner.Controls.Add(cardDiag);
        inner.Controls.Add(Spacer(8));
        inner.Controls.Add(cardCC);
        inner.Controls.Add(Spacer(8));
        inner.Controls.Add(cardHist);
        inner.Controls.Add(Spacer(8));
        inner.Controls.Add(cardPE);
        inner.Controls.Add(Spacer(8));
        inner.Controls.Add(cardAux);
        inner.Controls.Add(Spacer(8));
        inner.Controls.Add(cardPlan);

        scroll.Controls.Add(inner);
        panel.Controls.Add(scroll);
        return panel;
    }

    // ── 右侧：文件夹预览 + 快捷操作 ─────────────────────────

    private Control BuildRightPanel()
    {
        var panel = new Panel
        {
            Dock      = DockStyle.Fill,
            BackColor = MedStyleHelper.ContentBg,
            Padding   = new Padding(6, 0, 0, 0)
        };

        // 患者文件夹卡
        var cardFolder = MedStyleHelper.CreateCard("患者文件夹");
        cardFolder.Dock    = DockStyle.Top;
        cardFolder.Height  = 200;
        cardFolder.Padding = new Padding(12, 44, 12, 8);
        cardFolder.Margin  = new Padding(0, 0, 0, 8);

        var treeView = new TreeView
        {
            Dock        = DockStyle.Fill,
            Font        = MedStyleHelper.FontSmall,
            BorderStyle = BorderStyle.None,
            BackColor   = Color.White
        };

        // 动态显示文件夹结构
        Load += (_, _) =>
        {
            if (_existingPatient != null && !string.IsNullOrWhiteSpace(_existingPatient.PatientFolder)
                && Directory.Exists(_existingPatient.PatientFolder))
            {
                var root = treeView.Nodes.Add(_existingPatient.Name + " 档案");
                AddFolderNodes(root, _existingPatient.PatientFolder, 2);
                treeView.ExpandAll();
            }
            else
            {
                treeView.Nodes.Add("（暂无文件夹，保存后自动创建）");
            }
        };

        var btnOpenFolder = MedStyleHelper.CreateSecondaryBtn("打开文件夹", 120);
        btnOpenFolder.Dock   = DockStyle.Bottom;
        btnOpenFolder.Height = 32;
        btnOpenFolder.Click += (_, _) =>
        {
            var dir = _existingPatient?.PatientFolder;
            if (!string.IsNullOrWhiteSpace(dir) && Directory.Exists(dir))
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(dir) { UseShellExecute = true });
        };

        cardFolder.Controls.Add(treeView);
        cardFolder.Controls.Add(btnOpenFolder);

        // 快捷操作卡
        var cardAction = MedStyleHelper.CreateCard("快捷操作");
        cardAction.Dock    = DockStyle.Top;
        cardAction.Height  = 140;
        cardAction.Padding = new Padding(12, 44, 12, 8);
        cardAction.Margin  = new Padding(0, 8, 0, 0);

        var actionGrid = new TableLayoutPanel
        {
            Dock        = DockStyle.Fill,
            ColumnCount = 2,
            RowCount    = 2,
            BackColor   = Color.White
        };
        actionGrid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
        actionGrid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
        actionGrid.RowStyles.Add(new RowStyle(SizeType.Percent, 50));
        actionGrid.RowStyles.Add(new RowStyle(SizeType.Percent, 50));

        var aBtn1 = MedStyleHelper.CreatePrimaryBtn("预览档案", 120);
        var aBtn2 = MedStyleHelper.CreateSecondaryBtn("复制信息", 120);
        var aBtn3 = MedStyleHelper.CreateSecondaryBtn("打印档案", 120);
        var aBtn4 = MedStyleHelper.CreateSecondaryBtn("导出 Word", 120);
        foreach (var b in new[] { aBtn1, aBtn2, aBtn3, aBtn4 })
            b.Dock = DockStyle.Fill;
        aBtn1.Click += BtnPreview_Click;

        actionGrid.Controls.Add(aBtn1, 0, 0);
        actionGrid.Controls.Add(aBtn2, 1, 0);
        actionGrid.Controls.Add(aBtn3, 0, 1);
        actionGrid.Controls.Add(aBtn4, 1, 1);
        cardAction.Controls.Add(actionGrid);

        panel.Controls.Add(cardAction);
        panel.Controls.Add(Spacer(8));
        panel.Controls.Add(cardFolder);

        return panel;
    }

    private static void AddFolderNodes(TreeNode parent, string path, int depth)
    {
        if (depth <= 0) return;
        try
        {
            foreach (var dir in Directory.GetDirectories(path))
                AddFolderNodes(parent.Nodes.Add(Path.GetFileName(dir)), dir, depth - 1);
            foreach (var file in Directory.GetFiles(path))
                parent.Nodes.Add(Path.GetFileName(file));
        }
        catch { }
    }

    // ── Footer ────────────────────────────────────────────────

    private Control BuildFooter()
    {
        var footer = new Panel
        {
            Dock      = DockStyle.Bottom,
            Height    = 52,
            BackColor = Color.White,
            Padding   = new Padding(12, 8, 12, 8)
        };
        footer.Controls.Add(new Panel { Dock = DockStyle.Top, Height = 1, BackColor = MedStyleHelper.BorderColor });

        var flow = new FlowLayoutPanel
        {
            Dock          = DockStyle.Fill,
            FlowDirection = FlowDirection.RightToLeft,
            WrapContents  = false,
            BackColor     = Color.Transparent
        };

        var btnSave = MedStyleHelper.CreatePrimaryBtn(
            _existingPatient == null ? "保存信息" : "保存修改", 130);
        var btnNewRecord = MedStyleHelper.CreateSuccessBtn("新建病程记录", 140);
        var btnCancel    = MedStyleHelper.CreateSecondaryBtn("取消", 80);

        btnSave.Click      += BtnSave_Click;
        btnNewRecord.Click += (_, _) =>
        {
            // 先保存再开新建病程
            BtnSave_Click(null, EventArgs.Empty);
        };
        btnCancel.Click    += (_, _) => DialogResult = DialogResult.Cancel;

        flow.Controls.AddRange(new Control[] { btnSave, btnNewRecord, btnCancel });
        footer.Controls.Add(flow);
        return footer;
    }

    // ── 帮助方法 ──────────────────────────────────────────────

    private static Panel WrapField(string label, Control control)
    {
        var p = new Panel { Dock = DockStyle.Fill, BackColor = Color.White, Padding = new Padding(4, 4, 4, 0) };
        var lbl = new Label
        {
            Text      = label,
            Font      = MedStyleHelper.FontSmall,
            ForeColor = MedStyleHelper.TextGray,
            Dock      = DockStyle.Top,
            Height    = 16
        };
        control.Dock = DockStyle.Fill;
        p.Controls.Add(control);
        p.Controls.Add(lbl);
        return p;
    }

    private static Panel WrapCard(string title, Control content, int cardHeight)
    {
        var card = MedStyleHelper.CreateCard(title);
        card.Dock    = DockStyle.Top;
        card.Height  = cardHeight;
        card.Padding = new Padding(12, 44, 12, 8);
        content.Dock = DockStyle.Fill;
        card.Controls.Add(content);
        return card;
    }

    private static Panel Spacer(int height)
        => new() { Dock = DockStyle.Top, Height = height, BackColor = MedStyleHelper.ContentBg };

    private static TextBox MakeTextBox(int width) => new()
    {
        Width       = width,
        Font        = MedStyleHelper.FontBody,
        BorderStyle = BorderStyle.FixedSingle,
        Margin      = new Padding(0)
    };

    private static TextBox MakeMultilineTextBox(int height) => new()
    {
        Multiline   = true,
        ScrollBars  = ScrollBars.Vertical,
        Height      = height,
        Dock        = DockStyle.Fill,
        Font        = MedStyleHelper.FontBody,
        BorderStyle = BorderStyle.FixedSingle,
        BackColor   = Color.White
    };

    // ── 数据填充 ──────────────────────────────────────────────

    private void LoadPatient(Patient patient)
    {
        txtName.Text                = patient.Name;
        cmbGender.SelectedItem      = string.IsNullOrWhiteSpace(patient.Gender) ? "男" : patient.Gender;
        numAge.Value                = Math.Clamp(patient.Age, 0, 150);
        txtBedNumber.Text           = patient.BedNumber;
        txtMedicalRecordNumber.Text = patient.MedicalRecordNumber;
        txtDepartment.Text          = patient.Department;
        txtAttendingDoctor.Text     = patient.AttendingDoctor;
        txtDiagnosis.Text           = patient.Diagnosis;
        dtpAdmissionDate.Value      = patient.AdmissionDate == default ? DateTime.Now : patient.AdmissionDate;
        txtChiefComplaint.Text      = patient.ChiefComplaint;
        txtHistory.Text             = patient.History;
        txtPhysicalExam.Text        = patient.PhysicalExam;
        txtAuxiliaryExam.Text       = patient.AuxiliaryExam;
        txtTreatmentPlan.Text       = patient.TreatmentPlan;
    }

    // ── 事件处理 ──────────────────────────────────────────────

    private void BtnSave_Click(object? sender, EventArgs e)
    {
        if (string.IsNullOrWhiteSpace(txtName.Text))
        {
            MessageBox.Show("请输入患者姓名。", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            txtName.Focus();
            return;
        }

        if (string.IsNullOrWhiteSpace(txtChiefComplaint.Text))
        {
            MessageBox.Show("请输入患者主诉。", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            txtChiefComplaint.Focus();
            return;
        }

        Patient = _existingPatient ?? new Patient();
        Patient.Name                = txtName.Text.Trim();
        Patient.Gender              = cmbGender.SelectedItem?.ToString() ?? "男";
        Patient.Age                 = (int)numAge.Value;
        Patient.BedNumber           = txtBedNumber.Text.Trim();
        Patient.MedicalRecordNumber = txtMedicalRecordNumber.Text.Trim();
        Patient.Department          = txtDepartment.Text.Trim();
        Patient.AttendingDoctor     = txtAttendingDoctor.Text.Trim();
        Patient.Diagnosis           = txtDiagnosis.Text.Trim();
        Patient.AdmissionDate       = dtpAdmissionDate.Value;
        Patient.ChiefComplaint      = txtChiefComplaint.Text.Trim();
        Patient.History             = txtHistory.Text.Trim();
        Patient.PhysicalExam        = txtPhysicalExam.Text.Trim();
        Patient.AuxiliaryExam       = txtAuxiliaryExam.Text.Trim();
        Patient.TreatmentPlan       = txtTreatmentPlan.Text.Trim();
        Patient.UpdatedAt           = DateTime.Now;

        try
        {
            Patient.PatientFolder = string.IsNullOrWhiteSpace(Patient.PatientFolder)
                ? _folderService.CreatePatientFolder(Patient)
                : Patient.PatientFolder;
        }
        catch (Exception ex)
        {
            MessageBox.Show($"创建患者目录失败：{ex.Message}，档案仍会保存。",
                "警告", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }

        DialogResult = DialogResult.OK;
    }

    private void BtnPreview_Click(object? sender, EventArgs e)
    {
        var preview = new Patient
        {
            Name                = txtName.Text.Trim(),
            Gender              = cmbGender.SelectedItem?.ToString() ?? "男",
            Age                 = (int)numAge.Value,
            BedNumber           = txtBedNumber.Text.Trim(),
            MedicalRecordNumber = txtMedicalRecordNumber.Text.Trim(),
            Department          = txtDepartment.Text.Trim(),
            AttendingDoctor     = txtAttendingDoctor.Text.Trim(),
            Diagnosis           = txtDiagnosis.Text.Trim(),
            AdmissionDate       = dtpAdmissionDate.Value,
            ChiefComplaint      = txtChiefComplaint.Text.Trim(),
            History             = txtHistory.Text.Trim(),
            PhysicalExam        = txtPhysicalExam.Text.Trim(),
            AuxiliaryExam       = txtAuxiliaryExam.Text.Trim(),
            TreatmentPlan       = txtTreatmentPlan.Text.Trim()
        };

        using var previewForm = new Form
        {
            Text          = "患者档案预览",
            Size          = new Size(820, 700),
            StartPosition = FormStartPosition.CenterParent
        };
        previewForm.Controls.Add(new TextBox
        {
            Dock        = DockStyle.Fill,
            Multiline   = true,
            ScrollBars  = ScrollBars.Both,
            ReadOnly    = true,
            Font        = MedStyleHelper.FontBody,
            Text        = preview.GetFullDangAn()
        });
        previewForm.ShowDialog(this);
    }
}
