using Android.Content.PM;
using Avalonia;
using Avalonia.Android;
using GetFrame.Core.Services;
using GetFrame.Android;

namespace GetFrame.Android;

[Activity(
    Label = "GetFrame",
    Theme = "@style/MyTheme.NoActionBar",
    Icon = "@mipmap/icon",
    LaunchMode = LaunchMode.SingleTop,
    ConfigurationChanges = ConfigChanges.Orientation | ConfigChanges.ScreenSize | ConfigChanges.UiMode,
    ScreenOrientation = ScreenOrientation.FullUser)]
public class MainActivity : AvaloniaMainActivity<GetFrame.Core.App>
{
    protected override AppBuilder CustomizeAppBuilder(AppBuilder builder)
    {
        GetFrame.Core.App.RegisterVideoService(new VideoService());
        return base.CustomizeAppBuilder(builder);
    }
}
