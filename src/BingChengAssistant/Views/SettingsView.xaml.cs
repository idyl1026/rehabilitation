using BingChengAssistant.Services;
using BingChengAssistant.ViewModels;

namespace BingChengAssistant.Views;

public partial class SettingsView : System.Windows.Window
{
    public SettingsView()
    {
        DataContext = new SettingsViewModel();
        InitializeComponent();
        AboutVersionText.Text = AppInfo.FullTitle;
        Title = $"设置 - {AppInfo.Title}";
    }

    private void ImportScales_Click(object sender, System.Windows.RoutedEventArgs e)
    {
        var dlg = new Microsoft.Win32.OpenFileDialog
        {
            Title = "选择量表Excel文件（格式：代码|名称|说明）",
            Filter = "Excel 文件 (*.xlsx)|*.xlsx",
        };
        if (dlg.ShowDialog() != true) return;

        var (ok, skip, error) = ImportService.ImportScales(dlg.FileName);
        if (!string.IsNullOrEmpty(error))
        {
            System.Windows.MessageBox.Show($"导入失败：{error}", "错误",
                System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
            return;
        }
        System.Windows.MessageBox.Show(
            $"导入完成！\n新增量表：{ok} 个\n跳过（重复或无名称）：{skip} 个\n\n新量表在「康复评估」窗口左侧列表中显示。", "导入成功");
    }

    private void ExportScaleTemplate_Click(object sender, System.Windows.RoutedEventArgs e)
    {
        var dlg = new Microsoft.Win32.SaveFileDialog
        {
            Title = "保存量表导入模板",
            FileName = "量表导入模板.xlsx",
            Filter = "Excel 文件 (*.xlsx)|*.xlsx",
        };
        if (dlg.ShowDialog() != true) return;

        try
        {
            var dir = System.IO.Path.GetDirectoryName(dlg.FileName)!;
            var path = ImportService.ExportScaleTemplate(dir);
            if (path != dlg.FileName && System.IO.File.Exists(path))
                System.IO.File.Move(path, dlg.FileName, overwrite: true);
            System.Windows.MessageBox.Show($"模板已保存：\n{dlg.FileName}", "完成");
        }
        catch (System.Exception ex)
        {
            System.Windows.MessageBox.Show($"保存失败：{ex.Message}", "错误");
        }
    }
}
