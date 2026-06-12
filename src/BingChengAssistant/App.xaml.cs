using System.Windows;
using BingChengAssistant.Data;
using BingChengAssistant.Services;

namespace BingChengAssistant;

public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        // 全局异常处理：避免无提示崩溃，显示错误信息并记录日志
        DispatcherUnhandledException += (s, ex) =>
        {
            // 递归展开内层异常，找到真正的根因
            var root = ex.Exception;
            while (root.InnerException != null) root = root.InnerException;

            LogService.Error("未处理异常", ex.Exception);
            System.Windows.MessageBox.Show(
                $"程序遇到意外错误：\n\n{root.GetType().Name}: {root.Message}\n\n错误已记录到日志，请联系开发者。",
                "错误", System.Windows.MessageBoxButton.OK,
                System.Windows.MessageBoxImage.Error);
            ex.Handled = true; // 不退出程序
        };

        AppDomain.CurrentDomain.UnhandledException += (s, ex) =>
        {
            if (ex.ExceptionObject is Exception exception)
                LogService.Error("严重未处理异常", exception);
        };

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
