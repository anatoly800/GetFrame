using System.Diagnostics;
using Avalonia.Media.Imaging;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GetFrame.Core.Models;
using GetFrame.Core.Services;


namespace GetFrame.Core.ViewModels;

public partial class MainWindowViewModel : ObservableObject
{
    private readonly IVideoService _videoService;
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

    public MainWindowViewModel(IVideoService? videoService)
    {
        if (videoService is null)
        {
            throw new ArgumentNullException(nameof(videoService), "Video service must be provided.");
        }
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
        string? path = await _videoService.AskVideoFilePathAsync();
        if (IsBusy || string.IsNullOrEmpty(path))
        {
            return;
        }

        await RunBusyOperationAsync(async ct =>
        {
            _videoMetadata = await _videoService.GetVideoInfoAsync(path,ct);
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
    private async Task SaveAsync()
    {
        if (_videoMetadata is null || !TryGetFrameIndex(out var frameIndex))
        {
            return;
        }

        var outputPath = Path.Combine(Path.GetDirectoryName(_videoMetadata.FilePath)!, $"frame-{frameIndex}.png");
        await RunBusyOperationAsync(async ct =>
        {
            await _videoService.SaveFrameAsPngAsync(_videoMetadata.FilePath, frameIndex, outputPath, ct);
            HasError = false;
            StatusText = $"Saved: {outputPath}";
        });
    }

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
            PreviewImage = await _videoService.GetFrameAsync(_videoMetadata.FilePath, frameIndex, ct);
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

}
