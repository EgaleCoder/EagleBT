using System.Collections.Concurrent;
using AudioHub.Core.Interfaces;
using AudioHub.Core.Models;
using Microsoft.Extensions.Logging;
using Windows.Devices.Bluetooth;
using Windows.Devices.Enumeration;

namespace AudioHub.Windows.Bluetooth;

/// <summary>
/// Implements Bluetooth device discovery and monitoring using Windows.Devices.Bluetooth and DeviceWatcher.
/// </summary>
public sealed class WindowsBluetoothDeviceService : IBluetoothDeviceService
{
    private readonly ILogger<WindowsBluetoothDeviceService> _logger;
    private readonly ConcurrentDictionary<string, Core.Models.BluetoothDevice> _devices = new(StringComparer.OrdinalIgnoreCase);
    private DeviceWatcher? _deviceWatcher;
    private bool _isDisposed;

    public event EventHandler<Core.Models.BluetoothDevice>? DeviceDiscovered;
    public event EventHandler<Core.Models.BluetoothDevice>? DeviceUpdated;
    public event EventHandler<string>? DeviceRemoved;

    public WindowsBluetoothDeviceService(ILogger<WindowsBluetoothDeviceService> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public Task<IReadOnlyList<Core.Models.BluetoothDevice>> GetDiscoveredDevicesAsync(CancellationToken cancellationToken = default)
    {
        IReadOnlyList<Core.Models.BluetoothDevice> list = _devices.Values.ToList();
        return Task.FromResult(list);
    }

    public void StartDiscovery()
    {
        ObjectDisposedException.ThrowIf(_isDisposed, this);

        if (_deviceWatcher != null)
        {
            _logger.LogDebug("Bluetooth discovery watcher is already running.");
            return;
        }

        _logger.LogInformation("Starting Windows Bluetooth device discovery watcher...");

        // Query selector for paired and active Bluetooth Classic devices
        string aqs = global::Windows.Devices.Bluetooth.BluetoothDevice.GetDeviceSelectorFromPairingState(true);
        string[] requestedProperties =
        [
            "System.ItemNameDisplay",
            "System.Devices.AConnection.Bluetooth.DeviceAddress",
            "System.Devices.Connected"
        ];

        _deviceWatcher = DeviceInformation.CreateWatcher(
            aqs,
            requestedProperties,
            DeviceInformationKind.DeviceInterface);

        _deviceWatcher.Added += OnDeviceAdded;
        _deviceWatcher.Updated += OnDeviceUpdated;
        _deviceWatcher.Removed += OnDeviceRemoved;
        _deviceWatcher.EnumerationCompleted += OnEnumerationCompleted;
        _deviceWatcher.Stopped += OnWatcherStopped;

        _deviceWatcher.Start();
    }

    public void StopDiscovery()
    {
        if (_deviceWatcher == null) return;

        _logger.LogInformation("Stopping Windows Bluetooth device discovery watcher...");
        _deviceWatcher.Stop();
        _deviceWatcher.Added -= OnDeviceAdded;
        _deviceWatcher.Updated -= OnDeviceUpdated;
        _deviceWatcher.Removed -= OnDeviceRemoved;
        _deviceWatcher.EnumerationCompleted -= OnEnumerationCompleted;
        _deviceWatcher.Stopped -= OnWatcherStopped;
        _deviceWatcher = null;
    }

    private void OnDeviceAdded(DeviceWatcher sender, DeviceInformation info)
    {
        bool isConnected = false;
        if (info.Properties.TryGetValue("System.Devices.Connected", out object? connectedObj) && connectedObj is bool b)
        {
            isConnected = b;
        }

        ulong address = 0;
        if (info.Properties.TryGetValue("System.Devices.AConnection.Bluetooth.DeviceAddress", out object? addrObj) && addrObj is ulong u)
        {
            address = u;
        }

        var device = new Core.Models.BluetoothDevice
        {
            Id = info.Id,
            Name = string.IsNullOrWhiteSpace(info.Name) ? "Unknown Bluetooth Device" : info.Name,
            BluetoothAddress = address,
            DeviceType = BluetoothDeviceType.Classic,
            IsConnected = isConnected,
            IsPaired = info.Pairing.IsPaired
        };

        _devices[info.Id] = device;
        _logger.LogInformation("Bluetooth device discovered: {DeviceName} (ID: {DeviceId}, Connected: {IsConnected})",
            device.Name, device.Id, device.IsConnected);

        DeviceDiscovered?.Invoke(this, device);
    }

    private void OnDeviceUpdated(DeviceWatcher sender, DeviceInformationUpdate update)
    {
        if (_devices.TryGetValue(update.Id, out var existingDevice))
        {
            if (update.Properties.TryGetValue("System.Devices.Connected", out object? connectedObj) && connectedObj is bool b)
            {
                existingDevice.IsConnected = b;
            }

            _logger.LogDebug("Bluetooth device updated: {DeviceName} (Connected: {IsConnected})",
                existingDevice.Name, existingDevice.IsConnected);

            DeviceUpdated?.Invoke(this, existingDevice);
        }
    }

    private void OnDeviceRemoved(DeviceWatcher sender, DeviceInformationUpdate update)
    {
        if (_devices.TryRemove(update.Id, out var removedDevice))
        {
            _logger.LogInformation("Bluetooth device removed: {DeviceName} (ID: {DeviceId})",
                removedDevice.Name, update.Id);

            DeviceRemoved?.Invoke(this, update.Id);
        }
    }

    private void OnEnumerationCompleted(DeviceWatcher sender, object args)
    {
        _logger.LogInformation("Initial Bluetooth device enumeration completed. Discovered {Count} devices.", _devices.Count);
    }

    private void OnWatcherStopped(DeviceWatcher sender, object args)
    {
        _logger.LogInformation("Bluetooth device watcher stopped.");
    }

    public void Dispose()
    {
        if (_isDisposed) return;
        _isDisposed = true;
        StopDiscovery();
        _devices.Clear();
    }
}
