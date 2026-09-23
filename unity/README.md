# XR Driving Simulator - Unity Project

A VR driving simulator prototype for Meta Quest 3 to teach real-world vehicle perception (size judgment and distance estimation).

## Prerequisites

- **Unity Editor:** 6000.4.0f1 (required)
- **Platform:** Meta Quest 3 (Android)
- **XR Runtime:** OpenXR

## Project Structure

```
unity/
├── Assets/
│   ├── Scripts/
│   │   ├── Core/          # GameManager, XRDebugger
│   │   ├── Vehicle/       # VehiclePhysics, InputMapper
│   │   ├── Obstacles/     # ObstacleDetector, CollisionFeedback
│   │   ├── UI/            # HUDManager
│   │   └── XR/            # VROriginSetup
│   ├── Tests/             # NUnit test suite (~50 tests)
│   ├── Editor/            # Build automation, validation
│   └── InputActions/      # VRCarInput.inputactions
├── QUICK_START.md         # 5-minute setup guide
├── INSTALL_AND_SETUP.md   # Detailed installation
├── BUILD_AND_DEPLOYMENT.md  # Build & deployment steps
└── ... other docs
```

## Quick Start

1. **Clone/open in Unity 6000.4.0f1**
2. **Install XR packages** (see `QUICK_START.md`):
   - `com.unity.xr.management`
   - `com.unity.xr.core-utils`
   - `com.unity.xr.interaction.toolkit`
   - `com.unity.inputsystem`
3. **Run tests** via **Window → Test Runner**
4. **Play scenes** or **build for Quest 3**

## Test Suite

~50 unit tests validate:
- Vehicle physics (acceleration, braking, steering)
- Input mapping (XR controller triggers/joystick)
- Obstacle detection (raycasting)
- HUD updates (distance/speed display)
- Collision feedback (visual/audio)
- XR compatibility

Run with **Window → General → Test Runner → Play Mode** (or **Edit Mode** for faster iteration).

## Building for Meta Quest 3

See `BUILD_AND_DEPLOYMENT.md` for:
- Platform-specific build settings
- APK signing
- Deployment via ADB
- Testing on device

## Validation Tools

- **VRCar → Validate XR Configuration** (Editor menu) — checks XR setup
- `./validate-xr.sh` — bash script for CI/validation

## Known Limitations (In Scope)

- Simplified vehicle physics (no transmission, gear shifting, or realistic tire friction)
- No AI traffic or complex traffic rules
- Basic 1:1 scale perception (no AR scaling or size distortion effects)
- 3 use cases only: corner judgment, obstacle approach, basic circuit driving

## Documentation

- **QUICK_START.md** — 5-minute setup
- **INSTALL_AND_SETUP.md** — Full installation walkthrough
- **VR_CAR_PROJECT_SPECIFICATION.md** — Detailed SDD
- **UNITY_XR_SETUP_GUIDE.md** — XR-specific setup
- **XR_VALIDATION_GUIDE.md** — Validation checklist
- **BUILD_AND_DEPLOYMENT.md** — Build & deployment
- **TROUBLESHOOTING_AND_BEST_PRACTICES.md** — Common issues & fixes
- **SESSION_FINAL_SUMMARY.md** — Final handoff summary

## Contributing

- Follow the existing script and test structure
- Run **Window → Test Runner** before committing
- Validate XR configuration via **VRCar → Validate XR Configuration**
- Use input system overrides for testing (MockXRController)

---

**Last updated:** September 2026
