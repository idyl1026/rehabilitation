using System.Text;
using System.Text.RegularExpressions;

namespace MedicalProgress.App.Services;

public class ClinicalTextNormalizeService
{
    public string Normalize(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return string.Empty;

        var normalized = text
            .Replace("\r\n", "\n")
            .Replace('\r', '\n')
            .Replace('\u00a0', ' ')
            .Replace('\t', ' ')
            .Replace('：', ':')
            .Replace('，', ',')
            .Replace('；', ';')
            .Replace('（', '(')
            .Replace('）', ')');

        normalized = Regex.Replace(normalized, @"[ ]{2,}", " ");
        normalized = InsertClinicalBreaks(normalized);
        normalized = Regex.Replace(normalized, @"\n{3,}", "\n\n");

        var lines = normalized
            .Split('\n')
            .Select(line => line.Trim())
            .Where(line => !string.IsNullOrWhiteSpace(line))
            .Where(line => !IsLikelyPageNoise(line))
            .ToList();

        return string.Join(Environment.NewLine, lines);
    }

    private static string InsertClinicalBreaks(string text)
    {
        var normalized = text;

        normalized = Regex.Replace(normalized, @"(?<!^)(20\d{2}[-/.年]\d{1,2}[-/.月]\d{1,2}\s*\d{1,2}:\d{1,2}:\d{1,2})", "\n$1");

        string[] labels =
        {
            "首次病程记录", "副主任医师查房记录", "主治医师查房记录", "病程记录",
            "一、病史特点", "二、诊断及鉴别诊断", "三、诊疗计划",
            "主诉", "病史小结", "体检", "辅助检查", "诊断", "诊断依据", "鉴别诊断",
            "主观症状", "客观体征", "检查结果", "分析指导", "评估分析", "下一步诊疗计划", "诊疗计划"
        };

        foreach (var label in labels)
        {
            normalized = Regex.Replace(normalized, $@"(?<!^)(?<!\n)({Regex.Escape(label)}\s*:)", "\n$1");
            normalized = Regex.Replace(normalized, $@"(?<!^)(?<!\n)({Regex.Escape(label)}\s*：)", "\n$1");
        }

        normalized = Regex.Replace(normalized, @"(?<!^)(?<!\n)([一二三四五六七八九十]、)", "\n$1");
        return normalized;
    }

    private static bool IsLikelyPageNoise(string line)
    {
        if (Regex.IsMatch(line, @"^第\s*\d+\s*页\s*/?\s*共?\s*\d*\s*页?$"))
            return true;

        if (Regex.IsMatch(line, @"^\-+\s*\d+\s*\-+$"))
            return true;

        return false;
    }

    public string BuildPlainSummary(IEnumerable<string> lines, int maxLines = 20)
    {
        var builder = new StringBuilder();
        foreach (var line in lines.Where(l => !string.IsNullOrWhiteSpace(l)).Take(maxLines))
        {
            builder.AppendLine(line.Trim());
        }

        return builder.ToString().Trim();
    }
}
