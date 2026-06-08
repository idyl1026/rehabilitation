namespace BingChengAssistant.Models;

public class RehabScaleDict
{
    public int Id { get; set; }
    public string Code { get; set; } = "";     // VAS / NRS / ROM / MMT
    public string Name { get; set; } = "";
    public string Description { get; set; } = "";
    public string ScaleType { get; set; } = "";  // numeric / grade / composite
    public bool IsActive { get; set; } = true;
}

public class RehabAssessmentRecord
{
    public int Id { get; set; }
    public int AdmissionId { get; set; }
    public int DoctorId { get; set; }
    public int ScaleId { get; set; }
    public string ScaleName { get; set; } = "";
    public DateTime AssessmentDate { get; set; } = DateTime.Now;
    public string ResultSummary { get; set; } = "";
    public string Interpretation { get; set; } = "";
    public string RehabAdvice { get; set; } = "";
    public string NoteText { get; set; } = "";   // 导入病程用的文本
    public bool IsSyncedToWord { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.Now;
}
