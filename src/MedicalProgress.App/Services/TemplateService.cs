using System.Text;
using System.Text.RegularExpressions;
using MedicalProgress.App.Models;
using MedicalProgress.App.Templates;

namespace MedicalProgress.App.Services;

public class TemplateService
{
    private readonly Dictionary<string, Func<Patient, ProgressRecord?, int, string>> _templateGenerators;

    public TemplateService()
    {
        _templateGenerators = new Dictionary<string, Func<Patient, ProgressRecord?, int, string>>
        {
            { "首次病程", GenerateInitialProgress },
            { "日常病程", GenerateDailyProgress },
            { "出院小结", GenerateDischargeSummary }
        };
    }

    public List<string> GetAvailableTemplates()
    {
        return _templateGenerators.Keys.ToList();
    }

    public string GenerateFromTemplate(string templateType, Patient patient, ProgressRecord? previousRecord)
    {
        if (_templateGenerators.TryGetValue(templateType, out var generator))
        {
            var hospitalDays = (DateTime.Now - patient.AdmissionDate).Days + 1;
            return generator(patient, previousRecord, hospitalDays);
        }

        return GenerateDailyProgress(patient, previousRecord, 1);
    }

    private string GenerateInitialProgress(Patient patient, ProgressRecord? previousRecord, int hospitalDays)
    {
        var template = ProgressTemplates.GetInitialProgressTemplate();
        var patientInfo = $"{patient.Name}，{patient.Gender}，{patient.Age}岁，{patient.AdmissionDate:yyyy年MM月dd日}入院";

        var diagnosis = ExtractDiagnosis(patient.ChiefComplaint, patient.History);

        var result = template
            .Replace("{Date}", DateTime.Now.ToString("yyyy年MM月dd日"))
            .Replace("{PatientInfo}", patientInfo)
            .Replace("{ChiefComplaintSection}", FormatChiefComplaint(patient.ChiefComplaint))
            .Replace("{HistorySection}", FormatHistory(patient.History))
            .Replace("{PhysicalExamSection}", FormatPhysicalExam(patient.PhysicalExam))
            .Replace("{AuxiliaryExamSection}", FormatAuxiliaryExam(patient.AuxiliaryExam))
            .Replace("{PreliminaryDiagnosis}", diagnosis)
            .Replace("{TreatmentPlanSection}", FormatTreatmentPlan(patient.TreatmentPlan));

        return result;
    }

    private string GenerateDailyProgress(Patient patient, ProgressRecord? previousRecord, int hospitalDays)
    {
        var template = ProgressTemplates.GetDailyProgressTemplate();
        var patientInfo = $"{patient.Name}，{patient.Gender}，{patient.Age}岁，住院第{hospitalDays}天";

        var conditionChanges = GenerateConditionChanges(patient, previousRecord, hospitalDays);
        var treatmentSituation = GenerateTreatmentSituation(patient);
        var examSituation = GenerateExamSituation(patient);
        var nextPlan = GenerateNextPlan(patient, hospitalDays);

        var result = template
            .Replace("{Date}", DateTime.Now.ToString("yyyy年MM月dd日"))
            .Replace("{PatientInfo}", patientInfo)
            .Replace("{ConditionChanges}", conditionChanges)
            .Replace("{TreatmentSituation}", treatmentSituation)
            .Replace("{ExamSituation}", examSituation)
            .Replace("{NextPlan}", nextPlan);

        return result;
    }

    private string GenerateDischargeSummary(Patient patient, ProgressRecord? previousRecord, int hospitalDays)
    {
        var template = ProgressTemplates.GetDischargeSummaryTemplate();
        var patientInfo = $"{patient.Name}，{patient.Gender}，{patient.Age}岁";

        var admissionSituation = $"{patient.Name}因\"{patient.ChiefComplaint}\"入院。{patient.History}";
        var treatmentProcess = $"住院期间给予{patient.TreatmentPlan}。{GenerateTreatmentSummary(patient)}";
        var dischargeSituation = GenerateDischargeSituation(patient);
        var dischargeOrders = GenerateDischargeOrders(patient);

        var result = template
            .Replace("{Date}", DateTime.Now.ToString("yyyy年MM月dd日"))
            .Replace("{PatientInfo}", patientInfo)
            .Replace("{HospitalDays}", hospitalDays.ToString())
            .Replace("{AdmissionSituation}", admissionSituation)
            .Replace("{TreatmentProcess}", treatmentProcess)
            .Replace("{DischargeSituation}", dischargeSituation)
            .Replace("{DischargeOrders}", dischargeOrders);

        return result;
    }

    private string FormatChiefComplaint(string chiefComplaint)
    {
        if (string.IsNullOrWhiteSpace(chiefComplaint))
            return "患者家属代诉，患者因" + "\"反复不适\"入院。";

        if (chiefComplaint.Length > 200)
            return chiefComplaint.Substring(0, 200) + "...";

        return chiefComplaint;
    }

    private string FormatHistory(string history)
    {
        if (string.IsNullOrWhiteSpace(history))
            return "患者自发病以来，精神、食欲、睡眠可，大小便正常，体重无明显变化。";

        if (history.Length > 500)
            return history.Substring(0, 500) + "...";

        return history;
    }

    private string FormatPhysicalExam(string physicalExam)
    {
        if (string.IsNullOrWhiteSpace(physicalExam))
            return "T：36.5℃，P：78次/分，R：18次/分，BP：120/80mmHg。神志清，精神可，心肺腹未见明显异常。";

        if (physicalExam.Length > 500)
            return physicalExam.Substring(0, 500) + "...";

        return physicalExam;
    }

    private string FormatAuxiliaryExam(string auxiliaryExam)
    {
        if (string.IsNullOrWhiteSpace(auxiliaryExam))
            return "暂无异常结果。";

        if (auxiliaryExam.Length > 500)
            return auxiliaryExam.Substring(0, 500) + "...";

        return auxiliaryExam;
    }

    private string ExtractDiagnosis(string chiefComplaint, string history)
    {
        var keywords = new[] { "诊断", "考虑", "可能", "待查" };

        foreach (var keyword in keywords)
        {
            if (chiefComplaint.Contains(keyword))
            {
                var index = chiefComplaint.IndexOf(keyword);
                var diagnosis = chiefComplaint.Substring(Math.Max(0, index - 20), Math.Min(100, chiefComplaint.Length - index));
                return diagnosis;
            }
        }

        if (!string.IsNullOrWhiteSpace(history) && history.Length > 10)
        {
            return "待进一步检查明确诊断";
        }

        return "待诊断";
    }

    private string FormatTreatmentPlan(string treatmentPlan)
    {
        if (string.IsNullOrWhiteSpace(treatmentPlan))
            return "1. 完善相关检查\n2. 对症支持治疗\n3. 密切观察病情变化";

        if (treatmentPlan.Length > 300)
            return treatmentPlan.Substring(0, 300) + "...";

        return treatmentPlan;
    }

    private string GenerateConditionChanges(Patient patient, ProgressRecord? previousRecord, int hospitalDays)
    {
        if (previousRecord == null || hospitalDays <= 1)
        {
            return "患者入院后一般情况可，无特殊不适主诉。";
        }

        var previousSummary = previousRecord.Summary;
        if (string.IsNullOrWhiteSpace(previousSummary))
        {
            previousSummary = "病情平稳";
        }

        var variations = new[]
        {
            "患者一般情况可，诉无明显不适。",
            "患者诉症状较前有所改善，无特殊不适。",
            "患者病情稳定，继续目前治疗。",
            "患者精神食欲可，大小便正常。"
        };

        var random = new Random();
        return variations[random.Next(variations.Length)];
    }

    private string GenerateTreatmentSituation(Patient patient)
    {
        if (string.IsNullOrWhiteSpace(patient.TreatmentPlan))
        {
            return "继续当前治疗方案，密切观察病情变化。";
        }

        if (patient.TreatmentPlan.Length > 200)
        {
            return patient.TreatmentPlan.Substring(0, 200) + "...";
        }

        return $"继续予以{patient.TreatmentPlan}治疗。";
    }

    private string GenerateExamSituation(Patient patient)
    {
        if (string.IsNullOrWhiteSpace(patient.PhysicalExam))
        {
            return "生命体征平稳，心肺腹查体无明显异常。";
        }

        if (patient.PhysicalExam.Length > 200)
        {
            return patient.PhysicalExam.Substring(0, 200) + "...";
        }

        return patient.PhysicalExam;
    }

    private string GenerateNextPlan(Patient patient, int hospitalDays)
    {
        var plans = new List<string>
        {
            "1. 继续当前治疗方案",
            "2. 完善相关辅助检查",
            "3. 密切观察病情变化",
            "4. 根据检查结果调整治疗"
        };

        if (hospitalDays > 7)
        {
            plans.Add("5. 评估出院指征");
        }

        return string.Join("\n", plans);
    }

    private string GenerateTreatmentSummary(Patient patient)
    {
        if (string.IsNullOrWhiteSpace(patient.TreatmentPlan))
        {
            return "住院期间予以对症支持治疗，病情稳定。";
        }

        return $"住院期间予以{patient.TreatmentPlan}，患者病情稳定，好转出院。";
    }

    private string GenerateDischargeSituation(Patient patient)
    {
        return $"{patient.Name}一般情况可，精神食欲正常，无特殊不适主诉。查体未见明显异常。各项检查结果回报后进一步明确诊断及治疗。";
    }

    private string GenerateDischargeOrders(Patient patient)
    {
        int hospitalDays = patient.DischargeDate.HasValue
            ? (int)(patient.DischargeDate.Value - patient.AdmissionDate).TotalDays
            : 0;
        return $@"1. 注意休息，合理饮食
2. 按时服药，定期复查
3. 不适随诊
4. 出院后{hospitalDays}天门诊复诊";
    }
}
