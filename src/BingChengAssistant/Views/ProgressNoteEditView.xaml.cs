using BingChengAssistant.Models;
using BingChengAssistant.ViewModels;

namespace BingChengAssistant.Views;

public partial class ProgressNoteEditView : System.Windows.Window
{
    private readonly ProgressNoteEditViewModel _vm;

    public ProgressNoteEditView(Admission adm)
    {
        _vm = new ProgressNoteEditViewModel();
        DataContext = _vm;
        InitializeComponent();
        _vm.Admission = adm;
        _vm.OnSaved = () => { DialogResult = true; Close(); };
    }

    private void SaveButton_Click(object sender, System.Windows.RoutedEventArgs e)
    {
        _vm.SaveCommand.Execute(null);
    }
}
