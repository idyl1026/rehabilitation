using BingChengAssistant.ViewModels;

namespace BingChengAssistant.Views;

public partial class SettingsView : System.Windows.Window
{
    public SettingsView()
    {
        DataContext = new SettingsViewModel();
        InitializeComponent();
    }
}
