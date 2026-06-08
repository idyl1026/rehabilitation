using System.Text;
using System.Text.RegularExpressions;
using MedicalProgress.App.Models;

namespace MedicalProgress.App.Services;

public class ClinicalChangeAnalyzerService
{
    public List<AnalysisSuggestion> Analyze(
        ParsedClinicalDocument clinicalDocument,
        IEnumerable<StructuredExamResult> examResults,
        string normalizedText)
    {
        var suggestions = new List<AnalysisSuggestion>();
        var results = examResults.ToList();
        var fullText = $"{normalizedText}\n{clinicalDocument.PhysicalExam}\n{clinicalDocument.AuxiliaryExam}";

        AddLabSuggestions(suggestions, results);
        AddImagingSuggestions(suggestions, results, fullText);
        AddRehabSuggestions(suggestions, fullText);
        AddGeneralClinicalSuggestions(suggestions, clinicalDocument, fullText);

        return suggestions
            .GroupBy(s => $"{s.Category}|{s.Tag}|{s.AnalysisText}")
            .Select(g => g.First())
            .ToList();
    }

    public string BuildAnalysisDraft(IEnumerable<AnalysisSuggestion> suggestions)
    {
        var ordered = suggestions
            .OrderByDescending(s => RiskWeight(s.RiskLevel))
            .ThenBy(s => s.Category)
            .ToList();

        if (ordered.Count == 0)
            return "患者目前情况：本次资料暂未识别到明确异常变化，请结合症状、查体及检查结果进一步评估。";

        var builder = new StringBuilder();
        builder.Append("患者目前情况：");

        foreach (var suggestion in ordered)
        {
            builder.Append(suggestion.AnalysisText.TrimEnd('。', '；', ';'));
            builder.Append("。");
        }

        var recommendations = ordered
            .Select(s => s.Recommendation)
            .Where(r => !string.IsNullOrWhiteSpace(r))
            .Distinct()
            .ToList();

        if (recommendations.Count > 0)
        {
            builder.Append("建议");
            builder.Append(string.Join("；", recommendations.Select(r => r.TrimEnd('。', '；', ';'))));
            builder.Append("。");
        }

        return builder.ToString();
    }

    private static void AddLabSuggestions(List<AnalysisSuggestion> suggestions, List<StructuredExamResult> results)
    {
        foreach (var result in results)
        {
            var item = result.ItemName;
            var value = ParseDecimal(result.ResultValue);
            var flag = result.AbnormalFlag;

            if (IsItem(item, "HGB", "血红蛋白") && (flag == "Low" || value is > 0 and < 130))
            {
                suggestions.Add(new AnalysisSuggestion
                {
                    Category = "Lab",
                    Tag = "HemoglobinLow",
                    RiskLevel = "Notice",
                    Evidence = result.RawLine,
                    AnalysisText = "血红蛋白偏低，提示轻度贫血或营养状态需关注",
                    Recommendation = "加强营养支持并动态复查血常规"
                });
            }

            if (IsItem(item, "RBC", "红细胞") && flag == "Low")
            {
                suggestions.Add(new AnalysisSuggestion
                {
                    Category = "Lab",
                    Tag = "RedCellLow",
                    RiskLevel = "Notice",
                    Evidence = result.RawLine,
                    AnalysisText = "红细胞计数偏低，与贫血或营养状态异常可能相关",
                    Recommendation = "结合血红蛋白、症状及营养状态综合判断"
                });
            }

            if (IsItem(item, "钾", "K") && (flag == "Low" || value is > 0 and < 3.5m))
            {
                suggestions.Add(new AnalysisSuggestion
                {
                    Category = "Lab",
                    Tag = "PotassiumLow",
                    RiskLevel = "Warning",
                    Evidence = result.RawLine,
                    AnalysisText = "血钾偏低，需关注低钾相关乏力、心律失常等风险",
                    Recommendation = "根据医嘱补钾或饮食补钾，并复查电解质"
                });
            }

            if ((item.Contains("D-二聚体", StringComparison.OrdinalIgnoreCase) || item.Contains("D二聚体", StringComparison.OrdinalIgnoreCase)) &&
                (flag == "High" || value > 0.5m))
            {
                suggestions.Add(new AnalysisSuggestion
                {
                    Category = "Lab",
                    Tag = "DDimerHigh",
                    RiskLevel = "Warning",
                    Evidence = result.RawLine,
                    AnalysisText = "D-二聚体升高，需结合肢体肿胀、活动量及血管超声评估血栓风险",
                    Recommendation = "注意观察下肢肿胀疼痛，必要时完善血管超声或复查凝血指标"
                });
            }

            if (IsItem(item, "CRP", "C反应蛋白") && (flag == "High" || value > 8m))
            {
                suggestions.Add(new AnalysisSuggestion
                {
                    Category = "Lab",
                    Tag = "CrpHigh",
                    RiskLevel = "Warning",
                    Evidence = result.RawLine,
                    AnalysisText = "C反应蛋白升高，需结合发热、咳嗽、尿路症状等判断感染或炎症风险",
                    Recommendation = "动态观察体温及感染相关症状，必要时复查炎症指标"
                });
            }
        }
    }

    private static void AddImagingSuggestions(List<AnalysisSuggestion> suggestions, List<StructuredExamResult> results, string fullText)
    {
        var imagingText = string.Join("\n", results
            .Where(r => r.ExamType is "Imaging" or "Unknown")
            .Select(r => $"{r.ReportName} {r.Conclusion} {r.RawLine}"));

        var text = $"{fullText}\n{imagingText}";

        if (ContainsAny(text, "MRI", "CT", "头颅") && ContainsAny(text, "脑梗", "软化灶", "缺血性脑白质"))
        {
            suggestions.Add(new AnalysisSuggestion
            {
                Category = "Imaging",
                Tag = "OldStrokeLesion",
                RiskLevel = "Info",
                Evidence = "头颅影像提示脑梗死、软化灶或缺血性改变",
                AnalysisText = "颅脑影像与脑梗死恢复期及既往脑血管病变相符",
                Recommendation = "结合神经功能缺损情况继续康复评定和训练"
            });
        }

        if (ContainsAny(text, "未见新发", "无新发", "未见明确新发"))
        {
            suggestions.Add(new AnalysisSuggestion
            {
                Category = "Imaging",
                Tag = "NoNewLesion",
                RiskLevel = "Info",
                Evidence = "影像描述未见新发病灶",
                AnalysisText = "目前影像未提示明确新发急性病灶，支持继续当前康复治疗计划",
                Recommendation = "若症状突然加重，应及时复查影像并重新评估"
            });
        }
    }

    private static void AddRehabSuggestions(List<AnalysisSuggestion> suggestions, string text)
    {
        if (ContainsAny(text, "偏瘫", "肢体活动不利", "肌力"))
        {
            suggestions.Add(new AnalysisSuggestion
            {
                Category = "Rehab",
                Tag = "HemiplegiaFunction",
                RiskLevel = "Info",
                Evidence = "文本包含偏瘫、肢体活动不利或肌力评定",
                AnalysisText = "患者仍存在偏瘫相关肢体功能障碍，康复重点为肌力、控制能力及日常活动能力改善",
                Recommendation = "继续偏瘫肢体综合训练、作业治疗、手功能训练和平衡训练"
            });
        }

        if (Regex.IsMatch(text, @"踝背伸-踝跖屈[:：]?\s*[\d\-\+]+-?1-1级") || text.Contains("远端肌力较差"))
        {
            suggestions.Add(new AnalysisSuggestion
            {
                Category = "Rehab",
                Tag = "DistalWeakness",
                RiskLevel = "Notice",
                Evidence = "远端肌力或踝背伸/跖屈较弱",
                AnalysisText = "患侧远端肌力较差，影响步态控制、足下垂风险及精细运动恢复",
                Recommendation = "加强踝背伸、手功能及精细运动训练，必要时评估矫形或辅助具需求"
            });
        }

        if (text.Contains("Brunnstrom", StringComparison.OrdinalIgnoreCase))
        {
            suggestions.Add(new AnalysisSuggestion
            {
                Category = "Rehab",
                Tag = "BrunnstromStage",
                RiskLevel = "Info",
                Evidence = "文本包含 Brunnstrom 分期",
                AnalysisText = "已记录 Brunnstrom 分期，可用于判断偏瘫肢体运动恢复阶段并调整训练策略",
                Recommendation = "后续病程中建议连续记录分期变化，评价康复疗效"
            });
        }

        if (text.Contains("Ashworth", StringComparison.OrdinalIgnoreCase) || text.Contains("痉挛"))
        {
            suggestions.Add(new AnalysisSuggestion
            {
                Category = "Rehab",
                Tag = "Spasticity",
                RiskLevel = "Notice",
                Evidence = "文本包含 Ashworth 评分或痉挛描述",
                AnalysisText = "患侧肢体存在痉挛状态，可能影响关节活动、步态及主动运动控制",
                Recommendation = "注意牵伸、良肢位摆放、抗痉挛训练，并动态复评肌张力"
            });
        }

        if (text.Contains("站立平衡2级") || text.Contains("坐位平衡") || text.Contains("跌倒评分"))
        {
            suggestions.Add(new AnalysisSuggestion
            {
                Category = "Rehab",
                Tag = "BalanceRisk",
                RiskLevel = "Notice",
                Evidence = "文本包含平衡等级或跌倒评分",
                AnalysisText = "患者平衡能力受限，存在跌倒风险及步行安全问题",
                Recommendation = "加强坐站平衡、转移训练和步态训练，落实防跌倒宣教"
            });
        }

        if (ContainsAny(text, "饮水不呛", "饮水无呛咳"))
        {
            suggestions.Add(new AnalysisSuggestion
            {
                Category = "Rehab",
                Tag = "SwallowStable",
                RiskLevel = "Info",
                Evidence = "查体描述饮水不呛或无呛咳",
                AnalysisText = "当前资料未提示明显吞咽呛咳表现",
                Recommendation = "继续观察进食饮水情况，如出现呛咳需完善吞咽评估"
            });
        }
    }

    private static void AddGeneralClinicalSuggestions(List<AnalysisSuggestion> suggestions, ParsedClinicalDocument clinicalDocument, string text)
    {
        if (ContainsAny(text, "神清", "精神可", "生命体征平稳"))
        {
            suggestions.Add(new AnalysisSuggestion
            {
                Category = "General",
                Tag = "StableGeneralCondition",
                RiskLevel = "Info",
                Evidence = "查体提示神清、精神可或生命体征平稳",
                AnalysisText = "患者一般情况尚平稳，可在监测安全风险基础上继续治疗",
                Recommendation = "继续观察生命体征、症状变化及治疗耐受情况"
            });
        }

        if (!string.IsNullOrWhiteSpace(clinicalDocument.ChiefComplaint))
        {
            suggestions.Add(new AnalysisSuggestion
            {
                Category = "General",
                Tag = "ChiefComplaintSummary",
                RiskLevel = "Info",
                Evidence = clinicalDocument.ChiefComplaint,
                AnalysisText = $"本次主要问题为{clinicalDocument.ChiefComplaint}",
                Recommendation = string.Empty
            });
        }
    }

    private static bool IsItem(string item, params string[] names)
    {
        return names.Any(name => item.Equals(name, StringComparison.OrdinalIgnoreCase) || item.Contains(name, StringComparison.OrdinalIgnoreCase));
    }

    private static bool ContainsAny(string text, params string[] values)
    {
        return values.Any(value => text.Contains(value, StringComparison.OrdinalIgnoreCase));
    }

    private static decimal ParseDecimal(string value)
    {
        var match = Regex.Match(value, @"[<>]?(?<num>\d+(?:\.\d+)?)");
        return match.Success && decimal.TryParse(match.Groups["num"].Value, out var parsed)
            ? parsed
            : 0m;
    }

    private static int RiskWeight(string riskLevel)
    {
        return riskLevel switch
        {
            "Warning" => 3,
            "Notice" => 2,
            _ => 1
        };
    }
}
