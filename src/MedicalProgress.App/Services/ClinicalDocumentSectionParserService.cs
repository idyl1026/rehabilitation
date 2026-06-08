using System.Text.RegularExpressions;

namespace MedicalProgress.App.Services;

public class ClinicalDocumentSectionParserService
{
    public ParsedClinicalDocument Parse(string normalizedText)
    {
        var parsed = new ParsedClinicalDocument
        {
            PatientName = MatchValue(normalizedText, @"姓名\s*(?<value>[\u4e00-\u9fa5]{2,8})"),
            Gender = MatchValue(normalizedText, @"性别\s*(?<value>男|女)"),
            Age = MatchValue(normalizedText, @"年龄\s*(?<value>\d{1,3})\s*岁?"),
            MedicalRecordNumber = MatchValue(normalizedText, @"病历号\s*(?<value>[A-Za-z0-9]+)"),
            Department = MatchValue(normalizedText, @"科室\s*(?<value>.+?)(床号|20\d{2})"),
            BedNumber = MatchValue(normalizedText, @"床号\s*(?<value>[A-Za-z0-9\-]+)"),
            ChiefComplaint = ExtractSection(normalizedText, "主诉", "病史小结"),
            HistorySummary = ExtractSection(normalizedText, "病史小结", "体检"),
            PhysicalExam = ExtractSection(normalizedText, "体检", "辅助检查"),
            AuxiliaryExam = ExtractSection(normalizedText, "辅助检查", "二、诊断及鉴别诊断"),
            Diagnosis = ExtractSection(normalizedText, "诊断", "诊断依据"),
            TreatmentPlan = ExtractSection(normalizedText, "三、诊疗计划", string.Empty)
        };

        return parsed;
    }

    private static string MatchValue(string text, string pattern)
    {
        var match = Regex.Match(text, pattern, RegexOptions.Singleline);
        return match.Success ? Clean(match.Groups["value"].Value) : string.Empty;
    }

    private static string ExtractSection(string text, string startLabel, string endLabel)
    {
        var startPattern = Regex.Escape(startLabel) + @"\s*[:：]?\s*";
        var start = Regex.Match(text, startPattern);
        if (!start.Success)
            return string.Empty;

        var contentStart = start.Index + start.Length;
        var contentEnd = text.Length;

        if (!string.IsNullOrWhiteSpace(endLabel))
        {
            var end = Regex.Match(text[contentStart..], Regex.Escape(endLabel) + @"\s*[:：]?");
            if (end.Success)
                contentEnd = contentStart + end.Index;
        }

        if (contentEnd <= contentStart)
            return string.Empty;

        return Clean(text[contentStart..contentEnd]);
    }

    private static string Clean(string value)
    {
        return Regex.Replace(value, @"\s+", " ").Trim(' ', ':', '：', ',', '，', ';', '；');
    }
}

public class ParsedClinicalDocument
{
    public string PatientName { get; set; } = string.Empty;
    public string Gender { get; set; } = string.Empty;
    public string Age { get; set; } = string.Empty;
    public string MedicalRecordNumber { get; set; } = string.Empty;
    public string Department { get; set; } = string.Empty;
    public string BedNumber { get; set; } = string.Empty;
    public string ChiefComplaint { get; set; } = string.Empty;
    public string HistorySummary { get; set; } = string.Empty;
    public string PhysicalExam { get; set; } = string.Empty;
    public string AuxiliaryExam { get; set; } = string.Empty;
    public string Diagnosis { get; set; } = string.Empty;
    public string TreatmentPlan { get; set; } = string.Empty;
}
