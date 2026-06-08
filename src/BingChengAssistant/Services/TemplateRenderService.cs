using BingChengAssistant.Models;

namespace BingChengAssistant.Services;

public static class TemplateRenderService
{
    public static string Render(string template, Admission adm, Doctor? doctor = null)
    {
        var p = adm.Patient;
        return template
            .Replace("{姓名}", p?.Name ?? "")
            .Replace("{性别}", p?.Gender ?? "")
            .Replace("{年龄}", $"{p?.Age ?? 0}")
            .Replace("{住院号}", adm.AdmissionNo)
            .Replace("{床号}", adm.BedNo)
            .Replace("{入院日期}", adm.AdmissionDate.ToString("yyyy-MM-dd"))
            .Replace("{主要诊断}", adm.MainDiagnosis)
            .Replace("{既往史}", p?.PastHistory ?? "")
            .Replace("{过敏史}", p?.AllergyHistory ?? "")
            .Replace("{科室}", adm.Department)
            .Replace("{医生姓名}", doctor?.Name ?? AppContextService.CurrentDoctor?.Name ?? "")
            .Replace("{今日日期}", DateTime.Now.ToString("yyyy-MM-dd"))
            .Replace("{今日症状}", "")
            .Replace("{查体}", "")
            .Replace("{检查摘要}", "")
            .Replace("{康复评估}", "")
            .Replace("{处理计划}", "");
    }
}
