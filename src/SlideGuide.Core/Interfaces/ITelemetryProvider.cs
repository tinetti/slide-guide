using SlideGuide.Core.Models;

namespace SlideGuide.Core.Interfaces;

/// <summary>
/// Provides access to telemetry data from a sim racing application.
/// </summary>
public interface ITelemetryProvider
{
    /// <summary>
    /// Gets whether the telemetry provider is currently connected to the sim.
    /// </summary>
    bool IsConnected { get; }

    /// <summary>
    /// Event raised when new telemetry data is available.
    /// </summary>
    event EventHandler<TelemetryData>? TelemetryUpdated;

    /// <summary>
    /// Event raised when the connection status changes.
    /// </summary>
    event EventHandler<bool>? ConnectionStatusChanged;

    /// <summary>
    /// Starts the telemetry provider and begins polling for data.
    /// </summary>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    Task StartAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Stops the telemetry provider.
    /// </summary>
    Task StopAsync();

    /// <summary>
    /// Gets the current telemetry data snapshot.
    /// </summary>
    TelemetryData GetCurrentData();
}
