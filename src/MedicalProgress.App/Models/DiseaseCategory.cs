using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MedicalProgress.App.Models;

public class DiseaseCategory
{
    [Key]
    public int Id { get; set; }

    [Required]
    public int SubjectId { get; set; }

    [Required]
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(50)]
    public string Code { get; set; } = string.Empty;

    [MaxLength(500)]
    public string Description { get; set; } = string.Empty;

    public int ParentId { get; set; } = 0;

    public int SortOrder { get; set; } = 0;

    public DateTime CreatedAt { get; set; } = DateTime.Now;

    [ForeignKey("SubjectId")]
    public virtual Subject? Subject { get; set; }

    public virtual ICollection<KnowledgeTemplate> Templates { get; set; } = new List<KnowledgeTemplate>();
}
