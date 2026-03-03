using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using GetFrame.Core.ViewModels;

namespace GetFrame.Core.Views;

public partial class SettingsView : Window
{
    public SettingsView()
    {
        InitializeComponent();
        var isDarkTheme = GetFrame.Core.App.SettingsService.GetKey("DarkTheme");
        ThemeToggle.IsChecked = isDarkTheme == null || bool.Parse(isDarkTheme); // default: true (Dark)
        PngSavePathTextBox.Text = GetFrame.Core.App.SettingsService.GetKey("PngSavePath") ?? string.Empty;
        FfmpegPathTextBox.Text = GetFrame.Core.App.SettingsService.GetKey("FfmpegPath") ?? string.Empty; 

        ThemeToggle.IsCheckedChanged += OnThemeToggleChanged;
        BackButton.Click += OnBackButtonClicked;
        SelectPngPathButton.Click += OnSelectPngPathClicked;
        SelectFfmpegPathButton.Click += OnSelectFfmpegPathClicked;

        var selectFfmpeg = GetFrame.Core.App.SettingsService.GetKey("SelectFFmpeg");  
        FfmpegPathPanel.IsVisible = selectFfmpeg is null || selectFfmpeg != "false";
    }

    private void OnThemeToggleChanged(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        var isDark = ThemeToggle.IsChecked == true;
        GetFrame.Core.App.SettingsService.SetKey("DarkTheme", isDark.ToString());
        MainWindowViewModel.Current?.ApplyTheme();
    }

    private async void OnSelectPngPathClicked(object? sender, RoutedEventArgs e)
    {
        if (!this.StorageProvider.CanPickFolder)
            return;

        var folders = await this.StorageProvider.OpenFolderPickerAsync(
            new FolderPickerOpenOptions { Title = "Select PNG Save Folder" });
        var folder = folders?.FirstOrDefault();
        if (folder != null)
        {
            var localPath = folder.Path?.LocalPath;
            if (localPath != null)
            {
                PngSavePathTextBox.Text = localPath;
                GetFrame.Core.App.SettingsService.SetKey("PngSavePath", localPath);
            }
        }
    }

    private async void OnSelectFfmpegPathClicked(object? sender, RoutedEventArgs e)
    {
        if (!this.StorageProvider.CanOpen)
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

        var files = await this.StorageProvider.OpenFilePickerAsync(options);
        var file = files?.FirstOrDefault();
        if (file != null)
        {
            var localPath = file.Path?.LocalPath;
            if (!string.IsNullOrEmpty(localPath))
            {
                FfmpegPathTextBox.Text = localPath;
                GetFrame.Core.App.SettingsService.SetKey("FfmpegPath", localPath);
            }
        }
    }

    private void OnBackButtonClicked(object? sender, RoutedEventArgs e)
    {
        Close();
    }
}