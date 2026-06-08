using System.IO.Compression;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml.Linq;

namespace MedicalProgress.App.Services;

public class TextExtractionService
{
    public async Task<string> ExtractTextAsync(string filePath)
    {
        if (!File.Exists(filePath))
            throw new FileNotFoundException("File not found.", filePath);

        var extension = Path.GetExtension(filePath).ToLowerInvariant();
        return extension switch
        {
            ".txt" => await ReadTextFileAsync(filePath),
            ".docx" => await Task.Run(() => ExtractDocx(filePath)),
            ".pdf" => await Task.Run(() => ExtractPdfBestEffort(filePath)),
            _ => throw new NotSupportedException($"Unsupported file type: {extension}")
        };
    }

    private static async Task<string> ReadTextFileAsync(string filePath)
    {
        var bytes = await File.ReadAllBytesAsync(filePath);
        var utf8 = new UTF8Encoding(false, true);

        try
        {
            return utf8.GetString(bytes);
        }
        catch
        {
            return Encoding.Default.GetString(bytes);
        }
    }

    private static string ExtractDocx(string filePath)
    {
        using var archive = ZipFile.OpenRead(filePath);
        var documentXml = archive.GetEntry("word/document.xml");
        if (documentXml == null)
            return string.Empty;

        using var stream = documentXml.Open();
        using var reader = new StreamReader(stream, Encoding.UTF8);
        var xmlContent = reader.ReadToEnd();

        var doc = XDocument.Parse(xmlContent);
        XNamespace w = "http://schemas.openxmlformats.org/wordprocessingml/2006/main";

        var paragraphs = doc.Descendants(w + "p")
            .Select(p => string.Join("", p.Descendants(w + "t").Select(t => t.Value)))
            .Where(text => !string.IsNullOrWhiteSpace(text));

        return string.Join(Environment.NewLine, paragraphs);
    }

    private static string ExtractPdfBestEffort(string filePath)
    {
        var bytes = File.ReadAllBytes(filePath);
        var raw = Encoding.Latin1.GetString(bytes);
        var matches = Regex.Matches(raw, @"\((?<text>(?:\\.|[^\\)])*)\)");
        var pieces = new List<string>();

        foreach (Match match in matches)
        {
            var text = match.Groups["text"].Value;
            text = text
                .Replace("\\(", "(")
                .Replace("\\)", ")")
                .Replace("\\n", "\n")
                .Replace("\\r", "\n")
                .Replace("\\t", " ");

            if (LooksReadable(text))
                pieces.Add(text);
        }

        return string.Join(Environment.NewLine, pieces);
    }

    private static bool LooksReadable(string text)
    {
        if (string.IsNullOrWhiteSpace(text) || text.Length < 2)
            return false;

        var printable = text.Count(ch => !char.IsControl(ch));
        return printable >= Math.Max(2, text.Length * 0.8);
    }
}
