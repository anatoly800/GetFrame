# Copilot Instructions for GetFrame

## Project Overview

GetFrame is a cross-platform video frame extraction application targeting **Windows** and **Android**. It allows users to open a video file, select a frame by number, preview it, and export it as a PNG image.

## Solution Structure

```
GetFrame.slnx                    # Solution file
setup.sh                         # Linux environment setup script (run automatically, do not invoke manually)
src/
  GetFrame.Core/                 # Shared core: MVVM ViewModels, Models, Services interfaces, Avalonia UI
  GetFrame.Windows/              # Windows-specific IVideoService implementation (uses FFmpeg/FFprobe)
  GetFrame.Android/              # Android-specific IVideoService implementation
```

Key files:
- `src/GetFrame.Core/Views/MainWindow.axaml` — main UI layout (Avalonia XAML)
- `src/GetFrame.Core/ViewModels/MainWindowViewModel.cs` — main MVVM ViewModel
- `src/GetFrame.Core/Services/IVideoService.cs` — platform service interface
- `src/GetFrame.Windows/VideoService.cs` — Windows implementation
- `src/GetFrame.Android/VideoService.cs` — Android implementation

## Technology Stack

- **Language**: C# 13, .NET 9
- **UI framework**: [Avalonia UI](https://avaloniaui.net/) 11.x with Fluent theme
- **MVVM**: [CommunityToolkit.Mvvm](https://learn.microsoft.com/en-us/dotnet/communitytoolkit/mvvm/) 8.x — use source generators (`[ObservableProperty]`, `[RelayCommand]`)
- **Nullable reference types**: enabled (`<Nullable>enable</Nullable>`)
- **Implicit usings**: enabled

## Coding Conventions

- Follow the existing code style: 4-space indentation, Allman brace style for types, K&R for short lambdas.
- Use `partial` classes and CommunityToolkit.Mvvm source generators for ViewModels — do not write boilerplate `INotifyPropertyChanged` manually.
- All UI text and messages must be in **English**.
- Prefer `async`/`await` with `CancellationToken` propagation for all I/O and heavy operations.
- Keep the UI thread responsive: run heavy work (video decoding, file I/O) off the UI thread and marshal results back via `Dispatcher` or `await`.
- Use file-scoped namespaces.
- Platform-specific code belongs in the platform projects (`GetFrame.Windows`, `GetFrame.Android`), not in `GetFrame.Core`.
- The `IVideoService` interface must remain platform-agnostic.

## Architecture Guidelines

- `GetFrame.Core` must not reference platform assemblies; it only depends on `Avalonia` and `CommunityToolkit.Mvvm`.
- New platform features should be added by extending `IVideoService` and implementing in each platform project.
- The ViewModel owns all application logic; Views contain only layout and data bindings.
- Debounce user input (800 ms) before triggering expensive operations, as already done for frame number changes.

## Build & Test

- **Build**: `dotnet build GetFrame.slnx`
- **Restore**: `dotnet restore GetFrame.slnx`
- **Windows target**: `src/GetFrame.Windows/GetFrame.Windows.csproj`
- **Android target**: `src/GetFrame.Android/GetFrame.Android.csproj`
- There is currently no automated test project. If adding tests, place them under `src/GetFrame.Tests/` and use xUnit.

## Dependencies

- Do not add new NuGet packages without strong justification.
- If a new package is needed, add it to the appropriate `.csproj` (shared packages go to `GetFrame.Core.csproj`).
- Do not change existing package versions unless fixing a security vulnerability.

## Pull Request Requirements

- Reference the related issue in the PR description.
- Ensure `dotnet build` passes for both Windows and Android targets before submitting.
- Do not commit build artifacts (`bin/`, `obj/`) or IDE-specific files.
- Do not commit secrets, credentials, or API keys.
