using FluentAssertions;
using SlideGuide.Core.Models;

namespace SlideGuide.Core.Tests.Models;

public class TelemetryDataTests
{
    [Fact]
    public void Empty_ShouldReturnDisconnectedData()
    {
        // Act
        var result = TelemetryData.Empty;

        // Assert
        result.IsConnected.Should().BeFalse();
        result.Timestamp.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public void NewInstance_ShouldHaveDefaultTimestamp()
    {
        // Act
        var result = new TelemetryData();

        // Assert
        result.Timestamp.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
    }

    [Theory]
    [InlineData(0.0f)]
    [InlineData(0.5f)]
    [InlineData(1.0f)]
    public void Throttle_ShouldAcceptValidValues(float value)
    {
        // Arrange
        var data = new TelemetryData();

        // Act
        data.Throttle = value;

        // Assert
        data.Throttle.Should().Be(value);
    }

    [Theory]
    [InlineData(0.0f)]
    [InlineData(0.5f)]
    [InlineData(1.0f)]
    public void Brake_ShouldAcceptValidValues(float value)
    {
        // Arrange
        var data = new TelemetryData();

        // Act
        data.Brake = value;

        // Assert
        data.Brake.Should().Be(value);
    }

    [Fact]
    public void TelemetryData_ShouldSupportFullRange()
    {
        // Arrange & Act
        var data = new TelemetryData
        {
            IsConnected = true,
            Throttle = 0.85f,
            Brake = 0.65f,
            Clutch = 0.0f,
            SteeringAngle = 0.25f,
            Speed = 45.5f,
            Gear = 3,
            Rpm = 5500f,
            LateralG = 1.2f,
            LongitudinalG = -0.8f,
            IsOnTrack = true
        };

        // Assert
        data.IsConnected.Should().BeTrue();
        data.Throttle.Should().Be(0.85f);
        data.Brake.Should().Be(0.65f);
        data.Clutch.Should().Be(0.0f);
        data.SteeringAngle.Should().Be(0.25f);
        data.Speed.Should().Be(45.5f);
        data.Gear.Should().Be(3);
        data.Rpm.Should().Be(5500f);
        data.LateralG.Should().Be(1.2f);
        data.LongitudinalG.Should().Be(-0.8f);
        data.IsOnTrack.Should().BeTrue();
    }
}
