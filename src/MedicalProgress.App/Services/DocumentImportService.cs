using MedicalProgress.App.Models;

namespace MedicalProgress.App.Services;

public class DocumentImportService
{
    private readonly TextExtractionService _textExtractionService;
    private readonly ClinicalTextNormalizeService _normalizeService;
    private readonly ExamResultParserService _parserService;
    private readonly ClinicalDocumentSectionParserService _sectionParserService;

    public DocumentImportService()
    {
        _textExtractionService = new TextExtractionService();
        _normalizeService = new ClinicalTextNormalizeService();
        _parserService = new ExamResultParserService();
        _sectionParserService = new ClinicalDocumentSectionParserService();
    }

    public ImportPreview ImportFromText(string text, int? patientId = null)
    {
        var normalized = _normalizeService.Normalize(text);
        var document = new ImportedDocument
        {
            PatientId = patientId,
            SourceType = "Paste",
            SourceFileName = "Clipboard",
            DocumentType = _parserService.DetectExamType(normalized),
            RawText = text,
            NormalizedText = normalized,
            ImportedAt = DateTime.Now
        };

        var results = _parserService.Parse(normalized, patientId);
        var clinicalDocument = _sectionParserService.Parse(normalized);
        return new ImportPreview(document, results, clinicalDocument);
    }

    public async Task<ImportPreview> ImportFromFileAsync(string filePath, int? patientId = null)
    {
        var rawText = await _textExtractionService.ExtractTextAsync(filePath);
        var normalized = _normalizeService.Normalize(rawText);
        var documentType = _parserService.DetectExamType(normalized);

        var document = new ImportedDocument
        {
            PatientId = patientId,
            SourceType = Path.GetExtension(filePath).TrimStart('.').ToUpperInvariant(),
            SourceFilePath = filePath,
            SourceFileName = Path.GetFileName(filePath),
            DocumentType = documentType,
            RawText = rawText,
            NormalizedText = normalized,
            ImportedAt = DateTime.Now
        };

        var results = _parserService.Parse(normalized, patientId);
        var clinicalDocument = _sectionParserService.Parse(normalized);
        return new ImportPreview(document, results, clinicalDocument);
    }
}

public record ImportPreview(
    ImportedDocument Document,
    List<StructuredExamResult> ExamResults,
    ParsedClinicalDocument ClinicalDocument);
