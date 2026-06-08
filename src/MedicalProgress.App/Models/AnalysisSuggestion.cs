using System.ComponentModel.DataAnnotations;

namespace MedicalProgress.App.Models;

public class AnalysisSuggestion
{
    [MaxLength(50)]
    public string Category { get; set; } = string.Empty;

    [MaxLength(80)]
    public string Tag { get; set; } = string.Empty;

    public string Evidence { get; set; } = string.Empty;

    public string AnalysisText { get; set; } = string.Empty;

    public string Recommendation { get; set; } = string.Empty;

    [MaxLength(20)]
    public string RiskLevel { get; set; } = "Info";
}
