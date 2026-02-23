using Android.Graphics;
using Android.Media;
using Android.OS;
using Avalonia.Media.Imaging;
using GetFrame.Models;
using GetFrame.Services;
using Bitmap = Avalonia.Media.Imaging.Bitmap;

namespace GetFrame.Android;

public sealed class AndroidVideoService : IVideoService
{
    public async Task<VideoInfo> GetVideoInfoAsync(string path, CancellationToken cancellationToken)
    {
        return await Task.Run(() =>
        {
            cancellationToken.ThrowIfCancellationRequested();
            using var retriever = new MediaMetadataRetriever();
            retriever.SetDataSource(path);

            var width = ParseInt(retriever.ExtractMetadata(MetadataKey.VideoWidth));
            var height = ParseInt(retriever.ExtractMetadata(MetadataKey.VideoHeight));
            var durationMs = ParseDouble(retriever.ExtractMetadata(MetadataKey.Duration));
            var fpsText = retriever.ExtractMetadata(MetadataKey.CaptureFramerate)
                ?? throw new InvalidOperationException("Cannot read FPS on this device/API.");
            var fps = ParseDouble(fpsText);

            var totalFrames = Build.VERSION.SdkInt >= BuildVersionCodes.P
                ? ParseInt(retriever.ExtractMetadata(MetadataKey.VideoFrameCount))
                : (int)Math.Max(1, Math.Round(durationMs / 1000d * fps));

            var codec = retriever.ExtractMetadata(MetadataKey.Mimetype) ?? "unknown";

            return new VideoInfo(path, width, height, durationMs, fps, Math.Max(1, totalFrames), codec);
        }, cancellationToken);
    }

    public async Task<Bitmap> GetFrameAsync(string path, int frameIndex, int? requestedWidth, int? requestedHeight, CancellationToken cancellationToken)
    {
        return await Task.Run(() =>
        {
            cancellationToken.ThrowIfCancellationRequested();
            using var retriever = new MediaMetadataRetriever();
            retriever.SetDataSource(path);

            using var nativeBitmap = GetFrameInternal(retriever, frameIndex, requestedWidth, requestedHeight)
                ?? throw new InvalidOperationException("Frame extraction failed.");

            using var stream = new MemoryStream();
            nativeBitmap.Compress(Android.Graphics.Bitmap.CompressFormat.Png, 100, stream);
            stream.Position = 0;
            return new Bitmap(stream);
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
            nativeBitmap.Compress(Android.Graphics.Bitmap.CompressFormat.Png, 100, output);
        }, cancellationToken);
    }

    private static Android.Graphics.Bitmap? GetFrameInternal(MediaMetadataRetriever retriever, int frameIndex, int? requestedWidth, int? requestedHeight)
    {
        if (Build.VERSION.SdkInt >= BuildVersionCodes.P)
        {
            return retriever.GetFrameAtIndex(frameIndex);
        }

        var durationMs = ParseDouble(retriever.ExtractMetadata(MetadataKey.Duration));
        var fps = ParseDouble(retriever.ExtractMetadata(MetadataKey.CaptureFramerate)
            ?? throw new InvalidOperationException("Cannot read FPS on this device/API."));

        var timeUs = (long)Math.Max(0, ((frameIndex / fps) * 1_000_000d) - 1);

        if (requestedWidth is > 0 && requestedHeight is > 0)
        {
            return retriever.GetScaledFrameAtTime(timeUs, MediaMetadataRetrieverOption.Closest, requestedWidth.Value, requestedHeight.Value);
        }

        return retriever.GetFrameAtTime(timeUs, MediaMetadataRetrieverOption.Closest);
    }

    private static int ParseInt(string? value)
        => int.TryParse(value, out var parsed) ? parsed : 0;

    private static double ParseDouble(string? value)
        => double.TryParse(value, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out var parsed)
            ? parsed
            : 0;
}
