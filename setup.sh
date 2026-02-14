#!/bin/bash
set -e

echo "--- 1. Setup dependencies for Avalonia ---"
sudo apt-get update
sudo apt-get install -y \
    wget curl unzip git ca-certificates \
    libicu-dev libfontconfig1 libx11-6

# --- 2. Check and install Java 21 ---
echo "--- 2. Checking Java ---"
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

echo "--- 3. Installing .NET SDK 8.0 and 9.0 ---"
sudo apt-get install -y dotnet-sdk-8.0 dotnet-sdk-9.0

echo "--- 4. Setting up Android SDK ---"
export ANDROID_HOME=$HOME/android-sdk
mkdir -p $ANDROID_HOME/cmdline-tools

# Download Command Line Tools (current link for 2025)
# Note: Google sometimes changes the version ID in the link
CMD_LINE_TOOLS_URL="https://dl.google.com/android/repository/commandlinetools-linux-11076708_latest.zip"
wget -q $CMD_LINE_TOOLS_URL -O cmdline-tools.zip
unzip -q cmdline-tools.zip -d $ANDROID_HOME/cmdline-tools
rm cmdline-tools.zip

# Adjust folder structure for sdkmanager
if [ -d "$ANDROID_HOME/cmdline-tools/cmdline-tools" ]; then
    mv $ANDROID_HOME/cmdline-tools/cmdline-tools $ANDROID_HOME/cmdline-tools/latest
fi

# Set paths for the current session
export PATH=$PATH:$ANDROID_HOME/cmdline-tools/latest/bin:$ANDROID_HOME/platform-tools

echo "--- 5. Accepting licenses and installing Android components ---"
yes | sdkmanager --licenses
# platforms;android-34 for .NET 8, android-35 for .NET 9
sdkmanager "platform-tools" "platforms;android-34" "platforms;android-35" "build-tools;34.0.0"

echo "--- 6. Installing .NET workloads for Android ---"
sudo dotnet workload install android

echo "--- 7. Saving environment variables ---"
# Use a separator to avoid duplicate entries on subsequent runs
if ! grep -q "ANDROID_HOME" ~/.bashrc; then
    {
        echo "export JAVA_HOME=$JAVA_HOME"
        echo "export ANDROID_HOME=$ANDROID_HOME"
        echo "export PATH=\$PATH:\$ANDROID_HOME/cmdline-tools/latest/bin:\$ANDROID_HOME/platform-tools"
    } >> ~/.bashrc
fi

echo "--------------------------------------------------------"
echo "Setup complete! To apply the paths, run:"
echo "source ~/.bashrc"