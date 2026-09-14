# Technical Findings & Spike Log

## 1. Environment Baseline

- **Operating System**: Microsoft Windows 10 Pro (Build 19045, 64-bit).
- **Windows Architecture**: x64.
- **Target .NET Version**: .NET 8.0 LTS (`net8.0` and `net8.0-windows10.0.19041.0`).
- **Target Hardware**: Mustang GoBoult Torq Bluetooth Earbuds.

---

## 2. Windows Bluetooth API Observations

- **A2DP Sink**: Supported natively on Windows 10 Build 19041+ via `Windows.Media.Audio.AudioPlaybackConnection`.
- **Classic Bluetooth Enumeration**: Requires pairing or discovery using AQS device selectors:
  ```text
  System.Devices.AConnection.Bluetooth.DeviceAddress != ""
  ```
- **Connection State Notification**: `DeviceInformation.Update` events provide real-time status updates without aggressive polling.

---

## 3. Windows Audio (WASAPI) Observations

- **Endpoints**: Distinct enumeration for `eRender` (playback) and `eCapture` (microphones/line-in).
- **Bluetooth Endpoints**: Bluetooth audio devices appear as standard WASAPI endpoints once paired and connected in Windows Settings.
- **Sample Rate & Bit Depth**: Most Bluetooth A2DP audio endpoints operate at 44.1 kHz or 48 kHz stereo (16-bit or 24-bit). A format conversion stage is required prior to mixing.

---

## 4. Technical Spike Questions & Current Status

| # | Question | Current Status | Finding |
| :- | :--- | :--- | :--- |
| 1 | Can Windows discover the phone? | Testing in Phase 1 | Pending spike execution |
| 2 | Can Windows discover the tablet? | Testing in Phase 1 | Pending spike execution |
| 3 | Can Windows discover Mustang GoBoult Torq? | Testing in Phase 1 | Pending spike execution |
| 4 | Can phone audio route to Windows PC via A2DP Sink? | Testing in Phase 1 | Supported in Build 19041+ |
| 5 | What is the latency over the full routing path? | Testing in Phase 1 | Baseline measurement in progress |
