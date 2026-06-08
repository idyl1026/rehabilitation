using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MedicalProgress.App.Models;

public class ProgressRecord
{
    [Key]
    public int Id { get; set; }

    [Required]
    public int PatientId { get; set; }

    [Required]
    public DateTime RecordDate { get; set; } = DateTime.Now;

    [Required]
    public string Content { get; set; } = string.Empty;

    [MaxLength(500)]
    public string Summary { get; set; } = string.Empty;

    [MaxLength(50)]
    public string RecordType { get; set; } = "日常病程";

    public bool HasDuplicate { get; set; } = false;

    [MaxLength(500)]
    public string DuplicateInfo { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; } = DateTime.Now;

    [ForeignKey("PatientId")]
    public virtual Patient? Patient { get; set; }

    public string GetFormattedRecord()
    {
        return $"【{RecordDate:yyyy-MM-dd}】{RecordType}\n{Content}";
    }

    public string GetShortSummary()
    {
        if (!string.IsNullOrEmpty(Summary))
            return Summary;

        var maxLength = Math.Min(100, Content.Length);
        return Content.Substring(0, maxLength) + (Content.Length > maxLength ? "..." : "");
    }
}
