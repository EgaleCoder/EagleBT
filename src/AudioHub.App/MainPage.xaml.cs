using AudioHub.Core.Models;
using AudioHub_App.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace AudioHub_App;

/// <summary>
/// Diagnostic console and multi-device audio routing hub for EagleBT Phase 3.
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

    private void ChannelMuteButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button { Tag: RouteChannelViewModel channel })
        {
            channel.ToggleMute();
        }
    }

    private async void StopChannelButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button { Tag: RouteChannelViewModel channel })
        {
            await channel.StopAsync();
        }
    }

    private async void StopAllStreamsButton_Click(object sender, RoutedEventArgs e)
    {
        await ViewModel.StopAllStreamsAsync();
    }

    public static Visibility BoolToVisibility(bool value) =>
        value ? Visibility.Visible : Visibility.Collapsed;

    public static Visibility InvertBoolToVisibility(bool value) =>
        value ? Visibility.Collapsed : Visibility.Visible;

    public static string FormatVolume(float volume) =>
        $"{volume * 100:0}%";

    public static string MuteText(bool isMuted) =>
        isMuted ? "Unmute" : "Mute";
}
