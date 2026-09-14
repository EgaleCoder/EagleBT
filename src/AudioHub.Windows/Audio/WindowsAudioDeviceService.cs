using AudioHub.Core.Interfaces;
using AudioHub.Core.Models;
using Microsoft.Extensions.Logging;
using NAudio.CoreAudioApi;
using NAudio.CoreAudioApi.Interfaces;

namespace AudioHub.Windows.Audio;

/// <summary>
/// Implements WASAPI audio endpoint discovery, state monitoring, and default device management.
/// </summary>
public sealed class WindowsAudioDeviceService : IAudioDeviceService, IMMNotificationClient
{
    private readonly ILogger<WindowsAudioDeviceService> _logger;
    private readonly MMDeviceEnumerator _deviceEnumerator;
    private bool _isDisposed;

    public event EventHandler<Core.Models.AudioDevice>? DeviceAdded;
    public event EventHandler<string>? DeviceRemoved;
    public event EventHandler<Core.Models.AudioDevice>? DeviceStateChanged;
    public event EventHandler<(AudioFlowType Flow, string DeviceId)>? DefaultDeviceChanged;

    public WindowsAudioDeviceService(ILogger<WindowsAudioDeviceService> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _deviceEnumerator = new MMDeviceEnumerator();
        _deviceEnumerator.RegisterEndpointNotificationCallback(this);
    }

    public Task<IReadOnlyList<Core.Models.AudioDevice>> GetAudioDevicesAsync(
        AudioFlowType flow = AudioFlowType.All,
        CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_isDisposed, this);

        var result = new List<Core.Models.AudioDevice>();

        if (flow is AudioFlowType.All or AudioFlowType.Render)
        {
            EnumerateEndpoints(DataFlow.Render, result);
        }

        if (flow is AudioFlowType.All or AudioFlowType.Capture)
        {
            EnumerateEndpoints(DataFlow.Capture, result);
        }

        _logger.LogInformation("Enumerated {Count} total WASAPI audio devices (Flow: {Flow})", result.Count, flow);
        return Task.FromResult<IReadOnlyList<Core.Models.AudioDevice>>(result);
    }

    public Task<Core.Models.AudioDevice?> GetDefaultDeviceAsync(
        AudioFlowType flow,
        CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_isDisposed, this);

        DataFlow dataFlow = flow == AudioFlowType.Capture ? DataFlow.Capture : DataFlow.Render;
        try
        {
            MMDevice defaultDevice = _deviceEnumerator.GetDefaultAudioEndpoint(dataFlow, Role.Multimedia);
            return Task.FromResult<Core.Models.AudioDevice?>(MapToAudioDevice(defaultDevice, isDefault: true));
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to retrieve default audio device for flow {Flow}", flow);
            return Task.FromResult<Core.Models.AudioDevice?>(null);
        }
    }

    private void EnumerateEndpoints(DataFlow dataFlow, List<Core.Models.AudioDevice> destinationList)
    {
        string? defaultId = null;
        try
        {
            MMDevice? defaultDevice = _deviceEnumerator.GetDefaultAudioEndpoint(dataFlow, Role.Multimedia);
            defaultId = defaultDevice?.ID;
        }
        catch
        {
            // Default device may not exist on headless or virtual setups
        }

        MMDeviceCollection collection = _deviceEnumerator.EnumerateAudioEndPoints(dataFlow, DeviceState.Active);
        foreach (MMDevice mmDevice in collection)
        {
            bool isDefault = string.Equals(mmDevice.ID, defaultId, StringComparison.OrdinalIgnoreCase);
            destinationList.Add(MapToAudioDevice(mmDevice, isDefault));
        }
    }

    private static Core.Models.AudioDevice MapToAudioDevice(MMDevice mmDevice, bool isDefault)
    {
        bool isBluetooth = false;
        try
        {
            // Check for Bluetooth indications in device properties or friendly name
            isBluetooth = mmDevice.FriendlyName.Contains("Bluetooth", StringComparison.OrdinalIgnoreCase) ||
                          mmDevice.DeviceFriendlyName.Contains("Bluetooth", StringComparison.OrdinalIgnoreCase) ||
                          mmDevice.FriendlyName.Contains("Mustang", StringComparison.OrdinalIgnoreCase) ||
                          mmDevice.FriendlyName.Contains("Boult", StringComparison.OrdinalIgnoreCase) ||
                          mmDevice.FriendlyName.Contains("Torq", StringComparison.OrdinalIgnoreCase);
        }
        catch
        {
            // Ignore property query errors on unplugged devices
        }

        return new Core.Models.AudioDevice
        {
            Id = mmDevice.ID,
            Name = string.IsNullOrWhiteSpace(mmDevice.FriendlyName) ? "Unknown Audio Device" : mmDevice.FriendlyName,
            Flow = mmDevice.DataFlow == DataFlow.Render ? AudioFlowType.Render : AudioFlowType.Capture,
            IsDefault = isDefault,
            State = (Core.Models.AudioDeviceState)(int)mmDevice.State,
            IsBluetoothEndpoint = isBluetooth
        };
    }

    #region IMMNotificationClient Implementation

    public void OnDeviceStateChanged(string deviceId, DeviceState newState)
    {
        _logger.LogInformation("Audio device state changed: ID {DeviceId}, NewState: {State}", deviceId, newState);
        try
        {
            MMDevice mmDevice = _deviceEnumerator.GetDevice(deviceId);
            var device = MapToAudioDevice(mmDevice, isDefault: false);
            DeviceStateChanged?.Invoke(this, device);
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Could not query updated state for device {DeviceId}", deviceId);
        }
    }

    public void OnDeviceAdded(string pwstrDeviceId)
    {
        _logger.LogInformation("Audio device added: ID {DeviceId}", pwstrDeviceId);
        try
        {
            MMDevice mmDevice = _deviceEnumerator.GetDevice(pwstrDeviceId);
            var device = MapToAudioDevice(mmDevice, isDefault: false);
            DeviceAdded?.Invoke(this, device);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to resolve added audio device {DeviceId}", pwstrDeviceId);
        }
    }

    public void OnDeviceRemoved(string deviceId)
    {
        _logger.LogInformation("Audio device removed: ID {DeviceId}", deviceId);
        DeviceRemoved?.Invoke(this, deviceId);
    }

    public void OnDefaultDeviceChanged(DataFlow flow, Role role, string defaultDeviceId)
    {
        if (role != Role.Multimedia) return;

        AudioFlowType flowType = flow == DataFlow.Render ? AudioFlowType.Render : AudioFlowType.Capture;
        _logger.LogInformation("Default audio endpoint changed: Flow {Flow}, New ID: {DeviceId}", flowType, defaultDeviceId);
        DefaultDeviceChanged?.Invoke(this, (flowType, defaultDeviceId));
    }

    public void OnPropertyValueChanged(string pwstrDeviceId, PropertyKey key)
    {
        // Property changes like format or volume level
    }

    #endregion

    public void Dispose()
    {
        if (_isDisposed) return;
        _isDisposed = true;
        _deviceEnumerator.UnregisterEndpointNotificationCallback(this);
        _deviceEnumerator.Dispose();
    }
}
