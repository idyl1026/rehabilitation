using System.IO;
namespace BingChengAssistant.Data;

public static class DirectoryInitializer
{
    public static string BaseDir => AppDomain.CurrentDomain.BaseDirectory;

    public static void EnsureDirectories()
    {
        string[] dirs = {
            "data", "data/backup", "data/logs",
            "doctors", "doctors/shared", "doctors/shared/templates",
            "doctors/shared/knowledge_base", "doctors/shared/rehab_scales",
            "templates", "templates/word", "templates/note",
            "import", "config"
        };
        foreach (var d in dirs)
            Directory.CreateDirectory(Path.Combine(BaseDir, d));

        // settings.json
        var settingsPath = Path.Combine(BaseDir, "config", "settings.json");
        if (!File.Exists(settingsPath))
            File.WriteAllText(settingsPath, """
{
  "AppVersion": "1.2.0",
  "AutoLogin": false,
  "DefaultDoctorId": 0,
  "Theme": "Medical",
  "Language": "zh-CN"
}
""");
    }

    public static string DoctorDir(string doctorFolder)
    {
        var dir = Path.Combine(BaseDir, "doctors", doctorFolder);
        Directory.CreateDirectory(Path.Combine(dir, "patients"));
        Directory.CreateDirectory(Path.Combine(dir, "attachments"));
        Directory.CreateDirectory(Path.Combine(dir, "exports"));
        return dir;
    }
}
