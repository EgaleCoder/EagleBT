using System.Collections.Concurrent;
using AudioHub.Core.Interfaces;
using Microsoft.Extensions.Logging;
using Windows.Media.Audio;

namespace AudioHub.Windows.Bluetooth;

/// <summary>
/// Implements Bluetooth A2DP audio sink stream reception on Windows 10/11 using
/// Windows.Media.Audio.AudioPlaybackConnection.
/// </summary>
public sealed class WindowsBluetoothAudioSinkService : IBluetoothAudioSinkService
{
    private readonly ILogger<WindowsBluetoothAudioSinkService> _logger;
    private readonly ConcurrentDictionary<string, AudioPlaybackConnection> _connections = new(StringComparer.OrdinalIgnoreCase);
    private readonly ConcurrentDictionary<string, AudioPlaybackStreamState> _states = new(StringComparer.OrdinalIgnoreCase);
    private bool _isDisposed;

    public event EventHandler<AudioPlaybackStreamEventArgs>? StreamStateChanged;

    public IReadOnlyDictionary<string, AudioPlaybackStreamState> ActiveStreams => _states;

    public WindowsBluetoothAudioSinkService(ILogger<WindowsBluetoothAudioSinkService> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<bool> StartStreamAsync(string deviceId, CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_isDisposed, this);
        ArgumentException.ThrowIfNullOrWhiteSpace(deviceId);
        cancellationToken.ThrowIfCancellationRequested();

        // If already active or opening, do not duplicate
        if (_states.TryGetValue(deviceId, out var currentState) &&
            currentState is AudioPlaybackStreamState.Streaming or AudioPlaybackStreamState.Opening)
        {
            _logger.LogInformation("Audio stream for device {DeviceId} is already {State}.", deviceId, currentState);
            return currentState == AudioPlaybackStreamState.Streaming;
        }

        UpdateState(deviceId, AudioPlaybackStreamState.Opening, "Initiating audio playback connection...");

        try
        {
            // Create the WinRT AudioPlaybackConnection from the remote Bluetooth device ID
            var connection = AudioPlaybackConnection.TryCreateFromId(deviceId);
            if (connection == null)
            {
                string msg = $"Device {deviceId} does not support AudioPlaybackConnection (A2DP Source).";
                _logger.LogWarning("{Message}", msg);
                UpdateState(deviceId, AudioPlaybackStreamState.Failed, msg);
                return false;
            }

            // Register for connection state changes
            connection.StateChanged += (sender, _) =>
            {
                var newState = sender.State switch
                {
                    AudioPlaybackConnectionState.Opened => AudioPlaybackStreamState.Streaming,
                    AudioPlaybackConnectionState.Closed => AudioPlaybackStreamState.Closed,
                    _ => AudioPlaybackStreamState.Closed
                };
                UpdateState(deviceId, newState, $"AudioPlaybackConnection state changed to {sender.State}.");
            };

            // Enable the connection
            connection.Start();

            // Open the audio path asynchronously
            var result = await connection.OpenAsync();
            if (result.Status == AudioPlaybackConnectionOpenResultStatus.Success)
            {
                _connections[deviceId] = connection;
                UpdateState(deviceId, AudioPlaybackStreamState.Streaming, "Audio stream opened successfully.");
                _logger.LogInformation("A2DP Sink stream established for remote device {DeviceId}", deviceId);
                return true;
            }
            else
            {
                string errorMsg = $"Failed to open AudioPlaybackConnection. Status: {result.Status}, ExtendedError: {result.ExtendedError?.Message ?? "None"}";
                _logger.LogWarning("{Message}", errorMsg);
                connection.Dispose();
                UpdateState(deviceId, AudioPlaybackStreamState.Failed, errorMsg);
                return false;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected exception starting audio stream for device {DeviceId}", deviceId);
            UpdateState(deviceId, AudioPlaybackStreamState.Failed, ex.Message);
            return false;
        }
    }

    public Task StopStreamAsync(string deviceId, CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_isDisposed, this);
        ArgumentException.ThrowIfNullOrWhiteSpace(deviceId);
        cancellationToken.ThrowIfCancellationRequested();

        if (_connections.TryRemove(deviceId, out var connection))
        {
            try
            {
                connection.Dispose();
            }
            catch (Exception ex)
            {
                _logger.LogDebug(ex, "Error disposing connection for {DeviceId}", deviceId);
            }
        }

        UpdateState(deviceId, AudioPlaybackStreamState.Closed, "Audio stream stopped.");
        _logger.LogInformation("A2DP Sink stream stopped for device {DeviceId}", deviceId);
        return Task.CompletedTask;
    }

    public async Task StopAllStreamsAsync(CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_isDisposed, this);
        cancellationToken.ThrowIfCancellationRequested();

        var deviceIds = _connections.Keys.ToList();
        foreach (var devId in deviceIds)
        {
            await StopStreamAsync(devId, cancellationToken);
        }
    }

    public AudioPlaybackStreamState GetStreamState(string deviceId)
    {
        return _states.TryGetValue(deviceId, out var state) ? state : AudioPlaybackStreamState.Closed;
    }

    public bool IsDeviceStreaming(string deviceId)
    {
        return _states.TryGetValue(deviceId, out var state) && state == AudioPlaybackStreamState.Streaming;
    }

    private void UpdateState(string deviceId, AudioPlaybackStreamState state, string? message)
    {
        _states[deviceId] = state;
        StreamStateChanged?.Invoke(this, new AudioPlaybackStreamEventArgs
        {
            DeviceId = deviceId,
            State = state,
            Message = message
        });
    }

    public void Dispose()
    {
        if (_isDisposed) return;
        _isDisposed = true;

        foreach (var kvp in _connections)
        {
            try
            {
                kvp.Value.Dispose();
            }
            catch
            {
                // Ignore disposal errors on teardown
            }
        }

        _connections.Clear();
        _states.Clear();
    }
}
