using System.Text.RegularExpressions;

namespace MedicalProgress.App.Services;

public class KnowledgePoint
{
    public string Title { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public List<string> Keywords { get; set; } = new();
    public double Score { get; set; }
}

public class KnowledgePointExtractorService
{
    private static readonly string[] ClinicalKeywords =
    {
        "NIHSS", "Brunnstrom", "Ashworth", "Barthel", "MMSE", "MoCA", "FMA", "VAS", "MRS",
        "洼田", "偏瘫", "痉挛", "肌力", "肌张力", "吞咽", "平衡", "步行", "关节",
        "脑卒中", "脑梗死", "脑出血", "康复", "评定", "训练", "治疗", "手术", "药物",
        "诊断", "体征", "症状", "影像", "CT", "MRI", "血压", "心率", "血糖", "血红蛋白", "肌酐"
    };

    private static readonly Regex RegexNumeric = new(@"\d+分|[≥≤<>]\d|mg|次/日|次/天|ml|mmHg|mmol|g/L|IU", RegexOptions.Compiled);
    private static readonly Regex RegexImperative = new(@"应|需|禁止|优先|必须|不得|每日|每次", RegexOptions.Compiled);
    private static readonly Regex RegexDiagnosis = new(@"诊断|标准|原则|方法|评估|适应症|禁忌症", RegexOptions.Compiled);
    private static readonly Regex RegexTransition = new(@"如前所述|综上所述|由此可见|总之|因此可以看出", RegexOptions.Compiled);
    private static readonly Regex RegexCitation = new(@"研究表明|学者认为|有报道|文献报道|据报道", RegexOptions.Compiled);
    private static readonly Regex RegexExample = new(@"例如|比如|举例", RegexOptions.Compiled);
    private static readonly Regex RegexListItem = new(@"^[\s]*[①②③④⑤⑥⑦⑧⑨⑩\d]+[\.、．]\s*", RegexOptions.Compiled);
    private static readonly Regex RegexChineseListItem = new(@"^[\s]*[一二三四五六七八九十]+[、，。\s]", RegexOptions.Compiled);

    public List<KnowledgePoint> Extract(string rawText)
    {
        if (string.IsNullOrWhiteSpace(rawText))
            return new List<KnowledgePoint>();

        // 分句
        var sentences = SplitSentences(rawText);

        // 给每句打分
        var scored = sentences
            .Select(s => (Sentence: s, Score: ScoreSentence(s)))
            .Where(x => x.Score >= 2)
            .ToList();

        if (scored.Count == 0)
            return new List<KnowledgePoint>();

        // 将高分句子合并为知识点（连续的句子合并为一条）
        var groups = GroupContiguousSentences(rawText, scored.Select(x => x.Sentence).ToList());

        var result = new List<KnowledgePoint>();
        foreach (var group in groups)
        {
            var content = string.Join("", group);
            var keywords = ExtractKeywords(content);
            var avgScore = group.Average(s => ScoreSentence(s));
            var title = ExtractTitle(group[0]);

            result.Add(new KnowledgePoint
            {
                Title = title,
                Content = content.Trim(),
                Keywords = keywords,
                Score = Math.Round(avgScore, 2)
            });
        }

        return result;
    }

    private static List<string> SplitSentences(string text)
    {
        // 按句号、问号、感叹号、换行分句
        var parts = Regex.Split(text, @"(?<=[。！？\n])");
        var sentences = new List<string>();
        foreach (var p in parts)
        {
            var trimmed = p.Trim();
            if (trimmed.Length >= 4)
                sentences.Add(trimmed);
        }
        return sentences;
    }

    private static int ScoreSentence(string sentence)
    {
        var score = 0;

        // 含数值/量表分数
        if (RegexNumeric.IsMatch(sentence)) score += 3;

        // 含应/需/禁止等指令性词
        if (RegexImperative.IsMatch(sentence)) score += 2;

        // 含临床关键词
        if (ClinicalKeywords.Any(k => sentence.Contains(k, StringComparison.OrdinalIgnoreCase))) score += 2;

        // 是编号列表项
        if (RegexListItem.IsMatch(sentence) || RegexChineseListItem.IsMatch(sentence)) score += 2;

        // 含诊断/标准/原则等
        if (RegexDiagnosis.IsMatch(sentence)) score += 1;

        // 过渡句惩罚
        if (RegexTransition.IsMatch(sentence)) score -= 3;

        // 引用句惩罚
        if (RegexCitation.IsMatch(sentence)) score -= 2;

        // 例子惩罚
        if (RegexExample.IsMatch(sentence)) score -= 2;

        // 句长惩罚
        if (sentence.Length < 10) score -= 2;
        if (sentence.Length > 200) score -= 1;

        return score;
    }

    private static List<List<string>> GroupContiguousSentences(string rawText, List<string> keepSentences)
    {
        // 按原文顺序排列保留的句子，连续的合并在一起
        var groups = new List<List<string>>();
        List<string>? current = null;
        string? lastSentence = null;

        foreach (var sentence in keepSentences)
        {
            if (current == null)
            {
                current = new List<string> { sentence };
            }
            else
            {
                // 如果上一句和这句在原文中是相邻的（间距很近），则合并
                var lastIdx = rawText.IndexOf(lastSentence!, StringComparison.Ordinal);
                var currIdx = rawText.IndexOf(sentence, StringComparison.Ordinal);
                var gap = currIdx - lastIdx - lastSentence!.Length;

                if (gap <= 20) // 中间间距不超过20字符视为连续
                {
                    current.Add(sentence);
                }
                else
                {
                    groups.Add(current);
                    current = new List<string> { sentence };
                }
            }
            lastSentence = sentence;
        }

        if (current != null && current.Count > 0)
            groups.Add(current);

        return groups;
    }

    private static string ExtractTitle(string firstSentence)
    {
        // 取前20字作为标题
        var clean = Regex.Replace(firstSentence, @"^[\s①②③④⑤⑥⑦⑧⑨⑩\d\.、]+", "").Trim();
        return clean.Length > 20 ? clean[..20] : clean;
    }

    private static List<string> ExtractKeywords(string content)
    {
        return ClinicalKeywords
            .Where(k => content.Contains(k, StringComparison.OrdinalIgnoreCase))
            .ToList();
    }
}
