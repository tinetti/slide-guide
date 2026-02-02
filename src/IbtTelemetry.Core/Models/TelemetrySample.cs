using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using IbtTelemetry.Core.Constants;
using IbtTelemetry.Core.Models.Headers;
using static IbtTelemetry.Core.Constants.IrsdkConstants;

namespace IbtTelemetry.Core.Models;

/// <summary>
/// Represents a single telemetry sample
/// Parses parameter values from binary buffer using variable headers
/// </summary>
public class TelemetrySample
{
    private readonly byte[] _buffer;
    private readonly IReadOnlyList<VarHeader> _varHeaders;

    /// <summary>
    /// Create a telemetry sample from binary buffer and variable headers
    /// </summary>
    public TelemetrySample(byte[] buffer, IReadOnlyList<VarHeader> varHeaders)
    {
        _buffer = buffer ?? throw new ArgumentNullException(nameof(buffer));
        _varHeaders = varHeaders ?? throw new ArgumentNullException(nameof(varHeaders));
    }

    /// <summary>
    /// Get a parameter by name (case-insensitive)
    /// </summary>
    public TelemetryParameter? GetParameter(string name)
    {
        var header = _varHeaders.FirstOrDefault(h =>
            string.Equals(h.Name, name, StringComparison.OrdinalIgnoreCase));

        if (header == null)
        {
            return null;
        }

        var typeInfo = IrsdkConstants.GetTypeInfo(header.Type);
        var value = ExtractValue(header, typeInfo);

        return new TelemetryParameter(
            header.Name,
            header.Description,
            value,
            header.Unit
        );
    }

    /// <summary>
    /// Convert all parameters to a dictionary
    /// </summary>
    public Dictionary<string, Dictionary<string, object?>> ToJsonDictionary()
    {
        var result = new Dictionary<string, Dictionary<string, object?>>();

        foreach (var header in _varHeaders)
        {
            var typeInfo = IrsdkConstants.GetTypeInfo(header.Type);
            var value = ExtractValue(header, typeInfo);

            result[header.Name] = new Dictionary<string, object?>
            {
                ["value"] = value,
                ["unit"] = header.Unit
            };
        }

        return result;
    }

    /// <summary>
    /// Extract a value from the buffer based on variable header
    /// </summary>
    private object? ExtractValue(VarHeader header, IrsdkConstants.VarTypeInfo typeInfo)
    {
        var offset = header.Offset;

        // Handle array types
        if (header.Count > 1)
        {
            return ExtractArrayValue(header, typeInfo);
        }

        // Handle single values
        return header.Type switch
        {
            VarType.Char => ExtractString(offset, typeInfo.Size),
            VarType.Bool => _buffer[offset] != 0,
            VarType.Int => BitConverter.ToInt32(_buffer, offset),
            VarType.BitField => BitConverter.ToUInt32(_buffer, offset),
            VarType.Float => BitConverter.ToSingle(_buffer, offset),
            VarType.Double => BitConverter.ToDouble(_buffer, offset),
            _ => null
        };
    }

    /// <summary>
    /// Extract an array value from the buffer
    /// </summary>
    private object? ExtractArrayValue(VarHeader header, IrsdkConstants.VarTypeInfo typeInfo)
    {
        var offset = header.Offset;
        var count = header.Count;

        return header.Type switch
        {
            VarType.Char => ExtractString(offset, count),
            VarType.Bool => ExtractBoolArray(offset, count),
            VarType.Int => ExtractInt32Array(offset, count),
            VarType.BitField => ExtractUInt32Array(offset, count),
            VarType.Float => ExtractFloatArray(offset, count),
            VarType.Double => ExtractDoubleArray(offset, count),
            _ => null
        };
    }

    private string ExtractString(int offset, int length)
    {
        var nullIndex = Array.IndexOf(_buffer, (byte)0, offset, Math.Min(length, _buffer.Length - offset));
        var strLength = nullIndex >= 0 ? nullIndex - offset : length;
        return Encoding.ASCII.GetString(_buffer, offset, strLength);
    }

    private bool[] ExtractBoolArray(int offset, int count)
    {
        var result = new bool[count];
        for (int i = 0; i < count; i++)
        {
            result[i] = _buffer[offset + i] != 0;
        }
        return result;
    }

    private int[] ExtractInt32Array(int offset, int count)
    {
        var result = new int[count];
        for (int i = 0; i < count; i++)
        {
            result[i] = BitConverter.ToInt32(_buffer, offset + (i * 4));
        }
        return result;
    }

    private uint[] ExtractUInt32Array(int offset, int count)
    {
        var result = new uint[count];
        for (int i = 0; i < count; i++)
        {
            result[i] = BitConverter.ToUInt32(_buffer, offset + (i * 4));
        }
        return result;
    }

    private float[] ExtractFloatArray(int offset, int count)
    {
        var result = new float[count];
        for (int i = 0; i < count; i++)
        {
            result[i] = BitConverter.ToSingle(_buffer, offset + (i * 4));
        }
        return result;
    }

    private double[] ExtractDoubleArray(int offset, int count)
    {
        var result = new double[count];
        for (int i = 0; i < count; i++)
        {
            result[i] = BitConverter.ToDouble(_buffer, offset + (i * 8));
        }
        return result;
    }
}
