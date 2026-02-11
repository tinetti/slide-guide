using SlideGuide.Core.Models;

namespace SlideGuide.Rendering;

/// <summary>
/// Represents the context for a rendering operation.
/// </summary>
public class RenderContext
{
    /// <summary>
    /// Gets or sets the current telemetry data being rendered.
    /// </summary>
    public TelemetryData? TelemetryData { get; set; }

    /// <summary>
    /// Gets or sets the width of the rendering surface.
    /// </summary>
    public int Width { get; set; }

    /// <summary>
    /// Gets or sets the height of the rendering surface.
    /// </summary>
    public int Height { get; set; }

    /// <summary>
    /// Gets or sets the delta time since the last frame in seconds.
    /// </summary>
    public float DeltaTime { get; set; }
}
