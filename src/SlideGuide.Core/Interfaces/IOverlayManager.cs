using SlideGuide.Core.Models;

namespace SlideGuide.Core.Interfaces;

/// <summary>
/// Manages the lifecycle and state of overlays.
/// </summary>
public interface IOverlayManager
{
    /// <summary>
    /// Gets all registered overlays.
    /// </summary>
    IReadOnlyList<IOverlay> Overlays { get; }

    /// <summary>
    /// Event raised when an overlay's visibility changes.
    /// </summary>
    event EventHandler<IOverlay>? OverlayVisibilityChanged;

    /// <summary>
    /// Event raised when an overlay's configuration changes.
    /// </summary>
    event EventHandler<IOverlay>? OverlayConfigurationChanged;

    /// <summary>
    /// Registers an overlay with the manager.
    /// </summary>
    /// <param name="overlay">The overlay to register.</param>
    void RegisterOverlay(IOverlay overlay);

    /// <summary>
    /// Unregisters an overlay from the manager.
    /// </summary>
    /// <param name="overlayId">The ID of the overlay to unregister.</param>
    void UnregisterOverlay(string overlayId);

    /// <summary>
    /// Gets an overlay by its ID.
    /// </summary>
    /// <param name="overlayId">The ID of the overlay.</param>
    /// <returns>The overlay, or null if not found.</returns>
    IOverlay? GetOverlay(string overlayId);

    /// <summary>
    /// Shows an overlay.
    /// </summary>
    /// <param name="overlayId">The ID of the overlay to show.</param>
    void ShowOverlay(string overlayId);

    /// <summary>
    /// Hides an overlay.
    /// </summary>
    /// <param name="overlayId">The ID of the overlay to hide.</param>
    void HideOverlay(string overlayId);

    /// <summary>
    /// Updates all visible overlays with new telemetry data.
    /// </summary>
    /// <param name="data">The telemetry data.</param>
    void UpdateOverlays(TelemetryData data);

    /// <summary>
    /// Updates the configuration of an overlay.
    /// </summary>
    /// <param name="overlayId">The ID of the overlay.</param>
    /// <param name="configuration">The new configuration.</param>
    void UpdateOverlayConfiguration(string overlayId, OverlayConfiguration configuration);
}
