using AudioHub.Core.Models;
using Xunit;

namespace AudioHub.Core.Tests;

public sealed class AudioRouteTests
{
    [Fact]
    public void EffectiveVolume_WhenActiveAndUnmuted_CalculatesCorrectProduct()
    {
        // Arrange
        var source = new AudioSource
        {
            Id = "src-1",
            DisplayName = "Phone Media",
            Type = SourceType.RemoteMedia,
            Volume = 0.8f,
            IsMuted = false
        };

        var output = new AudioOutput
        {
            Id = "out-1",
            DisplayName = "Mustang GoBoult Torq",
            MasterVolume = 0.5f,
            IsMuted = false
        };

        var route = new AudioRoute
        {
            Id = "route-1",
            Source = source,
            Output = output,
            IsActive = true,
            RouteGain = 1.0f
        };

        // Act
        float effectiveVolume = route.EffectiveVolume;

        // Assert: 0.8 * 1.0 * 0.5 = 0.4
        Assert.Equal(0.4f, effectiveVolume, precision: 3);
    }

    [Theory]
    [InlineData(true, false, true)]  // Source muted
    [InlineData(false, true, true)]  // Output muted
    [InlineData(false, false, false)] // Route inactive
    public void EffectiveVolume_WhenMutedOrInactive_ReturnsZero(bool sourceMuted, bool outputMuted, bool routeActive)
    {
        // Arrange
        var source = new AudioSource
        {
            Id = "src-1",
            DisplayName = "Phone Media",
            Type = SourceType.RemoteMedia,
            Volume = 0.8f,
            IsMuted = sourceMuted
        };

        var output = new AudioOutput
        {
            Id = "out-1",
            DisplayName = "Mustang GoBoult Torq",
            MasterVolume = 0.8f,
            IsMuted = outputMuted
        };

        var route = new AudioRoute
        {
            Id = "route-1",
            Source = source,
            Output = output,
            IsActive = routeActive,
            RouteGain = 1.0f
        };

        // Act & Assert
        Assert.Equal(0f, route.EffectiveVolume);
    }

    [Fact]
    public void EffectiveVolume_ClampsToMaximumOne()
    {
        // Arrange
        var source = new AudioSource
        {
            Id = "src-1",
            DisplayName = "PC YouTube",
            Type = SourceType.LocalApplication,
            Volume = 1.0f,
            IsMuted = false
        };

        var output = new AudioOutput
        {
            Id = "out-1",
            DisplayName = "Mustang GoBoult Torq",
            MasterVolume = 1.0f,
            IsMuted = false
        };

        var route = new AudioRoute
        {
            Id = "route-1",
            Source = source,
            Output = output,
            IsActive = true,
            RouteGain = 2.0f // Boosted gain
        };

        // Act & Assert
        Assert.Equal(1.0f, route.EffectiveVolume);
    }
}
