namespace MedicalProgress.App.Templates;

public static class ProgressTemplates
{
    public static string GetInitialProgressTemplate()
    {
        return @"【首次病程记录】
日期：{Date}
患者信息：{PatientInfo}

一、病例特点
{ChiefComplaintSection}

二、现病史
{HistorySection}

三、体格检查
{PhysicalExamSection}

四、辅助检查
{AuxiliaryExamSection}

五、初步诊断
{PreliminaryDiagnosis}

六、诊疗计划
{TreatmentPlanSection}";
    }

    public static string GetDailyProgressTemplate()
    {
        return @"【日常病程记录】
日期：{Date}
患者信息：{PatientInfo}

一、病情变化
{ConditionChanges}

二、治疗情况
{TreatmentSituation}

三、查体情况
{ExamSituation}

四、下一步计划
{NextPlan}";
    }

    public static string GetDischargeSummaryTemplate()
    {
        return @"【出院小结】
日期：{Date}
患者信息：{PatientInfo}
住院天数：{HospitalDays}天

一、入院情况
{AdmissionSituation}

二、诊疗经过
{TreatmentProcess}

三、出院情况
{DischargeSituation}

四、出院医嘱
{DischargeOrders}";
    }

    public static Dictionary<string, string> GetTemplatePlaceholders()
    {
        return new Dictionary<string, string>
        {
            { "{Date}", "记录日期" },
            { "{PatientInfo}", "患者基本信息" },
            { "{ChiefComplaintSection}", "主诉相关" },
            { "{HistorySection}", "现病史" },
            { "{PhysicalExamSection}", "体格检查" },
            { "{AuxiliaryExamSection}", "辅助检查" },
            { "{PreliminaryDiagnosis}", "初步诊断" },
            { "{TreatmentPlanSection}", "诊疗计划" },
            { "{ConditionChanges}", "病情变化" },
            { "{TreatmentSituation}", "治疗情况" },
            { "{ExamSituation}", "查体情况" },
            { "{NextPlan}", "下一步计划" },
            { "{AdmissionSituation}", "入院情况" },
            { "{TreatmentProcess}", "诊疗经过" },
            { "{DischargeSituation}", "出院情况" },
            { "{DischargeOrders}", "出院医嘱" },
            { "{HospitalDays}", "住院天数" }
        };
    }
}
