namespace AudioHub.Core.Models;

/// <summary>
/// Event arguments dispatched when media metadata or playback state changes.
/// </summary>
public sealed class MediaPlaybackStateChangedEventArgs : EventArgs
{
    public MediaMetadata Metadata { get; }
    public MediaPlaybackInfo PlaybackInfo { get; }
    public string? SourceOrSessionId { get; }

    public MediaPlaybackStateChangedEventArgs(
        MediaMetadata metadata,
        MediaPlaybackInfo playbackInfo,
        string? sourceOrSessionId = null)
    {
        Metadata = metadata ?? throw new ArgumentNullException(nameof(metadata));
        PlaybackInfo = playbackInfo ?? throw new ArgumentNullException(nameof(playbackInfo));
        SourceOrSessionId = sourceOrSessionId;
    }
}
