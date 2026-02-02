using System.IO;
using System.Text;
using IbtTelemetry.Core.Constants;

namespace IbtTelemetry.Core.Models.Headers;

/// <summary>
/// Variable header (144 bytes)
/// Defines a telemetry parameter's type, location, and metadata
/// </summary>
public class VarHeader
{
    /// <summary>Size of the header in bytes</summary>
    public const int SizeInBytes = 144;

    /// <summary>Variable data type</summary>
    public IrsdkConstants.VarType Type { get; init; }

    /// <summary>Offset into buffer where data starts</summary>
    public int Offset { get; init; }

    /// <summary>Number of elements (for arrays)</summary>
    public int Count { get; init; }

    /// <summary>Whether count represents time samples</summary>
    public bool CountAsTime { get; init; }

    /// <summary>Variable name (max 32 chars)</summary>
    public string Name { get; init; } = string.Empty;

    /// <summary>Variable description (max 64 chars)</summary>
    public string Description { get; init; } = string.Empty;

    /// <summary>Unit of measurement (max 32 chars)</summary>
    public string Unit { get; init; } = string.Empty;

    /// <summary>
    /// Read VarHeader from a BinaryReader
    /// </summary>
    public static VarHeader FromBinaryReader(BinaryReader reader)
    {
        var type = (IrsdkConstants.VarType)reader.ReadInt32();    // 4 bytes
        var offset = reader.ReadInt32();            // 4 bytes
        var count = reader.ReadInt32();             // 4 bytes
        var countAsTime = reader.ReadByte() != 0;   // 1 byte

        // Skip 3 bytes of padding (16-byte alignment)
        reader.ReadBytes(3);

        // Read fixed-length strings (null-terminated ASCII)
        var name = ReadFixedString(reader, 32);         // 32 bytes
        var description = ReadFixedString(reader, 64);  // 64 bytes
        var unit = ReadFixedString(reader, 32);         // 32 bytes

        return new VarHeader
        {
            Type = type,
            Offset = offset,
            Count = count,
            CountAsTime = countAsTime,
            Name = name,
            Description = description,
            Unit = unit
        };
    }

    /// <summary>
    /// Read a fixed-length null-terminated ASCII string
    /// </summary>
    private static string ReadFixedString(BinaryReader reader, int length)
    {
        var bytes = reader.ReadBytes(length);

        // Find null terminator
        var nullIndex = Array.IndexOf(bytes, (byte)0);
        if (nullIndex >= 0)
        {
            return Encoding.ASCII.GetString(bytes, 0, nullIndex);
        }

        return Encoding.ASCII.GetString(bytes);
    }
}
