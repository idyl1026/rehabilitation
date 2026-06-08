using BingChengAssistant.Models;
using BingChengAssistant.ViewModels;

namespace BingChengAssistant.Views;

public partial class DischargeArchiveView : System.Windows.Window
{
    private readonly DischargeArchiveViewModel _vm;

    public DischargeArchiveView(Admission adm)
    {
        InitializeComponent();
        _vm = (DischargeArchiveViewModel)DataContext;
        _vm.Admission = adm;
        _vm.OnArchived = () => { DialogResult = true; Close(); };
    }

    private void ArchiveButton_Click(object sender, System.Windows.RoutedEventArgs e)
    {
        _vm.ArchiveCommand.Execute(null);
    }
}
