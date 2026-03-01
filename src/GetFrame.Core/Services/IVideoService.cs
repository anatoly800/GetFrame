using Avalonia.Media.Imaging;
using GetFrame.Core.Models;

namespace GetFrame.Core.Services;

public interface IVideoService
{
    Task<string?> GetVideoFilePath();

    Task<VideoMetadata> GetVideoInfoAsync(string path, CancellationToken cancellationToken);

    Task<Bitmap> GetFrameAsync(string path, int frameIndex, CancellationToken cancellationToken);

    Task SaveFrameAsPngAsync(string path, int frameIndex, string outputPngPath, CancellationToken cancellationToken);
}
