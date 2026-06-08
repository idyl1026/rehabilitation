using System.Text;
using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using MedicalProgress.App.Data;
using MedicalProgress.App.Models;

namespace MedicalProgress.App.Services;

public class KnowledgeImportService
{
    private readonly string _baseDataPath;
    private readonly TextExtractionService _textExtractionService = new();

    public KnowledgeImportService()
    {
        _baseDataPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "KnowledgeBase");
    }

    public async Task<ImportResult> ImportFromFolderAsync(string subjectName, string folderPath, IProgress<ImportProgress>? progress = null)
    {
        var result = new ImportResult();

        try
        {
            if (!Directory.Exists(folderPath))
            {
                result.ErrorMessage = $"文件夹不存在：{folderPath}";
                return result;
            }

            using var context = new AppDbContext();

            var subject = await context.Subjects.FirstOrDefaultAsync(s => s.Name == subjectName);
            if (subject == null)
            {
                subject = new Subject
                {
                    Name = subjectName,
                    FolderPath = folderPath,
                    Description = $"从 {folderPath} 导入的资料",
                    CreatedAt = DateTime.Now,
                    LastUpdatedAt = DateTime.Now
                };
                context.Subjects.Add(subject);
                await context.SaveChangesAsync();
            }

            var files = Directory.GetFiles(folderPath, "*.*", SearchOption.AllDirectories)
                .Where(f => f.EndsWith(".txt", StringComparison.OrdinalIgnoreCase) ||
                           f.EndsWith(".docx", StringComparison.OrdinalIgnoreCase) ||
                           f.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase))
                .ToList();

            result.TotalFiles = files.Count;

            for (int i = 0; i < files.Count; i++)
            {
                var file = files[i];
                try
                {
                    var template = await ProcessFileAsync(context, subject.Id, file);
                    if (template != null)
                    {
                        result.SuccessCount++;
                        result.ImportedFiles.Add(file);
                    }
                    else
                    {
                        result.FailedCount++;
                        result.FailedFiles.Add(file);
                    }

                    progress?.Report(new ImportProgress
                    {
                        CurrentFile = file,
                        ProcessedCount = i + 1,
                        TotalCount = files.Count,
                        Percentage = (int)((i + 1) * 100.0 / files.Count)
                    });
                }
                catch (Exception ex)
                {
                    result.FailedCount++;
                    result.FailedFiles.Add($"{file}: {ex.Message}");
                }
            }

            subject.LastUpdatedAt = DateTime.Now;
            subject.DocumentCount = await context.KnowledgeTemplates.CountAsync(t => t.SubjectId == subject.Id);
            await context.SaveChangesAsync();

            result.Subject = subject;
            result.Success = result.SuccessCount > 0;
        }
        catch (Exception ex)
        {
            result.ErrorMessage = ex.Message;
        }

        return result;
    }

    private async Task<KnowledgeTemplate?> ProcessFileAsync(AppDbContext context, int subjectId, string filePath)
    {
        string content;
        try
        {
            content = await _textExtractionService.ExtractTextAsync(filePath);
        }
        catch
        {
            return null;
        }

        if (string.IsNullOrWhiteSpace(content) || content.Length < 50)
            return null;

        var fileName = Path.GetFileNameWithoutExtension(filePath);
        var categoryName = GetCategoryFromPath(filePath);
        var category = await GetOrCreateCategoryAsync(context, subjectId, categoryName);

        var existingTemplate = await context.KnowledgeTemplates
            .FirstOrDefaultAsync(t => t.SubjectId == subjectId && t.SourceFile == filePath);

        if (existingTemplate != null)
        {
            var fileInfo = new FileInfo(filePath);
            if (existingTemplate.SourceFileDate == fileInfo.LastWriteTime)
            {
                return existingTemplate;
            }

            existingTemplate.Content = content;
            existingTemplate.Summary = GenerateSummary(content);
            existingTemplate.Keywords = ExtractKeywords(content);
            existingTemplate.SourceFileDate = fileInfo.LastWriteTime;
            existingTemplate.CategoryId = category.Id;

            await context.SaveChangesAsync();
            return existingTemplate;
        }

        var template = new KnowledgeTemplate
        {
            SubjectId = subjectId,
            CategoryId = category.Id,
            Title = fileName,
            TemplateType = DetermineTemplateType(fileName, content),
            Content = content,
            Summary = GenerateSummary(content),
            Keywords = ExtractKeywords(content),
            SourceFile = filePath,
            SourceFileDate = new FileInfo(filePath).LastWriteTime,
            CreatedAt = DateTime.Now,
            IsActive = true
        };

        context.KnowledgeTemplates.Add(template);
        await context.SaveChangesAsync();

        return template;
    }

    private string GetCategoryFromPath(string filePath)
    {
        var directory = Path.GetDirectoryName(filePath);
        if (string.IsNullOrEmpty(directory))
            return "未分类";

        var folderName = new DirectoryInfo(directory).Name;

        var categoryKeywords = new Dictionary<string, string[]>
        {
            { "神经系统", new[] { "脑", "神经", "脊髓", "瘫", "卒中", "梗死", "出血" } },
            { "骨关节", new[] { "骨", "关节", "椎", "膝", "肩", "颈", "腰", "骨折" } },
            { "鉴别诊断", new[] { "鉴别", "诊断" } },
            { "治疗方案", new[] { "治疗", "药物", "康复", "手术" } },
            { "病程记录", new[] { "病程", "记录" } }
        };

        foreach (var category in categoryKeywords)
        {
            if (category.Value.Any(keyword => folderName.Contains(keyword) || filePath.Contains(keyword)))
            {
                return category.Key;
            }
        }

        return folderName;
    }

    private async Task<DiseaseCategory> GetOrCreateCategoryAsync(AppDbContext context, int subjectId, string categoryName)
    {
        var category = await context.DiseaseCategories
            .FirstOrDefaultAsync(c => c.SubjectId == subjectId && c.Name == categoryName);

        if (category == null)
        {
            category = new DiseaseCategory
            {
                SubjectId = subjectId,
                Name = categoryName,
                Code = GenerateCategoryCode(categoryName),
                SortOrder = await context.DiseaseCategories.CountAsync(c => c.SubjectId == subjectId)
            };
            context.DiseaseCategories.Add(category);
            await context.SaveChangesAsync();
        }

        return category;
    }

    private string DetermineTemplateType(string fileName, string content)
    {
        var name = fileName.ToLower();

        if (name.Contains("首次") || content.Contains("首次病程"))
            return "首次病程";
        if (name.Contains("出院") || content.Contains("出院小结"))
            return "出院小结";
        if (name.Contains("会诊") || content.Contains("会诊记录"))
            return "会诊记录";
        if (name.Contains("鉴别") || content.Contains("鉴别诊断"))
            return "鉴别诊断";
        if (name.Contains("手术") || content.Contains("手术记录"))
            return "手术记录";
        if (name.Contains("抢救") || content.Contains("抢救记录"))
            return "抢救记录";

        return "日常病程";
    }

    private string GenerateSummary(string content)
    {
        if (string.IsNullOrWhiteSpace(content))
            return string.Empty;

        var lines = content.Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries)
            .Where(line => line.Trim().Length > 20)
            .Take(3)
            .Select(line => line.Trim())
            .ToList();

        var summary = string.Join(" ", lines);

        if (summary.Length > 200)
            summary = summary.Substring(0, 200) + "...";

        return summary;
    }

    private string ExtractKeywords(string content)
    {
        if (string.IsNullOrWhiteSpace(content))
            return string.Empty;

        var medicalTerms = new HashSet<string>
        {
            "脑梗死", "脑出血", "脑卒中", "颈椎病", "腰椎间盘突出", "膝关节",
            "脊髓损伤", "面神经麻痹", "肩周炎", "治疗", "检查", "诊断",
            "症状", "体征", "康复", "药物", "手术", "病程"
        };

        var found = new List<string>();
        foreach (var term in medicalTerms)
        {
            if (content.Contains(term) && !found.Contains(term))
            {
                found.Add(term);
            }
        }

        return string.Join(",", found);
    }

    private string GenerateCategoryCode(string categoryName)
    {
        var pinyin = new Dictionary<string, string>
        {
            { "神经系统", "SJNXT" },
            { "骨关节", "GGJ" },
            { "鉴别诊断", "JBZD" },
            { "治疗方案", "ZLFA" },
            { "病程记录", "BCJL" },
            { "未分类", "WFL" }
        };

        return pinyin.GetValueOrDefault(categoryName, "QT");
    }

    public List<string> GetAvailableSubjects()
    {
        using var context = new AppDbContext();
        return context.Subjects
            .Where(s => s.IsActive)
            .OrderByDescending(s => s.LastUpdatedAt)
            .Select(s => s.Name)
            .ToList();
    }
}

public class ImportProgress
{
    public string CurrentFile { get; set; } = string.Empty;
    public int ProcessedCount { get; set; }
    public int TotalCount { get; set; }
    public int Percentage { get; set; }
}

public class ImportResult
{
    public bool Success { get; set; } = false;
    public Subject? Subject { get; set; }
    public int TotalFiles { get; set; } = 0;
    public int SuccessCount { get; set; } = 0;
    public int FailedCount { get; set; } = 0;
    public List<string> ImportedFiles { get; set; } = new List<string>();
    public List<string> FailedFiles { get; set; } = new List<string>();
    public string ErrorMessage { get; set; } = string.Empty;
}
