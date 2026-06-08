using System.ComponentModel.DataAnnotations;

namespace MedicalProgress.App.Models;

public class Subject
{
    [Key]
    public int Id { get; set; }

    [Required]
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(500)]
    public string Description { get; set; } = string.Empty;

    [MaxLength(500)]
    public string FolderPath { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; } = DateTime.Now;

    public DateTime LastUpdatedAt { get; set; } = DateTime.Now;

    public bool IsActive { get; set; } = true;

    public int DocumentCount { get; set; } = 0;

    public virtual ICollection<DiseaseCategory> Categories { get; set; } = new List<DiseaseCategory>();

    public virtual ICollection<KnowledgeTemplate> Templates { get; set; } = new List<KnowledgeTemplate>();
}
