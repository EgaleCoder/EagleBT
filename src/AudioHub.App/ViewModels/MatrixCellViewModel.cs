using System.ComponentModel;
using System.Runtime.CompilerServices;
using AudioHub.Core.Models;

namespace AudioHub_App.ViewModels;

/// <summary>
/// Represents an interactive cross-point cell in the EagleBT Audio Hub routing matrix,
/// connecting a specific audio source (row) to an audio output destination (column).
/// </summary>
public sealed class MatrixCellViewModel : INotifyPropertyChanged
{
    private bool _isConnected;
    private bool _isConnecting;
    private string? _routeId;

    public string SourceId { get; }
    public string SourceName { get; }
    public SourceType SourceType { get; }
    public string OutputId { get; }
    public string OutputName { get; }
    public bool IsOutputBluetooth { get; }
    public bool IsOutputDefault { get; }

    public bool IsConnected
    {
        get => _isConnected;
        private set
        {
            if (SetField(ref _isConnected, value))
            {
                OnPropertyChanged(nameof(StatusBadgeText));
                OnPropertyChanged(nameof(ConnectionGlyph));
                OnPropertyChanged(nameof(CrossPointToolTip));
            }
        }
    }

    public bool IsConnecting
    {
        get => _isConnecting;
        set => SetField(ref _isConnecting, value);
    }

    public string? RouteId
    {
        get => _routeId;
        private set => SetField(ref _routeId, value);
    }

    public string StatusBadgeText => _isConnected ? "ACTIVE" : "ROUTE";

    public string ConnectionGlyph => _isConnected ? "\uE73E" : "\uE710"; // Checkmark vs Add / Route

    public string CrossPointToolTip => _isConnected
        ? $"Connected: {SourceName} \u2794 {OutputName} (Click to Disconnect)"
        : $"Route {SourceName} \u2794 {OutputName} (Click to Connect)";

    public event PropertyChangedEventHandler? PropertyChanged;

    public MatrixCellViewModel(
        string sourceId,
        string sourceName,
        SourceType sourceType,
        string outputId,
        string outputName,
        bool isOutputBluetooth,
        bool isOutputDefault,
        bool isConnected = false,
        string? routeId = null)
    {
        SourceId = sourceId ?? throw new ArgumentNullException(nameof(sourceId));
        SourceName = sourceName ?? throw new ArgumentNullException(nameof(sourceName));
        SourceType = sourceType;
        OutputId = outputId ?? throw new ArgumentNullException(nameof(outputId));
        OutputName = outputName ?? throw new ArgumentNullException(nameof(outputName));
        IsOutputBluetooth = isOutputBluetooth;
        IsOutputDefault = isOutputDefault;
        _isConnected = isConnected;
        _routeId = routeId;
    }

    public void SetConnected(bool isConnected, string? routeId = null)
    {
        _routeId = routeId;
        IsConnected = isConnected;
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
