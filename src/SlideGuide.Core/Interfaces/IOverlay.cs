using SlideGuide.Core.Models;

namespace SlideGuide.Core.Interfaces;

/// <summary>
/// Represents an overlay that can be displayed and updated with telemetry data.
/// </summary>
public interface IOverlay
{
    /// <summary>
    /// Gets the unique identifier for this overlay.
    /// </summary>
    string Id { get; }

    /// <summary>
    /// Gets the display name of this overlay.
    /// </summary>
    string Name { get; }

    /// <summary>
    /// Gets or sets the configuration for this overlay.
    /// </summary>
    OverlayConfiguration Configuration { get; set; }

    /// <summary>
    /// Updates the overlay with new telemetry data.
    /// </summary>
    /// <param name="data">The telemetry data to display.</param>
    void Update(TelemetryData data);

    /// <summary>
    /// Gets the type of renderer required for this overlay.
    /// </summary>
    string RendererType { get; }
}
