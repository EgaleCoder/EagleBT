using System.Collections.Concurrent;
using AudioHub.Core.Interfaces;
using AudioHub.Core.Models;

namespace AudioHub.Core.Services;

/// <summary>
/// Domain implementation of audio route coordination, managing concurrent multi-route sessions,
/// per-route gain scaling, and mute states.
/// </summary>
public sealed class AudioRoutingService : IAudioRoutingService
{
    private readonly ConcurrentDictionary<string, AudioRoute> _routes = new(StringComparer.OrdinalIgnoreCase);

    public event EventHandler<AudioRouteEventArgs>? RouteUpdated;
    public event EventHandler<string>? RouteRemoved;

    public AudioRoute? ActiveRoute => _routes.Values.LastOrDefault(r => r.IsActive);

    public IReadOnlyCollection<AudioRoute> ActiveRoutes => _routes.Values.Where(r => r.IsActive).ToList().AsReadOnly();

    public AudioRoute? GetRoute(string routeId)
    {
        if (string.IsNullOrWhiteSpace(routeId)) return null;
        return _routes.TryGetValue(routeId, out var route) && route.IsActive ? route : null;
    }

    public AudioRoute? GetRouteBySourceId(string sourceId)
    {
        if (string.IsNullOrWhiteSpace(sourceId)) return null;
        return _routes.Values.FirstOrDefault(r => r.IsActive && string.Equals(r.Source.Id, sourceId, StringComparison.OrdinalIgnoreCase));
    }

    public Task<AudioRoute> ConnectRouteAsync(
        AudioSource source,
        AudioOutput output,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(output);
        cancellationToken.ThrowIfCancellationRequested();

        // If a route already exists for this exact source, remove and replace it
        var existingRoute = GetRouteBySourceId(source.Id);
        if (existingRoute != null)
        {
            if (_routes.TryRemove(existingRoute.Id, out var removed))
            {
                removed.IsActive = false;
                RouteRemoved?.Invoke(this, removed.Id);
            }
        }

        var newRoute = new AudioRoute
        {
            Id = Guid.NewGuid().ToString("N"),
            Source = source,
            Output = output,
            IsActive = true,
            RouteGain = 1.0f
        };

        _routes[newRoute.Id] = newRoute;

        RouteUpdated?.Invoke(this, new AudioRouteEventArgs
        {
            Route = newRoute,
            Reason = "Route established"
        });

        return Task.FromResult(newRoute);
    }

    public Task DisconnectRouteAsync(string routeId, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(routeId);
        cancellationToken.ThrowIfCancellationRequested();

        if (_routes.TryRemove(routeId, out var route))
        {
            route.IsActive = false;
            RouteRemoved?.Invoke(this, route.Id);
        }

        return Task.CompletedTask;
    }

    public Task DisconnectAllRoutesAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var activeKeys = _routes.Keys.ToList();
        foreach (var key in activeKeys)
        {
            if (_routes.TryRemove(key, out var route))
            {
                route.IsActive = false;
                RouteRemoved?.Invoke(this, route.Id);
            }
        }

        return Task.CompletedTask;
    }

    public void SetRouteGain(string routeId, float gain)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(routeId);

        if (_routes.TryGetValue(routeId, out var route) && route.IsActive)
        {
            route.RouteGain = Math.Clamp(gain, 0.0f, 1.0f);
            RouteUpdated?.Invoke(this, new AudioRouteEventArgs
            {
                Route = route,
                Reason = "Gain updated"
            });
        }
    }

    public void SetRouteMute(string routeId, bool isMuted)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(routeId);

        if (_routes.TryGetValue(routeId, out var route) && route.IsActive)
        {
            route.Source.IsMuted = isMuted;
            RouteUpdated?.Invoke(this, new AudioRouteEventArgs
            {
                Route = route,
                Reason = isMuted ? "Route muted" : "Route unmuted"
            });
        }
    }
}
