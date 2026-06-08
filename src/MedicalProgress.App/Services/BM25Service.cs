using System.Text.RegularExpressions;
using MedicalProgress.App.Models;

namespace MedicalProgress.App.Services;

public class BM25Service
{
    private const float K1 = 1.5f;
    private const float B  = 0.75f;

    // 对模板列表做 BM25 打分并返回 TopN
    public List<(KnowledgeTemplate Template, float Score)> Rank(
        string query,
        IList<KnowledgeTemplate> templates,
        int topN = 5)
    {
        if (string.IsNullOrWhiteSpace(query) || templates.Count == 0)
            return [];

        var queryTokens = Tokenize(query);
        if (queryTokens.Count == 0) return [];

        // 标题+关键词权重 ×3，内容 ×1
        var docTokens = templates
            .Select(t => Tokenize(
                t.Title + t.Title + t.Title + " " +
                t.Keywords + t.Keywords + " " +
                t.Content))
            .ToList();

        var N      = docTokens.Count;
        var avgDl  = docTokens.Average(d => (double)d.Count);
        if (avgDl < 1) avgDl = 1;

        // 文档频率 DF
        var df = new Dictionary<string, int>(StringComparer.Ordinal);
        foreach (var tokens in docTokens)
            foreach (var tok in tokens.ToHashSet())
            {
                df.TryGetValue(tok, out var c);
                df[tok] = c + 1;
            }

        var results = new List<(KnowledgeTemplate, float)>(N);
        for (int i = 0; i < templates.Count; i++)
        {
            var tokens = docTokens[i];
            var dl     = tokens.Count < 1 ? 1 : tokens.Count;

            var tf = new Dictionary<string, int>(StringComparer.Ordinal);
            foreach (var t in tokens) { tf.TryGetValue(t, out var c); tf[t] = c + 1; }

            float score = 0f;
            foreach (var qTok in queryTokens)
            {
                if (!df.TryGetValue(qTok, out var dfVal)) continue;
                var idf = (float)Math.Log((N - dfVal + 0.5) / (dfVal + 0.5) + 1.0);
                tf.TryGetValue(qTok, out var tfVal);
                score += idf * (tfVal * (K1 + 1f))
                             / (tfVal + K1 * (1f - B + B * (float)(dl / avgDl)));
            }

            if (score > 0f)
                results.Add((templates[i], score));
        }

        return results.OrderByDescending(x => x.Item2).Take(topN)
            .Select(x => (x.Item1, x.Item2)).ToList();
    }

    // 中文二元 + 三元分词，忽略空白和标点
    private static List<string> Tokenize(string text)
    {
        if (string.IsNullOrWhiteSpace(text)) return [];
        var clean  = Regex.Replace(text, @"[\s\p{P}]", "");
        var tokens = new List<string>(clean.Length * 2);
        for (int i = 0; i < clean.Length - 1; i++)
            tokens.Add(clean[i..(i + 2)]);
        for (int i = 0; i < clean.Length - 2; i++)
            tokens.Add(clean[i..(i + 3)]);
        return tokens;
    }
}
