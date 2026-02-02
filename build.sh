#!/bin/bash
# Build script for iRacing Telemetry Parser

set -e  # Exit on error

echo "=== Building iRacing Telemetry Parser ==="
echo ""

# Clean
echo "Cleaning previous builds..."
dotnet clean --configuration Release > /dev/null

# Restore
echo "Restoring dependencies..."
dotnet restore

# Build
echo "Building solution..."
dotnet build --configuration Release --no-restore

# Test
echo "Running tests..."
dotnet test --configuration Release --no-build --logger "console;verbosity=minimal"

echo ""
echo "=== Build Complete ==="
echo ""
echo "Run the CLI with:"
echo "  dotnet run --project src/IbtTelemetry.Cli -- read sample.ibt"
echo ""
echo "Or publish an executable:"
echo "  dotnet publish src/IbtTelemetry.Cli -c Release -r win-x64 --self-contained -o ./publish"
