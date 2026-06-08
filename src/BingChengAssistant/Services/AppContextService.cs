using BingChengAssistant.Models;

namespace BingChengAssistant.Services;

public static class AppContextService
{
    public static Doctor? CurrentDoctor { get; set; }

    public static bool IsLoggedIn => CurrentDoctor != null;

    public static string DoctorDirPath => CurrentDoctor == null ? ""
        : Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "doctors", CurrentDoctor.FolderName);
}
