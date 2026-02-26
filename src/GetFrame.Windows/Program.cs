using Avalonia;

namespace GetFrame.Windows;

internal sealed class Program
{
    [STAThread]
    public static void Main(string[] args) => BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);

    public static AppBuilder BuildAvaloniaApp()
        => AppBuilder.Configure<GetFrame.Core.App>()
            .UsePlatformDetect()
            .LogToTrace();
}
