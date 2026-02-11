namespace SlideGuide.Core.Models;

/// <summary>
/// Represents the configuration for an overlay.
/// </summary>
public class OverlayConfiguration
{
    /// <summary>
    /// Gets or sets the unique identifier for this overlay.
    /// </summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the display name of the overlay.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets whether the overlay is currently visible.
    /// </summary>
    public bool IsVisible { get; set; }

    /// <summary>
    /// Gets or sets the position and size of the overlay.
    /// </summary>
    public OverlayPosition Position { get; set; } = new();

    /// <summary>
    /// Gets or sets the opacity of the overlay (0.0 to 1.0).
    /// </summary>
    public float Opacity { get; set; } = 0.9f;

    /// <summary>
    /// Gets or sets whether the overlay can be repositioned by the user.
    /// </summary>
    public bool IsLocked { get; set; }

    /// <summary>
    /// Gets or sets additional configuration properties specific to the overlay type.
    /// </summary>
    public Dictionary<string, object> Properties { get; set; } = new();
}
