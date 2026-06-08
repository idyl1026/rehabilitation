using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using MedicalProgress.App.Data;
using MedicalProgress.App.Models;

namespace MedicalProgress.App.Services;

public class LiteratureSearchService
{
    private readonly HttpClient _httpClient = new();

    public async Task<int> SearchPubMedAndSaveAsync(int subjectId, string keyword, int maxResults = 8)
    {
        if (string.IsNullOrWhiteSpace(keyword))
            return 0;

        var ids = await SearchPubMedIdsAsync(keyword, maxResults);
        if (ids.Count == 0)
            return 0;

        var articles = await FetchSummariesAsync(ids);
        if (articles.Count == 0)
            return 0;

        using var context = new AppDbContext();
        var category = await GetOrCreateCategoryAsync(context, subjectId, "联网文献");

        var saved = 0;
        foreach (var article in articles)
        {
            var sourceKey = $"PubMed:{article.Pmid}";
            var existing = await context.KnowledgeTemplates
                .FirstOrDefaultAsync(t => t.SubjectId == subjectId && t.SourceFile == sourceKey);
            if (existing != null)
                continue;

            var content = BuildContent(article);
            context.KnowledgeTemplates.Add(new KnowledgeTemplate
            {
                SubjectId = subjectId,
                CategoryId = category.Id,
                Title = article.Title.Length > 100 ? article.Title[..100] : article.Title,
                TemplateType = "文献摘要",
                Content = content,
                Summary = content.Length > 500 ? content[..500] + "..." : content,
                Keywords = keyword.Length > 200 ? keyword[..200] : keyword,
                SourceFile = sourceKey,
                SourceFileDate = DateTime.Now,
                CreatedAt = DateTime.Now,
                LastUsedAt = DateTime.Now,
                IsActive = true
            });
            saved++;
        }

        var subject = await context.Subjects.FindAsync(subjectId);
        if (subject != null)
        {
            subject.LastUpdatedAt = DateTime.Now;
            subject.DocumentCount = await context.KnowledgeTemplates.CountAsync(t => t.SubjectId == subjectId && t.IsActive);
        }

        await context.SaveChangesAsync();
        return saved;
    }

    private async Task<List<string>> SearchPubMedIdsAsync(string keyword, int maxResults)
    {
        var url = "https://eutils.ncbi.nlm.nih.gov/entrez/eutils/esearch.fcgi"
            + $"?db=pubmed&retmode=json&retmax={maxResults}&sort=relevance&term={Uri.EscapeDataString(keyword)}";
        using var response = await _httpClient.GetAsync(url);
        response.EnsureSuccessStatusCode();

        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        if (!json.RootElement.TryGetProperty("esearchresult", out var searchResult)
            || !searchResult.TryGetProperty("idlist", out var idList))
        {
            return new List<string>();
        }

        return idList.EnumerateArray()
            .Select(id => id.GetString())
            .Where(id => !string.IsNullOrWhiteSpace(id))
            .Cast<string>()
            .ToList();
    }

    private async Task<List<PubMedArticleSummary>> FetchSummariesAsync(List<string> ids)
    {
        var url = "https://eutils.ncbi.nlm.nih.gov/entrez/eutils/esummary.fcgi"
            + $"?db=pubmed&retmode=json&id={Uri.EscapeDataString(string.Join(",", ids))}";
        using var response = await _httpClient.GetAsync(url);
        response.EnsureSuccessStatusCode();

        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        if (!json.RootElement.TryGetProperty("result", out var result))
            return new List<PubMedArticleSummary>();

        var articles = new List<PubMedArticleSummary>();
        foreach (var id in ids)
        {
            if (!result.TryGetProperty(id, out var item))
                continue;

            var title = ReadString(item, "title");
            if (string.IsNullOrWhiteSpace(title))
                continue;

            var authors = new List<string>();
            if (item.TryGetProperty("authors", out var authorArray))
            {
                authors = authorArray.EnumerateArray()
                    .Select(a => ReadString(a, "name"))
                    .Where(a => !string.IsNullOrWhiteSpace(a))
                    .Take(6)
                    .ToList();
            }

            articles.Add(new PubMedArticleSummary(
                id,
                title,
                ReadString(item, "fulljournalname"),
                ReadString(item, "pubdate"),
                ReadString(item, "source"),
                ReadString(item, "elocationid"),
                authors));
        }

        return articles;
    }

    private static string BuildContent(PubMedArticleSummary article)
    {
        var builder = new StringBuilder();
        builder.AppendLine($"题名：{article.Title}");
        builder.AppendLine($"PMID：{article.Pmid}");
        if (article.Authors.Count > 0)
            builder.AppendLine($"作者：{string.Join(", ", article.Authors)}");
        if (!string.IsNullOrWhiteSpace(article.Journal))
            builder.AppendLine($"期刊：{article.Journal}");
        if (!string.IsNullOrWhiteSpace(article.PubDate))
            builder.AppendLine($"发表时间：{article.PubDate}");
        if (!string.IsNullOrWhiteSpace(article.Doi))
            builder.AppendLine($"DOI/定位：{article.Doi}");
        builder.AppendLine($"链接：https://pubmed.ncbi.nlm.nih.gov/{article.Pmid}/");
        builder.AppendLine();
        builder.AppendLine("用途提示：该条目来自 PubMed 联网检索，可作为病程分析和科研选题的参考线索；正式引用请打开链接核对全文、指南级别和适用人群。");
        return builder.ToString();
    }

    private static async Task<DiseaseCategory> GetOrCreateCategoryAsync(AppDbContext context, int subjectId, string name)
    {
        var category = await context.DiseaseCategories.FirstOrDefaultAsync(c => c.SubjectId == subjectId && c.Name == name);
        if (category != null)
            return category;

        category = new DiseaseCategory
        {
            SubjectId = subjectId,
            Name = name,
            Code = "WEBLIT",
            Description = "联网检索导入的文献摘要",
            SortOrder = await context.DiseaseCategories.CountAsync(c => c.SubjectId == subjectId)
        };
        context.DiseaseCategories.Add(category);
        await context.SaveChangesAsync();
        return category;
    }

    private static string ReadString(JsonElement element, string propertyName)
    {
        return element.TryGetProperty(propertyName, out var property) && property.ValueKind == JsonValueKind.String
            ? property.GetString() ?? string.Empty
            : string.Empty;
    }

    private sealed record PubMedArticleSummary(
        string Pmid,
        string Title,
        string Journal,
        string PubDate,
        string Source,
        string Doi,
        List<string> Authors);
}
