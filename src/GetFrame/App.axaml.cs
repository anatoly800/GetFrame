using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using GetFrame.Services;
using GetFrame.ViewModels;
using GetFrame.Views;

namespace GetFrame;

public partial class App : Application
{
    public static IVideoService VideoService { get; private set; } = new NotSupportedVideoService();

    public static void RegisterVideoService(IVideoService service) => VideoService = service;

    public override void Initialize() => AvaloniaXamlLoader.Load(this);

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.MainWindow = new MainWindow
            {
                DataContext = new MainWindowViewModel(VideoService)
            };
        }

        base.OnFrameworkInitializationCompleted();
    }
}
