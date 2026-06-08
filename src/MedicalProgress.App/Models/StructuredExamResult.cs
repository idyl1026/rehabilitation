using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MedicalProgress.App.Models;

public class StructuredExamResult
{
    [Key]
    public int Id { get; set; }

    public int? PatientId { get; set; }

    public int ImportedDocumentId { get; set; }

    public DateTime? ExamDate { get; set; }

    [MaxLength(50)]
    public string ExamType { get; set; } = "Unknown";

    [MaxLength(120)]
    public string ReportName { get; set; } = string.Empty;

    [MaxLength(120)]
    public string ItemName { get; set; } = string.Empty;

    [MaxLength(80)]
    public string ResultValue { get; set; } = string.Empty;

    [MaxLength(50)]
    public string Unit { get; set; } = string.Empty;

    [MaxLength(80)]
    public string ReferenceRange { get; set; } = string.Empty;

    [MaxLength(20)]
    public string AbnormalFlag { get; set; } = string.Empty;

    public string Conclusion { get; set; } = string.Empty;

    public string RawLine { get; set; } = string.Empty;

    public bool IsReviewed { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.Now;

    [ForeignKey("PatientId")]
    public virtual Patient? Patient { get; set; }

    [ForeignKey("ImportedDocumentId")]
    public virtual ImportedDocument? ImportedDocument { get; set; }
}
