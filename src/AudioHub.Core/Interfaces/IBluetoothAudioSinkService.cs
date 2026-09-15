namespace AudioHub.Core.Interfaces;

/// <summary>
/// Represents the connection and streaming state of a remote audio playback connection.
/// </summary>
public enum AudioPlaybackStreamState
{
    Closed = 0,
    Opening = 1,
    Streaming = 2,
    Failed = 3
}

/// <summary>
/// Event arguments providing details when a stream state changes.
/// </summary>
public sealed class AudioPlaybackStreamEventArgs : EventArgs
{
    public required string DeviceId { get; init; }
    public required AudioPlaybackStreamState State { get; init; }
    public string? Message { get; init; }
}

/// <summary>
/// Abstraction for managing Bluetooth A2DP audio sink streaming from remote sources to the local PC.
/// </summary>
public interface IBluetoothAudioSinkService : IDisposable
{
    /// <summary>
    /// Occurs when an audio playback stream state changes.
    /// </summary>
    event EventHandler<AudioPlaybackStreamEventArgs>? StreamStateChanged;

    /// <summary>
    /// Gets active streams and their current states keyed by device identifier.
    /// </summary>
    IReadOnlyDictionary<string, AudioPlaybackStreamState> ActiveStreams { get; }

    /// <summary>
    /// Initiates an A2DP sink audio stream for the specified remote Bluetooth device.
    /// </summary>
    Task<bool> StartStreamAsync(string deviceId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Terminates the active A2DP sink audio stream for the specified device.
    /// </summary>
    Task StopStreamAsync(string deviceId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns the current stream state for the specified device.
    /// </summary>
    AudioPlaybackStreamState GetStreamState(string deviceId);
}
