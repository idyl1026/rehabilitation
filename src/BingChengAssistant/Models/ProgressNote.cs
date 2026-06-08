namespace BingChengAssistant.Models;

public class ProgressNote
{
    public int Id { get; set; }
    public int AdmissionId { get; set; }
    public int DoctorId { get; set; }
    public string NoteType { get; set; } = "日常病程";   // 首次病程/日常病程/上级查房/康复评估/出院前
    public string Content { get; set; } = "";
    public DateTime RecordDate { get; set; } = DateTime.Now;
    public bool IsSyncedToWord { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.Now;
}

public class NoteTemplate
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public string NoteType { get; set; } = "";
    public string Content { get; set; } = "";
    public bool IsBuiltIn { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.Now;
}
