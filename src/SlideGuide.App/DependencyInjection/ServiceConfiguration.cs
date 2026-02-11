using Microsoft.Extensions.DependencyInjection;
using SlideGuide.App.Services;
using SlideGuide.App.ViewModels;
using SlideGuide.Core.Interfaces;
using SlideGuide.Core.Services;
using SlideGuide.Overlays;
using SlideGuide.Telemetry;

namespace SlideGuide.App.DependencyInjection;

/// <summary>
/// Configures services for dependency injection.
/// </summary>
public static class ServiceConfiguration
{
    /// <summary>
    /// Configures all application services.
    /// </summary>
    /// <param name="services">The service collection to configure.</param>
    public static void ConfigureServices(IServiceCollection services)
    {
        // Core services
        services.AddSingleton<IOverlayManager, OverlayManager>();
        services.AddSingleton<ITelemetryProvider, IRacingTelemetryProvider>();

        // Overlay factory and overlays
        services.AddSingleton<OverlayFactory>();
        services.AddSingleton(provider =>
        {
            var factory = provider.GetRequiredService<OverlayFactory>();
            var overlayManager = provider.GetRequiredService<IOverlayManager>();

            // Register all default overlays
            foreach (var overlay in factory.CreateAllDefaultOverlays())
            {
                overlayManager.RegisterOverlay(overlay);
            }

            return overlayManager;
        });

        // App services
        services.AddSingleton<WindowManager>();
        services.AddTransient<MainViewModel>();
    }
}
