using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using IbtTelemetry.Core.Services;
using Xunit;

namespace IbtTelemetry.Integration.Tests;

public class TelemetryFileReaderIntegrationTests
{
    private readonly string _testFilePath;

    public TelemetryFileReaderIntegrationTests()
    {
        // Get path to test data file
        var baseDir = AppContext.BaseDirectory;
        _testFilePath = Path.Combine(baseDir, "TestData", "sample.ibt");
    }

    [Fact]
    public async Task LoadFromFile_WithValidFile_LoadsSuccessfully()
    {
        // Arrange
        var service = new TelemetryService();

        // Act
        using var telemetry = await service.LoadFromFileAsync(_testFilePath);

        // Assert
        telemetry.Should().NotBeNull();
        telemetry.Header.Should().NotBeNull();
        telemetry.DiskHeader.Should().NotBeNull();
        telemetry.SessionInfo.Should().NotBeNull();
        telemetry.VarHeaders.Should().NotBeEmpty();
    }

    [Fact]
    public async Task LoadFromFile_VerifyHeaderValues()
    {
        // Arrange
        var service = new TelemetryService();

        // Act
        using var telemetry = await service.LoadFromFileAsync(_testFilePath);

        // Assert
        telemetry.Header.Version.Should().Be(2);
        telemetry.Header.TickRate.Should().Be(60);
        telemetry.Header.NumVars.Should().Be(287);
        telemetry.Header.NumBuf.Should().Be(1);
    }

    [Fact]
    public async Task LoadFromFile_VerifySessionInfo()
    {
        // Arrange
        var service = new TelemetryService();

        // Act
        using var telemetry = await service.LoadFromFileAsync(_testFilePath);

        // Assert
        telemetry.SessionInfo.WeekendInfo.Should().NotBeNull();
        telemetry.SessionInfo.WeekendInfo.Should().ContainKey("TrackName");
    }

    [Fact]
    public async Task GetSamplesAsync_StreamsSuccessfully()
    {
        // Arrange
        var service = new TelemetryService();
        using var telemetry = await service.LoadFromFileAsync(_testFilePath);

        // Act
        var samples = new System.Collections.Generic.List<Core.Models.TelemetrySample>();
        await foreach (var sample in telemetry.GetSamplesAsync())
        {
            samples.Add(sample);
            if (samples.Count >= 1) break;
        }

        // Assert
        samples.Should().HaveCount(1);
        samples[0].Should().NotBeNull();
    }

    [Fact]
    public async Task GetSamplesAsync_CanExtractParameters()
    {
        // Arrange
        var service = new TelemetryService();
        using var telemetry = await service.LoadFromFileAsync(_testFilePath);

        // Act
        Core.Models.TelemetrySample? sample = null;
        await foreach (var s in telemetry.GetSamplesAsync())
        {
            sample = s;
            break;
        }

        var speed = sample!.GetParameter("Speed");
        var rpm = sample.GetParameter("RPM");

        // Assert
        speed.Should().NotBeNull();
        rpm.Should().NotBeNull();
        speed!.Unit.Should().NotBeEmpty();
        rpm!.Unit.Should().NotBeEmpty();
    }

    [Fact]
    public async Task GetUniqueId_GeneratesId()
    {
        // Arrange
        var service = new TelemetryService();
        using var telemetry = await service.LoadFromFileAsync(_testFilePath);

        // Act
        var uniqueId = telemetry.GetUniqueId();

        // Assert
        uniqueId.Should().NotBeNullOrEmpty();
        uniqueId.Should().Contain("-");
    }

    [Fact]
    public async Task LoadFromFile_WithNonExistentFile_ThrowsException()
    {
        // Arrange
        var service = new TelemetryService();
        var nonExistentPath = Path.Combine(Path.GetTempPath(), "nonexistent.ibt");

        // Act & Assert
        await Assert.ThrowsAsync<FileNotFoundException>(async () =>
        {
            await service.LoadFromFileAsync(nonExistentPath);
        });
    }
}
