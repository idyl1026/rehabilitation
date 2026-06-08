using System.IO;
using System.Text;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using MedicalProgress.App.Models;

namespace MedicalProgress.App.Services;

public class WordExportService
{
    public async Task<string> ExportCombinedTimelineAsync(Patient patient, string content, string outputPath)
    {
        try
        {
            Directory.CreateDirectory(outputPath);
            var safeName = string.Join("_", patient.Name.Split(Path.GetInvalidFileNameChars(), StringSplitOptions.RemoveEmptyEntries));
            if (string.IsNullOrWhiteSpace(safeName))
                safeName = "患者";

            var fileName = $"联合浏览_{safeName}_{DateTime.Now:yyyyMMdd_HHmmss}.docx";
            var filePath = Path.Combine(outputPath, fileName);

            using (var wordDocument = WordprocessingDocument.Create(filePath, WordprocessingDocumentType.Document))
            {
                var mainPart = wordDocument.AddMainDocumentPart();
                mainPart.Document = new Document();
                var body = mainPart.Document.AppendChild(new Body());

                AddTitle(body, "联合浏览病程记录");
                AddEmptyParagraph(body);

                foreach (var line in content.Replace("\r\n", "\n").Split('\n'))
                {
                    if (string.IsNullOrWhiteSpace(line))
                    {
                        AddEmptyParagraph(body);
                    }
                    else if (line.Contains("首次病程", StringComparison.Ordinal)
                        || line.Contains("首程资料", StringComparison.Ordinal)
                        || line.StartsWith("20", StringComparison.Ordinal)
                        || line.StartsWith("【", StringComparison.Ordinal))
                    {
                        AddSectionHeader(body, line.Trim());
                    }
                    else if (line.Trim().All(c => c == '=' || c == '-'))
                    {
                        AddEmptyParagraph(body);
                    }
                    else
                    {
                        AddParagraph(body, line);
                    }
                }
            }

            return await Task.FromResult(filePath);
        }
        catch (Exception ex)
        {
            throw new Exception($"导出联合浏览Word失败：{ex.Message}", ex);
        }
    }

    public async Task<string> ExportChuYuanJiLuAsync(Patient patient, List<ProgressRecord> records, string outputPath)
    {
        try
        {
            var fileName = $"出院记录_{patient.Name}_{DateTime.Now:yyyyMMdd_HHmmss}.docx";
            var filePath = Path.Combine(outputPath, fileName);

            using (var wordDocument = WordprocessingDocument.Create(filePath, WordprocessingDocumentType.Document))
            {
                var mainPart = wordDocument.AddMainDocumentPart();
                mainPart.Document = new Document();
                var body = mainPart.Document.AppendChild(new Body());

                AddTitle(body, "\u51fa \u9662 \u8bb0 \u5f55");
                AddEmptyParagraph(body);

                AddSectionHeader(body, "\u4e00\u3001\u57fa\u672c\u4fe1\u606f");
                AddPatientInfo(body, patient);
                AddEmptyParagraph(body);

                if (!string.IsNullOrEmpty(patient.Diagnosis))
                {
                    AddSectionHeader(body, "\u4e8c\u3001\u5165\u9662\u8bca\u65ad");
                    AddParagraph(body, patient.Diagnosis);
                    AddEmptyParagraph(body);
                }

                if (!string.IsNullOrEmpty(patient.DischargeDiagnosis))
                {
                    AddSectionHeader(body, "\u4e09\u3001\u51fa\u9662\u8bca\u65ad");
                    AddParagraph(body, patient.DischargeDiagnosis);
                    AddEmptyParagraph(body);
                }

                AddSectionHeader(body, "\u56db\u3001\u4e3b\u8bc9");
                AddParagraph(body, patient.ChiefComplaint);
                AddEmptyParagraph(body);

                if (!string.IsNullOrEmpty(patient.History))
                {
                    AddSectionHeader(body, "\u4e94\u3001\u73b0\u75c5\u53f2");
                    AddParagraph(body, patient.History);
                    AddEmptyParagraph(body);
                }

                if (!string.IsNullOrEmpty(patient.PhysicalExam))
                {
                    AddSectionHeader(body, "\u516d\u3001\u4f53\u683c\u68c0\u67e5");
                    AddParagraph(body, patient.PhysicalExam);
                    AddEmptyParagraph(body);
                }

                if (!string.IsNullOrEmpty(patient.AuxiliaryExam))
                {
                    AddSectionHeader(body, "\u4e03\u3001\u8f85\u52a9\u68c0\u67e5");
                    AddParagraph(body, patient.AuxiliaryExam);
                    AddEmptyParagraph(body);
                }

                AddSectionHeader(body, "\u516b\u3001\u8bca\u7597\u7ecf\u8fc7");
                if (!string.IsNullOrEmpty(patient.TreatmentPlan))
                {
                    AddParagraph(body, $"\u4f4f\u9662\u671f\u95f4\u4e88\u4ee5{patient.TreatmentPlan}\u6cbb\u7597\u3002");
                }

                if (records.Count > 0)
                {
                    AddEmptyParagraph(body);
                    AddParagraph(body, $"\u5171\u8bb0\u5f55 {records.Count} \u6b21\u75c5\u7a0b\u3002\u8be6\u89c1\u4e0b\u65b9\u75c5\u7a0b\u8bb0\u5f55\u3002");
                }
                AddEmptyParagraph(body);

                AddSectionHeader(body, "\u4e5d\u3001\u51fa\u9662\u60c5\u51b5");
                AddParagraph(body, "\u60a3\u8005\u4e00\u822c\u60c5\u51b5\u53ef\uff0c\u7cbe\u795e\u98df\u6b32\u6b63\u5e38\uff0c\u5927\u4fbf\u6b63\u5e38\uff0c\u65e0\u7279\u6b8a\u4e0d\u9002\u4e3b\u8bc9\u3002");
                AddParagraph(body, "\u67e5\u4f53\uff1a\u751f\u547d\u4f53\u5f81\u5e73\u7a33\uff0c\u5fc3\u80ba\u8179\u67e5\u4f53\u672a\u89c1\u660e\u663e\u5f02\u5e38\u3002");
                AddEmptyParagraph(body);

                if (!string.IsNullOrEmpty(patient.DischargeOrders))
                {
                    AddSectionHeader(body, "\u5341\u3001\u51fa\u9662\u533b\u5631");
                    AddParagraph(body, patient.DischargeOrders);
                }
                else
                {
                    AddSectionHeader(body, "\u5341\u3001\u51fa\u9662\u533b\u5631");
                    var hospitalDays = (patient.DischargeDate ?? DateTime.Now) - patient.AdmissionDate;
                    AddParagraph(body, "1. \u6ce8\u610f\u4f11\u606f\uff0c\u5408\u7406\u996d\u98df");
                    AddParagraph(body, "2. \u6309\u65f6\u670d\u836f\uff0c\u5b9a\u671f\u590d\u67e5");
                    AddParagraph(body, "3. \u4e0d\u9002\u968f\u8bca");
                    AddParagraph(body, $"4. \u51fa\u9662\u540e{hospitalDays.Days + 1}\u5929\u95e8\u8bca\u590d\u8bcd");
                }

                if (records.Count > 0)
                {
                    AddPageBreak(body);

                    AddTitle(body, "\u75c5 \u7a0b \u8bb0 \u5f55");
                    AddEmptyParagraph(body);

                    foreach (var record in records.OrderBy(r => r.RecordDate))
                    {
                        AddRecordHeader(body, record);
                        AddParagraph(body, record.Content);
                        AddEmptyParagraph(body);
                    }
                }

                AddPageBreak(body);

                var hospitalDays2 = (patient.DischargeDate ?? DateTime.Now) - patient.AdmissionDate;
                AddEmptyParagraph(body);
                AddEmptyParagraph(body);
                AddRightAlignedParagraph(body, $"\u51fa\u9662\u65e5\u671f\uff1a{patient.DischargeDate?.ToString("yyyy\u5e74MM\u6708dd\u65e5") ?? DateTime.Now.ToString("yyyy\u5e74MM\u6708dd\u65e5")}");
                if (!string.IsNullOrEmpty(patient.AttendingDoctor))
                {
                    AddRightAlignedParagraph(body, $"\u4e3b\u6cbb\u533b\u5e08\uff1a{patient.AttendingDoctor}");
                }
            }

            return await Task.FromResult(filePath);
        }
        catch (Exception ex)
        {
            throw new Exception($"\u751f\u6210Word\u6587\u6863\u5931\u8d25\uff1a{ex.Message}", ex);
        }
    }

    private void AddTitle(Body body, string text)
    {
        var paragraph = new Paragraph();
        var run = new Run();
        var textElement = new Text(text);
        run.AppendChild(textElement);
        paragraph.AppendChild(run);

        paragraph.ParagraphProperties = new ParagraphProperties(
            new Justification { Val = JustificationValues.Center },
            new SpacingBetweenLines { After = "400" }
        );

        run.RunProperties = new RunProperties(
            new Bold(),
            new FontSize { Val = "44" },
            new RunFonts { EastAsia = "\u9ed1\u4f53" }
        );

        body.AppendChild(paragraph);
    }

    private void AddSectionHeader(Body body, string text)
    {
        var paragraph = new Paragraph();
        var run = new Run();
        var textElement = new Text(text);
        run.AppendChild(textElement);
        paragraph.AppendChild(run);

        paragraph.ParagraphProperties = new ParagraphProperties(
            new SpacingBetweenLines { Before = "240", After = "120" }
        );

        run.RunProperties = new RunProperties(
            new Bold(),
            new FontSize { Val = "28" },
            new RunFonts { EastAsia = "\u5b8b\u4f53" }
        );

        body.AppendChild(paragraph);
    }

    private void AddParagraph(Body body, string text)
    {
        var paragraph = new Paragraph();
        var run = new Run();
        var textElement = new Text(text);
        run.AppendChild(textElement);
        paragraph.AppendChild(run);

        paragraph.ParagraphProperties = new ParagraphProperties(
            new SpacingBetweenLines { After = "120", Line = "360", LineRule = LineSpacingRuleValues.Auto },
            new Justification { Val = JustificationValues.Both }
        );

        run.RunProperties = new RunProperties(
            new FontSize { Val = "24" },
            new RunFonts { EastAsia = "\u5b8b\u4f53" }
        );

        body.AppendChild(paragraph);
    }

    private void AddRightAlignedParagraph(Body body, string text)
    {
        var paragraph = new Paragraph();
        var run = new Run();
        var textElement = new Text(text);
        run.AppendChild(textElement);
        paragraph.AppendChild(run);

        paragraph.ParagraphProperties = new ParagraphProperties(
            new Justification { Val = JustificationValues.Right },
            new SpacingBetweenLines { After = "120" }
        );

        run.RunProperties = new RunProperties(
            new FontSize { Val = "24" },
            new RunFonts { EastAsia = "\u5b8b\u4f53" }
        );

        body.AppendChild(paragraph);
    }

    private void AddEmptyParagraph(Body body)
    {
        body.AppendChild(new Paragraph());
    }

    private void AddPageBreak(Body body)
    {
        var paragraph = new Paragraph();
        var run = new Run();
        var breakElement = new Break { Type = BreakValues.Page };
        run.AppendChild(breakElement);
        paragraph.AppendChild(run);
        body.AppendChild(paragraph);
    }

    private void AddPatientInfo(Body body, Patient patient)
    {
        AddInfoLine(body, "\u59d3\u540d", patient.Name);
        AddInfoLine(body, "\u6027\u522b", patient.Gender);
        AddInfoLine(body, "\u5e74\u9f84", $"{patient.Age}\u5c81");
        if (!string.IsNullOrEmpty(patient.BedNumber))
            AddInfoLine(body, "\u5e8a\u53f7", patient.BedNumber);
        if (!string.IsNullOrEmpty(patient.MedicalRecordNumber))
            AddInfoLine(body, "\u4f4f\u9662\u53f7", patient.MedicalRecordNumber);
        if (!string.IsNullOrEmpty(patient.Department))
            AddInfoLine(body, "\u79d1\u5ba4", patient.Department);
        if (!string.IsNullOrEmpty(patient.AttendingDoctor))
            AddInfoLine(body, "\u4e3b\u6cbb\u533b\u5e08", patient.AttendingDoctor);

        var hospitalDays = (patient.DischargeDate ?? DateTime.Now) - patient.AdmissionDate;
        AddInfoLine(body, "\u5165\u9662\u65e5\u671f", patient.AdmissionDate.ToString("yyyy\u5e74MM\u6708dd\u65e5"));
        AddInfoLine(body, "\u51fa\u9662\u65e5\u671f", patient.DischargeDate?.ToString("yyyy\u5e74MM\u6708dd\u65e5") ?? "\u5f85\u5b9a");
        AddInfoLine(body, "\u4f4f\u9662\u5929\u6570", $"{hospitalDays.Days + 1}\u5929");
    }

    private void AddInfoLine(Body body, string label, string value)
    {
        var paragraph = new Paragraph();

        var labelRun = new Run();
        labelRun.AppendChild(new Text($"{label}\uff1a") { Space = SpaceProcessingModeValues.Preserve });
        labelRun.RunProperties = new RunProperties(
            new Bold(),
            new FontSize { Val = "24" },
            new RunFonts { EastAsia = "\u5b8b\u4f53" }
        );

        var valueRun = new Run();
        valueRun.AppendChild(new Text(value));
        valueRun.RunProperties = new RunProperties(
            new FontSize { Val = "24" },
            new RunFonts { EastAsia = "\u5b8b\u4f53" }
        );

        paragraph.AppendChild(labelRun);
        paragraph.AppendChild(valueRun);

        paragraph.ParagraphProperties = new ParagraphProperties(
            new SpacingBetweenLines { After = "60" }
        );

        body.AppendChild(paragraph);
    }

    private void AddRecordHeader(Body body, ProgressRecord record)
    {
        var paragraph = new Paragraph();

        var titleRun = new Run();
        titleRun.AppendChild(new Text($"\u3010{record.RecordDate:MM\u6708dd\u65e5}\u3011{record.RecordType}") { Space = SpaceProcessingModeValues.Preserve });
        titleRun.RunProperties = new RunProperties(
            new Bold(),
            new FontSize { Val = "24" },
            new RunFonts { EastAsia = "\u5b8b\u4f53" }
        );

        paragraph.AppendChild(titleRun);

        paragraph.ParagraphProperties = new ParagraphProperties(
            new SpacingBetweenLines { Before = "240", After = "120" }
        );

        body.AppendChild(paragraph);
    }
}
