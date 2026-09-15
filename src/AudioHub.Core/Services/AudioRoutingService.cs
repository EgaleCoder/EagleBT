using AudioHub.Core.Interfaces;
using AudioHub.Core.Models;

namespace AudioHub.Core.Services;

/// <summary>
/// Domain implementation of audio route coordination, managing single active route sessions,
/// gain scaling, and mute states.
/// </summary>
public sealed class AudioRoutingService : IAudioRoutingService
{
    private readonly object _lock = new();
    private AudioRoute? _activeRoute;

    public event EventHandler<AudioRouteEventArgs>? RouteUpdated;
    public event EventHandler<string>? RouteRemoved;

    public AudioRoute? ActiveRoute
    {
        get
        {
            lock (_lock)
            {
                return _activeRoute;
            }
        }
    }

    public Task<AudioRoute> ConnectRouteAsync(
        AudioSource source,
        AudioOutput output,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(output);
        cancellationToken.ThrowIfCancellationRequested();

        AudioRoute newRoute;
        lock (_lock)
        {
            // Close existing route if active
            if (_activeRoute != null)
            {
                _activeRoute.IsActive = false;
                string oldId = _activeRoute.Id;
                _activeRoute = null;
                RouteRemoved?.Invoke(this, oldId);
            }

            newRoute = new AudioRoute
            {
                Id = Guid.NewGuid().ToString("N"),
                Source = source,
                Output = output,
                IsActive = true,
                RouteGain = 1.0f
            };

            _activeRoute = newRoute;
        }

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

        AudioRoute? routeToClose = null;
        lock (_lock)
        {
            if (_activeRoute != null && string.Equals(_activeRoute.Id, routeId, StringComparison.OrdinalIgnoreCase))
            {
                routeToClose = _activeRoute;
                routeToClose.IsActive = false;
                _activeRoute = null;
            }
        }

        if (routeToClose != null)
        {
            RouteRemoved?.Invoke(this, routeToClose.Id);
        }

        return Task.CompletedTask;
    }

    public void SetRouteGain(string routeId, float gain)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(routeId);

        AudioRoute? route;
        lock (_lock)
        {
            if (_activeRoute == null || !string.Equals(_activeRoute.Id, routeId, StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            _activeRoute.RouteGain = Math.Clamp(gain, 0.0f, 1.0f);
            route = _activeRoute;
        }

        RouteUpdated?.Invoke(this, new AudioRouteEventArgs
        {
            Route = route,
            Reason = "Gain updated"
        });
    }

    public void SetRouteMute(string routeId, bool isMuted)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(routeId);

        AudioRoute? route;
        lock (_lock)
        {
            if (_activeRoute == null || !string.Equals(_activeRoute.Id, routeId, StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            _activeRoute.Source.IsMuted = isMuted;
            route = _activeRoute;
        }

        RouteUpdated?.Invoke(this, new AudioRouteEventArgs
        {
            Route = route,
            Reason = isMuted ? "Route muted" : "Route unmuted"
        });
    }
}
