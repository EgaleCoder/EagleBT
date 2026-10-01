using AudioHub.Core.Models;

namespace AudioHub.Core.Interfaces;

/// <summary>
/// Service abstraction for controlling media playback (AVRCP / System Media Controls)
/// and tracking track metadata across connected Bluetooth audio sources and local PC media.
/// </summary>
public interface IMediaControlService : IDisposable
{
    /// <summary>
    /// Occurs when active media playback status or metadata changes.
    /// </summary>
    event EventHandler<MediaPlaybackStateChangedEventArgs>? PlaybackStateChanged;

    /// <summary>
    /// Gets the current media metadata for the active or preferred session.
    /// </summary>
    Task<MediaMetadata?> GetCurrentMediaMetadataAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the current playback transport information and capabilities.
    /// </summary>
    Task<MediaPlaybackInfo?> GetCurrentPlaybackInfoAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Sends a Play command to the active media session or specified source.
    /// </summary>
    Task<bool> PlayAsync(string? sourceOrSessionId = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Sends a Pause command to the active media session or specified source.
    /// </summary>
    Task<bool> PauseAsync(string? sourceOrSessionId = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Toggles Play/Pause on the active media session or specified source.
    /// </summary>
    Task<bool> TogglePlayPauseAsync(string? sourceOrSessionId = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Skips to the next media track.
    /// </summary>
    Task<bool> SkipNextAsync(string? sourceOrSessionId = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Skips to the previous media track.
    /// </summary>
    Task<bool> SkipPreviousAsync(string? sourceOrSessionId = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Stops media playback.
    /// </summary>
    Task<bool> StopAsync(string? sourceOrSessionId = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Sends a hardware media or volume keystroke (AVRCP transport fallback).
    /// </summary>
    Task<bool> SendHardwareMediaKeyAsync(MediaHardwareKey key, CancellationToken cancellationToken = default);
}
