using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;
using SlideGuide.App.Services;
using SlideGuide.App.ViewModels;
using SlideGuide.Core.Interfaces;

namespace SlideGuide.App;

/// <summary>
/// The main control window for SlideGuide.
/// </summary>
public sealed partial class MainWindow : Window
{
    private readonly MainViewModel _viewModel;
    private readonly ITelemetryProvider _telemetryProvider;
    private readonly IOverlayManager _overlayManager;
    private readonly WindowManager _windowManager;

    public MainWindow()
    {
        InitializeComponent();

        // Get services from DI container
        _telemetryProvider = App.ServiceProvider.GetRequiredService<ITelemetryProvider>();
        _overlayManager = App.ServiceProvider.GetRequiredService<IOverlayManager>();
        _windowManager = App.ServiceProvider.GetRequiredService<WindowManager>();
        _viewModel = App.ServiceProvider.GetRequiredService<MainViewModel>();

        // Subscribe to events
        _telemetryProvider.ConnectionStatusChanged += OnConnectionStatusChanged;
        BrakeOverlayCheckbox.Checked += (s, e) => _windowManager.ShowOverlay("brake-input");
        BrakeOverlayCheckbox.Unchecked += (s, e) => _windowManager.HideOverlay("brake-input");
        ThrottleOverlayCheckbox.Checked += (s, e) => _windowManager.ShowOverlay("throttle-input");
        ThrottleOverlayCheckbox.Unchecked += (s, e) => _windowManager.HideOverlay("throttle-input");
    }

    private async void StartStopButton_Click(object sender, RoutedEventArgs e)
    {
        if (_telemetryProvider.IsConnected || _viewModel.IsStarted)
        {
            await _viewModel.StopTelemetryAsync();
            StartStopButton.Content = "Start Telemetry";
        }
        else
        {
            await _viewModel.StartTelemetryAsync();
            StartStopButton.Content = "Stop Telemetry";
        }
    }

    private void ResetPositionsButton_Click(object sender, RoutedEventArgs e)
    {
        _windowManager.ResetAllPositions();
    }

    private void OnConnectionStatusChanged(object? sender, bool isConnected)
    {
        DispatcherQueue.TryEnqueue(() =>
        {
            if (isConnected)
            {
                StatusIndicator.Fill = new SolidColorBrush(Colors.Green);
                StatusText.Text = "Connected to iRacing";
            }
            else
            {
                StatusIndicator.Fill = new SolidColorBrush(Colors.Red);
                StatusText.Text = "Disconnected";
            }
        });
    }
}
