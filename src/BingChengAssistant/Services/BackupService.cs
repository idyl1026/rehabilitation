using System.IO;
using BingChengAssistant.Data;

namespace BingChengAssistant.Services;

public static class BackupService
{
    private static string BackupDir => Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "data", "backup");

    public static (bool ok, string path, string error) Backup()
    {
        try
        {
            Directory.CreateDirectory(BackupDir);
            var dest = Path.Combine(BackupDir, $"main_{DateTime.Now:yyyyMMdd_HHmmss}.db");
            File.Copy(DbConnectionFactory.DbPath, dest, overwrite: false);
            LogService.Info($"数据备份成功：{dest}");
            return (true, dest, "");
        }
        catch (Exception ex)
        {
            LogService.Error("备份失败", ex);
            return (false, "", ex.Message);
        }
    }

    public static List<BackupFile> GetBackups()
    {
        if (!Directory.Exists(BackupDir)) return new();
        return Directory.GetFiles(BackupDir, "main_*.db")
            .Select(f => new BackupFile
            {
                FileName = Path.GetFileName(f),
                FilePath = f,
                CreatedAt = File.GetCreationTime(f),
                SizeKb = (int)(new FileInfo(f).Length / 1024),
            })
            .OrderByDescending(f => f.CreatedAt)
            .ToList();
    }

    public static (bool ok, string error) Restore(string backupPath)
    {
        try
        {
            if (!File.Exists(backupPath)) return (false, "备份文件不存在");
            // 先备份当前数据库
            var safetyBackup = Path.Combine(BackupDir, $"before_restore_{DateTime.Now:yyyyMMdd_HHmmss}.db");
            File.Copy(DbConnectionFactory.DbPath, safetyBackup, overwrite: false);
            File.Copy(backupPath, DbConnectionFactory.DbPath, overwrite: true);
            LogService.Info($"数据还原成功，来源：{backupPath}");
            return (true, "");
        }
        catch (Exception ex)
        {
            LogService.Error("还原失败", ex);
            return (false, ex.Message);
        }
    }
}

public class BackupFile
{
    public string FileName { get; set; } = "";
    public string FilePath { get; set; } = "";
    public DateTime CreatedAt { get; set; }
    public int SizeKb { get; set; }
    public string Display => $"{FileName}  ({SizeKb} KB)  {CreatedAt:yyyy-MM-dd HH:mm}";
}
