namespace AudioHub.Core.Models;

/// <summary>
/// Represents an audio destination endpoint (such as Mustang GoBoult Torq or PC Speakers).
/// </summary>
public sealed class AudioOutput
{
    public required string Id { get; init; }
    public required string DisplayName { get; init; }
    public float MasterVolume { get; set; } = 1.0f;
    public bool IsMuted { get; set; }

    public override string ToString() => $"{DisplayName} - Master: {MasterVolume:P0}";
}
