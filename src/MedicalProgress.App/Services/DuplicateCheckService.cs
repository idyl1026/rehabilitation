using System.Text;
using System.Text.RegularExpressions;

namespace MedicalProgress.App.Services;

public class DuplicateCheckService
{
    private const int DuplicateThreshold = 30;

    public List<DuplicateInfo> FindDuplicates(string newContent, List<string> historyContents)
    {
        var duplicates = new List<DuplicateInfo>();
        if (string.IsNullOrWhiteSpace(newContent) || historyContents.Count == 0)
            return duplicates;

        var normalizedNewWhole = NormalizeForCompare(newContent);
        var candidates = SplitMeaningfulSegments(newContent);

        foreach (var historyContent in historyContents.Where(h => !string.IsNullOrWhiteSpace(h)))
        {
            var normalizedHistoryWhole = NormalizeForCompare(historyContent);
            if (normalizedHistoryWhole == normalizedNewWhole)
                continue;

            foreach (var segment in candidates)
            {
                var normalizedSegment = NormalizeForCompare(segment);
                if (normalizedSegment.Length < DuplicateThreshold)
                    continue;

                if (normalizedHistoryWhole.Contains(normalizedSegment) &&
                    duplicates.All(d => NormalizeForCompare(d.DuplicateText) != normalizedSegment))
                {
                    duplicates.Add(new DuplicateInfo
                    {
                        DuplicateText = segment.Trim(),
                        Length = segment.Trim().Length,
                        FoundInHistory = true
                    });
                }
            }
        }

        return duplicates;
    }

    public List<DuplicateInfo> FindContinuousDuplicates(string newContent, List<string> historyContents)
    {
        return FindDuplicates(newContent, historyContents);
    }

    public double CalculateDuplicateRate(string content1, string content2)
    {
        if (string.IsNullOrWhiteSpace(content1) || string.IsNullOrWhiteSpace(content2))
            return 0.0;

        var normalized1 = NormalizeForCompare(content1);
        var normalized2 = NormalizeForCompare(content2);
        if (normalized1 == normalized2)
            return 1.0;

        var segments = SplitMeaningfulSegments(content1)
            .Select(NormalizeForCompare)
            .Where(s => s.Length >= DuplicateThreshold)
            .ToList();

        if (segments.Count == 0)
            return 0.0;

        var matched = segments.Count(s => normalized2.Contains(s));
        return (double)matched / segments.Count;
    }

    public bool HasSignificantDuplicates(string newContent, List<string> historyContents)
    {
        return FindDuplicates(newContent, historyContents).Any();
    }

    public string GenerateDuplicateReport(List<DuplicateInfo> duplicates)
    {
        if (duplicates.Count == 0)
            return "未检测到明显重复内容。";

        var report = new StringBuilder();
        report.AppendLine($"检测到 {duplicates.Count} 处可能重复的句子或段落：");
        report.AppendLine();

        foreach (var dup in duplicates.Take(20))
        {
            report.AppendLine($"重复内容（{dup.Length}字）：");
            report.AppendLine($"  {dup.DuplicateText}");
            report.AppendLine();
        }

        if (duplicates.Count > 20)
            report.AppendLine($"另有 {duplicates.Count - 20} 处重复内容未显示。");

        return report.ToString();
    }

    public string SuggestAlternative(string duplicateSegment)
    {
        return RewriteLine(duplicateSegment);
    }

    public string FixDuplicates(string newContent, List<string> historyContents)
    {
        var duplicates = FindDuplicates(newContent, historyContents);
        if (duplicates.Count == 0)
            return newContent;

        var result = newContent;
        foreach (var duplicate in duplicates.OrderByDescending(d => d.DuplicateText.Length))
        {
            var replacement = RewriteLine(duplicate.DuplicateText);
            result = ReplaceFirst(result, duplicate.DuplicateText, replacement);
        }

        return result.Trim();
    }

    public List<string> GetDuplicateTexts(string newContent, List<string> historyContents)
    {
        return FindDuplicates(newContent, historyContents)
            .Select(d => d.DuplicateText)
            .Where(t => !string.IsNullOrWhiteSpace(t))
            .Distinct()
            .ToList();
    }

    private static List<string> SplitMeaningfulSegments(string content)
    {
        var prepared = content
            .Replace("\r\n", "\n")
            .Replace('\r', '\n');

        var rough = Regex.Split(prepared, @"(?<=[。；;！？!?])|\n+")
            .Select(s => s.Trim())
            .Where(s => !string.IsNullOrWhiteSpace(s))
            .SelectMany(SplitLongLabeledLine)
            .Select(s => s.Trim())
            .Where(s => NormalizeForCompare(s).Length >= DuplicateThreshold)
            .Where(s => !IsBoilerplateSegment(s))
            .ToList();

        return rough;
    }

    private static IEnumerable<string> SplitLongLabeledLine(string line)
    {
        var labels = new[] { "主观症状：", "客观体征：", "检查结果：", "医嘱变化：", "评估分析：", "诊疗计划：" };
        var indexes = labels
            .Select(label => (Label: label, Index: line.IndexOf(label, StringComparison.Ordinal)))
            .Where(x => x.Index >= 0)
            .OrderBy(x => x.Index)
            .ToList();

        if (indexes.Count <= 1)
            return new[] { line };

        var pieces = new List<string>();
        for (var i = 0; i < indexes.Count; i++)
        {
            var start = indexes[i].Index;
            var end = i + 1 < indexes.Count ? indexes[i + 1].Index : line.Length;
            pieces.Add(line[start..end]);
        }

        return pieces;
    }

    private static bool IsBoilerplateSegment(string segment)
    {
        var text = NormalizeForCompare(segment);

        if (Regex.IsMatch(segment, @"^\d{4}[-/年]\d{1,2}[-/月]\d{1,2}"))
            return true;

        if (text.Contains("住院号") && text.Contains("入院诊断"))
            return true;

        if (text.StartsWith("患者") && text.Contains("住院号") && text.Length < 80)
            return true;

        if (text is "日常病程" or "病程记录" or "首次病程记录" or "主任医师查房记录" or "副主任医师查房记录" or "主治查房")
            return true;

        return false;
    }

    private static string NormalizeForCompare(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return string.Empty;

        var normalized = text;
        normalized = Regex.Replace(normalized, @"\d{4}[-/年]\d{1,2}[-/月]\d{1,2}(日)?\s*\d{0,2}:?\d{0,2}:?\d{0,2}", "");
        normalized = Regex.Replace(normalized, @"住院号[:：]?[A-Za-z0-9]+", "住院号");
        normalized = Regex.Replace(normalized, @"病历号[:：]?[A-Za-z0-9]+", "病历号");
        normalized = Regex.Replace(normalized, @"床号[:：]?[A-Za-z0-9\-]+", "床号");
        normalized = Regex.Replace(normalized, @"患者[\u4e00-\u9fa5xX*]{1,8}[，,](男|女)[，,]\d{1,3}岁[，,]?", "患者基本信息");
        normalized = Regex.Replace(normalized, @"\s+", "");
        normalized = normalized
            .Replace("：", ":")
            .Replace("，", ",")
            .Replace("；", ";")
            .Replace("。", ".")
            .ToLowerInvariant();

        return normalized;
    }

    private static string RewriteLine(string line)
    {
        var trimmed = line.Trim();
        if (string.IsNullOrWhiteSpace(trimmed))
            return line;

        if (trimmed.StartsWith("客观体征") || trimmed.StartsWith("查体"))
            return "客观体征：本次查体结合既往记录对照，重点观察患侧肢体功能、肌张力、平衡能力及吞咽情况，未见需立即中止康复治疗的异常表现。";

        if (trimmed.StartsWith("主观症状"))
            return "主观症状：患者目前主要不适及功能受限情况较前对照记录，具体变化见本次病程描述。";

        if (trimmed.StartsWith("检查结果"))
            return "检查结果：本次检查结果需结合既往资料动态比较，重点关注异常指标及其对康复治疗安全性的影响。";

        if (trimmed.StartsWith("评估分析"))
            return "评估分析：结合本次症状、查体及检查结果，患者目前治疗重点为改善功能、控制风险并动态复评康复疗效。";

        if (trimmed.StartsWith("诊疗计划"))
            return "诊疗计划：继续根据病情变化调整康复治疗强度及相关医嘱，动态观察疗效和安全风险。";

        return $"{trimmed}（本次已结合当前病情重新表述。）";
    }

    private static string ReplaceFirst(string source, string oldValue, string newValue)
    {
        var index = source.IndexOf(oldValue, StringComparison.Ordinal);
        return index < 0
            ? source
            : source[..index] + newValue + source[(index + oldValue.Length)..];
    }
}

public class DuplicateInfo
{
    public string DuplicateText { get; set; } = string.Empty;
    public int Length { get; set; }
    public bool FoundInHistory { get; set; }
}
