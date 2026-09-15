using AudioHub.Core.Models;

namespace AudioHub.Core.Interfaces;

/// <summary>
/// Event arguments for audio route state changes.
/// </summary>
public sealed class AudioRouteEventArgs : EventArgs
{
    public required AudioRoute Route { get; init; }
    public string? Reason { get; init; }
}

/// <summary>
/// Coordinates audio routes between sources (e.g., remote Bluetooth devices, PC apps)
/// and target audio outputs (e.g., Mustang GoBoult Torq, PC speakers).
/// </summary>
public interface IAudioRoutingService
{
    /// <summary>
    /// Occurs when an audio route is created or updated.
    /// </summary>
    event EventHandler<AudioRouteEventArgs>? RouteUpdated;

    /// <summary>
    /// Occurs when an active audio route is closed.
    /// </summary>
    event EventHandler<string>? RouteRemoved;

    /// <summary>
    /// Gets the currently active primary audio route, if any.
    /// </summary>
    AudioRoute? ActiveRoute { get; }

    /// <summary>
    /// Establishes and activates a route from the specified source to the destination output.
    /// </summary>
    Task<AudioRoute> ConnectRouteAsync(
        AudioSource source,
        AudioOutput output,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Deactivates and closes the currently active route.
    /// </summary>
    Task DisconnectRouteAsync(string routeId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates the volume gain of the active route (0.0f to 1.0f).
    /// </summary>
    void SetRouteGain(string routeId, float gain);

    /// <summary>
    /// Sets the mute state of the active route.
    /// </summary>
    void SetRouteMute(string routeId, bool isMuted);
}
