using Avalonia;

namespace GetFrame.Windows;

internal sealed class Program
{
    [STAThread]
    public static void Main(string[] args)
    {
        GetFrame.Core.App.VideoService = new VideoService();
        GetFrame.Core.App.SettingsService = new GetFrame.Core.Services.SettingsService("GetFrameSettings.json");
        BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);
    }

    public static AppBuilder BuildAvaloniaApp()
        => AppBuilder.Configure<GetFrame.Core.App>()
            .UsePlatformDetect()
            .LogToTrace();
}
