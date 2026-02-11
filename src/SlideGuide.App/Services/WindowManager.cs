using SlideGuide.App.Windows;
using SlideGuide.Core.Interfaces;

namespace SlideGuide.App.Services;

/// <summary>
/// Manages the lifecycle of overlay windows.
/// </summary>
public class WindowManager
{
    private readonly Dictionary<string, OverlayWindow> _windows = new();
    private readonly IOverlayManager _overlayManager;

    public WindowManager(IOverlayManager overlayManager)
    {
        _overlayManager = overlayManager ?? throw new ArgumentNullException(nameof(overlayManager));
        _overlayManager.OverlayVisibilityChanged += OnOverlayVisibilityChanged;
    }

    /// <summary>
    /// Shows an overlay window.
    /// </summary>
    /// <param name="overlayId">The ID of the overlay to show.</param>
    public void ShowOverlay(string overlayId)
    {
        var overlay = _overlayManager.GetOverlay(overlayId);
        if (overlay == null)
        {
            return;
        }

        // Show the overlay in the overlay manager
        _overlayManager.ShowOverlay(overlayId);

        // Create window if it doesn't exist
        if (!_windows.ContainsKey(overlayId))
        {
            var window = new OverlayWindow(overlay);
            _windows[overlayId] = window;

            // TODO: Initialize renderer and start rendering
            // This will be implemented when the rendering pipeline is complete
        }

        // Show the window
        if (_windows.TryGetValue(overlayId, out var overlayWindow))
        {
            overlayWindow.Activate();
        }
    }

    /// <summary>
    /// Hides an overlay window.
    /// </summary>
    /// <param name="overlayId">The ID of the overlay to hide.</param>
    public void HideOverlay(string overlayId)
    {
        _overlayManager.HideOverlay(overlayId);

        if (_windows.TryGetValue(overlayId, out var window))
        {
            // On WinUI 3, hiding is done by closing the window
            // We'll keep the window object for later reuse
        }
    }

    /// <summary>
    /// Resets all overlay positions to their defaults.
    /// </summary>
    public void ResetAllPositions()
    {
        foreach (var overlay in _overlayManager.Overlays)
        {
            // Reset position (implementation depends on how positions are persisted)
            // For now, this is a placeholder
        }
    }

    /// <summary>
    /// Closes all overlay windows.
    /// </summary>
    public void CloseAllWindows()
    {
        foreach (var window in _windows.Values)
        {
            window.StopRendering();
            window.Close();
        }

        _windows.Clear();
    }

    private void OnOverlayVisibilityChanged(object? sender, IOverlay overlay)
    {
        if (overlay.Configuration.IsVisible)
        {
            ShowOverlay(overlay.Id);
        }
        else
        {
            HideOverlay(overlay.Id);
        }
    }
}
