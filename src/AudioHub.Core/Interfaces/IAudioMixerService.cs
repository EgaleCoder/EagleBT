using AudioHub.Core.Models;

namespace AudioHub.Core.Interfaces;

/// <summary>
/// Defines the contract for real-time software audio mixing of multiple concurrent audio streams.
/// </summary>
public interface IAudioMixerService : IDisposable
{
    /// <summary>
    /// Gets the master output strip state and dynamics limiter controls.
    /// </summary>
    AudioMixerMaster Master { get; }

    /// <summary>
    /// Gets all active audio channel strips registered in the mixer.
    /// </summary>
    IReadOnlyCollection<AudioMixerChannel> Channels { get; }

    /// <summary>
    /// Retrieves a channel by its identifier.
    /// </summary>
    AudioMixerChannel? GetChannel(string channelId);

    /// <summary>
    /// Adds a new audio channel strip or updates an existing one.
    /// </summary>
    AudioMixerChannel AddOrUpdateChannel(string channelId, string name, float initialGain = 1.0f, float initialPan = 0.0f);

    /// <summary>
    /// Removes an audio channel strip by its identifier.
    /// </summary>
    bool RemoveChannel(string channelId);

    /// <summary>
    /// Adjusts the linear gain of a specific channel [0.0, 1.0].
    /// </summary>
    void SetChannelGain(string channelId, float gain);

    /// <summary>
    /// Adjusts the stereo pan/balance of a specific channel [-1.0 (L) to +1.0 (R)].
    /// </summary>
    void SetChannelPan(string channelId, float pan);

    /// <summary>
    /// Sets the mute status of a specific channel.
    /// </summary>
    void SetChannelMute(string channelId, bool isMuted);

    /// <summary>
    /// Sets the solo status of a specific channel. When any channel is soloed, non-soloed channels are silenced.
    /// </summary>
    void SetChannelSolo(string channelId, bool isSolo);

    /// <summary>
    /// Sets the master output volume gain [0.0, 1.0].
    /// </summary>
    void SetMasterGain(float gain);

    /// <summary>
    /// Sets the master output mute state.
    /// </summary>
    void SetMasterMute(bool isMuted);

    /// <summary>
    /// Enables or disables the soft-knee saturation limiter on the master output.
    /// </summary>
    void SetLimiterEnabled(bool enabled);

    /// <summary>
    /// Resets the mixer, clearing all channels and returning master controls to default.
    /// </summary>
    void Reset();

    /// <summary>
    /// Processes a frame of multi-channel audio samples, performing summing, gain/pan scaling,
    /// solo/mute routing, soft-knee saturation limiting, and level tracking.
    /// </summary>
    /// <param name="channelSamples">Dictionary mapping channelId to interleaved stereo or mono float samples [-1.0, 1.0].</param>
    /// <param name="outputStereoBuffer">Destination interleaved stereo float buffer.</param>
    void ProcessAudioFrame(IReadOnlyDictionary<string, float[]> channelSamples, float[] outputStereoBuffer);

    /// <summary>
    /// Dispatched periodically with live telemetry of channel and master peak/RMS levels.
    /// </summary>
    event EventHandler<MixerLevelsEventArgs>? LevelsUpdated;

    /// <summary>
    /// Dispatched when a specific channel or master signal hits full scale saturation (0 dBFS).
    /// </summary>
    event EventHandler<string>? ChannelClipping;
}
