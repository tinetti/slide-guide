using SlideGuide.Core.Interfaces;
using SlideGuide.Core.Models;

namespace SlideGuide.Core.Services;

/// <summary>
/// Default implementation of the overlay manager.
/// </summary>
public class OverlayManager : IOverlayManager
{
    private readonly Dictionary<string, IOverlay> _overlays = new();
    private readonly object _lock = new();

    /// <inheritdoc/>
    public IReadOnlyList<IOverlay> Overlays
    {
        get
        {
            lock (_lock)
            {
                return _overlays.Values.ToList();
            }
        }
    }

    /// <inheritdoc/>
    public event EventHandler<IOverlay>? OverlayVisibilityChanged;

    /// <inheritdoc/>
    public event EventHandler<IOverlay>? OverlayConfigurationChanged;

    /// <inheritdoc/>
    public void RegisterOverlay(IOverlay overlay)
    {
        ArgumentNullException.ThrowIfNull(overlay);

        lock (_lock)
        {
            if (_overlays.ContainsKey(overlay.Id))
            {
                throw new InvalidOperationException($"An overlay with ID '{overlay.Id}' is already registered.");
            }

            _overlays[overlay.Id] = overlay;
        }
    }

    /// <inheritdoc/>
    public void UnregisterOverlay(string overlayId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(overlayId);

        lock (_lock)
        {
            _overlays.Remove(overlayId);
        }
    }

    /// <inheritdoc/>
    public IOverlay? GetOverlay(string overlayId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(overlayId);

        lock (_lock)
        {
            return _overlays.TryGetValue(overlayId, out var overlay) ? overlay : null;
        }
    }

    /// <inheritdoc/>
    public void ShowOverlay(string overlayId)
    {
        var overlay = GetOverlay(overlayId);
        if (overlay == null)
        {
            return;
        }

        if (!overlay.Configuration.IsVisible)
        {
            overlay.Configuration.IsVisible = true;
            OverlayVisibilityChanged?.Invoke(this, overlay);
        }
    }

    /// <inheritdoc/>
    public void HideOverlay(string overlayId)
    {
        var overlay = GetOverlay(overlayId);
        if (overlay == null)
        {
            return;
        }

        if (overlay.Configuration.IsVisible)
        {
            overlay.Configuration.IsVisible = false;
            OverlayVisibilityChanged?.Invoke(this, overlay);
        }
    }

    /// <inheritdoc/>
    public void UpdateOverlays(TelemetryData data)
    {
        ArgumentNullException.ThrowIfNull(data);

        List<IOverlay> visibleOverlays;
        lock (_lock)
        {
            visibleOverlays = _overlays.Values
                .Where(o => o.Configuration.IsVisible)
                .ToList();
        }

        foreach (var overlay in visibleOverlays)
        {
            overlay.Update(data);
        }
    }

    /// <inheritdoc/>
    public void UpdateOverlayConfiguration(string overlayId, OverlayConfiguration configuration)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(overlayId);
        ArgumentNullException.ThrowIfNull(configuration);

        var overlay = GetOverlay(overlayId);
        if (overlay == null)
        {
            return;
        }

        overlay.Configuration = configuration;
        OverlayConfigurationChanged?.Invoke(this, overlay);
    }
}
