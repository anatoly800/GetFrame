#!/bin/bash
set -e

echo "--- Updating system and installing basic dependencies ---"
sudo apt-get update
sudo apt-get install -y \
    wget \
    curl \
    unzip \
    git \
    libicu-dev \
    libfontconfig1 \
    libx11-6 \
    ca-certificates

# 1. Setup .NET SDK (8.0 or 9.0)
# On Ubuntu 24.04, it is recommended to use system repositories
echo "--- Installing .NET SDK 8.0/9.0 ---"
sudo apt-get install -y dotnet-sdk-8.0
# If you need .NET 9.0, uncomment the line below:
# sudo apt-get install -y dotnet-sdk-9.0

# Check Java 21 ---
echo "--- Checking Java ---"
if command -v java >/dev/null 2>&1; then
    # Extract major version (e.g., "21" from "21.0.1")
    JAVA_VER=$(java -version 2>&1 | head -n 1 | cut -d'"' -f2 | cut -d'.' -f1)
    if [ "$JAVA_VER" -ge 21 ]; then
        echo "Found JDK version $JAVA_VER. No installation required."
    else
        echo "Found JDK version $JAVA_VER, but 21+ is required. Installing..."
        sudo apt-get install -y openjdk-21-jdk
    fi
else
    echo "Java not found. Installing OpenJDK 21..."
    sudo apt-get install -y openjdk-21-jdk
fi

# Find JAVA_HOME dynamically
export JAVA_HOME=$(readlink -f $(which java) | sed "s:/bin/java::")
echo "JAVA_HOME set to: $JAVA_HOME"

# 3. Setup Android SDK
echo "--- Setting up Android SDK ---"
export ANDROID_HOME=$HOME/android-sdk
mkdir -p $ANDROID_HOME/cmdline-tools

# Download the latest Command Line Tools
# The URL may change, check https://developer.android.com/studio#downloads
CMD_LINE_TOOLS_URL="https://dl.google.com/android/repository/commandlinetools-linux-11076708_latest.zip"
wget -q $CMD_LINE_TOOLS_URL -O cmdline-tools.zip
unzip -q cmdline-tools.zip -d $ANDROID_HOME/cmdline-tools
rm cmdline-tools.zip

# Important: folder structure must be cmdline-tools/latest/bin/...
mv $ANDROID_HOME/cmdline-tools/cmdline-tools $ANDROID_HOME/cmdline-tools/latest

# Setup paths
export PATH=$PATH:$ANDROID_HOME/cmdline-tools/latest/bin:$ANDROID_HOME/platform-tools

# 4. Accept licenses and install Android SDK components
echo "--- Installing Android SDK components ---"
yes | sdkmanager --licenses
sdkmanager "platform-tools" "platforms;android-34" "build-tools;34.0.0"

# 5. Install .NET Workload for Android
echo "--- Installing .NET Workload for Android ---"
# The command requires sudo if the SDK is installed globally
sudo dotnet workload install android

if! grep -q "ANDROID_HOME" ~/.bashrc; then
    {
        echo "export JAVA_HOME=$JAVA_HOME"
        echo "export ANDROID_HOME=$ANDROID_HOME"
        echo "export PATH=\$PATH:\$ANDROID_HOME/cmdline-tools/latest/bin:\$ANDROID_HOME/platform-tools"
    } >> ~/.bashrc
fi

echo "--------------------------------------------------------"
echo "Installation completed successfully!"
echo "Apply the changes: source ~/.bashrc"
echo "Build the project: dotnet build -f net8.0-android -c Release"