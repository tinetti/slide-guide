# Build Instructions

## Platform Requirements

SlideGuide has different build requirements depending on which components you're working with:

### Full Application (Windows Only)

- **OS**: Windows 11
- **SDK**: .NET 8 SDK
- **IDE**: Visual Studio 2022 or JetBrains Rider
- **Additional**: Windows App SDK

### Core Libraries (Cross-Platform)

- **OS**: Windows, macOS, or Linux
- **SDK**: .NET 8 SDK
- **IDE**: Any IDE with .NET support (VS Code, Rider, Visual Studio, etc.)

## Building on Windows

To build the complete solution including the WinUI 3 application:

```bash
# Restore all dependencies
dotnet restore IbtTelemetry.sln

# Build entire solution
dotnet build IbtTelemetry.sln

# Build in Release mode
dotnet build IbtTelemetry.sln --configuration Release

# Run all tests
dotnet test IbtTelemetry.sln

# Run the application
dotnet run --project src/SlideGuide.App/SlideGuide.App.csproj
```

## Building on macOS/Linux

On non-Windows platforms, you can build and test the core libraries:

```bash
# Build core libraries
dotnet build src/SlideGuide.Core/
dotnet build src/SlideGuide.Telemetry/
dotnet build src/SlideGuide.Rendering/
dotnet build src/SlideGuide.Overlays/

# Run platform-agnostic tests
dotnet test tests/SlideGuide.Core.Tests/
dotnet test tests/SlideGuide.Telemetry.Tests/
```

**Note**: The `SlideGuide.App` project cannot be built on macOS/Linux as it requires Windows-specific frameworks (WinUI 3). However, all business logic in the Core libraries is cross-platform and can be developed and tested on any OS.

## Project Build Order

The solution projects have the following dependencies:

```
SlideGuide.Core (no dependencies)
  ↓
  ├── SlideGuide.Telemetry
  ├── SlideGuide.Rendering
  └── SlideGuide.Overlays
       ↓
       SlideGuide.App (requires Windows)
```

## Build Configurations

### Debug

Default configuration with full debugging symbols:
```bash
dotnet build --configuration Debug
```

### Release

Optimized build for production:
```bash
dotnet build --configuration Release
```

## Common Build Issues

### Issue: "NETSDK1100: To build a project targeting Windows..."

**Cause**: Attempting to build WinUI 3 project on non-Windows OS

**Solution**: This is expected. Build only the core libraries on macOS/Linux:
```bash
dotnet build src/SlideGuide.Core/
dotnet build src/SlideGuide.Telemetry/
dotnet build src/SlideGuide.Rendering/
dotnet build src/SlideGuide.Overlays/
```

### Issue: "Package 'Microsoft.WindowsAppSDK' could not be found"

**Cause**: Missing Windows App SDK on Windows

**Solution**: Install the Windows App SDK:
```powershell
winget install Microsoft.WindowsAppRuntime.1.5
```

### Issue: "CA1416: This call site is reachable on all platforms"

**Cause**: Platform analyzer warnings for Windows-specific code

**Solution**: This is expected for the Telemetry project. These are warnings, not errors, and can be ignored or suppressed.

## Continuous Integration

### GitHub Actions (Example)

```yaml
# .github/workflows/build.yml
name: Build and Test

on: [push, pull_request]

jobs:
  build-windows:
    runs-on: windows-latest
    steps:
      - uses: actions/checkout@v3
      - uses: actions/setup-dotnet@v3
        with:
          dotnet-version: '8.0.x'
      - run: dotnet restore
      - run: dotnet build --no-restore
      - run: dotnet test --no-build

  build-macos:
    runs-on: macos-latest
    steps:
      - uses: actions/checkout@v3
      - uses: actions/setup-dotnet@v3
        with:
          dotnet-version: '8.0.x'
      - run: dotnet test tests/SlideGuide.Core.Tests/
      - run: dotnet test tests/SlideGuide.Telemetry.Tests/
```

## IDE-Specific Notes

### Visual Studio 2022

- Open `IbtTelemetry.sln`
- Build → Build Solution (Ctrl+Shift+B)
- Test → Run All Tests (Ctrl+R, A)

### JetBrains Rider

- Open `IbtTelemetry.sln`
- Build → Build Solution (Ctrl+Shift+F9)
- Run Unit Tests (Ctrl+T, N)

### Visual Studio Code

```bash
# Install C# extension
code --install-extension ms-dotnettools.csharp

# Open folder
code .

# Use integrated terminal for dotnet commands
dotnet build
dotnet test
```

## Publishing

To create a distributable package:

```bash
# Publish self-contained (includes runtime)
dotnet publish src/SlideGuide.App/ \
  --configuration Release \
  --runtime win-x64 \
  --self-contained true \
  --output ./publish/win-x64

# Publish framework-dependent (requires .NET 8 installed)
dotnet publish src/SlideGuide.App/ \
  --configuration Release \
  --runtime win-x64 \
  --self-contained false \
  --output ./publish/win-x64-fx
```

## Clean Build

To remove all build artifacts:

```bash
# Clean all projects
dotnet clean IbtTelemetry.sln

# Remove bin and obj folders
find . -name "bin" -o -name "obj" | xargs rm -rf
```
