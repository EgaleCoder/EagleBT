using AudioHub.Core.Models;

namespace AudioHub.Core.Interfaces;

/// <summary>
/// Provides enumeration, endpoint monitoring, and default selection for Windows audio devices.
/// </summary>
public interface IAudioDeviceService : IDisposable
{
    /// <summary>
    /// Gets all available audio endpoints filtered by flow direction.
    /// </summary>
    Task<IReadOnlyList<AudioDevice>> GetAudioDevicesAsync(
        AudioFlowType flow = AudioFlowType.All,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the current default audio endpoint for the specified flow direction.
    /// </summary>
    Task<AudioDevice?> GetDefaultDeviceAsync(
        AudioFlowType flow,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Occurs when an audio endpoint is added or becomes active.
    /// </summary>
    event EventHandler<AudioDevice>? DeviceAdded;

    /// <summary>
    /// Occurs when an audio endpoint is removed or disabled.
    /// </summary>
    event EventHandler<string>? DeviceRemoved;

    /// <summary>
    /// Occurs when an audio endpoint's state changes.
    /// </summary>
    event EventHandler<AudioDevice>? DeviceStateChanged;

    /// <summary>
    /// Occurs when the default system audio endpoint changes.
    /// </summary>
    event EventHandler<(AudioFlowType Flow, string DeviceId)>? DefaultDeviceChanged;
}
