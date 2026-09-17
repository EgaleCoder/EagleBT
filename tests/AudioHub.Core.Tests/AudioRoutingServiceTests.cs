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
    public async Task ConnectRouteAsync_ReplacesExistingRouteForSameSourceGracefully()
    {
        // Arrange
        var source1 = new AudioSource { Id = "s1", DisplayName = "Phone 1 (Initial)", Type = SourceType.RemoteMedia };
        var source1Updated = new AudioSource { Id = "s1", DisplayName = "Phone 1 (Updated)", Type = SourceType.RemoteMedia };
        var output = new AudioOutput { Id = "o1", DisplayName = "Mustang GoBoult Torq" };

        var firstRoute = await _service.ConnectRouteAsync(source1, output);
        string? removedId = null;
        _service.RouteRemoved += (sender, id) => removedId = id;

        // Act
        var secondRoute = await _service.ConnectRouteAsync(source1Updated, output);

        // Assert
        Assert.False(firstRoute.IsActive);
        Assert.Equal(firstRoute.Id, removedId);
        Assert.True(secondRoute.IsActive);
        Assert.Equal("Phone 1 (Updated)", _service.GetRouteBySourceId("s1")?.Source.DisplayName);
    }

    [Fact]
    public async Task ConnectRouteAsync_MultipleDistinctSources_MaintainsAllActiveRoutesConcurrently()
    {
        // Arrange
        var phone = new AudioSource { Id = "phone-1", DisplayName = "Phone", Type = SourceType.RemoteMedia };
        var tablet = new AudioSource { Id = "tablet-1", DisplayName = "Tablet", Type = SourceType.RemoteMedia };
        var pc = new AudioSource { Id = "pc-1", DisplayName = "PC App", Type = SourceType.LocalApplication };
        var earbuds = new AudioOutput { Id = "earbuds-1", DisplayName = "Mustang GoBoult Torq" };

        // Act
        var route1 = await _service.ConnectRouteAsync(phone, earbuds);
        var route2 = await _service.ConnectRouteAsync(tablet, earbuds);
        var route3 = await _service.ConnectRouteAsync(pc, earbuds);

        // Assert: all 3 routes remain active concurrently
        Assert.Equal(3, _service.ActiveRoutes.Count);
        Assert.True(route1.IsActive);
        Assert.True(route2.IsActive);
        Assert.True(route3.IsActive);

        Assert.NotNull(_service.GetRoute(route1.Id));
        Assert.NotNull(_service.GetRoute(route2.Id));
        Assert.NotNull(_service.GetRoute(route3.Id));
    }

    [Fact]
    public async Task SetRouteGain_ModifiesSpecifiedRouteIndependently()
    {
        // Arrange
        var source1 = new AudioSource { Id = "s1", DisplayName = "Phone", Type = SourceType.RemoteMedia };
        var source2 = new AudioSource { Id = "s2", DisplayName = "Tablet", Type = SourceType.RemoteMedia };
        var output = new AudioOutput { Id = "o1", DisplayName = "Mustang GoBoult Torq" };

        var route1 = await _service.ConnectRouteAsync(source1, output);
        var route2 = await _service.ConnectRouteAsync(source2, output);

        // Act
        _service.SetRouteGain(route1.Id, 0.4f);
        _service.SetRouteGain(route2.Id, 0.9f);

        // Assert
        Assert.Equal(0.4f, route1.RouteGain);
        Assert.Equal(0.9f, route2.RouteGain);
    }

    [Fact]
    public async Task SetRouteMute_MutesSpecifiedRouteWithoutAffectingOtherRoutes()
    {
        // Arrange
        var source1 = new AudioSource { Id = "s1", DisplayName = "Phone", Type = SourceType.RemoteMedia, IsMuted = false };
        var source2 = new AudioSource { Id = "s2", DisplayName = "Tablet", Type = SourceType.RemoteMedia, IsMuted = false };
        var output = new AudioOutput { Id = "o1", DisplayName = "Mustang GoBoult Torq" };

        var route1 = await _service.ConnectRouteAsync(source1, output);
        var route2 = await _service.ConnectRouteAsync(source2, output);

        // Act
        _service.SetRouteMute(route1.Id, true);

        // Assert
        Assert.True(route1.Source.IsMuted);
        Assert.False(route2.Source.IsMuted);
    }

    [Fact]
    public async Task DisconnectRouteAsync_RemovesOnlyTargetRoute_LeavesOtherRoutesActive()
    {
        // Arrange
        var source1 = new AudioSource { Id = "s1", DisplayName = "Phone", Type = SourceType.RemoteMedia };
        var source2 = new AudioSource { Id = "s2", DisplayName = "Tablet", Type = SourceType.RemoteMedia };
        var output = new AudioOutput { Id = "o1", DisplayName = "Mustang GoBoult Torq" };

        var route1 = await _service.ConnectRouteAsync(source1, output);
        var route2 = await _service.ConnectRouteAsync(source2, output);

        // Act
        await _service.DisconnectRouteAsync(route1.Id);

        // Assert
        Assert.False(route1.IsActive);
        Assert.True(route2.IsActive);
        Assert.Single(_service.ActiveRoutes);
        Assert.Equal(route2.Id, _service.ActiveRoutes.First().Id);
    }

    [Fact]
    public async Task DisconnectAllRoutesAsync_DeactivatesAllActiveRoutesAndFiresEvents()
    {
        // Arrange
        var source1 = new AudioSource { Id = "s1", DisplayName = "Phone", Type = SourceType.RemoteMedia };
        var source2 = new AudioSource { Id = "s2", DisplayName = "Tablet", Type = SourceType.RemoteMedia };
        var output = new AudioOutput { Id = "o1", DisplayName = "Mustang GoBoult Torq" };

        var route1 = await _service.ConnectRouteAsync(source1, output);
        var route2 = await _service.ConnectRouteAsync(source2, output);

        var removedIds = new List<string>();
        _service.RouteRemoved += (sender, id) => removedIds.Add(id);

        // Act
        await _service.DisconnectAllRoutesAsync();

        // Assert
        Assert.Empty(_service.ActiveRoutes);
        Assert.False(route1.IsActive);
        Assert.False(route2.IsActive);
        Assert.Contains(route1.Id, removedIds);
        Assert.Contains(route2.Id, removedIds);
    }

    [Fact]
    public async Task GetRouteBySourceId_ReturnsCorrectRouteForSource()
    {
        // Arrange
        var source = new AudioSource { Id = "target-source", DisplayName = "Phone", Type = SourceType.RemoteMedia };
        var output = new AudioOutput { Id = "o1", DisplayName = "Mustang GoBoult Torq" };

        var route = await _service.ConnectRouteAsync(source, output);

        // Act
        var foundRoute = _service.GetRouteBySourceId("target-source");
        var notFound = _service.GetRouteBySourceId("non-existent");

        // Assert
        Assert.NotNull(foundRoute);
        Assert.Same(route, foundRoute);
        Assert.Null(notFound);
    }
}
