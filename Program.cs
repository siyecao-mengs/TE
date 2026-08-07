using Avalonia;
using System;
using System.IO;

namespace TerminalEmbellish.Desktop;

class Program
{
    [STAThread]
    public static void Main(string[] args)
    {
        var webView2Path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "WebView2");
        if (Directory.Exists(webView2Path))
            Environment.SetEnvironmentVariable("WEBVIEW2_BROWSER_EXECUTABLE_FOLDER", webView2Path);

        BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);
    }

    public static AppBuilder BuildAvaloniaApp()
        => AppBuilder.Configure<App>()
            .UsePlatformDetect()
#if DEBUG
            .WithDeveloperTools()
#endif
            .WithInterFont()
            .LogToTrace();
}
