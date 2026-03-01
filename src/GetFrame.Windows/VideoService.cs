// Ignore Spelling: Ffmpeg Vid

using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Media.Imaging;
using Avalonia.Platform.Storage;

using System.Diagnostics;
using System.Globalization;
using System.Text;
using System.Text.Json.Nodes;

using GetFrame.Core.Models;
using GetFrame.Core.Services;

namespace GetFrame.Windows;

public delegate void ProgressCallback(int value, string statusMessage, string progressColor);

public partial class VideoService : IVideoService
{

    public async Task<string?> GetVideoFilePath()
    {
        var topLevel = TopLevel.GetTopLevel(Application.Current?.ApplicationLifetime switch
        {
            IClassicDesktopStyleApplicationLifetime desktop => desktop.MainWindow,
            ISingleViewApplicationLifetime single => single.MainView,
            _ => null
        });

        if (topLevel == null)
            return null;

        var options = new FilePickerOpenOptions
        {
            Title = "Select a video file",
            AllowMultiple = false,
            FileTypeFilter =
            [
                new FilePickerFileType("Video Files")
            {
                Patterns = ["*.mp4", "*.mkv", "*.avi", "*.mov", "*.flv"]
            },
            new FilePickerFileType("All Files")
            {
                Patterns = ["*"]
            }
            ]
        };

        var result = await topLevel.StorageProvider.OpenFilePickerAsync(options);

        if (result == null || result.Count == 0)
            return null;

        return result[0].Path.LocalPath;
    }

    public async Task<VideoMetadata> GetVideoInfoAsync(string path, CancellationToken cancellationToken)
    {
        return await GetVideoMetadataAsync(path, await GetFFprobePath(), null, cancellationToken);
    }

    public async Task<Bitmap> GetFrameAsync(string path, int frameIndex, CancellationToken cancellationToken)
    {


        var ffmpegPath = await GetFFmpegPath();
        if (string.IsNullOrWhiteSpace(ffmpegPath))
        {
            throw new InvalidOperationException("FFmpeg path is not configured.");
        }

        var tempFramePath = await ExtractFrameForPreview(path, ffmpegPath, frameIndex, cancellationToken);
        if (tempFramePath == null || !File.Exists(tempFramePath))
        {
            throw new InvalidOperationException("Failed to extract frame from video.");
        }

        try
        {
            using var stream = File.OpenRead(tempFramePath);
            return new Bitmap(stream);
        }
        finally
        {
            try
            {
                if (File.Exists(tempFramePath))
                {
                    File.Delete(tempFramePath);
                }
            }
            catch
            {
                // Best effort cleanup
            }
        }
    }

    public async Task SaveFrameAsPngAsync(string path, int frameIndex, string outputPngPath, CancellationToken cancellationToken)
    {
        var ffmpegPath = await GetFFmpegPath();
        if (string.IsNullOrWhiteSpace(ffmpegPath))
        {
            throw new InvalidOperationException("FFmpeg path is not configured.");
        }

        if (!File.Exists(path))
        {
            throw new FileNotFoundException($"Video file not found: {path}");
        }

        if (frameIndex < 0)
        {
            throw new ArgumentException("Frame index cannot be negative.", nameof(frameIndex));
        }

        var startInfo = new ProcessStartInfo
        {
            FileName = ffmpegPath,
            Arguments = $"-y -i \"{path}\" -vf \"select='eq(n\\,{frameIndex})'\" -frames:v 1 \"{outputPngPath}\"",
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true
        };

        using var process = new Process { StartInfo = startInfo };
        try
        {
            process.Start();

            var outputTask = process.StandardOutput.ReadToEndAsync(cancellationToken);
            var errorTask = process.StandardError.ReadToEndAsync(cancellationToken);

            await process.WaitForExitAsync(cancellationToken);

            var output = await outputTask;
            var error = await errorTask;

            if (process.ExitCode != 0)
            {
                throw new InvalidOperationException($"FFmpeg failed with exit code {process.ExitCode}: {error}");
            }

            if (!File.Exists(outputPngPath))
            {
                throw new InvalidOperationException("Frame extraction succeeded but output file was not created.");
            }
        }
        catch (OperationCanceledException)
        {
            try
            {
                if (!process.HasExited)
                {
                    process.Kill();
                }
            }
            catch
            {
                // Ignore cleanup errors
            }
            throw;
        }
    }

    private static async Task<string> GetFFmpegPath()
    {
        var ffmpegPath = GetFrame.Core.App.SettingsService.GetKey("ffmpegPath");
        if (!string.IsNullOrWhiteSpace(ffmpegPath) && File.Exists(ffmpegPath))
        {
            return ffmpegPath;
        }

        var topLevel = TopLevel.GetTopLevel(Application.Current?.ApplicationLifetime switch
        {
            IClassicDesktopStyleApplicationLifetime desktop => desktop.MainWindow,
            ISingleViewApplicationLifetime single => single.MainView,
            _ => null
        });

        if (topLevel == null)
            return string.Empty;

        var options = new FilePickerOpenOptions
        {
            Title = "Select a ffmpeg path",
            AllowMultiple = false,
            FileTypeFilter =
            [
                new FilePickerFileType("FFmpeg")
            {
                Patterns = ["*.exe"]
            },
            new FilePickerFileType("All Files")
            {
                Patterns = ["*"]
            }
            ]
        };

        var result = await topLevel.StorageProvider.OpenFilePickerAsync(options);

        if (result == null || result.Count == 0)
        {
            return string.Empty;
        }

        GetFrame.Core.App.SettingsService.SetKey("ffmpegPath", result[0].Path.LocalPath);

        return result[0].Path.LocalPath;
    }

    private static async Task<string> GetFFprobePath()
    {
        var ffmpegPath = await GetFFmpegPath() ?? throw new InvalidOperationException("FFmpeg path is not configured.");
        var ffmpegDirectory = Path.GetDirectoryName(ffmpegPath) ?? throw new InvalidOperationException("Failed to determine FFmpeg directory.");
        var ffprobePath = Path.Combine(ffmpegDirectory, "ffprobe.exe");
        if (!File.Exists(ffprobePath))
        {
            throw new InvalidOperationException("FFprobe path is not configured.");
        }
        return ffprobePath;
    }

    public static async Task<VideoMetadata> GetVideoMetadataAsync(
        string filePath,
        string ffprobePath,
        ProgressCallback? progressCallback,
        CancellationToken cancellationToken)
    {
        progressCallback?.Invoke(0, "Starting metadata retrieval...", "Blue");
        var metadata = new VideoMetadata
        {
            FilePath = filePath,
            FileSize = new FileInfo(filePath).Length,
        };

        try
        {
            progressCallback?.Invoke(1, "Checking FFprobe executable...", "Blue");
            if (string.IsNullOrEmpty(ffprobePath))
            {
                metadata.StatusMessage = "FFprobe path is not set in settings.";
                metadata.VideoServiceErrorCode = VideoServiceErrorCode.FfprobeNotFound;
                return metadata;
            }

            if (!File.Exists(ffprobePath))
            {
                metadata.StatusMessage = $"FFprobe executable not found at: {ffprobePath}";
                metadata.VideoServiceErrorCode = VideoServiceErrorCode.FfprobeNotFound;
                return metadata;
            }

            if (!File.Exists(filePath))
            {
                metadata.StatusMessage = $"File not found at: {filePath}";
                metadata.VideoServiceErrorCode = VideoServiceErrorCode.FileNotFound;
                return metadata;
            }

            var startInfo = new ProcessStartInfo
            {
                FileName = ffprobePath,
                Arguments = $"-v quiet -print_format json -show_format -count_frames -show_streams \"{filePath}\"",
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            };
            progressCallback?.Invoke(1, "Retrieving video metadata...", "Green");

            using var process = new Process { StartInfo = startInfo };
            process.Start();

            using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            string output = "";
            string error = "";

            try
            {
                var outputTask = process.StandardOutput.ReadToEndAsync(cts.Token);
                var errorTask = process.StandardError.ReadToEndAsync(cts.Token);
                int progress = 50;
                bool increasing = true;

                int pointCount = 3;
                while (!process.HasExited)
                {
                    await Task.Delay(400, cts.Token);

                    cts.Token.ThrowIfCancellationRequested();

                    if (increasing)
                    {
                        progress += 2;
                        if (progress >= 90)
                        {
                            progress = 90;
                            increasing = false;
                        }
                    }
                    else
                    {
                        progress -= 2;
                        if (progress <= 50)
                        {
                            progress = 50;
                            increasing = true;
                        }
                    }

                    string dots = new('.', pointCount--);
                    if (pointCount < 1) pointCount = 3;
                    progressCallback?.Invoke(progress, $"Analyzing video metadata{dots}", "Blue");
                }

                await Task.WhenAll(outputTask, errorTask);

                output = await outputTask;
                error = await errorTask;
            }
            catch (OperationCanceledException)
            {
                try
                {
                    if (!process.HasExited)
                        process.Kill();
                }
                catch
                {
                    // ignore
                }

                metadata.StatusMessage = "Operation timed out or was cancelled.";
                metadata.VideoServiceErrorCode = VideoServiceErrorCode.OperationCancelled;
                return metadata;
            }

            if (process.ExitCode != 0)
            {
                metadata.StatusMessage = $"ffprobe failed with exit code {process.ExitCode}: {error}";
                metadata.VideoServiceErrorCode = VideoServiceErrorCode.FfprobeFailed;
                return metadata;
            }

            progressCallback?.Invoke(95, "Parsing metadata...", "Green");
            var jsonObject = JsonNode.Parse(output);
            if (jsonObject == null)
            {
                metadata.StatusMessage = "Failed to parse ffprobe output";
                metadata.VideoServiceErrorCode = VideoServiceErrorCode.FfprobeFailed;
                return metadata;
            }

            var streams = jsonObject["streams"] as JsonArray;
            if (streams is not null)
            {
                for (int i = 0; i < streams.Count; i++)
                {
                    var stream = streams[i];
                    if (stream?["codec_type"]?.ToString() == "video")
                    {

                        if (long.TryParse(stream["width"]?.ToString(), out var width) &&
                            long.TryParse(stream["height"]?.ToString(), out var height))
                        {
                            metadata.Width = (int)width;
                            metadata.Height = (int)height;
                        }

                        metadata.Framerate = stream["r_frame_rate"]?.ToString() ?? "N/A";

                        if (metadata.Framerate != "N/A")
                        {
                            if (metadata.Framerate.Contains('/'))
                            {
                                var parts = metadata.Framerate.Split('/');
                                if (parts.Length == 2 &&
                                    double.TryParse(parts[0], NumberStyles.Any, CultureInfo.InvariantCulture, out var numerator) &&
                                    double.TryParse(parts[1], NumberStyles.Any, CultureInfo.InvariantCulture, out var denominator) &&
                                    denominator != 0)
                                {
                                    metadata.Fps = numerator > 0 && denominator > 0 ? numerator / denominator : 0;
                                }
                            }
                            else if (double.TryParse(metadata.Framerate, NumberStyles.Any, CultureInfo.InvariantCulture, out var fps))
                            {
                                metadata.Fps = fps;
                            }
                        }

                        var duration = stream["duration"]?.ToString() ?? "N/A";

                        metadata.DurationMs = double.TryParse(duration, out var durationMs) ? durationMs * 1000 : 0;

                        if (long.TryParse(stream["nb_read_frames"]?.ToString(), out var readFrames))
                        {
                            metadata.Frames = readFrames;
                        }
                        else
                        {
                            if (long.TryParse(stream["nb_frames"]?.ToString(), out var frames))
                            {
                                metadata.Frames = frames;
                            }
                        }

                        break; // Take first video stream
                    }
                }
            }
            progressCallback?.Invoke(100, "Metadata retrieval complete", "Green");
        }
        catch (Exception ex)
        {
            metadata.StatusMessage = $"Error occurred while analyzing video: {ex.Message}";
            metadata.VideoServiceErrorCode = VideoServiceErrorCode.FfprobeFailed;
        }
        return metadata;
    }

    public static async Task<string?> FFmpegService_ExtractFrameRange(
        string videoFilePath,
        string ffmpegPath,
        long frameStart,
        long frameEnd,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(ffmpegPath))
        {

            return "FFmpeg path is not set.";
        }

        if (!File.Exists(ffmpegPath))
        {

            return "FFmpeg executable not found at the specified path. [" + ffmpegPath + "]";
        }

        if (!File.Exists(videoFilePath))
        {
            return "Video file does not exist. [" + videoFilePath + "]";
        }

        var directory = Path.GetDirectoryName(videoFilePath);
        if (!Directory.Exists(directory))
        {
            return "Output directory does not exist: [" + directory + "]";
        }

        if (frameStart < 0 || frameEnd < frameStart)
        {
            return $"Invalid frame range: start={frameStart}, end={frameEnd}";
        }

        var baseName = Path.GetFileNameWithoutExtension(videoFilePath);
        var outputFile = Path.Combine(directory, $"{baseName}_frame_{frameStart}-{frameEnd}_%03d.png");

        var startInfo = new ProcessStartInfo
        {
            FileName = ffmpegPath,
            Arguments = $"-y -i \"{videoFilePath}\" -vf \"select='between(n\\,{frameStart}\\,{frameEnd})'\" -vsync 0 \"{outputFile}\"",
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true
        };

        using var process = new Process { StartInfo = startInfo };
        try
        {
            process.Start();

            var outputTask = process.StandardOutput.ReadToEndAsync(cancellationToken);
            var errorTask = process.StandardError.ReadToEndAsync(cancellationToken);

            await process.WaitForExitAsync(cancellationToken);

            var output = await outputTask;
            var error = await errorTask;

            if (process.ExitCode != 0)
            {
                return $"FFmpeg failed with exit code {process.ExitCode}: {error}";
            }

            return outputFile;
        }
        catch (OperationCanceledException)
        {
            try
            {
                if (!process.HasExited)
                {
                    process.Kill();
                }
            }
            catch (Exception ex)
            {
                return "Failed to cancel FFmpeg process. " + ex.Message;
            }
            return "Operation was cancelled.";
        }
    }

    /// <summary>
    /// Extracts a single frame from a video file to a temporary location for preview purposes.
    /// NOTE: The caller is responsible for cleaning up the temporary file after use.
    /// </summary>
    /// <param name="videoFilePath">Path to the video file</param>
    /// <param name="frameNumber">Frame number to extract</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Path to the extracted frame image file, or null if extraction failed</returns>
    public static async Task<string?> ExtractFrameForPreview(
        string videoFilePath,
        string ffmpegPath,
        long frameNumber,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(ffmpegPath) || !File.Exists(ffmpegPath))
        {
            return null;
        }

        if (!File.Exists(videoFilePath))
        {
            return null;
        }

        if (frameNumber < 0)
        {
            return null;
        }

        // Create a temporary file for the preview frame
        var tempPath = Path.GetTempPath();
        var outputFile = Path.Combine(tempPath, $"vided_preview_frame_{frameNumber}_{Guid.NewGuid():N}.png");

        var startInfo = new ProcessStartInfo
        {
            FileName = ffmpegPath,
            Arguments = $"-y -i \"{videoFilePath}\" -vf \"select='eq(n\\,{frameNumber})'\" -frames:v 1 \"{outputFile}\"",
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true
        };

        using var process = new Process { StartInfo = startInfo };
        try
        {
            process.Start();

            var outputTask = process.StandardOutput.ReadToEndAsync(cancellationToken);
            var errorTask = process.StandardError.ReadToEndAsync(cancellationToken);

            await process.WaitForExitAsync(cancellationToken);

            var output = await outputTask;
            var error = await errorTask;

            if (process.ExitCode != 0 || !File.Exists(outputFile))
            {
                return null;
            }

            return outputFile;
        }
        catch (OperationCanceledException)
        {
            try
            {
                if (!process.HasExited)
                {
                    process.Kill();
                }
            }
            catch
            {
                // Ignore cleanup errors
            }
            return null;
        }
        catch
        {
            return null;
        }
    }

    private static string GetOutputVideoFileName(
        string srcFile,
        string outputVideoFolder,
        string extension = ".mp4")
    {

        outputVideoFolder = Path.GetFullPath(outputVideoFolder);
        if (string.IsNullOrEmpty(outputVideoFolder))
        {
            throw new ArgumentException($"Invalid output video folder path.${outputVideoFolder}");
        }

        try
        {
            if (!Directory.Exists(outputVideoFolder))
            {
                Directory.CreateDirectory(outputVideoFolder);
            }
        }
        catch (Exception ex)
        {
            throw new IOException($"Failed to create output video folder: {outputVideoFolder}", ex);
        }

        string baseFileName = "output_video";
        try
        {
            var fileName = Path.GetFileNameWithoutExtension(srcFile);
            if (!string.IsNullOrEmpty(fileName))
            {
                baseFileName = fileName;
            }
        }
        catch
        {
        }

        do
        {
            var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
            var fileName = Path.Combine(outputVideoFolder, $"{baseFileName}_{timestamp}{extension}");
            if (!File.Exists(fileName))
            {
                return fileName;
            }
        } while (true);
    }

    private static async Task<string> ReadFfmpegProgressAsync(
        Process process,
        ProgressCallback? progressCallback,
        double totalDurationMicroseconds,
        CancellationToken cancellationToken)
    {
        var outputBuilder = new StringBuilder();

        while (true)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var line = await process.StandardOutput.ReadLineAsync(cancellationToken);
            if (line is null)
            {
                break;
            }

            outputBuilder.AppendLine(line);

            var separatorIndex = line.IndexOf('=');
            if (separatorIndex <= 0)
            {
                continue;
            }

            var key = line[..separatorIndex];
            var value = line[(separatorIndex + 1)..];

            if (key == "out_time_ms" && totalDurationMicroseconds > 0)
            {
                if (double.TryParse(value, NumberStyles.Any, CultureInfo.InvariantCulture, out var outTimeUs))
                {
                    var percent = (int)Math.Clamp(outTimeUs / totalDurationMicroseconds * 100.0, 2, 99);
                    progressCallback?.Invoke(percent, "Processing", "Green");
                }
            }
        }

        return outputBuilder.ToString();
    }

    /// <summary>
    /// Encodes a video from a sequence of image files using FFmpeg concat demuxer.
    /// </summary>
    public static async Task<string> FFmpegService_EncodeAsync(
        string[] imageFilePaths,
        string ffmpegPath,
        int framerate,
        string outputVideoFileExtension,
        string encoderPreset,
        int encoderCrf,
        ProgressCallback? progressCallback,
        CancellationToken cancellationToken = default)
    {
        Process? process = null;

        try
        {
            progressCallback?.Invoke(1, "Initializing", "Green");

            if (string.IsNullOrWhiteSpace(ffmpegPath) || !File.Exists(ffmpegPath))
            {
                throw new FileNotFoundException($"FFmpeg executable not found at: {ffmpegPath ?? "null"}");
            }

            if (imageFilePaths == null || imageFilePaths.Length == 0)
            {
                throw new ArgumentException("No image files specified.", nameof(imageFilePaths));
            }

            // Calculate duration per frame
            double duration = 1.0 / framerate;
            string durationString = duration.ToString("0.000000", CultureInfo.InvariantCulture);
            var totalDurationMicroseconds = imageFilePaths.Length * duration * 1_000_000d;

            string newVideoFile;
            var listFilePath = Path.GetTempFileName();
            var tmpOutputFile = Path.GetTempFileName() + outputVideoFileExtension;
            try
            {

                progressCallback?.Invoke(2, "Processing. Press to cancel", "Green");

                using (var writer = new StreamWriter(listFilePath, false, new UTF8Encoding(false)))
                {
                    foreach (var imgPath in imageFilePaths)
                    {
                        // Escape paths for FFmpeg concat syntax
                        // Standard escape: replace backslash with forward slash, escape single quotes
                        var safePath = imgPath.Replace("\\", "/").Replace("'", "'\\''");

                        await writer.WriteLineAsync($"file '{safePath}'");
                        await writer.WriteLineAsync($"duration {durationString}");
                    }

                    // FFmpeg Concat quirk: The last image needs to be repeated without duration
                    // or just repeated to ensure it's displayed for the correct time.
                    if (imageFilePaths.Length > 0)
                    {
                        var lastPath = imageFilePaths.Last().Replace("\\", "/").Replace("'", "'\\''");
                        await writer.WriteLineAsync($"file '{lastPath}'");
                    }
                }

                // 4. Arguments
                // -preset fast: Speeds up encoding slightly without much quality loss
                // -crf 23: Standard quality constant rate factor
                if (string.IsNullOrWhiteSpace(encoderPreset))
                {
                    encoderPreset = "fast";
                }
                // -movflags +faststart: Optimizes MP4 for web streaming (optional but recommended)
                var arguments = $"-y -f concat -safe 0 -i \"{listFilePath}\" " +
                                $"-c:v libx264 -preset {encoderPreset} -crf {encoderCrf} -r {framerate} " +
                                     $"-pix_fmt yuv420p -progress pipe:1 -nostats \"{tmpOutputFile}\"";

                var startInfo = new ProcessStartInfo
                {
                    FileName = ffmpegPath,
                    Arguments = arguments,
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true
                };

                process = new Process { StartInfo = startInfo };

                // 5. Execution Handling
                if (!process.Start())
                {
                    throw new InvalidOperationException("Failed to start FFmpeg process.");
                }

                var progressTask = ReadFfmpegProgressAsync(process, progressCallback, totalDurationMicroseconds, cancellationToken);
                var errorTask = process.StandardError.ReadToEndAsync(cancellationToken);

                await process.WaitForExitAsync(cancellationToken);

                var outputLog = await progressTask;
                var errorLog = await errorTask;

                if (process.ExitCode != 0)
                {
                    // Combine logs for better debugging
                    throw new InvalidOperationException($"FFmpeg failed (Exit Code {process.ExitCode}).\nError: {errorLog}\nOutput: {outputLog}");
                }

                newVideoFile = GetOutputVideoFileName(imageFilePaths[0], outputVideoFileExtension);
                // move temp output to final location
                File.Move(tmpOutputFile, newVideoFile);
            }
            finally
            {
                // Cleanup temp file
                if (File.Exists(listFilePath))
                {
                    try { File.Delete(listFilePath); } catch { /* best effort */ }
                }
                // cleanup temp output if something went wrong
                if (File.Exists(tmpOutputFile))
                {
                    try { File.Delete(tmpOutputFile); } catch { /* best effort */ }
                }
            }

            progressCallback?.Invoke(100, "Success", "Green");
            return newVideoFile; // Success
        }
        catch (OperationCanceledException)
        {
            var cancelMessage = "Operation was cancelled.";
            if (process is not null && !process.HasExited)
            {
                try
                {
                    process.Kill(entireProcessTree: true);
                }
                catch (Exception ex)
                {
                    cancelMessage = $"Operation was cancelled, but failed to stop FFmpeg: {ex.Message}";
                }
            }

            progressCallback?.Invoke(1, "Cancelled", "Gray");
            return cancelMessage;
        }
        catch (Exception ex)
        {
            progressCallback?.Invoke(1, "Error", "Red");
            return ex.Message; // Return error message
        }
        finally
        {
            process?.Dispose();
        }
    }
}
