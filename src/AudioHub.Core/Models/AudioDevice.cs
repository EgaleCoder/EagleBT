namespace AudioHub.Core.Models;

/// <summary>
/// Specifies the audio stream direction (playback or capture).
/// </summary>
public enum AudioFlowType
{
    All = 0,
    Render = 1,
    Capture = 2
}

/// <summary>
/// Indicates the operational state of an audio device endpoint.
/// </summary>
public enum AudioDeviceState
{
    Active = 1,
    Disabled = 2,
    NotPresent = 4,
    Unplugged = 8
}

/// <summary>
/// Represents a Windows audio hardware endpoint (e.g. speakers, headphones, microphone).
/// </summary>
public sealed class AudioDevice
{
    public required string Id { get; init; }
    public required string Name { get; init; }
    public AudioFlowType Flow { get; init; }
    public bool IsDefault { get; set; }
    public AudioDeviceState State { get; set; } = AudioDeviceState.Active;
    public bool IsBluetoothEndpoint { get; set; }

    public override string ToString() => $"{Name} [{Flow}] - Default: {IsDefault}";
}
