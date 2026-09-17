using System.ComponentModel;
using System.Runtime.CompilerServices;
using AudioHub.Core.Interfaces;
using AudioHub.Core.Models;

namespace AudioHub_App.ViewModels;

/// <summary>
/// ViewModel representing an individual active audio stream channel strip in the multi-device hub.
/// </summary>
public sealed class RouteChannelViewModel : INotifyPropertyChanged
{
    private readonly IAudioRoutingService _audioRoutingService;
    private readonly Func<string, Task> _stopCallback;

    private float _gain;
    private bool _isMuted;
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
                OnPropertyChanged(nameof(FormattedVolume));
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
                OnPropertyChanged(nameof(MuteButtonText));
            }
        }
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

    public string MuteButtonText => _isMuted ? "Unmute" : "Mute";

    public event PropertyChangedEventHandler? PropertyChanged;

    public RouteChannelViewModel(
        AudioRoute route,
        AudioPlaybackStreamState initialState,
        IAudioRoutingService audioRoutingService,
        Func<string, Task> stopCallback)
    {
        ArgumentNullException.ThrowIfNull(route);
        _audioRoutingService = audioRoutingService ?? throw new ArgumentNullException(nameof(audioRoutingService));
        _stopCallback = stopCallback ?? throw new ArgumentNullException(nameof(stopCallback));

        RouteId = route.Id;
        DeviceId = route.Source.Id;
        DisplayName = route.Source.DisplayName;
        OutputName = route.Output.DisplayName;
        _gain = route.RouteGain;
        _isMuted = route.Source.IsMuted;
        _streamState = initialState;
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
        OnPropertyChanged(nameof(RouteGain));
        OnPropertyChanged(nameof(FormattedVolume));
        OnPropertyChanged(nameof(IsMuted));
        OnPropertyChanged(nameof(MuteButtonText));
    }

    public void ToggleMute()
    {
        IsMuted = !IsMuted;
    }

    public Task StopAsync()
    {
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
