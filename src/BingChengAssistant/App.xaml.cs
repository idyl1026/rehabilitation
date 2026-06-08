using System.Windows;
using BingChengAssistant.Data;
using BingChengAssistant.Services;

namespace BingChengAssistant;

public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        // 初始化目录结构
        DirectoryInitializer.EnsureDirectories();

        // 初始化数据库
        DatabaseInitializer.Initialize();

        // 初始化日志
        LogService.Info("病程助手 v1.2 启动");

        // 根据是否有医生决定显示哪个窗口
        var doctorRepo = new DoctorRepository();
        if (!doctorRepo.HasAnyDoctor())
        {
            var registerView = new Views.DoctorRegisterView();
            registerView.Show();
        }
        else
        {
            var loginView = new Views.LoginView();
            loginView.Show();
        }
    }
}
