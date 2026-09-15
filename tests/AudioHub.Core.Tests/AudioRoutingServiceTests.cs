using AudioHub.Core.Models;
using AudioHub.Core.Services;
using Xunit;

namespace AudioHub.Core.Tests;

public sealed class AudioRoutingServiceTests
{
    private readonly AudioRoutingService _service = new();

    [Fact]
    public async Task ConnectRouteAsync_EstablishesActiveRouteAndFiresEvent()
    {
        // Arrange
        var source = new AudioSource
        {
            Id = "phone-source",
            DisplayName = "Phone Media",
            Type = SourceType.RemoteMedia,
            Volume = 0.9f
        };

        var output = new AudioOutput
        {
            Id = "earbuds-output",
            DisplayName = "Mustang GoBoult Torq",
            MasterVolume = 0.75f
        };

        bool eventFired = false;
        _service.RouteUpdated += (sender, args) =>
        {
            if (args.Route.Source.Id == source.Id && args.Route.Output.Id == output.Id)
            {
                eventFired = true;
            }
        };

        // Act
        var route = await _service.ConnectRouteAsync(source, output);

        // Assert
        Assert.NotNull(route);
        Assert.True(route.IsActive);
        Assert.Equal(1.0f, route.RouteGain);
        Assert.Same(route, _service.ActiveRoute);
        Assert.True(eventFired);
    }

    [Fact]
    public async Task DisconnectRouteAsync_DeactivatesAndRemovesActiveRoute()
    {
        // Arrange
        var source = new AudioSource { Id = "s1", DisplayName = "Phone", Type = SourceType.RemoteMedia };
        var output = new AudioOutput { Id = "o1", DisplayName = "Earbuds" };
        var route = await _service.ConnectRouteAsync(source, output);

        string? removedId = null;
        _service.RouteRemoved += (sender, id) => removedId = id;

        // Act
        await _service.DisconnectRouteAsync(route.Id);

        // Assert
        Assert.Null(_service.ActiveRoute);
        Assert.False(route.IsActive);
        Assert.Equal(route.Id, removedId);
    }

    [Fact]
    public async Task SetRouteGain_ClampsValueBetweenZeroAndOne()
    {
        // Arrange
        var source = new AudioSource { Id = "s1", DisplayName = "Phone", Type = SourceType.RemoteMedia };
        var output = new AudioOutput { Id = "o1", DisplayName = "Earbuds" };
        var route = await _service.ConnectRouteAsync(source, output);

        // Act - test upper clamp
        _service.SetRouteGain(route.Id, 1.5f);
        Assert.Equal(1.0f, route.RouteGain);

        // Act - test lower clamp
        _service.SetRouteGain(route.Id, -0.5f);
        Assert.Equal(0.0f, route.RouteGain);

        // Act - test normal value
        _service.SetRouteGain(route.Id, 0.65f);
        Assert.Equal(0.65f, route.RouteGain);
    }

    [Fact]
    public async Task SetRouteMute_UpdatesSourceMuteAndNotifies()
    {
        // Arrange
        var source = new AudioSource { Id = "s1", DisplayName = "Phone", Type = SourceType.RemoteMedia, IsMuted = false };
        var output = new AudioOutput { Id = "o1", DisplayName = "Earbuds" };
        var route = await _service.ConnectRouteAsync(source, output);

        bool notifiedMute = false;
        _service.RouteUpdated += (sender, args) =>
        {
            if (args.Route.Source.IsMuted)
            {
                notifiedMute = true;
            }
        };

        // Act
        _service.SetRouteMute(route.Id, true);

        // Assert
        Assert.True(route.Source.IsMuted);
        Assert.True(notifiedMute);
    }

    [Fact]
    public async Task ConnectRouteAsync_ReplacesExistingActiveRouteGracefully()
    {
        // Arrange
        var source1 = new AudioSource { Id = "s1", DisplayName = "Phone 1", Type = SourceType.RemoteMedia };
        var source2 = new AudioSource { Id = "s2", DisplayName = "Phone 2", Type = SourceType.RemoteMedia };
        var output = new AudioOutput { Id = "o1", DisplayName = "Mustang GoBoult Torq" };

        var firstRoute = await _service.ConnectRouteAsync(source1, output);
        string? removedId = null;
        _service.RouteRemoved += (sender, id) => removedId = id;

        // Act
        var secondRoute = await _service.ConnectRouteAsync(source2, output);

        // Assert
        Assert.False(firstRoute.IsActive);
        Assert.Equal(firstRoute.Id, removedId);
        Assert.Same(secondRoute, _service.ActiveRoute);
        Assert.Equal("Phone 2", _service.ActiveRoute?.Source.DisplayName);
    }
}
