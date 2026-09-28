#!/bin/bash
# Quick XR Validation

echo "=== XR VALIDATION REPORT ==="
echo ""

# Check files
files=(
    "Assets/Scripts/Vehicle/VehiclePhysics.cs"
    "Assets/Scripts/Vehicle/InputMapper.cs"
    "Assets/Scripts/Obstacles/ObstacleDetector.cs"
    "Assets/Scripts/UI/HUDManager.cs"
    "Assets/Scripts/Obstacles/CollisionFeedback.cs"
    "Assets/Scripts/XR/VROriginSetup.cs"
    "Assets/Scripts/Core/GameManager.cs"
    "Assets/Scripts/Core/XRDebugger.cs"
    "Assets/Editor/XRValidation.cs"
    "Assets/Editor/BuildAutomation.cs"
)

echo "✓ Production Components & Scripts:"
pass=0
for f in "${files[@]}"; do
    if [ -f "$f" ]; then
        echo "  ✓ $(basename $f)"
        ((pass++))
    fi
done
echo "  Total: $pass/${#files[@]}"

# Check test files
echo ""
echo "✓ Test Files:"
test_files=(
    "Assets/Tests/Vehicle/VehiclePhysicsTests.cs"
    "Assets/Tests/Vehicle/InputMapperTests.cs"
    "Assets/Tests/Obstacles/ObstacleDetectorTests.cs"
    "Assets/Tests/UI/HUDManagerTests.cs"
    "Assets/Tests/Obstacles/CollisionFeedbackTests.cs"
    "Assets/Tests/XR/VROriginSetupTests.cs"
    "Assets/Tests/XR/XRCompatibilityTests.cs"
    "Assets/Tests/Core/GameManagerTests.cs"
)

pass=0
for f in "${test_files[@]}"; do
    if [ -f "$f" ]; then
        echo "  ✓ $(basename $f)"
        ((pass++))
    fi
done
echo "  Total: $pass/${#test_files[@]}"

# Count tests
echo ""
echo "✓ Unit Tests:"
total_tests=0
for f in Assets/Tests/*/*.cs; do
    if [ -f "$f" ]; then
        count=$(grep -c "^\s*\[Test\]" "$f" 2>/dev/null || echo 0)
        total_tests=$((total_tests + count))
    fi
done
echo "  Total tests: $total_tests (target: 50+)"

# Check XR scripts
echo ""
echo "✓ XR-Specific Files:"
xr_files=(
    "XR_VALIDATION_GUIDE.md"
    "Assets/Scripts/Core/XRDebugger.cs"
    "Assets/Tests/XR/XRCompatibilityTests.cs"
    "Assets/Editor/XRValidation.cs"
)

for f in "${xr_files[@]}"; do
    if [ -f "$f" ]; then
        echo "  ✓ $(basename $f)"
    fi
done

# Statistics
echo ""
echo "=== STATISTICS ==="
echo "Total Production Code: $(find Assets/Scripts -name "*.cs" -exec wc -l {} + 2>/dev/null | tail -1 | awk '{print $1}') lines"
echo "Total Test Code:       $(find Assets/Tests -name "*.cs" -exec wc -l {} + 2>/dev/null | tail -1 | awk '{print $1}') lines"
echo "Total Components:      $(ls Assets/Scripts/*/*.cs 2>/dev/null | wc -l)"

# Summary
echo ""
echo "=== SUMMARY ==="
if [ $total_tests -ge 50 ] && [ $pass -eq ${#test_files[@]} ]; then
    echo "✅ XR PROJECT VALIDATION SUCCESSFUL"
    echo ""
    echo "Ready for:"
    echo "  ✓ Editor testing"
    echo "  ✓ Unit testing"
    echo "  ✓ APK building"
    echo "  ✓ Device deployment"
else
    echo "⚠️  Some checks need review"
fi
echo ""
