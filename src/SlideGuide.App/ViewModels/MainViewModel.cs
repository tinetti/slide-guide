using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SlideGuide.Core.Interfaces;

namespace SlideGuide.App.ViewModels;

/// <summary>
/// ViewModel for the main window.
/// </summary>
public partial class MainViewModel : ObservableObject
{
    private readonly ITelemetryProvider _telemetryProvider;
    private readonly IOverlayManager _overlayManager;

    [ObservableProperty]
    private bool _isStarted;

    [ObservableProperty]
    private bool _isConnected;

    public MainViewModel(ITelemetryProvider telemetryProvider, IOverlayManager overlayManager)
    {
        _telemetryProvider = telemetryProvider ?? throw new ArgumentNullException(nameof(telemetryProvider));
        _overlayManager = overlayManager ?? throw new ArgumentNullException(nameof(overlayManager));

        _telemetryProvider.ConnectionStatusChanged += OnConnectionStatusChanged;
        _telemetryProvider.TelemetryUpdated += OnTelemetryUpdated;
    }

    [RelayCommand]
    public async Task StartTelemetryAsync()
    {
        if (!IsStarted)
        {
            await _telemetryProvider.StartAsync();
            IsStarted = true;
        }
    }

    [RelayCommand]
    public async Task StopTelemetryAsync()
    {
        if (IsStarted)
        {
            await _telemetryProvider.StopAsync();
            IsStarted = false;
            IsConnected = false;
        }
    }

    private void OnConnectionStatusChanged(object? sender, bool isConnected)
    {
        IsConnected = isConnected;
    }

    private void OnTelemetryUpdated(object? sender, Core.Models.TelemetryData data)
    {
        // Update overlays with new telemetry data
        _overlayManager.UpdateOverlays(data);
    }
}
