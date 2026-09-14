namespace AudioHub.Core.Models;

/// <summary>
/// Defines an active routing mapping from an AudioSource to an AudioOutput.
/// </summary>
public sealed class AudioRoute
{
    public required string Id { get; init; }
    public required AudioSource Source { get; init; }
    public required AudioOutput Output { get; init; }
    public bool IsActive { get; set; } = true;
    public float RouteGain { get; set; } = 1.0f;

    /// <summary>
    /// Computes the effective volume taking into account source, route gain, and output volume.
    /// </summary>
    public float EffectiveVolume =>
        (Source.IsMuted || Output.IsMuted || !IsActive)
            ? 0f
            : Math.Clamp(Source.Volume * RouteGain * Output.MasterVolume, 0f, 1f);

    public override string ToString() =>
        $"{Source.DisplayName} -> {Output.DisplayName} [{(IsActive ? "Active" : "Inactive")}] EffectiveVol: {EffectiveVolume:P0}";
}
