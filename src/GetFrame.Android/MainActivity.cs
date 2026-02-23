using Android.App;
using Android.Content.PM;
using Android.OS;
using Avalonia;
using Avalonia.Android;
using GetFrame;

namespace GetFrame.Android;

[Activity(
    Label = "GetFrame",
    Theme = "@style/MyTheme.NoActionBar",
    Icon = "@mipmap/icon",
    LaunchMode = LaunchMode.SingleTop,
    ConfigurationChanges = ConfigChanges.Orientation | ConfigChanges.ScreenSize | ConfigChanges.UiMode,
    ScreenOrientation = ScreenOrientation.FullUser)]
public class MainActivity : AvaloniaMainActivity<App>
{
    protected override AppBuilder CustomizeAppBuilder(AppBuilder builder)
    {
        App.RegisterVideoService(new AndroidVideoService());
        return base.CustomizeAppBuilder(builder);
    }
}
