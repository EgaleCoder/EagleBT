using System.Collections.Concurrent;
using AudioHub.Core.Interfaces;
using AudioHub.Core.Models;
using AudioHub.Core.Services;
using Microsoft.Extensions.Logging;

namespace AudioHub.Windows.Audio;

/// <summary>
/// Thread-safe real-time software audio mixer implementing multi-stream summing,
/// equal-power panning, solo/mute matrix routing, soft-knee saturation limiting,
/// and live VU metering telemetry.
/// </summary>
public sealed class SoftwareAudioMixerService : IAudioMixerService
{
    private readonly ILogger<SoftwareAudioMixerService> _logger;
    private readonly ConcurrentDictionary<string, AudioMixerChannel> _channels = new(StringComparer.OrdinalIgnoreCase);
    private readonly Timer _telemetryTimer;
    private bool _isDisposed;

    public AudioMixerMaster Master { get; } = new();

    public IReadOnlyCollection<AudioMixerChannel> Channels => _channels.Values.ToList().AsReadOnly();

    public event EventHandler<MixerLevelsEventArgs>? LevelsUpdated;
    public event EventHandler<string>? ChannelClipping;

    public SoftwareAudioMixerService(ILogger<SoftwareAudioMixerService> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        // 30 Hz VU metering telemetry timer (~33ms intervals)
        _telemetryTimer = new Timer(OnTelemetryTick, null, TimeSpan.FromMilliseconds(33), TimeSpan.FromMilliseconds(33));
    }

    public AudioMixerChannel? GetChannel(string channelId)
    {
        if (string.IsNullOrWhiteSpace(channelId)) return null;
        return _channels.TryGetValue(channelId, out var ch) ? ch : null;
    }

    public AudioMixerChannel AddOrUpdateChannel(string channelId, string name, float initialGain = 1.0f, float initialPan = 0.0f)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(channelId);
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        return _channels.AddOrUpdate(
            channelId,
            id => new AudioMixerChannel
            {
                ChannelId = id,
                Name = name,
                Gain = Math.Clamp(initialGain, 0f, 1f),
                Pan = Math.Clamp(initialPan, -1f, 1f),
                IsMuted = false,
                IsSolo = false
            },
            (_, existing) =>
            {
                existing.Name = name;
                return existing;
            });
    }

    public bool RemoveChannel(string channelId)
    {
        if (string.IsNullOrWhiteSpace(channelId)) return false;
        return _channels.TryRemove(channelId, out _);
    }

    public void SetChannelGain(string channelId, float gain)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(channelId);
        if (_channels.TryGetValue(channelId, out var channel))
        {
            channel.Gain = Math.Clamp(gain, 0f, 1f);
        }
    }

    public void SetChannelPan(string channelId, float pan)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(channelId);
        if (_channels.TryGetValue(channelId, out var channel))
        {
            channel.Pan = Math.Clamp(pan, -1f, 1f);
        }
    }

    public void SetChannelMute(string channelId, bool isMuted)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(channelId);
        if (_channels.TryGetValue(channelId, out var channel))
        {
            channel.IsMuted = isMuted;
        }
    }

    public void SetChannelSolo(string channelId, bool isSolo)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(channelId);
        if (_channels.TryGetValue(channelId, out var channel))
        {
            channel.IsSolo = isSolo;
        }
    }

    public void SetMasterGain(float gain)
    {
        Master.MasterGain = Math.Clamp(gain, 0f, 1f);
    }

    public void SetMasterMute(bool isMuted)
    {
        Master.IsMuted = isMuted;
    }

    public void SetLimiterEnabled(bool enabled)
    {
        Master.LimiterEnabled = enabled;
    }

    public void Reset()
    {
        _channels.Clear();
        Master.MasterGain = 1.0f;
        Master.IsMuted = false;
        Master.LimiterEnabled = true;
        Master.PeakLeft = 0f;
        Master.PeakRight = 0f;
        Master.IsClipping = false;
    }

    public void ProcessAudioFrame(IReadOnlyDictionary<string, float[]> channelSamples, float[] outputStereoBuffer)
    {
        ArgumentNullException.ThrowIfNull(channelSamples);
        ArgumentNullException.ThrowIfNull(outputStereoBuffer);

        int sampleCount = outputStereoBuffer.Length;
        if (sampleCount == 0) return;

        // Clear output buffer initially
        Array.Clear(outputStereoBuffer, 0, sampleCount);

        bool anySolo = _channels.Values.Any(c => c.IsSolo);

        // Sum active channels into output buffer
        foreach (var channel in _channels.Values)
        {
            bool isAudible = anySolo ? (channel.IsSolo && !channel.IsMuted) : !channel.IsMuted;

            if (!channelSamples.TryGetValue(channel.ChannelId, out var inSamples) || inSamples.Length < sampleCount)
            {
                // Smoothly decay channel levels if no new samples provided
                channel.PeakLeft *= 0.85f;
                channel.PeakRight *= 0.85f;
                continue;
            }

            var (panLeft, panRight) = AudioMixerDsp.CalculateEqualPowerPan(channel.Pan);
            float effectiveGainL = isAudible ? channel.Gain * panLeft : 0f;
            float effectiveGainR = isAudible ? channel.Gain * panRight : 0f;

            float chPeakL = 0f;
            float chPeakR = 0f;

            for (int i = 0; i < sampleCount - 1; i += 2)
            {
                float sL = inSamples[i];
                float sR = inSamples[i + 1];

                if (MathF.Abs(sL) > chPeakL) chPeakL = MathF.Abs(sL);
                if (MathF.Abs(sR) > chPeakR) chPeakR = MathF.Abs(sR);

                outputStereoBuffer[i] += sL * effectiveGainL;
                outputStereoBuffer[i + 1] += sR * effectiveGainR;
            }

            channel.PeakLeft = Math.Clamp(chPeakL * channel.Gain, 0f, 1f);
            channel.PeakRight = Math.Clamp(chPeakR * channel.Gain, 0f, 1f);

            if (chPeakL >= 0.999f || chPeakR >= 0.999f)
            {
                ChannelClipping?.Invoke(this, channel.ChannelId);
            }
        }

        // Apply Master gain, mute, and soft-knee saturation limiter
        float masterL = 0f;
        float masterR = 0f;
        bool masterClipping = false;

        for (int i = 0; i < sampleCount - 1; i += 2)
        {
            float outL = Master.IsMuted ? 0f : outputStereoBuffer[i] * Master.MasterGain;
            float outR = Master.IsMuted ? 0f : outputStereoBuffer[i + 1] * Master.MasterGain;

            if (MathF.Abs(outL) >= 0.999f || MathF.Abs(outR) >= 0.999f)
            {
                masterClipping = true;
            }

            if (Master.LimiterEnabled)
            {
                outL = AudioMixerDsp.SoftClip(outL);
                outR = AudioMixerDsp.SoftClip(outR);
            }

            outputStereoBuffer[i] = Math.Clamp(outL, -1.0f, 1.0f);
            outputStereoBuffer[i + 1] = Math.Clamp(outR, -1.0f, 1.0f);

            if (MathF.Abs(outputStereoBuffer[i]) > masterL) masterL = MathF.Abs(outputStereoBuffer[i]);
            if (MathF.Abs(outputStereoBuffer[i + 1]) > masterR) masterR = MathF.Abs(outputStereoBuffer[i + 1]);
        }

        Master.PeakLeft = masterL;
        Master.PeakRight = masterR;
        Master.IsClipping = masterClipping;
    }

    private void OnTelemetryTick(object? state)
    {
        if (_isDisposed) return;

        // Collect current channel peaks
        var channelPeaks = new Dictionary<string, (float PeakLeft, float PeakRight)>(StringComparer.OrdinalIgnoreCase);
        foreach (var ch in _channels.Values)
        {
            // Natural release decay for analog VU meter behavior
            ch.PeakLeft *= 0.88f;
            ch.PeakRight *= 0.88f;
            if (ch.PeakLeft < 0.001f) ch.PeakLeft = 0f;
            if (ch.PeakRight < 0.001f) ch.PeakRight = 0f;

            channelPeaks[ch.ChannelId] = (ch.PeakLeft, ch.PeakRight);
        }

        Master.PeakLeft *= 0.88f;
        Master.PeakRight *= 0.88f;
        if (Master.PeakLeft < 0.001f) Master.PeakLeft = 0f;
        if (Master.PeakRight < 0.001f) Master.PeakRight = 0f;

        LevelsUpdated?.Invoke(this, new MixerLevelsEventArgs
        {
            ChannelPeaks = channelPeaks,
            MasterPeak = (Master.PeakLeft, Master.PeakRight),
            IsMasterClipping = Master.IsClipping
        });
    }

    public void Dispose()
    {
        if (_isDisposed) return;
        _isDisposed = true;

        _telemetryTimer.Dispose();
        _channels.Clear();
    }
}
