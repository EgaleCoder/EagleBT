namespace AudioHub.Core.Models;

/// <summary>
/// Represents the real-time mixing state of an individual audio stream channel strip.
/// </summary>
public sealed class AudioMixerChannel
{
    public required string ChannelId { get; init; }
    public required string Name { get; set; }
    public float Gain { get; set; } = 1.0f;
    public float Pan { get; set; } = 0.0f; // -1.0 (Left) to +1.0 (Right), 0.0 (Center)
    public bool IsMuted { get; set; }
    public bool IsSolo { get; set; }

    /// <summary>
    /// Current peak audio amplitude for the Left channel [0.0, 1.0].
    /// </summary>
    public float PeakLeft { get; set; }

    /// <summary>
    /// Current peak audio amplitude for the Right channel [0.0, 1.0].
    /// </summary>
    public float PeakRight { get; set; }

    /// <summary>
    /// Current RMS (root-mean-square) power level for the Left channel [0.0, 1.0].
    /// </summary>
    public float RmsLeft { get; set; }

    /// <summary>
    /// Current RMS power level for the Right channel [0.0, 1.0].
    /// </summary>
    public float RmsRight { get; set; }
}
