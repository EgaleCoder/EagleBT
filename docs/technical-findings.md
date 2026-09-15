# Technical Findings & Spike Log

## 1. Environment Baseline

- **Operating System**: Microsoft Windows 10 Pro (Build 19045, 64-bit).
- **Windows Architecture**: x64.
- **Target .NET Version**: .NET 8.0 LTS (`net8.0`, `net8.0-windows10.0.19041.0`, `net8.0-windows10.0.22621.0`).
- **Target Hardware**: Mustang GoBoult Torq Bluetooth Earbuds.

---

## 2. Windows Bluetooth API Observations

- **A2DP Sink**: Supported natively on Windows 10 Build 19041+ and Windows 11 via `Windows.Media.Audio.AudioPlaybackConnection`.
  - Calling `AudioPlaybackConnection.TryCreateFromId(deviceId)` instantiates an audio receiver channel for a paired Bluetooth A2DP source (e.g., smartphone).
  - Calling `connection.Start()` enables audio reception, and `connection.OpenAsync()` initiates real-time audio playback routing.
  - Connection lifecycle events are notified via `connection.StateChanged` (`Opened`, `Closed`).
- **Classic Bluetooth Enumeration**: Operates via `Windows.Devices.Enumeration.DeviceWatcher` using AQS pairing filter:
  ```csharp
  BluetoothDevice.GetDeviceSelectorFromPairingState(true);
  ```
- **Connection State Notification**: `DeviceInformation.Update` events provide real-time status updates without aggressive polling.

---

## 3. Windows Audio (WASAPI) Observations

- **Endpoints**: Distinct enumeration for `eRender` (playback) and `eCapture` (microphones/line-in).
- **Bluetooth Endpoints**: Bluetooth audio devices appear as standard WASAPI endpoints once paired and connected in Windows Settings.
- **Endpoint Selection**: Render devices can be programmatically targeted; Mustang GoBoult Torq is detected by friendly name matching and device interface properties.
- **Sample Rate & Bit Depth**: Most Bluetooth A2DP audio endpoints operate at 44.1 kHz or 48 kHz stereo (16-bit or 24-bit).

---

## 4. Technical Spike Questions & Status

| # | Question | Current Status | Finding |
| :- | :--- | :--- | :--- |
| 1 | Can Windows discover the phone? | Validated (Phase 1) | DeviceWatcher detects paired smartphones via Bluetooth Classic AQS. |
| 2 | Can Windows discover the tablet? | Validated (Phase 1) | DeviceWatcher detects paired tablets via Bluetooth Classic AQS. |
| 3 | Can Windows discover Mustang GoBoult Torq? | Validated (Phase 1) | Detected as active WASAPI render endpoint with Bluetooth flag. |
| 4 | Can phone audio route to Windows PC via A2DP Sink? | Validated (Phase 2) | Supported via `Windows.Media.Audio.AudioPlaybackConnection`. |
| 5 | What is the latency over the full routing path? | Phase 2 Baseline | Windows A2DP sink introduces ~120–180ms buffer latency, acceptable for music/media. |
| 6 | Can active route gain and mute be controlled in software? | Validated (Phase 2) | Implemented via `IAudioRoutingService` route gain scaling. |
