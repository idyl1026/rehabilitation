using MedicalProgress.App.Models;

namespace MedicalProgress.App.Services;

public class GeneratorService
{
    private readonly TemplateService _templateService;
    private readonly DuplicateCheckService _duplicateCheckService;

    public GeneratorService()
    {
        _templateService = new TemplateService();
        _duplicateCheckService = new DuplicateCheckService();
    }

    public GeneratorService(TemplateService templateService, DuplicateCheckService duplicateCheckService)
    {
        _templateService = templateService;
        _duplicateCheckService = duplicateCheckService;
    }

    public string GenerateInitialProgress(Patient patient)
    {
        return _templateService.GenerateFromTemplate("首次病程", patient, null);
    }

    public string GenerateDailyProgress(Patient patient, ProgressRecord? previousRecord, List<ProgressRecord> allRecords)
    {
        var newContent = _templateService.GenerateFromTemplate("日常病程", patient, previousRecord);

        var historyContents = allRecords.Select(r => r.Content).ToList();

        var hasDuplicates = _duplicateCheckService.HasSignificantDuplicates(newContent, historyContents);

        if (hasDuplicates)
        {
            newContent = _duplicateCheckService.FixDuplicates(newContent, historyContents);
        }

        return newContent;
    }

    public string GenerateDischargeSummary(Patient patient, List<ProgressRecord> allRecords)
    {
        return _templateService.GenerateFromTemplate("出院小结", patient, null);
    }

    public GenerateResult GenerateProgress(Patient patient, ProgressRecord? previousRecord, List<ProgressRecord> allRecords, string templateType = "日常病程")
    {
        var result = new GenerateResult();

        var content = _templateService.GenerateFromTemplate(templateType, patient, previousRecord);

        result.OriginalContent = content;

        var historyContents = allRecords.Select(r => r.Content).ToList();

        var duplicates = _duplicateCheckService.FindDuplicates(content, historyContents);

        result.Duplicates = duplicates;
        result.HasDuplicates = duplicates.Any(d => d.Length >= 20);

        if (result.HasDuplicates)
        {
            result.Content = _duplicateCheckService.FixDuplicates(content, historyContents);
            result.DuplicateReport = _duplicateCheckService.GenerateDuplicateReport(duplicates);
        }
        else
        {
            result.Content = content;
            result.DuplicateReport = "未检测到重复内容 ✓";
        }

        result.Summary = GenerateSummary(result.Content);

        return result;
    }

    public string CheckDuplicates(string content, List<string> historyContents)
    {
        var duplicates = _duplicateCheckService.FindDuplicates(content, historyContents);
        return _duplicateCheckService.GenerateDuplicateReport(duplicates);
    }

    public bool HasDuplicates(string content, List<string> historyContents)
    {
        return _duplicateCheckService.HasSignificantDuplicates(content, historyContents);
    }

    public string FixDuplicatesInContent(string content, List<string> historyContents)
    {
        return _duplicateCheckService.FixDuplicates(content, historyContents);
    }

    public List<string> GetDuplicateTexts(string content, List<string> historyContents)
    {
        return _duplicateCheckService.GetDuplicateTexts(content, historyContents);
    }

    public bool ValidateContent(string content, out string errorMessage)
    {
        errorMessage = string.Empty;

        if (string.IsNullOrWhiteSpace(content))
        {
            errorMessage = "病程内容不能为空";
            return false;
        }

        if (content.Length < 20)
        {
            errorMessage = "病程内容过短，请补充更多内容";
            return false;
        }

        return true;
    }

    public List<string> GetAvailableTemplates()
    {
        return _templateService.GetAvailableTemplates();
    }

    private string GenerateSummary(string content)
    {
        if (string.IsNullOrWhiteSpace(content))
            return string.Empty;

        var lines = content.Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);

        if (lines.Length == 0)
            return string.Empty;

        var summary = lines
            .Where(line => !string.IsNullOrWhiteSpace(line) && line.Length > 10)
            .Take(3)
            .Select(line => line.Trim())
            .Aggregate((current, next) => current + " " + next);

        if (summary.Length > 100)
        {
            summary = summary.Substring(0, 100) + "...";
        }

        return summary;
    }
}

public class GenerateResult
{
    public string Content { get; set; } = string.Empty;
    public string OriginalContent { get; set; } = string.Empty;
    public string Summary { get; set; } = string.Empty;
    public string DuplicateReport { get; set; } = string.Empty;
    public List<DuplicateInfo> Duplicates { get; set; } = new List<DuplicateInfo>();
    public bool HasDuplicates { get; set; } = false;
}
