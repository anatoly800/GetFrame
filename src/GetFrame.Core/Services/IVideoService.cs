using Avalonia.Media.Imaging;
using GetFrame.Core.Models;

namespace GetFrame.Core.Services;

public interface IVideoService
{
    Task<string?> AskVideoFilePathAsync();

    Task<VideoMetadata> GetVideoInfoAsync(string path, CancellationToken cancellationToken);

    Task<Bitmap> GetFrameAsync(string path, int frameIndex, int? requestedWidth, int? requestedHeight, CancellationToken cancellationToken);

    Task SaveFrameAsPngAsync(string path, int frameIndex, string outputPngPath, CancellationToken cancellationToken);
}
