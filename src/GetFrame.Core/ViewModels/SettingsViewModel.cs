using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Platform.Storage;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GetFrame.Core.Services;

namespace GetFrame.Core.ViewModels;

public partial class SettingsViewModel : ObservableObject
{
    private readonly IVideoService _videoService;
    private readonly ISettingsService _settingsService;
    private readonly MainWindowViewModel _mainViewModel;

    [ObservableProperty] private bool isDarkTheme;
    [ObservableProperty] private string pngSavePath = string.Empty;
    [ObservableProperty] private string ffmpegPath = string.Empty;
    [ObservableProperty] private bool isFfmpegPathVisible;
    [ObservableProperty] private string settingsStatusText = string.Empty;
    [ObservableProperty] private bool hasSettingsError;

    public SettingsViewModel(
        IVideoService videoService,
        ISettingsService settingsService,
        MainWindowViewModel mainViewModel)
    {
        _videoService = videoService;
        _settingsService = settingsService;
        _mainViewModel = mainViewModel;

        // Subscribe to settings status events
        _settingsService.OnStatusChanged += (message) =>
        {
            SettingsStatusText = message;
            HasSettingsError = !string.IsNullOrEmpty(message);
        };

        LoadSettings();
    }

    private void LoadSettings()
    {
        var isDark = _settingsService.GetKey("DarkTheme");
        IsDarkTheme = isDark == null || bool.Parse(isDark);

        PngSavePath = _settingsService.GetKey("PngSavePath") ?? string.Empty;
        FfmpegPath = _settingsService.GetKey("FfmpegPath") ?? string.Empty;

        var selectFfmpeg = _settingsService.GetKey("SelectFFmpeg");
        IsFfmpegPathVisible = selectFfmpeg is null || selectFfmpeg != "false";
    }

    partial void OnIsDarkThemeChanged(bool value)
    {
        _settingsService.SetKey("DarkTheme", value.ToString());
        _mainViewModel.ApplyTheme();
    }

    partial void OnPngSavePathChanged(string value)
    {
        _settingsService.SetKey("PngSavePath", value);
    }

    partial void OnFfmpegPathChanged(string value)
    {
        _settingsService.SetKey("FfmpegPath", value);
    }

    [RelayCommand]
    private void GoBack()
    {
        _mainViewModel.CurrentView = _mainViewModel;
    }

    private static TopLevel? GetTopLevel()
    {
        return Application.Current?.ApplicationLifetime switch
        {
            IClassicDesktopStyleApplicationLifetime desktop => TopLevel.GetTopLevel(desktop.MainWindow),
            ISingleViewApplicationLifetime single => TopLevel.GetTopLevel(single.MainView),
            _ => null
        };
    }

    [RelayCommand]
    private async Task SelectPngPathAsync()
    {
        var topLevel = GetTopLevel();
        if (topLevel == null)
            return;

        if (!topLevel.StorageProvider.CanPickFolder)
            return;

        var folders = await topLevel.StorageProvider.OpenFolderPickerAsync(
            new FolderPickerOpenOptions { Title = "Select PNG Save Folder" });
        var folder = folders?.FirstOrDefault();
        if (folder != null)
        {
            var localPath = folder.Path?.LocalPath;
            if (localPath != null)
            {
                PngSavePath = localPath;
            }
        }
    }

    [RelayCommand]
    private async Task SelectFfmpegPathAsync()
    {
        var topLevel = GetTopLevel();
        if (topLevel == null)
            return;

        if (!topLevel.StorageProvider.CanOpen)
            return;

        var options = new FilePickerOpenOptions
        {
            Title = "Select FFmpeg Executable",
            AllowMultiple = false,
            FileTypeFilter = new[]
            {
                new FilePickerFileType("Executable")
                {
                    Patterns = ["*.exe"]
                }
            }
        };

        var files = await topLevel.StorageProvider.OpenFilePickerAsync(options);
        var file = files?.FirstOrDefault();
        if (file != null)
        {
            var localPath = file.Path?.LocalPath;
            if (!string.IsNullOrEmpty(localPath))
            {
                FfmpegPath = localPath;
            }
        }
    }
}
