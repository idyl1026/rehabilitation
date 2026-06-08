using System.Diagnostics;
using System.Windows.Forms;
using MedicalProgress.App.Helpers;
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
        Text          = "办理出院 - 出院记录归档";
        Size          = new Size(1100, 820);
        StartPosition = FormStartPosition.CenterParent;
        MaximizeBox   = false;
        MinimizeBox   = false;
        MedStyleHelper.ApplyWindowStyle(this);

        var header = MedStyleHelper.CreateHeader("办理出院  |  出院记录归档");

        // 患者信息条（浅蓝背景）
        var infoBar = new Panel
        {
            Dock      = DockStyle.Top,
            Height    = 52,
            BackColor = MedStyleHelper.LightBlue,
            Padding   = new Padding(14, 0, 14, 0)
        };
        lblPatientInfo = new Label
        {
            Dock      = DockStyle.Left,
            Width     = 700,
            Font      = MedStyleHelper.FontBold,
            ForeColor = MedStyleHelper.PrimaryBlue,
            TextAlign = ContentAlignment.MiddleLeft
        };
        lblHospitalDays = new Label
        {
            Dock      = DockStyle.Right,
            Width     = 300,
            Font      = MedStyleHelper.FontSmall,
            ForeColor = MedStyleHelper.TextGray,
            TextAlign = ContentAlignment.MiddleRight
        };
        infoBar.Controls.Add(lblPatientInfo);
        infoBar.Controls.Add(lblHospitalDays);
        infoBar.Controls.Add(new Panel { Dock = DockStyle.Bottom, Height = 1, BackColor = MedStyleHelper.BorderColor });

        // 底部按钮行
        var footer = new Panel
        {
            Dock      = DockStyle.Bottom,
            Height    = 52,
            BackColor = Color.White,
            Padding   = new Padding(12, 8, 12, 8)
        };
        footer.Controls.Add(new Panel { Dock = DockStyle.Top, Height = 1, BackColor = MedStyleHelper.BorderColor });
        var footFlow = new FlowLayoutPanel
        {
            Dock          = DockStyle.Fill,
            FlowDirection = FlowDirection.RightToLeft,
            WrapContents  = false,
            BackColor     = Color.Transparent
        };
        btnDischarge = MedStyleHelper.CreateSuccessBtn("完成归档", 130);
        btnDischarge.Click += BtnDischarge_Click;
        btnPreview   = MedStyleHelper.CreateSecondaryBtn("出院记录预览", 130);
        btnPreview.Click += BtnPreview_Click;
        btnCancel    = MedStyleHelper.CreateSecondaryBtn("取消", 80);
        btnCancel.Click += (s, e) => { DialogResult = DialogResult.Cancel; Close(); };
        footFlow.Controls.AddRange(new Control[] { btnDischarge, btnPreview, btnCancel });
        footer.Controls.Add(footFlow);

        // 三栏主体
        var body = new TableLayoutPanel
        {
            Dock        = DockStyle.Fill,
            ColumnCount = 3,
            RowCount    = 1,
            BackColor   = MedStyleHelper.ContentBg,
            Padding     = new Padding(10)
        };
        body.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33));
        body.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 34));
        body.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33));

        body.Controls.Add(BuildDischargeInfoPanel(), 0, 0);
        body.Controls.Add(BuildTimelinePanel(), 1, 0);
        body.Controls.Add(BuildArchivePanel(), 2, 0);

        Controls.Add(body);
        Controls.Add(footer);
        Controls.Add(infoBar);
        Controls.Add(header);
    }

    private Panel BuildDischargeInfoPanel()
    {
        var panel = new Panel { Dock = DockStyle.Fill, BackColor = MedStyleHelper.ContentBg, Padding = new Padding(0, 0, 4, 0) };
        var card  = MedStyleHelper.CreateCard("出院信息");
        card.Dock    = DockStyle.Fill;
        card.Padding = new Padding(12, 44, 12, 12);

        var inner = new TableLayoutPanel
        {
            Dock        = DockStyle.Fill,
            ColumnCount = 1,
            BackColor   = Color.White
        };

        // 出院日期
        inner.Controls.Add(MakeSectionLbl("出院日期"));
        dtpDischargeDate = new DateTimePicker
        {
            Format       = DateTimePickerFormat.Short,
            Font         = MedStyleHelper.FontBody,
            Dock         = DockStyle.Top,
            Height       = 30
        };
        inner.Controls.Add(dtpDischargeDate);
        inner.Controls.Add(MakeSectionLbl("出院诊断"));
        txtDischargeDiagnosis = new TextBox
        {
            Multiline = true, ScrollBars = ScrollBars.Vertical,
            Dock = DockStyle.Top, Height = 80,
            Font = MedStyleHelper.FontBody, BorderStyle = BorderStyle.FixedSingle, BackColor = Color.White
        };
        inner.Controls.Add(txtDischargeDiagnosis);

        inner.Controls.Add(MakeSectionLbl("出院医嘱"));
        txtDischargeOrders = new TextBox
        {
            Multiline = true, ScrollBars = ScrollBars.Vertical,
            Dock = DockStyle.Top, Height = 120,
            Font = MedStyleHelper.FontBody, BorderStyle = BorderStyle.FixedSingle, BackColor = Color.White
        };
        inner.Controls.Add(txtDischargeOrders);

        // 选项
        chkExportWord = new CheckBox
        {
            Text    = "导出 Word 文档",
            Checked = true, Dock = DockStyle.Top, Height = 26,
            Font    = MedStyleHelper.FontSmall, ForeColor = MedStyleHelper.TextDark
        };
        chkSaveToFolder = new CheckBox
        {
            Text    = "保存到患者目录",
            Checked = true, Dock = DockStyle.Top, Height = 26,
            Font    = MedStyleHelper.FontSmall, ForeColor = MedStyleHelper.TextDark
        };
        inner.Controls.Add(chkExportWord);
        inner.Controls.Add(chkSaveToFolder);

        card.Controls.Add(inner);
        panel.Controls.Add(card);
        return panel;
    }

    private Panel BuildTimelinePanel()
    {
        var panel = new Panel { Dock = DockStyle.Fill, BackColor = MedStyleHelper.ContentBg, Padding = new Padding(4, 0, 4, 0) };
        var card  = MedStyleHelper.CreateCard("住院经过汇总");
        card.Dock    = DockStyle.Fill;
        card.Padding = new Padding(12, 44, 12, 12);

        var timeline = new RichTextBox
        {
            Dock       = DockStyle.Fill,
            ReadOnly   = true,
            Font       = MedStyleHelper.FontSmall,
            BackColor  = Color.White,
            BorderStyle= BorderStyle.None,
            DetectUrls = false
        };

        // 填入时间轴内容
        Load += (_, _) =>
        {
            var sb = new System.Text.StringBuilder();
            foreach (var r in _records.OrderBy(r => r.RecordDate))
            {
                sb.AppendLine($"[{r.RecordDate:MM-dd HH:mm}]  {r.RecordType}");
                var summary = r.GetShortSummary().Replace("\r", "").Replace("\n", " ");
                if (summary.Length > 80) summary = summary[..80] + "...";
                sb.AppendLine("   " + summary);
                sb.AppendLine();
            }
            timeline.Text = sb.ToString();
        };

        card.Controls.Add(timeline);
        panel.Controls.Add(card);
        return panel;
    }

    private Panel BuildArchivePanel()
    {
        var panel = new Panel { Dock = DockStyle.Fill, BackColor = MedStyleHelper.ContentBg, Padding = new Padding(4, 0, 0, 0) };
        var card  = MedStyleHelper.CreateCard("归档文件预览");
        card.Dock    = DockStyle.Fill;
        card.Padding = new Padding(12, 44, 12, 12);

        var list = new CheckedListBox
        {
            Dock         = DockStyle.Top,
            Height       = 160,
            CheckOnClick = true,
            Font         = MedStyleHelper.FontSmall,
            BorderStyle  = BorderStyle.None,
            BackColor    = Color.White
        };
        list.Items.Add("患者档案（danganwenjian.txt）", true);
        list.Items.Add("出院小结（Word）", true);
        list.Items.Add("病程记录合并", true);
        list.Items.Add("检查资料（如有）", true);

        var preview = new RichTextBox
        {
            Dock       = DockStyle.Fill,
            ReadOnly   = true,
            Font       = MedStyleHelper.FontSmall,
            BackColor  = Color.White,
            BorderStyle= BorderStyle.None,
            DetectUrls = false
        };
        Load += (_, _) =>
        {
            if (Patient != null)
                preview.Text = Patient.GetChuYuanXiaoJie();
        };

        card.Controls.Add(preview);
        card.Controls.Add(new Panel { Dock = DockStyle.Top, Height = 1, BackColor = MedStyleHelper.BorderColor });
        card.Controls.Add(list);
        panel.Controls.Add(card);
        return panel;
    }

    private static Label MakeSectionLbl(string text) => new()
    {
        Text      = text,
        Font      = MedStyleHelper.FontBold,
        ForeColor = MedStyleHelper.TextDark,
        Dock      = DockStyle.Top,
        Height    = 26,
        Padding   = new Padding(0, 6, 0, 0)
    };

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
