# Installation & Setup Guide - VR Car Hito 1

**For**: Meta Quest 3 XR Development  
**Unity Version**: 6000.4.0f1 (LTS)  
**Platform**: Linux, macOS, Windows

---

## Quick Start (5 minutes)

### Option A: Already Have Unity Installed

```bash
# 1. Clone or open the project
cd /path/to/XR

# 2. Open in Unity 6000.4.0f1
# File → Open Project → Select this folder

# 3. Create scene automatically
# Menu: VRCar → Setup CU1 Scene

# Done! Scene is ready for testing
```

### Option B: Need to Install Unity

Follow the installation steps below for your platform.

---

## Installation by Platform

### 🐧 Linux (Ubuntu 20.04+)

```bash
# 1. Install dependencies
sudo apt-get update
sudo apt-get install -y \
  build-essential \
  libxrender1 \
  libxrandr2 \
  libxi6 \
  libgconf-2-4 \
  libglu1-mesa \
  openjdk-11-jdk

# 2. Download Unity 6000.4.0f1
# Visit: https://unity.com/download
# Or use Unity Hub:
wget https://public-cdn.cloud.unity3d.com/hub/prod/UnityHubSetup.AppImage
chmod +x UnityHubSetup.AppImage
./UnityHubSetup.AppImage

# 3. Or download directly (if you have login token):
# Download from: https://unity.com/download/lts/6000.4

# 4. Extract and install
tar -xzf Unity_6000.4.0f1_Linux.tar.gz
cd Unity-6000.4.0f1
./Editor/Unity -quit

# 5. Install Android SDK (required for Quest)
mkdir -p ~/Android/sdk
# Download Android SDK from:
# https://developer.android.com/studio/install#linux

# 6. Verify installation
unity --version
```

### 🍎 macOS (12.0+)

```bash
# 1. Install Homebrew (if not already)
/bin/bash -c "$(curl -fsSL https://raw.githubusercontent.com/Homebrew/install/HEAD/install.sh)"

# 2. Install dependencies
brew install openjdk@11

# 3. Download Unity 6000.4.0f1
# Option A: Use Unity Hub
brew install --cask unity-hub

# Option B: Direct download
# Visit: https://unity.com/download/lts/6000.4
# Download: UnityDownloadAssistant-2022.3.0.dmg

# 4. Install Unity
# Run the installer or use Hub to install 6000.4.0f1

# 5. Install Android SDK (required for Quest)
brew install android-sdk

# 6. Verify
unity --version
```

### 🪟 Windows 10/11

```powershell
# 1. Install dependencies
# - Visual Studio 2019+ with C++ support (or just VS Build Tools)
# - Java 11+ (OpenJDK 11)

# Download from:
# https://www.oracle.com/java/technologies/javase/jdk11-archive-downloads.html

# 2. Download Unity 6000.4.0f1
# Visit: https://unity.com/download/lts/6000.4
# Download: UnityDownloadAssistant-6000.4.0f1.exe

# 3. Run installer
# Accept default paths or customize

# 4. Install Android SDK
# Download: https://developer.android.com/studio
# Install Android Studio or just the SDK

# 5. Verify installation
unity --version
```

---

## Post-Installation Setup

### 1. Configure Android SDK

```bash
# Linux/macOS
export ANDROID_HOME=~/Android/sdk
export PATH=$ANDROID_HOME/tools:$ANDROID_HOME/tools/bin:$PATH

# Windows (PowerShell)
$env:ANDROID_HOME = "C:\Users\YourUsername\AppData\Local\Android\sdk"
$env:PATH += ";$env:ANDROID_HOME\tools;$env:ANDROID_HOME\tools\bin"
```

**Via Android Studio:**
1. Open Android Studio
2. SDK Manager
3. SDK Platforms: Install API 31+ (for Quest 3)
4. SDK Tools: Install Android SDK Build-Tools, NDK

### 2. Configure Unity

```bash
# Launch Unity and configure:
# Edit → Preferences → External Tools

# Set these paths:
Android SDK Path: /path/to/Android/sdk
Android NDK Path: /path/to/ndk/android-ndk-r23
Java Development Kit: /path/to/openjdk-11
Gradle: /path/to/gradle
```

### 3. Create/Open VR Car Project

```bash
# Open Unity
unity

# File → Open Project
# Select: /path/to/XR

# Wait for project to load (~1-2 minutes first time)
```

### 4. Generate Scene

**In Unity Editor Menu:**
```
VRCar → Setup CU1 Scene
```

This runs `Assets/Editor/SceneSetup.cs` which:
- Creates `Assets/Scenes/CU1_CornerJudgment.unity`
- Sets up XROrigin, Vehicle, Obstacles, UI
- Initializes GameManager
- Scene is ready to test

### 5. Verify Installation

```bash
# In Unity, run validation:
# Menu: VRCar → XRValidation.ValidateXRConfiguration()

# Expected output:
# ✓ PASSED (18 checks)
# ✓ VR Support enabled for Android
# ✓ Oculus SDK enabled
# ✓ Graphics API: OpenGL ES 3.0+
# ✓ Target FPS: 90
```

---

## Automated Setup Script (Post-Install)

Once Unity is installed, you can run full setup automatically:

```bash
cd /path/to/XR

# Generate scene and build report
./build.sh all

# Expected output:
# ✅ Project validation passed
# ✅ Scene generated
# ✅ 62 tests counted
# ✅ Build report created
```

---

## Troubleshooting Installation

### Issue: "Unity command not found"

**Solution 1: Add to PATH (Linux/macOS)**
```bash
# Add to ~/.bashrc or ~/.zshrc:
export PATH="/path/to/Unity/Editor:$PATH"

# Or create symlink:
sudo ln -s /path/to/Unity/Editor/Unity /usr/local/bin/unity
```

**Solution 2: Use full path**
```bash
/path/to/Unity/Editor/Unity --version
```

### Issue: "Android SDK not found"

**Solution:**
```bash
# Verify path exists
ls -la $ANDROID_HOME/

# Set correct path
export ANDROID_HOME=/path/to/Android/sdk

# Or in Unity Editor:
# Edit → Preferences → External Tools
# Set "Android SDK Path" manually
```

### Issue: "Java not found"

**Solution:**
```bash
# Install JDK 11+
java -version

# Verify JAVA_HOME
echo $JAVA_HOME

# Set if needed:
export JAVA_HOME=/path/to/java11
```

### Issue: Project won't open in Unity

**Solution 1: Check .gitignore**
```bash
# Regenerate Library folder
rm -rf Library
rm -rf Temp

# Unity will rebuild when opening
```

**Solution 2: Update Unity**
```bash
unity --version
# If not 6000.4.0f1, update via Unity Hub
```

---

## Automated Scene Generation

If you have issues generating scene manually:

```bash
# Use automated script
cd /home/user/XR

# This runs the scene setup via Unity CLI:
./build.sh validate

# Then open in editor and run:
# VRCar → Setup CU1 Scene
```

---

## Build APK for Meta Quest 3

After scene is set up:

```bash
# Build APK automatically
./build.sh build

# Or manually in Unity:
# File → Build Settings
# Platform: Android
# Scene: CU1_CornerJudgment.unity
# Player Settings: Verified by XRValidation
# Click: Build

# Output: Builds/VRCar-Hito1.apk (~150-200MB)
```

---

## Minimum System Requirements

| Component | Requirement |
|-----------|-------------|
| **OS** | Windows 10+, macOS 12+, Ubuntu 20.04+ |
| **RAM** | 8GB minimum (16GB recommended) |
| **Disk** | 15GB free for Unity + SDKs |
| **Processor** | Intel i5 / AMD Ryzen 5 or better |
| **GPU** | NVIDIA GTX 960 / AMD R9 290 or better |
| **Java** | OpenJDK 11+ |
| **Android SDK** | API 21+ (API 31+ for Quest 3) |

---

## Verification Checklist

After installation, verify:

```
[ ] Unity 6000.4.0f1 installed
    unity --version

[ ] Android SDK installed
    ls -la $ANDROID_HOME/

[ ] Android NDK installed
    ls -la $ANDROID_HOME/ndk/

[ ] Java 11+ installed
    java -version

[ ] Gradle installed
    gradle --version

[ ] Project opens without errors
    unity (launch editor)

[ ] Scene generates successfully
    VRCar → Setup CU1 Scene

[ ] Validation passes
    VRCar → XRValidation.ValidateXRConfiguration()
```

---

## Quick Links

| Resource | URL |
|----------|-----|
| **Unity Download** | https://unity.com/download/lts/6000.4 |
| **Unity Hub** | https://unity.com/download |
| **Android SDK** | https://developer.android.com/studio |
| **OpenJDK 11** | https://adoptopenjdk.net/ |
| **Meta Quest Dev** | https://developer.meta.com/quest |

---

## Support & Debugging

### Get Help
```bash
# Show all build commands
./build.sh help

# Show installation info
./build.sh validate

# Generate detailed report
./build.sh report

# Check for issues
make check
```

### Debug Mode (Verbose Output)

```bash
# Run with verbose logging
./build.sh build -v

# Or in Unity Editor:
# Help → Report a Bug...
# Window → Console (for error messages)
```

---

## Next Steps

1. **Install Unity** (follow platform-specific guide above)
2. **Open project** in Unity 6000.4.0f1
3. **Create scene** via menu: VRCar → Setup CU1 Scene
4. **Verify setup** via: VRCar → XRValidation.ValidateXRConfiguration()
5. **Test locally** in Play Mode
6. **Build APK** via: ./build.sh build
7. **Deploy to Quest 3** via: adb install Builds/VRCar-Hito1.apk

---

**Version**: 1.0  
**Last Updated**: 2026-09-22  
**Status**: Ready for Production Installation
