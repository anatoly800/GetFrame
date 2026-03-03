using System.Diagnostics;
using Avalonia;
using Avalonia.Media.Imaging;
using Avalonia.Platform.Storage;
using Avalonia.Styling;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GetFrame.Core.Models;
using GetFrame.Core.Services;
using GetFrame.Core.Views;
using Avalonia.Svg.Skia;

namespace GetFrame.Core.ViewModels;

public partial class MainWindowViewModel : ObservableObject
{
    private readonly DispatcherTimer _debounceTimer;
    private readonly DispatcherTimer _elapsedTimer;
    private readonly Stopwatch _stopwatch = new();

    private CancellationTokenSource? _operationCts;
    private VideoMetadata? _videoMetadata;

    [ObservableProperty] private string frameNumberText = "0";
    [ObservableProperty] private string statusText = "Idle";
    [ObservableProperty] private string elapsedSeconds = "0.00";
    [ObservableProperty] private Bitmap? previewImage;
    [ObservableProperty] private bool hasError;
    [ObservableProperty] private bool isBusy;
    [ObservableProperty] private bool isDarkTheme = true;

    [ObservableProperty] private SvgImage? openIconSource;
    [ObservableProperty] private SvgImage? settingsIconSource;
    [ObservableProperty] private SvgImage? saveIconSource;
    [ObservableProperty] private SvgImage? openPngFolderIconSource;

    [ObservableProperty] private ObservableObject _currentView;

    private string? _savedFilePath = null;

    // Singleton reference so SettingsView can trigger theme refresh
    public static MainWindowViewModel? Current { get; private set; }

    public MainWindowViewModel()
    {
        Current = this;
        _currentView = this;

        _debounceTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(800) };
        _debounceTimer.Tick += (_, _) =>
        {
            _debounceTimer.Stop();
            _ = ExtractPreviewAsync();
        };
        _elapsedTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(50) };
        _elapsedTimer.Tick += (_, _) => ElapsedSeconds = _stopwatch.Elapsed.TotalSeconds.ToString("0.00");
        ApplyTheme();
    }

    partial void OnFrameNumberTextChanged(string value)
    {
        if (_videoMetadata is null || IsBusy)
        {
            return;
        }
        _debounceTimer.Stop();
        _debounceTimer.Start();
    }

    [RelayCommand]
    private async Task OpenAsync()
    {
        string? path = await GetFrame.Core.App.VideoService.GetVideoFilePath();
        if (IsBusy || string.IsNullOrEmpty(path))
        {
            return;
        }

        await RunBusyOperationAsync(async ct =>
        {
            _videoMetadata = await GetFrame.Core.App.VideoService.GetVideoInfoAsync(path, ct);
            if (_videoMetadata is null)
            {
                HasError = true;
                StatusText = "Failed to load video metadata.";
                return;
            }
            if (_videoMetadata.VideoServiceErrorCode != VideoServiceErrorCode.None)
            {
                HasError = true;
                StatusText = $"Error: {_videoMetadata.StatusMessage}";
                return;
            }
            if (_videoMetadata.Frames == 0)
            {
                HasError = true;
                StatusText = "Video contains no frames.";
                return;
            }

            if (_videoMetadata.Frames >= int.MaxValue)
            {
                HasError = true;
                StatusText = $"Video contains too many frames ({_videoMetadata.Frames}). Maximum supported is {int.MaxValue}.";
                return;
            }

            FrameNumberText = Math.Max(0, _videoMetadata.Frames - 1).ToString();
            HasError = false;
            StatusText = _videoMetadata.BuildInfoText();
            await ExtractPreviewAsync();
        });
    }

    [RelayCommand(CanExecute = nameof(CanSave))]
    private async Task Save()
    {
        if (_videoMetadata is null || !TryGetFrameIndex(out var frameIndex))
        {
            return;
        }

        var directory = GetFrame.Core.App.SettingsService.GetKey("PngSavePath") ?? Path.GetDirectoryName(_videoMetadata.FilePath);
        if (directory == null)
        {
            HasError = true;
            StatusText = $"Invalid save directory. Source [{_videoMetadata.FilePath}]";
            return;
        }
        if (!Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
        }
        var baseFileName = Path.GetFileNameWithoutExtension(_videoMetadata.FilePath);
        _savedFilePath = Path.Combine(directory, $"{baseFileName}[{frameIndex}].png");
        await RunBusyOperationAsync(async ct =>
        {
            await GetFrame.Core.App.VideoService.SaveFrameAsPngAsync(_videoMetadata.FilePath, frameIndex, _savedFilePath, ct);
            HasError = false;
            StatusText = $"Saved: {_savedFilePath}";
        });
        OpenPngFolderCommand.NotifyCanExecuteChanged();
    }

    [RelayCommand(CanExecute = nameof(CanOpenPngFolder))]
    private void OpenPngFolder()
    {
        try
        {
            GetFrame.Core.App.VideoService.OpenPngFolder(_savedFilePath);
        }
        catch (Exception ex)
        {
            HasError = true;
            StatusText = $"Failed to open folder: {ex.Message}";
        }
    }

    private bool CanOpenPngFolder() => _savedFilePath is not null;

    [RelayCommand]
    private void Cancel()
    {
        _operationCts?.Cancel();
    }

    private bool CanSave() => _videoMetadata is not null && !IsBusy;

    private async Task ExtractPreviewAsync()
    {
        if (_videoMetadata is null || !TryGetFrameIndex(out var frameIndex))
        {
            return;
        }
        await RunBusyOperationAsync(async ct =>
        {
            PreviewImage = await GetFrame.Core.App.VideoService.GetFrameAsync(_videoMetadata.FilePath, frameIndex, ct);
            HasError = false;
            StatusText = _videoMetadata.BuildInfoText();
        });
    }

    private async Task RunBusyOperationAsync(Func<CancellationToken, Task> action)
    {
        _operationCts?.Cancel();
        _operationCts = new CancellationTokenSource();
        IsBusy = true;
        CancelCommand.NotifyCanExecuteChanged();
        SaveCommand.NotifyCanExecuteChanged();
        _stopwatch.Restart();
        _elapsedTimer.Start();

        try
        {
            await action(_operationCts.Token);
        }
        catch (OperationCanceledException)
        {
            HasError = true;
            StatusText = "Operation canceled.";
        }
        catch (Exception ex)
        {
            HasError = true;
            StatusText = ex.Message;
        }
        finally
        {
            IsBusy = false;
            CancelCommand.NotifyCanExecuteChanged();
            SaveCommand.NotifyCanExecuteChanged();
            _elapsedTimer.Stop();
            _stopwatch.Stop();
        }
    }

    private bool TryGetFrameIndex(out int frameIndex)
    {
        frameIndex = 0;
        if (_videoMetadata is null || !long.TryParse(FrameNumberText, out long tmp) || tmp < 0)
        {
            HasError = true;
            StatusText = "Frame number must be a non-negative integer.";
            return false;
        }
        frameIndex = (int)Math.Min(tmp, int.MaxValue);
        if (frameIndex >= _videoMetadata.Frames)
        {
            frameIndex = (int)Math.Max(0, _videoMetadata.Frames - 1);
        }
        return true;
    }

    [RelayCommand]
    private void Settings()
    {
        var settingsViewModel = new SettingsViewModel(
            GetFrame.Core.App.VideoService,
            GetFrame.Core.App.SettingsService,
            this);
        CurrentView = settingsViewModel;
    }

    public void ApplyTheme()
    {
        var raw = GetFrame.Core.App.SettingsService.GetKey("DarkTheme");
        IsDarkTheme = raw == null || !bool.TryParse(raw, out var v) || v;
        if (Application.Current is not null)
        {
            Application.Current.RequestedThemeVariant = IsDarkTheme ? ThemeVariant.Dark : ThemeVariant.Light;
        }
        var suffix = IsDarkTheme ? "dark" : "light";
        var folder = IsDarkTheme ? "dark_theme_white" : "light_theme_black";
        OpenIconSource = LoadSvgIcon(folder, $"open-{suffix}.svg");
        SettingsIconSource = LoadSvgIcon(folder, $"settings-{suffix}.svg");
        SaveIconSource = LoadSvgIcon(folder, $"save-{suffix}.svg");
        OpenPngFolderIconSource = LoadSvgIcon(folder, $"eye-{suffix}.svg");
    }

    private static SvgImage? LoadSvgIcon(string folder, string fileName)
    {
        try
        {
            var uri = new Uri($"avares://GetFrame.Core/resources/{folder}/{fileName}");
            return new SvgImage { Source = SvgSource.Load(fileName, uri) };
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Failed to load SVG icon {folder}/{fileName}: {ex.Message}");
            return null;
        }
    }

    public void ShowError(string message)
    {
        if (string.IsNullOrEmpty(message))
        {
            HasError = false;
            StatusText = "Idle";
            return;
        }
        HasError = true;
        StatusText = message;
    }
}
