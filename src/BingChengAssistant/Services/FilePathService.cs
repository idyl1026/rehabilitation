using System.IO;

namespace BingChengAssistant.Services;

public static class FilePathService
{
    private static readonly char[] InvalidChars = Path.GetInvalidFileNameChars();

    public static string SanitizeFileName(string name)
    {
        foreach (var c in InvalidChars)
            name = name.Replace(c, '_');
        return name.Trim();
    }

    public static string GetPatientWordPath(string doctorFolder, DateTime admissionDate, string patientName, string admissionNo = "")
    {
        var baseDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "doctors", doctorFolder, "patients");
        Directory.CreateDirectory(baseDir);
        var safeName = SanitizeFileName(patientName);
        var dateStr = admissionDate.ToString("yyyy-MM-dd");
        var fileName = $"{dateStr}-{safeName}.docx";
        var fullPath = Path.Combine(baseDir, fileName);
        if (File.Exists(fullPath) && !string.IsNullOrEmpty(admissionNo))
            fileName = $"{dateStr}-{safeName}-{SanitizeFileName(admissionNo)}.docx";
        return Path.Combine(baseDir, fileName);
    }
}
