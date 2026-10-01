using System.Runtime.InteropServices;
using AudioHub.Core.Interfaces;
using AudioHub.Core.Models;
using Microsoft.Extensions.Logging;
using Windows.Media.Control;

namespace AudioHub.Windows.Bluetooth;

/// <summary>
/// Implements AVRCP media transport controls and track metadata tracking for EagleBT
/// utilizing Windows.Media.Control (GlobalSystemMediaTransportControlsSessionManager)
/// with Win32 hardware media key emulation fallback.
/// </summary>
public sealed class WindowsMediaControlService : IMediaControlService
{
    private readonly ILogger<WindowsMediaControlService> _logger;
    private readonly SemaphoreSlim _initLock = new(1, 1);

    private GlobalSystemMediaTransportControlsSessionManager? _sessionManager;
    private GlobalSystemMediaTransportControlsSession? _currentSession;
    private MediaMetadata _cachedMetadata = new();
    private MediaPlaybackInfo _cachedPlaybackInfo = new();
    private bool _isDisposed;
    private bool _isInitialized;

    public event EventHandler<MediaPlaybackStateChangedEventArgs>? PlaybackStateChanged;

    public WindowsMediaControlService(ILogger<WindowsMediaControlService> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<MediaMetadata?> GetCurrentMediaMetadataAsync(CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_isDisposed, this);
        await EnsureInitializedAsync(cancellationToken);

        if (_currentSession != null)
        {
            await RefreshCurrentSessionMetadataAsync(_currentSession);
        }

        return _cachedMetadata;
    }

    public async Task<MediaPlaybackInfo?> GetCurrentPlaybackInfoAsync(CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_isDisposed, this);
        await EnsureInitializedAsync(cancellationToken);

        if (_currentSession != null)
        {
            RefreshCurrentSessionPlaybackInfo(_currentSession);
        }

        return _cachedPlaybackInfo;
    }

    public async Task<bool> PlayAsync(string? sourceOrSessionId = null, CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_isDisposed, this);
        await EnsureInitializedAsync(cancellationToken);

        var session = ResolveSession(sourceOrSessionId);
        if (session != null)
        {
            try
            {
                bool success = await session.TryPlayAsync();
                if (success)
                {
                    _logger.LogInformation("Play command sent successfully via WinRT GSMTC.");
                    return true;
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to invoke TryPlayAsync on current media session. Falling back to hardware key.");
            }
        }

        return await SendHardwareMediaKeyAsync(MediaHardwareKey.PlayPause, cancellationToken);
    }

    public async Task<bool> PauseAsync(string? sourceOrSessionId = null, CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_isDisposed, this);
        await EnsureInitializedAsync(cancellationToken);

        var session = ResolveSession(sourceOrSessionId);
        if (session != null)
        {
            try
            {
                bool success = await session.TryPauseAsync();
                if (success)
                {
                    _logger.LogInformation("Pause command sent successfully via WinRT GSMTC.");
                    return true;
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to invoke TryPauseAsync on current media session. Falling back to hardware key.");
            }
        }

        return await SendHardwareMediaKeyAsync(MediaHardwareKey.PlayPause, cancellationToken);
    }

    public async Task<bool> TogglePlayPauseAsync(string? sourceOrSessionId = null, CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_isDisposed, this);
        await EnsureInitializedAsync(cancellationToken);

        var session = ResolveSession(sourceOrSessionId);
        if (session != null)
        {
            try
            {
                bool success = await session.TryTogglePlayPauseAsync();
                if (success)
                {
                    _logger.LogInformation("TogglePlayPause command sent successfully via WinRT GSMTC.");
                    return true;
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to invoke TryTogglePlayPauseAsync. Falling back to hardware key.");
            }
        }

        return await SendHardwareMediaKeyAsync(MediaHardwareKey.PlayPause, cancellationToken);
    }

    public async Task<bool> SkipNextAsync(string? sourceOrSessionId = null, CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_isDisposed, this);
        await EnsureInitializedAsync(cancellationToken);

        var session = ResolveSession(sourceOrSessionId);
        if (session != null)
        {
            try
            {
                bool success = await session.TrySkipNextAsync();
                if (success)
                {
                    _logger.LogInformation("SkipNext command sent successfully via WinRT GSMTC.");
                    return true;
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to invoke TrySkipNextAsync. Falling back to hardware key.");
            }
        }

        return await SendHardwareMediaKeyAsync(MediaHardwareKey.NextTrack, cancellationToken);
    }

    public async Task<bool> SkipPreviousAsync(string? sourceOrSessionId = null, CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_isDisposed, this);
        await EnsureInitializedAsync(cancellationToken);

        var session = ResolveSession(sourceOrSessionId);
        if (session != null)
        {
            try
            {
                bool success = await session.TrySkipPreviousAsync();
                if (success)
                {
                    _logger.LogInformation("SkipPrevious command sent successfully via WinRT GSMTC.");
                    return true;
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to invoke TrySkipPreviousAsync. Falling back to hardware key.");
            }
        }

        return await SendHardwareMediaKeyAsync(MediaHardwareKey.PreviousTrack, cancellationToken);
    }

    public async Task<bool> StopAsync(string? sourceOrSessionId = null, CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_isDisposed, this);
        await EnsureInitializedAsync(cancellationToken);

        var session = ResolveSession(sourceOrSessionId);
        if (session != null)
        {
            try
            {
                bool success = await session.TryStopAsync();
                if (success)
                {
                    _logger.LogInformation("Stop command sent successfully via WinRT GSMTC.");
                    return true;
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to invoke TryStopAsync. Falling back to hardware key.");
            }
        }

        return await SendHardwareMediaKeyAsync(MediaHardwareKey.Stop, cancellationToken);
    }

    public Task<bool> SendHardwareMediaKeyAsync(MediaHardwareKey key, CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_isDisposed, this);
        cancellationToken.ThrowIfCancellationRequested();

        byte vk = key switch
        {
            MediaHardwareKey.PlayPause => VK_MEDIA_PLAY_PAUSE,
            MediaHardwareKey.NextTrack => VK_MEDIA_NEXT_TRACK,
            MediaHardwareKey.PreviousTrack => VK_MEDIA_PREV_TRACK,
            MediaHardwareKey.Stop => VK_MEDIA_STOP,
            MediaHardwareKey.VolumeUp => VK_VOLUME_UP,
            MediaHardwareKey.VolumeDown => VK_VOLUME_DOWN,
            MediaHardwareKey.VolumeMute => VK_VOLUME_MUTE,
            _ => 0
        };

        if (vk == 0) return Task.FromResult(false);

        try
        {
            keybd_event(vk, 0, KEYEVENTF_EXTENDEDKEY, 0);
            keybd_event(vk, 0, KEYEVENTF_EXTENDEDKEY | KEYEVENTF_KEYUP, 0);
            _logger.LogDebug("Hardware media key {Key} (0x{Vk:X2}) sent successfully.", key, vk);
            return Task.FromResult(true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send hardware media key {Key}", key);
            return Task.FromResult(false);
        }
    }

    private async Task EnsureInitializedAsync(CancellationToken cancellationToken)
    {
        if (_isInitialized) return;

        await _initLock.WaitAsync(cancellationToken);
        try
        {
            if (_isInitialized) return;

            try
            {
                _sessionManager = await GlobalSystemMediaTransportControlsSessionManager.RequestAsync();
                if (_sessionManager != null)
                {
                    _sessionManager.CurrentSessionChanged += OnCurrentSessionChanged;
                    _sessionManager.SessionsChanged += OnSessionsChanged;

                    var initialSession = _sessionManager.GetCurrentSession();
                    BindSession(initialSession);
                    _logger.LogInformation("GlobalSystemMediaTransportControlsSessionManager initialized successfully.");
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Could not initialize GlobalSystemMediaTransportControlsSessionManager. Media controls will use hardware keys.");
            }

            _isInitialized = true;
        }
        finally
        {
            _initLock.Release();
        }
    }

    private GlobalSystemMediaTransportControlsSession? ResolveSession(string? sourceOrSessionId)
    {
        if (_sessionManager == null) return null;

        if (string.IsNullOrEmpty(sourceOrSessionId))
        {
            return _currentSession ?? _sessionManager.GetCurrentSession();
        }

        try
        {
            var sessions = _sessionManager.GetSessions();
            return sessions.FirstOrDefault(s =>
                string.Equals(s.SourceAppUserModelId, sourceOrSessionId, StringComparison.OrdinalIgnoreCase))
                ?? _currentSession
                ?? _sessionManager.GetCurrentSession();
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Error resolving specific media session for {Id}", sourceOrSessionId);
            return _currentSession ?? _sessionManager.GetCurrentSession();
        }
    }

    private void BindSession(GlobalSystemMediaTransportControlsSession? session)
    {
        if (ReferenceEquals(_currentSession, session)) return;

        if (_currentSession != null)
        {
            try
            {
                _currentSession.MediaPropertiesChanged -= OnSessionMediaPropertiesChanged;
                _currentSession.PlaybackInfoChanged -= OnSessionPlaybackInfoChanged;
            }
            catch
            {
                // Ignore COM cleanup exceptions
            }
        }

        _currentSession = session;

        if (_currentSession != null)
        {
            try
            {
                _currentSession.MediaPropertiesChanged += OnSessionMediaPropertiesChanged;
                _currentSession.PlaybackInfoChanged += OnSessionPlaybackInfoChanged;

                RefreshCurrentSessionPlaybackInfo(_currentSession);
                _ = RefreshCurrentSessionMetadataAsync(_currentSession);
            }
            catch (Exception ex)
            {
                _logger.LogDebug(ex, "Failed to bind event listeners to current media session.");
            }
        }
        else
        {
            _cachedMetadata = new MediaMetadata();
            _cachedPlaybackInfo = new MediaPlaybackInfo();
            DispatchStateChanged();
        }
    }

    private void OnCurrentSessionChanged(GlobalSystemMediaTransportControlsSessionManager sender, CurrentSessionChangedEventArgs args)
    {
        try
        {
            var newSession = sender.GetCurrentSession();
            BindSession(newSession);
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Exception in OnCurrentSessionChanged");
        }
    }

    private void OnSessionsChanged(GlobalSystemMediaTransportControlsSessionManager sender, SessionsChangedEventArgs args)
    {
        try
        {
            if (_currentSession == null)
            {
                var newSession = sender.GetCurrentSession();
                BindSession(newSession);
            }
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Exception in OnSessionsChanged");
        }
    }

    private void OnSessionMediaPropertiesChanged(GlobalSystemMediaTransportControlsSession sender, MediaPropertiesChangedEventArgs args)
    {
        _ = RefreshCurrentSessionMetadataAsync(sender);
    }

    private void OnSessionPlaybackInfoChanged(GlobalSystemMediaTransportControlsSession sender, PlaybackInfoChangedEventArgs args)
    {
        RefreshCurrentSessionPlaybackInfo(sender);
    }

    private async Task RefreshCurrentSessionMetadataAsync(GlobalSystemMediaTransportControlsSession session)
    {
        try
        {
            var props = await session.TryGetMediaPropertiesAsync();
            if (props != null)
            {
                _cachedMetadata = new MediaMetadata
                {
                    Title = props.Title ?? string.Empty,
                    Artist = props.Artist ?? string.Empty,
                    AlbumTitle = props.AlbumTitle ?? string.Empty,
                    TrackNumber = props.TrackNumber,
                    SourceAppId = session.SourceAppUserModelId ?? string.Empty
                };

                DispatchStateChanged();
            }
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Error refreshing session media properties.");
        }
    }

    private void RefreshCurrentSessionPlaybackInfo(GlobalSystemMediaTransportControlsSession session)
    {
        try
        {
            var playbackInfo = session.GetPlaybackInfo();
            if (playbackInfo != null)
            {
                var controls = playbackInfo.Controls;
                _cachedPlaybackInfo = new MediaPlaybackInfo
                {
                    Status = MapStatus(playbackInfo.PlaybackStatus),
                    CanPlay = controls?.IsPlayEnabled ?? true,
                    CanPause = controls?.IsPauseEnabled ?? true,
                    CanTogglePlayPause = controls?.IsPlayPauseToggleEnabled ?? true,
                    CanSkipNext = controls?.IsNextEnabled ?? true,
                    CanSkipPrevious = controls?.IsPreviousEnabled ?? true,
                    CanStop = controls?.IsStopEnabled ?? true
                };

                DispatchStateChanged();
            }
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Error refreshing session playback info.");
        }
    }

    private void DispatchStateChanged()
    {
        PlaybackStateChanged?.Invoke(this, new MediaPlaybackStateChangedEventArgs(
            _cachedMetadata,
            _cachedPlaybackInfo,
            _currentSession?.SourceAppUserModelId));
    }

    private static MediaPlaybackStatus MapStatus(GlobalSystemMediaTransportControlsSessionPlaybackStatus status) => status switch
    {
        GlobalSystemMediaTransportControlsSessionPlaybackStatus.Closed => MediaPlaybackStatus.Closed,
        GlobalSystemMediaTransportControlsSessionPlaybackStatus.Opened => MediaPlaybackStatus.Opened,
        GlobalSystemMediaTransportControlsSessionPlaybackStatus.Changing => MediaPlaybackStatus.Changing,
        GlobalSystemMediaTransportControlsSessionPlaybackStatus.Stopped => MediaPlaybackStatus.Stopped,
        GlobalSystemMediaTransportControlsSessionPlaybackStatus.Playing => MediaPlaybackStatus.Playing,
        GlobalSystemMediaTransportControlsSessionPlaybackStatus.Paused => MediaPlaybackStatus.Paused,
        _ => MediaPlaybackStatus.Unknown
    };

    public void Dispose()
    {
        if (_isDisposed) return;
        _isDisposed = true;

        if (_currentSession != null)
        {
            try
            {
                _currentSession.MediaPropertiesChanged -= OnSessionMediaPropertiesChanged;
                _currentSession.PlaybackInfoChanged -= OnSessionPlaybackInfoChanged;
            }
            catch
            {
                // Ignore disposal errors
            }
            _currentSession = null;
        }

        if (_sessionManager != null)
        {
            try
            {
                _sessionManager.CurrentSessionChanged -= OnCurrentSessionChanged;
                _sessionManager.SessionsChanged -= OnSessionsChanged;
            }
            catch
            {
                // Ignore disposal errors
            }
            _sessionManager = null;
        }

        _initLock.Dispose();
    }

    #region Win32 P/Invoke Key Simulation

    [DllImport("user32.dll", SetLastError = true)]
    private static extern void keybd_event(byte bVk, byte bScan, uint dwFlags, nuint dwExtraInfo);

    private const byte VK_VOLUME_MUTE = 0xAD;
    private const byte VK_VOLUME_DOWN = 0xAE;
    private const byte VK_VOLUME_UP = 0xAF;
    private const byte VK_MEDIA_NEXT_TRACK = 0xB0;
    private const byte VK_MEDIA_PREV_TRACK = 0xB1;
    private const byte VK_MEDIA_STOP = 0xB2;
    private const byte VK_MEDIA_PLAY_PAUSE = 0xB3;

    private const uint KEYEVENTF_EXTENDEDKEY = 0x0001;
    private const uint KEYEVENTF_KEYUP = 0x0002;

    #endregion
}
