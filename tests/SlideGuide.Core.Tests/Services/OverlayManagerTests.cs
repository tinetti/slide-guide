using FluentAssertions;
using Moq;
using SlideGuide.Core.Interfaces;
using SlideGuide.Core.Models;
using SlideGuide.Core.Services;

namespace SlideGuide.Core.Tests.Services;

public class OverlayManagerTests
{
    private readonly OverlayManager _sut;

    public OverlayManagerTests()
    {
        _sut = new OverlayManager();
    }

    [Fact]
    public void RegisterOverlay_WithValidOverlay_ShouldAddToCollection()
    {
        // Arrange
        var overlay = CreateMockOverlay("test-1");

        // Act
        _sut.RegisterOverlay(overlay);

        // Assert
        _sut.Overlays.Should().Contain(overlay);
    }

    [Fact]
    public void RegisterOverlay_WithDuplicateId_ShouldThrowException()
    {
        // Arrange
        var overlay1 = CreateMockOverlay("test-1");
        var overlay2 = CreateMockOverlay("test-1");
        _sut.RegisterOverlay(overlay1);

        // Act & Assert
        var act = () => _sut.RegisterOverlay(overlay2);
        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void UnregisterOverlay_WithExistingOverlay_ShouldRemoveFromCollection()
    {
        // Arrange
        var overlay = CreateMockOverlay("test-1");
        _sut.RegisterOverlay(overlay);

        // Act
        _sut.UnregisterOverlay("test-1");

        // Assert
        _sut.Overlays.Should().NotContain(overlay);
    }

    [Fact]
    public void GetOverlay_WithExistingId_ShouldReturnOverlay()
    {
        // Arrange
        var overlay = CreateMockOverlay("test-1");
        _sut.RegisterOverlay(overlay);

        // Act
        var result = _sut.GetOverlay("test-1");

        // Assert
        result.Should().Be(overlay);
    }

    [Fact]
    public void GetOverlay_WithNonExistingId_ShouldReturnNull()
    {
        // Act
        var result = _sut.GetOverlay("non-existing");

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public void ShowOverlay_WithExistingOverlay_ShouldSetVisibilityAndRaiseEvent()
    {
        // Arrange
        var overlay = CreateMockOverlay("test-1");
        _sut.RegisterOverlay(overlay);
        IOverlay? raisedOverlay = null;
        _sut.OverlayVisibilityChanged += (s, o) => raisedOverlay = o;

        // Act
        _sut.ShowOverlay("test-1");

        // Assert
        overlay.Configuration.IsVisible.Should().BeTrue();
        raisedOverlay.Should().Be(overlay);
    }

    [Fact]
    public void HideOverlay_WithVisibleOverlay_ShouldSetVisibilityAndRaiseEvent()
    {
        // Arrange
        var overlay = CreateMockOverlay("test-1");
        _sut.RegisterOverlay(overlay);
        _sut.ShowOverlay("test-1");
        IOverlay? raisedOverlay = null;
        _sut.OverlayVisibilityChanged += (s, o) => raisedOverlay = o;

        // Act
        _sut.HideOverlay("test-1");

        // Assert
        overlay.Configuration.IsVisible.Should().BeFalse();
        raisedOverlay.Should().Be(overlay);
    }

    [Fact]
    public void UpdateOverlays_WithVisibleOverlays_ShouldCallUpdateOnEach()
    {
        // Arrange
        var overlay1 = new Mock<IOverlay>();
        var overlay2 = new Mock<IOverlay>();
        SetupMockOverlay(overlay1, "test-1", true);
        SetupMockOverlay(overlay2, "test-2", true);

        _sut.RegisterOverlay(overlay1.Object);
        _sut.RegisterOverlay(overlay2.Object);

        var telemetryData = new TelemetryData { IsConnected = true };

        // Act
        _sut.UpdateOverlays(telemetryData);

        // Assert
        overlay1.Verify(o => o.Update(telemetryData), Times.Once);
        overlay2.Verify(o => o.Update(telemetryData), Times.Once);
    }

    [Fact]
    public void UpdateOverlays_WithHiddenOverlays_ShouldNotCallUpdate()
    {
        // Arrange
        var overlay = new Mock<IOverlay>();
        SetupMockOverlay(overlay, "test-1", false);
        _sut.RegisterOverlay(overlay.Object);

        var telemetryData = new TelemetryData { IsConnected = true };

        // Act
        _sut.UpdateOverlays(telemetryData);

        // Assert
        overlay.Verify(o => o.Update(It.IsAny<TelemetryData>()), Times.Never);
    }

    [Fact]
    public void UpdateOverlayConfiguration_WithExistingOverlay_ShouldUpdateAndRaiseEvent()
    {
        // Arrange
        var overlay = CreateMockOverlay("test-1");
        _sut.RegisterOverlay(overlay);
        IOverlay? raisedOverlay = null;
        _sut.OverlayConfigurationChanged += (s, o) => raisedOverlay = o;

        var newConfig = new OverlayConfiguration
        {
            Id = "test-1",
            IsVisible = true,
            Opacity = 0.5f
        };

        // Act
        _sut.UpdateOverlayConfiguration("test-1", newConfig);

        // Assert
        overlay.Configuration.Should().Be(newConfig);
        raisedOverlay.Should().Be(overlay);
    }

    private IOverlay CreateMockOverlay(string id)
    {
        var mock = new Mock<IOverlay>();
        mock.SetupGet(o => o.Id).Returns(id);
        mock.SetupGet(o => o.Name).Returns($"Overlay {id}");
        mock.SetupGet(o => o.RendererType).Returns("Test");
        mock.SetupProperty(o => o.Configuration, new OverlayConfiguration
        {
            Id = id,
            Name = $"Overlay {id}",
            IsVisible = false
        });
        return mock.Object;
    }

    private void SetupMockOverlay(Mock<IOverlay> mock, string id, bool isVisible)
    {
        mock.SetupGet(o => o.Id).Returns(id);
        mock.SetupGet(o => o.Name).Returns($"Overlay {id}");
        mock.SetupGet(o => o.RendererType).Returns("Test");
        mock.SetupGet(o => o.Configuration).Returns(new OverlayConfiguration
        {
            Id = id,
            IsVisible = isVisible
        });
    }
}
