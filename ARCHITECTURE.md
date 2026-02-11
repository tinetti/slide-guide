# SlideGuide Architecture

## Overview

SlideGuide follows a clean, layered architecture with clear separation of concerns. The application is divided into several projects, each with a specific responsibility.

## Project Structure

### SlideGuide.Core (Platform-Agnostic)

**Purpose**: Contains core business logic, interfaces, and models that are platform-independent.

**Key Components**:
- `Interfaces/`: Core abstractions (ITelemetryProvider, IOverlay, IOverlayManager, IRenderer)
- `Models/`: Data models (TelemetryData, OverlayConfiguration, OverlayPosition)
- `Services/`: Business logic implementations (OverlayManager)

**Dependencies**: None (pure .NET 8 BCL)

**Platform**: Cross-platform (Windows, macOS, Linux)

### SlideGuide.Telemetry (Platform-Specific)

**Purpose**: Handles iRacing integration and telemetry reading via Windows shared memory.

**Key Components**:
- `IRacingTelemetryProvider`: Implements ITelemetryProvider
- `SharedMemory/SharedMemoryReader`: Windows memory-mapped file access
- `Models/IRacingHeader`: iRacing memory structure definitions
- `IRacingConstants`: Memory map names and constants

**Dependencies**:
- SlideGuide.Core
- System.Runtime.InteropServices (P/Invoke)

**Platform**: Windows-only (shared memory access)

**Design Notes**:
- Uses polling mechanism to read telemetry at ~60Hz
- Implements automatic reconnection on disconnection
- Thread-safe operation with async/await pattern

### SlideGuide.Rendering (Platform-Specific)

**Purpose**: Provides rendering abstractions and Direct2D implementation.

**Key Components**:
- `Interfaces/IRenderer`: Platform-agnostic renderer interface
- `Direct2D/Direct2DRenderer`: Direct2D implementation
- `OverlayRenderers/`: Specific overlay renderer implementations
- `RenderContext`: Rendering state and context

**Dependencies**:
- SlideGuide.Core
- Vortice.Direct2D1
- Vortice.Direct3D11
- Vortice.DXGI

**Platform**: Windows-only (Direct2D)

**Design Notes**:
- Hardware-accelerated rendering with Direct2D
- Resource caching (brushes, text formats)
- Transparent background support for overlays

### SlideGuide.Overlays (Platform-Agnostic)

**Purpose**: Defines overlay types and provides overlay factory.

**Key Components**:
- `OverlayTypes/BaseOverlay`: Base class for all overlays
- `OverlayTypes/BrakeOverlay`: Brake input overlay
- `OverlayTypes/ThrottleOverlay`: Throttle input overlay
- `OverlayFactory`: Factory for creating overlay instances

**Dependencies**: SlideGuide.Core

**Platform**: Cross-platform

**Design Notes**:
- Template method pattern for overlay base class
- Factory pattern for overlay creation
- Extensible design for adding new overlay types

### SlideGuide.App (Platform-Specific)

**Purpose**: Main WinUI 3 application with UI and window management.

**Key Components**:
- `App.xaml/cs`: Application entry point and DI setup
- `MainWindow.xaml/cs`: Main control window
- `ViewModels/MainViewModel`: MVVM ViewModel
- `Windows/OverlayWindow`: Transparent overlay window host
- `Services/WindowManager`: Overlay window lifecycle management
- `DependencyInjection/ServiceConfiguration`: DI container setup

**Dependencies**:
- SlideGuide.Core
- SlideGuide.Telemetry
- SlideGuide.Rendering
- SlideGuide.Overlays
- Microsoft.WindowsAppSDK
- CommunityToolkit.Mvvm
- Microsoft.Extensions.DependencyInjection

**Platform**: Windows 11 (WinUI 3)

**Design Notes**:
- MVVM pattern with CommunityToolkit.Mvvm
- Dependency injection using Microsoft.Extensions.DependencyInjection
- Event-driven communication between components
- Window management for multiple overlay windows

## Design Patterns

### Dependency Injection

All services are registered in `ServiceConfiguration` and injected via constructor injection:

```csharp
services.AddSingleton<IOverlayManager, OverlayManager>();
services.AddSingleton<ITelemetryProvider, IRacingTelemetryProvider>();
```

### Factory Pattern

`OverlayFactory` creates overlay instances and allows registration of custom overlay types:

```csharp
var factory = new OverlayFactory();
var overlay = factory.CreateOverlay("brake-input");
```

### Observer Pattern

Events are used for communication between components:
- `ITelemetryProvider.TelemetryUpdated` - New telemetry data available
- `ITelemetryProvider.ConnectionStatusChanged` - Connection status changed
- `IOverlayManager.OverlayVisibilityChanged` - Overlay shown/hidden
- `IOverlayManager.OverlayConfigurationChanged` - Overlay config updated

### MVVM Pattern

The main window uses MVVM with:
- `MainWindow.xaml`: View (UI)
- `MainViewModel`: ViewModel (presentation logic)
- Core services: Model (business logic)

### Repository/Provider Pattern

`ITelemetryProvider` abstracts telemetry data access, allowing for:
- Real implementation (IRacingTelemetryProvider)
- Mock implementation for testing
- Future implementations (other sims, replay files, etc.)

## Data Flow

### Telemetry Update Flow

```
iRacing (Shared Memory)
  ↓
IRacingTelemetryProvider (polling)
  ↓ [TelemetryUpdated event]
MainViewModel
  ↓
IOverlayManager.UpdateOverlays()
  ↓
Individual Overlays (Update method)
  ↓
OverlayWindow (render loop)
  ↓
IRenderer (Direct2D)
  ↓
Screen
```

### Overlay Visibility Flow

```
User (clicks checkbox)
  ↓
MainWindow event handler
  ↓
WindowManager.ShowOverlay()
  ↓
IOverlayManager.ShowOverlay()
  ↓ [OverlayVisibilityChanged event]
WindowManager
  ↓
OverlayWindow.Activate()
```

## Threading Model

- **Main Thread**: UI operations (WinUI 3 dispatcher)
- **Telemetry Thread**: Background polling of iRacing shared memory
- **Render Thread**: Each overlay window has its own render loop on the UI thread

Thread synchronization:
- `OverlayManager` uses locks for thread-safe access to overlay collection
- `TelemetryProvider` uses `async/await` and `CancellationToken` for safe shutdown
- Event handlers marshal to UI thread using `DispatcherQueue.TryEnqueue()`

## Error Handling

- Exceptions in telemetry reading are caught and logged, with automatic reconnection
- Rendering errors are caught to prevent overlay crashes
- Service resolution failures are propagated to the user with clear error messages

## Testing Strategy

### Unit Tests

- **Core.Tests**: Test business logic in isolation with mocked dependencies
- **Telemetry.Tests**: Test telemetry provider with mocked shared memory
- Platform-agnostic tests run on macOS/Linux for CI

### Integration Tests

- Test interaction between multiple components
- Require Windows for full Direct2D/shared memory tests

### Manual Testing

- Requires active iRacing session
- Verify overlay rendering, positioning, and performance
- Test multiple monitors and DPI scaling scenarios

## Extension Points

### Adding New Overlay Types

1. Create class inheriting `BaseOverlay`
2. Override `Id`, `Name`, `RendererType` properties
3. Implement `OnUpdate()` for custom update logic
4. Create corresponding renderer in `SlideGuide.Rendering`
5. Register in `OverlayFactory`

### Custom Telemetry Sources

Implement `ITelemetryProvider` to support:
- Other racing simulators
- Replay file playback
- Test data generators

### Custom Renderers

Implement `IRenderer` for:
- Different rendering backends (e.g., OpenGL, Vulkan)
- Headless rendering for testing
- Recording/streaming scenarios

## Performance Considerations

- **Telemetry Polling**: Default 16ms (~60 Hz) for smooth updates
- **Rendering**: Each overlay renders at 60 FPS independently
- **Resource Pooling**: Brushes and text formats are cached to minimize allocations
- **Lock Contention**: Minimal locking in hot paths (telemetry update, rendering)

## Future Improvements

- Configuration file persistence (JSON)
- Logging infrastructure (Serilog)
- Performance metrics overlay
- Plugin system for custom overlays
- Cloud synchronization of settings
- Telemetry recording and playback
