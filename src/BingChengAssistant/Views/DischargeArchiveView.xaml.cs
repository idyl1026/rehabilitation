using BingChengAssistant.Models;
using BingChengAssistant.ViewModels;

namespace BingChengAssistant.Views;

public partial class DischargeArchiveView : System.Windows.Window
{
    private readonly DischargeArchiveViewModel _vm;

    public DischargeArchiveView(Admission adm)
    {
        _vm = new DischargeArchiveViewModel();
        DataContext = _vm;
        InitializeComponent();
        _vm.Admission = adm;
        _vm.OnArchived = () => { DialogResult = true; Close(); };
    }

    private void ArchiveButton_Click(object sender, System.Windows.RoutedEventArgs e)
    {
        _vm.ArchiveCommand.Execute(null);
    }
}
