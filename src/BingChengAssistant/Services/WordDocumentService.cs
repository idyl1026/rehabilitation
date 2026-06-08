using System.IO;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using BingChengAssistant.Models;

namespace BingChengAssistant.Services;

public static class WordDocumentService
{
    /// <summary>新建患者Word文档（病例回顾文档）</summary>
    public static void CreatePatientDocument(string filePath, Admission adm, Doctor doctor)
    {
        using var doc = WordprocessingDocument.Create(filePath, WordprocessingDocumentType.Document);
        var mainPart = doc.AddMainDocumentPart();
        mainPart.Document = new Document(new Body());
        var body = mainPart.Document.Body!;

        AddTitle(body, "病程助手病例回顾文档");
        AddHeading(body, "一、患者基本信息");
        AddField(body, "患者姓名", adm.Patient?.Name ?? "");
        AddField(body, "性别", adm.Patient?.Gender ?? "");
        AddField(body, "年龄", $"{adm.Patient?.Age}岁");
        AddField(body, "住院号", adm.AdmissionNo);
        AddField(body, "床号", adm.BedNo);
        AddField(body, "科室", adm.Department);
        AddField(body, "入院日期", adm.AdmissionDate.ToString("yyyy-MM-dd"));
        AddField(body, "主管医生", doctor.Name);
        AddField(body, "主要诊断", adm.MainDiagnosis);
        AddField(body, "文档创建时间", DateTime.Now.ToString("yyyy-MM-dd HH:mm"));

        AddHeading(body, "二、本次住院信息");
        AddParagraph(body, $"入院日期：{adm.AdmissionDate:yyyy-MM-dd}    科室：{adm.Department}");
        AddParagraph(body, $"主要诊断：{adm.MainDiagnosis}");

        AddHeading(body, "三、病程记录汇总");
        AddParagraph(body, "（病程记录将在保存时同步至此处）");

        AddHeading(body, "四、康复评估记录");
        AddParagraph(body, "（康复评估记录将在保存时同步至此处）");

        AddHeading(body, "五、出院记录");
        AddParagraph(body, "（出院归档后更新）");

        AddHeading(body, "六、科研备注");
        AddParagraph(body, "病例特点：");
        AddParagraph(body, "治疗亮点：");
        AddParagraph(body, "随访价值：");
        AddParagraph(body, "是否纳入科研分析：");

        mainPart.Document.Save();
    }

    /// <summary>追加病程记录到Word文档</summary>
    public static void AppendProgressNote(string filePath, ProgressNote note)
    {
        if (!File.Exists(filePath)) return;
        using var doc = WordprocessingDocument.Open(filePath, true);
        var body = doc.MainDocumentPart!.Document.Body!;

        // 找到"三、病程记录汇总"的位置之后插入
        var header = $"【{note.RecordDate:yyyy-MM-dd HH:mm} {note.NoteType}】";
        AddParagraph(body, "");
        AddParagraph(body, header);
        foreach (var line in note.Content.Split('\n'))
            AddParagraph(body, line.TrimEnd('\r'));

        doc.MainDocumentPart.Document.Save();
    }

    /// <summary>追加康复评估记录到Word文档</summary>
    public static void AppendRehabRecord(string filePath, RehabAssessmentRecord rec)
    {
        if (!File.Exists(filePath)) return;
        using var doc = WordprocessingDocument.Open(filePath, true);
        var body = doc.MainDocumentPart!.Document.Body!;
        AddParagraph(body, "");
        AddParagraph(body, $"【{rec.AssessmentDate:yyyy-MM-dd} {rec.ScaleName}】");
        AddParagraph(body, $"结果：{rec.ResultSummary}");
        AddParagraph(body, $"解释：{rec.Interpretation}");
        AddParagraph(body, $"建议：{rec.RehabAdvice}");
        doc.MainDocumentPart.Document.Save();
    }

    /// <summary>生成出院归档Word（汇总所有内容）</summary>
    public static void GenerateDischargeDocument(string filePath, Admission adm, Doctor doctor,
        List<ProgressNote> notes, List<RehabAssessmentRecord> rehabs)
    {
        using var doc = WordprocessingDocument.Create(filePath, WordprocessingDocumentType.Document);
        var mainPart = doc.AddMainDocumentPart();
        mainPart.Document = new Document(new Body());
        var body = mainPart.Document.Body!;

        AddTitle(body, "病例回顾文档（出院归档）");
        AddField(body, "患者姓名", adm.Patient?.Name ?? "");
        AddField(body, "性别/年龄", $"{adm.Patient?.Gender} / {adm.Patient?.Age}岁");
        AddField(body, "住院号", adm.AdmissionNo);
        AddField(body, "入院日期", adm.AdmissionDate.ToString("yyyy-MM-dd"));
        AddField(body, "出院日期", adm.DischargeDate?.ToString("yyyy-MM-dd") ?? "");
        AddField(body, "主要诊断", adm.MainDiagnosis);
        AddField(body, "出院诊断", adm.MainDiagnosis);
        AddField(body, "转归", adm.DischargeOutcome);
        AddField(body, "主管医生", doctor.Name);
        AddField(body, "归档时间", DateTime.Now.ToString("yyyy-MM-dd HH:mm"));

        AddHeading(body, "病程记录");
        foreach (var n in notes)
        {
            AddParagraph(body, $"【{n.RecordDate:yyyy-MM-dd HH:mm} {n.NoteType}】");
            foreach (var line in n.Content.Split('\n'))
                AddParagraph(body, line.TrimEnd('\r'));
            AddParagraph(body, "");
        }

        AddHeading(body, "康复评估记录");
        foreach (var r in rehabs)
        {
            AddParagraph(body, $"【{r.AssessmentDate:yyyy-MM-dd} {r.ScaleName}】结果：{r.ResultSummary}，{r.Interpretation}");
        }

        AddHeading(body, "出院医嘱");
        AddParagraph(body, adm.DischargeOrders);

        AddHeading(body, "康复建议");
        AddParagraph(body, adm.RehabAdvice);

        AddHeading(body, "科研备注");
        AddParagraph(body, adm.ResearchNote);

        mainPart.Document.Save();
    }

    private static void AddTitle(Body body, string text)
    {
        var p = new Paragraph(new Run(new Text(text)));
        var rpr = new RunProperties(new Bold(), new FontSize { Val = "36" });
        p.PrependChild(new ParagraphProperties(new Justification { Val = JustificationValues.Center }));
        body.AppendChild(p);
    }

    private static void AddHeading(Body body, string text)
    {
        var p = new Paragraph(new Run(
            new RunProperties(new Bold(), new FontSize { Val = "28" }),
            new Text(text)));
        body.AppendChild(p);
    }

    private static void AddField(Body body, string label, string value)
    {
        var p = new Paragraph(new Run(new Text($"{label}：{value}")));
        body.AppendChild(p);
    }

    private static void AddParagraph(Body body, string text)
    {
        body.AppendChild(new Paragraph(new Run(new Text(text))));
    }
}
