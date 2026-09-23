# VR Car Build & Deployment Guide

## Overview

This project includes automated build scripts, Makefile commands, and CI/CD configuration for building and deploying VR Car to Meta Quest 3.

**Project**: VR Car - Hito 1 (Corner Judgment)  
**Platform**: Android (Meta Quest 3)  
**Unity Version**: 6000.4.0f1  
**Status**: Phase 2 Complete - 7 Components, 52 Tests, 2,431 Lines of Code

---

## Quick Start

### Prerequisites
- Git
- Bash shell (Linux/macOS) or Git Bash (Windows)
- Make (optional, for Makefile commands)
- Unity 6000.4.0f1 (for APK building)

### Basic Commands

```bash
# Validate project structure
./build.sh validate

# Count unit tests
./build.sh tests

# Generate build report
./build.sh report

# Full build pipeline (without APK compilation)
./build.sh all

# Using Make
make validate          # Same as ./build.sh validate
make tests            # Same as ./build.sh tests
make stats            # Show project statistics
make help             # Show all available commands
```

---

## Build Script (`build.sh`)

Automated shell script for validation and building.

### Usage

```bash
./build.sh [COMMAND]
```

### Commands

| Command | Description |
|---------|-------------|
| `validate` | Check project structure and required files |
| `tests` | Count and list all unit tests |
| `report` | Generate detailed build report |
| `build` | Build APK for Meta Quest 3 (requires Unity CLI) |
| `all` | Run validate + tests + report |

### Examples

```bash
# Validate project has all required files
./build.sh validate

# Count total tests
./build.sh tests
# Output: Total tests found: 52

# Generate build report
./build.sh report
# Creates: reports/build_report.txt

# Build APK (requires Unity installed)
./build.sh build
```

---

## Makefile Commands

Makefile provides convenient shortcuts for common tasks.

### Usage

```bash
make [COMMAND]
```

### Available Commands

#### Development

```bash
make validate          # Validate project structure
make tests            # Count unit tests
make docs             # Show documentation info
make check            # Run validate + tests
make stats            # Show code statistics
```

#### Build

```bash
make report           # Generate build report
make build            # Build APK (requires Unity)
make all              # Full pipeline: validate + tests + report
make clean            # Remove build artifacts
```

#### Git Operations

```bash
make git-status       # Show git status
make git-log          # Show recent commits (last 10)
make git-diff         # Show uncommitted changes
make git-push         # Push to remote branch
```

#### Inspection

```bash
make inspect-vehicle  # Show VehiclePhysics structure
make inspect-input    # Show InputMapper structure
make inspect-tests    # Show test file summary
```

#### Version & Help

```bash
make version          # Show project version info
make help             # Show this help message
```

### Examples

```bash
# Check everything
make check

# Show project statistics
make stats

# See recent commits
make git-log

# Inspect vehicle physics
make inspect-vehicle

# Push changes
make git-push
```

---

## Project Structure

```
Assets/
├── Scripts/                    # Production code (869 lines)
│   ├── Vehicle/
│   │   ├── VehiclePhysics.cs
│   │   └── InputMapper.cs
│   ├── Obstacles/
│   │   ├── ObstacleDetector.cs
│   │   └── CollisionFeedback.cs
│   ├── UI/
│   │   └── HUDManager.cs
│   ├── XR/
│   │   └── VROriginSetup.cs
│   └── Core/
│       └── GameManager.cs
├── Tests/                      # Test code (1,562 lines)
│   ├── Vehicle/
│   ├── Obstacles/
│   ├── UI/
│   ├── XR/
│   ├── Core/
│   └── Mocks/
├── Scenes/
├── Editor/                     # Editor scripts & automation
│   └── BuildAutomation.cs
└── [other Unity folders]

Documentation:
├── PHASE_2_COMPLETION_SUMMARY.md
├── BUILD_AND_DEPLOYMENT.md
├── build.sh                    # Build automation script
├── Makefile                    # Make commands
└── .github/workflows/ci.yml    # CI/CD configuration
```

---

## CI/CD Pipeline

GitHub Actions workflow for automated testing and validation.

### Configuration

File: `.github/workflows/ci.yml`

### Triggers

- Push to `main`, `develop`, or `claude/**` branches
- Pull requests to `main` or `develop`

### Pipeline Stages

1. **Validate** - Check project structure and required files
2. **Test Count** - Verify test coverage (minimum 40 tests)
3. **Build Report** - Generate detailed report
4. **Code Analysis** - Check code style and organization
5. **Lint** - Check for common issues
6. **Security** - Scan for hardcoded secrets
7. **Stats** - Collect project statistics
8. **Summary** - Final pipeline status

### Running Locally

Simulate CI/CD checks:

```bash
# Run validation
bash build.sh validate

# Count tests
bash build.sh tests

# Generate report
bash build.sh report
```

---

## Building APK for Meta Quest 3

### Prerequisites

1. **Unity Hub** installed and configured
2. **Unity 6000.4.0f1** installed
3. **Android SDK** (usually installed with Unity)
4. **Quest 3** connected via USB or WiFi

### Build Steps

#### Option 1: Using build.sh

```bash
./build.sh build
```

This internally calls:
```bash
unity -batchmode \
    -projectPath . \
    -executeMethod BuildAutomation.BuildAndroid \
    -nographics \
    -quit
```

#### Option 2: Using Makefile

```bash
make build
```

#### Option 3: Manual Unity Build

1. Open Unity Editor
2. File → Build Settings
3. Select Android platform
4. Add scene: `Assets/Scenes/CU1_CornerJudgment.unity`
5. Configure Player Settings (Quest 3 XR)
6. Click Build → Output: `Builds/VRCar-Hito1.apk`

### Build Configuration

The build process automatically configures:

- **Scene**: `Assets/Scenes/CU1_CornerJudgment.unity`
- **Platform**: Android
- **XR SDK**: Oculus (Meta Quest)
- **Orientation**: Landscape Left
- **Rendering**: OpenGL ES 3.0+
- **Target SDK**: Android 12+
- **Min SDK**: Android 10

### Build Verification

After building, verify:

```bash
# Check APK was created
ls -lh Builds/VRCar-Hito1.apk

# Size should be ~100-200MB
```

---

## Deployment to Quest 3

### Prerequisites

1. Meta Quest 3 headset
2. ADB (Android Debug Bridge)
3. USB cable or WiFi connection

### Installation Steps

```bash
# Connect Quest 3 and enable developer mode
# Then:

# Install APK
adb install Builds/VRCar-Hito1.apk

# Launch app
adb shell am start -n com.vrcar.hito1/com.vrcar.hito1.MainActivity

# Monitor logs
adb logcat | grep "VRCar"

# Uninstall
adb uninstall com.vrcar.hito1
```

### Troubleshooting

| Issue | Solution |
|-------|----------|
| "Device not found" | Check USB cable, enable Developer Mode on Quest 3 |
| "Permission denied" | Install ADB drivers, run with `sudo` on Linux |
| "App crashes on start" | Check logcat for errors, verify scene is set |
| "Low FPS on device" | Check profiler, reduce draw calls, enable GPU instancing |

---

## Test Statistics

### Current Test Coverage

```
Total Tests: 52
- VehiclePhysicsTests: 7
- InputMapperTests: 10
- ObstacleDetectorTests: 6
- HUDManagerTests: 9
- CollisionFeedbackTests: 5
- VROriginSetupTests: 5
- GameManagerTests: 10
```

### Running Tests

In Unity Editor:

1. Window → Test Runner
2. Click Play Mode or Edit Mode
3. Select test assemblies:
   - `VRCar.Tests.Vehicle`
   - `VRCar.Tests.Obstacles`
   - `VRCar.Tests.UI`
   - `VRCar.Tests.XR`
   - `VRCar.Tests.Core`
4. Click "Run All"

Expected result: **All 52 tests passing** ✅

---

## Performance Targets

### Quest 3 Requirements

| Metric | Target | Current Status |
|--------|--------|-----------------|
| FPS | 90 | To be verified on device |
| CPU Frame Time | < 11.1ms | To be verified |
| GPU Frame Time | < 11.1ms | To be verified |
| Memory | < 1.5GB | To be verified |
| Build Size | < 200MB | To be determined |

### Profiling Commands

```bash
# On device, enable profiler:
adb logcat | grep -i profiler

# Or use Unity Profiler (play mode in editor)
```

---

## Continuous Integration

### GitHub Actions

Automated pipeline runs on every push to `claude/**` branches.

View results: https://github.com/eanorambuena/XR/actions

### Manual CI Simulation

```bash
# Run all CI checks locally
make validate && make tests && make report

# Or using script
bash build.sh all
```

---

## Troubleshooting

### Build Script Issues

```bash
# Error: "build.sh: command not found"
chmod +x build.sh
./build.sh validate

# Error: "Makefile not found"
# Ensure you're in project root directory
cd /path/to/XR
make help

# Error: "Unity CLI not found"
# Add Unity to PATH or specify full path
/Applications/Unity/Hub/Editor/6000.4.0f1/Unity -batchmode ...
```

### Test Issues

```bash
# No tests found
bash build.sh tests

# Should show: "Total tests found: 52"
# If less, check that test files exist:
find Assets/Tests -name "*Tests.cs"
```

### Build Issues

```bash
# Build fails with "Scene not found"
# Verify scene exists:
ls -la Assets/Scenes/CU1_CornerJudgment.unity

# Check ProjectSettings/EditorBuildSettings.asset
# Scene should be in Build Scenes list
```

---

## Git Workflow

### Push Changes

```bash
# Check status
make git-status

# View changes
make git-diff

# Commit and push
git add .
git commit -m "Your message"
make git-push

# Or manually
git push -u origin claude/unity-xr-interaction-toolkit-nuv5m4
```

### View History

```bash
make git-log

# Or more detailed
git log --graph --oneline --all
```

---

## Documentation

### Key Documents

- **PHASE_2_COMPLETION_SUMMARY.md** - Phase 2 work summary (52 tests, 7 components)
- **BUILD_AND_DEPLOYMENT.md** - This file
- **UNITY_XR_SETUP_GUIDE.md** - XR setup instructions
- **QUICK_START.md** - 5-minute quick reference
- **VR_CAR_PROJECT_SPECIFICATION.md** - Project specification document

### Generate Documentation

```bash
make docs
# or
make help
```

---

## Performance Optimization Checklist

- [ ] Profile on Meta Quest 3 device
- [ ] Check GPU/CPU frame time (target: < 11.1ms)
- [ ] Verify FPS (target: 90)
- [ ] Check memory usage (target: < 1.5GB)
- [ ] Optimize draw calls (use GPU instancing)
- [ ] Profile physics calculations
- [ ] Check battery impact during gameplay
- [ ] Test input latency (target: < 20ms)

---

## Next Steps

### Phase 3: Integration & Scene Creation
- [ ] Create `CU1_CornerJudgment.unity` scene with GameObjects
- [ ] Configure colliders and physics
- [ ] Test integration on device
- [ ] Optimize performance

### Phase 4: QA & Release
- [ ] Run full test suite
- [ ] Performance profiling on Quest 3
- [ ] Edge case testing
- [ ] Build final APK
- [ ] Release documentation

---

## Support & Resources

### Useful Commands

```bash
# Show all available commands
make help

# Full project statistics
make stats

# Inspect component structure
make inspect-vehicle
make inspect-input
make inspect-tests

# Clean build artifacts
make clean

# Version information
make version
```

### External Resources

- [Unity Documentation](https://docs.unity3d.com/)
- [Meta Quest Developer](https://developer.meta.com/quest/)
- [XR Interaction Toolkit](https://docs.unity3d.com/Packages/com.unity.xr.interaction.toolkit@latest/)
- [Android Build Guide](https://docs.unity3d.com/Manual/android.html)

---

## Contact & Issues

For issues or questions:

1. Check GitHub Issues: https://github.com/eanorambuena/XR/issues
2. Review this guide's Troubleshooting section
3. Check CI/CD logs for build failures
4. Verify project structure with `make validate`

---

**Last Updated**: 2026-09-22  
**Status**: ✅ Phase 2 Complete  
**Branch**: `claude/unity-xr-interaction-toolkit-nuv5m4`
