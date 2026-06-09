using BingChengAssistant.ViewModels;

namespace BingChengAssistant.Views;

public partial class TemplateManageView : System.Windows.Window
{
    public TemplateManageView()
    {
        DataContext = new TemplateManageViewModel();
        InitializeComponent();
    }
}
