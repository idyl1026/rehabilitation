using System.IO;
using System.Text;
using System.Xml.Linq;

namespace MedicalProgress.App.Services;

public static class DocxHelper
{
    public static async Task<string> ReadDocxFileAsync(string filePath)
    {
        if (!File.Exists(filePath))
            throw new FileNotFoundException("文件不存在", filePath);

        if (!filePath.EndsWith(".docx", StringComparison.OrdinalIgnoreCase))
            throw new ArgumentException("文件不是.docx格式");

        try
        {
            return await Task.Run(() => ExtractTextFromDocx(filePath));
        }
        catch (Exception ex)
        {
            throw new Exception($"读取Word文档失败：{ex.Message}", ex);
        }
    }

    private static string ExtractTextFromDocx(string filePath)
    {
        using var archive = System.IO.Compression.ZipFile.OpenRead(filePath);
        var documentXml = archive.GetEntry("word/document.xml");

        if (documentXml == null)
            throw new Exception("无法读取文档内容");

        using var stream = documentXml.Open();
        using var reader = new StreamReader(stream, Encoding.UTF8);
        var xmlContent = reader.ReadToEnd();

        var doc = XDocument.Parse(xmlContent);
        XNamespace w = "http://schemas.openxmlformats.org/wordprocessingml/2006/main";

        var paragraphs = doc.Descendants(w + "p")
            .Select(p => string.Join("", p.Descendants(w + "t").Select(t => t.Value)))
            .Where(text => !string.IsNullOrWhiteSpace(text))
            .ToList();

        return string.Join("\n", paragraphs);
    }

    public static bool IsValidDocxFile(string filePath)
    {
        try
        {
            if (!File.Exists(filePath))
                return false;

            if (!filePath.EndsWith(".docx", StringComparison.OrdinalIgnoreCase))
                return false;

            using var archive = System.IO.Compression.ZipFile.OpenRead(filePath);
            return archive.GetEntry("word/document.xml") != null;
        }
        catch
        {
            return false;
        }
    }

    public static List<string> GetDocxFiles(string folderPath, bool includeSubfolders = true)
    {
        if (!Directory.Exists(folderPath))
            return new List<string>();

        var searchOption = includeSubfolders ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly;

        return Directory.GetFiles(folderPath, "*.docx", searchOption).ToList();
    }

    public static List<string> GetAllDocumentFiles(string folderPath, bool includeSubfolders = true)
    {
        if (!Directory.Exists(folderPath))
            return new List<string>();

        var searchOption = includeSubfolders ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly;
        var files = new List<string>();

        files.AddRange(Directory.GetFiles(folderPath, "*.txt", searchOption));
        files.AddRange(Directory.GetFiles(folderPath, "*.docx", searchOption));

        return files.OrderBy(f => f).ToList();
    }
}
