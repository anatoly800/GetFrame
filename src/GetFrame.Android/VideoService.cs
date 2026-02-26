using Android.Media;
using GetFrame.Core.Models;
using GetFrame.Core.Services;

namespace GetFrame.Android;

public sealed class VideoService : IVideoService
{

    public async Task<string?> AskVideoFilePathAsync()
    {
        // In Android file-picker should set this path.
        var path = Environment.GetEnvironmentVariable("GETFRAME_SAMPLE_VIDEO");
        if (string.IsNullOrWhiteSpace(path))
        {
            return null;
        }
        return path;
    }

    public async Task<VideoMetadata> GetVideoInfoAsync(string path, CancellationToken cancellationToken)
    {
        return await Task.Run(() =>
        {
            try
            {
                cancellationToken.ThrowIfCancellationRequested();

                VideoMetadata videoMetadata = new()
                {
                    FilePath = path
                };
                using MediaMetadataRetriever retriever = new();
                retriever.SetDataSource(path);
                videoMetadata.Width = int.TryParse(retriever.ExtractMetadata(MetadataKey.VideoWidth), out var width) ? width : 0;
                videoMetadata.Height = int.TryParse(retriever.ExtractMetadata(MetadataKey.VideoHeight), out var height) ? height : 0;
                videoMetadata.DurationMs = ParseDouble(retriever.ExtractMetadata(MetadataKey.Duration));
                videoMetadata.Framerate = retriever.ExtractMetadata(MetadataKey.CaptureFramerate) ?? throw new InvalidOperationException("Cannot read FPS on this device/API.");
                videoMetadata.Fps = ParseDouble(videoMetadata.Framerate);
                videoMetadata.Frames = OperatingSystem.IsAndroidVersionAtLeast(28)
                    ? long.TryParse(retriever.ExtractMetadata(MetadataKey.VideoFrameCount), out var frames) ? frames : 0
                    : (long)Math.Max(1, Math.Round(videoMetadata.DurationMs / 1000d * videoMetadata.Fps));
                videoMetadata.FileSize = new FileInfo(path).Length;
                return videoMetadata;
            }
            catch (Exception ex)
            {
                return new VideoMetadata
                {
                    FilePath = path,
                    VideoServiceErrorCode = cancellationToken.IsCancellationRequested
                        ? VideoServiceErrorCode.OperationCanceled
                        : VideoServiceErrorCode.MetadataRetrievalFailed,
                    StatusMessage = cancellationToken.IsCancellationRequested
                        ? "Operation was canceled."
                        : $"Failed to retrieve video metadata: {ex.Message}"
                };
            }
        }, cancellationToken);
    }

    public async Task<Avalonia.Media.Imaging.Bitmap> GetFrameAsync(
        string path,
        int frameIndex,
        int? requestedWidth,
        int? requestedHeight,
        CancellationToken cancellationToken
        )
    {
        return await Task.Run(() =>
        {
            cancellationToken.ThrowIfCancellationRequested();
            using var retriever = new MediaMetadataRetriever();
            retriever.SetDataSource(path);

            using var nativeBitmap = GetFrameInternal(retriever, frameIndex, requestedWidth, requestedHeight)
                ?? throw new InvalidOperationException("Frame extraction failed.");

            using MemoryStream stream = new();

            var fmt = global::Android.Graphics.Bitmap.CompressFormat.Png ?? throw new InvalidOperationException("PNG compression format is not supported on this device.");
            nativeBitmap.Compress(fmt, 100, stream);
            stream.Position = 0;
            return new Avalonia.Media.Imaging.Bitmap(stream);
        }, cancellationToken);
    }

    public async Task SaveFrameAsPngAsync(string path, int frameIndex, string outputPngPath, CancellationToken cancellationToken)
    {
        await Task.Run(() =>
        {
            cancellationToken.ThrowIfCancellationRequested();
            using var retriever = new MediaMetadataRetriever();
            retriever.SetDataSource(path);

            using var nativeBitmap = GetFrameInternal(retriever, frameIndex, null, null)
                ?? throw new InvalidOperationException("Frame extraction failed.");
            using var output = File.Open(outputPngPath, FileMode.Create, FileAccess.Write, FileShare.None);

            var fmt = global::Android.Graphics.Bitmap.CompressFormat.Png ?? throw new InvalidOperationException("PNG compression format is not supported on this device.");
            nativeBitmap.Compress(fmt, 100, output);
        }, cancellationToken);
    }

    private static global::Android.Graphics.Bitmap? GetFrameInternal(MediaMetadataRetriever retriever, int frameIndex, int? requestedWidth, int? requestedHeight)
    {
        if (OperatingSystem.IsAndroidVersionAtLeast(28))
        {
            return retriever.GetFrameAtIndex(frameIndex);
        }

        var fps = ParseDouble(retriever.ExtractMetadata(MetadataKey.CaptureFramerate)
            ?? throw new InvalidOperationException("Cannot read FPS on this device/API."));

        var timeUs = (long)Math.Max(0, ((frameIndex / fps) * 1_000_000d) - 1);

        if (requestedWidth is > 0 && requestedHeight is > 0)
        {
            if (OperatingSystem.IsAndroidVersionAtLeast(27))
            {
                return retriever.GetScaledFrameAtTime(timeUs, Option.Closest, requestedWidth.Value, requestedHeight.Value);
            }
            else
            {
                var unscaledBitmap = retriever.GetFrameAtTime(timeUs, Option.Closest);
                if (unscaledBitmap != null)
                {
                    var scaledBitmap = global::Android.Graphics.Bitmap.CreateScaledBitmap(unscaledBitmap, requestedWidth.Value, requestedHeight.Value, true);
                    if (scaledBitmap != unscaledBitmap)
                    {
                        unscaledBitmap.Dispose();
                    }
                    return scaledBitmap;
                }
                return null;
            }
        }

        return retriever.GetFrameAtTime(timeUs, Option.Closest);
    }

    private static double ParseDouble(string? value)
        => double.TryParse(value, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out var parsed)
            ? parsed
            : 0;
}
