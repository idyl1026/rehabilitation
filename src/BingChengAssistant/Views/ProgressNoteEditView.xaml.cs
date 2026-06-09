using BingChengAssistant.Models;
using BingChengAssistant.ViewModels;

namespace BingChengAssistant.Views;

public partial class ProgressNoteEditView : System.Windows.Window
{
    private readonly ProgressNoteEditViewModel _vm;

    public ProgressNoteEditView(Admission adm, ProgressNote? note = null)
    {
        _vm = new ProgressNoteEditViewModel();
        DataContext = _vm;
        InitializeComponent();
        _vm.Admission = adm;
        if (note != null) _vm.LoadNote(note);
        _vm.OnSaved = () => { DialogResult = true; Close(); };
    }

    private void SaveButton_Click(object sender, System.Windows.RoutedEventArgs e)
    {
        _vm.SaveCommand.Execute(null);
    }

    private void InsertKnowledge_Click(object sender, System.Windows.RoutedEventArgs e)
    {
        if (sender is System.Windows.Controls.Button btn && btn.Tag is BingChengAssistant.Models.KnowledgeItem item)
            _vm.InsertKnowledge(item);
    }
}
