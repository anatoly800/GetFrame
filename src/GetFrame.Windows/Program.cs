using Avalonia;

namespace GetFrame.Windows;

internal sealed class Program
{
    [STAThread]
    public static void Main(string[] args)
    {
        GetFrame.Core.App.RegisterVideoService(new VideoService());
        GetFrame.Core.App.RegisterSettingsService(new GetFrame.Core.Services.SettingsService("GetFrameSettings.json"));
        BuildAvaloniaApp()
            .StartWithClassicDesktopLifetime(args);
    }

    public static AppBuilder BuildAvaloniaApp()
        => AppBuilder.Configure<GetFrame.Core.App>()
            .UsePlatformDetect()
            .LogToTrace();
}
