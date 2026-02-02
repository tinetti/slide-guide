using System.IO;

namespace IbtTelemetry.Core.Models.Headers;

/// <summary>
/// Sub header used when writing telemetry to disk (32 bytes)
/// Contains session timing metadata
/// </summary>
public class DiskSubHeader
{
    /// <summary>Size of the header in bytes</summary>
    public const int SizeInBytes = 32;

    /// <summary>Start date (float representation)</summary>
    public float StartDate { get; init; }

    /// <summary>Start time in seconds</summary>
    public double StartTime { get; init; }

    /// <summary>End time in seconds</summary>
    public double EndTime { get; init; }

    /// <summary>Number of laps completed</summary>
    public int LapCount { get; init; }

    /// <summary>Number of telemetry records</summary>
    public int RecordCount { get; init; }

    /// <summary>
    /// Read DiskSubHeader from a BinaryReader
    /// </summary>
    public static DiskSubHeader FromBinaryReader(BinaryReader reader)
    {
        return new DiskSubHeader
        {
            StartDate = reader.ReadSingle(),    // 4 bytes (float)
            StartTime = reader.ReadDouble(),    // 8 bytes
            EndTime = reader.ReadDouble(),      // 8 bytes
            LapCount = reader.ReadInt32(),      // 4 bytes
            RecordCount = reader.ReadInt32()    // 4 bytes
        };
    }
}
