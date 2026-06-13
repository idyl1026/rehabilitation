using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
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

        // 编辑模式：把已有内容载入富文本编辑器
        if (!string.IsNullOrEmpty(_vm.Content))
            SetEditorText(_vm.Content);
    }

    // ===== 富文本编辑器辅助 =====
    private string GetEditorText()
    {
        var range = new TextRange(FullTextBox.Document.ContentStart, FullTextBox.Document.ContentEnd);
        return range.Text.TrimEnd('\r', '\n');
    }

    private void SetEditorText(string text)
    {
        FullTextBox.Document.Blocks.Clear();
        FullTextBox.Document.Blocks.Add(BuildParagraph(text, null));
    }

    private static Paragraph BuildParagraph(string text, Brush? brush)
    {
        var p = new Paragraph { Margin = new Thickness(0) };
        var lines = (text ?? "").Replace("\r", "").Split('\n');
        for (int i = 0; i < lines.Length; i++)
        {
            var run = new Run(lines[i]);
            if (brush != null) run.Foreground = brush;
            p.Inlines.Add(run);
            if (i < lines.Length - 1) p.Inlines.Add(new LineBreak());
        }
        return p;
    }

    private void AppendToEditor(string text, Brush? brush)
    {
        FullTextBox.Document.Blocks.Add(BuildParagraph(text.TrimStart('\r', '\n'), brush));
        FullTextBox.ScrollToEnd();
    }

    private void SyncEditorToVm() => _vm.Content = GetEditorText();

    private void FullTextBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        if (WordCountText != null)
            WordCountText.Text = $"已输入 {GetEditorText().Length} 字";
    }

    // ===== 一键整理 =====
    private void Compose_Click(object sender, RoutedEventArgs e)
    {
        _vm.RunCompose();
        SetEditorText(_vm.Content);
        FullTextTab.IsSelected = true;
    }

    // ===== 知识卡片插入（重复检测 + 强制插入标红）=====
    private void InsertKnowledge_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not Button btn || btn.Tag is not KnowledgeItem item) return;

        var current = GetEditorText();
        bool duplicate = !string.IsNullOrEmpty(current) &&
                         (current.Contains(item.Title) || current.Contains(item.Content));

        Brush? brush = null;
        if (duplicate)
        {
            var r = System.Windows.MessageBox.Show(
                $"该知识卡片【{item.Title}】已插入过。\n\n是否仍要重复插入？（重复内容将以红色标注）",
                "重复插入提醒", MessageBoxButton.YesNo, MessageBoxImage.Warning);
            if (r != MessageBoxResult.Yes) return;
            brush = Brushes.Red;   // 强制插入 → 标红
        }

        AppendToEditor(ProgressNoteEditViewModel.CardText(item), brush);
        SyncEditorToVm();
        _vm.RecordKnowledgeUsed(item.Id);
        FullTextTab.IsSelected = true;
    }

    // ===== 更换 / 移除 匹配的知识卡片 =====
    private void ReplaceCard_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.Tag is KnowledgeItem item)
        {
            _vm.ReplaceMatchedCard(item);
            SetEditorText(_vm.Content);
        }
    }

    private void RemoveCard_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.Tag is KnowledgeItem item)
        {
            _vm.RemoveMatchedCard(item);
            SetEditorText(_vm.Content);
        }
    }

    // ===== 量表评估 =====
    private void OpenAssessment_Click(object sender, RoutedEventArgs e)
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

    // ===== 底部操作（先把编辑器内容同步回VM）=====
    private void Copy_Click(object sender, RoutedEventArgs e)
    {
        SyncEditorToVm();
        _vm.CopyCommand.Execute(null);
    }

    private void Sync_Click(object sender, RoutedEventArgs e)
    {
        SyncEditorToVm();
        _vm.SyncWordCommand.Execute(null);
    }

    private void Save_Click(object sender, RoutedEventArgs e)
    {
        SyncEditorToVm();
        _vm.SaveCommand.Execute(null);
    }
}
