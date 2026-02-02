using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using IbtTelemetry.Core.Models.Headers;

namespace IbtTelemetry.Core.Models;

/// <summary>
/// Represents a complete iRacing telemetry file
/// Provides streaming access to telemetry samples
/// </summary>
public class Telemetry : IDisposable
{
    private readonly Stream _stream;
    private bool _disposed;

    /// <summary>
    /// Telemetry header
    /// </summary>
    public TelemetryHeader Header { get; init; }

    /// <summary>
    /// Disk sub-header with timing information
    /// </summary>
    public DiskSubHeader DiskHeader { get; init; }

    /// <summary>
    /// Session information from YAML
    /// </summary>
    public SessionInfo SessionInfo { get; init; }

    /// <summary>
    /// Variable headers defining telemetry parameters
    /// </summary>
    public IReadOnlyList<VarHeader> VarHeaders { get; init; }

    /// <summary>
    /// Create a Telemetry instance
    /// </summary>
    public Telemetry(
        TelemetryHeader header,
        DiskSubHeader diskHeader,
        SessionInfo sessionInfo,
        IReadOnlyList<VarHeader> varHeaders,
        Stream stream)
    {
        Header = header ?? throw new ArgumentNullException(nameof(header));
        DiskHeader = diskHeader ?? throw new ArgumentNullException(nameof(diskHeader));
        SessionInfo = sessionInfo ?? throw new ArgumentNullException(nameof(sessionInfo));
        VarHeaders = varHeaders ?? throw new ArgumentNullException(nameof(varHeaders));
        _stream = stream ?? throw new ArgumentNullException(nameof(stream));
    }

    /// <summary>
    /// Generate unique ID for this session
    /// Format: {SubSessionID}-{SessionID}
    /// </summary>
    public string GetUniqueId()
    {
        var subSessionId = SessionInfo.GetWeekendValue<object>("SubSessionID");
        var sessionId = SessionInfo.GetWeekendValue<object>("SessionID");

        return $"{subSessionId}-{sessionId}";
    }

    /// <summary>
    /// Stream telemetry samples asynchronously
    /// Memory-efficient: only one sample in memory at a time
    /// </summary>
    public async IAsyncEnumerable<TelemetrySample> GetSamplesAsync(
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        var bufferSize = Header.BufLen;
        var sampleCount = Header.NumBuf;
        var buffer = new byte[bufferSize];

        // Seek to start of telemetry data
        _stream.Seek(Header.BufOffset, SeekOrigin.Begin);

        for (int i = 0; i < sampleCount; i++)
        {
            cancellationToken.ThrowIfCancellationRequested();

            // Read one sample buffer
            var bytesRead = await _stream.ReadAsync(buffer.AsMemory(0, bufferSize), cancellationToken);

            if (bytesRead != bufferSize)
            {
                throw new InvalidDataException(
                    $"Expected to read {bufferSize} bytes but got {bytesRead} at sample {i}");
            }

            // Create a copy of the buffer for this sample
            var sampleBuffer = new byte[bufferSize];
            Buffer.BlockCopy(buffer, 0, sampleBuffer, 0, bufferSize);

            yield return new TelemetrySample(sampleBuffer, VarHeaders);
        }
    }

    /// <summary>
    /// Get a specific sample by index
    /// </summary>
    public async Task<TelemetrySample> GetSampleAtAsync(int index, CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        if (index < 0 || index >= Header.NumBuf)
        {
            throw new ArgumentOutOfRangeException(
                nameof(index),
                $"Index must be between 0 and {Header.NumBuf - 1}");
        }

        var bufferSize = Header.BufLen;
        var buffer = new byte[bufferSize];

        // Calculate offset for this sample
        var sampleOffset = Header.BufOffset + (index * bufferSize);

        _stream.Seek(sampleOffset, SeekOrigin.Begin);

        var bytesRead = await _stream.ReadAsync(buffer.AsMemory(0, bufferSize), cancellationToken);

        if (bytesRead != bufferSize)
        {
            throw new InvalidDataException(
                $"Expected to read {bufferSize} bytes but got {bytesRead} at sample {index}");
        }

        return new TelemetrySample(buffer, VarHeaders);
    }

    /// <summary>
    /// Dispose resources
    /// </summary>
    public void Dispose()
    {
        if (!_disposed)
        {
            _stream?.Dispose();
            _disposed = true;
        }

        GC.SuppressFinalize(this);
    }
}
