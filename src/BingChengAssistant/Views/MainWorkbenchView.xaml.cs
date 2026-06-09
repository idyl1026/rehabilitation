using System.Diagnostics;
using System.IO;
using System.Windows.Controls;
using BingChengAssistant.Data;
using BingChengAssistant.Models;
using BingChengAssistant.Services;
using BingChengAssistant.ViewModels;

namespace BingChengAssistant.Views;

public partial class MainWorkbenchView : System.Windows.Window
{
    private readonly MainWorkbenchViewModel _vm;

    public MainWorkbenchView()
    {
        InitializeComponent();
        _vm = (MainWorkbenchViewModel)DataContext;

        _vm.OpenNewPatient = () =>
        {
            var dlg = new PatientEditView();
            dlg.Owner = this;
            if (dlg.ShowDialog() == true) _vm.LoadAdmissions();
        };

        _vm.OpenEditPatient = (adm) =>
        {
            var dlg = new PatientEditView(adm);
            dlg.Owner = this;
            if (dlg.ShowDialog() == true) _vm.LoadAdmissions();
        };

        _vm.OpenNewNote = (adm) =>
        {
            try
            {
                var dlg = new ProgressNoteEditView(adm) { Owner = this };
                if (dlg.ShowDialog() == true) _vm.LoadAdmissions();
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"打开病程记录窗口失败：{ex.Message}", "错误");
                BingChengAssistant.Services.LogService.Error("打开ProgressNoteEditView失败", ex);
            }
        };

        _vm.OpenRehab = (adm) =>
        {
            try
            {
                var dlg = new RehabAssessmentView(adm) { Owner = this };
                if (dlg.ShowDialog() == true) _vm.LoadAdmissions();
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"打开康复评估窗口失败：{ex.Message}", "错误");
                BingChengAssistant.Services.LogService.Error("打开RehabAssessmentView失败", ex);
            }
        };

        _vm.OpenDischarge = (adm) =>
        {
            try
            {
                var dlg = new DischargeArchiveView(adm) { Owner = this };
                if (dlg.ShowDialog() == true) _vm.LoadAdmissions();
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"打开出院归档窗口失败：{ex.Message}", "错误");
                BingChengAssistant.Services.LogService.Error("打开DischargeArchiveView失败", ex);
            }
        };

        _vm.OpenWordFile = (adm) =>
        {
            var wordRepo = new WordDocRepository();
            var doc = wordRepo.GetByAdmission(adm.Id);
            if (doc != null && File.Exists(doc.FilePath))
                Process.Start(new ProcessStartInfo(doc.FilePath) { UseShellExecute = true });
            else
                System.Windows.MessageBox.Show("Word文档不存在", "提示");
        };

        _vm.OpenPatientDir = (adm) =>
        {
            var dir = Path.GetDirectoryName(new WordDocRepository().GetByAdmission(adm.Id)?.FilePath ?? "");
            if (Directory.Exists(dir))
                Process.Start(new ProcessStartInfo(dir) { UseShellExecute = true });
        };

        _vm.ConfirmDeleteAdmission = (adm) =>
        {
            var result = System.Windows.MessageBox.Show(
                $"确认删除患者【{adm.Patient?.Name}】的全部住院记录？\n\n此操作不可恢复，将同时删除病程记录、康复评估等所有数据。",
                "删除确认", System.Windows.MessageBoxButton.YesNo,
                System.Windows.MessageBoxImage.Warning);
            if (result == System.Windows.MessageBoxResult.Yes)
            {
                new PatientRepository().DeleteAdmission(adm.Id);
                _vm.SelectedAdmission = null;
                _vm.LoadAdmissions();
            }
        };
    }

    private void StatusFilter_Checked(object sender, System.Windows.RoutedEventArgs e)
    {
        if (sender is System.Windows.Controls.RadioButton rb && _vm != null)
            _vm.StatusFilter = rb.Tag?.ToString() ?? "";
    }

    private void RefreshButton_Click(object sender, System.Windows.RoutedEventArgs e)
    {
        _vm.LoadAdmissions();
    }

    private void ImportLegacyButton_Click(object sender, System.Windows.RoutedEventArgs e)
    {
        if (!LegacyMigrationService.LegacyDbExists())
        {
            System.Windows.MessageBox.Show(
                $"未找到旧版数据库，请确认是否安装过旧版软件。\n\n预期路径：\n{LegacyMigrationService.LegacyDbPath}",
                "未找到旧版数据", System.Windows.MessageBoxButton.OK,
                System.Windows.MessageBoxImage.Information);
            return;
        }

        var confirm = System.Windows.MessageBox.Show(
            $"检测到旧版数据库，将把旧版患者和病程记录导入当前医生账号。\n\n已存在的患者（姓名+入院日期相同）会自动跳过，不会重复导入。\n\n是否继续？",
            "导入旧版数据", System.Windows.MessageBoxButton.YesNo,
            System.Windows.MessageBoxImage.Question);

        if (confirm != System.Windows.MessageBoxResult.Yes) return;

        var doctorId = AppContextService.CurrentDoctor?.Id ?? 0;
        if (doctorId == 0)
        {
            System.Windows.MessageBox.Show("请先登录医生账号再导入。", "提示");
            return;
        }

        var (patients, notes, error) = LegacyMigrationService.Migrate(doctorId);

        if (!string.IsNullOrEmpty(error))
        {
            System.Windows.MessageBox.Show($"导入失败：{error}", "错误",
                System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
            return;
        }

        System.Windows.MessageBox.Show(
            $"导入完成！\n共导入患者：{patients} 人\n病程记录：{notes} 条",
            "导入成功", System.Windows.MessageBoxButton.OK,
            System.Windows.MessageBoxImage.Information);

        _vm.LoadAdmissions();
    }
}
