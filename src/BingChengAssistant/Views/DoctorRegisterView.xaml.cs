using BingChengAssistant.ViewModels;

namespace BingChengAssistant.Views;

public partial class DoctorRegisterView : System.Windows.Window
{
    public DoctorRegisterView()
    {
        InitializeComponent();
        var vm = (DoctorRegisterViewModel)DataContext;
        vm.OnSuccess = () =>
        {
            var main = new MainWorkbenchView();
            main.Show();
            Close();
        };
    }

    private void RegisterButton_Click(object sender, System.Windows.RoutedEventArgs e)
    {
        // 把PasswordBox的值传给ViewModel（PasswordBox不支持MVVM绑定）
        var vm = (DoctorRegisterViewModel)DataContext;
        vm.Pin = PinBox.Password;
        vm.ConfirmPin = ConfirmPinBox.Password;
        vm.RegisterCommand.Execute(null);
    }
}
