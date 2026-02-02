using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using IbtTelemetry.Core.IO;
using IbtTelemetry.Core.Models;

namespace IbtTelemetry.Core.Services;

/// <summary>
/// Default implementation of ITelemetryService
/// </summary>
public class TelemetryService : ITelemetryService
{
    /// <summary>
    /// Load telemetry from a file
    /// </summary>
    public async Task<Telemetry> LoadFromFileAsync(string filePath, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(filePath))
        {
            throw new ArgumentException("File path cannot be null or empty", nameof(filePath));
        }

        if (!File.Exists(filePath))
        {
            throw new FileNotFoundException($"Telemetry file not found: {filePath}", filePath);
        }

        // Open file stream and keep it open for sample reading
        var fileStream = new FileStream(
            filePath,
            FileMode.Open,
            FileAccess.Read,
            FileShare.Read,
            bufferSize: 4096,
            useAsync: true
        );

        try
        {
            var reader = new TelemetryFileReader(fileStream, leaveOpen: true);
            return await reader.ReadAsync(cancellationToken);
        }
        catch
        {
            // If reading fails, dispose the stream
            await fileStream.DisposeAsync();
            throw;
        }
    }
}
