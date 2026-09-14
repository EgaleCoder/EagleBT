using AudioHub.Core.Models;

namespace AudioHub.Core.Interfaces;

/// <summary>
/// Provides discovery, state monitoring, and enumeration of Bluetooth devices.
/// </summary>
public interface IBluetoothDeviceService : IDisposable
{
    /// <summary>
    /// Gets the list of currently discovered or paired Bluetooth devices.
    /// </summary>
    Task<IReadOnlyList<BluetoothDevice>> GetDiscoveredDevicesAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Occurs when a new Bluetooth device is discovered.
    /// </summary>
    event EventHandler<BluetoothDevice>? DeviceDiscovered;

    /// <summary>
    /// Occurs when an existing Bluetooth device updates its state (e.g. connected, disconnected).
    /// </summary>
    event EventHandler<BluetoothDevice>? DeviceUpdated;

    /// <summary>
    /// Occurs when a Bluetooth device is no longer reachable or removed.
    /// </summary>
    event EventHandler<string>? DeviceRemoved;

    /// <summary>
    /// Starts background discovery and continuous watcher monitoring.
    /// </summary>
    void StartDiscovery();

    /// <summary>
    /// Stops background discovery and continuous watcher monitoring.
    /// </summary>
    void StopDiscovery();
}
