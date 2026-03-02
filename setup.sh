#!/bin/bash

# Setup .NET SDK
echo "Setting up .NET SDK..."

# Automatically determine Ubuntu version
if [ -f /etc/os-release ]; then
    . /etc/os-release
    UBUNTU_VERSION=$VERSION_ID
else
    echo "Warning: Could not determine Ubuntu version, defaulting to 22.04"
    UBUNTU_VERSION="22.04"
fi

echo "Detected Ubuntu version: $UBUNTU_VERSION"

apt-get update && apt-get install -y wget
wget https://packages.microsoft.com/config/ubuntu/$UBUNTU_VERSION/packages-microsoft-prod.deb -O packages-microsoft-prod.deb
dpkg -i packages-microsoft-prod.deb
apt-get update && apt-get install -y dotnet-sdk-9.0

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
dotnet restore /workspace/GetFrame.slnx

echo "Setup complete!"