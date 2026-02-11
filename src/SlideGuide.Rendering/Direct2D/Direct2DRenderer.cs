using SlideGuide.Core.Interfaces;
using Vortice.Direct2D1;
using Vortice.DXGI;
using Vortice.Mathematics;

namespace SlideGuide.Rendering.Direct2D;

/// <summary>
/// Implements rendering using Direct2D.
/// </summary>
public class Direct2DRenderer : IRenderer
{
    private ID2D1Factory? _factory;
    private ID2D1HwndRenderTarget? _renderTarget;
    private readonly Dictionary<Color4, ID2D1SolidColorBrush> _brushes = new();
    private IntPtr _hwnd;
    private bool _disposed;

    /// <summary>
    /// Creates a new Direct2D renderer for the specified window handle.
    /// </summary>
    /// <param name="hwnd">The window handle to render to.</param>
    public Direct2DRenderer(IntPtr hwnd)
    {
        _hwnd = hwnd;
    }

    /// <inheritdoc/>
    public void Initialize(int width, int height)
    {
        // Create Direct2D factory
        _factory = D2D1.D2D1CreateFactory<ID2D1Factory>();

        // Create render target
        var renderTargetProperties = new RenderTargetProperties
        {
            PixelFormat = new Vortice.DCommon.PixelFormat(Format.B8G8R8A8_UNorm, Vortice.DCommon.AlphaMode.Premultiplied)
        };

        var hwndRenderTargetProperties = new HwndRenderTargetProperties
        {
            Hwnd = _hwnd,
            PixelSize = new SizeI(width, height)
        };

        _renderTarget = _factory.CreateHwndRenderTarget(renderTargetProperties, hwndRenderTargetProperties);
    }

    /// <inheritdoc/>
    public void BeginDraw()
    {
        _renderTarget?.BeginDraw();
    }

    /// <inheritdoc/>
    public void EndDraw()
    {
        _renderTarget?.EndDraw();
    }

    /// <inheritdoc/>
    public void Clear()
    {
        // Clear with transparent background
        _renderTarget?.Clear(new Color4(0, 0, 0, 0));
    }

    /// <inheritdoc/>
    public void DrawRectangle(float x, float y, float width, float height, float r, float g, float b, float a = 1.0f)
    {
        if (_renderTarget == null)
        {
            return;
        }

        var color = new Color4(r, g, b, a);
        var brush = GetOrCreateBrush(color);
        var rect = new System.Drawing.RectangleF(x, y, width, height);

        _renderTarget.FillRectangle(rect, brush);
    }

    /// <inheritdoc/>
    public void DrawText(string text, float x, float y, float fontSize, float r, float g, float b, float a = 1.0f)
    {
        // TODO: Implement text rendering with DirectWrite
        // For now, text rendering is not implemented in this simplified version
        // This will be added when the full WinUI 3 integration is complete
    }

    /// <inheritdoc/>
    public void Resize(int width, int height)
    {
        if (_renderTarget != null)
        {
            var size = new SizeI(width, height);
            _renderTarget.Resize(size);
        }
    }

    private ID2D1SolidColorBrush GetOrCreateBrush(Color4 color)
    {
        if (_renderTarget == null)
        {
            throw new InvalidOperationException("Render target not initialized");
        }

        if (!_brushes.TryGetValue(color, out var brush))
        {
            brush = _renderTarget.CreateSolidColorBrush(color);
            _brushes[color] = brush;
        }

        return brush;
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        foreach (var brush in _brushes.Values)
        {
            brush?.Dispose();
        }
        _brushes.Clear();

        _renderTarget?.Dispose();
        _factory?.Dispose();

        _disposed = true;
    }
}
