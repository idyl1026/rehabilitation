using System.ComponentModel.DataAnnotations;

namespace MedicalProgress.App.Models;

public class Patient
{
    [Key]
    public int Id { get; set; }

    [Required]
    [MaxLength(50)]
    public string Name { get; set; } = string.Empty;

    [Required]
    [MaxLength(10)]
    public string Gender { get; set; } = "男";

    [Range(0, 150)]
    public int Age { get; set; }

    [MaxLength(50)]
    public string BedNumber { get; set; } = string.Empty;

    [MaxLength(50)]
    public string MedicalRecordNumber { get; set; } = string.Empty;

    [MaxLength(100)]
    public string Department { get; set; } = string.Empty;

    [MaxLength(200)]
    public string AttendingDoctor { get; set; } = string.Empty;

    [MaxLength(100)]
    public string Diagnosis { get; set; } = string.Empty;

    [Required]
    [MaxLength(500)]
    public string ChiefComplaint { get; set; } = string.Empty;

    [MaxLength(2000)]
    public string History { get; set; } = string.Empty;

    [MaxLength(2000)]
    public string PhysicalExam { get; set; } = string.Empty;

    [MaxLength(2000)]
    public string AuxiliaryExam { get; set; } = string.Empty;

    [MaxLength(2000)]
    public string TreatmentPlan { get; set; } = string.Empty;

    public DateTime AdmissionDate { get; set; } = DateTime.Now;

    public DateTime? DischargeDate { get; set; }

    [MaxLength(500)]
    public string DischargeDiagnosis { get; set; } = string.Empty;

    [MaxLength(2000)]
    public string DischargeOrders { get; set; } = string.Empty;

    public bool IsDischarged { get; set; } = false;

    [MaxLength(500)]
    public string PatientFolder { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; } = DateTime.Now;

    public DateTime UpdatedAt { get; set; } = DateTime.Now;

    public virtual ICollection<ProgressRecord> ProgressRecords { get; set; } = new List<ProgressRecord>();

    public string GetPatientInfo()
    {
        var info = $"姓名：{Name}，性别：{Gender}，年龄：{Age}岁";
        if (!string.IsNullOrEmpty(BedNumber))
            info += $"，床号：{BedNumber}";
        if (!string.IsNullOrEmpty(MedicalRecordNumber))
            info += $"，住院号：{MedicalRecordNumber}";
        info += $"，入院日期：{AdmissionDate:yyyy-MM-dd}";
        return info;
    }

    public string GetBriefSummary()
    {
        return $"{Name}，{Gender}，{Age}岁，{ChiefComplaint}";
    }

    public string GetFullDangAn()
    {
        var sb = new System.Text.StringBuilder();
        sb.AppendLine("【患者档案】");
        sb.AppendLine($"姓名：{Name}");
        sb.AppendLine($"性别：{Gender}");
        sb.AppendLine($"年龄：{Age}岁");
        if (!string.IsNullOrEmpty(BedNumber))
            sb.AppendLine($"床号：{BedNumber}");
        if (!string.IsNullOrEmpty(MedicalRecordNumber))
            sb.AppendLine($"住院号：{MedicalRecordNumber}");
        if (!string.IsNullOrEmpty(Department))
            sb.AppendLine($"科室：{Department}");
        if (!string.IsNullOrEmpty(AttendingDoctor))
            sb.AppendLine($"主治医师：{AttendingDoctor}");
        if (!string.IsNullOrEmpty(Diagnosis))
            sb.AppendLine($"入院诊断：{Diagnosis}");
        sb.AppendLine($"入院日期：{AdmissionDate:yyyy年MM月dd日}");
        sb.AppendLine();
        sb.AppendLine("【主诉】");
        sb.AppendLine(ChiefComplaint);
        if (!string.IsNullOrEmpty(History))
        {
            sb.AppendLine();
            sb.AppendLine("【现病史】");
            sb.AppendLine(History);
        }
        if (!string.IsNullOrEmpty(PhysicalExam))
        {
            sb.AppendLine();
            sb.AppendLine("【体格检查】");
            sb.AppendLine(PhysicalExam);
        }
        if (!string.IsNullOrEmpty(AuxiliaryExam))
        {
            sb.AppendLine();
            sb.AppendLine("【辅助检查】");
            sb.AppendLine(AuxiliaryExam);
        }
        if (!string.IsNullOrEmpty(TreatmentPlan))
        {
            sb.AppendLine();
            sb.AppendLine("【诊疗计划】");
            sb.AppendLine(TreatmentPlan);
        }
        return sb.ToString();
    }

    public string GetChuYuanXiaoJie()
    {
        var sb = new System.Text.StringBuilder();
        var hospitalDays = (DischargeDate ?? DateTime.Now) - AdmissionDate;
        sb.AppendLine("【出院小结】");
        sb.AppendLine($"姓名：{Name}，性别：{Gender}，年龄：{Age}岁");
        if (!string.IsNullOrEmpty(BedNumber))
            sb.AppendLine($"床号：{BedNumber}");
        if (!string.IsNullOrEmpty(MedicalRecordNumber))
            sb.AppendLine($"住院号：{MedicalRecordNumber}");
        sb.AppendLine($"住院天数：{hospitalDays.Days + 1}天");
        sb.AppendLine($"入院日期：{AdmissionDate:yyyy年MM月dd日}");
        sb.AppendLine($"出院日期：{DischargeDate?.ToString("yyyy年MM月dd日") ?? "待定"}");
        sb.AppendLine();
        if (!string.IsNullOrEmpty(Diagnosis))
        {
            sb.AppendLine($"入院诊断：{Diagnosis}");
        }
        if (!string.IsNullOrEmpty(DischargeDiagnosis))
        {
            sb.AppendLine($"出院诊断：{DischargeDiagnosis}");
        }
        sb.AppendLine();
        sb.AppendLine("【入院情况】");
        sb.AppendLine($"患者因\"{ChiefComplaint}\"入院。");
        if (!string.IsNullOrEmpty(History))
            sb.AppendLine(History);
        sb.AppendLine();
        sb.AppendLine("【诊疗经过】");
        if (!string.IsNullOrEmpty(TreatmentPlan))
            sb.AppendLine($"住院期间予以{TreatmentPlan}治疗，");
        sb.AppendLine("患者病情好转出院。");
        sb.AppendLine();
        sb.AppendLine("【出院情况】");
        sb.AppendLine("患者一般情况可，精神食欲正常，无特殊不适主诉。");
        if (!string.IsNullOrEmpty(DischargeOrders))
        {
            sb.AppendLine();
            sb.AppendLine("【出院医嘱】");
            sb.AppendLine(DischargeOrders);
        }
        return sb.ToString();
    }
}
