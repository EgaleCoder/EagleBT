namespace AudioHub.Core.Models;

/// <summary>
/// Represents the current playback status of a media stream or device.
/// </summary>
public enum MediaPlaybackStatus
{
    Unknown = 0,
    Closed = 1,
    Opened = 2,
    Changing = 3,
    Stopped = 4,
    Playing = 5,
    Paused = 6
}
