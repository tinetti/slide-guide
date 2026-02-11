using SlideGuide.Core.Models;

namespace SlideGuide.Core.Interfaces;

/// <summary>
/// Represents a renderer that can draw overlays.
/// </summary>
public interface IRenderer : IDisposable
{
    /// <summary>
    /// Initializes the renderer with the specified dimensions.
    /// </summary>
    /// <param name="width">The width of the rendering surface.</param>
    /// <param name="height">The height of the rendering surface.</param>
    void Initialize(int width, int height);

    /// <summary>
    /// Begins a rendering frame.
    /// </summary>
    void BeginDraw();

    /// <summary>
    /// Ends a rendering frame and presents the result.
    /// </summary>
    void EndDraw();

    /// <summary>
    /// Clears the rendering surface with a transparent background.
    /// </summary>
    void Clear();

    /// <summary>
    /// Draws a filled rectangle.
    /// </summary>
    void DrawRectangle(float x, float y, float width, float height, float r, float g, float b, float a = 1.0f);

    /// <summary>
    /// Draws text at the specified position.
    /// </summary>
    void DrawText(string text, float x, float y, float fontSize, float r, float g, float b, float a = 1.0f);

    /// <summary>
    /// Resizes the rendering surface.
    /// </summary>
    void Resize(int width, int height);
}
