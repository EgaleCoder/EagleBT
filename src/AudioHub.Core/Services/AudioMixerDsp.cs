namespace AudioHub.Core.Services;

/// <summary>
/// High-performance digital signal processing (DSP) utilities for multi-channel audio mixing,
/// equal-power stereo panning, sample summing, soft-knee saturation limiting, and level metering.
/// </summary>
public static class AudioMixerDsp
{
    private const float PiOverFour = MathF.PI / 4.0f;

    /// <summary>
    /// Calculates Left and Right gain factors using the -3dB Constant Power Pan Law.
    /// </summary>
    /// <param name="pan">Pan position in range [-1.0 (L) to +1.0 (R)].</param>
    /// <returns>Stereo gain multiplier tuple (LeftFactor, RightFactor).</returns>
    public static (float Left, float Right) CalculateEqualPowerPan(float pan)
    {
        float clampedPan = Math.Clamp(pan, -1.0f, 1.0f);
        float angle = (clampedPan + 1.0f) * PiOverFour; // 0 (Hard L) to PI/2 (Hard R)
        return (MathF.Cos(angle), MathF.Sin(angle));
    }

    /// <summary>
    /// Applies a smooth soft-knee saturation curve via hyperbolic tangent (tanh) to prevent digital clipping
    /// when multiple audio streams sum above 0 dBFS (1.0f).
    /// </summary>
    /// <param name="sample">Input audio sample.</param>
    /// <returns>Soft-limited sample bounded in [-1.0, 1.0].</returns>
    public static float SoftClip(float sample)
    {
        // For signals within normal linear range, tanh is nearly transparent
        if (sample is > -0.7f and < 0.7f)
        {
            return sample;
        }

        // Apply smooth hyperbolic tangent saturation
        return MathF.Tanh(sample);
    }

    /// <summary>
    /// Measures peak amplitude and RMS (root-mean-square) energy from a buffer of interleaved stereo samples.
    /// </summary>
    public static (float PeakLeft, float PeakRight, float RmsLeft, float RmsRight) CalculateStereoLevels(
        ReadOnlySpan<float> interleavedStereoSamples)
    {
        if (interleavedStereoSamples.IsEmpty)
        {
            return (0f, 0f, 0f, 0f);
        }

        float peakL = 0f;
        float peakR = 0f;
        float sumSquareL = 0f;
        float sumSquareR = 0f;
        int frameCount = interleavedStereoSamples.Length / 2;

        for (int i = 0; i < interleavedStereoSamples.Length - 1; i += 2)
        {
            float l = MathF.Abs(interleavedStereoSamples[i]);
            float r = MathF.Abs(interleavedStereoSamples[i + 1]);

            if (l > peakL) peakL = l;
            if (r > peakR) peakR = r;

            sumSquareL += l * l;
            sumSquareR += r * r;
        }

        float rmsL = frameCount > 0 ? MathF.Sqrt(sumSquareL / frameCount) : 0f;
        float rmsR = frameCount > 0 ? MathF.Sqrt(sumSquareR / frameCount) : 0f;

        return (
            Math.Clamp(peakL, 0f, 1f),
            Math.Clamp(peakR, 0f, 1f),
            Math.Clamp(rmsL, 0f, 1f),
            Math.Clamp(rmsR, 0f, 1f));
    }
}
