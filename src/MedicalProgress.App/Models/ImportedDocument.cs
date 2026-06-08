using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MedicalProgress.App.Models;

public class ImportedDocument
{
    [Key]
    public int Id { get; set; }

    public int? PatientId { get; set; }

    [MaxLength(260)]
    public string SourceFilePath { get; set; } = string.Empty;

    [MaxLength(120)]
    public string SourceFileName { get; set; } = string.Empty;

    [MaxLength(20)]
    public string SourceType { get; set; } = "Paste";

    [MaxLength(50)]
    public string DocumentType { get; set; } = "Unknown";

    public string RawText { get; set; } = string.Empty;

    public string NormalizedText { get; set; } = string.Empty;

    public bool IsReviewed { get; set; }

    public DateTime ImportedAt { get; set; } = DateTime.Now;

    public DateTime? ReviewedAt { get; set; }

    [ForeignKey("PatientId")]
    public virtual Patient? Patient { get; set; }

    public virtual ICollection<StructuredExamResult> ExamResults { get; set; } = new List<StructuredExamResult>();
}
