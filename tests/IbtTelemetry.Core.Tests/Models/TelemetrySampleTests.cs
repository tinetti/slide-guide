using System;
using System.Collections.Generic;
using FluentAssertions;
using IbtTelemetry.Core.Constants;
using IbtTelemetry.Core.Models;
using IbtTelemetry.Core.Models.Headers;
using Xunit;

namespace IbtTelemetry.Core.Tests.Models;

public class TelemetrySampleTests
{
    [Fact]
    public void GetParameter_WithValidFloat_ReturnsParameter()
    {
        // Arrange
        var buffer = new byte[16];
        BitConverter.GetBytes(123.45f).CopyTo(buffer, 0);

        var varHeaders = new List<VarHeader>
        {
            new VarHeader
            {
                Type = IrsdkConstants.VarType.Float,
                Offset = 0,
                Count = 1,
                Name = "Speed",
                Description = "Speed in m/s",
                Unit = "m/s"
            }
        };

        var sample = new TelemetrySample(buffer, varHeaders);

        // Act
        var param = sample.GetParameter("Speed");

        // Assert
        param.Should().NotBeNull();
        param!.Name.Should().Be("Speed");
        param.Value.Should().BeOfType<float>();
        ((float)param.Value!).Should().BeApproximately(123.45f, 0.01f);
        param.Unit.Should().Be("m/s");
    }

    [Fact]
    public void GetParameter_WithInt_ReturnsParameter()
    {
        // Arrange
        var buffer = new byte[16];
        BitConverter.GetBytes(5000).CopyTo(buffer, 0);

        var varHeaders = new List<VarHeader>
        {
            new VarHeader
            {
                Type = IrsdkConstants.VarType.Int,
                Offset = 0,
                Count = 1,
                Name = "RPM",
                Description = "Engine RPM",
                Unit = "revs/min"
            }
        };

        var sample = new TelemetrySample(buffer, varHeaders);

        // Act
        var param = sample.GetParameter("RPM");

        // Assert
        param.Should().NotBeNull();
        param!.Value.Should().BeOfType<int>();
        ((int)param.Value!).Should().Be(5000);
    }

    [Fact]
    public void GetParameter_CaseInsensitive_ReturnsParameter()
    {
        // Arrange
        var buffer = new byte[16];
        BitConverter.GetBytes(100.0f).CopyTo(buffer, 0);

        var varHeaders = new List<VarHeader>
        {
            new VarHeader
            {
                Type = IrsdkConstants.VarType.Float,
                Offset = 0,
                Count = 1,
                Name = "Throttle",
                Description = "Throttle position",
                Unit = "%"
            }
        };

        var sample = new TelemetrySample(buffer, varHeaders);

        // Act
        var param1 = sample.GetParameter("throttle");
        var param2 = sample.GetParameter("THROTTLE");
        var param3 = sample.GetParameter("Throttle");

        // Assert
        param1.Should().NotBeNull();
        param2.Should().NotBeNull();
        param3.Should().NotBeNull();
    }

    [Fact]
    public void GetParameter_WithNonExistent_ReturnsNull()
    {
        // Arrange
        var buffer = new byte[16];
        var varHeaders = new List<VarHeader>
        {
            new VarHeader
            {
                Type = IrsdkConstants.VarType.Float,
                Offset = 0,
                Count = 1,
                Name = "Speed",
                Description = "Speed",
                Unit = "m/s"
            }
        };

        var sample = new TelemetrySample(buffer, varHeaders);

        // Act
        var param = sample.GetParameter("NonExistent");

        // Assert
        param.Should().BeNull();
    }

    [Fact]
    public void ToJsonDictionary_ReturnsAllParameters()
    {
        // Arrange
        var buffer = new byte[16];
        BitConverter.GetBytes(123.45f).CopyTo(buffer, 0);
        BitConverter.GetBytes(5000).CopyTo(buffer, 4);

        var varHeaders = new List<VarHeader>
        {
            new VarHeader
            {
                Type = IrsdkConstants.VarType.Float,
                Offset = 0,
                Count = 1,
                Name = "Speed",
                Description = "Speed",
                Unit = "m/s"
            },
            new VarHeader
            {
                Type = IrsdkConstants.VarType.Int,
                Offset = 4,
                Count = 1,
                Name = "RPM",
                Description = "Engine RPM",
                Unit = "revs/min"
            }
        };

        var sample = new TelemetrySample(buffer, varHeaders);

        // Act
        var dict = sample.ToJsonDictionary();

        // Assert
        dict.Should().HaveCount(2);
        dict.Should().ContainKey("Speed");
        dict.Should().ContainKey("RPM");
        dict["Speed"]["value"].Should().BeOfType<float>();
        dict["RPM"]["value"].Should().BeOfType<int>();
    }
}
