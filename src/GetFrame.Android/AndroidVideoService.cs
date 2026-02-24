using Android.Media;
using GetFrame.Models;
using GetFrame.Services;

namespace GetFrame.Android
    public sealed class AndroidVideoService : IVideoService
{
    public async Task<VideoInfo> GetVideoInfoAsync(string path, CancellationToken cancellationToken)
    {
        return await Task.Run(() =>
        {
            cancellationToken.ThrowIfCancellationRequested();
            using MediaMetadataRetriever retriever = new();
            retriever.SetDataSource(path);
            var width = ParseInt(retriever.ExtractMetadata(MetadataKey.VideoWidth));
            var height = ParseInt(retriever.ExtractMetadata(MetadataKey.VideoHeight));
            var durationMs = ParseDouble(retriever.ExtractMetadata(MetadataKey.Duration));
            var fpsText = retriever.ExtractMetadata(MetadataKey.CaptureFramerate) ?? throw new InvalidOperationException("Cannot read FPS on this device/API.");
            var fps = ParseDouble(fpsText);
            var totalFrames = OperatingSystem.IsAndroidVersionAtLeast(28)
                ? ParseInt(retriever.ExtractMetadata(MetadataKey.VideoFrameCount))
                : (int)Math.Max(1, Math.Round(durationMs / 1000d * fps));
            var codec = retriever.ExtractMetadata(MetadataKey.Mimetype) ?? "unknown";
            return new VideoInfo(path, width, height, durationMs, fps, Math.Max(1, totalFrames), codec);
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

    private static int ParseInt(string? value)
        => int.TryParse(value, out var parsed) ? parsed : 0;

    private static double ParseDouble(string? value)
        => double.TryParse(value, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out var parsed)
            ? parsed
            : 0;
}
