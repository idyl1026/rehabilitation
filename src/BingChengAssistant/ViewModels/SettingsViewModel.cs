using System.Collections.ObjectModel;
using BingChengAssistant.Services;

namespace BingChengAssistant.ViewModels;

public class SettingsViewModel : BaseViewModel
{
    private string _statusMessage = "";
    private BackupFile? _selectedBackup;

    public ObservableCollection<BackupFile> Backups { get; } = new();
    public BackupFile? SelectedBackup { get => _selectedBackup; set => SetField(ref _selectedBackup, value); }
    public string StatusMessage { get => _statusMessage; set => SetField(ref _statusMessage, value); }
    public string DbPath => BingChengAssistant.Data.DbConnectionFactory.DbPath;
    public string AppVersion => "v1.2  内网单机版";

    private RelayCommand? _backupCommand, _restoreCommand, _refreshCommand;
    public RelayCommand BackupCommand => _backupCommand ??= new(DoBackup);
    public RelayCommand RestoreCommand => _restoreCommand ??= new(DoRestore);
    public RelayCommand RefreshCommand => _refreshCommand ??= new(LoadBackups);

    public SettingsViewModel() => LoadBackups();

    private void LoadBackups()
    {
        Backups.Clear();
        foreach (var b in BackupService.GetBackups()) Backups.Add(b);
        StatusMessage = "";
    }

    private void DoBackup()
    {
        var (ok, path, error) = BackupService.Backup();
        StatusMessage = ok ? $"✓ 备份成功：{System.IO.Path.GetFileName(path)}" : $"✗ 备份失败：{error}";
        LoadBackups();
    }

    private void DoRestore()
    {
        if (SelectedBackup == null) { StatusMessage = "请先选择要还原的备份文件"; return; }
        var (ok, error) = BackupService.Restore(SelectedBackup.FilePath);
        StatusMessage = ok ? "✓ 还原成功，请重启软件使数据生效" : $"✗ 还原失败：{error}";
    }
}
