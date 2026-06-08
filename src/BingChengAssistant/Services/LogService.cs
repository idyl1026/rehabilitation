namespace BingChengAssistant.Services;

public static class LogService
{
    private static string LogFile => Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "data", "logs", "app.log");

    public static void Info(string msg) => Write("INFO", msg);
    public static void Error(string msg, Exception? ex = null) => Write("ERROR", ex == null ? msg : $"{msg} | {ex.Message}");

    private static void Write(string level, string msg)
    {
        try
        {
            var line = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] [{level}] {msg}{Environment.NewLine}";
            File.AppendAllText(LogFile, line);
        }
        catch { /* 日志失败不抛异常 */ }
    }
}
