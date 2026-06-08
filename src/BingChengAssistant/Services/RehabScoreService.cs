namespace BingChengAssistant.Services;

public static class RehabScoreService
{
    public static (string Interpretation, string Advice) InterpretVasNrs(int score)
    {
        return score switch
        {
            0 => ("无痛", "继续保持，无需特殊处理。"),
            <= 3 => ("轻度疼痛", "可采用物理治疗及适当活动，注意避免加重疼痛的动作。"),
            <= 6 => ("中度疼痛", "建议结合疼痛控制和功能训练调整运动强度，必要时药物辅助。"),
            _ => ("重度疼痛", "建议优先控制疼痛，暂缓高强度训练，评估疼痛原因。")
        };
    }

    public static (string Interpretation, string Advice) InterpretMmt(int grade)
    {
        return grade switch
        {
            0 => ("无肌肉收缩", "以被动活动和神经肌肉电刺激为主。"),
            1 => ("可触及肌肉收缩但无关节活动", "以神经肌肉电刺激和辅助主动训练为主。"),
            2 => ("去重力状态下可完成关节活动", "可进行去重力位主动训练。"),
            3 => ("抗重力可完成关节活动", "可进行抗重力主动训练，逐渐增加阻力。"),
            4 => ("可抗部分阻力", "以抗阻训练为主，逐渐增加训练强度。"),
            5 => ("正常肌力", "肌力正常，维持性训练为主。"),
            _ => ("", "")
        };
    }

    public static string BuildNoteText(string scaleName, string resultSummary, string interpretation, string advice)
    {
        return $"康复评估：\n患者今日完成{scaleName}评估，结果为{resultSummary}，提示{interpretation}。结合患者当前诊断及功能状态，建议{advice}";
    }
}
