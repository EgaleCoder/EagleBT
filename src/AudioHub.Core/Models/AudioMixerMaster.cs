namespace AudioHub.Core.Models;

/// <summary>
/// Represents the master output state and dynamics limiter controls of the audio mixer.
/// </summary>
public sealed class AudioMixerMaster
{
    public float MasterGain { get; set; } = 1.0f;
    public bool IsMuted { get; set; }
    public bool LimiterEnabled { get; set; } = true;

    /// <summary>
    /// Master stereo output Left peak amplitude [0.0, 1.0].
    /// </summary>
    public float PeakLeft { get; set; }

    /// <summary>
    /// Master stereo output Right peak amplitude [0.0, 1.0].
    /// </summary>
    public float PeakRight { get; set; }

    /// <summary>
    /// Indicates whether the master output signal is actively hitting or exceeding full scale (0 dBFS).
    /// </summary>
    public bool IsClipping { get; set; }
}
