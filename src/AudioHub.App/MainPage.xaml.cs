using AudioHub.Core.Models;
using AudioHub_App.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace AudioHub_App;

/// <summary>
/// Diagnostic console and real-time audio mixer hub for EagleBT Phase 4.
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

    private void MasterMuteButton_Click(object sender, RoutedEventArgs e)
    {
        ViewModel.ToggleMasterMute();
    }

    private void ChannelMuteButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button { Tag: RouteChannelViewModel channel })
        {
            channel.ToggleMute();
        }
    }

    private void ChannelSoloButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button { Tag: RouteChannelViewModel channel })
        {
            channel.ToggleSolo();
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

    private void MixerViewNavButton_Click(object sender, RoutedEventArgs e)
    {
        ViewModel.SetViewIndex(0);
    }

    private void MatrixViewNavButton_Click(object sender, RoutedEventArgs e)
    {
        ViewModel.SetViewIndex(1);
    }

    private void DevicesViewNavButton_Click(object sender, RoutedEventArgs e)
    {
        ViewModel.SetViewIndex(2);
    }

    private async void MatrixCellButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button { Tag: MatrixCellViewModel cell })
        {
            await ViewModel.ToggleMatrixCrossPointAsync(cell);
        }
    }

    private void RefreshMatrixButton_Click(object sender, RoutedEventArgs e)
    {
        ViewModel.RefreshMatrix();
    }

    private async void PlayPauseButton_Click(object sender, RoutedEventArgs e)
    {
        await ViewModel.TogglePlayPauseAsync();
    }

    private async void SkipPreviousButton_Click(object sender, RoutedEventArgs e)
    {
        await ViewModel.SkipPreviousAsync();
    }

    private async void SkipNextButton_Click(object sender, RoutedEventArgs e)
    {
        await ViewModel.SkipNextAsync();
    }

    private async void StopMediaButton_Click(object sender, RoutedEventArgs e)
    {
        await ViewModel.StopMediaAsync();
    }

    private async void VolumeUpButton_Click(object sender, RoutedEventArgs e)
    {
        await ViewModel.VolumeUpAsync();
    }

    private async void VolumeDownButton_Click(object sender, RoutedEventArgs e)
    {
        await ViewModel.VolumeDownAsync();
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
