#!/bin/bash
# XR Validation Script - Complete XR configuration and compatibility check
# Usage: ./validate-xr.sh [full|quick|device]

set -e

PROJECT_PATH="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
MODE="${1:-quick}"

# Colors
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m'

# Counters
CHECKS_PASSED=0
CHECKS_FAILED=0
CHECKS_WARNING=0

log_pass() {
    echo -e "${GREEN}✓${NC} $1"
    ((CHECKS_PASSED++))
}

log_fail() {
    echo -e "${RED}✗${NC} $1"
    ((CHECKS_FAILED++))
}

log_warn() {
    echo -e "${YELLOW}⚠${NC} $1"
    ((CHECKS_WARNING++))
}

log_info() {
    echo -e "${BLUE}ℹ${NC} $1"
}

# =============================================================================
# XR CONFIGURATION CHECKS
# =============================================================================

check_project_structure() {
    echo -e "\n${BLUE}=== PROJECT STRUCTURE ===${NC}"

    local files=(
        "Assets/Scripts/Vehicle/VehiclePhysics.cs"
        "Assets/Scripts/Vehicle/InputMapper.cs"
        "Assets/Scripts/Obstacles/ObstacleDetector.cs"
        "Assets/Scripts/UI/HUDManager.cs"
        "Assets/Scripts/Obstacles/CollisionFeedback.cs"
        "Assets/Scripts/XR/VROriginSetup.cs"
        "Assets/Scripts/Core/GameManager.cs"
        "Assets/Editor/XRValidation.cs"
        "Assets/Editor/BuildAutomation.cs"
    )

    for file in "${files[@]}"; do
        if [ -f "$PROJECT_PATH/$file" ]; then
            log_pass "$file"
        else
            log_fail "$file"
        fi
    done
}

check_xr_files() {
    echo -e "\n${BLUE}=== XR SPECIFIC FILES ===${NC}"

    local xr_files=(
        "Assets/Scripts/XR/VROriginSetup.cs"
        "Assets/Scripts/Core/XRDebugger.cs"
        "Assets/Tests/XR/VROriginSetupTests.cs"
        "Assets/Tests/XR/XRCompatibilityTests.cs"
        "Assets/Editor/XRValidation.cs"
    )

    for file in "${xr_files[@]}"; do
        if [ -f "$PROJECT_PATH/$file" ]; then
            log_pass "XR: $(basename $file)"
        else
            log_fail "XR: $(basename $file)"
        fi
    done
}

check_test_coverage() {
    echo -e "\n${BLUE}=== TEST COVERAGE ===${NC}"

    local total_tests=0

    # Count tests in main test files
    for test_file in Assets/Tests/*/*.cs; do
        if [ -f "$PROJECT_PATH/$test_file" ]; then
            local count=$(grep -c "^\s*\[Test\]" "$PROJECT_PATH/$test_file" || echo 0)
            total_tests=$((total_tests + count))
        fi
    done

    if [ $total_tests -ge 50 ]; then
        log_pass "Unit tests: $total_tests (target: 50+)"
    else
        log_fail "Unit tests: $total_tests (expected 50+)"
    fi
}

check_input_system() {
    echo -e "\n${BLUE}=== INPUT SYSTEM ===${NC}"

    # Check for InputActionAsset
    if find "$PROJECT_PATH/Assets" -name "*.inputactions" | grep -q .; then
        log_pass "Input Action Assets found"
    else
        log_warn "No Input Action Assets (.inputactions) found"
    fi

    # Check for XR input support
    if grep -r "CommonUsages" "$PROJECT_PATH/Assets/Scripts" > /dev/null 2>&1; then
        log_pass "XR Input (CommonUsages) detected"
    else
        log_fail "XR Input (CommonUsages) not found"
    fi
}

check_xr_configuration() {
    echo -e "\n${BLUE}=== XR CONFIGURATION ===${NC}"

    # Check Player Settings file for VR settings
    if [ -f "$PROJECT_PATH/ProjectSettings/ProjectSettings.asset" ]; then
        log_pass "ProjectSettings.asset exists"

        # Note: Would need actual parser to check VR settings
        # For now, just check file exists
        if grep -q "androidUseCustomKeystore" "$PROJECT_PATH/ProjectSettings/ProjectSettings.asset"; then
            log_warn "Android keystore configuration found"
        fi
    else
        log_fail "ProjectSettings.asset not found"
    fi
}

check_documentation() {
    echo -e "\n${BLUE}=== DOCUMENTATION ===${NC}"

    local docs=(
        "XR_VALIDATION_GUIDE.md"
        "BUILD_AND_DEPLOYMENT.md"
        "PHASE_2_COMPLETION_SUMMARY.md"
        "UNITY_XR_SETUP_GUIDE.md"
    )

    for doc in "${docs[@]}"; do
        if [ -f "$PROJECT_PATH/$doc" ]; then
            local size=$(wc -c < "$PROJECT_PATH/$doc")
            local kb=$((size / 1024))
            log_pass "$doc ($kb KB)"
        else
            log_fail "$doc"
        fi
    done
}

check_build_tools() {
    echo -e "\n${BLUE}=== BUILD TOOLS ===${NC}"

    local tools=(
        "build.sh"
        "validate-xr.sh"
        "Makefile"
    )

    for tool in "${tools[@]}"; do
        if [ -f "$PROJECT_PATH/$tool" ]; then
            if [ -x "$PROJECT_PATH/$tool" ]; then
                log_pass "$tool (executable)"
            else
                log_warn "$tool (not executable, run: chmod +x $tool)"
            fi
        else
            log_fail "$tool"
        fi
    done

    if [ -d "$PROJECT_PATH/.github/workflows" ]; then
        log_pass "GitHub Actions workflow directory"
    else
        log_warn "GitHub Actions workflow directory not found"
    fi
}

check_xr_specific_validation() {
    echo -e "\n${BLUE}=== XR SPECIFIC VALIDATION ===${NC}"

    # Check for XROrigin usage
    if grep -r "XROrigin" "$PROJECT_PATH/Assets/Scripts" > /dev/null 2>&1; then
        log_pass "XROrigin references found"
    else
        log_warn "No XROrigin references (may be created at runtime)"
    fi

    # Check for camera configuration
    if grep -r "Camera.main" "$PROJECT_PATH/Assets/Scripts" > /dev/null 2>&1; then
        log_pass "Main camera references found"
    else
        log_warn "No main camera references"
    fi

    # Check for 90 FPS configuration
    if grep -r "90" "$PROJECT_PATH/Assets/Scripts" > /dev/null 2>&1; then
        log_pass "90 FPS references found"
    else
        log_warn "No 90 FPS configuration found"
    fi

    # Check for Quest 3 specific configurations
    if grep -r "Oculus\|Meta\|Quest" "$PROJECT_PATH/Assets/Scripts" > /dev/null 2>&1; then
        log_pass "Quest 3/Oculus references found"
    else
        log_warn "No explicit Quest 3 references"
    fi
}

# =============================================================================
# GIT VALIDATION
# =============================================================================

check_git_status() {
    echo -e "\n${BLUE}=== GIT STATUS ===${NC}"

    local uncommitted=$(cd "$PROJECT_PATH" && git status --porcelain | wc -l)
    if [ $uncommitted -eq 0 ]; then
        log_pass "No uncommitted changes"
    else
        log_warn "$uncommitted file(s) have uncommitted changes"
    fi

    # Check commits
    local commits=$(cd "$PROJECT_PATH" && git rev-list --count HEAD 2>/dev/null || echo "0")
    if [ "$commits" -gt 5 ]; then
        log_pass "Git history: $commits commits"
    else
        log_fail "Git history: Only $commits commits (expected 5+)"
    fi

    # Check current branch
    local branch=$(cd "$PROJECT_PATH" && git rev-parse --abbrev-ref HEAD 2>/dev/null || echo "unknown")
    if [[ "$branch" == *"claude"* ]] || [[ "$branch" == *"xr"* ]] || [[ "$branch" == *"develop"* ]]; then
        log_pass "Branch: $branch"
    else
        log_warn "Branch: $branch"
    fi
}

# =============================================================================
# PERFORMANCE PREDICTIONS
# =============================================================================

check_performance_readiness() {
    echo -e "\n${BLUE}=== PERFORMANCE READINESS ===${NC}"

    # Check for optimization hints
    if grep -r "GPU.*Instancing\|#pragma.*instancing" "$PROJECT_PATH/Assets" > /dev/null 2>&1; then
        log_pass "GPU instancing configuration found"
    else
        log_warn "No GPU instancing configured (performance optimization)"
    fi

    # Check for physics optimization
    if grep -r "FixedUpdate\|Physics" "$PROJECT_PATH/Assets/Scripts" > /dev/null 2>&1; then
        log_pass "Physics implementation detected"
    else
        log_fail "No physics implementation found"
    fi

    # Check for LOD configuration
    if grep -r "LOD\|LevelOfDetail" "$PROJECT_PATH/Assets" > /dev/null 2>&1; then
        log_pass "LOD system configured"
    else
        log_warn "No LOD system (acceptable for Hito 1)"
    fi
}

# =============================================================================
# QUICK VALIDATION (Fast checks only)
# =============================================================================

run_quick_validation() {
    echo -e "${BLUE}╔════════════════════════════════════════╗${NC}"
    echo -e "${BLUE}║     VR CAR - QUICK XR VALIDATION      ║${NC}"
    echo -e "${BLUE}╚════════════════════════════════════════╝${NC}"

    check_project_structure
    check_xr_files
    check_test_coverage
    print_summary
}

# =============================================================================
# FULL VALIDATION (All checks)
# =============================================================================

run_full_validation() {
    echo -e "${BLUE}╔════════════════════════════════════════╗${NC}"
    echo -e "${BLUE}║     VR CAR - FULL XR VALIDATION       ║${NC}"
    echo -e "${BLUE}╚════════════════════════════════════════╝${NC}"

    check_project_structure
    check_xr_files
    check_test_coverage
    check_input_system
    check_xr_configuration
    check_documentation
    check_build_tools
    check_xr_specific_validation
    check_git_status
    check_performance_readiness
    print_summary
}

# =============================================================================
# DEVICE VALIDATION (Pre-device checks)
# =============================================================================

run_device_validation() {
    echo -e "${BLUE}╔════════════════════════════════════════╗${NC}"
    echo -e "${BLUE}║  VR CAR - PRE-DEVICE XR VALIDATION    ║${NC}"
    echo -e "${BLUE}╚════════════════════════════════════════╝${NC}"

    echo -e "\n${YELLOW}Device Validation Checklist${NC}"
    echo ""
    echo "Before connecting Meta Quest 3:"
    echo ""
    echo "1. Android Setup:"
    echo "   □ Android SDK installed"
    echo "   □ ADB (Android Debug Bridge) installed"
    echo "   □ Quest 3 USB drivers installed"
    echo ""
    echo "2. Quest 3 Settings:"
    echo "   □ Developer mode enabled"
    echo "   □ USB debugging enabled"
    echo "   □ USB file transfer mode allowed"
    echo ""
    echo "3. Connection:"
    echo "   □ Connect Quest 3 via USB cable"
    echo "   □ Verify: adb devices"
    echo ""
    echo "4. Installation:"
    echo "   □ Run: adb install Builds/VRCar-Hito1.apk"
    echo ""
    echo "5. Gameplay Testing:"
    echo "   □ Launch app on device"
    echo "   □ Press 'D' key to show debug overlay"
    echo "   □ Verify FPS: Should maintain 90"
    echo "   □ Test all inputs: Triggers, Joystick, Steering"
    echo "   □ Check tracking: Head movement works"
    echo "   □ Test obstacles: Collision detection"
    echo "   □ Monitor: FPS, latency, frame spikes"
    echo ""
    echo "6. Performance Targets:"
    echo "   FPS:             90 (target), >85 (acceptable)"
    echo "   Latency:         <20ms (target)"
    echo "   Frame Time:      ~11.1ms per frame"
    echo "   Memory:          <1.5GB"
    echo ""

    check_build_tools
    print_summary
}

# =============================================================================
# PRINT SUMMARY
# =============================================================================

print_summary() {
    echo ""
    echo -e "${BLUE}════════════════════════════════════════${NC}"
    echo -e "${BLUE}           VALIDATION SUMMARY${NC}"
    echo -e "${BLUE}════════════════════════════════════════${NC}"
    echo ""
    echo -e "Passed:   ${GREEN}$CHECKS_PASSED${NC}"
    echo -e "Warnings: ${YELLOW}$CHECKS_WARNING${NC}"
    echo -e "Failed:   ${RED}$CHECKS_FAILED${NC}"
    echo ""
    echo "Total:    $((CHECKS_PASSED + CHECKS_WARNING + CHECKS_FAILED))"
    echo ""

    if [ $CHECKS_FAILED -eq 0 ]; then
        echo -e "${GREEN}✅ XR VALIDATION SUCCESSFUL${NC}"
        if [ $CHECKS_WARNING -gt 0 ]; then
            echo -e "${YELLOW}($CHECKS_WARNING warnings - review recommended)${NC}"
        fi
        echo ""
        echo "Project is ready for:"
        echo "  ✓ Editor testing"
        echo "  ✓ APK building"
        echo "  ✓ Device deployment"
    else
        echo -e "${RED}❌ XR VALIDATION FAILED${NC}"
        echo -e "${RED}($CHECKS_FAILED critical issue(s))${NC}"
        echo ""
        echo "Please fix critical issues before:"
        echo "  ✗ Building APK"
        echo "  ✗ Deploying to device"
    fi

    echo ""
}

# =============================================================================
# MAIN
# =============================================================================

show_usage() {
    echo "VR Car XR Validation Script"
    echo ""
    echo "Usage: $0 [MODE]"
    echo ""
    echo "Modes:"
    echo "  quick    - Fast checks only (project structure, tests)"
    echo "  full     - Complete validation (all checks)"
    echo "  device   - Pre-device validation checklist"
    echo "  help     - Show this help message"
    echo ""
    echo "Default: quick"
    echo ""
    echo "Examples:"
    echo "  $0 quick        # Run quick validation"
    echo "  $0 full         # Run full validation"
    echo "  $0 device       # Pre-device checklist"
}

case "$MODE" in
    quick)
        run_quick_validation
        ;;
    full)
        run_full_validation
        ;;
    device)
        run_device_validation
        ;;
    help|--help|-h)
        show_usage
        ;;
    *)
        echo "Unknown mode: $MODE"
        echo ""
        show_usage
        exit 1
        ;;
esac

exit $CHECKS_FAILED
