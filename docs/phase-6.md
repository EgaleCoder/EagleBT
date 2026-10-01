# Phase 6: AVRCP Media Control Integration (Play/Pause/Track Skip & Metadata)

## 1. Goal

Phase 6 incorporates two-way Bluetooth AVRCP (Audio/Video Remote Control Profile) media controls and system media session tracking into the EagleBT audio routing pipeline:

```text
                               ┌──────────────────────────────────────────────────────────┐
                               │             EAGLEBT AVRCP TRANSPORT (PHASE 6)            │
                               ├──────────────────────────────────────────────────────────┤
                               │ Now Playing: Artist — Track Title [Playing / Paused]     │
                               │ Controls: [⏮ Prev]  [⏯ Play/Pause]  [⏭ Next]  [⏹ Stop]    │
                               │ Volume Sync: [Vol -] [Vol +]                             │
                               └──────────────────────────┬───────────────────────────────┘
                                                          │
                               ┌──────────────────────────▼───────────────────────────────┐
                               │        WinRT GlobalSystemMediaTransportControls          │
                               │       + Win32 Hardware Media Keystroke Emulation         │
                               └──────────────────────────┬───────────────────────────────┘
                                                          │
                     ┌────────────────────────────────────┴────────────────────────────────────┐
                     ▼                                                                         ▼
┌─────────────────────────────────────────┐                               ┌─────────────────────────────────────────┐
│       Remote Device (Phone/Tablet)      │                               │         Local PC Media Sessions         │
│  Play/Pause/Skip across paired sources  │                               │   Spotify, Web Media, System Players    │
└─────────────────────────────────────────┘                               └─────────────────────────────────────────┘
```

Phase 6 provides complete remote media transport coordination:
1. **Playback Controls**: Play, Pause, Toggle Play/Pause, Skip Next, Skip Previous, Stop.
2. **Metadata & Status Tracking**: Live display of current Track Title, Artist, Album, and Playback Status (`Playing`, `Paused`, `Stopped`, `Changing`).
3. **Hardware & Protocol Resiliency**: Dual-layer architecture utilizing Windows Runtime `GlobalSystemMediaTransportControlsSessionManager` with native Win32 hardware media key emulation fallback.
4. **Volume Syncing**: Stepwise volume adjustment commands synchronized across remote playback routes and local hardware endpoints.

---

## 2. Phase 6 Deliverables

### Core Domain Abstractions (`AudioHub.Core`)
- **`MediaPlaybackStatus`**:
  - Strongly-typed status enumeration (`Unknown`, `Closed`, `Opened`, `Changing`, `Stopped`, `Playing`, `Paused`).
- **`MediaMetadata`**:
  - Immutable domain record encapsulating `Title`, `Artist`, `AlbumTitle`, `TrackNumber`, `SourceAppId`, `HasMetadata`, and `DisplayText`.
- **`MediaPlaybackInfo`**:
  - Immutable domain record encapsulating `Status`, `IsPlaying`, and capability flags (`CanPlay`, `CanPause`, `CanTogglePlayPause`, `CanSkipNext`, `CanSkipPrevious`, `CanStop`).
- **`MediaHardwareKey`**:
  - Enumeration for hardware transport keystrokes (`PlayPause`, `NextTrack`, `PreviousTrack`, `Stop`, `VolumeUp`, `VolumeDown`, `VolumeMute`).
- **`MediaPlaybackStateChangedEventArgs`**:
  - Event payload containing updated metadata, playback info, and optional source or session identifier.
- **`IMediaControlService`**:
  - Service abstraction for querying current track metadata, playback status, dispatching transport commands, and broadcasting real-time playback state transitions.

### Windows Platform Implementation (`AudioHub.Windows`)
- **`WindowsMediaControlService`**:
  - Implements `IMediaControlService` using `Windows.Media.Control.GlobalSystemMediaTransportControlsSessionManager`.
  - Subscribes to `CurrentSessionChanged` and `SessionsChanged` events.
  - Dynamically binds to active sessions and hooks `MediaPropertiesChanged` and `PlaybackInfoChanged` events.
  - Asynchronously retrieves track metadata (`TryGetMediaPropertiesAsync`) and transport info (`GetPlaybackInfo`).
  - Dispatches commands (`TryPlayAsync`, `TryPauseAsync`, `TryTogglePlayPauseAsync`, `TrySkipNextAsync`, `TrySkipPreviousAsync`, `TryStopAsync`).
  - Implements native Win32 `keybd_event` P/Invoke fallback for hardware media transport keys (`VK_MEDIA_PLAY_PAUSE`, `VK_MEDIA_NEXT_TRACK`, `VK_MEDIA_PREV_TRACK`, `VK_MEDIA_STOP`, `VK_VOLUME_UP`, `VK_VOLUME_DOWN`, `VK_VOLUME_MUTE`), ensuring universal command dispatch even when an external player lacks SMTC registration.
  - Safe, thread-safe synchronization with `SemaphoreSlim` and graceful COM exception handling.

### Dependency Injection Wiring (`AudioHub.Infrastructure`)
- Registered `IMediaControlService` $\rightarrow$ `WindowsMediaControlService` as a Singleton in `ServiceCollectionExtensions.cs`.

### Presentation Layer (`AudioHub.App`)
- **`MainViewModel`**:
  - Injected `IMediaControlService`.
  - Exposes `MediaMetadata`, `PlaybackInfo`, `MediaTitle`, `MediaArtist`, `MediaAlbum`, `MediaDisplayText`, `MediaStatusText`, `IsMediaPlaying`, `PlayPauseGlyph`, `PlayPauseButtonText`, and capability bindings.
  - Dispatches transport actions: `TogglePlayPauseAsync()`, `SkipNextAsync()`, `SkipPreviousAsync()`, `StopMediaAsync()`, `VolumeUpAsync()`, `VolumeDownAsync()`.
  - Dispatches media events safely to the WinUI UI thread via `DispatcherQueue`.
- **`MainPage.xaml`**:
  - Subtitle updated to *"Phase 6: AVRCP Media Control & Interactive Audio Routing Hub"*.
  - Added dedicated **AVRCP Media Transport & Now Playing Bar** in the persistent header:
    - Live track title and artist display with marquee text trimming.
    - Media status badge with dynamic visual styling.
    - Previous Track, Play/Pause toggle (with dynamic Segoe Fluent icon swap: `&#xE768;` for Play vs `&#xE769;` for Pause), Next Track, and Stop buttons.
    - Hardware volume increment/decrement sync buttons.
- **`MainPage.xaml.cs`**:
  - Wired event handlers for `PlayPauseButton_Click`, `SkipPreviousButton_Click`, `SkipNextButton_Click`, `StopMediaButton_Click`, `VolumeUpButton_Click`, and `VolumeDownButton_Click`.

### Automated Unit Test Suite (`tests/AudioHub.Core.Tests`)
- **`MediaControlTests`**:
  - Validates `MediaMetadata` default initialization, text formatting combinations (Title only, Artist only, Title + Artist), and empty metadata handling.
  - Validates `MediaPlaybackInfo` status transitions and `IsPlaying` evaluations across all states (`Playing`, `Paused`, `Stopped`, `Closed`, `Changing`).
  - Validates `MediaPlaybackStateChangedEventArgs` payload integrity.

---

## 3. Exit Criteria

Phase 6 is considered complete when:
1. Core domain abstractions define `IMediaControlService`, `MediaMetadata`, `MediaPlaybackInfo`, and `MediaPlaybackStatus`.
2. Windows platform service implements WinRT GSMTC media session monitoring and command dispatch with Win32 hardware key fallback.
3. Service is registered in dependency injection (`AddAudioHubServices`).
4. WinUI 3 dashboard provides persistent Now Playing telemetry and interactive media transport buttons (Play/Pause, Prev, Next, Stop, Volume Sync).
5. Unit test suite validates media domain models and state representations.
