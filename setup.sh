#!/bin/bash
set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
SOLUTION_PATH="$SCRIPT_DIR/GetFrame.slnx"

# Setup .NET SDK
echo "Setting up .NET SDK..."

# Determine Ubuntu version and guard unsupported values.
if [ -f /etc/os-release ]; then
    . /etc/os-release
    UBUNTU_VERSION="${VERSION_ID:-22.04}"
else
    echo "Warning: Could not determine Ubuntu version, defaulting to 22.04"
    UBUNTU_VERSION="22.04"
fi

case "$UBUNTU_VERSION" in
    20.04|22.04|24.04) ;;
    *)
        echo "Warning: Ubuntu $UBUNTU_VERSION is not explicitly supported by this script. Falling back to 24.04 feed."
        UBUNTU_VERSION="24.04"
        ;;
esac

echo "Using Microsoft package feed for Ubuntu version: $UBUNTU_VERSION"

apt-get update
apt-get install -y wget

if ! command -v dotnet >/dev/null 2>&1; then
    wget "https://packages.microsoft.com/config/ubuntu/$UBUNTU_VERSION/packages-microsoft-prod.deb" -O /tmp/packages-microsoft-prod.deb
    dpkg -i /tmp/packages-microsoft-prod.deb
    apt-get update
    apt-get install -y dotnet-sdk-9.0
else
    echo "dotnet is already installed: $(dotnet --version)"
fi

# Setup Avalonia dependencies (graphics framework)
echo "Installing system dependencies for Avalonia..."
apt-get install -y \
    libasound2-dev \
    libgtk-3-dev \
    libglib2.0-dev \
    libfontconfig1-dev \
    libfreetype6-dev \
    libpng-dev \
    libjpeg-dev \
    libcups2-dev \
    libavahi-client-dev \
    libkrb5-dev \
    libx11-dev \
    libx11-xcb-dev \
    libxcb1-dev \
    libxrandr-dev \
    libxinerama-dev \
    libxcursor-dev \
    libxi-dev \
    libxss-dev \
    libwayland-dev \
    libegl1-mesa-dev \
    libgbm-dev

# Setup Android SDK for building Android (optional)
echo "Setting up Android SDK (optional)..."
apt-get install -y openjdk-11-jdk android-tools-adb

# Setup necessary NuGet packages
echo "Restoring project..."
dotnet restore "$SOLUTION_PATH"

echo "Setup complete!"
