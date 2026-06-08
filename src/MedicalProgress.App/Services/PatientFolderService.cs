using System.IO;
using System.Text;
using MedicalProgress.App.Models;

namespace MedicalProgress.App.Services;

public class PatientFolderService
{
    private readonly string _baseOutputPath;

    public PatientFolderService()
    {
        _baseOutputPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
            "MedicalProgress",
            "Patients");

        if (!Directory.Exists(_baseOutputPath))
        {
            Directory.CreateDirectory(_baseOutputPath);
        }
    }

    public string CreatePatientFolder(Patient patient)
    {
        var folderName = GenerateFolderName(patient);
        var folderPath = Path.Combine(_baseOutputPath, folderName);

        if (!Directory.Exists(folderPath))
        {
            Directory.CreateDirectory(folderPath);
        }

        return folderPath;
    }

    public async Task<string> SaveProgressRecordAsync(Patient patient, ProgressRecord record)
    {
        var folderPath = GetOrCreatePatientFolder(patient);
        var fileName = $"{record.RecordDate:yyyyMMdd}_{record.RecordType}.txt";
        var filePath = Path.Combine(folderPath, fileName);

        var content = new StringBuilder();
        content.AppendLine("=" + new string('=', 60));
        content.AppendLine($"【{record.RecordType}】");
        content.AppendLine($"日期：{record.RecordDate:yyyy年MM月dd日 HH:mm}");
        content.AppendLine($"患者：{patient.Name}，{patient.Gender}，{patient.Age}岁");
        if (!string.IsNullOrEmpty(patient.BedNumber))
            content.AppendLine($"床号：{patient.BedNumber}");
        if (!string.IsNullOrEmpty(patient.MedicalRecordNumber))
            content.AppendLine($"住院号：{patient.MedicalRecordNumber}");
        content.AppendLine("=" + new string('=', 60));
        content.AppendLine();
        content.AppendLine(record.Content);

        await File.WriteAllTextAsync(filePath, content.ToString(), Encoding.UTF8);

        return filePath;
    }

    public async Task<string> SaveDangAnAsync(Patient patient)
    {
        var folderPath = GetOrCreatePatientFolder(patient);
        var filePath = Path.Combine(folderPath, "01_患者档案.txt");

        var content = patient.GetFullDangAn();
        await File.WriteAllTextAsync(filePath, content, Encoding.UTF8);

        return filePath;
    }

    public async Task<string> GenerateChuYuanWordAsync(Patient patient, List<ProgressRecord> records)
    {
        var folderPath = GetOrCreatePatientFolder(patient);
        var fileName = $"出院记录_{patient.Name}_{DateTime.Now:yyyyMMdd}.docx";
        var filePath = Path.Combine(folderPath, fileName);

        var content = await GenerateChuYuanContentAsync(patient, records);

        await File.WriteAllTextAsync(filePath.Replace(".docx", ".txt"), content, Encoding.UTF8);

        return filePath;
    }

    private async Task<string> GenerateChuYuanContentAsync(Patient patient, List<ProgressRecord> records)
    {
        var sb = new StringBuilder();

        sb.AppendLine("╔══════════════════════════════════════════════════════════════════╗");
        sb.AppendLine("║                        出 院 记 录                                ║");
        sb.AppendLine("╚══════════════════════════════════════════════════════════════════╝");
        sb.AppendLine();

        sb.AppendLine("【基本信息】");
        sb.AppendLine($"姓名：{patient.Name}");
        sb.AppendLine($"性别：{patient.Gender}");
        sb.AppendLine($"年龄：{patient.Age}岁");
        if (!string.IsNullOrEmpty(patient.BedNumber))
            sb.AppendLine($"床号：{patient.BedNumber}");
        if (!string.IsNullOrEmpty(patient.MedicalRecordNumber))
            sb.AppendLine($"住院号：{patient.MedicalRecordNumber}");
        if (!string.IsNullOrEmpty(patient.Department))
            sb.AppendLine($"科室：{patient.Department}");
        if (!string.IsNullOrEmpty(patient.AttendingDoctor))
            sb.AppendLine($"主治医师：{patient.AttendingDoctor}");

        var hospitalDays = (patient.DischargeDate ?? DateTime.Now) - patient.AdmissionDate;
        sb.AppendLine($"入院日期：{patient.AdmissionDate:yyyy年MM月dd日}");
        sb.AppendLine($"出院日期：{patient.DischargeDate?.ToString("yyyy年MM月dd日") ?? "待定"}");
        sb.AppendLine($"住院天数：{hospitalDays.Days + 1}天");
        sb.AppendLine();

        if (!string.IsNullOrEmpty(patient.Diagnosis))
        {
            sb.AppendLine("【入院诊断】");
            sb.AppendLine(patient.Diagnosis);
            sb.AppendLine();
        }

        if (!string.IsNullOrEmpty(patient.DischargeDiagnosis))
        {
            sb.AppendLine("【出院诊断】");
            sb.AppendLine(patient.DischargeDiagnosis);
            sb.AppendLine();
        }

        sb.AppendLine("【主诉】");
        sb.AppendLine(patient.ChiefComplaint);
        sb.AppendLine();

        if (!string.IsNullOrEmpty(patient.History))
        {
            sb.AppendLine("【现病史】");
            sb.AppendLine(patient.History);
            sb.AppendLine();
        }

        if (!string.IsNullOrEmpty(patient.PhysicalExam))
        {
            sb.AppendLine("【体格检查】");
            sb.AppendLine(patient.PhysicalExam);
            sb.AppendLine();
        }

        if (!string.IsNullOrEmpty(patient.AuxiliaryExam))
        {
            sb.AppendLine("【辅助检查】");
            sb.AppendLine(patient.AuxiliaryExam);
            sb.AppendLine();
        }

        sb.AppendLine("【诊疗经过】");
        if (!string.IsNullOrEmpty(patient.TreatmentPlan))
            sb.AppendLine($"住院期间予以{patient.TreatmentPlan}治疗。");

        if (records.Count > 0)
        {
            sb.AppendLine();
            sb.AppendLine("【病程记录摘要】");
            sb.AppendLine($"共记录 {records.Count} 次病程：");
            sb.AppendLine();

            var groupedRecords = records.GroupBy(r => r.RecordType);
            foreach (var group in groupedRecords)
            {
                sb.AppendLine($"◇ {group.Key}：{group.Count()} 次");
            }

            sb.AppendLine();
            sb.AppendLine("--- 病程详情 ---");
            foreach (var record in records.OrderBy(r => r.RecordDate))
            {
                sb.AppendLine();
                sb.AppendLine($"【{record.RecordDate:MM月dd日}】{record.RecordType}");
                sb.AppendLine(record.Content);
                sb.AppendLine(new string('-', 60));
            }
        }

        sb.AppendLine();
        sb.AppendLine("【出院情况】");
        sb.AppendLine("患者一般情况可，精神食欲正常，大小便正常，无特殊不适主诉。");
        sb.AppendLine("查体：生命体征平稳，心肺腹查体未见明显异常。");
        sb.AppendLine();

        if (!string.IsNullOrEmpty(patient.DischargeOrders))
        {
            sb.AppendLine("【出院医嘱】");
            sb.AppendLine(patient.DischargeOrders);
        }
        else
        {
            sb.AppendLine("【出院医嘱】");
            sb.AppendLine("1. 注意休息，合理饮食");
            sb.AppendLine("2. 按时服药，定期复查");
            sb.AppendLine("3. 不适随诊");
            sb.AppendLine($"4. 出院后{hospitalDays.Days + 1}天门诊复诊");
        }

        sb.AppendLine();
        sb.AppendLine(new string('=', 60));
        sb.AppendLine($"出院日期：{patient.DischargeDate?.ToString("yyyy年MM月dd日") ?? DateTime.Now.ToString("yyyy年MM月dd日")}");
        if (!string.IsNullOrEmpty(patient.AttendingDoctor))
            sb.AppendLine($"主治医师：{patient.AttendingDoctor}");

        return await Task.FromResult(sb.ToString());
    }

    private string GetOrCreatePatientFolder(Patient patient)
    {
        if (!string.IsNullOrEmpty(patient.PatientFolder) && Directory.Exists(patient.PatientFolder))
        {
            return patient.PatientFolder;
        }

        var folderPath = CreatePatientFolder(patient);
        patient.PatientFolder = folderPath;
        return folderPath;
    }

    private string GenerateFolderName(Patient patient)
    {
        var dateStr = patient.AdmissionDate.ToString("yyyyMMdd");
        var name = patient.Name;
        var diagnosis = string.Empty;

        if (!string.IsNullOrEmpty(patient.Diagnosis))
        {
            diagnosis = patient.Diagnosis.Length > 10
                ? patient.Diagnosis.Substring(0, 10)
                : patient.Diagnosis;
            diagnosis = RemoveInvalidChars(diagnosis);
        }

        var folderName = $"{dateStr}_{name}";
        if (!string.IsNullOrEmpty(diagnosis))
            folderName += $"_{diagnosis}";

        return RemoveInvalidChars(folderName);
    }

    private string RemoveInvalidChars(string fileName)
    {
        var invalidChars = Path.GetInvalidFileNameChars();
        foreach (var c in invalidChars)
        {
            fileName = fileName.Replace(c.ToString(), "");
        }
        return fileName;
    }

    public string GetPatientFolder(Patient patient)
    {
        if (!string.IsNullOrEmpty(patient.PatientFolder) && Directory.Exists(patient.PatientFolder))
        {
            return patient.PatientFolder;
        }

        return CreatePatientFolder(patient);
    }

    public List<string> GetPatientFolders()
    {
        if (!Directory.Exists(_baseOutputPath))
            return new List<string>();

        return Directory.GetDirectories(_baseOutputPath).ToList();
    }

    public bool FolderExists(Patient patient)
    {
        if (string.IsNullOrEmpty(patient.PatientFolder))
            return false;

        return Directory.Exists(patient.PatientFolder);
    }

    public string BaseOutputPath => _baseOutputPath;
}
