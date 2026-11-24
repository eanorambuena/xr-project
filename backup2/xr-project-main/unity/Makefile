# VR Car Project Makefile
# Automates common development and build tasks

.PHONY: help validate tests report build clean docs check-env setup-ci

SHELL := /bin/bash
PROJECT_NAME := VRCar-Hito1
UNITY_VERSION := 6000.4.0f1
BUILD_DIR := Builds
REPORTS_DIR := reports

# Color output
BLUE := \033[0;34m
GREEN := \033[0;32m
YELLOW := \033[1;33m
RED := \033[0;31m
NC := \033[0m

help:
	@echo "$(BLUE)VR Car - Build Automation Help$(NC)"
	@echo ""
	@echo "$(GREEN)Development Commands:$(NC)"
	@echo "  make validate      - Validate project structure"
	@echo "  make tests         - Count and list unit tests"
	@echo "  make docs          - Generate project documentation"
	@echo "  make check         - Run all checks (validate + tests)"
	@echo ""
	@echo "$(GREEN)Build Commands:$(NC)"
	@echo "  make report        - Generate build report"
	@echo "  make build         - Build APK for Quest 3 (requires Unity)"
	@echo "  make all           - Full build pipeline (validate + report + build)"
	@echo ""
	@echo "$(GREEN)Maintenance:$(NC)"
	@echo "  make clean         - Remove build artifacts"
	@echo "  make setup-ci      - Setup CI/CD configuration"
	@echo "  make git-status    - Show git status"
	@echo "  make git-log       - Show recent commits"
	@echo ""

validate:
	@echo "$(BLUE)=== Validating Project Structure ===$(NC)"
	@bash build.sh validate

tests:
	@echo "$(BLUE)=== Counting Unit Tests ===$(NC)"
	@bash build.sh tests

report:
	@echo "$(BLUE)=== Generating Build Report ===$(NC)"
	@bash build.sh report

build:
	@echo "$(BLUE)=== Building APK ===$(NC)"
	@bash build.sh build

all: validate tests report
	@echo "$(GREEN)✓ Build pipeline complete!$(NC)"
	@echo "$(YELLOW)Note: APK build requires Unity CLI$(NC)"

docs:
	@echo "$(BLUE)=== Project Documentation ===$(NC)"
	@if [ -f "PHASE_2_COMPLETION_SUMMARY.md" ]; then \
		echo "$(GREEN)✓ Phase 2 Summary exists$(NC)"; \
		wc -l PHASE_2_COMPLETION_SUMMARY.md; \
	fi
	@echo ""
	@echo "$(GREEN)Project Files:$(NC)"
	@find Assets/Scripts -name "*.cs" -type f | wc -l | xargs echo "  Production Scripts:"
	@find Assets/Tests -name "*.cs" -type f | wc -l | xargs echo "  Test Scripts:"

check: validate tests
	@echo "$(GREEN)✓ All checks passed!$(NC)"

clean:
	@echo "$(BLUE)=== Cleaning Build Artifacts ===$(NC)"
	@rm -rf $(BUILD_DIR)
	@rm -rf $(REPORTS_DIR)
	@rm -f build_report.json
	@echo "$(GREEN)✓ Cleaned$(NC)"

setup-ci:
	@echo "$(BLUE)=== Setting up CI/CD Configuration ===$(NC)"
	@mkdir -p .github/workflows
	@echo "$(YELLOW)Create: .github/workflows/ci.yml$(NC)"
	@echo "CI/CD configuration ready for GitHub Actions"

git-status:
	@echo "$(BLUE)=== Git Status ===$(NC)"
	@git status

git-log:
	@echo "$(BLUE)=== Recent Commits ===$(NC)"
	@git log --oneline -10

git-diff:
	@echo "$(BLUE)=== Uncommitted Changes ===$(NC)"
	@git diff --stat

git-push:
	@echo "$(BLUE)=== Pushing to Remote ===$(NC)"
	@git push -u origin $$(git rev-parse --abbrev-ref HEAD)

# Stats target
stats:
	@echo "$(BLUE)=== Project Statistics ===$(NC)"
	@echo ""
	@echo "$(GREEN)Production Code:$(NC)"
	@find Assets/Scripts -name "*.cs" -exec wc -l {} + | tail -1
	@echo ""
	@echo "$(GREEN)Test Code:$(NC)"
	@find Assets/Tests -name "*.cs" -exec wc -l {} + | tail -1
	@echo ""
	@echo "$(GREEN)Components:$(NC)"
	@ls -1 Assets/Scripts/*/*.cs 2>/dev/null | wc -l | xargs echo "  Total:"
	@echo ""
	@echo "$(GREEN)Test Files:$(NC)"
	@ls -1 Assets/Tests/*/*.cs 2>/dev/null | wc -l | xargs echo "  Total:"

# Inspection targets
inspect-vehicle:
	@echo "$(BLUE)=== VehiclePhysics Inspection ===$(NC)"
	@grep -n "public\|private" Assets/Scripts/Vehicle/VehiclePhysics.cs | head -20

inspect-input:
	@echo "$(BLUE)=== InputMapper Inspection ===$(NC)"
	@grep -n "public\|private" Assets/Scripts/Vehicle/InputMapper.cs | head -20

inspect-tests:
	@echo "$(BLUE)=== Test Summary ===$(NC)"
	@find Assets/Tests -name "*Tests.cs" -exec grep -l "\[Test\]" {} \; | sort
	@echo ""
	@find Assets/Tests -name "*Tests.cs" -exec grep -c "\[Test\]" {} \; | paste -d' ' <(find Assets/Tests -name "*Tests.cs" | sort) - | column -t

# Version info
version:
	@echo "$(BLUE)VR Car Project Information$(NC)"
	@echo "Project: $(PROJECT_NAME)"
	@echo "Unity Version: $(UNITY_VERSION)"
	@echo "Platform: Android (Meta Quest 3)"
	@echo "Git Branch: $$(git rev-parse --abbrev-ref HEAD)"
	@echo "Git Commit: $$(git rev-parse --short HEAD)"
	@echo "Last Updated: $$(git log -1 --format=%ai)"

# Default target
.DEFAULT_GOAL := help
