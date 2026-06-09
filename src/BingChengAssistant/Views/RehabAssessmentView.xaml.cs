using BingChengAssistant.Models;
using BingChengAssistant.ViewModels;

namespace BingChengAssistant.Views;

public partial class RehabAssessmentView : System.Windows.Window
{
    private readonly RehabAssessmentViewModel _vm;

    public RehabAssessmentView(Admission adm)
    {
        _vm = new RehabAssessmentViewModel();
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
