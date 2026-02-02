using System.Threading;
using System.Threading.Tasks;
using IbtTelemetry.Core.Models;

namespace IbtTelemetry.Core.Services;

/// <summary>
/// Service for loading and working with telemetry files
/// </summary>
public interface ITelemetryService
{
    /// <summary>
    /// Load telemetry from a file
    /// </summary>
    /// <param name="filePath">Path to .ibt file</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Telemetry instance</returns>
    Task<Telemetry> LoadFromFileAsync(string filePath, CancellationToken cancellationToken = default);
}
