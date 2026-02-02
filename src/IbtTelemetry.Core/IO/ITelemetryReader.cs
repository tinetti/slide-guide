using System.Threading;
using System.Threading.Tasks;
using IbtTelemetry.Core.Models;

namespace IbtTelemetry.Core.IO;

/// <summary>
/// Interface for reading telemetry data
/// </summary>
public interface ITelemetryReader
{
    /// <summary>
    /// Read telemetry from the source
    /// </summary>
    /// <returns>Telemetry instance with stream kept open for sample reading</returns>
    Task<Telemetry> ReadAsync(CancellationToken cancellationToken = default);
}
