# Windows Audio Subsystem & WASAPI Architecture

## 1. Overview

EagleBT relies on the Windows Audio Session API (**WASAPI**) for all low-level audio routing, mixing, and stream management. WASAPI allows the application to communicate directly with audio device endpoints with minimal latency and high fidelity.

---

## 2. Core Audio Endpoints & WASAPI

The Windows audio architecture models sound hardware as **Endpoints**:
- **Render Endpoints (`eRender`)**: Playback devices such as the default PC speakers, USB headsets, or Bluetooth audio sinks (**Mustang GoBoult Torq**).
- **Capture Endpoints (`eCapture`)**: Input devices including microphones, line-in jacks, and incoming virtual audio streams.

### Key WASAPI Interfaces
- **`IMMDeviceEnumerator`**: Enumerates active and disabled audio endpoints across the system and monitors default device changes.
- **`IMMDevice`**: Represents an individual audio device endpoint.
- **`IAudioClient`**: Manages the audio stream between the application and the audio engine, configuring buffer size, latency mode, and sample format.
- **`IAudioRenderClient` / `IAudioCaptureClient`**: Writes or reads raw PCM audio packets to and from the audio endpoint buffers.

---

## 3. Audio Routing Pipeline

```text
┌─────────────────────────┐
│   Incoming Stream       │
│  (Remote Phone / PC App)│
└────────────┬────────────┘
             │ PCM Float32 / Int16
             ▼
┌─────────────────────────┐
│   Format Normalizer     │ Resamples to target rate (e.g. 48kHz, Stereo)
└────────────┬────────────┘
             │
             ▼
┌─────────────────────────┐
│     Software Mixer      │ Gain, Pan & Mute per route
└────────────┬────────────┘
             │
             ▼
┌─────────────────────────┐
│      WASAPI Output      │ Writes to Mustang GoBoult Torq endpoint
└─────────────────────────┘
```

---

## 4. Loopback Recording for PC Application Routing

To route PC-based audio (e.g., YouTube in a browser, Spotify, Microsoft Teams) independently:
- WASAPI provides **WASAPI Loopback Capture**.
- In modern Windows 10/11, process-specific loopback audio capture (`AUDIOCLIENT_ACTIVATION_PARAMS` / `AudioClientProperties`) enables capturing audio from a specific application without capturing the entire system sound.

---

## 5. NAudio Role & Boundaries

- **NAudio** is utilized as a battle-tested helper for Core Audio COM interop, WASAPI wrapper classes (`WasapiOut`, `WasapiLoopbackCapture`), and sample format conversion (`WaveFormatConversionStream`).
- Direct WinRT/WASAPI APIs are preferred when fine-grained control over low-latency hardware buffering is required.
