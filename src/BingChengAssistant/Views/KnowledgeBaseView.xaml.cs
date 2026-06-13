using BingChengAssistant.Services;
using BingChengAssistant.ViewModels;

namespace BingChengAssistant.Views;

public partial class KnowledgeBaseView : System.Windows.Window
{
    private readonly KnowledgeBaseViewModel _vm;

    public KnowledgeBaseView()
    {
        _vm = new KnowledgeBaseViewModel();
        DataContext = _vm;
        InitializeComponent();
    }

    private void ImportExcel_Click(object sender, System.Windows.RoutedEventArgs e)
    {
        var dlg = new Microsoft.Win32.OpenFileDialog
        {
            Title = "选择知识库Excel文件（格式：标题|分类|标签|内容）",
            Filter = "Excel 文件 (*.xlsx)|*.xlsx",
        };
        if (dlg.ShowDialog() != true) return;

        var (ok, skip, error) = ImportService.ImportKnowledge(dlg.FileName);
        if (!string.IsNullOrEmpty(error))
        {
            System.Windows.MessageBox.Show($"导入失败：{error}", "错误",
                System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
            return;
        }
        System.Windows.MessageBox.Show(
            $"导入完成！\n新增：{ok} 条\n跳过（重复或不完整）：{skip} 条", "导入成功");
        _vm.SearchKeyword = "";   // 触发刷新
    }

    private void ClearAll_Click(object sender, System.Windows.RoutedEventArgs e)
    {
        var confirm = System.Windows.MessageBox.Show(
            "确认清空全部知识库条目？\n\n此操作将彻底删除所有知识卡片与引用历史，不可恢复。\n常用于重新导入前清理旧数据。",
            "清空知识库", System.Windows.MessageBoxButton.YesNo,
            System.Windows.MessageBoxImage.Warning);
        if (confirm != System.Windows.MessageBoxResult.Yes) return;

        try
        {
            int n = new BingChengAssistant.Data.KnowledgeRepository().ClearAll();
            _vm.SearchKeyword = "";   // 触发列表刷新
            System.Windows.MessageBox.Show($"已清空 {n} 条知识卡片。", "完成");
        }
        catch (System.Exception ex)
        {
            System.Windows.MessageBox.Show($"清空失败：{ex.Message}", "错误");
        }
    }

    private void ExportTemplate_Click(object sender, System.Windows.RoutedEventArgs e)
    {
        var dlg = new Microsoft.Win32.SaveFileDialog
        {
            Title = "保存知识库导入模板",
            FileName = "知识库导入模板.xlsx",
            Filter = "Excel 文件 (*.xlsx)|*.xlsx",
        };
        if (dlg.ShowDialog() != true) return;

        try
        {
            var dir = System.IO.Path.GetDirectoryName(dlg.FileName)!;
            var path = ImportService.ExportKnowledgeTemplate(dir);
            if (path != dlg.FileName && System.IO.File.Exists(path))
                System.IO.File.Move(path, dlg.FileName, overwrite: true);
            System.Windows.MessageBox.Show($"模板已保存：\n{dlg.FileName}\n\n按模板格式填写后，用「导入Excel」批量导入。", "完成");
        }
        catch (System.Exception ex)
        {
            System.Windows.MessageBox.Show($"保存失败：{ex.Message}", "错误");
        }
    }
}
