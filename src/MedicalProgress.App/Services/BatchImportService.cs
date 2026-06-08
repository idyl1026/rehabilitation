using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Spreadsheet;
using Microsoft.EntityFrameworkCore;
using MedicalProgress.App.Data;
using MedicalProgress.App.Models;

namespace MedicalProgress.App.Services;

public class BatchImportService
{
    // ─────────────────────────────────────────────
    //  JSON 导入
    // ─────────────────────────────────────────────

    public async Task<BatchImportResult> ImportFromJsonAsync(string filePath, int subjectId, IProgress<ImportProgress>? progress = null)
    {
        var result = new BatchImportResult();
        try
        {
            var json = await File.ReadAllTextAsync(filePath, Encoding.UTF8);
            var items = JsonSerializer.Deserialize<List<TemplateJsonItem>>(json, JsonOptions);
            if (items == null || items.Count == 0)
            {
                result.ErrorMessage = "JSON 文件为空或格式不正确";
                return result;
            }

            result.TotalRecords = items.Count;
            using var context = new AppDbContext();
            var categoryCache = await context.DiseaseCategories
                .Where(c => c.SubjectId == subjectId)
                .ToDictionaryAsync(c => c.Name.Trim(), c => c.Id);

            for (int i = 0; i < items.Count; i++)
            {
                try
                {
                    var item = items[i];
                    if (string.IsNullOrWhiteSpace(item.Title) || string.IsNullOrWhiteSpace(item.Content))
                    {
                        result.FailedCount++;
                        result.FailedTitles.Add($"第 {i + 1} 条：标题或内容为空");
                        continue;
                    }

                    var categoryId = await GetOrCreateCategoryIdAsync(context, subjectId, item.Category, categoryCache);
                    var templateType = NormalizeTemplateType(item.Type);

                    context.KnowledgeTemplates.Add(new KnowledgeTemplate
                    {
                        SubjectId = subjectId,
                        CategoryId = categoryId,
                        Title = item.Title.Length > 100 ? item.Title[..100] : item.Title,
                        TemplateType = templateType,
                        Content = item.Content,
                        Summary = BuildSummary(item.Content),
                        Keywords = item.Keywords ?? string.Empty,
                        IsActive = true,
                        CreatedAt = DateTime.Now,
                        LastUsedAt = DateTime.Now
                    });

                    result.SuccessCount++;
                    result.ImportedTitles.Add(item.Title);

                    progress?.Report(new ImportProgress
                    {
                        ProcessedCount = i + 1,
                        TotalCount = items.Count,
                        Percentage = (int)((i + 1) * 100.0 / items.Count),
                        CurrentFile = item.Title
                    });
                }
                catch (Exception ex)
                {
                    result.FailedCount++;
                    result.FailedTitles.Add($"第 {i + 1} 条：{ex.Message}");
                }
            }

            await context.SaveChangesAsync();
        }
        catch (JsonException ex)
        {
            result.ErrorMessage = $"JSON 格式错误：{ex.Message}";
        }
        catch (Exception ex)
        {
            result.ErrorMessage = ex.Message;
        }
        return result;
    }

    // ─────────────────────────────────────────────
    //  CSV 导入（原有逻辑，稍作整理）
    // ─────────────────────────────────────────────

    public async Task<BatchImportResult> ImportFromCsvAsync(string filePath, int subjectId, IProgress<ImportProgress>? progress = null)
    {
        var result = new BatchImportResult();
        try
        {
            var lines = await File.ReadAllLinesAsync(filePath, Encoding.UTF8);
            if (lines.Length == 0)
            {
                result.ErrorMessage = "文件为空";
                return result;
            }

            var header = lines[0].Split(new[] { '\t', ',' }, StringSplitOptions.RemoveEmptyEntries);
            int titleIndex = -1, typeIndex = -1, categoryIndex = -1, keywordsIndex = -1, contentIndex = -1;

            for (int i = 0; i < header.Length; i++)
            {
                var h = header[i].ToLowerInvariant().Trim();
                if (h.Contains("标题") || h.Contains("title")) titleIndex = i;
                else if (h.Contains("类型") || h.Contains("type")) typeIndex = i;
                else if (h.Contains("分类") || h.Contains("category")) categoryIndex = i;
                else if (h.Contains("关键词") || h.Contains("keywords")) keywordsIndex = i;
                else if (h.Contains("内容") || h.Contains("content")) contentIndex = i;
            }

            if (titleIndex == -1 || contentIndex == -1)
            {
                result.ErrorMessage = "文件缺少必要的列（标题、内容）";
                return result;
            }

            result.TotalRecords = lines.Length - 1;
            using var context = new AppDbContext();
            var categoryCache = await context.DiseaseCategories
                .Where(c => c.SubjectId == subjectId)
                .ToDictionaryAsync(c => c.Name.Trim(), c => c.Id);

            for (int i = 1; i < lines.Length; i++)
            {
                try
                {
                    var columns = ParseCsvLine(lines[i]);
                    if (columns.Count <= Math.Max(titleIndex, contentIndex)) continue;

                    var title = columns[titleIndex].Trim();
                    var content = columns[contentIndex].Trim();
                    if (string.IsNullOrWhiteSpace(title) || string.IsNullOrWhiteSpace(content)) continue;

                    var categoryName = categoryIndex != -1 && columns.Count > categoryIndex ? columns[categoryIndex].Trim() : "";
                    var keywords = keywordsIndex != -1 && columns.Count > keywordsIndex ? columns[keywordsIndex].Trim() : "";
                    var templateType = NormalizeTemplateType(typeIndex != -1 && columns.Count > typeIndex ? columns[typeIndex].Trim() : "");

                    var categoryId = await GetOrCreateCategoryIdAsync(context, subjectId, categoryName, categoryCache);

                    context.KnowledgeTemplates.Add(new KnowledgeTemplate
                    {
                        SubjectId = subjectId,
                        CategoryId = categoryId,
                        Title = title.Length > 100 ? title[..100] : title,
                        TemplateType = templateType,
                        Content = content,
                        Summary = BuildSummary(content),
                        Keywords = keywords,
                        IsActive = true,
                        CreatedAt = DateTime.Now,
                        LastUsedAt = DateTime.Now
                    });

                    result.SuccessCount++;
                    result.ImportedTitles.Add(title);

                    progress?.Report(new ImportProgress
                    {
                        ProcessedCount = i,
                        TotalCount = result.TotalRecords,
                        Percentage = (int)(i * 100.0 / result.TotalRecords),
                        CurrentFile = title
                    });
                }
                catch (Exception ex)
                {
                    result.FailedCount++;
                    result.FailedTitles.Add($"第 {i + 1} 行：{ex.Message}");
                }
            }

            await context.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            result.ErrorMessage = ex.Message;
        }
        return result;
    }

    // ─────────────────────────────────────────────
    //  示例文件生成
    // ─────────────────────────────────────────────

    public string GetSampleJsonContent()
    {
        var samples = new List<TemplateJsonItem>
        {
            new()
            {
                Title = "脑梗死恢复期首次病程记录",
                Type = "首次病程",
                Category = "神经系统",
                Keywords = "脑梗死,偏瘫,失语,康复评定,Brunnstrom",
                Content = """
2024-XX-XX  首次病程记录

一、病例特点：
1. 患者，男/女，XX岁，主因"一侧肢体无力XX天"入院。
2. 查体：神清，精神可，生命体征平稳。患侧上肢肌力X级，下肢肌力X级，Brunnstrom分期上肢X期/手X期/下肢X期，改良Ashworth肌张力X级，站立平衡X级。
3. 辅助检查：头颅MRI示脑梗死病灶（部位）。

二、诊断及鉴别诊断：
初步诊断：脑梗死恢复期

三、诊疗计划：
1. 完善相关检查，动态监测生命体征。
2. 完善康复评定（肌力、肌张力、Brunnstrom、ADL、吞咽、认知）。
3. 予以偏瘫肢体综合训练、作业治疗、言语训练（如有言语障碍）等综合康复治疗。
4. 动态评估疗效，调整康复方案。
"""
            },
            new()
            {
                Title = "脑梗死恢复期日常病程（好转）",
                Type = "日常病程",
                Category = "神经系统",
                Keywords = "脑梗死,偏瘫,肌力改善,功能好转",
                Content = """
2024-XX-XX  病程记录

患者今日一般情况可，精神食欲正常，诉患侧肢体活动较前好转，无新发头痛、头晕。

查体：生命体征平稳。患侧上肢肌力X→X+1级，下肢肌力X级，Brunnstrom分期上肢X期/手X期/下肢X期，站立平衡X级。

今日已完成：偏瘫肢体综合训练、作业治疗、平衡训练。

分析：患者经综合康复治疗后肌力有所改善，功能状态逐步恢复，继续当前康复计划。

下一步：继续康复治疗，复查相关化验，动态评估。
"""
            },
            new()
            {
                Title = "颈椎病首次病程记录",
                Type = "首次病程",
                Category = "骨关节",
                Keywords = "颈椎病,神经根型,颈肩痛,上肢麻木",
                Content = """
2024-XX-XX  首次病程记录

一、病例特点：
1. 患者，男/女，XX岁，主因"颈肩痛伴上肢麻木XX月"入院。
2. 查体：颈椎活动受限，压顶试验/臂丛牵拉试验（±），双上肢肌力、感觉检查。
3. 辅助检查：颈椎MRI示椎间盘突出，神经根受压。

二、诊断：神经根型颈椎病

三、诊疗计划：
1. 予以颈椎牵引、理疗、针灸等综合治疗。
2. 康复训练：颈椎功能训练、肌肉放松练习。
3. 健康教育：体位保护，避免长时间低头。
"""
            }
        };

        return JsonSerializer.Serialize(samples, new JsonSerializerOptions
        {
            WriteIndented = true,
            Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
        });
    }

    public string GetSampleCsvContent()
    {
        var sb = new StringBuilder();
        sb.AppendLine("标题,类型,分类,关键词,内容");
        sb.AppendLine("脑梗死日常病程,日常病程,神经系统,\"脑梗死,偏瘫\",\"患者今日一般情况可，精神食欲正常，继续康复治疗。\"");
        sb.AppendLine("颈椎病首次病程,首次病程,骨关节,\"颈椎病,上肢麻木\",\"患者因颈肩痛伴上肢麻木入院，予以综合治疗。\"");
        return sb.ToString();
    }

    // ─────────────────────────────────────────────
    //  XLSX 导入
    // ─────────────────────────────────────────────

    public async Task<BatchImportResult> ImportFromXlsxAsync(string filePath, int subjectId, IProgress<ImportProgress>? progress = null)
    {
        var result = new BatchImportResult();
        try
        {
            var rows = await Task.Run(() => ReadXlsxRows(filePath));
            if (rows.Count == 0) { result.ErrorMessage = "文件为空"; return result; }

            var header = rows[0];
            int titleIdx = -1, typeIdx = -1, categoryIdx = -1, keywordsIdx = -1, contentIdx = -1;
            for (int i = 0; i < header.Count; i++)
            {
                var h = header[i].ToLowerInvariant().Trim();
                if      (h.Contains("标题") || h.Contains("title"))                           titleIdx    = i;
                else if (h.Contains("类型") || h.Contains("type"))                            typeIdx     = i;
                else if (h.Contains("分类") || h.Contains("category") || h.Contains("疾病") || h.Contains("主题")) categoryIdx = i;
                else if (h.Contains("关键词") || h.Contains("keywords") || h.Contains("标签")) keywordsIdx = i;
                else if (h.Contains("内容") || h.Contains("content") || h.Contains("病程") || h.Contains("课程")) contentIdx  = i;
            }

            if (contentIdx == -1) { result.ErrorMessage = "文件缺少内容列（需包含【内容】或【病程】字样的列标题）"; return result; }

            result.TotalRecords = rows.Count - 1;
            using var context = new AppDbContext();
            var categoryCache = await context.DiseaseCategories
                .Where(c => c.SubjectId == subjectId)
                .ToDictionaryAsync(c => c.Name.Trim(), c => c.Id);

            for (int i = 1; i < rows.Count; i++)
            {
                try
                {
                    var cols = rows[i];
                    var content = contentIdx < cols.Count ? cols[contentIdx].Trim() : "";
                    // 跳过空行和说明/占位行（全部被【】包裹）
                    if (string.IsNullOrWhiteSpace(content)) continue;
                    if (content.StartsWith("【") && content.EndsWith("】")) continue;

                    var categoryName = categoryIdx  != -1 && categoryIdx  < cols.Count ? cols[categoryIdx].Trim()  : "";
                    var keywords     = keywordsIdx  != -1 && keywordsIdx  < cols.Count ? cols[keywordsIdx].Trim()  : "";
                    var typeName     = typeIdx      != -1 && typeIdx      < cols.Count ? cols[typeIdx].Trim()      : "";

                    string title;
                    if (titleIdx != -1 && titleIdx < cols.Count && !string.IsNullOrWhiteSpace(cols[titleIdx]))
                        title = cols[titleIdx].Trim();
                    else if (!string.IsNullOrWhiteSpace(categoryName))
                        title = categoryName.Length > 80 ? categoryName[..80] : categoryName;
                    else
                    {
                        var s = BuildSummary(content);
                        title = s.Length > 50 ? s[..50] : s;
                    }

                    var categoryId   = await GetOrCreateCategoryIdAsync(context, subjectId, categoryName, categoryCache);
                    var templateType = NormalizeTemplateType(typeName);

                    context.KnowledgeTemplates.Add(new KnowledgeTemplate
                    {
                        SubjectId    = subjectId,
                        CategoryId   = categoryId,
                        Title        = title.Length > 100 ? title[..100] : title,
                        TemplateType = templateType,
                        Content      = content,
                        Summary      = BuildSummary(content),
                        Keywords     = keywords,
                        IsActive     = true,
                        CreatedAt    = DateTime.Now,
                        LastUsedAt   = DateTime.Now
                    });

                    result.SuccessCount++;
                    result.ImportedTitles.Add(title);
                    progress?.Report(new ImportProgress
                    {
                        ProcessedCount = i,
                        TotalCount     = result.TotalRecords,
                        Percentage     = (int)(i * 100.0 / result.TotalRecords),
                        CurrentFile    = title
                    });
                }
                catch (Exception ex)
                {
                    result.FailedCount++;
                    result.FailedTitles.Add($"第 {i + 1} 行：{ex.Message}");
                }
            }

            await context.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            result.ErrorMessage = ex.Message;
        }
        return result;
    }

    // ─────────────────────────────────────────────
    //  XLSX 示例模板生成
    // ─────────────────────────────────────────────

    public void CreateSampleXlsx(string filePath)
    {
        var tableRows = new List<string[]>
        {
            new[] { "标题", "类型", "分类", "关键词", "内容" },
            new[] { "【必填，最多100字】", "【可选：首次病程/日常病程/出院小结/会诊记录/专科评定】", "【可选：疾病分类，如神经系统/骨关节】", "【可选：逗号分隔关键词】", "【必填：病程正文，支持多行】" },
            new[] { "脑梗死恢复期首次病程记录", "首次病程", "神经系统", "脑梗死,偏瘫,Brunnstrom,康复评定",
                "一、病例特点：\n患者，男/女，XX岁，主因一侧肢体无力XX天入院。\n查体：患侧上肢肌力X级，下肢肌力X级，Brunnstrom分期上肢X期/手X期/下肢X期。\n\n二、诊断：脑梗死恢复期\n\n三、诊疗计划：\n1. 完善康复评定（肌力、Brunnstrom、ADL、吞咽）。\n2. 予偏瘫肢体综合训练、作业治疗、言语训练等综合康复。\n3. 动态评估疗效，调整康复方案。" },
            new[] { "脑梗死日常病程（好转）", "日常病程", "神经系统", "脑梗死,肌力改善,功能好转",
                "患者今日一般情况可，精神食欲正常，诉患侧肢体活动较前好转，无新发头痛头晕。\n\n查体：生命体征平稳。患侧上肢肌力X→X+1级，下肢肌力X级，站立平衡X级。\n\n今日完成：偏瘫肢体综合训练、作业治疗、平衡训练。\n\n分析：患者经综合康复治疗后肌力有所改善，继续当前康复计划。\n\n计划：继续康复治疗，复查相关化验，动态评估。" },
            new[] { "颈椎病首次病程记录", "首次病程", "骨关节", "颈椎病,神经根型,颈肩痛,上肢麻木",
                "一、病例特点：\n患者，男/女，XX岁，主因颈肩痛伴上肢麻木XX月入院。\n查体：颈椎活动受限，压顶试验（±），臂丛牵拉试验（±）。\n辅助检查：颈椎MRI示椎间盘突出，神经根受压。\n\n二、诊断：神经根型颈椎病\n\n三、诊疗计划：\n1. 颈椎牵引、理疗、针灸等综合治疗。\n2. 康复训练：颈椎功能训练、肌肉放松。\n3. 健康教育：避免长时间低头，体位保护。" }
        };

        using var doc = SpreadsheetDocument.Create(filePath, SpreadsheetDocumentType.Workbook);
        var wbPart = doc.AddWorkbookPart();
        wbPart.Workbook = new Workbook();

        var wsPart = wbPart.AddNewPart<WorksheetPart>();
        var sheetData = new SheetData();
        wsPart.Worksheet = new Worksheet(sheetData);

        var sstPart = wbPart.AddNewPart<SharedStringTablePart>();
        var sst = new SharedStringTable();
        sstPart.SharedStringTable = sst;

        var strList = new List<string>();
        int GetOrAdd(string s)
        {
            var idx = strList.IndexOf(s);
            if (idx >= 0) return idx;
            idx = strList.Count;
            strList.Add(s);
            sst.AppendChild(new SharedStringItem(new Text(s) { Space = SpaceProcessingModeValues.Preserve }));
            return idx;
        }

        for (int r = 0; r < tableRows.Count; r++)
        {
            var xmlRow = new Row { RowIndex = (uint)(r + 1) };
            var cols = tableRows[r];
            for (int c = 0; c < cols.Length; c++)
            {
                xmlRow.Append(new Cell
                {
                    CellReference = XlsxColLetter(c) + (r + 1),
                    DataType = CellValues.SharedString,
                    CellValue = new CellValue(GetOrAdd(cols[c]).ToString())
                });
            }
            sheetData.Append(xmlRow);
        }

        var sheets = wbPart.Workbook.AppendChild(new Sheets());
        sheets.Append(new Sheet { Id = wbPart.GetIdOfPart(wsPart), SheetId = 1, Name = "病程模板" });
        wbPart.Workbook.Save();
    }

    // ─────────────────────────────────────────────
    //  私有辅助方法
    // ─────────────────────────────────────────────

    private static List<List<string>> ReadXlsxRows(string filePath)
    {
        var result = new List<List<string>>();
        using var doc = SpreadsheetDocument.Open(filePath, false);
        var wbPart = doc.WorkbookPart!;
        var sheet  = wbPart.Workbook.Descendants<Sheet>().First();
        var wsPart = (WorksheetPart)wbPart.GetPartById(sheet.Id!.Value!);
        var sheetData   = wsPart.Worksheet.GetFirstChild<SheetData>()!;
        var sharedStrs  = wbPart.SharedStringTablePart?.SharedStringTable
            .Elements<SharedStringItem>().Select(s => s.InnerText).ToArray();

        foreach (var row in sheetData.Elements<Row>())
        {
            var cells = row.Elements<Cell>().ToList();
            if (cells.Count == 0) continue;
            var maxCol  = cells.Max(c => XlsxColIndex(c.CellReference?.Value ?? "A"));
            var rowData = new string[maxCol + 1];
            Array.Fill(rowData, string.Empty);
            foreach (var cell in cells)
            {
                var col = XlsxColIndex(cell.CellReference?.Value ?? "A");
                rowData[col] = XlsxCellText(cell, sharedStrs);
            }
            result.Add(new List<string>(rowData));
        }
        return result;
    }

    private static int XlsxColIndex(string cellRef)
    {
        var letters = new string(cellRef.TakeWhile(char.IsLetter).ToArray()).ToUpperInvariant();
        int idx = 0;
        foreach (var ch in letters) idx = idx * 26 + (ch - 'A' + 1);
        return idx - 1;
    }

    private static string XlsxColLetter(int col)
    {
        var result = string.Empty;
        col++;
        while (col > 0) { col--; result = (char)('A' + col % 26) + result; col /= 26; }
        return result;
    }

    private static string XlsxCellText(Cell cell, string[]? sharedStrs)
    {
        if (cell.CellValue == null) return string.Empty;
        var raw = cell.CellValue.InnerText;
        if (cell.DataType?.Value == CellValues.SharedString && sharedStrs != null)
            if (int.TryParse(raw, out var idx) && idx >= 0 && idx < sharedStrs.Length)
                return sharedStrs[idx];
        return raw;
    }

    private static async Task<int> GetOrCreateCategoryIdAsync(
        AppDbContext context,
        int subjectId,
        string? categoryName,
        Dictionary<string, int> cache)
    {
        if (string.IsNullOrWhiteSpace(categoryName))
            return 0;

        var key = categoryName.Trim();
        if (cache.TryGetValue(key, out var id))
            return id;

        var newCategory = new DiseaseCategory
        {
            SubjectId = subjectId,
            Name = key,
            Code = "CAT" + cache.Count,
            CreatedAt = DateTime.Now
        };
        context.DiseaseCategories.Add(newCategory);
        await context.SaveChangesAsync();
        cache[key] = newCategory.Id;
        return newCategory.Id;
    }

    private static string NormalizeTemplateType(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
            return "日常病程";

        var valid = new HashSet<string> { "首次病程", "日常病程", "出院小结", "会诊记录", "检查分析", "专科评定", "抢救记录", "手术记录", "文献摘要" };
        return valid.Contains(raw.Trim()) ? raw.Trim() : "日常病程";
    }

    private static string BuildSummary(string content)
    {
        if (string.IsNullOrWhiteSpace(content)) return string.Empty;
        var clean = content.Replace("\r", " ").Replace("\n", " ").Trim();
        return clean.Length > 200 ? clean[..200] + "..." : clean;
    }

    private static List<string> ParseCsvLine(string line)
    {
        var result = new List<string>();
        var current = new StringBuilder();
        bool inQuotes = false;

        for (int i = 0; i < line.Length; i++)
        {
            char c = line[i];
            if (c == '"')
            {
                if (inQuotes && i + 1 < line.Length && line[i + 1] == '"')
                {
                    current.Append('"');
                    i++;
                }
                else
                {
                    inQuotes = !inQuotes;
                }
            }
            else if ((c == ',' || c == '\t') && !inQuotes)
            {
                result.Add(current.ToString());
                current.Clear();
            }
            else
            {
                current.Append(c);
            }
        }
        result.Add(current.ToString());
        return result;
    }

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
    };
}

// ─────────────────────────────────────────────
//  JSON 模板条目数据结构
// ─────────────────────────────────────────────

public class TemplateJsonItem
{
    [JsonPropertyName("title")]
    public string Title { get; set; } = string.Empty;

    [JsonPropertyName("type")]
    public string Type { get; set; } = "日常病程";

    [JsonPropertyName("category")]
    public string? Category { get; set; }

    [JsonPropertyName("keywords")]
    public string? Keywords { get; set; }

    [JsonPropertyName("content")]
    public string Content { get; set; } = string.Empty;
}

// ─────────────────────────────────────────────
//  结果与进度（原有，保留）
// ─────────────────────────────────────────────

public class BatchImportResult
{
    public string ErrorMessage { get; set; } = string.Empty;
    public int TotalRecords { get; set; }
    public int SuccessCount { get; set; }
    public int FailedCount { get; set; }
    public List<string> ImportedTitles { get; set; } = new();
    public List<string> FailedTitles { get; set; } = new();
    public bool Success => string.IsNullOrEmpty(ErrorMessage);
}
