using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using GetFrame.Core.Services;
using GetFrame.Core.ViewModels;
using GetFrame.Core.Views;
using GetFrame.Core.Models;

namespace GetFrame.Core;

public partial class App : Application
{
    public static IVideoService? VideoService { get; private set; }
    public static ISettingsService? SettingsService { get; private set; }

    public static void RegisterVideoService(IVideoService service) => VideoService = service;
    public static void RegisterSettingsService(ISettingsService service) => SettingsService = service;

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
        else if (ApplicationLifetime is ISingleViewApplicationLifetime singleView)
        {
            singleView.MainView = new MainView
            {
                DataContext = new MainWindowViewModel(VideoService)
            };
        }

        base.OnFrameworkInitializationCompleted();
    }
}
