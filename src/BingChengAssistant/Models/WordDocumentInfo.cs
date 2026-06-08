namespace BingChengAssistant.Models;

public class WordDocumentInfo
{
    public int Id { get; set; }
    public int AdmissionId { get; set; }
    public string FilePath { get; set; } = "";
    public string FileName { get; set; } = "";
    public string Status { get; set; } = "已创建";  // 已创建/已归档
    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public DateTime? LastSyncedAt { get; set; }
}

public class ResearchCaseIndex
{
    public int Id { get; set; }
    public int AdmissionId { get; set; }
    public int DoctorId { get; set; }
    public string PatientName { get; set; } = "";
    public string AdmissionNo { get; set; } = "";
    public string MainDiagnosis { get; set; } = "";
    public DateTime AdmissionDate { get; set; }
    public DateTime? DischargeDate { get; set; }
    public string DoctorName { get; set; } = "";
    public int NoteCount { get; set; }
    public int RehabCount { get; set; }
    public string WordFilePath { get; set; } = "";
    public string ResearchNote { get; set; } = "";
    public DateTime UpdatedAt { get; set; } = DateTime.Now;
}
