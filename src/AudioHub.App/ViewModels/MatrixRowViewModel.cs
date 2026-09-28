using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using AudioHub.Core.Models;

namespace AudioHub_App.ViewModels;

/// <summary>
/// Represents a row in the audio routing cross-point matrix, corresponding to an individual audio source.
/// </summary>
public sealed class MatrixRowViewModel : INotifyPropertyChanged
{
    private bool _isStreaming;
    private int _activeRoutesCount;

    public string SourceId { get; }
    public string SourceName { get; }
    public SourceType SourceType { get; }
    public bool IsBluetooth => SourceType == SourceType.RemoteMedia || SourceType == SourceType.RemoteCall;

    public bool IsStreaming
    {
        get => _isStreaming;
        set
        {
            if (SetField(ref _isStreaming, value))
            {
                OnPropertyChanged(nameof(StatusText));
                OnPropertyChanged(nameof(IsActiveOrStreaming));
            }
        }
    }

    public int ActiveRoutesCount
    {
        get => _activeRoutesCount;
        set
        {
            if (SetField(ref _activeRoutesCount, value))
            {
                OnPropertyChanged(nameof(StatusText));
                OnPropertyChanged(nameof(IsActiveOrStreaming));
            }
        }
    }

    public bool IsActiveOrStreaming => _isStreaming || _activeRoutesCount > 0;

    public string StatusText => _isStreaming
        ? $"Streaming ({_activeRoutesCount} routes)"
        : (_activeRoutesCount > 0 ? $"{_activeRoutesCount} active routes" : "Idle");

    public string SourceGlyph => SourceType switch
    {
        SourceType.RemoteMedia => "\uE702",   // Bluetooth Phone
        SourceType.RemoteCall => "\uE717",    // Call / Headset
        SourceType.SystemAudio => "\uE767",   // Volume / Speaker
        SourceType.LocalApplication => "\uE71D", // App
        _ => "\uE8D6"                         // Audio device
    };

    public ObservableCollection<MatrixCellViewModel> Cells { get; } = new();

    public event PropertyChangedEventHandler? PropertyChanged;

    public MatrixRowViewModel(string sourceId, string sourceName, SourceType sourceType)
    {
        SourceId = sourceId ?? throw new ArgumentNullException(nameof(sourceId));
        SourceName = sourceName ?? throw new ArgumentNullException(nameof(sourceName));
        SourceType = sourceType;
    }

    public void RecalculateActiveRoutes()
    {
        int count = 0;
        foreach (var cell in Cells)
        {
            if (cell.IsConnected) count++;
        }
        ActiveRoutesCount = count;
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
