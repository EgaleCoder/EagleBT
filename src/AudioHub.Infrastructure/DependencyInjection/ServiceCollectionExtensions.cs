using AudioHub.Core.Interfaces;
using AudioHub.Windows.Audio;
using AudioHub.Windows.Bluetooth;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace AudioHub.Infrastructure.DependencyInjection;

public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers all core AudioHub services, Windows Bluetooth, and WASAPI implementations.
    /// </summary>
    public static IServiceCollection AddAudioHubServices(this IServiceCollection services)
    {
        // Add structured logging
        services.AddLogging(builder =>
        {
            builder.AddConsole();
            builder.SetMinimumLevel(LogLevel.Debug);
        });

        // Register Windows Platform Subsystem Services
        services.AddSingleton<IBluetoothDeviceService, WindowsBluetoothDeviceService>();
        services.AddSingleton<IAudioDeviceService, WindowsAudioDeviceService>();

        return services;
    }
}
