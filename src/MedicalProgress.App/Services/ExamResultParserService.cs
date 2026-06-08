using System.Globalization;
using System.Text.RegularExpressions;
using MedicalProgress.App.Models;

namespace MedicalProgress.App.Services;

public class ExamResultParserService
{
    private static readonly string[] LabHints =
    {
        "WBC", "RBC", "HGB", "PLT", "ALT", "AST", "CRP", "PCT", "GLU", "K", "Na", "Cl",
        "白细胞", "红细胞", "血红蛋白", "血小板", "谷丙", "谷草", "肌酐", "尿素", "葡萄糖",
        "钾", "钠", "氯", "C反应蛋白", "降钙素原"
    };

    private static readonly string[] ImagingHints =
    {
        "CT", "MRI", "DR", "X线", "超声", "彩超", "影像", "提示", "所见", "印象", "结论"
    };

    private static readonly string[] ScaleHints =
    {
        "NIHSS", "BI", "Barthel", "MMSE", "MoCA", "MRS", "FMA", "VAS", "洼田", "Brunnstrom"
    };

    public List<StructuredExamResult> Parse(string normalizedText, int? patientId = null, int importedDocumentId = 0)
    {
        var results = new List<StructuredExamResult>();
        if (string.IsNullOrWhiteSpace(normalizedText))
            return results;

        var lines = normalizedText.Split(new[] { "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries);
        DateTime? currentDate = null;
        var reportName = DetectReportName(lines);
        var examType = DetectExamType(normalizedText);

        foreach (var originalLine in lines)
        {
            var line = originalLine.Trim();
            currentDate ??= ExtractDate(line);

            var dateInLine = ExtractDate(line);
            if (dateInLine.HasValue)
                currentDate = dateInLine;

            var segments = SplitExamSegments(line, currentDate, reportName);
            foreach (var segment in segments)
            {
                var segmentType = DetectExamType($"{segment.ReportName} {segment.Text}");
                var items = ParseItemLine(
                    segment.Text,
                    patientId,
                    importedDocumentId,
                    segment.ExamDate ?? currentDate,
                    segmentType == "Unknown" ? examType : segmentType,
                    string.IsNullOrWhiteSpace(segment.ReportName) ? reportName : segment.ReportName);

                if (items.Count == 0 && IsNarrativeReport(segmentType, segment.Text))
                {
                    items.Add(new StructuredExamResult
                    {
                        PatientId = patientId,
                        ImportedDocumentId = importedDocumentId,
                        ExamDate = segment.ExamDate ?? currentDate,
                        ExamType = segmentType,
                        ReportName = string.IsNullOrWhiteSpace(segment.ReportName) ? reportName : segment.ReportName,
                        ItemName = "Report conclusion",
                        Conclusion = TrimSegmentHeader(segment.Text),
                        RawLine = segment.Text
                    });
                }

                results.AddRange(items);
            }
        }

        if (!results.Any() && IsNarrativeReport(examType, normalizedText))
        {
            results.Add(new StructuredExamResult
            {
                PatientId = patientId,
                ImportedDocumentId = importedDocumentId,
                ExamDate = currentDate,
                ExamType = examType,
                ReportName = reportName,
                ItemName = "Report conclusion",
                Conclusion = normalizedText,
                RawLine = normalizedText
            });
        }

        return results;
    }

    public string DetectExamType(string text)
    {
        if (ContainsAny(text, ScaleHints))
            return "Scale";

        if (ContainsAny(text, ImagingHints))
            return "Imaging";

        if (ContainsAny(text, LabHints))
            return "Lab";

        return "Unknown";
    }

    private static List<StructuredExamResult> ParseItemLine(
        string line,
        int? patientId,
        int importedDocumentId,
        DateTime? examDate,
        string examType,
        string reportName)
    {
        var results = new List<StructuredExamResult>();
        var abnormal = DetectAbnormalFlag(line);
        var target = ExtractExamTargetText(line);

        var numericPattern = @"(?<name>\*?[\u4e00-\u9fa5A-Za-z][\u4e00-\u9fa5A-Za-z0-9\-\+\.\s/%]{1,24}?)[\s:：]*(?<value>[<>]?\d+(?:\.\d+)?)\s*(?<unit>×10\^\d+/L|[A-Za-zμ%/^\*\.\-\u4e00-\u9fa5]+)?(?<flag>[↑↓])?";
        var matches = Regex.Matches(target, numericPattern, RegexOptions.IgnoreCase);

        foreach (Match match in matches)
        {
            var itemName = NormalizeItemName(match.Groups["name"].Value);
            if (!LooksLikeExamItem(itemName))
                continue;

            var itemFlag = DetectAbnormalFlag(match.Value);
            results.Add(new StructuredExamResult
            {
                PatientId = patientId,
                ImportedDocumentId = importedDocumentId,
                ExamDate = examDate,
                ExamType = examType,
                ReportName = reportName,
                ItemName = itemName,
                ResultValue = match.Groups["value"].Value,
                Unit = match.Groups["unit"].Value.Trim(),
                AbnormalFlag = string.IsNullOrEmpty(itemFlag) ? abnormal : itemFlag,
                RawLine = line
            });
        }

        if (line.Contains("结论") || line.Contains("印象") || line.Contains("提示"))
        {
            results.Add(new StructuredExamResult
            {
                PatientId = patientId,
                ImportedDocumentId = importedDocumentId,
                ExamDate = examDate,
                ExamType = examType,
                ReportName = reportName,
                ItemName = "Report conclusion",
                Conclusion = line,
                AbnormalFlag = abnormal,
                RawLine = line
            });
        }

        return results;
    }

    private static List<ExamSegment> SplitExamSegments(string line, DateTime? fallbackDate, string fallbackReportName)
    {
        var target = ExtractExamTargetText(line);
        var pattern = @"(?<date>20\d{2}[-/.年]\d{1,2}[-/.月]\d{1,2})\s*[，,、\s]*(?<name>[^:：。；;\n]{2,80}?)(?:[:：])";
        var matches = Regex.Matches(target, pattern);

        if (matches.Count == 0)
        {
            return new List<ExamSegment>
            {
                new(fallbackDate, fallbackReportName, target)
            };
        }

        var segments = new List<ExamSegment>();
        for (var i = 0; i < matches.Count; i++)
        {
            var match = matches[i];
            var start = match.Index;
            var end = i + 1 < matches.Count ? matches[i + 1].Index : target.Length;
            var text = target[start..end].Trim(' ', '，', ',', '。', ';', '；');
            var name = CleanReportName(match.Groups["name"].Value);
            var date = ParseDate(match.Groups["date"].Value) ?? fallbackDate;

            if (!string.IsNullOrWhiteSpace(text))
                segments.Add(new ExamSegment(date, name, text));
        }

        return segments.Count > 0
            ? segments
            : new List<ExamSegment> { new(fallbackDate, fallbackReportName, target) };
    }

    private static string DetectReportName(string[] lines)
    {
        var title = lines
            .Select(l => l.Trim())
            .FirstOrDefault(l => l.Length is > 2 and <= 80 && (l.Contains("报告") || l.Contains("检查") || l.Contains("检验")));

        return title ?? string.Empty;
    }

    private static DateTime? ExtractDate(string line)
    {
        var match = Regex.Match(line, @"(?<year>20\d{2})[-年/.](?<month>\d{1,2})[-月/.](?<day>\d{1,2})");
        if (!match.Success)
            return null;

        return ParseDate(match.Value);
    }

    private static DateTime? ParseDate(string value)
    {
        var match = Regex.Match(value, @"(?<year>20\d{2})[-年/.](?<month>\d{1,2})[-月/.](?<day>\d{1,2})");
        if (!match.Success)
            return null;

        var normalized = $"{match.Groups["year"].Value}-{match.Groups["month"].Value}-{match.Groups["day"].Value}";
        return DateTime.TryParse(normalized, CultureInfo.InvariantCulture, DateTimeStyles.None, out var date)
            ? date
            : null;
    }

    private static string DetectAbnormalFlag(string line)
    {
        if (line.Contains("↑") || line.Contains("偏高") || Regex.IsMatch(line, @"\bH\b"))
            return "High";

        if (line.Contains("↓") || line.Contains("偏低") || Regex.IsMatch(line, @"\bL\b"))
            return "Low";

        if (line.Contains("阳性") || line.Contains("异常"))
            return "Abnormal";

        return string.Empty;
    }

    private static string ExtractExamTargetText(string line)
    {
        var markers = new[] { "检查结果:", "辅助检查:", "辅检:" };
        foreach (var marker in markers)
        {
            var index = line.IndexOf(marker, StringComparison.OrdinalIgnoreCase);
            if (index >= 0)
                return line[(index + marker.Length)..];
        }

        return line;
    }

    private static string CleanReportName(string reportName)
    {
        var name = Regex.Replace(reportName, @"\s+", " ").Trim(' ', ',', '，', ';', '；');
        name = Regex.Replace(name, @"^(检查结果|辅助检查|辅检)\s*[:：]?\s*", "");
        return name;
    }

    private static string TrimSegmentHeader(string text)
    {
        return Regex.Replace(
            text,
            @"^20\d{2}[-/.年]\d{1,2}[-/.月]\d{1,2}\s*[，,、\s]*[^:：]{2,80}[:：]\s*",
            "").Trim();
    }

    private static bool LooksLikeExamItem(string name)
    {
        var trimmed = name.Trim();
        if (trimmed.Length < 2 || trimmed.Length > 40)
            return false;

        return ContainsAny(trimmed, LabHints) || ContainsAny(trimmed, ScaleHints) || Regex.IsMatch(trimmed, @"^[A-Za-z]{2,8}$");
    }

    private static string NormalizeItemName(string itemName)
    {
        var name = Regex.Replace(itemName.Trim().TrimStart('*', ',', '，', ';', '；'), @"\s+", " ");

        return name switch
        {
            "白细胞" or "白细胞计数" => "WBC",
            "红细胞" or "红细胞计数" => "RBC",
            "血红蛋白" => "HGB",
            "血小板" or "血小板计数" => "PLT",
            "C反应蛋白" => "CRP",
            "降钙素原" => "PCT",
            _ => name
        };
    }

    private static bool ContainsAny(string text, IEnumerable<string> hints)
    {
        return hints.Any(hint => text.Contains(hint, StringComparison.OrdinalIgnoreCase));
    }

    private static bool IsNarrativeReport(string examType, string text)
    {
        return examType is "Imaging" or "Scale" || text.Length > 80;
    }

    private record ExamSegment(DateTime? ExamDate, string ReportName, string Text);
}
