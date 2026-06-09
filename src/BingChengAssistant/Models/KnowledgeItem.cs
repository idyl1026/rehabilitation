namespace BingChengAssistant.Models;

public class KnowledgeItem
{
    public int Id { get; set; }
    public string Title { get; set; } = "";
    public string Content { get; set; } = "";
    public string Category { get; set; } = "通用";   // 通用/康复/疼痛/运动
    public string Tags { get; set; } = "";            // 逗号分隔关键词
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.Now;
}
