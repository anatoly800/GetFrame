using Android.Content.PM;
using Avalonia;
using Avalonia.Android;
using Android.OS;
using AndroidX.Core.Content;
using AndroidX.Core.App;

namespace GetFrame.Android;

[Activity(
    Label = "GetFrame",
    Theme = "@style/MyTheme.NoActionBar",
    Icon = "@mipmap/icon",
    LaunchMode = LaunchMode.SingleTop,
    ConfigurationChanges = ConfigChanges.Orientation | ConfigChanges.ScreenSize | ConfigChanges.UiMode,
    ScreenOrientation = ScreenOrientation.FullUser
)]

public class MainActivity : AvaloniaMainActivity<GetFrame.Core.App>
{
    private const int RequestStoragePermission = 1;

    private static string GetStoragePermission()
    {
        if (OperatingSystem.IsAndroidVersionAtLeast(33))
            return global::Android.Manifest.Permission.ReadMediaVideo;
        return global::Android.Manifest.Permission.ReadExternalStorage;
    }

    protected override void OnCreate(Bundle? savedInstanceState)
    {
        base.OnCreate(savedInstanceState);

        var permission = GetStoragePermission();
        RequestStoragePermissions(permission);
    }

    protected override AppBuilder CustomizeAppBuilder(AppBuilder builder)
    {
        GetFrame.Core.App.VideoService = new VideoService();
        GetFrame.Core.App.SettingsService = new GetFrame.Core.Services.SettingsService("GetFrameSettings.json");
        return base.CustomizeAppBuilder(builder);
    }

    private void RequestStoragePermissions(string permission)
    {
        if (ContextCompat.CheckSelfPermission(this, permission) != Permission.Granted)
        {
            ActivityCompat.RequestPermissions(this, permissions: [permission], RequestStoragePermission);
        }
    }

    public override void OnRequestPermissionsResult(int requestCode, string[] permissions, Permission[] grantResults)
    {
        base.OnRequestPermissionsResult(requestCode, permissions, grantResults);
        
        if (requestCode == RequestStoragePermission)
        {
            if (grantResults.Length < 1 || grantResults[0] != Permission.Granted)
            {
                Toast.MakeText(this, "Storage permission is required to select video files.", ToastLength.Long)?.Show();
                Finish();
            }
        }
    }
}
