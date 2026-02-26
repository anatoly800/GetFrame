// Ignore Spelling: Fmpeg Vid

using Avalonia.Controls.Shapes;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Text.Json.Nodes;


namespace GetFrame.Core.Models;

/// <summary>
/// Represents metadata information about a video file.
/// </summary>
public class VideoMetadata
{

    public VideoServiceErrorCode VideoServiceErrorCode { get; set; } = VideoServiceErrorCode.None;

    public string StatusMessage  { get; set; } = string.Empty;

    private string _filePath = string.Empty;
    public required string FilePath
    {
        get => _filePath;
        set
        {
            if (_filePath != value)
            {
                if (string.IsNullOrWhiteSpace(value))
                    throw new ArgumentException("FilePath cannot be null or whitespace.", nameof(value));
                if (value.IndexOfAny(System.IO.Path.GetInvalidPathChars()) >= 0)
                    throw new ArgumentException("FilePath contains invalid characters.", nameof(value));
                _filePath = value;
            }
        }
    }

    public int Width { get; set; } = 0;
    public int Height { get; set; } = 0;
    public double DurationMs { get; set; } = 0;
    public string Framerate { get; set; } = string.Empty;
    public double Fps { get; set; } = 0.0;
    public long Frames { get; set; } = 0;
    public long FileSize { get; set; } = 0;
    public string FormatFileSize()
    {
        string[] sizes = ["B", "KB", "MB", "GB"];
        double len = FileSize;
        int order = 0;
        while (len >= 1024 && order < sizes.Length - 1)
        {
            order++;
            len /= 1024;
        }
        return $"{len:0.##} {sizes[order]}";
    }

    public string BuildInfoText()
    {
        var duration = TimeSpan.FromMilliseconds(DurationMs);
        var hhmmss = $"{duration:hh\\:mm\\:ss}.{Math.Max(0, Frames - 1):000}";
        return $"{Width}x{Height}, {hhmmss}, Frames [{Frames}], Framerate {Framerate} (FPS{Fps:0.##}), File Size {FormatFileSize()}";
    }

}
