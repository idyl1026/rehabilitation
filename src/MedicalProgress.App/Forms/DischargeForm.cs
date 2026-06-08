using System.Diagnostics;
using System.Windows.Forms;
using MedicalProgress.App.Models;
using MedicalProgress.App.Services;

namespace MedicalProgress.App.Forms;

public class DischargeForm : Form
{
    public Patient? Patient { get; private set; }
    private readonly List<ProgressRecord> _records;
    private readonly WordExportService _wordExportService;
    private readonly PatientFolderService _folderService;

    private DateTimePicker dtpDischargeDate = null!;
    private TextBox txtDischargeDiagnosis = null!;
    private TextBox txtDischargeOrders = null!;
    private CheckBox chkExportWord = null!;
    private CheckBox chkSaveToFolder = null!;
    private Label lblPatientInfo = null!;
    private Label lblHospitalDays = null!;
    private Button btnCancel = null!;
    private Button btnDischarge = null!;
    private Button btnPreview = null!;

    public DischargeForm(Patient patient, List<ProgressRecord> records)
    {
        Patient = patient;
        _records = records;
        _wordExportService = new WordExportService();
        _folderService = new PatientFolderService();

        InitializeComponent();
        LoadPatientInfo();
    }

    private void InitializeComponent()
    {
        this.Text = "办理出院 - 出院记录归档";
        this.Size = new System.Drawing.Size(750, 720);
        this.StartPosition = FormStartPosition.CenterScreen;
        this.FormBorderStyle = FormBorderStyle.FixedDialog;
        this.MaximizeBox = false;
        this.MinimizeBox = false;
        this.CancelButton = btnCancel;
        this.StartPosition = FormStartPosition.CenterParent;
        this.BackColor = Color.FromArgb(240, 244, 248);

        var pnlHeader = new Panel
        {
            Dock = DockStyle.Top,
            Height = 70,
            BackColor = Color.FromArgb(40, 167, 69)
        };

        var lblTitle = new Label
        {
            Text = "🏥 办理出院 - 出院记录归档",
            Font = new Font("微软雅黑", 16, FontStyle.Bold),
            ForeColor = Color.White,
            TextAlign = ContentAlignment.MiddleCenter,
            Dock = DockStyle.Fill
        };
        pnlHeader.Controls.Add(lblTitle);

        var pnlMain = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(25),
            RowCount = 8,
            ColumnCount = 1
        };
        pnlMain.RowStyles.Add(new RowStyle(SizeType.Absolute, 50));
        pnlMain.RowStyles.Add(new RowStyle(SizeType.Absolute, 45));
        pnlMain.RowStyles.Add(new RowStyle(SizeType.Absolute, 50));
        pnlMain.RowStyles.Add(new RowStyle(SizeType.Percent, 18));
        pnlMain.RowStyles.Add(new RowStyle(SizeType.Percent, 22));
        pnlMain.RowStyles.Add(new RowStyle(SizeType.Percent, 18));
        pnlMain.RowStyles.Add(new RowStyle(SizeType.Absolute, 45));
        pnlMain.RowStyles.Add(new RowStyle(SizeType.Absolute, 55));

        var lblPatientInfoHeader = new Label
        {
            Text = "👤 患者信息",
            Font = new Font("微软雅黑", 11, FontStyle.Bold),
            Dock = DockStyle.Fill,
            ForeColor = Color.FromArgb(50, 50, 50),
            TextAlign = ContentAlignment.BottomLeft
        };

        lblPatientInfo = new Label
        {
            Text = "",
            Font = new Font("微软雅黑", 10),
            TextAlign = ContentAlignment.MiddleLeft,
            Dock = DockStyle.Fill,
            ForeColor = Color.FromArgb(0, 123, 215),
            Padding = new Padding(5, 5, 0, 0)
        };

        lblHospitalDays = new Label
        {
            Text = "",
            Font = new Font("微软雅黑", 9),
            TextAlign = ContentAlignment.MiddleLeft,
            Dock = DockStyle.Fill,
            ForeColor = Color.FromArgb(100, 100, 100),
            Padding = new Padding(5, 2, 0, 0)
        };

        var lblDischargeDate = CreateSectionLabel("📅 出院日期");
        
        var pnlDate = new FlowLayoutPanel
        {
            FlowDirection = FlowDirection.LeftToRight,
            Dock = DockStyle.Fill,
            Height = 50,
            Padding = new Padding(5, 5, 5, 5)
        };

        dtpDischargeDate = new DateTimePicker
        {
            Width = 160,
            Format = DateTimePickerFormat.Short,
            Font = new Font("微软雅黑", 10),
            CalendarForeColor = Color.FromArgb(50, 50, 50)
        };

        pnlDate.Controls.Add(dtpDischargeDate);

        var lblDischargeDiagnosis = CreateSectionLabel("🔍 出院诊断");

        txtDischargeDiagnosis = CreateModernTextBox();

        var lblDischargeOrders = CreateSectionLabel("💊 出院医嘱");

        txtDischargeOrders = CreateModernTextBox();

        var pnlOptions = new FlowLayoutPanel
        {
            FlowDirection = FlowDirection.LeftToRight,
            Dock = DockStyle.Fill,
            Height = 45,
            Padding = new Padding(5, 5, 5, 5)
        };

        chkExportWord = new CheckBox
        {
            Text = "导出为Word文档",
            AutoSize = true,
            Checked = true,
            Margin = new Padding(10, 5, 30, 5),
            Font = new Font("微软雅黑", 10),
            ForeColor = Color.FromArgb(50, 50, 50)
        };

        chkSaveToFolder = new CheckBox
        {
            Text = "保存到患者目录",
            AutoSize = true,
            Checked = true,
            Margin = new Padding(10, 5, 10, 5),
            Font = new Font("微软雅黑", 10),
            ForeColor = Color.FromArgb(50, 50, 50)
        };

        pnlOptions.Controls.Add(chkExportWord);
        pnlOptions.Controls.Add(chkSaveToFolder);

        var pnlButtons = new FlowLayoutPanel
        {
            FlowDirection = FlowDirection.RightToLeft,
            Dock = DockStyle.Fill,
            Height = 55,
            Padding = new Padding(5, 5, 5, 5)
        };

        btnPreview = CreateModernButton("📋 出院记录", Color.FromArgb(255, 193, 7), 42, Color.Black);
        btnPreview.Click += BtnPreview_Click;
        btnPreview.Width = 140;

        btnDischarge = CreateModernButton("🏥 确认出院", Color.FromArgb(40, 167, 69), 42);
        btnDischarge.Click += BtnDischarge_Click;
        btnDischarge.Width = 140;

        btnCancel = CreateModernButton("❌ 取消", Color.FromArgb(108, 117, 125), 42);
        btnCancel.Click += (s, e) => { this.DialogResult = DialogResult.Cancel; this.Close(); };
        btnCancel.Width = 100;

        pnlButtons.Controls.Add(btnPreview);
        pnlButtons.Controls.Add(btnDischarge);
        pnlButtons.Controls.Add(btnCancel);

        pnlMain.Controls.Add(lblPatientInfoHeader);
        pnlMain.Controls.Add(lblPatientInfo);
        pnlMain.Controls.Add(lblHospitalDays);
        pnlMain.Controls.Add(lblDischargeDate);
        pnlMain.Controls.Add(pnlDate);
        pnlMain.Controls.Add(lblDischargeDiagnosis);
        pnlMain.Controls.Add(txtDischargeDiagnosis);
        pnlMain.Controls.Add(lblDischargeOrders);
        pnlMain.Controls.Add(txtDischargeOrders);
        pnlMain.Controls.Add(pnlOptions);
        pnlMain.Controls.Add(pnlButtons);

        this.Controls.Add(pnlMain);
        this.Controls.Add(pnlHeader);
    }

    private Label CreateSectionLabel(string text)
    {
        return new Label
        {
            Text = text,
            Font = new Font("微软雅黑", 11, FontStyle.Bold),
            Dock = DockStyle.Fill,
            ForeColor = Color.FromArgb(50, 50, 50),
            TextAlign = ContentAlignment.BottomLeft,
            Padding = new Padding(5, 5, 0, 5)
        };
    }

    private TextBox CreateModernTextBox()
    {
        return new TextBox
        {
            Multiline = true,
            ScrollBars = ScrollBars.Vertical,
            Dock = DockStyle.Fill,
            Font = new Font("微软雅黑", 10),
            BorderStyle = BorderStyle.FixedSingle,
            BackColor = Color.White
        };
    }

    private Button CreateModernButton(string text, Color backColor, int height, Color? foreColor = null)
    {
        var btn = new Button
        {
            Text = text,
            Height = height,
            Font = new Font("微软雅黑", 10),
            BackColor = backColor,
            ForeColor = foreColor ?? Color.White,
            FlatStyle = FlatStyle.Flat,
            Margin = new Padding(5)
        };
        btn.FlatAppearance.BorderSize = 0;
        btn.FlatAppearance.MouseOverBackColor = ControlPaint.Dark(backColor, 0.1f);
        return btn;
    }

    private void LoadPatientInfo()
    {
        if (Patient == null)
            return;

        var hospitalDays = (DateTime.Now - Patient.AdmissionDate).Days + 1;

        lblPatientInfo.Text = $"患者：{Patient.Name}，{Patient.Gender}，{Patient.Age}岁" +
                             (string.IsNullOrEmpty(Patient.BedNumber) ? "" : $"，床号：{Patient.BedNumber}") +
                             (string.IsNullOrEmpty(Patient.MedicalRecordNumber) ? "" : $"，住院号：{Patient.MedicalRecordNumber}");

        lblHospitalDays.Text = $"📅 住院天数：{hospitalDays}天  |  📋 病程记录：{_records.Count}次";

        dtpDischargeDate.Value = DateTime.Now;

        if (!string.IsNullOrEmpty(Patient.Diagnosis))
        {
            txtDischargeDiagnosis.Text = Patient.Diagnosis;
        }

        txtDischargeOrders.Text = @"1. 注意休息，合理饮食
2. 按时服药，定期复查
3. 不适随诊
4. 出院后门诊复诊";
    }

    private void BtnPreview_Click(object? sender, EventArgs e)
    {
        if (Patient == null) return;

        // 先把当前表单的值同步到 Patient，以便出院小结能读到
        Patient.DischargeDate      = dtpDischargeDate.Value;
        Patient.DischargeDiagnosis = txtDischargeDiagnosis.Text.Trim();
        Patient.DischargeOrders    = txtDischargeOrders.Text.Trim();

        using var form = new DischargeSummaryForm(Patient, _records);
        form.ShowDialog(this);
    }

    private async void BtnDischarge_Click(object? sender, EventArgs e)
    {
        if (Patient == null)
        {
            MessageBox.Show("患者信息无效。", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            return;
        }

        try
        {
            Patient.DischargeDate = dtpDischargeDate.Value;
            Patient.DischargeDiagnosis = txtDischargeDiagnosis.Text.Trim();
            Patient.DischargeOrders = txtDischargeOrders.Text.Trim();
            Patient.IsDischarged = true;

            btnDischarge.Enabled = false;
            btnDischarge.Text = "处理中...";

            var messages = new List<string>();

            if (chkSaveToFolder.Checked)
            {
                await SaveToPatientFolder(messages);
            }

            if (chkExportWord.Checked)
            {
                await Export出院Word(messages);
            }

            this.DialogResult = DialogResult.OK;
            this.Close();

            var resultMessage = "出院办理完成！\n\n" + string.Join("\n", messages);

            if (chkExportWord.Checked && messages.Any(m => m.Contains("Word文档")))
            {
                var wordFile = messages.FirstOrDefault(m => m.Contains("Word文档"));
                if (!string.IsNullOrEmpty(wordFile))
                {
                    var filePath = wordFile.Split('：').LastOrDefault();
                    if (!string.IsNullOrEmpty(filePath) && System.IO.File.Exists(filePath))
                    {
                        Process.Start("explorer.exe", $"/select,\"{filePath}\"");
                    }
                }
            }

            MessageBox.Show(resultMessage, "出院办理成功", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"出院办理失败：{ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            btnDischarge.Enabled = true;
            btnDischarge.Text = "确认出院";
        }
    }

    private async Task SaveToPatientFolder(List<string> messages)
    {
        try
        {
            var folderPath = _folderService.GetPatientFolder(Patient);

            if (!System.IO.Directory.Exists(folderPath))
            {
                folderPath = _folderService.CreatePatientFolder(Patient);
                Patient.PatientFolder = folderPath;
            }

            await _folderService.SaveDangAnAsync(Patient);

            foreach (var record in _records)
            {
                await _folderService.SaveProgressRecordAsync(Patient, record);
            }

            var chuYuanFilePath = await _folderService.GenerateChuYuanWordAsync(Patient, _records);

            messages.Add($"✓ 已保存到目录：{folderPath}");
        }
        catch (Exception ex)
        {
            messages.Add($"⚠ 目录保存失败：{ex.Message}");
        }
    }

    private async Task Export出院Word(List<string> messages)
    {
        try
        {
            var folderPath = _folderService.GetPatientFolder(Patient);
            if (!System.IO.Directory.Exists(folderPath))
            {
                folderPath = _folderService.CreatePatientFolder(Patient);
            }

            var filePath = await _wordExportService.ExportChuYuanJiLuAsync(Patient, _records, folderPath);
            messages.Add($"✓ 已导出Word文档：{System.IO.Path.GetFileName(filePath)}");
        }
        catch (Exception ex)
        {
            messages.Add($"⚠ Word导出失败：{ex.Message}");
        }
    }
}
