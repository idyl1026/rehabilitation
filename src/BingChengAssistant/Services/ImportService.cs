using System.IO;
using BingChengAssistant.Data;
using BingChengAssistant.Models;
using ClosedXML.Excel;

namespace BingChengAssistant.Services;

/// <summary>
/// Excel 批量导入：知识库 / 评估量表
/// 知识库格式：标题 | 分类 | 标签 | 内容（第一行为表头）
/// 量表格式：代码 | 名称 | 说明（第一行为表头）
/// </summary>
public static class ImportService
{
    public static (int ok, int skip, string error) ImportKnowledge(string xlsxPath)
    {
        try
        {
            using var wb = new XLWorkbook(xlsxPath);
            var ws = wb.Worksheet(1);
            var repo = new KnowledgeRepository();
            var existing = repo.GetAll().Select(k => k.Title).ToHashSet();

            int ok = 0, skip = 0;
            foreach (var row in ws.RowsUsed().Skip(1))
            {
                var title = row.Cell(1).GetString().Trim();
                var category = row.Cell(2).GetString().Trim();
                var tags = row.Cell(3).GetString().Trim();
                var content = row.Cell(4).GetString().Trim();

                if (string.IsNullOrEmpty(title) || string.IsNullOrEmpty(content)) { skip++; continue; }
                if (existing.Contains(title)) { skip++; continue; }

                repo.Insert(new KnowledgeItem
                {
                    Title = title,
                    Category = string.IsNullOrEmpty(category) ? "通用" : category,
                    Tags = tags,
                    Content = content,
                });
                existing.Add(title);
                ok++;
            }
            OperationLogService.Log("导入知识库", $"成功{ok}条 跳过{skip}条");
            return (ok, skip, "");
        }
        catch (Exception ex)
        {
            LogService.Error("导入知识库失败", ex);
            return (0, 0, ex.Message);
        }
    }

    public static (int ok, int skip, string error) ImportScales(string xlsxPath)
    {
        try
        {
            using var wb = new XLWorkbook(xlsxPath);
            var ws = wb.Worksheet(1);

            using var c = DbConnectionFactory.Create();
            int ok = 0, skip = 0;
            foreach (var row in ws.RowsUsed().Skip(1))
            {
                var code = row.Cell(1).GetString().Trim();
                var name = row.Cell(2).GetString().Trim();
                var desc = row.Cell(3).GetString().Trim();
                var content = row.Cell(4).GetString().Trim();   // 第4列：完整量表内容（可选）

                if (string.IsNullOrEmpty(name)) { skip++; continue; }
                if (string.IsNullOrEmpty(code))
                    code = name.Length > 16 ? name[..16] : name;   // 无代码用名称替代

                using var cmd = c.CreateCommand();
                cmd.CommandText = "INSERT OR IGNORE INTO rehab_scale_dict (code, name, description, scale_type, content) VALUES (@code, @name, @desc, 'generic', @content)";
                cmd.Parameters.AddWithValue("@code", code);
                cmd.Parameters.AddWithValue("@name", name);
                cmd.Parameters.AddWithValue("@desc", desc);
                cmd.Parameters.AddWithValue("@content", content);
                if (cmd.ExecuteNonQuery() > 0) { ok++; }
                else
                {
                    // 已存在：若本次带内容而库里为空，回填完整内容
                    if (!string.IsNullOrEmpty(content))
                    {
                        using var upd = c.CreateCommand();
                        upd.CommandText = "UPDATE rehab_scale_dict SET content=@content WHERE code=@code AND (content IS NULL OR content='')";
                        upd.Parameters.AddWithValue("@content", content);
                        upd.Parameters.AddWithValue("@code", code);
                        if (upd.ExecuteNonQuery() > 0) { ok++; continue; }
                    }
                    skip++;
                }
            }
            OperationLogService.Log("导入量表", $"成功{ok}条 跳过{skip}条");
            return (ok, skip, "");
        }
        catch (Exception ex)
        {
            LogService.Error("导入量表失败", ex);
            return (0, 0, ex.Message);
        }
    }

    /// <summary>生成知识库导入标准模板</summary>
    public static string ExportKnowledgeTemplate(string dir)
    {
        using var wb = new XLWorkbook();
        var ws = wb.AddWorksheet("知识库");
        ws.Cell(1, 1).Value = "标题";
        ws.Cell(1, 2).Value = "分类";
        ws.Cell(1, 3).Value = "标签";
        ws.Cell(1, 4).Value = "内容";
        ws.Cell(2, 1).Value = "腰椎间盘突出症康复要点";
        ws.Cell(2, 2).Value = "骨科";
        ws.Cell(2, 3).Value = "腰突,腰痛,核心训练";
        ws.Cell(2, 4).Value = "急性期卧床休息不超过3天……（此行为示例，可删除）";
        ws.Row(1).Style.Font.Bold = true;
        ws.Columns().AdjustToContents();
        var path = Path.Combine(dir, "知识库导入模板.xlsx");
        wb.SaveAs(path);
        return path;
    }

    /// <summary>生成量表导入标准模板</summary>
    public static string ExportScaleTemplate(string dir)
    {
        using var wb = new XLWorkbook();
        var ws = wb.AddWorksheet("量表");
        ws.Cell(1, 1).Value = "代码";
        ws.Cell(1, 2).Value = "名称";
        ws.Cell(1, 3).Value = "说明";
        ws.Cell(1, 4).Value = "完整内容";
        ws.Cell(2, 1).Value = "BBS";
        ws.Cell(2, 2).Value = "Berg平衡量表";
        ws.Cell(2, 3).Value = "0-56分，<40分有跌倒风险（此行为示例，可删除）";
        ws.Cell(2, 4).Value = "1.由坐到站：独立完成4分…（粘贴完整量表条目与评分标准，评估时可在软件中查看）";
        ws.Row(1).Style.Font.Bold = true;
        ws.Columns().AdjustToContents();
        var path = Path.Combine(dir, "量表导入模板.xlsx");
        wb.SaveAs(path);
        return path;
    }
}
