using SlideGuide.Core.Interfaces;
using SlideGuide.Core.Models;

namespace SlideGuide.Overlays.OverlayTypes;

/// <summary>
/// Base class for all overlay implementations.
/// </summary>
public abstract class BaseOverlay : IOverlay
{
    private OverlayConfiguration _configuration;

    /// <inheritdoc/>
    public abstract string Id { get; }

    /// <inheritdoc/>
    public abstract string Name { get; }

    /// <inheritdoc/>
    public abstract string RendererType { get; }

    /// <inheritdoc/>
    public OverlayConfiguration Configuration
    {
        get => _configuration;
        set
        {
            _configuration = value ?? throw new ArgumentNullException(nameof(value));
            OnConfigurationChanged();
        }
    }

    /// <summary>
    /// Gets the last telemetry data that was updated.
    /// </summary>
    protected TelemetryData? LastTelemetryData { get; private set; }

    /// <summary>
    /// Creates a new base overlay with the specified configuration.
    /// </summary>
    protected BaseOverlay()
    {
        _configuration = new OverlayConfiguration
        {
            Id = Id,
            Name = Name,
            IsVisible = false,
            Position = GetDefaultPosition()
        };
    }

    /// <inheritdoc/>
    public void Update(TelemetryData data)
    {
        LastTelemetryData = data ?? throw new ArgumentNullException(nameof(data));
        OnUpdate(data);
    }

    /// <summary>
    /// Called when telemetry data is updated.
    /// </summary>
    /// <param name="data">The new telemetry data.</param>
    protected virtual void OnUpdate(TelemetryData data)
    {
        // Override in derived classes to handle updates
    }

    /// <summary>
    /// Called when the configuration changes.
    /// </summary>
    protected virtual void OnConfigurationChanged()
    {
        // Override in derived classes to handle configuration changes
    }

    /// <summary>
    /// Gets the default position for this overlay type.
    /// </summary>
    /// <returns>The default position.</returns>
    protected virtual OverlayPosition GetDefaultPosition()
    {
        return new OverlayPosition(100, 100, 100, 340);
    }
}
