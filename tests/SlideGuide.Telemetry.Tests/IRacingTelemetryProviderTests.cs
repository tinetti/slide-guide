using FluentAssertions;
using SlideGuide.Core.Models;
using SlideGuide.Telemetry;

namespace SlideGuide.Telemetry.Tests;

public class IRacingTelemetryProviderTests
{
    [Fact]
    public void Constructor_ShouldInitializeWithDefaultInterval()
    {
        // Act
        var provider = new IRacingTelemetryProvider();

        // Assert
        provider.Should().NotBeNull();
        provider.IsConnected.Should().BeFalse();
    }

    [Fact]
    public void Constructor_WithCustomInterval_ShouldAccept()
    {
        // Act
        var provider = new IRacingTelemetryProvider(pollingIntervalMs: 33);

        // Assert
        provider.Should().NotBeNull();
    }

    [Fact]
    public void GetCurrentData_BeforeConnection_ShouldReturnEmptyData()
    {
        // Arrange
        var provider = new IRacingTelemetryProvider();

        // Act
        var data = provider.GetCurrentData();

        // Assert
        data.Should().NotBeNull();
        data.IsConnected.Should().BeFalse();
    }

    [Fact]
    public async Task StartAsync_ShouldNotThrow()
    {
        // Arrange
        var provider = new IRacingTelemetryProvider();

        // Act
        var act = async () => await provider.StartAsync();

        // Assert
        await act.Should().NotThrowAsync();

        // Cleanup
        await provider.StopAsync();
    }

    [Fact]
    public async Task StopAsync_ShouldNotThrow()
    {
        // Arrange
        var provider = new IRacingTelemetryProvider();
        await provider.StartAsync();

        // Act
        var act = async () => await provider.StopAsync();

        // Assert
        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task ConnectionStatusChanged_WhenStartedWithoutIRacing_ShouldRemainDisconnected()
    {
        // Arrange
        var provider = new IRacingTelemetryProvider();
        var connectionStatusChanged = false;
        provider.ConnectionStatusChanged += (s, isConnected) => connectionStatusChanged = true;

        // Act
        await provider.StartAsync();
        await Task.Delay(100); // Give it time to check for iRacing
        await provider.StopAsync();

        // Assert
        // On macOS, iRacing won't be running, so we should remain disconnected
        // Connection status change event may or may not fire depending on timing
        provider.IsConnected.Should().BeFalse();
    }
}
