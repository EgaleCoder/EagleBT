# Windows Bluetooth Stack & Profile Evaluation

## 1. Overview

The Windows Bluetooth subsystem operates across multiple layers, combining user-mode WinRT APIs with kernel-mode drivers provided by Microsoft and device manufacturers. Understanding what the Windows Bluetooth stack natively allows is the foundation of EagleBT.

---

## 2. Windows Bluetooth API Surfaces

EagleBT evaluates and targets two primary API sets:

1. **`Windows.Devices.Enumeration`**:
   - `DeviceWatcher`: Provides asynchronous, event-driven detection of Bluetooth devices being added, updated, removed, or changing connection status.
   - **AQS (Advanced Query Syntax)**: Used to filter specifically for Bluetooth Classic and BLE radios.
   - Example AQS selector:
     ```csharp
     BluetoothDevice.GetDeviceSelectorFromPairingState(true);
     ```

2. **`Windows.Devices.Bluetooth`**:
   - `BluetoothDevice`: Provides access to paired Bluetooth Classic devices, device names, Bluetooth addresses, connection status, and device class.
   - `BluetoothAdapter`: Queries local radio capabilities (e.g., whether Classic Bluetooth or Low Energy is supported).

---

## 3. Bluetooth Profile Strategy

| Profile | Full Name | EagleBT Role | Strategy & Priority |
| :--- | :--- | :--- | :--- |
| **A2DP** | Advanced Audio Distribution Profile | Audio stream playback | **Phase 1 Priority**. Windows acts both as an A2DP Sink (receiving audio from phones/tablets) and A2DP Source (sending audio to Mustang GoBoult Torq). |
| **AVRCP** | Audio/Video Remote Control Profile | Media playback control | **Phase 6**. Handles play, pause, next track, and remote volume sync. |
| **HFP** | Hands-Free Profile | Voice calls & microphone | **Phase 7**. Manages bidirectional mono audio with microphone routing. |
| **HSP** | Headset Profile | Legacy voice audio | Fallback if HFP is unavailable. |

---

## 4. The Windows 10 A2DP Sink Feasibility Focus

Starting in Windows 10 version 2004 (Build 19041), Microsoft restored support for **A2DP Sink** via the `Windows.Media.Audio.AudioPlaybackConnection` API:
- Allows a Windows PC to register as an audio receiver for a paired mobile phone.
- Once connected, audio played on the phone is routed directly into the Windows audio pipeline.

### Feasibility Spike Questions
1. Does the local Bluetooth radio support concurrent A2DP connections (receiving from phone while transmitting to Mustang GoBoult Torq)?
2. What latency is introduced by the Windows A2DP sink buffer?
3. How does Windows handle audio routing when both phone media and local PC apps are active?

These questions will be resolved through isolated technical spikes.
