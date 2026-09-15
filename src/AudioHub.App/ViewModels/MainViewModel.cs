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
    private readonly IBluetoothAudioSinkService _bluetoothAudioSinkService;
    private readonly IAudioRoutingService _audioRoutingService;
    private readonly DispatcherQueue _dispatcherQueue;

    private string _statusMessage = "Ready";
    private bool _isScanning;
    private bool _isDisposed;

    private BluetoothDevice? _activeStreamDevice;
    private AudioDevice? _selectedOutputDevice;
    private AudioRoute? _activeRoute;
    private AudioPlaybackStreamState _streamState = AudioPlaybackStreamState.Closed;
    private string _streamStatusText = "Idle — No active audio stream";
    private float _routeGain = 1.0f;
    private bool _isRouteMuted;

    public ObservableCollection<BluetoothDevice> BluetoothDevices { get; } = new();
    public ObservableCollection<AudioDevice> AudioDevices { get; } = new();
    public ObservableCollection<AudioDevice> RenderEndpoints { get; } = new();

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

    public BluetoothDevice? ActiveStreamDevice
    {
        get => _activeStreamDevice;
        set
        {
            if (SetField(ref _activeStreamDevice, value))
            {
                OnPropertyChanged(nameof(HasActiveStreamDevice));
            }
        }
    }

    public bool HasActiveStreamDevice => _activeStreamDevice != null;

    public AudioDevice? SelectedOutputDevice
    {
        get => _selectedOutputDevice;
        set => SetField(ref _selectedOutputDevice, value);
    }

    public AudioRoute? ActiveRoute
    {
        get => _activeRoute;
        set
        {
            if (SetField(ref _activeRoute, value))
            {
                OnPropertyChanged(nameof(HasActiveRoute));
            }
        }
    }

    public bool HasActiveRoute => _activeRoute != null;

    public AudioPlaybackStreamState StreamState
    {
        get => _streamState;
        private set
        {
            if (SetField(ref _streamState, value))
            {
                OnPropertyChanged(nameof(IsStreaming));
                OnPropertyChanged(nameof(IsOpening));
            }
        }
    }

    public bool IsStreaming => _streamState == AudioPlaybackStreamState.Streaming;
    public bool IsOpening => _streamState == AudioPlaybackStreamState.Opening;

    public string StreamStatusText
    {
        get => _streamStatusText;
        private set => SetField(ref _streamStatusText, value);
    }

    public float RouteGain
    {
        get => _routeGain;
        set
        {
            if (SetField(ref _routeGain, value) && _activeRoute != null)
            {
                _audioRoutingService.SetRouteGain(_activeRoute.Id, value);
            }
        }
    }

    public bool IsRouteMuted
    {
        get => _isRouteMuted;
        set
        {
            if (SetField(ref _isRouteMuted, value) && _activeRoute != null)
            {
                _audioRoutingService.SetRouteMute(_activeRoute.Id, value);
            }
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public MainViewModel(
        IBluetoothDeviceService bluetoothDeviceService,
        IAudioDeviceService audioDeviceService,
        IBluetoothAudioSinkService bluetoothAudioSinkService,
        IAudioRoutingService audioRoutingService)
    {
        _bluetoothDeviceService = bluetoothDeviceService ?? throw new ArgumentNullException(nameof(bluetoothDeviceService));
        _audioDeviceService = audioDeviceService ?? throw new ArgumentNullException(nameof(audioDeviceService));
        _bluetoothAudioSinkService = bluetoothAudioSinkService ?? throw new ArgumentNullException(nameof(bluetoothAudioSinkService));
        _audioRoutingService = audioRoutingService ?? throw new ArgumentNullException(nameof(audioRoutingService));
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

        // Subscribe to Stream & Route events
        _bluetoothAudioSinkService.StreamStateChanged += OnStreamStateChanged;
        _audioRoutingService.RouteUpdated += OnRouteUpdated;
        _audioRoutingService.RouteRemoved += OnRouteRemoved;
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
            RenderEndpoints.Clear();

            foreach (var audioDev in audioList)
            {
                AudioDevices.Add(audioDev);
                if (audioDev.Flow == AudioFlowType.Render)
                {
                    RenderEndpoints.Add(audioDev);
                }
            }

            // Auto-select preferred output (Mustang GoBoult Torq or default playback device)
            SelectedOutputDevice = RenderEndpoints.FirstOrDefault(d => d.IsBluetoothEndpoint)
                                ?? RenderEndpoints.FirstOrDefault(d => d.IsDefault)
                                ?? RenderEndpoints.FirstOrDefault();

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
            RenderEndpoints.Clear();

            foreach (var audioDev in audioList)
            {
                AudioDevices.Add(audioDev);
                if (audioDev.Flow == AudioFlowType.Render)
                {
                    RenderEndpoints.Add(audioDev);
                }
            }

            if (SelectedOutputDevice == null || !RenderEndpoints.Any(d => d.Id == SelectedOutputDevice.Id))
            {
                SelectedOutputDevice = RenderEndpoints.FirstOrDefault(d => d.IsBluetoothEndpoint)
                                    ?? RenderEndpoints.FirstOrDefault(d => d.IsDefault)
                                    ?? RenderEndpoints.FirstOrDefault();
            }
        }
        catch (Exception ex)
        {
            StatusMessage = $"Failed to refresh audio: {ex.Message}";
        }
    }

    public async Task StartStreamingAsync(BluetoothDevice device)
    {
        ArgumentNullException.ThrowIfNull(device);

        ActiveStreamDevice = device;
        StreamStatusText = $"Connecting A2DP Sink to {device.Name}...";
        StatusMessage = $"Initiating stream from {device.Name}...";

        // Setup domain route
        var targetOutput = SelectedOutputDevice;
        if (targetOutput != null)
        {
            var source = new AudioSource
            {
                Id = device.Id,
                DisplayName = device.Name,
                Type = SourceType.RemoteMedia,
                Volume = 1.0f,
                IsMuted = false
            };

            var output = new AudioOutput
            {
                Id = targetOutput.Id,
                DisplayName = targetOutput.Name,
                MasterVolume = 1.0f,
                IsMuted = false
            };

            ActiveRoute = await _audioRoutingService.ConnectRouteAsync(source, output);
        }

        // Start Bluetooth A2DP Sink stream
        bool success = await _bluetoothAudioSinkService.StartStreamAsync(device.Id);
        if (!success)
        {
            StatusMessage = $"Could not open stream for {device.Name}. Ensure device is paired and audio is active.";
        }
    }

    public async Task StopStreamingAsync()
    {
        if (ActiveStreamDevice != null)
        {
            StatusMessage = $"Stopping stream from {ActiveStreamDevice.Name}...";
            await _bluetoothAudioSinkService.StopStreamAsync(ActiveStreamDevice.Id);
        }

        if (ActiveRoute != null)
        {
            await _audioRoutingService.DisconnectRouteAsync(ActiveRoute.Id);
            ActiveRoute = null;
        }

        ActiveStreamDevice = null;
        StreamStatusText = "Idle — No active audio stream";
        StatusMessage = "Stream stopped.";
    }

    public void ToggleMute()
    {
        IsRouteMuted = !IsRouteMuted;
    }

    private void OnStreamStateChanged(object? sender, AudioPlaybackStreamEventArgs e)
    {
        _dispatcherQueue.TryEnqueue(() =>
        {
            StreamState = e.State;
            StreamStatusText = e.State switch
            {
                AudioPlaybackStreamState.Streaming => $"Streaming: {ActiveStreamDevice?.Name ?? e.DeviceId} ➔ {SelectedOutputDevice?.Name ?? "Earbuds"}",
                AudioPlaybackStreamState.Opening => "Opening A2DP sink connection...",
                AudioPlaybackStreamState.Failed => $"Stream connection failed: {e.Message ?? "Unknown error"}",
                AudioPlaybackStreamState.Closed => "Stream closed.",
                _ => "Idle"
            };

            StatusMessage = StreamStatusText;
        });
    }

    private void OnRouteUpdated(object? sender, AudioRouteEventArgs e)
    {
        _dispatcherQueue.TryEnqueue(() =>
        {
            ActiveRoute = e.Route;
            _routeGain = e.Route.RouteGain;
            _isRouteMuted = e.Route.Source.IsMuted;
            OnPropertyChanged(nameof(RouteGain));
            OnPropertyChanged(nameof(IsRouteMuted));
        });
    }

    private void OnRouteRemoved(object? sender, string routeId)
    {
        _dispatcherQueue.TryEnqueue(() =>
        {
            if (_activeRoute != null && string.Equals(_activeRoute.Id, routeId, StringComparison.OrdinalIgnoreCase))
            {
                ActiveRoute = null;
            }
        });
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

            if (ActiveStreamDevice != null && string.Equals(ActiveStreamDevice.Id, deviceId, StringComparison.OrdinalIgnoreCase))
            {
                _ = StopStreamingAsync();
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
                if (device.Flow == AudioFlowType.Render)
                {
                    RenderEndpoints.Add(device);
                }
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
                RenderEndpoints.Remove(existing);
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

                int renderIndex = RenderEndpoints.IndexOf(existing);
                if (renderIndex >= 0)
                {
                    RenderEndpoints[renderIndex] = device;
                }
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
        OnPropertyChanged(propertyName);
        return true;
    }

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
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

        _bluetoothAudioSinkService.StreamStateChanged -= OnStreamStateChanged;
        _audioRoutingService.RouteUpdated -= OnRouteUpdated;
        _audioRoutingService.RouteRemoved -= OnRouteRemoved;
    }
}
