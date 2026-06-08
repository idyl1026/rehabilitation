using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MedicalProgress.App.Models;

public class KnowledgeTemplate
{
    [Key]
    public int Id { get; set; }

    [Required]
    public int SubjectId { get; set; }

    public int CategoryId { get; set; } = 0;

    [Required]
    [MaxLength(100)]
    public string Title { get; set; } = string.Empty;

    [Required]
    [MaxLength(50)]
    public string TemplateType { get; set; } = string.Empty;

    [Required]
    public string Content { get; set; } = string.Empty;

    [MaxLength(2000)]
    public string Summary { get; set; } = string.Empty;

    [MaxLength(200)]
    public string Keywords { get; set; } = string.Empty;

    [MaxLength(100)]
    public string SourceFile { get; set; } = string.Empty;

    public DateTime SourceFileDate { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.Now;

    public DateTime LastUsedAt { get; set; }

    public int UseCount { get; set; } = 0;

    public bool IsFavorite { get; set; } = false;

    public bool IsActive { get; set; } = true;

    [ForeignKey("SubjectId")]
    public virtual Subject? Subject { get; set; }

    [ForeignKey("CategoryId")]
    public virtual DiseaseCategory? Category { get; set; }

    public string GetPreview()
    {
        if (string.IsNullOrEmpty(Content))
            return string.Empty;

        var maxLength = Math.Min(150, Content.Length);
        return Content.Substring(0, maxLength) + (Content.Length > maxLength ? "..." : "");
    }
}
