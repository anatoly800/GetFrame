using System.Diagnostics;
using Avalonia.Media.Imaging;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GetFrame.Models;
using GetFrame.Services;

namespace GetFrame.ViewModels;

public partial class MainWindowViewModel : ObservableObject
{
    private readonly IVideoService _videoService;
    private readonly DispatcherTimer _debounceTimer;
    private readonly DispatcherTimer _elapsedTimer;
    private readonly Stopwatch _stopwatch = new();

    private CancellationTokenSource? _operationCts;
    private VideoInfo? _videoInfo;

    [ObservableProperty] private string frameNumberText = "0";
    [ObservableProperty] private string statusText = "Idle";
    [ObservableProperty] private string elapsedSeconds = "0.00";
    [ObservableProperty] private Bitmap? previewImage;
    [ObservableProperty] private bool hasError;
    [ObservableProperty] private bool isBusy;

    public MainWindowViewModel(IVideoService videoService)
    {
        _videoService = videoService;
        _debounceTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(800) };
        _debounceTimer.Tick += (_, _) =>
        {
            _debounceTimer.Stop();
            _ = ExtractPreviewAsync();
        };

        _elapsedTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(50) };
        _elapsedTimer.Tick += (_, _) => ElapsedSeconds = _stopwatch.Elapsed.TotalSeconds.ToString("0.00");
    }

    partial void OnFrameNumberTextChanged(string value)
    {
        if (_videoInfo is null || IsBusy)
        {
            return;
        }

        _debounceTimer.Stop();
        _debounceTimer.Start();
    }

    [RelayCommand]
    private async Task OpenAsync()
    {
        // In Android file-picker should set this path.
        var path = Environment.GetEnvironmentVariable("GETFRAME_SAMPLE_VIDEO");
        if (string.IsNullOrWhiteSpace(path))
        {
            HasError = true;
            StatusText = "Set GETFRAME_SAMPLE_VIDEO to a local video path.";
            return;
        }

        await RunBusyOperationAsync(async ct =>
        {
            _videoInfo = await _videoService.GetVideoInfoAsync(path, ct);
            FrameNumberText = Math.Max(0, _videoInfo.TotalFrames - 1).ToString();
            HasError = false;
            StatusText = BuildInfoText(_videoInfo);
            await ExtractPreviewAsync();
        });
    }

    [RelayCommand(CanExecute = nameof(CanSave))]
    private async Task SaveAsync()
    {
        if (_videoInfo is null || !TryGetFrameIndex(out var frameIndex))
        {
            return;
        }

        var outputPath = Path.Combine(Path.GetDirectoryName(_videoInfo.Path)!, $"frame-{frameIndex}.png");
        await RunBusyOperationAsync(async ct => await _videoService.SaveFrameAsPngAsync(_videoInfo.Path, frameIndex, outputPath, ct));
        HasError = false;
        StatusText = $"Saved: {outputPath}";
    }

    [RelayCommand]
    private void Cancel()
    {
        _operationCts?.Cancel();
    }

    private bool CanSave() => _videoInfo is not null && !IsBusy;

    private async Task ExtractPreviewAsync()
    {
        if (_videoInfo is null || !TryGetFrameIndex(out var frameIndex))
        {
            return;
        }

        await RunBusyOperationAsync(async ct =>
        {
            PreviewImage = await _videoService.GetFrameAsync(_videoInfo.Path, frameIndex, 960, 540, ct);
            HasError = false;
            StatusText = BuildInfoText(_videoInfo);
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
        if (_videoInfo is null || !int.TryParse(FrameNumberText, out frameIndex) || frameIndex < 0)
        {
            HasError = true;
            StatusText = "Frame number must be a non-negative integer.";
            return false;
        }

        if (frameIndex >= _videoInfo.TotalFrames)
        {
            frameIndex = Math.Max(0, _videoInfo.TotalFrames - 1);
        }

        return true;
    }

    private static string BuildInfoText(VideoInfo info)
    {
        var duration = TimeSpan.FromMilliseconds(info.DurationMs);
        var hhmmss = $"{duration:hh\\:mm\\:ss}.{Math.Max(0, info.TotalFrames - 1):000}";
        return $"{info.Width}x{info.Height}, {hhmmss}, Frames [{info.TotalFrames}], FPS {info.Fps:0.###}, Codec {info.Codec}";
    }
}
