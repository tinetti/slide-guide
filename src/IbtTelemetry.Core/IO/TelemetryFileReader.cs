using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using IbtTelemetry.Core.Models;
using IbtTelemetry.Core.Models.Headers;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace IbtTelemetry.Core.IO;

/// <summary>
/// Reads iRacing telemetry from a stream
/// </summary>
public class TelemetryFileReader : ITelemetryReader
{
    private readonly Stream _stream;
    private readonly bool _leaveOpen;

    /// <summary>
    /// Create a reader from a stream
    /// </summary>
    /// <param name="stream">Stream to read from</param>
    /// <param name="leaveOpen">Whether to leave the stream open after reading headers</param>
    public TelemetryFileReader(Stream stream, bool leaveOpen = true)
    {
        _stream = stream ?? throw new ArgumentNullException(nameof(stream));
        _leaveOpen = leaveOpen;
    }

    /// <summary>
    /// Read telemetry headers and session info
    /// Stream is kept open for sample reading
    /// </summary>
    public async Task<Telemetry> ReadAsync(CancellationToken cancellationToken = default)
    {
        using var reader = new BinaryReader(_stream, Encoding.ASCII, leaveOpen: true);

        // Read telemetry header (112 bytes)
        var telemetryHeader = TelemetryHeader.FromBinaryReader(reader);

        // Read disk sub-header (32 bytes)
        var diskSubHeader = DiskSubHeader.FromBinaryReader(reader);

        // Read session info YAML
        _stream.Seek(telemetryHeader.SessionInfoOffset, SeekOrigin.Begin);
        var sessionInfoBytes = new byte[telemetryHeader.SessionInfoLength];
        await _stream.ReadAsync(sessionInfoBytes.AsMemory(0, telemetryHeader.SessionInfoLength), cancellationToken);

        var sessionInfoYaml = Encoding.UTF8.GetString(sessionInfoBytes).TrimEnd('\0');
        var sessionInfo = ParseSessionInfo(sessionInfoYaml);

        // Read variable headers
        _stream.Seek(telemetryHeader.VarHeaderOffset, SeekOrigin.Begin);
        var varHeaders = new List<VarHeader>(telemetryHeader.NumVars);

        for (int i = 0; i < telemetryHeader.NumVars; i++)
        {
            var varHeader = VarHeader.FromBinaryReader(reader);
            varHeaders.Add(varHeader);
        }

        // Create Telemetry instance with the stream (kept open for sample reading)
        return new Telemetry(
            telemetryHeader,
            diskSubHeader,
            sessionInfo,
            varHeaders,
            _stream
        );
    }

    /// <summary>
    /// Parse session info YAML
    /// </summary>
    private SessionInfo ParseSessionInfo(string yaml)
    {
        try
        {
            var deserializer = new DeserializerBuilder()
                .WithNamingConvention(PascalCaseNamingConvention.Instance)
                .IgnoreUnmatchedProperties()
                .Build();

            return deserializer.Deserialize<SessionInfo>(yaml) ?? new SessionInfo();
        }
        catch (Exception ex)
        {
            throw new InvalidDataException("Failed to parse session info YAML", ex);
        }
    }
}
