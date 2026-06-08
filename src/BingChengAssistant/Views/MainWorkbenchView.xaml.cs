using System.Diagnostics;
using System.IO;
using System.Windows.Controls;
using BingChengAssistant.Data;
using BingChengAssistant.Models;
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
            var dlg = new ProgressNoteEditView(adm);
            dlg.Owner = this;
            if (dlg.ShowDialog() == true) _vm.LoadAdmissions();
        };

        _vm.OpenRehab = (adm) =>
        {
            var dlg = new RehabAssessmentView(adm);
            dlg.Owner = this;
            if (dlg.ShowDialog() == true) _vm.LoadAdmissions();
        };

        _vm.OpenDischarge = (adm) =>
        {
            var dlg = new DischargeArchiveView(adm);
            dlg.Owner = this;
            if (dlg.ShowDialog() == true) _vm.LoadAdmissions();
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
    }

    private void StatusFilter_Checked(object sender, System.Windows.RoutedEventArgs e)
    {
        if (sender is System.Windows.Controls.RadioButton rb && _vm != null)
            _vm.StatusFilter = rb.Tag?.ToString() ?? "";
    }
}
