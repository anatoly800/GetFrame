using Avalonia.Media.Imaging;
using GetFrame.Models;

namespace GetFrame.Services;

public sealed class NotSupportedVideoService : IVideoService
{
    private static InvalidOperationException Ex() => new("Video extraction is available only on Android runtime.");

    public Task<VideoInfo> GetVideoInfoAsync(string path, CancellationToken cancellationToken) => Task.FromException<VideoInfo>(Ex());

    public Task<Bitmap> GetFrameAsync(string path, int frameIndex, int? requestedWidth, int? requestedHeight, CancellationToken cancellationToken)
        => Task.FromException<Bitmap>(Ex());

    public Task SaveFrameAsPngAsync(string path, int frameIndex, string outputPngPath, CancellationToken cancellationToken)
        => Task.FromException(Ex());
}
