using Avalonia.Media.Imaging;
using GetFrame.Models;

namespace GetFrame.Services;

public interface IVideoService
{
    Task<VideoInfo> GetVideoInfoAsync(string path, CancellationToken cancellationToken);

    Task<Bitmap> GetFrameAsync(string path, int frameIndex, int? requestedWidth, int? requestedHeight, CancellationToken cancellationToken);

    Task SaveFrameAsPngAsync(string path, int frameIndex, string outputPngPath, CancellationToken cancellationToken);
}
