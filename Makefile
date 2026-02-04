# iRacing Telemetry Parser - Makefile

.PHONY: help build clean restore test test-unit test-integration coverage run publish publish-all install format lint check

# Configuration
SOLUTION = IbtTelemetry.sln
CLI_PROJECT = src/IbtTelemetry.Cli/IbtTelemetry.Cli.csproj
CORE_PROJECT = src/IbtTelemetry.Core/IbtTelemetry.Core.csproj
SERVICE_PROJECT = src/IbtTelemetry.Service/IbtTelemetry.Service.csproj
UNIT_TESTS = tests/IbtTelemetry.Core.Tests/IbtTelemetry.Core.Tests.csproj
INTEGRATION_TESTS = tests/IbtTelemetry.Integration.Tests/IbtTelemetry.Integration.Tests.csproj

CONFIGURATION ?= Release
RUNTIME ?= $(shell uname -s | tr '[:upper:]' '[:lower:]' | sed 's/darwin/osx/g')-$(shell uname -m | sed 's/x86_64/x64/g; s/aarch64/arm64/g; s/arm64/arm64/g')
PUBLISH_DIR = ./publish
SAMPLE_FILE = sample.ibt

## help: Display this help message
help:
	@echo "Available targets:"
	@grep -E '^## ' $(MAKEFILE_LIST) | sed 's/## /  /'

## all: Restore, build, and test the solution
all: restore build test

## restore: Restore NuGet packages
restore:
	dotnet restore $(SOLUTION)

## build: Build the solution in Release mode
build:
	dotnet build $(SOLUTION) --configuration $(CONFIGURATION) --no-restore

## build-debug: Build the solution in Debug mode
build-debug:
	$(MAKE) build CONFIGURATION=Debug

## clean: Clean build artifacts
clean:
	dotnet clean $(SOLUTION)
	rm -rf $(PUBLISH_DIR)
	rm -rf **/bin **/obj
	find . -type d -name "TestResults" -exec rm -rf {} + 2>/dev/null || true

## test: Run all tests
test:
	dotnet test $(SOLUTION) --configuration $(CONFIGURATION) --no-build

## test-unit: Run unit tests only
test-unit:
	dotnet test $(UNIT_TESTS) --configuration $(CONFIGURATION)

## test-integration: Run integration tests only
test-integration:
	dotnet test $(INTEGRATION_TESTS) --configuration $(CONFIGURATION)

## coverage: Run tests with code coverage
coverage:
	dotnet test $(SOLUTION) --configuration $(CONFIGURATION) \
		--collect:"XPlat Code Coverage" \
		--results-directory ./TestResults

## watch: Run tests in watch mode
watch:
	dotnet watch test $(SOLUTION)

## run: Convert sample.ibt to Parquet (primary function)
run:
	dotnet run --project $(CLI_PROJECT) -- $(SAMPLE_FILE) telemetry.parquet

## run-inspect: Inspect sample.ibt contents
run-inspect:
	dotnet run --project $(CLI_PROJECT) -- read $(SAMPLE_FILE)

## run-dir: Process all .ibt files in a directory
run-dir:
	@echo "Usage: make run-dir DIR=/path/to/telemetry"
	@if [ -z "$(DIR)" ]; then \
		echo "Error: DIR not specified"; \
		exit 1; \
	fi
	dotnet run --project $(CLI_PROJECT) -- read $(DIR)

## run-all-samples: Run CLI with all samples
run-all-samples:
	dotnet run --project $(CLI_PROJECT) -- read $(SAMPLE_FILE) --samples

## run-limit: Run CLI with limited samples (default: 10)
run-limit:
	dotnet run --project $(CLI_PROJECT) -- read $(SAMPLE_FILE) --limit 10

## run-json: Run CLI with JSON output
run-json:
	dotnet run --project $(CLI_PROJECT) -- read $(SAMPLE_FILE) --json

## publish: Publish CLI for current platform
publish: restore
	dotnet publish $(CLI_PROJECT) \
		--configuration $(CONFIGURATION) \
		--runtime $(RUNTIME) \
		--self-contained \
		--output $(PUBLISH_DIR)/$(RUNTIME)

## publish-win-x64: Publish CLI for Windows x64
publish-win-x64: restore
	dotnet publish $(CLI_PROJECT) \
		--configuration $(CONFIGURATION) \
		--runtime win-x64 \
		--self-contained \
		--output $(PUBLISH_DIR)/win-x64

## publish-linux-x64: Publish CLI for Linux x64
publish-linux-x64: restore
	dotnet publish $(CLI_PROJECT) \
		--configuration $(CONFIGURATION) \
		--runtime linux-x64 \
		--self-contained \
		--output $(PUBLISH_DIR)/linux-x64

## publish-osx-x64: Publish CLI for macOS x64
publish-osx-x64: restore
	dotnet publish $(CLI_PROJECT) \
		--configuration $(CONFIGURATION) \
		--runtime osx-x64 \
		--self-contained \
		--output $(PUBLISH_DIR)/osx-x64

## publish-osx-arm64: Publish CLI for macOS ARM64
publish-osx-arm64: restore
	dotnet publish $(CLI_PROJECT) \
		--configuration $(CONFIGURATION) \
		--runtime osx-arm64 \
		--self-contained \
		--output $(PUBLISH_DIR)/osx-arm64

## publish-all: Publish CLI for all platforms
publish-all: publish-win-x64 publish-linux-x64 publish-osx-x64 publish-osx-arm64

## publish-service: Publish Windows Service
publish-service: restore
	dotnet publish $(SERVICE_PROJECT) \
		--configuration $(CONFIGURATION) \
		--runtime win-x64 \
		--self-contained \
		--output $(PUBLISH_DIR)/service

## pack: Create NuGet package for Core library
pack: restore build
	dotnet pack $(CORE_PROJECT) --configuration $(CONFIGURATION) --output $(PUBLISH_DIR)/nuget

## install: Install CLI as dotnet global tool
install: build
	dotnet tool install --global --add-source ./src/IbtTelemetry.Cli/bin/$(CONFIGURATION)/net8.0 IbtTelemetry.Cli || true

## format: Format code using dotnet format
format:
	dotnet format $(SOLUTION)

## format-check: Check code formatting without making changes
format-check:
	dotnet format $(SOLUTION) --verify-no-changes

## lint: Run code analysis
lint:
	dotnet build $(SOLUTION) --configuration $(CONFIGURATION) /p:EnforceCodeStyleInBuild=true /p:TreatWarningsAsErrors=true

## check: Run format check, lint, and tests
check: format-check lint test

## rebuild: Clean and build
rebuild: clean restore build

## info: Display project information
info:
	@echo "Solution:     $(SOLUTION)"
	@echo "Runtime:      $(RUNTIME)"
	@echo "Config:       $(CONFIGURATION)"
	@echo ".NET Version: $$(dotnet --version)"
	@echo "Projects:"
	@echo "  - CLI:      $(CLI_PROJECT)"
	@echo "  - Core:     $(CORE_PROJECT)"
	@echo "  - Service:  $(SERVICE_PROJECT)"

## convert: Convert telemetry to Parquet for machine learning
convert:
	dotnet run --project $(CLI_PROJECT) -- $(SAMPLE_FILE) telemetry_ml.parquet

## convert-all: Convert with all telemetry variables to Parquet
convert-all:
	dotnet run --project $(CLI_PROJECT) -- $(SAMPLE_FILE) telemetry_all.parquet --all

## list-vars: List all available telemetry variables
list-vars:
	dotnet run --project $(CLI_PROJECT) -- list-vars $(SAMPLE_FILE)
