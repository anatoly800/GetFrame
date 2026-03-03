using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using GetFrame.Core.Services;
using GetFrame.Core.Views;

namespace GetFrame.Core;

public partial class App : Application
{
    public static IVideoService VideoService { get; set; } = default!;
    public static ISettingsService SettingsService { get; set; } = default!;

    public override void Initialize() => AvaloniaXamlLoader.Load(this);

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.MainWindow = new MainWindow();
        }
        else if (ApplicationLifetime is ISingleViewApplicationLifetime singleView)
        {
            singleView.MainView = new MainView
            {
                DataContext = new ViewModels.MainWindowViewModel()
            };
        }

        base.OnFrameworkInitializationCompleted();
    }
}
