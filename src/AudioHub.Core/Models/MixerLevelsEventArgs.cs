namespace AudioHub.Core.Models;

/// <summary>
/// Telemetry event arguments containing real-time stereo peak and RMS levels for mixer channels and master output.
/// </summary>
public sealed class MixerLevelsEventArgs : EventArgs
{
    public required IReadOnlyDictionary<string, (float PeakLeft, float PeakRight)> ChannelPeaks { get; init; }
    public required (float PeakLeft, float PeakRight) MasterPeak { get; init; }
    public bool IsMasterClipping { get; init; }
}
