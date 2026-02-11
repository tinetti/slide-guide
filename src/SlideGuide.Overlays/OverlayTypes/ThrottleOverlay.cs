using SlideGuide.Core.Models;

namespace SlideGuide.Overlays.OverlayTypes;

/// <summary>
/// Overlay that displays throttle pedal input.
/// </summary>
public class ThrottleOverlay : BaseOverlay
{
    /// <inheritdoc/>
    public override string Id => "throttle-input";

    /// <inheritdoc/>
    public override string Name => "Throttle Input";

    /// <inheritdoc/>
    public override string RendererType => "ThrottleInput";

    /// <inheritdoc/>
    protected override OverlayPosition GetDefaultPosition()
    {
        return new OverlayPosition(170, 100, 100, 340);
    }

    /// <inheritdoc/>
    protected override void OnUpdate(TelemetryData data)
    {
        // Throttle-specific update logic can go here
        // For example, tracking max throttle, etc.
    }
}
