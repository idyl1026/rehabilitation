using System.Text;
using System.Text.RegularExpressions;
using BingChengAssistant.Data;
using BingChengAssistant.Models;

namespace BingChengAssistant.Services;

/// <summary>
/// 病程结构化组装：从上次病程提取主诉/查体、规则化整理格式、按诊断匹配知识卡片。
/// 内网无AI，采用基于章节标记与关键词的规则化处理。
/// </summary>
public static class NoteComposeService
{
    // 章节别名 -> 标准名
    private static readonly (string std, string[] alias)[] SectionDefs =
    {
        ("主诉",     new[]{"主诉"}),
        ("现病史",   new[]{"现病史","病史"}),
        ("查体",     new[]{"查体","体格检查","专科检查","体征"}),
        ("辅助检查", new[]{"辅助检查","辅检","检查结果","辅助检查结果"}),
        ("康复评估", new[]{"康复评估","评估","评定结果","量表评估"}),
    };

    /// <summary>从一段病程文本中提取指定章节内容（支持【主诉】或 主诉： 两种写法）</summary>
    public static string ExtractSection(string content, string sectionStd)
    {
        if (string.IsNullOrWhiteSpace(content)) return "";
        var def = SectionDefs.FirstOrDefault(d => d.std == sectionStd);
        var aliases = def.alias ?? new[] { sectionStd };

        // 所有可能的章节起始词（用于界定本节结束）
        var allHeads = SectionDefs.SelectMany(d => d.alias)
            .Concat(new[] { "诊断", "诊疗计划", "处理", "分析", "下一步", "医师签名", "记录" })
            .Distinct().ToArray();

        foreach (var alias in aliases)
        {
            // 【主诉】xxx 或 主诉：xxx，直到下一个章节标记
            var pattern = $@"[【\[]?\s*{Regex.Escape(alias)}\s*[】\]]?\s*[:：]?\s*(?<body>.*?)(?=(\n\s*[【\[]?\s*({string.Join("|", allHeads.Select(Regex.Escape))})\s*[】\]]?\s*[:：])|$)";
            var m = Regex.Match(content, pattern, RegexOptions.Singleline);
            if (m.Success)
            {
                var body = m.Groups["body"].Value.Trim();
                if (body.Length > 0) return body;
            }
        }
        return "";
    }

    /// <summary>带入上次病程的主诉/现病史/查体（优先取最近一条有内容的）</summary>
    public static (string chief, string present, string exam) CarryForward(int admissionId)
    {
        var notes = new ProgressNoteRepository().GetByAdmission(admissionId);
        string chief = "", present = "", exam = "";
        foreach (var n in notes) // 已按时间倒序
        {
            if (chief == "") chief = ExtractSection(n.Content, "主诉");
            if (present == "") present = ExtractSection(n.Content, "现病史");
            if (exam == "") exam = ExtractSection(n.Content, "查体");
            if (chief != "" && present != "" && exam != "") break;
        }
        return (chief, present, exam);
    }

    /// <summary>按主要诊断匹配相关知识卡片</summary>
    public static List<KnowledgeItem> MatchKnowledge(string diagnosis, int limit = 5)
    {
        if (string.IsNullOrWhiteSpace(diagnosis)) return new();
        var repo = new KnowledgeRepository();
        var result = new List<KnowledgeItem>();
        var seen = new HashSet<int>();

        // 拆分诊断为关键词（去掉数字编号与标点）
        var keywords = Regex.Split(diagnosis, @"[\s,，;；、.。\d]+")
            .Where(k => k.Length >= 2).Distinct().ToList();

        foreach (var kw in keywords)
        {
            foreach (var item in repo.Search(kw))
            {
                if (seen.Add(item.Id)) result.Add(item);
                if (result.Count >= limit) return result;
            }
        }
        return result;
    }

    /// <summary>规则化组装日常病程全文</summary>
    public static string Compose(
        Admission adm, Doctor? doctor, string noteType,
        string chief, string present, string exam, string auxExam, string assessment,
        IEnumerable<KnowledgeItem> knowledge)
    {
        var sb = new StringBuilder();
        var p = adm.Patient;
        int day = (int)((DateTime.Today - adm.AdmissionDate).TotalDays) + 1;

        sb.AppendLine($"【{noteType}记录】");
        sb.AppendLine($"记录日期：{DateTime.Now:yyyy-MM-dd}");
        sb.AppendLine();
        sb.AppendLine($"患者{p?.Name}，{p?.Gender}，{p?.Age}岁，入院第{day}天。诊断：{adm.MainDiagnosis}。");
        sb.AppendLine();

        void Sec(string title, string body)
        {
            sb.AppendLine($"【{title}】");
            sb.AppendLine(string.IsNullOrWhiteSpace(body) ? "" : body.Trim());
            sb.AppendLine();
        }

        Sec("主诉", chief);
        Sec("现病史", present);
        Sec("查体", exam);
        Sec("辅助检查", auxExam);
        if (!string.IsNullOrWhiteSpace(assessment)) Sec("康复评估", assessment);

        sb.AppendLine("【分析与诊疗计划】");
        var kList = knowledge?.ToList() ?? new();
        if (kList.Count > 0)
        {
            sb.AppendLine("（结合相关康复知识参考）");
            int i = 1;
            foreach (var k in kList)
            {
                sb.AppendLine($"{i}. {k.Title}：{Summarize(k.Content)}");
                i++;
            }
        }
        else
        {
            sb.AppendLine("1. ");
        }
        sb.AppendLine();
        sb.AppendLine($"医师签名：{doctor?.Name ?? AppContextService.CurrentDoctor?.Name ?? ""}");
        return sb.ToString();
    }

    private static string Summarize(string content, int max = 120)
    {
        content = Regex.Replace(content, @"\s+", " ").Trim();
        return content.Length <= max ? content : content[..max] + "…";
    }
}
