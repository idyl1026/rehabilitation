namespace BingChengAssistant.Models;

public class Patient
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public string Gender { get; set; } = "男";
    public int Age { get; set; }
    public string Phone { get; set; } = "";
    public string AllergyHistory { get; set; } = "";
    public string PastHistory { get; set; } = "";
    public string Remark { get; set; } = "";
    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public bool IsActive { get; set; } = true;
}

public class Admission
{
    public int Id { get; set; }
    public int PatientId { get; set; }
    public int DoctorId { get; set; }
    public string AdmissionNo { get; set; } = "";
    public string BedNo { get; set; } = "";
    public string Department { get; set; } = "";
    public DateTime AdmissionDate { get; set; } = DateTime.Today;
    public DateTime? DischargeDate { get; set; }
    public string MainDiagnosis { get; set; } = "";
    public string SecondaryDiagnosis { get; set; } = "";
    public string Status { get; set; } = "在院";   // 在院/已出院/已归档/病危
    public string DischargeOutcome { get; set; } = "";
    public string DischargeOrders { get; set; } = "";
    public string RehabAdvice { get; set; } = "";
    public string ExercisePrescription { get; set; } = "";
    public string FollowUpAdvice { get; set; } = "";
    public string ResearchNote { get; set; } = "";
    public DateTime CreatedAt { get; set; } = DateTime.Now;

    // 导航属性（不存数据库，运行时填充）
    public Patient? Patient { get; set; }
    public string WordStatus { get; set; } = "未创建";

    /// <summary>住院天数（在院取今日，出院取出院日）</summary>
    public int HospitalDays => (int)((DischargeDate ?? DateTime.Today) - AdmissionDate).TotalDays + 1;
}

public class PatientInsuranceInfo
{
    public int Id { get; set; }
    public int AdmissionId { get; set; }
    public string InsuranceType { get; set; } = "";
    public string InsuranceRegion { get; set; } = "";
}
