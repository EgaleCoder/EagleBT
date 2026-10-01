namespace AudioHub.Core.Models;

/// <summary>
/// Encapsulates playback transport state and capabilities for a media session.
/// </summary>
public sealed record MediaPlaybackInfo
{
    public MediaPlaybackStatus Status { get; init; } = MediaPlaybackStatus.Closed;
    public bool CanPlay { get; init; } = true;
    public bool CanPause { get; init; } = true;
    public bool CanTogglePlayPause { get; init; } = true;
    public bool CanSkipNext { get; init; } = true;
    public bool CanSkipPrevious { get; init; } = true;
    public bool CanStop { get; init; } = true;

    public bool IsPlaying => Status == MediaPlaybackStatus.Playing;
}
