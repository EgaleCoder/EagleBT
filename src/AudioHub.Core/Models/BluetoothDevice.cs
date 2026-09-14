namespace AudioHub.Core.Models;

/// <summary>
/// Specifies the type of Bluetooth radio transport.
/// </summary>
public enum BluetoothDeviceType
{
    Unknown = 0,
    Classic = 1,
    LowEnergy = 2,
    Dual = 3
}

/// <summary>
/// Represents a discovered or paired Bluetooth device in the system.
/// </summary>
public sealed class BluetoothDevice
{
    public required string Id { get; init; }
    public required string Name { get; init; }
    public ulong BluetoothAddress { get; init; }
    public BluetoothDeviceType DeviceType { get; init; } = BluetoothDeviceType.Unknown;
    public bool IsConnected { get; set; }
    public bool IsPaired { get; init; }
    public int? SignalStrength { get; set; }
    public string ConnectionStatusText => IsConnected ? "Connected" : "Paired";

    public override string ToString() => $"{Name} ({Id}) - Connected: {IsConnected}";
}
