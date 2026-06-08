using MedicalProgress.App.Data;
using MedicalProgress.App.Models;
using Microsoft.EntityFrameworkCore;

namespace MedicalProgress.App.Services;

public class KnowledgeCardMatchService
{
    private readonly BM25Service _bm25 = new();

    /// <summary>
    /// 根据患者上下文（诊断+症状+检查结果）在知识库中检索匹配的知识卡片。
    /// </summary>
    /// <param name="patientContext">患者上下文字符串（诊断+症状+检查结果拼接）</param>
    /// <param name="subjectId">学科 ID，0 表示全库搜索</param>
    /// <param name="topN">返回条数，默认 5</param>
    /// <returns>按相关度排序的 KnowledgeTemplate 列表</returns>
    public async Task<List<KnowledgeTemplate>> MatchAsync(string patientContext, int subjectId = 0, int topN = 5)
    {
        if (string.IsNullOrWhiteSpace(patientContext))
            return new List<KnowledgeTemplate>();

        using var context = new AppDbContext();
        IQueryable<KnowledgeTemplate> query = context.KnowledgeTemplates
            .Include(t => t.Subject)
            .Include(t => t.Category)
            .Where(t => t.IsActive);

        if (subjectId > 0)
            query = query.Where(t => t.SubjectId == subjectId);

        var templates = await query.ToListAsync();

        if (templates.Count == 0)
            return new List<KnowledgeTemplate>();

        var ranked = _bm25.Rank(patientContext, templates, topN);
        return ranked.Select(r => r.Template).ToList();
    }

    /// <summary>
    /// 同步版本（UI 线程使用）
    /// </summary>
    public List<KnowledgeTemplate> Match(string patientContext, int subjectId = 0, int topN = 5)
    {
        return MatchAsync(patientContext, subjectId, topN).GetAwaiter().GetResult();
    }
}
