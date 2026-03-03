using Avalonia;
using GetFrame.Core.ViewModels;

namespace GetFrame.Windows;

internal sealed class Program
{
    [STAThread]
    public static void Main(string[] args)
    {
        GetFrame.Core.App.VideoService = new VideoService();
        GetFrame.Core.App.SettingsService = new GetFrame.Core.Services.SettingsService("GetFrameSettings.json");
        GetFrame.Core.App.SettingsService.SetKey("SelectFFmpeg", "true");
        
        // Subscribe to settings error events
        GetFrame.Core.App.SettingsService.OnError += (message) =>
        {
            Avalonia.Threading.Dispatcher.UIThread.Post(() =>
            {
                MainWindowViewModel.Current?.ShowError(message);
            });
        };
        
        BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);
    }

    public static AppBuilder BuildAvaloniaApp()
        => AppBuilder.Configure<GetFrame.Core.App>()
            .UsePlatformDetect()
            .LogToTrace();
}
