using SlideGuide.Core.Models;

namespace SlideGuide.Overlays.OverlayTypes;

/// <summary>
/// Overlay that displays brake pedal input.
/// </summary>
public class BrakeOverlay : BaseOverlay
{
    /// <inheritdoc/>
    public override string Id => "brake-input";

    /// <inheritdoc/>
    public override string Name => "Brake Input";

    /// <inheritdoc/>
    public override string RendererType => "BrakeInput";

    /// <inheritdoc/>
    protected override OverlayPosition GetDefaultPosition()
    {
        return new OverlayPosition(50, 100, 100, 340);
    }

    /// <inheritdoc/>
    protected override void OnUpdate(TelemetryData data)
    {
        // Brake-specific update logic can go here
        // For example, tracking max brake pressure, etc.
    }
}
