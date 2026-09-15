# Phase 2: Single Remote Device Audio Stream

## 1. Goal

Phase 2 establishes the fundamental audio routing pipeline for EagleBT:
```text
Remote Device (Phone/Tablet) ──► Windows PC (EagleBT A2DP Sink) ──► Mustang GoBoult Torq (WASAPI Render)
```
The goal is to allow a user to stream audio from a single paired mobile device (smartphone or tablet) into their Windows PC, and hear it routed into their Bluetooth earbuds (**Mustang GoBoult Torq**) or selected audio render endpoint.

---

## 2. Phase 2 Deliverables

### Core Domain Abstractions (`AudioHub.Core`)
- **`IBluetoothAudioSinkService`**:
  - Defines stream lifecycle methods: `StartStreamAsync(deviceId)` and `StopStreamAsync(deviceId)`.
  - Exposes `AudioPlaybackStreamState` (`Closed`, `Opening`, `Streaming`, `Failed`).
  - Dispatches `StreamStateChanged` events.
- **`IAudioRoutingService` & `AudioRoutingService`**:
  - Coordinates active domain `AudioRoute` instances connecting an `AudioSource` to an `AudioOutput`.
  - Supports route establishment, teardown, gain scaling (`RouteGain`), and muting.
  - Fully decoupled domain logic with zero UI or OS dependencies.

### Windows Platform Implementation (`AudioHub.Windows`)
- **`WindowsBluetoothAudioSinkService`**:
  - Implements `IBluetoothAudioSinkService` using the native WinRT `Windows.Media.Audio.AudioPlaybackConnection` API (available on Windows 10 Build 19041+ and Windows 11).
  - Instantiates connections via `AudioPlaybackConnection.TryCreateFromId(deviceId)`.
  - Enables incoming audio reception via `Start()` and opens the stream via `OpenAsync()`.
  - Monitors connection state transitions (`Opened`, `Closed`) via the `StateChanged` event.

### Dependency Injection Wiring (`AudioHub.Infrastructure`)
- Registers `IAudioRoutingService` -> `AudioRoutingService` (Singleton).
- Registers `IBluetoothAudioSinkService` -> `WindowsBluetoothAudioSinkService` (Singleton).

### Presentation Layer (`AudioHub.App`)
- **`MainViewModel`**:
  - Tracks `ActiveStreamDevice`, `SelectedOutputDevice`, and `ActiveRoute`.
  - Provides `StartStreamingAsync` and `StopStreamingAsync` workflows.
  - Automatically identifies and prefers the **Mustang GoBoult Torq** Bluetooth endpoint for render output.
  - Exposes route volume (`RouteGain`), mute state, and real-time streaming status.
- **`MainPage.xaml`**:
  - Dedicated **Active Audio Route & Stream Controller** card at the top.
  - Direct "Stream Audio" action buttons on paired Bluetooth devices in the device list.
  - Output device selector with Bluetooth endpoint badge indicators.
  - Interactive volume slider and mute toggle for the active route.

### Automated Test Suite (`tests/AudioHub.Core.Tests`)
- **`AudioRoutingServiceTests`**:
  - Validates route establishment and event notifications.
  - Validates route deactivation and cleanup.
  - Validates volume gain clamping ([0.0, 1.0]).
  - Validates source mute handling.
  - Validates graceful replacement of existing active routes.

---

## 3. Exit Criteria

Phase 2 is considered complete when:
1. Solution compiles cleanly with zero warnings or errors.
2. Automated unit test suite passes 100% (10/10 tests).
3. `IBluetoothAudioSinkService` connects remote A2DP streams via `Windows.Media.Audio.AudioPlaybackConnection`.
4. Domain audio routing logic handles route lifecycle, volume gain, and muting.
5. WinUI 3 UI provides clear streaming controls, output selection, and status feedback.
