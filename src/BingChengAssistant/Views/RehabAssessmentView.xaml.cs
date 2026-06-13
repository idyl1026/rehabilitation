using BingChengAssistant.Models;
using BingChengAssistant.ViewModels;

namespace BingChengAssistant.Views;

public partial class RehabAssessmentView : System.Windows.Window
{
    private readonly RehabAssessmentViewModel _vm;

    /// <summary>保存后供调用方读取的评估文本</summary>
    public string LastNoteText { get; private set; } = "";

    public RehabAssessmentView(Admission adm)
    {
        _vm = new RehabAssessmentViewModel();
        DataContext = _vm;
        InitializeComponent();
        _vm.Admission = adm;
        _vm.OnSaved = () => { LastNoteText = _vm.NoteText; DialogResult = true; Close(); };
    }

    private void SaveButton_Click(object sender, System.Windows.RoutedEventArgs e)
    {
        _vm.SaveCommand.Execute(null);
    }
}
