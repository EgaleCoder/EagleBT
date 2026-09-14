namespace AudioHub.Core.Models;

/// <summary>
/// Categorizes the origin of an audio stream.
/// </summary>
public enum SourceType
{
    RemoteMedia = 0,
    RemoteCall = 1,
    LocalApplication = 2,
    SystemAudio = 3
}

/// <summary>
/// Represents an audio origin stream that can be routed to an output.
/// </summary>
public sealed class AudioSource
{
    public required string Id { get; init; }
    public required string DisplayName { get; init; }
    public SourceType Type { get; init; }
    public float Volume { get; set; } = 1.0f;
    public bool IsMuted { get; set; }

    public override string ToString() => $"{DisplayName} ({Type}) - Vol: {Volume:P0}";
}
