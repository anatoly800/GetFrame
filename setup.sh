#!/bin/bash
set -euo pipefail

sudo apt-get update
sudo apt-get install -y wget curl unzip git ca-certificates libicu-dev

# --- Java 21 ---
echo "--- Checking Java ---"
if command -v java >/dev/null 2>&1; then
    JAVA_VER=$(java -version 2>&1 | head -n 1 | cut -d'"' -f2 | cut -d'.' -f1)
    if [ "$JAVA_VER" -ge 21 ]; then
        echo "Found JDK version $JAVA_VER."
    else
        echo "Found JDK $JAVA_VER, installing OpenJDK 21..."
        sudo apt-get install -y openjdk-21-jdk
    fi
else
    echo "Java not found. Installing OpenJDK 21..."
    sudo apt-get install -y openjdk-21-jdk
fi

export JAVA_HOME
JAVA_HOME=$(readlink -f "$(which java)" | sed "s:/bin/java::")
echo "JAVA_HOME set to: $JAVA_HOME"

# Project targets net9.0 / net9.0-android, so SDK 9 is mandatory.
echo "--- Installing .NET SDK 9.0 ---"
sudo apt-get install -y dotnet-sdk-9.0

# Optional: keep SDK 10 available too (already used in some images/pipelines).
echo "--- Installing .NET SDK 10.0 (optional) ---"
sudo apt-get install -y dotnet-sdk-10.0 || true

# NuGet source configuration.
# If a mirror is available, pass NUGET_SOURCE_URL env var before running setup.sh.
if [ -n "${NUGET_SOURCE_URL:-}" ]; then
    echo "--- Configuring NuGet mirror source ---"
    dotnet nuget remove source getframe-mirror >/dev/null 2>&1 || true
    dotnet nuget add source "$NUGET_SOURCE_URL" --name getframe-mirror
fi

echo "--- Setting up Android SDK ---"
export ANDROID_HOME="$HOME/android-sdk"
mkdir -p "$ANDROID_HOME/cmdline-tools"

CMD_LINE_TOOLS_URL="https://dl.google.com/android/repository/commandlinetools-linux-11076708_latest.zip"
wget -q "$CMD_LINE_TOOLS_URL" -O cmdline-tools.zip
unzip -q cmdline-tools.zip -d "$ANDROID_HOME/cmdline-tools"
rm cmdline-tools.zip

if [ -d "$ANDROID_HOME/cmdline-tools/cmdline-tools" ]; then
    mv "$ANDROID_HOME/cmdline-tools/cmdline-tools" "$ANDROID_HOME/cmdline-tools/latest"
fi

export PATH="$PATH:$ANDROID_HOME/cmdline-tools/latest/bin:$ANDROID_HOME/platform-tools"

echo "--- Accepting licenses and installing Android components ---"
yes | sdkmanager --licenses
sdkmanager "platform-tools" "platforms;android-35" "build-tools;35.0.0"

echo "--- Installing .NET workloads for Android ---"
dotnet workload update
dotnet workload install android

echo "--- Saving environment variables ---"
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
