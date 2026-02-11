# SlideGuide

**SlideGuide** is a Windows 11 coaching companion application for iRacing sim racing. It provides real-time telemetry overlays to help drivers improve their technique by visualizing brake and throttle inputs during races.

## Features

- **Real-time Telemetry**: Connects to iRacing via shared memory for instant data access
- **Configurable Overlays**: Display brake and throttle inputs as transparent overlays
- **Direct2D Rendering**: High-performance rendering with minimal performance impact
- **Minimal UI**: Simple control panel for managing overlays
- **Modular Architecture**: Clean separation of concerns following SOLID principles
- **Cross-Platform Testing**: Core business logic can be tested on macOS/Linux

## Architecture

SlideGuide is built with a modular architecture that separates platform-specific code from business logic:

```
SlideGuide/
├── src/
│   ├── SlideGuide.Core/          # Platform-agnostic business logic
│   ├── SlideGuide.Telemetry/     # iRacing telemetry integration
│   ├── SlideGuide.Rendering/     # Direct2D rendering
│   ├── SlideGuide.Overlays/      # Overlay definitions
│   └── SlideGuide.App/           # WinUI 3 application
└── tests/
    ├── SlideGuide.Core.Tests/
    ├── SlideGuide.Telemetry.Tests/
    └── SlideGuide.Integration.Tests/
```

### Key Design Principles

- **Separation of Concerns**: Platform-specific code isolated from business logic
- **Dependency Injection**: All dependencies injected via interfaces
- **Interface-Based Testing**: External dependencies behind interfaces for mocking
- **MVVM Pattern**: Clean UI separation using WinUI 3 and CommunityToolkit.Mvvm

## Technology Stack

- **.NET 8** - Latest LTS version
- **WinUI 3** - Modern Windows 11 UI framework
- **Vortice.Direct2D1** - Modern Direct2D/DirectX bindings
- **xUnit** - Testing framework
- **Moq** - Mocking framework
- **FluentAssertions** - Assertion library
- **CommunityToolkit.Mvvm** - MVVM helpers

## Prerequisites

- Windows 11 (for running the application)
- .NET 8 SDK
- Visual Studio 2022 or Rider (for Windows development)
- iRacing subscription (for telemetry data)

## Building

### On Windows

```bash
# Restore dependencies
dotnet restore

# Build all projects
dotnet build

# Run tests
dotnet test

# Run the application
dotnet run --project src/SlideGuide.App/SlideGuide.App.csproj
```

### On macOS/Linux (Development)

Core and telemetry projects can be built and tested on non-Windows platforms:

```bash
# Build core libraries
dotnet build src/SlideGuide.Core/SlideGuide.Core.csproj
dotnet build src/SlideGuide.Telemetry/SlideGuide.Telemetry.csproj
dotnet build src/SlideGuide.Overlays/SlideGuide.Overlays.csproj

# Run platform-agnostic tests
dotnet test tests/SlideGuide.Core.Tests/SlideGuide.Core.Tests.csproj
dotnet test tests/SlideGuide.Telemetry.Tests/SlideGuide.Telemetry.Tests.csproj
```

Note: The Rendering and App projects require Windows and cannot be built on macOS/Linux.

## Usage

1. **Launch iRacing**: Start iRacing and load into a session (test, practice, or race)

2. **Start SlideGuide**: Launch the SlideGuide application

3. **Enable Overlays**: Check the overlays you want to display:
   - Brake Input
   - Throttle Input

4. **Start Telemetry**: Click "Start Telemetry" to begin receiving data from iRacing

5. **Position Overlays**: Drag overlays to your preferred screen positions

6. **Drive**: The overlays will update in real-time as you drive

## Configuration

Overlay positions and settings are automatically saved and restored between sessions.

## Development

### Adding New Overlays

1. Create a new overlay class in `SlideGuide.Overlays/OverlayTypes/` inheriting from `BaseOverlay`
2. Create a corresponding renderer in `SlideGuide.Rendering/OverlayRenderers/`
3. Register the overlay in `OverlayFactory`
4. Add UI controls in `MainWindow.xaml`

### Testing

The project includes comprehensive unit tests:

- **Core.Tests**: Business logic tests (cross-platform)
- **Telemetry.Tests**: Telemetry provider tests with mocked shared memory
- **Integration.Tests**: End-to-end integration tests

Run all tests:
```bash
dotnet test
```

Run specific test project:
```bash
dotnet test tests/SlideGuide.Core.Tests/
```

## Project Status

✅ Core abstractions and interfaces
✅ Telemetry integration (basic)
✅ Rendering infrastructure (Direct2D)
✅ Overlay system (Brake & Throttle)
✅ WinUI 3 application structure
✅ Unit tests (Core & Telemetry)

🚧 **In Progress:**
- Full iRacing telemetry variable parsing
- DirectWrite text rendering integration
- Overlay position persistence
- Additional overlay types (steering, G-forces, etc.)

## Known Limitations

- Windows 11 only (WinUI 3 requirement)
- Requires active iRacing session for telemetry
- Text rendering not yet implemented (uses graphical indicators)
- Full iRacing telemetry parsing is simplified in current implementation

## Contributing

This is a personal project, but suggestions and feedback are welcome!

## License

TBD

## Acknowledgments

- iRacing for providing shared memory telemetry access
- Vortice.Windows team for modern .NET DirectX bindings
- WinUI 3 team for the modern Windows app framework

---

**Note**: This application is not affiliated with or endorsed by iRacing.com Motorsport Simulations, LLC.
