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
