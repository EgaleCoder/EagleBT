using AudioHub.Core.Models;
using AudioHub_App.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace AudioHub_App;

/// <summary>
/// Diagnostic console and audio routing console for EagleBT Phase 2.
/// </summary>
public sealed partial class MainPage : Page
{
    public MainViewModel ViewModel { get; }

    public MainPage()
    {
        InitializeComponent();

        var app = (App)Application.Current;
        ViewModel = app.Services.GetRequiredService<MainViewModel>();
    }

    private async void Page_Loaded(object sender, RoutedEventArgs e)
    {
        await ViewModel.InitializeAsync();
    }

    private async void RefreshAudioButton_Click(object sender, RoutedEventArgs e)
    {
        await ViewModel.RefreshAudioDevicesAsync();
    }

    private async void StreamButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button { Tag: BluetoothDevice device })
        {
            await ViewModel.StartStreamingAsync(device);
        }
    }

    private async void StopStreamButton_Click(object sender, RoutedEventArgs e)
    {
        await ViewModel.StopStreamingAsync();
    }

    private void MuteButton_Click(object sender, RoutedEventArgs e)
    {
        ViewModel.ToggleMute();
    }

    public static Visibility BoolToVisibility(bool value) =>
        value ? Visibility.Visible : Visibility.Collapsed;

    public static string FormatVolume(float volume) =>
        $"{volume * 100:0}%";

    public static string MuteText(bool isMuted) =>
        isMuted ? "Unmute" : "Mute";
}
