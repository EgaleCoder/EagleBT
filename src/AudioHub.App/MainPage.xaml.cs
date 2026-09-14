using AudioHub_App.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace AudioHub_App;

/// <summary>
/// Diagnostic console for Phase 1 Bluetooth and Audio discovery.
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
}
