using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;
using SlideGuide.Core.Interfaces;
using SlideGuide.Core.Models;
using SlideGuide.Rendering;
using System.Runtime.InteropServices;

namespace SlideGuide.App.Windows;

/// <summary>
/// Represents a transparent overlay window that hosts a renderer.
/// </summary>
public class OverlayWindow : Window
{
    private readonly IOverlay _overlay;
    private IRenderer? _renderer;
    private DispatcherTimer? _renderTimer;

    /// <summary>
    /// Creates a new overlay window for the specified overlay.
    /// </summary>
    /// <param name="overlay">The overlay to display.</param>
    public OverlayWindow(IOverlay overlay)
    {
        _overlay = overlay ?? throw new ArgumentNullException(nameof(overlay));

        // Configure window
        Title = overlay.Name;

        // Set window properties for transparent overlay
        SystemBackdrop = new MicaBackdrop { Kind = Microsoft.UI.Composition.SystemBackdrops.MicaKind.BaseAlt };

        // Set initial size and position
        var config = overlay.Configuration;
        AppWindow.MoveAndResize(new Windows.Graphics.RectInt32(
            config.Position.X,
            config.Position.Y,
            config.Position.Width,
            config.Position.Height));

        // TODO: Make window transparent and always on top
        // This requires P/Invoke to set extended window styles on Windows
        // Will be implemented when building on Windows

        Closed += OnClosed;
    }

    /// <summary>
    /// Initializes the renderer and starts rendering.
    /// </summary>
    public void StartRendering(IRenderer renderer)
    {
        _renderer = renderer ?? throw new ArgumentNullException(nameof(renderer));

        var config = _overlay.Configuration;
        _renderer.Initialize(config.Position.Width, config.Position.Height);

        // Start render loop
        _renderTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromMilliseconds(16) // ~60 FPS
        };
        _renderTimer.Tick += OnRenderTick;
        _renderTimer.Start();
    }

    /// <summary>
    /// Stops rendering.
    /// </summary>
    public void StopRendering()
    {
        _renderTimer?.Stop();
        _renderTimer = null;
    }

    /// <summary>
    /// Gets the overlay associated with this window.
    /// </summary>
    public IOverlay Overlay => _overlay;

    private void OnRenderTick(object? sender, object e)
    {
        if (_renderer == null)
        {
            return;
        }

        try
        {
            _renderer.BeginDraw();
            _renderer.Clear();

            // Create render context
            var context = new RenderContext
            {
                TelemetryData = _overlay.Configuration.IsVisible ? ((dynamic)_overlay).LastTelemetryData : null,
                Width = _overlay.Configuration.Position.Width,
                Height = _overlay.Configuration.Position.Height
            };

            // TODO: Render the overlay using the appropriate renderer based on RendererType
            // This will be implemented when the full rendering pipeline is set up

            _renderer.EndDraw();
        }
        catch
        {
            // Log error in production
        }
    }

    private void OnClosed(object sender, WindowEventArgs args)
    {
        StopRendering();
        _renderer?.Dispose();
        _renderer = null;
    }
}
