# XR Validation Guide - VR Car Hito 1

**Purpose**: Complete guide to validate that VR Car functions correctly for Meta Quest 3  
**Target**: 90 FPS, < 20ms latency, smooth VR experience  
**Status**: Comprehensive validation tools available

---

## 🎯 Pre-Build Validation

### 1. Editor Validation (XRValidation.cs)

The project includes automated XR validation script. Run in Unity Editor:

#### Via Menu
```
Assets → Editor → XRValidation
```

#### Via Code
```csharp
XRValidation.ValidateXRConfiguration();
```

**Validates**:
- ✅ Player Settings (VR enabled, Oculus SDK)
- ✅ XR Management configuration
- ✅ Input System setup
- ✅ Scene configuration (XROrigin, Main Camera)
- ✅ Required components (all 7 components)
- ✅ Build Settings (Android, scenes)
- ✅ Performance settings (quality, physics, GPU instancing)

**Expected Output**:
```
✓ PASSED (18 checks)
✓ VR Support enabled for Android
✓ Oculus (Meta Quest) SDK enabled
✓ Graphics API: OpenGL ES 3.0+
✓ Target FPS: 90
...
```

---

## 🧪 Unit Tests for XR

### XR Compatibility Tests (52 Total)

All tests include specific XR validation. Run in Unity Test Runner:

#### Test Categories

**Phase 2 Tests** (52 existing)
- VehiclePhysicsTests - 7 tests
- InputMapperTests - 10 tests
- ObstacleDetectorTests - 6 tests
- HUDManagerTests - 9 tests
- CollisionFeedbackTests - 5 tests
- VROriginSetupTests - 5 tests
- GameManagerTests - 10 tests

**XR Compatibility Tests** (10 new)
- InputMapper_XRTriggers_MapCorrectly - Trigger input validation
- InputMapper_XRJoystick_SteeringWorks - Joystick mapping
- InputMapper_ContinuousXRInput_NoLatency - Input latency
- VehiclePhysics_XRFrameRate_MatchesQuest3 - 90 FPS physics
- HUD_XRDisplay_RenderCorrectly - World-space UI
- XROriginSetup_HeightConfiguration - VR positioning
- VehiclePhysics_XRPerformance_LowLatency - Physics performance
- InputMapper_XRLatency_WithinBudget - Input response time
- Components_XRStability_NoMemoryLeaks - Memory safety
- AllComponents_XRInitialization_NoErrors - Initialization

### Running XR Tests

```
Window → Test Runner → Play Mode
Select: VRCar.Tests.XR.XRCompatibilityTests
Click: Run All
```

**Expected**: All 10 tests pass ✅

---

## 📊 Runtime Debugging

### XR Debugger (XRDebugger.cs)

Real-time performance monitoring during gameplay.

#### Enable in Scene

Add to vehicle GameObject:
```csharp
vehicleGameObject.AddComponent<XRDebugger>();
```

#### Toggle Debug Display

Press **'D'** key during gameplay to toggle debug overlay

#### Displays

**Frame Rate**
- Current FPS (target: 90)
- Frame time in ms
- Fixed delta time

**XR Device**
- Headset name and model
- Connection status
- VR enabled status
- Eye texture resolution

**Input**
- Right trigger value (0-1)
- Left trigger value (0-1)
- Right joystick (X, Y)
- Mapped vehicle inputs

**Vehicle Physics**
- Current speed (m/s and km/h)
- Maximum speed limit
- Position (X, Y, Z)
- Rotation (heading angle)

**Tracking**
- Headset position
- Head rotation
- Tracking status (ACTIVE/LOST)

**Performance Warnings**
- ⚠️ Low FPS warning (< 85)
- 🔴 Frame spikes
- Memory usage

---

## 🔍 Pre-Build Checklist

### Project Settings

Run XRValidation to verify:

```
✓ Player Settings
  □ VR Support: Enabled for Android
  □ XR SDKs: Contains "Oculus"
  □ Orientation: Landscape Left
  □ Graphics APIs: OpenGL ES 3.0+
  □ VSync: Disabled
  □ Target FPS: 90

✓ XR Management
  □ XR Loaders: Configured
  □ OpenXR Runtime: Enabled

✓ Input System
  □ New Input System: Enabled
  □ Input Actions: Configured

✓ Build Settings
  □ Platform: Android
  □ Scenes: CU1_CornerJudgment.unity
  □ Min SDK: API 21+
```

### Performance Settings

```
✓ Quality Settings
  □ Level: Appropriate for Quest 3
  □ GPU Instancing: Enabled where possible
  □ Physics Timestep: 1/90s (0.01111s)
```

### Scene Setup

```
✓ GameObject Hierarchy
  □ XROrigin present
  □ Camera as child of XROrigin
  □ Canvas: World Space
  □ Vehicle with colliders
  □ Obstacles with "Obstacle" tag
```

---

## 🏗️ Build Verification

### Before APK Build

1. **Run Editor Validation**
   ```
   XRValidation.ValidateXRConfiguration()
   ```
   Expected: ✅ All checks pass

2. **Run All Tests**
   ```
   Unity Test Runner → Play Mode
   Run All → Expected: 62+ tests pass
   ```

3. **Check Build Settings**
   ```
   File → Build Settings
   - Platform: Android
   - Scenes: CU1_CornerJudgment.unity
   - Player Settings: Verified by XRValidation
   ```

4. **Profile Performance**
   ```
   Window → Analysis → Profiler
   Play Mode → Capture 5-10 seconds
   Check: CPU, GPU, Memory
   ```

### Build Command

```bash
# Using automated script
./build.sh validate    # Verify structure
./build.sh report      # Generate report

# If all pass, build APK:
./build.sh build       # Compiles for Quest 3
```

### APK Verification

```bash
# Check file size (should be 100-200MB)
ls -lh Builds/VRCar-Hito1.apk

# Check for errors in build log
# APK should contain:
#   - XR plugins (Oculus/Meta)
#   - All scripts compiled
#   - Scene data
#   - Assets
```

---

## 📱 Device Validation (Meta Quest 3)

### Installation

```bash
# Connect Quest 3 with USB
adb install Builds/VRCar-Hito1.apk

# Launch app
adb shell am start -n com.vrcar.hito1/com.vrcar.hito1.MainActivity

# Monitor logs
adb logcat | grep "VRCar\|Unity"
```

### Real-Device Testing Checklist

#### Input Testing
- [ ] Right trigger controls acceleration (0-100%)
- [ ] Left trigger controls braking (0-100%)
- [ ] Right joystick controls steering (left/center/right)
- [ ] No input lag (immediate response)

#### Physics Testing
- [ ] Vehicle accelerates smoothly
- [ ] Speed increases up to 72 km/h (20 m/s)
- [ ] Braking works and slows vehicle
- [ ] Steering rotates vehicle smoothly
- [ ] No physics jitter or instability

#### Display Testing
- [ ] HUD is visible and readable
- [ ] Distance displayed correctly
- [ ] Speed displayed in km/h
- [ ] Colors change: Green → Yellow → Red
- [ ] Text is clear from both eyes

#### Tracking Testing
- [ ] Head tracking works (look around)
- [ ] No tracking loss or jumpiness
- [ ] Controllers track correctly
- [ ] No latency between head/hand movement and display

#### Performance Testing
- [ ] Maintains 90 FPS (no frame drops)
- [ ] Smooth motion (no stuttering)
- [ ] No thermal throttling
- [ ] Battery drain acceptable (< 10% per 30 min)

#### Obstacle Detection
- [ ] Detects obstacles ahead
- [ ] Distance calculation accurate
- [ ] Detects from multiple angles
- [ ] Ignores non-Obstacle objects

#### Collision Feedback
- [ ] Visual feedback on collision (color change)
- [ ] Audio feedback on collision
- [ ] Only responds to Obstacle tag

---

## 🐛 Debugging in VR

### Debug Display (Press 'D' in game)

Shows real-time:
- **FPS**: Should be 90 (or very close)
- **Headset Status**: Should show connected headset
- **Input Values**: Trigger and joystick values (0-1)
- **Vehicle Data**: Speed, position, rotation
- **Tracking**: Head position and rotation
- **Performance**: Warnings if FPS drops

### Expected Values During Gameplay

| Metric | Expected | Warning Threshold |
|--------|----------|-------------------|
| FPS | 90 | < 85 |
| Frame Time | ~11.1ms | > 13ms |
| Latency | < 20ms | > 25ms |
| Head Position | ~(0, 1.7, 0) | Varies by tracking |
| Trigger Range | 0.0-1.0 | Out of range = error |
| Joystick Range | -1.0 to 1.0 | Out of range = error |

### Performance Profiling

1. **CPU Performance**
   ```
   Window → Profiler → CPU Usage
   Expected: < 16ms per frame at 90 FPS
   ```

2. **GPU Performance**
   ```
   Window → Profiler → GPU Usage
   Expected: < 5ms per frame (varies by device)
   ```

3. **Memory**
   ```
   Window → Profiler → Memory
   Expected: < 1.5GB used
   ```

---

## ⚙️ Common Issues & Solutions

### Issue: Low FPS (< 90)

**Causes**:
- Too many draw calls
- Heavy physics calculations
- Memory pressure

**Solutions**:
```
1. Check Profiler (CPU/GPU)
2. Reduce quality settings
3. Enable GPU instancing
4. Profile physics calculations
5. Check memory usage
```

### Issue: Input Lag

**Causes**:
- XR Input not polling
- Physics timestep mismatch
- Update/FixedUpdate ordering

**Solutions**:
```
1. Verify InputMapper.Update() called every frame
2. Check Time.fixedDeltaTime = 1/90 (0.01111s)
3. Profile input latency with debugger
4. Check for blocking operations in Update
```

### Issue: Tracking Loss

**Causes**:
- Poor lighting conditions
- Reflective surfaces
- Camera obstruction

**Solutions**:
```
1. Check lighting (well-lit space)
2. Check for reflective surfaces
3. Verify controllers visible to headset
4. Recenter headset (press menu button)
```

### Issue: Collision Detection Not Working

**Causes**:
- Objects missing "Obstacle" tag
- Colliders not configured
- Physics layers misconfigured

**Solutions**:
```
1. Verify obstacle tags:
   Edit → Project Settings → Tags
   Add "Obstacle" tag
   Assign to test obstacles
   
2. Check colliders:
   Obstacle must have Collider component
   Vehicle must have Collider component
   
3. Physics settings:
   Window → Physics
   Check collision matrix
```

### Issue: HUD Not Visible

**Causes**:
- Canvas in wrong render mode
- Canvas position outside view
- Text component missing

**Solutions**:
```
1. Check Canvas render mode:
   Should be: World Space (for VR)
   NOT: Screen Space Overlay
   
2. Check Canvas position:
   Should be ~1.5m in front of camera
   
3. Check TextMeshPro:
   Component should exist
   Font should be assigned
```

---

## 📋 Validation Checklist for Release

### Pre-Release Validation

```
Editor Validation
□ Run XRValidation.ValidateXRConfiguration()
□ All checks pass (no critical issues)
□ Verify all 7 components present

Unit Testing
□ Run all 62 tests
□ Expected: 100% pass rate
□ No failures or errors

Performance
□ Profile in editor (90 FPS target)
□ CPU frame time < 11ms
□ Memory < 1.5GB
□ GPU performance acceptable

Build Verification
□ Build validates successfully
□ APK creates without errors
□ APK size 100-200MB
□ No missing assets or scripts

Device Testing (on Quest 3)
□ App installs without errors
□ Launches successfully
□ Maintains 90 FPS
□ All inputs respond correctly
□ Tracking works without issues
□ HUD displays correctly
□ Physics and collision work

Documentation
□ This guide reviewed
□ All edge cases tested
□ Performance profiling complete
□ Issues documented and resolved
```

---

## 🚀 Final Validation Steps

### 1. Clean Build
```bash
# Remove old build
make clean

# Full validation pipeline
./build.sh all

# Expected output:
# ✅ Validation passed
# ✅ 52+ tests found
# ✅ Report generated
```

### 2. Profile Performance
```
- Launch game in editor with Profiler
- Maintain 90 FPS for 1+ minute
- Monitor CPU/GPU/Memory
- Note any spikes or issues
```

### 3. Test on Device
```bash
# Install APK
adb install Builds/VRCar-Hito1.apk

# Play for 5+ minutes
# Verify all features work
# Check FPS with debug display (D key)
# Test all inputs thoroughly
```

### 4. Document Results
```
✅ Editor validation passed
✅ 62 tests passing
✅ Performance acceptable
✅ Device testing successful
✅ Ready for release
```

---

## 📞 Support & Debugging Resources

### Useful Tools
- **XRValidation.cs** - Editor validation
- **XRCompatibilityTests.cs** - Automated tests
- **XRDebugger.cs** - Runtime debugging
- **build.sh** - Build validation
- **Profiler** - Performance analysis

### Key Files
- Unity Project Settings: `ProjectSettings/`
- Input Configuration: `Assets/InputActions/`
- Player Settings: `ProjectSettings/ProjectSettings.asset`

### Testing Commands
```bash
# Validate project
./build.sh validate

# Run tests
# (in Unity Test Runner)

# Generate report
./build.sh report

# Build APK
./build.sh build
```

---

**Version**: 1.0  
**Last Updated**: 2026-09-22  
**Target Device**: Meta Quest 3  
**Target FPS**: 90  
**Target Latency**: < 20ms
