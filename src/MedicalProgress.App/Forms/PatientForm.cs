using MedicalProgress.App.Models;
using MedicalProgress.App.Services;

namespace MedicalProgress.App.Forms;

public class PatientForm : Form
{
    public Patient? Patient { get; private set; }

    private readonly PatientFolderService _folderService = new();
    private readonly Patient? _existingPatient;

    private TextBox txtName = null!;
    private ComboBox cmbGender = null!;
    private NumericUpDown numAge = null!;
    private TextBox txtBedNumber = null!;
    private TextBox txtMedicalRecordNumber = null!;
    private TextBox txtDepartment = null!;
    private TextBox txtAttendingDoctor = null!;
    private TextBox txtDiagnosis = null!;
    private DateTimePicker dtpAdmissionDate = null!;
    private TextBox txtChiefComplaint = null!;
    private TextBox txtHistory = null!;
    private TextBox txtPhysicalExam = null!;
    private TextBox txtAuxiliaryExam = null!;
    private TextBox txtTreatmentPlan = null!;

    public PatientForm() : this(null)
    {
    }

    public PatientForm(Patient? patient)
    {
        _existingPatient = patient;
        InitializeComponent();
        if (patient != null)
            LoadPatient(patient);
    }

    private void InitializeComponent()
    {
        Text = _existingPatient == null ? "新建患者档案" : "修改患者档案";
        Size = new Size(980, 920);
        StartPosition = FormStartPosition.CenterParent;
        MinimumSize = new Size(880, 760);
        BackColor = Color.FromArgb(245, 247, 250);

        var header = new Label
        {
            Dock = DockStyle.Top,
            Height = 56,
            Text = Text,
            TextAlign = ContentAlignment.MiddleCenter,
            Font = new Font("Microsoft YaHei UI", 16, FontStyle.Bold),
            BackColor = Color.FromArgb(0, 123, 215),
            ForeColor = Color.White
        };

        var scroll = new Panel { Dock = DockStyle.Fill, AutoScroll = true, Padding = new Padding(18) };
        var content = new TableLayoutPanel
        {
            Dock = DockStyle.Top,
            AutoSize = true,
            ColumnCount = 1,
            RowCount = 16,
            BackColor = Color.White,
            Padding = new Padding(18)
        };

        content.Controls.Add(CreateSectionLabel("基本信息"));
        content.Controls.Add(BuildBasicRow());
        content.Controls.Add(CreateSectionLabel("科室与医生"));
        content.Controls.Add(BuildDepartmentRow());
        content.Controls.Add(CreateSectionLabel("入院诊断"));
        txtDiagnosis = CreateMultilineTextBox(88);
        content.Controls.Add(txtDiagnosis);
        content.Controls.Add(CreateSectionLabel("主诉"));
        txtChiefComplaint = CreateMultilineTextBox(72);
        content.Controls.Add(txtChiefComplaint);
        content.Controls.Add(CreateSectionLabel("现病史"));
        txtHistory = CreateMultilineTextBox(110);
        content.Controls.Add(txtHistory);
        content.Controls.Add(CreateSectionLabel("查体 / 康复评定"));
        txtPhysicalExam = CreateMultilineTextBox(130);
        content.Controls.Add(txtPhysicalExam);
        content.Controls.Add(CreateSectionLabel("辅助检查"));
        txtAuxiliaryExam = CreateMultilineTextBox(110);
        content.Controls.Add(txtAuxiliaryExam);
        content.Controls.Add(CreateSectionLabel("诊疗计划"));
        txtTreatmentPlan = CreateMultilineTextBox(110);
        content.Controls.Add(txtTreatmentPlan);
        content.Controls.Add(BuildButtonRow());

        scroll.Controls.Add(content);
        Controls.Add(scroll);
        Controls.Add(header);
    }

    private Control BuildBasicRow()
    {
        var row = CreateInputRow();
        txtName = CreateTextBox(120);
        cmbGender = new ComboBox
        {
            Width = 80,
            DropDownStyle = ComboBoxStyle.DropDownList,
            Font = new Font("Microsoft YaHei UI", 10),
            Margin = new Padding(5)
        };
        cmbGender.Items.AddRange(new[] { "男", "女" });
        cmbGender.SelectedIndex = 0;
        numAge = new NumericUpDown { Width = 80, Minimum = 0, Maximum = 150, Value = 50, Font = new Font("Microsoft YaHei UI", 10), Margin = new Padding(5) };
        txtBedNumber = CreateTextBox(90);
        txtMedicalRecordNumber = CreateTextBox(140);

        row.Controls.AddRange(new Control[]
        {
            CreateLabel("姓名", 48), txtName,
            CreateLabel("性别", 48), cmbGender,
            CreateLabel("年龄", 48), numAge,
            CreateLabel("床号", 48), txtBedNumber,
            CreateLabel("住院号", 60), txtMedicalRecordNumber
        });
        return row;
    }

    private Control BuildDepartmentRow()
    {
        var row = CreateInputRow();
        txtDepartment = CreateTextBox(180);
        txtAttendingDoctor = CreateTextBox(360);
        dtpAdmissionDate = new DateTimePicker
        {
            Width = 155,
            Format = DateTimePickerFormat.Custom,
            CustomFormat = "yyyy-MM-dd",
            Font = new Font("Microsoft YaHei UI", 10),
            Margin = new Padding(5)
        };

        row.Controls.AddRange(new Control[]
        {
            CreateLabel("科室", 48), txtDepartment,
            CreateLabel("医生（可填多位）", 118), txtAttendingDoctor,
            CreateLabel("入院日期", 78), dtpAdmissionDate
        });
        return row;
    }

    private Control BuildButtonRow()
    {
        var row = new FlowLayoutPanel
        {
            FlowDirection = FlowDirection.RightToLeft,
            Dock = DockStyle.Top,
            Height = 58,
            Padding = new Padding(5, 12, 5, 5)
        };

        var btnSave = CreateButton(_existingPatient == null ? "保存档案" : "保存修改", Color.FromArgb(40, 167, 69), 130);
        btnSave.Click += BtnSave_Click;
        var btnPreview = CreateButton("生成档案预览", Color.FromArgb(0, 123, 215), 150);
        btnPreview.Click += BtnPreview_Click;
        var btnCancel = CreateButton("取消", Color.FromArgb(108, 117, 125), 100);
        btnCancel.Click += (_, _) => DialogResult = DialogResult.Cancel;

        row.Controls.AddRange(new Control[] { btnSave, btnPreview, btnCancel });
        return row;
    }

    private static FlowLayoutPanel CreateInputRow() => new()
    {
        FlowDirection = FlowDirection.LeftToRight,
        Height = 46,
        AutoSize = true,
        Dock = DockStyle.Top,
        Padding = new Padding(0, 4, 0, 4)
    };

    private static Label CreateSectionLabel(string text) => new()
    {
        Text = text,
        Dock = DockStyle.Top,
        Height = 34,
        Font = new Font("Microsoft YaHei UI", 11, FontStyle.Bold),
        ForeColor = Color.FromArgb(45, 45, 45),
        TextAlign = ContentAlignment.BottomLeft,
        Margin = new Padding(0, 12, 0, 0)
    };

    private static Label CreateLabel(string text, int width) => new()
    {
        Text = text,
        Width = width,
        TextAlign = ContentAlignment.MiddleRight,
        Font = new Font("Microsoft YaHei UI", 10),
        Margin = new Padding(5)
    };

    private static TextBox CreateTextBox(int width) => new()
    {
        Width = width,
        Height = 28,
        Font = new Font("Microsoft YaHei UI", 10),
        BorderStyle = BorderStyle.FixedSingle,
        Margin = new Padding(5)
    };

    private static TextBox CreateMultilineTextBox(int height) => new()
    {
        Multiline = true,
        ScrollBars = ScrollBars.Vertical,
        Height = height,
        Dock = DockStyle.Top,
        Font = new Font("Microsoft YaHei UI", 10),
        BorderStyle = BorderStyle.FixedSingle,
        BackColor = Color.White,
        Margin = new Padding(5)
    };

    private static Button CreateButton(string text, Color color, int width) => new()
    {
        Text = text,
        Width = width,
        Height = 36,
        Font = new Font("Microsoft YaHei UI", 10),
        BackColor = color,
        ForeColor = Color.White,
        FlatStyle = FlatStyle.Flat,
        Margin = new Padding(5)
    };

    private void LoadPatient(Patient patient)
    {
        txtName.Text = patient.Name;
        cmbGender.SelectedItem = string.IsNullOrWhiteSpace(patient.Gender) ? "男" : patient.Gender;
        numAge.Value = Math.Clamp(patient.Age, 0, 150);
        txtBedNumber.Text = patient.BedNumber;
        txtMedicalRecordNumber.Text = patient.MedicalRecordNumber;
        txtDepartment.Text = patient.Department;
        txtAttendingDoctor.Text = patient.AttendingDoctor;
        txtDiagnosis.Text = patient.Diagnosis;
        dtpAdmissionDate.Value = patient.AdmissionDate == default ? DateTime.Now : patient.AdmissionDate;
        txtChiefComplaint.Text = patient.ChiefComplaint;
        txtHistory.Text = patient.History;
        txtPhysicalExam.Text = patient.PhysicalExam;
        txtAuxiliaryExam.Text = patient.AuxiliaryExam;
        txtTreatmentPlan.Text = patient.TreatmentPlan;
    }

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
        Patient.Name = txtName.Text.Trim();
        Patient.Gender = cmbGender.SelectedItem?.ToString() ?? "男";
        Patient.Age = (int)numAge.Value;
        Patient.BedNumber = txtBedNumber.Text.Trim();
        Patient.MedicalRecordNumber = txtMedicalRecordNumber.Text.Trim();
        Patient.Department = txtDepartment.Text.Trim();
        Patient.AttendingDoctor = txtAttendingDoctor.Text.Trim();
        Patient.Diagnosis = txtDiagnosis.Text.Trim();
        Patient.AdmissionDate = dtpAdmissionDate.Value;
        Patient.ChiefComplaint = txtChiefComplaint.Text.Trim();
        Patient.History = txtHistory.Text.Trim();
        Patient.PhysicalExam = txtPhysicalExam.Text.Trim();
        Patient.AuxiliaryExam = txtAuxiliaryExam.Text.Trim();
        Patient.TreatmentPlan = txtTreatmentPlan.Text.Trim();
        Patient.UpdatedAt = DateTime.Now;

        try
        {
            Patient.PatientFolder = string.IsNullOrWhiteSpace(Patient.PatientFolder)
                ? _folderService.CreatePatientFolder(Patient)
                : Patient.PatientFolder;
        }
        catch (Exception ex)
        {
            MessageBox.Show($"创建患者目录失败：{ex.Message}，档案仍会保存。", "警告", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }

        DialogResult = DialogResult.OK;
    }

    private void BtnPreview_Click(object? sender, EventArgs e)
    {
        var preview = new Patient
        {
            Name = txtName.Text.Trim(),
            Gender = cmbGender.SelectedItem?.ToString() ?? "男",
            Age = (int)numAge.Value,
            BedNumber = txtBedNumber.Text.Trim(),
            MedicalRecordNumber = txtMedicalRecordNumber.Text.Trim(),
            Department = txtDepartment.Text.Trim(),
            AttendingDoctor = txtAttendingDoctor.Text.Trim(),
            Diagnosis = txtDiagnosis.Text.Trim(),
            AdmissionDate = dtpAdmissionDate.Value,
            ChiefComplaint = txtChiefComplaint.Text.Trim(),
            History = txtHistory.Text.Trim(),
            PhysicalExam = txtPhysicalExam.Text.Trim(),
            AuxiliaryExam = txtAuxiliaryExam.Text.Trim(),
            TreatmentPlan = txtTreatmentPlan.Text.Trim()
        };

        using var previewForm = new Form
        {
            Text = "患者档案预览",
            Size = new Size(820, 700),
            StartPosition = FormStartPosition.CenterParent
        };
        var textBox = new TextBox
        {
            Dock = DockStyle.Fill,
            Multiline = true,
            ScrollBars = ScrollBars.Both,
            ReadOnly = true,
            Font = new Font("Microsoft YaHei UI", 10),
            Text = preview.GetFullDangAn()
        };
        previewForm.Controls.Add(textBox);
        previewForm.ShowDialog(this);
    }
}
