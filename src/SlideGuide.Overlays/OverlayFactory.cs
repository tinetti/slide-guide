using SlideGuide.Core.Interfaces;
using SlideGuide.Overlays.OverlayTypes;

namespace SlideGuide.Overlays;

/// <summary>
/// Factory for creating overlay instances.
/// </summary>
public class OverlayFactory
{
    private readonly Dictionary<string, Func<IOverlay>> _overlayCreators = new();

    /// <summary>
    /// Creates a new overlay factory with default overlay types registered.
    /// </summary>
    public OverlayFactory()
    {
        // Register default overlay types
        RegisterOverlayType("brake-input", () => new BrakeOverlay());
        RegisterOverlayType("throttle-input", () => new ThrottleOverlay());
    }

    /// <summary>
    /// Registers a new overlay type.
    /// </summary>
    /// <param name="overlayId">The unique identifier for the overlay type.</param>
    /// <param name="creator">A function that creates a new instance of the overlay.</param>
    public void RegisterOverlayType(string overlayId, Func<IOverlay> creator)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(overlayId);
        ArgumentNullException.ThrowIfNull(creator);

        _overlayCreators[overlayId] = creator;
    }

    /// <summary>
    /// Creates a new overlay instance.
    /// </summary>
    /// <param name="overlayId">The ID of the overlay type to create.</param>
    /// <returns>A new overlay instance, or null if the type is not registered.</returns>
    public IOverlay? CreateOverlay(string overlayId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(overlayId);

        if (_overlayCreators.TryGetValue(overlayId, out var creator))
        {
            return creator();
        }

        return null;
    }

    /// <summary>
    /// Gets all registered overlay type IDs.
    /// </summary>
    /// <returns>A collection of overlay type IDs.</returns>
    public IEnumerable<string> GetRegisteredOverlayTypes()
    {
        return _overlayCreators.Keys;
    }

    /// <summary>
    /// Creates all default overlays.
    /// </summary>
    /// <returns>A collection of all default overlay instances.</returns>
    public IEnumerable<IOverlay> CreateAllDefaultOverlays()
    {
        return _overlayCreators.Keys
            .Select(CreateOverlay)
            .Where(overlay => overlay != null)
            .Cast<IOverlay>();
    }
}
