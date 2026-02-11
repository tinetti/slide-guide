namespace SlideGuide.Core.Models;

/// <summary>
/// Represents the position and size of an overlay window.
/// </summary>
public class OverlayPosition
{
    /// <summary>
    /// Gets or sets the X coordinate (left edge) in screen pixels.
    /// </summary>
    public int X { get; set; }

    /// <summary>
    /// Gets or sets the Y coordinate (top edge) in screen pixels.
    /// </summary>
    public int Y { get; set; }

    /// <summary>
    /// Gets or sets the width in pixels.
    /// </summary>
    public int Width { get; set; }

    /// <summary>
    /// Gets or sets the height in pixels.
    /// </summary>
    public int Height { get; set; }

    /// <summary>
    /// Creates a new overlay position with specified coordinates and dimensions.
    /// </summary>
    public OverlayPosition(int x, int y, int width, int height)
    {
        X = x;
        Y = y;
        Width = width;
        Height = height;
    }

    /// <summary>
    /// Creates a new overlay position with default values.
    /// </summary>
    public OverlayPosition() : this(0, 0, 200, 400)
    {
    }
}
