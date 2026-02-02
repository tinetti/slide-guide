# iRacing Telemetry File (.ibt) Format Specification

**Version:** 1.0
**Date:** 2026-02-01
**Based on:** iRacing SDK Version 2

## Table of Contents

1. [Overview](#overview)
2. [File Structure](#file-structure)
3. [Binary Layout](#binary-layout)
4. [Header Structures](#header-structures)
5. [Data Types](#data-types)
6. [Session Information](#session-information)
7. [Variable Headers](#variable-headers)
8. [Telemetry Samples](#telemetry-samples)
9. [Enumerations and Constants](#enumerations-and-constants)
10. [Reading Strategy](#reading-strategy)
11. [Examples](#examples)

---

## Overview

The `.ibt` file format is a binary format used by iRacing to store recorded telemetry data from racing sessions. It contains metadata about the session, variable definitions, and time-series telemetry samples captured during gameplay.

### Key Characteristics

- **Binary Format**: Little-endian byte order
- **Fixed Headers**: Predictable structure with known offsets
- **YAML Metadata**: Session information stored as YAML
- **Streaming Data**: Telemetry samples stored sequentially
- **Typical Size**: 100MB - 1GB+ depending on session length

---

## File Structure

The `.ibt` file consists of five main sections in order:

```
┌─────────────────────────────────────┐
│ 1. Telemetry Header (112 bytes)     │  ← File structure metadata
├─────────────────────────────────────┤
│ 2. Disk Sub-Header (32 bytes)       │  ← Session timing metadata
├─────────────────────────────────────┤
│ 3. Session Info (variable)          │  ← YAML-encoded session data
├─────────────────────────────────────┤
│ 4. Variable Headers (144×N bytes)   │  ← N variable definitions
├─────────────────────────────────────┤
│ 5. Sample Data (bufLen×numBuf)      │  ← Telemetry samples
└─────────────────────────────────────┘
```

---

## Binary Layout

### Byte Offsets

| Offset | Size | Section | Description |
|--------|------|---------|-------------|
| 0      | 112  | Header  | Telemetry Header |
| 112    | 32   | Header  | Disk Sub-Header |
| 144+   | Var  | Data    | Session Info (offset specified in header) |
| Var    | Var  | Data    | Variable Headers (offset specified in header) |
| Var    | Var  | Data    | Sample Buffers (offset specified in header) |

---

## Header Structures

### 1. Telemetry Header (112 bytes)

The main header containing file structure information. All fields are 32-bit signed integers (int32).

#### Structure

| Offset | Type  | Field Name         | Description |
|--------|-------|--------------------|-------------|
| 0      | int32 | Version            | SDK version (typically 2) |
| 4      | int32 | Status             | Status field |
| 8      | int32 | TickRate           | Telemetry update rate in Hz (e.g., 60) |
| 12     | int32 | SessionInfoUpdate  | Session info update counter |
| 16     | int32 | SessionInfoLength  | Length of session info YAML in bytes |
| 20     | int32 | SessionInfoOffset  | Byte offset to session info YAML |
| 24     | int32 | NumVars            | Number of variable headers |
| 28     | int32 | VarHeaderOffset    | Byte offset to variable headers array |
| 32     | int32 | NumBuf             | Number of telemetry sample buffers |
| 36     | int32 | BufLen             | Length of each sample buffer in bytes |
| 40-48  | int32 | *Reserved*         | Unused (3 integers) |
| 52     | int32 | BufOffset          | Byte offset to telemetry sample data |
| 56-111 | int32 | *Reserved*         | Additional unused fields (14 integers) |

**Total Size:** 28 × 4 bytes = 112 bytes

#### Field Details

- **Version**: Always 2 for current SDK
- **TickRate**: Typically 60 Hz (60 samples per second)
- **NumVars**: Usually 287 variables (varies by iRacing version)
- **NumBuf**: Number of telemetry samples = session duration × tick rate
- **BufLen**: Size of one sample in bytes (typically ~1464 bytes)

---

### 2. Disk Sub-Header (32 bytes)

Session timing metadata written when recording to disk.

#### Structure

| Offset | Type   | Field Name   | Description |
|--------|--------|--------------|-------------|
| 0      | float  | StartDate    | Start date (float representation) |
| 4      | double | StartTime    | Session start time in seconds |
| 12     | double | EndTime      | Session end time in seconds |
| 20     | int32  | LapCount     | Number of laps completed |
| 24     | int32  | RecordCount  | Number of telemetry records |
| 28-31  | *pad*  | *Padding*    | 4 bytes padding |

**Total Size:** 32 bytes

#### Field Details

- **StartTime/EndTime**: Timestamps in seconds since session start
- **LapCount**: Total completed laps during recording
- **RecordCount**: Should equal NumBuf from main header

---

### 3. Variable Header (144 bytes)

Defines a single telemetry variable's type, location, and metadata. There are `NumVars` of these structures starting at `VarHeaderOffset`.

#### Structure

| Offset | Type      | Size | Field Name   | Description |
|--------|-----------|------|--------------|-------------|
| 0      | int32     | 4    | Type         | Variable data type (0-5) |
| 4      | int32     | 4    | Offset       | Byte offset within sample buffer |
| 8      | int32     | 4    | Count        | Number of elements (1 for scalar, >1 for array) |
| 12     | byte      | 1    | CountAsTime  | Boolean: whether count represents time samples |
| 13     | byte[3]   | 3    | *Padding*    | Alignment padding |
| 16     | char[32]  | 32   | Name         | Variable name (null-terminated ASCII) |
| 48     | char[64]  | 64   | Description  | Variable description (null-terminated ASCII) |
| 112    | char[32]  | 32   | Unit         | Unit of measurement (null-terminated ASCII) |

**Total Size:** 144 bytes

#### Field Details

- **Type**: See [Data Types](#data-types) section
- **Offset**: Position in sample buffer to read this variable
- **Count**: 1 for single values, >1 for arrays (e.g., tire temps have Count=4)
- **Name**: Variable identifier (e.g., "Speed", "RPM", "LFtempCL")
- **Unit**: Measurement unit (e.g., "m/s", "rpm", "C", "%")

---

## Data Types

### Variable Types (VarType enum)

| Value | Name     | Size | C# Type | Description |
|-------|----------|------|---------|-------------|
| 0     | Char     | 1    | byte    | ASCII character / string |
| 1     | Bool     | 1    | bool    | Boolean (0 = false, non-zero = true) |
| 2     | Int      | 4    | int32   | Signed 32-bit integer |
| 3     | BitField | 4    | uint32  | Unsigned 32-bit bitfield |
| 4     | Float    | 4    | float   | 32-bit floating point |
| 5     | Double   | 8    | double  | 64-bit floating point |

### Type Information

```csharp
VarType.Char      → irsdk_char (1 byte)
VarType.Bool      → irsdk_bool (1 byte)
VarType.Int       → irsdk_int (4 bytes)
VarType.BitField  → irsdk_bitField (4 bytes)
VarType.Float     → irsdk_float (4 bytes)
VarType.Double    → irsdk_double (8 bytes)
```

### Reading Values

**Single Values (Count = 1):**
```
Read [Size] bytes at [Offset] in sample buffer
Convert to appropriate type based on VarType
```

**Array Values (Count > 1):**
```
For i = 0 to Count-1:
    Read [Size] bytes at [Offset + (i × Size)]
    Convert to appropriate type
```

**String Values (VarType.Char, Count > 1):**
```
Read Count bytes at Offset
Find null terminator (0x00)
Decode as ASCII string
```

---

## Session Information

### Structure

Session information is stored as YAML text starting at `SessionInfoOffset` with length `SessionInfoLength`.

#### Format

- **Encoding**: UTF-8
- **Format**: YAML (indented key-value pairs)
- **Termination**: Null-padded to SessionInfoLength

#### Top-Level Keys

```yaml
WeekendInfo:
  TrackName: sachsenring
  TrackID: 521
  SessionID: 12345
  SubSessionID: 67890
  # ... more fields

SessionInfo:
  Sessions:
    - SessionNum: 0
      SessionType: Practice
      # ... session details

DriverInfo:
  DriverCarIdx: 0
  Drivers:
    - CarIdx: 0
      UserName: "Driver Name"
      # ... driver details

# ... additional sections
```

#### Common Fields

**WeekendInfo:**
- TrackName: Track identifier
- TrackID: Numeric track ID
- SessionID: Session identifier
- SubSessionID: Sub-session identifier

**SessionInfo:**
- Sessions[]: Array of session data
  - SessionType: Practice/Qualifying/Race
  - SessionTime: Duration in seconds
  - SessionLaps: Number of laps

**DriverInfo:**
- DriverCarIdx: Player's car index
- Drivers[]: Array of all drivers
  - UserName: Driver name
  - CarNumber: Car number
  - IRating: iRating

### Parsing

```csharp
// Read YAML bytes
byte[] yamlBytes = ReadBytes(SessionInfoOffset, SessionInfoLength);
string yaml = Encoding.UTF8.GetString(yamlBytes).TrimEnd('\0');

// Parse with YAML deserializer
var sessionInfo = YamlDeserializer.Deserialize(yaml);
```

---

## Variable Headers

### Reading Variable Headers

Starting at `VarHeaderOffset`, read `NumVars` variable headers sequentially:

```
For i = 0 to NumVars-1:
    Seek to VarHeaderOffset + (i × 144)
    Read VarHeader structure (144 bytes)
```

### Example Variables

| Name        | Type     | Count | Offset | Unit  | Description |
|-------------|----------|-------|--------|-------|-------------|
| SessionTime | Double   | 1     | 0      | s     | Session time in seconds |
| Speed       | Float    | 1     | 8      | m/s   | GPS vehicle speed |
| RPM         | Float    | 1     | 12     | rpm   | Engine RPM |
| Gear        | Int      | 1     | 16     |       | Current gear (-1=R, 0=N, 1-8) |
| LFtempCL    | Float    | 1     | 20     | C     | LF tire left carcass temp |
| SessionFlags| BitField | 1     | 24     |       | Session status flags |

---

## Telemetry Samples

### Sample Buffer Structure

Each telemetry sample is a binary buffer of `BufLen` bytes containing values for all variables.

#### Layout

```
Sample Buffer (BufLen bytes)
├─ Variable 0: [Type] at Offset N
├─ Variable 1: [Type] at Offset N+M
├─ Variable 2: [Type] at Offset N+M+O
└─ ...
```

### Reading Samples

**Sequential Reading:**
```
Seek to BufOffset
For i = 0 to NumBuf-1:
    Read BufLen bytes into buffer
    Parse variables using VarHeaders
```

**Random Access:**
```
sampleOffset = BufOffset + (index × BufLen)
Seek to sampleOffset
Read BufLen bytes
Parse variables using VarHeaders
```

### Extracting Variable Values

```csharp
foreach (var varHeader in varHeaders)
{
    var offset = varHeader.Offset;
    var value = varHeader.Type switch
    {
        VarType.Char => ReadString(buffer, offset, varHeader.Count),
        VarType.Bool => buffer[offset] != 0,
        VarType.Int => BitConverter.ToInt32(buffer, offset),
        VarType.BitField => BitConverter.ToUInt32(buffer, offset),
        VarType.Float => BitConverter.ToSingle(buffer, offset),
        VarType.Double => BitConverter.ToDouble(buffer, offset)
    };
}
```

---

## Enumerations and Constants

### Engine Warnings (BitField)

| Bit | Value | Flag                 | Description |
|-----|-------|----------------------|-------------|
| 0   | 0x01  | WaterTempWarning     | Water temperature warning |
| 1   | 0x02  | FuelPressureWarning  | Fuel pressure warning |
| 2   | 0x04  | OilPressureWarning   | Oil pressure warning |
| 3   | 0x08  | EngineStalled        | Engine is stalled |
| 4   | 0x10  | PitSpeedLimiter      | Pit speed limiter active |
| 5   | 0x20  | RevLimiterActive     | Rev limiter active |
| 6   | 0x40  | OilTempWarning       | Oil temperature warning |

### Session Flags (BitField)

**Global Flags:**
| Bit | Value      | Flag          | Description |
|-----|------------|---------------|-------------|
| 0   | 0x00000001 | Checkered     | Checkered flag |
| 1   | 0x00000002 | White         | White flag |
| 2   | 0x00000004 | Green         | Green flag |
| 3   | 0x00000008 | Yellow        | Yellow flag |
| 4   | 0x00000010 | Red           | Red flag |
| 5   | 0x00000020 | Blue          | Blue flag |
| 6   | 0x00000040 | Debris        | Debris on track |
| 14  | 0x00004000 | Caution       | Caution period |

**Driver Flags:**
| Bit | Value      | Flag         | Description |
|-----|------------|--------------|-------------|
| 16  | 0x00010000 | Black        | Black flag |
| 17  | 0x00020000 | Disqualify   | Disqualified |
| 18  | 0x00040000 | Servicible   | Car is servicible |

### Track Location (TrkLoc)

| Value | Name           | Description |
|-------|----------------|-------------|
| -1    | NotInWorld     | Not in world |
| 0     | OffTrack       | Off track |
| 1     | InPitStall     | In pit stall |
| 2     | AproachingPits | Approaching pits |
| 3     | OnTrack        | On track |

### Track Surface (TrkSurf)

| Value | Name                | Description |
|-------|---------------------|-------------|
| -1    | SurfaceNotInWorld   | Not in world |
| 0     | UndefinedMaterial   | Undefined |
| 1-4   | Asphalt1-4Material  | Asphalt (various types) |
| 5-6   | Concrete1-2Material | Concrete |
| 7-8   | RacingDirt1-2       | Racing dirt |
| 15-18 | Grass1-4Material    | Grass |
| 19-22 | Dirt1-4Material     | Dirt |
| 23    | SandMaterial        | Sand |
| 24-25 | Gravel1-2Material   | Gravel |

### Session State

| Value | Name       | Description |
|-------|------------|-------------|
| 0     | Invalid    | Invalid state |
| 1     | GetInCar   | Getting in car |
| 2     | Warmup     | Warm-up period |
| 3     | ParadeLaps | Parade laps |
| 4     | Racing     | Racing |
| 5     | Checkered  | Checkered flag |
| 6     | CoolDown   | Cool-down lap |

### Pit Service Flags (BitField)

| Bit | Value  | Flag              | Description |
|-----|--------|-------------------|-------------|
| 0   | 0x0001 | LFTireChange      | Left front tire change |
| 1   | 0x0002 | RFTireChange      | Right front tire change |
| 2   | 0x0004 | LRTireChange      | Left rear tire change |
| 3   | 0x0008 | RRTireChange      | Right rear tire change |
| 4   | 0x0010 | FuelFill          | Fuel fill |
| 5   | 0x0020 | WindshieldTearoff | Windshield tearoff |
| 6   | 0x0040 | FastRepair        | Fast repair |

---

## Reading Strategy

### Efficient File Reading

**1. Read Headers First:**
```
1. Read TelemetryHeader (0-111 bytes)
2. Read DiskSubHeader (112-143 bytes)
3. Seek to SessionInfoOffset, read SessionInfoLength bytes
4. Seek to VarHeaderOffset, read NumVars × 144 bytes
```

**2. Stream Samples:**
```
Seek to BufOffset
For each sample:
    Read BufLen bytes
    Process immediately
    Release buffer (memory efficient)
```

### Memory-Efficient Processing

For large files (300MB+):

- **Don't load all samples**: Use streaming/async enumeration
- **Process one at a time**: Read → Process → Release
- **Use async I/O**: Non-blocking file operations

### Example (C#)

```csharp
public async IAsyncEnumerable<TelemetrySample> GetSamplesAsync()
{
    var buffer = new byte[BufLen];
    stream.Seek(BufOffset, SeekOrigin.Begin);

    for (int i = 0; i < NumBuf; i++)
    {
        await stream.ReadAsync(buffer, 0, BufLen);
        yield return new TelemetrySample(buffer, VarHeaders);
    }
}
```

---

## Examples

### Example File Structure

**Small Session Example:**
- Duration: 5 minutes
- Tick Rate: 60 Hz
- Variables: 287
- Samples: 5 × 60 × 60 = 18,000

**Binary Layout:**
```
Offset      | Size        | Section
------------|-------------|------------------
0x00000000  | 112 bytes   | TelemetryHeader
0x00000070  | 32 bytes    | DiskSubHeader
0x00000090  | ~8 KB       | SessionInfo (YAML)
0x00002090  | 41,328 B    | VarHeaders (287 × 144)
0x0000C1D0  | ~26 MB      | Samples (18,000 × 1464)
```

### Reading a Specific Variable

**Read "Speed" from Sample #100:**

```csharp
// 1. Find VarHeader for "Speed"
var speedHeader = varHeaders.First(h => h.Name == "Speed");
// Type: Float, Offset: 8, Count: 1

// 2. Calculate sample offset
var sampleOffset = BufOffset + (100 × BufLen);

// 3. Seek and read sample buffer
stream.Seek(sampleOffset, SeekOrigin.Begin);
var buffer = new byte[BufLen];
stream.Read(buffer, 0, BufLen);

// 4. Extract speed value
var speed = BitConverter.ToSingle(buffer, speedHeader.Offset);
// Speed in m/s
```

### Calculating File Size

```
FileSize = BufOffset + (NumBuf × BufLen)

Example:
  BufOffset = 50,000 bytes
  NumBuf = 18,000 samples
  BufLen = 1,464 bytes/sample

  FileSize = 50,000 + (18,000 × 1,464)
          = 50,000 + 26,352,000
          = 26,402,000 bytes
          ≈ 26.4 MB
```

---

## Version History

| Version | Date       | Changes |
|---------|------------|---------|
| 1.0     | 2026-02-01 | Initial specification |

---

## References

- iRacing SDK Documentation
- iRacing Developer Forums
- Sample implementations: C#, Node.js

---

## License

This specification is based on publicly available iRacing SDK information and reverse engineering of the .ibt file format.
