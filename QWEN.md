# GetFrame - Project Context

## Project Overview

GetFrame is a cross-platform desktop and mobile application for extracting frames from video files. It allows users to:
- Open video files (MP4, MKV, AVI, MOV, FLV containers)
- Navigate to a specific frame by number
- Preview the selected frame
- Export the frame as a PNG image

**Platforms:** Windows (desktop) and Android (mobile)  
**UI Framework:** Avalonia UI 11.x with Fluent theme  
**Language:** English (UI and messages)

## Solution Structure

```
GetFrame.slnx                        # Solution file
setup.sh                             # Linux environment setup script
src/
  GetFrame.Core/                     # Shared core project
    ViewModels/                      # MVVM ViewModels (CommunityToolkit.Mvvm)
    Views/                           # Avalonia XAML views
    Models/                          # Data models (VideoMetadata, etc.)
    Services/                        # Service interfaces (IVideoService, ISettingsService)
    Converters/                      # Value converters for bindings
    resources/                       # SVG icons, images (theme-aware)
    App.axaml                        # Application definition
    GetFrame.Core.csproj

  GetFrame.Windows/                  # Windows-specific implementation
    VideoService.cs                  # FFmpeg/FFprobe-based IVideoService
    Program.cs                       # Application entry point
    GetFrame.Windows.csproj

  GetFrame.Android/                  # Android-specific implementation
    VideoService.cs                  # Android IVideoService implementation
    MainActivity.cs                  # Android activity entry point
    GetFrame.Android.csproj
```

## Technology Stack

| Component | Technology |
|-----------|------------|
| Language | C# 13, .NET 9 |
| UI Framework | Avalonia UI 11.3.12 |
| MVVM Toolkit | CommunityToolkit.Mvvm 8.4.0 |
| Graphics | Avalonia.Svg.Skia 11.3.0 |
| Video Processing | FFmpeg / FFprobe (external) |
| Nullable Reference Types | Enabled |
| Implicit Usings | Enabled |

## Architecture

### Core Patterns

- **MVVM with Source Generators:** ViewModels use `[ObservableProperty]` and `[RelayCommand]` from CommunityToolkit.Mvvm
- **Dependency Injection via Static Properties:** `App.VideoService` and `App.SettingsService` are static singletons
- **Platform Abstraction:** `IVideoService` interface defines platform-agnostic video operations
- **Settings Persistence:** `SettingsService` stores key-value pairs in JSON file (`GetFrameSettings.json`)

### Key Components

| File | Purpose |
|------|---------|
| `src/GetFrame.Core/ViewModels/MainWindowViewModel.cs` | Main application logic, frame preview, save operations |
| `src/GetFrame.Core/Views/MainWindow.axaml` | Main UI layout |
| `src/GetFrame.Core/Services/IVideoService.cs` | Platform-agnostic video service interface |
| `src/GetFrame.Core/Services/ISettingsService.cs` | Settings persistence interface |
| `src/GetFrame.Core/Models/VideoMetadata.cs` | Video metadata (resolution, duration, frames, fps) |
| `src/GetFrame.Windows/VideoService.cs` | Windows implementation using FFmpeg/FFprobe |
| `src/GetFrame.Android/VideoService.cs` | Android implementation |

### VideoMetadata Properties

```csharp
string FilePath      // Path to video file
int Width, Height    // Video resolution
double DurationMs    // Duration in milliseconds
string Framerate     // Frame rate as fraction (e.g., "30000/1001")
double Fps           // Frames per second (calculated)
long Frames          // Total frame count
long FileSize        // File size in bytes
```

## Building and Running

### Prerequisites

- .NET 9 SDK
- For Android: `dotnet workload install android`
- For Windows desktop: FFmpeg executable (selected at runtime)
- For Linux development: Run `setup.sh` to install dependencies

### Build Commands

```bash
# Restore dependencies
dotnet restore GetFrame.slnx

# Build all projects
dotnet build GetFrame.slnx

# Build Windows project only
dotnet build src/GetFrame.Windows/GetFrame.Windows.csproj

# Build Android project only
dotnet build src/GetFrame.Android/GetFrame.Android.csproj -c Release
```

### Run Commands

```bash
# Run Windows desktop app
dotnet run --project src/GetFrame.Windows/GetFrame.Windows.csproj

# Deploy Android app (requires Android SDK)
dotnet publish src/GetFrame.Android/GetFrame.Android.csproj -c Release -f net9.0-android
```

### CI/CD

GitHub Actions workflow (`.github/workflows/dotnet-desktop.yml`) builds both Windows and Android targets on push/PR to `master`.

## Development Conventions

### Code Style

- **Indentation:** 4 spaces
- **Brace Style:** Allman for types, K&R for short lambdas
- **Namespaces:** File-scoped namespaces
- **Async:** Use `async`/`await` with `CancellationToken` propagation
- **UI Responsiveness:** Heavy operations run off UI thread; results marshaled via `Dispatcher`

### ViewModel Patterns

```csharp
// Use source generators for properties
[ObservableProperty] private string frameNumberText = "0";

// Use source generators for commands
[RelayCommand(CanExecute = nameof(CanSave))]
private async Task Save() { ... }

// Debounce expensive operations (800ms default)
partial void OnFrameNumberTextChanged(string value)
{
    _debounceTimer.Stop();
    _debounceTimer.Start();
}
```

### Platform-Specific Code

- `GetFrame.Core` must NOT reference platform assemblies
- Platform-specific implementations belong in `GetFrame.Windows` or `GetFrame.Android`
- Extend `IVideoService` interface for new platform features

### Settings Usage

```csharp
// Get a setting
var ffmpegPath = App.SettingsService.GetKey("ffmpegPath");

// Set a setting
App.SettingsService.SetKey("ffmpegPath", "/path/to/ffmpeg");
```

### Error Handling

- Use `VideoServiceErrorCode` enum for platform-specific errors
- Display user-friendly messages via `MainWindowViewModel.StatusText`
- Set `HasError = true` to indicate error state in UI

## Dependencies

### Core Packages (GetFrame.Core.csproj)

- `Avalonia` 11.3.12
- `Avalonia.Themes.Fluent` 11.3.12
- `Avalonia.Svg.Skia` 11.3.0
- `CommunityToolkit.Mvvm` 8.4.0

### Platform Packages

- Windows: `Avalonia.Desktop` 11.3.12
- Android: `Avalonia.Android` 11.3.12

**Note:** Do not add new NuGet packages without strong justification. Shared packages go to `GetFrame.Core.csproj`.

## Testing

No automated tests currently exist. If adding tests:
- Create `src/GetFrame.Tests/` project
- Use xUnit framework
- Mock `IVideoService` for ViewModel tests

## Key Implementation Details

### Frame Extraction (Windows)

Uses FFmpeg command:
```bash
ffmpeg -y -i "video.mp4" -vf "select='eq(n\,FRAME_NUM)'" -frames:v 1 "output.png"
```

### Metadata Retrieval (Windows)

Uses FFprobe command:
```bash
ffprobe -v quiet -print_format json -show_format -count_frames -show_streams "video.mp4"
```

### Debouncing

Frame number input has 800ms debounce before triggering preview extraction.

### Theme Support

- Dark/Light theme toggle via `SettingsService`
- SVG icons are theme-aware (`dark_theme_white/`, `light_theme_black/`)
- Theme stored in settings as `DarkTheme` (boolean)

## Common Tasks

### Adding a New Platform Feature

1. Extend `IVideoService` interface in `GetFrame.Core`
2. Implement in `GetFrame.Windows/VideoService.cs`
3. Implement in `GetFrame.Android/VideoService.cs`
4. Update ViewModel to use new method

### Adding a New Setting

```csharp
// Get
var value = App.SettingsService.GetKey("MySetting") ?? "default";

// Set
App.SettingsService.SetKey("MySetting", value);
```

### Debugging UI Bindings

- Enable `AvaloniaXamlIlDebuggerLaunch` in `.csproj`
- Check `Dispatcher` usage for thread marshaling
- Verify `[ObservableProperty]` generates partial property

## Troubleshooting

| Issue | Solution |
|-------|----------|
| FFmpeg not found | Select path via UI; stored in settings |
| No frames extracted | Check FFmpeg/FFprobe paths; verify video format |
| UI freezes | Ensure heavy work runs off UI thread |
| Android build fails | Run `dotnet workload install android` |

## Related Files

- `.github/copilot-instructions.md` — AI assistant guidelines
- `.vscode/launch.json` — VS Code debug configurations
- `.vscode/tasks.json` — VS Code build tasks
- `setup.sh` — Linux dependency installation script
