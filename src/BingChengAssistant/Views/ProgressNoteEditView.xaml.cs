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

    // 一键整理后自动切换到"病程全文"页查看结果
    private void Compose_Click(object sender, System.Windows.RoutedEventArgs e)
    {
        FullTextTab.IsSelected = true;
    }

    // 打开量表评估窗口，保存后把评估文本带回到结构化"评估结果"字段
    private void OpenAssessment_Click(object sender, System.Windows.RoutedEventArgs e)
    {
        if (_vm.Admission == null) return;
        try
        {
            var dlg = new RehabAssessmentView(_vm.Admission) { Owner = this };
            if (dlg.ShowDialog() == true && !string.IsNullOrWhiteSpace(dlg.LastNoteText))
                _vm.AppendAssessment(dlg.LastNoteText);
        }
        catch (System.Exception ex)
        {
            System.Windows.MessageBox.Show($"打开量表评估失败：{ex.Message}", "错误");
        }
    }
}
