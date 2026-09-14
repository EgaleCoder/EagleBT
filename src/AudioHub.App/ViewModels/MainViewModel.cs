using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using AudioHub.Core.Interfaces;
using AudioHub.Core.Models;
using Microsoft.UI.Dispatching;

namespace AudioHub_App.ViewModels;

public sealed class MainViewModel : INotifyPropertyChanged, IDisposable
{
    private readonly IBluetoothDeviceService _bluetoothDeviceService;
    private readonly IAudioDeviceService _audioDeviceService;
    private readonly DispatcherQueue _dispatcherQueue;

    private string _statusMessage = "Ready";
    private bool _isScanning;
    private bool _isDisposed;

    public ObservableCollection<BluetoothDevice> BluetoothDevices { get; } = new();
    public ObservableCollection<AudioDevice> AudioDevices { get; } = new();

    public string StatusMessage
    {
        get => _statusMessage;
        set => SetField(ref _statusMessage, value);
    }

    public bool IsScanning
    {
        get => _isScanning;
        set => SetField(ref _isScanning, value);
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public MainViewModel(
        IBluetoothDeviceService bluetoothDeviceService,
        IAudioDeviceService audioDeviceService)
    {
        _bluetoothDeviceService = bluetoothDeviceService ?? throw new ArgumentNullException(nameof(bluetoothDeviceService));
        _audioDeviceService = audioDeviceService ?? throw new ArgumentNullException(nameof(audioDeviceService));
        _dispatcherQueue = DispatcherQueue.GetForCurrentThread() ?? throw new InvalidOperationException("DispatcherQueue not available.");

        // Subscribe to Bluetooth device service events
        _bluetoothDeviceService.DeviceDiscovered += OnBluetoothDeviceDiscovered;
        _bluetoothDeviceService.DeviceUpdated += OnBluetoothDeviceUpdated;
        _bluetoothDeviceService.DeviceRemoved += OnBluetoothDeviceRemoved;

        // Subscribe to Audio device service events
        _audioDeviceService.DeviceAdded += OnAudioDeviceAdded;
        _audioDeviceService.DeviceRemoved += OnAudioDeviceRemoved;
        _audioDeviceService.DeviceStateChanged += OnAudioDeviceStateChanged;
        _audioDeviceService.DefaultDeviceChanged += OnDefaultAudioDeviceChanged;
    }

    public async Task InitializeAsync()
    {
        StatusMessage = "Loading devices...";
        IsScanning = true;

        try
        {
            // Load Audio Devices
            var audioList = await _audioDeviceService.GetAudioDevicesAsync(AudioFlowType.All);
            AudioDevices.Clear();
            foreach (var audioDev in audioList)
            {
                AudioDevices.Add(audioDev);
            }

            // Start Bluetooth Watcher
            _bluetoothDeviceService.StartDiscovery();
            StatusMessage = "Listening for Bluetooth & Audio devices";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error loading devices: {ex.Message}";
        }
        finally
        {
            IsScanning = false;
        }
    }

    public async Task RefreshAudioDevicesAsync()
    {
        try
        {
            var audioList = await _audioDeviceService.GetAudioDevicesAsync(AudioFlowType.All);
            AudioDevices.Clear();
            foreach (var audioDev in audioList)
            {
                AudioDevices.Add(audioDev);
            }
        }
        catch (Exception ex)
        {
            StatusMessage = $"Failed to refresh audio: {ex.Message}";
        }
    }

    private void OnBluetoothDeviceDiscovered(object? sender, BluetoothDevice device)
    {
        _dispatcherQueue.TryEnqueue(() =>
        {
            var existing = BluetoothDevices.FirstOrDefault(d => string.Equals(d.Id, device.Id, StringComparison.OrdinalIgnoreCase));
            if (existing == null)
            {
                BluetoothDevices.Add(device);
            }
        });
    }

    private void OnBluetoothDeviceUpdated(object? sender, BluetoothDevice device)
    {
        _dispatcherQueue.TryEnqueue(() =>
        {
            var existing = BluetoothDevices.FirstOrDefault(d => string.Equals(d.Id, device.Id, StringComparison.OrdinalIgnoreCase));
            if (existing != null)
            {
                int index = BluetoothDevices.IndexOf(existing);
                BluetoothDevices[index] = device;
            }
        });
    }

    private void OnBluetoothDeviceRemoved(object? sender, string deviceId)
    {
        _dispatcherQueue.TryEnqueue(() =>
        {
            var existing = BluetoothDevices.FirstOrDefault(d => string.Equals(d.Id, deviceId, StringComparison.OrdinalIgnoreCase));
            if (existing != null)
            {
                BluetoothDevices.Remove(existing);
            }
        });
    }

    private void OnAudioDeviceAdded(object? sender, AudioDevice device)
    {
        _dispatcherQueue.TryEnqueue(() =>
        {
            var existing = AudioDevices.FirstOrDefault(d => string.Equals(d.Id, device.Id, StringComparison.OrdinalIgnoreCase));
            if (existing == null)
            {
                AudioDevices.Add(device);
            }
        });
    }

    private void OnAudioDeviceRemoved(object? sender, string deviceId)
    {
        _dispatcherQueue.TryEnqueue(() =>
        {
            var existing = AudioDevices.FirstOrDefault(d => string.Equals(d.Id, deviceId, StringComparison.OrdinalIgnoreCase));
            if (existing != null)
            {
                AudioDevices.Remove(existing);
            }
        });
    }

    private void OnAudioDeviceStateChanged(object? sender, AudioDevice device)
    {
        _dispatcherQueue.TryEnqueue(() =>
        {
            var existing = AudioDevices.FirstOrDefault(d => string.Equals(d.Id, device.Id, StringComparison.OrdinalIgnoreCase));
            if (existing != null)
            {
                int index = AudioDevices.IndexOf(existing);
                AudioDevices[index] = device;
            }
        });
    }

    private void OnDefaultAudioDeviceChanged(object? sender, (AudioFlowType Flow, string DeviceId) args)
    {
        _dispatcherQueue.TryEnqueue(() =>
        {
            foreach (var dev in AudioDevices.Where(d => d.Flow == args.Flow))
            {
                dev.IsDefault = string.Equals(dev.Id, args.DeviceId, StringComparison.OrdinalIgnoreCase);
            }
        });
    }

    private bool SetField<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value)) return false;
        field = value;
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        return true;
    }

    public void Dispose()
    {
        if (_isDisposed) return;
        _isDisposed = true;

        _bluetoothDeviceService.DeviceDiscovered -= OnBluetoothDeviceDiscovered;
        _bluetoothDeviceService.DeviceUpdated -= OnBluetoothDeviceUpdated;
        _bluetoothDeviceService.DeviceRemoved -= OnBluetoothDeviceRemoved;

        _audioDeviceService.DeviceAdded -= OnAudioDeviceAdded;
        _audioDeviceService.DeviceRemoved -= OnAudioDeviceRemoved;
        _audioDeviceService.DeviceStateChanged -= OnAudioDeviceStateChanged;
        _audioDeviceService.DefaultDeviceChanged -= OnDefaultAudioDeviceChanged;
    }
}
