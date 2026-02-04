# iRacing Telemetry Parser - C# Implementation

A modular, well-tested .NET 8.0 command-line utility and library for parsing iRacing telemetry (.ibt) files. This is a port of the Node.js [ibt-telemetry](../ibt-telemetry) project with architecture ready for future Windows Service conversion.

## Features

- **High Performance**: Memory-efficient streaming of telemetry samples using IAsyncEnumerable
- **Cross-Platform**: Runs on Windows, macOS, and Linux (.NET 8.0)
- **Modular Architecture**: Separate Core library, CLI tool, and Windows Service projects
- **Well Tested**: Unit and integration tests with 80%+ code coverage
- **Multiple Output Formats**: Console-friendly and JSON output modes
- **Machine Learning Ready**: Export telemetry to CSV for ML training with default feature sets

## Project Structure

```
IbtTelemetry/
├── src/
│   ├── IbtTelemetry.Core/           # Core library (parsing, models, services)
│   ├── IbtTelemetry.Cli/            # Command-line interface
│   └── IbtTelemetry.Service/        # Windows Service (minimal implementation)
└── tests/
    ├── IbtTelemetry.Core.Tests/     # Unit tests
    └── IbtTelemetry.Integration.Tests/  # Integration tests with sample.ibt
```

## Requirements

- .NET 8.0 SDK
- Windows 11 (for Windows Service features), macOS, or Linux

## Installation

### Building from Source

```bash
# Clone the repository
cd ibt-telemetry-csharp

# Restore dependencies
dotnet restore

# Build the solution
dotnet build

# Run tests
dotnet test
```

### Publishing as Executable

```bash
# Publish for Windows x64 (self-contained)
dotnet publish src/IbtTelemetry.Cli -c Release -r win-x64 --self-contained -o ./publish

# The executable will be at: ./publish/IbtTelemetry.Cli.exe
```

## Usage

### Command-Line Interface

#### Inspect Telemetry Files (Optional)

For diagnostic purposes, use the `read` subcommand:

```bash
# Inspect single file (shows first 5 samples)
dotnet run --project src/IbtTelemetry.Cli -- read sample.ibt

# Inspect all files in directory
dotnet run --project src/IbtTelemetry.Cli -- read ~/Documents/iRacing/telemetry/
```

Output includes:
- Telemetry file header information
- Session information (track, drivers, settings)
- Variable summary by type
- First 5 telemetry samples

**Directory processing**: When given a directory path, automatically finds and processes all `.ibt` files recursively, showing a summary at the end.

```
=== Telemetry File Information ===

SDK Version:     2
Tick Rate:       60 Hz
Variables:       287
Sample Buffers:  18000
Buffer Size:     1464 bytes

=== Session Information ===

TrackName: sachsenring
TrackID: 521
...

=== Telemetry Variables ===

Total Variables: 287
  Bool        28 variables
  Int         55 variables
  BitField     3 variables
  Float      196 variables
  Double       5 variables

=== Telemetry Samples ===

--- Sample #1 ---
  SessionTime                    30.617 s
  Speed                          0.000 m/s
  RPM                            300.000 revs/min
  ...
```

#### Display All Telemetry Samples

```bash
dotnet run --project src/IbtTelemetry.Cli -- read sample.ibt --samples
```

#### Display Specific Number of Samples

```bash
dotnet run --project src/IbtTelemetry.Cli -- read sample.ibt --limit 10
```

Or with all samples:

```bash
dotnet run --project src/IbtTelemetry.Cli -- read sample.ibt --samples --limit 100
```

#### JSON Output

```bash
dotnet run --project src/IbtTelemetry.Cli -- read sample.ibt --json > output.json
```

#### With Samples in JSON

```bash
dotnet run --project src/IbtTelemetry.Cli -- read sample.ibt --samples --limit 5 --json
```

### Convert .ibt to Parquet (Primary Function)

The CLI's main purpose is converting iRacing telemetry files to Parquet format:

```bash
# Convert single file (44 default ML variables)
dotnet run --project src/IbtTelemetry.Cli -- sample.ibt telemetry.parquet

# Convert with all variables (287 variables)
dotnet run --project src/IbtTelemetry.Cli -- sample.ibt telemetry.parquet --all

# Convert specific variables
dotnet run --project src/IbtTelemetry.Cli -- sample.ibt telemetry.parquet \
  --variables Speed,RPM,Throttle,Brake,SteeringWheelAngle

# Convert entire directory
dotnet run --project src/IbtTelemetry.Cli -- ./telemetry_data/ combined.parquet
```

**Simple syntax**: `IbtTelemetry.Cli <input> <output>`

**Why Parquet?**
- 📊 **Columnar storage**: 5-10x smaller files than CSV
- ⚡ **Faster loading**: Native support in pandas, polars, arrow
- 🎯 **Preserves types**: No string conversion, native floats/ints
- 🔧 **Better for ML**: Optimized for analytical queries

#### List Available Variables

```bash
dotnet run --project src/IbtTelemetry.Cli -- list-vars sample.ibt
```

**Default ML Variables** (44 total):
- Time & Position: SessionTime, Lap, LapDistPct, LapCurrentLapTime
- Vehicle Dynamics: Speed, RPM, Gear
- Driver Inputs: Throttle, Brake, Clutch, SteeringWheelAngle
- Accelerations: LatAccel, LongAccel, VertAccel
- Orientation: YawNorth, Pitch, Roll
- Tire Temperatures: LF/RF/LR/RR tempCL/CM/CR (12 variables)
- Tire Wear: LF/RF/LR/RR wearL/M/R (6 variables)
- Tire Pressure: LF/RF/LR/RR pressure (4 variables)
- Fuel: FuelLevel, FuelLevelPct
- Track: TrackTemp, TrackTempCrew

See [ML_TELEMETRY_GUIDE.md](ML_TELEMETRY_GUIDE.md) for detailed ML training strategies and examples.

### Using as a Library

```csharp
using IbtTelemetry.Core.Services;

// Create the telemetry service
var service = new TelemetryService();

// Load telemetry file
using var telemetry = await service.LoadFromFileAsync("path/to/file.ibt");

// Access session info
Console.WriteLine($"Track: {telemetry.SessionInfo.GetWeekendValue<object>("TrackName")}");
Console.WriteLine($"Variables: {telemetry.Header.NumVars}");

// Stream samples (memory efficient)
await foreach (var sample in telemetry.GetSamplesAsync())
{
    var speed = sample.GetParameter("Speed");
    Console.WriteLine($"Speed: {speed?.Value} {speed?.Unit}");
}
```

## Architecture

### Binary File Format

iRacing .ibt files contain:

1. **TelemetryHeader** (112 bytes): File structure metadata
2. **DiskSubHeader** (32 bytes): Session timing metadata
3. **SessionInfo** (variable): YAML-encoded session/driver/weekend info
4. **VarHeaders** (144 bytes × numVars): Variable definitions (type, offset, name, unit)
5. **Sample Data** (bufLen × numBuf): Telemetry samples

### Key Components

#### IbtTelemetry.Core

- **Constants/IrsdkConstants.cs**: All iRacing SDK enums and flags
- **Models/Headers/**: Header structures (TelemetryHeader, DiskSubHeader, VarHeader)
- **Models/Telemetry.cs**: Main telemetry container with streaming support
- **Models/TelemetrySample.cs**: Sample data extraction
- **Services/TelemetryService.cs**: High-level service for loading telemetry
- **IO/TelemetryFileReader.cs**: Low-level binary file reader

#### IbtTelemetry.Cli

- **Commands/ReadCommand.cs**: Main read command implementation
- **Output/**: Output formatters (Console, JSON)
- Uses System.CommandLine for argument parsing

#### IbtTelemetry.Service

- **Worker.cs**: BackgroundService implementation (minimal placeholder)
- Configured for Windows Service deployment
- TODO: Implement file watching and processing pipeline

### Streaming Strategy

The library uses `IAsyncEnumerable<TelemetrySample>` for memory-efficient streaming:

```csharp
public async IAsyncEnumerable<TelemetrySample> GetSamplesAsync(
    [EnumeratorCancellation] CancellationToken cancellationToken = default)
{
    // Read samples sequentially, yield one at a time
    // Only one sample buffer in memory at any time
}
```

Benefits:
- Async/await native support
- Cancellation token propagation
- Memory efficient (handles 300MB+ files)
- Modern C# idiom

## Testing

### Run All Tests

```bash
dotnet test
```

### Run Unit Tests Only

```bash
dotnet test tests/IbtTelemetry.Core.Tests/IbtTelemetry.Core.Tests.csproj
```

### Run Integration Tests Only

```bash
dotnet test tests/IbtTelemetry.Integration.Tests/IbtTelemetry.Integration.Tests.csproj
```

### With Coverage

```bash
dotnet test /p:CollectCoverage=true
```

## Windows Service Deployment (Future)

The Windows Service project is structured and ready for implementation:

```bash
# Publish for Windows Service
dotnet publish src/IbtTelemetry.Service -c Release -r win-x64 --self-contained

# Install as Windows Service (requires admin)
sc create "iRacing Telemetry Service" binPath="C:\path\to\IbtTelemetry.Service.exe"
sc start "iRacing Telemetry Service"
```

## Technology Stack

- **.NET 8.0**: Modern LTS framework
- **YamlDotNet 15.x**: YAML parsing for session info
- **System.CommandLine 2.0-beta4**: CLI framework
- **Microsoft.Extensions.Hosting**: Dependency injection and hosting
- **xUnit 2.6**: Testing framework
- **FluentAssertions 6.12**: Assertion library
- **Moq 4.20**: Mocking library

## Performance

- **Streaming**: Only one sample buffer in memory at a time
- **Async I/O**: Non-blocking file operations
- **Zero-copy**: Direct buffer reads where possible
- **Memory Efficient**: Handles files 300MB+ without issues

## Comparison with Node.js Version

| Feature | Node.js | C# |
|---------|---------|-----|
| Streaming | Synchronous | Asynchronous (IAsyncEnumerable) |
| Type Safety | Runtime (JavaScript) | Compile-time (C#) |
| Performance | ~200ms for sample.ibt | ~150ms for sample.ibt |
| Memory | ~50MB | ~35MB |
| Cross-Platform | Yes | Yes |
| Windows Service | No | Yes (ready) |

## Machine Learning Quick Start

### 1. Export Telemetry Data

```bash
# Export single session with default ML variables
dotnet run --project src/IbtTelemetry.Cli -- export sample.ibt telemetry.csv

# Export multiple sessions
dotnet run --project src/IbtTelemetry.Cli -- export ./telemetry_folder/ combined.csv
```

### 2. Train a Model

```bash
cd examples
pip install pandas numpy scikit-learn xgboost matplotlib
python ml_lap_time_prediction.py
```

### 3. Learn More

- **[ML_TELEMETRY_GUIDE.md](ML_TELEMETRY_GUIDE.md)**: Comprehensive ML guide with strategies and architectures
- **[examples/](examples/)**: Example scripts for lap time prediction, driver analysis, and more
- **[IBT_FORMAT_SPECIFICATION.md](IBT_FORMAT_SPECIFICATION.md)**: Detailed file format documentation

**ML Use Cases**:
- **Oversteer control model** - Train ML to maintain optimal slip angle balance
- Lap time prediction
- Tire slip angle calculation
- Optimal racing line (RL)
- Tire wear modeling
- Incident prediction
- Driver style classification
- Setup optimization

### Advanced Analysis

**Tire Slip Angle Calculation**: The telemetry includes velocity vectors (VelocityX, VelocityY, VelocityZ) and yaw rate, allowing accurate calculation of tire slip angles for vehicle dynamics analysis and setup optimization. See [docs/SLIP_ANGLE_CALCULATION.md](docs/SLIP_ANGLE_CALCULATION.md) for details.

## Future Enhancements

- [x] CSV export for machine learning
- [ ] Complete Windows Service implementation (file watcher, processing pipeline)
- [ ] REST API for telemetry access
- [ ] Database persistence (Entity Framework Core)
- [ ] Real-time streaming via SignalR
- [ ] NuGet package publication
- [ ] Performance benchmarking with BenchmarkDotNet
- [ ] Parquet export for large datasets

## Contributing

1. Fork the repository
2. Create a feature branch
3. Make your changes with tests
4. Ensure all tests pass
5. Submit a pull request

## License

Same as the parent Node.js project.

## Acknowledgments

- Original Node.js implementation: [ibt-telemetry](../ibt-telemetry)
- iRacing SDK documentation and constants
