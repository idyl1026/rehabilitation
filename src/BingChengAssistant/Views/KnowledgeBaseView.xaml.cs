using BingChengAssistant.ViewModels;

namespace BingChengAssistant.Views;

public partial class KnowledgeBaseView : System.Windows.Window
{
    public KnowledgeBaseView()
    {
        var vm = new KnowledgeBaseViewModel();
        DataContext = vm;
        InitializeComponent();
    }
}
