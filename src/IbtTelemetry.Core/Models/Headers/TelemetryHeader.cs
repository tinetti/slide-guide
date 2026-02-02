using System.IO;

namespace IbtTelemetry.Core.Models.Headers;

/// <summary>
/// iRacing Telemetry Header (112 bytes)
/// Contains file structure metadata
/// </summary>
public class TelemetryHeader
{
    /// <summary>Size of the header in bytes</summary>
    public const int SizeInBytes = 112;

    /// <summary>SDK version</summary>
    public int Version { get; init; }

    /// <summary>Status field</summary>
    public int Status { get; init; }

    /// <summary>Telemetry tick rate (Hz)</summary>
    public int TickRate { get; init; }

    /// <summary>Session info update counter</summary>
    public int SessionInfoUpdate { get; init; }

    /// <summary>Length of session info YAML string</summary>
    public int SessionInfoLength { get; init; }

    /// <summary>Offset to session info YAML string</summary>
    public int SessionInfoOffset { get; init; }

    /// <summary>Number of variable headers</summary>
    public int NumVars { get; init; }

    /// <summary>Offset to variable headers</summary>
    public int VarHeaderOffset { get; init; }

    /// <summary>Number of telemetry buffers</summary>
    public int NumBuf { get; init; }

    /// <summary>Length of each buffer in bytes</summary>
    public int BufLen { get; init; }

    /// <summary>Offset to telemetry buffers</summary>
    public int BufOffset { get; init; }

    /// <summary>
    /// Read TelemetryHeader from a BinaryReader
    /// </summary>
    public static TelemetryHeader FromBinaryReader(BinaryReader reader)
    {
        // Read all 28 int32 values (112 bytes / 4 bytes each)
        var parts = new int[28];
        for (int i = 0; i < 28; i++)
        {
            parts[i] = reader.ReadInt32();
        }

        return new TelemetryHeader
        {
            Version = parts[0],
            Status = parts[1],
            TickRate = parts[2],
            SessionInfoUpdate = parts[3],
            SessionInfoLength = parts[4],
            SessionInfoOffset = parts[5],
            NumVars = parts[6],
            VarHeaderOffset = parts[7],
            NumBuf = parts[8],
            BufLen = parts[9],
            // parts[10], [11], [12] are unused/padding
            BufOffset = parts[13]
            // parts[14-27] are additional unused fields
        };
    }
}
