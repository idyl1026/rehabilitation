using System.Reflection;

namespace BingChengAssistant.Services;

/// <summary>应用版本信息：从程序集读取，构建时由 GitHub Actions 用 tag 注入</summary>
public static class AppInfo
{
    public static string Version
    {
        get
        {
            // 优先取 InformationalVersion（workflow 注入完整版本，如 1.4.6）
            var info = Assembly.GetExecutingAssembly()
                .GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion;
            if (!string.IsNullOrWhiteSpace(info))
            {
                // 去掉可能的 +commit 后缀
                var plus = info.IndexOf('+');
                if (plus > 0) info = info[..plus];
                return info;
            }
            var v = Assembly.GetExecutingAssembly().GetName().Version;
            return v == null ? "1.2.0" : $"{v.Major}.{v.Minor}.{v.Build}";
        }
    }

    public static string Title => $"病程助手 v{Version}";

    public static string FullTitle => $"病程助手 v{Version}  ·  内网单机版";
}
