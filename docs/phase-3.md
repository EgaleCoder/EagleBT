# Phase 3: Multiple Remote Devices Audio Stream

## 1. Goal

Phase 3 advances the EagleBT audio routing pipeline from a single-device connection to concurrent multi-device streaming:
```text
Mobile Phone (A2DP) ──┐
                      ├──► Windows PC (EagleBT Multi-Sink) ──► Mustang GoBoult Torq (WASAPI Render)
Tablet (A2DP) ────────┘
```
The goal is to allow a user to stream audio concurrently from multiple paired remote Bluetooth devices (such as a smartphone and a tablet) into Windows, manage independent stream channels with per-route volume and mute controls, and route the combined playback into their target Bluetooth earbuds (**Mustang GoBoult Torq**) or selected audio render endpoint.

---

## 2. Phase 3 Deliverables

### Core Domain Abstractions (`AudioHub.Core`)
- **`IAudioRoutingService` & `AudioRoutingService`**:
  - Expanded to support concurrent active routes via `ActiveRoutes` (`IReadOnlyCollection<AudioRoute>`).
  - Added query methods: `GetRoute(routeId)` and `GetRouteBySourceId(sourceId)`.
  - Added global route teardown: `DisconnectAllRoutesAsync()`.
  - Upgraded `ConnectRouteAsync` to maintain concurrent active streams from distinct sources, while seamlessly updating or replacing reconnected routes from the same source.
  - Per-route volume gain (`SetRouteGain`) and mute (`SetRouteMute`) applied independently to target routes.
- **`IBluetoothAudioSinkService`**:
  - Added `StopAllStreamsAsync()` for simultaneous shutdown of all active sink connections.
  - Added `IsDeviceStreaming(deviceId)` to query real-time streaming status per device.

### Windows Platform Implementation (`AudioHub.Windows`)
- **`WindowsBluetoothAudioSinkService`**:
  - Implemented `StopAllStreamsAsync` leveraging thread-safe `ConcurrentDictionary` connection pools.
  - Implemented `IsDeviceStreaming` querying active connection states.
  - Preserved multi-connection WinRT `AudioPlaybackConnection` lifecycle with per-device state isolation.

### Presentation Layer (`AudioHub.App`)
- **`RouteChannelViewModel`**:
  - Dedicated MVVM view model representing an individual active stream channel strip.
  - Exposes `DisplayName`, `OutputName`, `RouteGain`, `IsMuted`, and `FormattedVolume`.
  - Handles independent mute toggle and individual channel termination (`StopAsync`).
- **`MainViewModel`**:
  - Exposes `ObservableCollection<RouteChannelViewModel> ActiveChannels`.
  - Dynamically computes `HasActiveChannels` and `ActiveStreamsCount`.
  - Supports non-destructive concurrent streaming: starting a second device stream does not displace earlier active streams.
  - Added `StopStreamingAsync(deviceId)` and `StopAllStreamsAsync()` methods.
  - Generates multi-stream status messages summarizing all actively streaming sources.
- **`MainPage.xaml` & `MainPage.xaml.cs`**:
  - Modernized **Active Stream Router** card with an `ItemsControl` displaying individual channel strips for all active streams.
  - Added "Stop All Streams" button when streams are active.
  - Each channel strip includes an independent volume slider, mute toggle button, and "Stop Stream" button.

### Automated Test Suite (`tests/AudioHub.Core.Tests`)
- **`AudioRoutingServiceTests`**:
  - Validates concurrent multiple routes running simultaneously.
  - Validates independent volume gain adjustments per route.
  - Validates independent route mute toggling without cross-talk.
  - Validates selective route disconnection leaving remaining streams intact.
  - Validates `DisconnectAllRoutesAsync` global teardown and event dispatches.
  - Validates route replacement when reconnecting the same source.
  - Validates source ID lookup via `GetRouteBySourceId`.

---

## 3. Exit Criteria

Phase 3 is considered complete when:
1. Domain architecture supports multiple concurrent `AudioRoute` instances routed into a common output.
2. `IBluetoothAudioSinkService` and platform implementations support concurrent streams and batch teardown.
3. WinUI 3 interface provides multi-channel controls with independent volume, mute, and stream management.
4. Automated unit test suite verifies concurrent routing, independent gain/mute scaling, and lifecycle teardown.
