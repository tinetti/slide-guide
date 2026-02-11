using SlideGuide.Core.Interfaces;

namespace SlideGuide.Rendering.OverlayRenderers;

/// <summary>
/// Renders the brake pedal input as a vertical bar.
/// </summary>
public class BrakeInputRenderer
{
    private const float BarWidth = 60f;
    private const float BarHeight = 300f;
    private const float Padding = 20f;

    /// <summary>
    /// Renders the brake input visualization.
    /// </summary>
    /// <param name="renderer">The renderer to use.</param>
    /// <param name="context">The render context.</param>
    public void Render(IRenderer renderer, RenderContext context)
    {
        if (context.TelemetryData == null || !context.TelemetryData.IsConnected)
        {
            DrawDisconnectedState(renderer, context);
            return;
        }

        var brakeValue = Math.Clamp(context.TelemetryData.Brake, 0f, 1f);

        // Calculate positions
        var x = Padding;
        var y = Padding;

        // Draw background bar (dark gray)
        renderer.DrawRectangle(x, y, BarWidth, BarHeight, 0.2f, 0.2f, 0.2f, 0.8f);

        // Draw brake bar (red, bottom to top based on value)
        var filledHeight = BarHeight * brakeValue;
        var filledY = y + BarHeight - filledHeight;
        renderer.DrawRectangle(x, filledY, BarWidth, filledHeight, 0.9f, 0.1f, 0.1f, 0.9f);

        // Draw percentage indicator using small rectangles (since text rendering is not yet implemented)
        DrawPercentageIndicator(renderer, x, y, BarHeight, brakeValue);
    }

    private void DrawDisconnectedState(IRenderer renderer, RenderContext context)
    {
        var x = Padding;
        var y = Padding;

        // Draw background bar (darker)
        renderer.DrawRectangle(x, y, BarWidth, BarHeight, 0.1f, 0.1f, 0.1f, 0.6f);

        // Draw "X" pattern to indicate no data
        DrawXPattern(renderer, x + BarWidth / 2 - 15, y + BarHeight / 2 - 15, 30);
    }

    private void DrawPercentageIndicator(IRenderer renderer, float x, float y, float barHeight, float value)
    {
        // Draw small indicator marks at 25%, 50%, 75%, 100%
        var marks = new[] { 0.25f, 0.5f, 0.75f, 1.0f };
        foreach (var mark in marks)
        {
            var markY = y + barHeight * (1 - mark);
            var markColor = value >= mark ? (1f, 1f, 1f) : (0.4f, 0.4f, 0.4f);
            renderer.DrawRectangle(x + BarWidth + 5, markY - 1, 10, 2, markColor.Item1, markColor.Item2, markColor.Item3, 0.8f);
        }
    }

    private void DrawXPattern(IRenderer renderer, float x, float y, float size)
    {
        // Draw diagonal lines to form an X
        for (int i = 0; i < (int)size; i++)
        {
            renderer.DrawRectangle(x + i, y + i, 2, 2, 0.5f, 0.5f, 0.5f, 0.8f);
            renderer.DrawRectangle(x + size - i, y + i, 2, 2, 0.5f, 0.5f, 0.5f, 0.8f);
        }
    }
}
