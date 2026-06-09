using BingChengAssistant.Models;
using BingChengAssistant.ViewModels;

namespace BingChengAssistant.Views;

public partial class PatientEditView : System.Windows.Window
{
    private readonly PatientEditViewModel _vm;

    public PatientEditView(Admission? adm = null)
    {
        _vm = new PatientEditViewModel();
        DataContext = _vm;
        InitializeComponent();
        if (adm != null) _vm.LoadAdmission(adm);
        _vm.OnSuccess = () => { DialogResult = true; Close(); };
    }

    private void SaveButton_Click(object sender, System.Windows.RoutedEventArgs e)
    {
        _vm.SaveCommand.Execute(null);
    }
}
