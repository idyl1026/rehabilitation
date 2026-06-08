using System.ComponentModel.DataAnnotations;

namespace MedicalProgress.App.Models;

public class DepartmentProfile
{
    [Key]
    public int Id { get; set; }

    [Required]
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    public string DiagnosisKeywords { get; set; } = string.Empty;

    public string ExamKeywords { get; set; } = string.Empty;

    public string ScaleKeywords { get; set; } = string.Empty;

    public string TreatmentKeywords { get; set; } = string.Empty;

    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; } = DateTime.Now;
}
