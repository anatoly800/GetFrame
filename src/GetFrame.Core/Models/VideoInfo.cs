namespace GetFrame.Models;

public sealed record VideoInfo(
    string Path,
    int Width,
    int Height,
    double DurationMs,
    double Fps,
    int TotalFrames,
    string Codec);
