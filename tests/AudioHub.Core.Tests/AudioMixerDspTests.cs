using AudioHub.Core.Services;
using Xunit;

namespace AudioHub.Core.Tests;

public sealed class AudioMixerDspTests
{
    [Fact]
    public void CalculateEqualPowerPan_Center_YieldsEqualGainAroundMinusThreeDb()
    {
        // Act
        var (left, right) = AudioMixerDsp.CalculateEqualPowerPan(0.0f);

        // Assert: center pan (angle = pi/4) -> cos(pi/4) == sin(pi/4) ~= 0.7071 (-3 dB)
        Assert.InRange(left, 0.70f, 0.71f);
        Assert.InRange(right, 0.70f, 0.71f);
        Assert.Equal(left, right, 4);

        // Total power sum: L^2 + R^2 should equal 1.0
        float power = (left * left) + (right * right);
        Assert.InRange(power, 0.999f, 1.001f);
    }

    [Fact]
    public void CalculateEqualPowerPan_HardLeft_YieldsUnityLeftAndZeroRight()
    {
        // Act
        var (left, right) = AudioMixerDsp.CalculateEqualPowerPan(-1.0f);

        // Assert: hard left -> left = 1.0, right = 0.0
        Assert.Equal(1.0f, left, 4);
        Assert.Equal(0.0f, right, 4);
    }

    [Fact]
    public void CalculateEqualPowerPan_HardRight_YieldsZeroLeftAndUnityRight()
    {
        // Act
        var (left, right) = AudioMixerDsp.CalculateEqualPowerPan(1.0f);

        // Assert: hard right -> left = 0.0, right = 1.0
        Assert.Equal(0.0f, left, 4);
        Assert.Equal(1.0f, right, 4);
    }

    [Fact]
    public void SoftClip_LinearRange_PreservesSampleUntouched()
    {
        // Act & Assert
        Assert.Equal(0.0f, AudioMixerDsp.SoftClip(0.0f));
        Assert.Equal(0.5f, AudioMixerDsp.SoftClip(0.5f));
        Assert.Equal(-0.5f, AudioMixerDsp.SoftClip(-0.5f));
    }

    [Fact]
    public void SoftClip_OverdrivenSignals_CompressesSmoothlyWithinUnitBounds()
    {
        // Act
        float clippedHigh = AudioMixerDsp.SoftClip(2.5f);
        float clippedVeryHigh = AudioMixerDsp.SoftClip(10.0f);
        float clippedNegative = AudioMixerDsp.SoftClip(-3.0f);

        // Assert: tanh(x) is strictly within (-1.0, 1.0)
        Assert.InRange(clippedHigh, 0.7f, 1.0f);
        Assert.InRange(clippedVeryHigh, 0.99f, 1.0f);
        Assert.InRange(clippedNegative, -1.0f, -0.7f);
    }

    [Fact]
    public void CalculateStereoLevels_EmptyBuffer_ReturnsZeroes()
    {
        // Act
        var (peakL, peakR, rmsL, rmsR) = AudioMixerDsp.CalculateStereoLevels(ReadOnlySpan<float>.Empty);

        // Assert
        Assert.Equal(0f, peakL);
        Assert.Equal(0f, peakR);
        Assert.Equal(0f, rmsL);
        Assert.Equal(0f, rmsR);
    }

    [Fact]
    public void CalculateStereoLevels_StereoBuffer_CalculatesAccuratePeaks()
    {
        // Interleaved stereo samples: L0, R0, L1, R1, L2, R2
        float[] samples = [0.8f, 0.2f, -0.95f, 0.5f, 0.1f, -0.6f];

        // Act
        var (peakL, peakR, rmsL, rmsR) = AudioMixerDsp.CalculateStereoLevels(samples);

        // Assert
        Assert.Equal(0.95f, peakL, 2);
        Assert.Equal(0.6f, peakR, 2);
        Assert.True(rmsL > 0f);
        Assert.True(rmsR > 0f);
    }
}
