using System.ComponentModel;
using System.Runtime.CompilerServices;
using AudioHub.Core.Interfaces;
using AudioHub.Core.Models;

namespace AudioHub_App.ViewModels;

/// <summary>
/// ViewModel representing an individual active audio stream channel strip in the multi-device hub mixer.
/// </summary>
public sealed class RouteChannelViewModel : INotifyPropertyChanged
{
    private readonly IAudioRoutingService _audioRoutingService;
    private readonly IAudioMixerService _audioMixerService;
    private readonly Func<string, Task> _stopCallback;

    private float _gain;
    private float _pan;
    private bool _isMuted;
    private bool _isSolo;
    private float _peakLeft;
    private float _peakRight;
    private AudioPlaybackStreamState _streamState;

    public string RouteId { get; }
    public string DeviceId { get; }
    public string DisplayName { get; }
    public string OutputName { get; }

    public float RouteGain
    {
        get => _gain;
        set
        {
            if (SetField(ref _gain, Math.Clamp(value, 0.0f, 1.0f)))
            {
                _audioRoutingService.SetRouteGain(RouteId, _gain);
                _audioMixerService.SetChannelGain(RouteId, _gain);
                OnPropertyChanged(nameof(FormattedVolume));
            }
        }
    }

    public float Pan
    {
        get => _pan;
        set
        {
            if (SetField(ref _pan, Math.Clamp(value, -1.0f, 1.0f)))
            {
                _audioMixerService.SetChannelPan(RouteId, _pan);
                OnPropertyChanged(nameof(FormattedPan));
            }
        }
    }

    public bool IsMuted
    {
        get => _isMuted;
        set
        {
            if (SetField(ref _isMuted, value))
            {
                _audioRoutingService.SetRouteMute(RouteId, _isMuted);
                _audioMixerService.SetChannelMute(RouteId, _isMuted);
                OnPropertyChanged(nameof(MuteButtonText));
            }
        }
    }

    public bool IsSolo
    {
        get => _isSolo;
        set
        {
            if (SetField(ref _isSolo, value))
            {
                _audioMixerService.SetChannelSolo(RouteId, _isSolo);
                OnPropertyChanged(nameof(SoloButtonText));
            }
        }
    }

    public float PeakLeft
    {
        get => _peakLeft;
        set => SetField(ref _peakLeft, Math.Clamp(value, 0.0f, 1.0f));
    }

    public float PeakRight
    {
        get => _peakRight;
        set => SetField(ref _peakRight, Math.Clamp(value, 0.0f, 1.0f));
    }

    public AudioPlaybackStreamState StreamState
    {
        get => _streamState;
        set
        {
            if (SetField(ref _streamState, value))
            {
                OnPropertyChanged(nameof(IsStreaming));
            }
        }
    }

    public bool IsStreaming => _streamState == AudioPlaybackStreamState.Streaming;

    public string FormattedVolume => $"{_gain * 100:0}%";

    public string FormattedPan => _pan switch
    {
        < -0.05f => $"L {MathF.Abs(_pan) * 100:0}%",
        > 0.05f => $"R {_pan * 100:0}%",
        _ => "C"
    };

    public string MuteButtonText => _isMuted ? "Unmute" : "Mute";

    public string SoloButtonText => _isSolo ? "Solo On" : "Solo";

    public event PropertyChangedEventHandler? PropertyChanged;

    public RouteChannelViewModel(
        AudioRoute route,
        AudioPlaybackStreamState initialState,
        IAudioRoutingService audioRoutingService,
        IAudioMixerService audioMixerService,
        Func<string, Task> stopCallback)
    {
        ArgumentNullException.ThrowIfNull(route);
        _audioRoutingService = audioRoutingService ?? throw new ArgumentNullException(nameof(audioRoutingService));
        _audioMixerService = audioMixerService ?? throw new ArgumentNullException(nameof(audioMixerService));
        _stopCallback = stopCallback ?? throw new ArgumentNullException(nameof(stopCallback));

        RouteId = route.Id;
        DeviceId = route.Source.Id;
        DisplayName = route.Source.DisplayName;
        OutputName = route.Output.DisplayName;
        _gain = route.RouteGain;
        _isMuted = route.Source.IsMuted;
        _streamState = initialState;

        // Register channel in mixer
        _audioMixerService.AddOrUpdateChannel(RouteId, DisplayName, _gain, _pan);
    }

    public void UpdateState(AudioPlaybackStreamState state)
    {
        StreamState = state;
    }

    public void UpdateRoute(AudioRoute route)
    {
        ArgumentNullException.ThrowIfNull(route);
        _gain = route.RouteGain;
        _isMuted = route.Source.IsMuted;
        _audioMixerService.SetChannelGain(RouteId, _gain);
        _audioMixerService.SetChannelMute(RouteId, _isMuted);

        OnPropertyChanged(nameof(RouteGain));
        OnPropertyChanged(nameof(FormattedVolume));
        OnPropertyChanged(nameof(IsMuted));
        OnPropertyChanged(nameof(MuteButtonText));
    }

    public void UpdateLevels(float peakLeft, float peakRight)
    {
        PeakLeft = peakLeft;
        PeakRight = peakRight;
    }

    public void ToggleMute()
    {
        IsMuted = !IsMuted;
    }

    public void ToggleSolo()
    {
        IsSolo = !IsSolo;
    }

    public Task StopAsync()
    {
        _audioMixerService.RemoveChannel(RouteId);
        return _stopCallback(DeviceId);
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
}
