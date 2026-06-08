using MedicalProgress.App.Models;

namespace MedicalProgress.App.Services;

public class FirstProgressQualityService
{
    public List<string> Check(Patient patient, ParsedClinicalDocument firstProgress, string rawFirstProgressText)
    {
        var warnings = new List<string>();
        var text = rawFirstProgressText ?? string.Empty;

        Require(warnings, !string.IsNullOrWhiteSpace(firstProgress.ChiefComplaint) || !string.IsNullOrWhiteSpace(patient.ChiefComplaint), "首程缺少明确主诉。");
        Require(warnings, !string.IsNullOrWhiteSpace(firstProgress.HistorySummary) || !string.IsNullOrWhiteSpace(patient.History), "首程病史小结不完整，建议补充起病经过、诊疗经过和当前主要功能问题。");
        Require(warnings, !string.IsNullOrWhiteSpace(firstProgress.PhysicalExam) || !string.IsNullOrWhiteSpace(patient.PhysicalExam), "首程查体内容不足，康复病程建议包含神经系统查体和功能查体。");
        Require(warnings, !string.IsNullOrWhiteSpace(firstProgress.AuxiliaryExam) || !string.IsNullOrWhiteSpace(patient.AuxiliaryExam), "首程缺少辅助检查或影像/化验依据。");
        Require(warnings, ContainsAny(text, "诊断依据", "根据患者病史", "结合患者病史"), "首程缺少诊断依据，建议按病史、查体、辅检分别说明。");
        Require(warnings, text.Contains("鉴别诊断"), "首程缺少鉴别诊断。");
        Require(warnings, ContainsAny(text, "诊疗计划", "治疗计划", "康复治疗"), "首程缺少诊疗计划。");

        Require(warnings, ContainsAny(text, "肌力", "肌张力", "Brunnstrom", "Ashworth", "平衡", "巴氏", "Barthel", "ADL"),
            "康复医学首程建议体现功能评定，如肌力、肌张力、Brunnstrom/Ashworth、平衡、ADL/Barthel 等。");

        Require(warnings, ContainsAny(text, "活动受限", "功能障碍", "偏瘫", "步态", "吞咽", "言语", "认知"),
            "建议补充康复问题列表或功能障碍分析，体现身体功能、活动和参与受限。");

        Require(warnings, ContainsAny(text, "综合康复", "作业治疗", "物理治疗", "平衡功能训练", "偏瘫肢体综合训练", "吞咽训练", "言语训练"),
            "诊疗计划建议写明具体康复治疗项目，而不仅是笼统治疗。");

        return warnings;
    }

    private static void Require(List<string> warnings, bool condition, string message)
    {
        if (!condition)
            warnings.Add(message);
    }

    private static bool ContainsAny(string text, params string[] values)
    {
        return values.Any(value => text.Contains(value, StringComparison.OrdinalIgnoreCase));
    }
}
