using System.Windows.Input;
using BingChengAssistant.Services;
using BingChengAssistant.ViewModels;

namespace BingChengAssistant.Views;

public partial class LoginView : System.Windows.Window
{
    public LoginView()
    {
        InitializeComponent();
        LoginTitleText.Text = AppInfo.Title;
        Title = $"{AppInfo.Title} — 医生登录";
        var vm = (LoginViewModel)DataContext;
        vm.OnLoginSuccess = () =>
        {
            var main = new MainWorkbenchView();
            main.Show();
            Close();
        };
        vm.OnAddDoctor = () => { };
    }

    private void LoginButton_Click(object sender, System.Windows.RoutedEventArgs e)
    {
        var vm = (LoginViewModel)DataContext;
        vm.Pin = PinBox.Password;
        vm.LoginCommand.Execute(null);
    }

    private void PinBox_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter) LoginButton_Click(sender, e);
    }

    private void AddDoctorButton_Click(object sender, System.Windows.RoutedEventArgs e)
    {
        var reg = new DoctorRegisterView();
        reg.ShowDialog();
        // 刷新医生列表
        var vm = (LoginViewModel)DataContext;
        vm.AddDoctorCommand.Execute(null);
    }
}
