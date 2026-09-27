# Phase 4: Real-Time Audio Mixer (Software Mixing of Multiple Active Streams)

## 1. Goal

Phase 4 introduces a real-time software audio mixer into the EagleBT pipeline:
```text
Mobile Phone (A2DP Stream 1) ──┐
                               ├──► Software Audio Mixer (32-bit Float DSP) ──► Mustang GoBoult Torq (WASAPI Render)
Tablet (A2DP Stream 2) ────────┤      - Equal-Power Stereo Panning
                               │      - Solo / Mute Matrix
Local PC Audio (Loopback) ─────┘      - Soft-Knee Saturation Limiter (0 dBFS Anti-Clip)
                                      - 30 Hz Dual L/R VU Level Telemetry
```
When multiple remote Bluetooth devices (and PC applications) stream audio simultaneously, the software mixer sums their PCM audio signals in real time, provides independent volume, stereo balance/panning, and solo/mute routing for each channel strip, prevents digital clipping via a soft-knee saturation limiter, and feeds live level telemetry to the WinUI 3 dashboard for responsive VU metering.

---

## 2. Phase 4 Deliverables

### Core Domain Abstractions (`AudioHub.Core`)
- **`AudioMixerChannel`**:
  - Encapsulates channel state: `ChannelId`, `Name`, `Gain` ($0.0$ to $1.0$), `Pan` ($-1.0$ [L] to $+1.0$ [R]), `IsMuted`, `IsSolo`, `PeakLeft`, `PeakRight`, `RmsLeft`, `RmsRight`.
- **`AudioMixerMaster`**:
  - Encapsulates master bus state: `MasterGain` ($0.0$ to $1.0$), `IsMuted`, `LimiterEnabled`, `PeakLeft`, `PeakRight`, and `IsClipping`.
- **`MixerLevelsEventArgs`**:
  - Telemetry payload containing snapshot of all active channel peaks and master stereo peak levels.
- **`IAudioMixerService`**:
  - Contract for software audio mixing coordination, channel lifecycle management, gain/pan/solo/mute parameter adjustments, and audio frame processing (`ProcessAudioFrame`).
- **`AudioMixerDsp`**:
  - Pure DSP routines:
    1. **$-3\text{ dB}$ Constant Power Pan Law**:
       $$\text{angle} = (\text{pan} + 1) \cdot \frac{\pi}{4}, \quad \text{gain}_L = \cos(\text{angle}), \quad \text{gain}_R = \sin(\text{angle})$$
       Guarantees identical acoustic power whether a source is panned center, left, or right.
    2. **Solo Matrix**:
       If any channel is soloed, only soloed channels contribute to the output sum.
    3. **Soft-Knee Saturation Limiter**:
       $$f(x) = \begin{cases} x & |x| < 0.7 \\ \tanh(x) & |x| \ge 0.7 \end{cases}$$
       Smoothly compresses signals exceeding $0\text{ dBFS}$ to prevent harsh digital square-wave clipping or integer overflow distortion.
    4. **Stereo Level Metering**:
       Calculates peak amplitude and RMS power across stereo sample frames.

### Windows Platform Implementation (`AudioHub.Windows`)
- **`SoftwareAudioMixerService`**:
  - Thread-safe software mixer managing channels via `ConcurrentDictionary`.
  - Implements multi-stream summing, per-channel gain and pan scaling, master bus processing, and clipping detection.
  - Generates $30\text{ Hz}$ high-precision telemetry updates (`LevelsUpdated`) with an analog-style exponential decay envelope ($0.88$ factor) for realistic VU meter ballistics.

### Dependency Injection Wiring (`AudioHub.Infrastructure`)
- Registered `IAudioMixerService` $\rightarrow$ `SoftwareAudioMixerService` as a Singleton in `ServiceCollectionExtensions.cs`.

### Presentation Layer (`AudioHub.App`)
- **`RouteChannelViewModel`**:
  - Extended with `Pan`, `FormattedPan` (e.g. `"L 40%"`, `"C"`, `"R 25%"`), `IsSolo`, `SoloButtonText`, `PeakLeft`, and `PeakRight`.
  - Synchronizes parameter changes directly with `IAudioMixerService`.
- **`MainViewModel`**:
  - Injects `IAudioMixerService`.
  - Exposes `MasterVolume`, `FormattedMasterVolume`, `IsMasterMuted`, `MasterMuteText`, `IsLimiterEnabled`, `LimiterStatusText`, `MasterPeakLeft`, `MasterPeakRight`, and `IsMasterClipping`.
  - Dispatches live level telemetry to UI thread via `DispatcherQueue`.
- **`MainPage.xaml` & `MainPage.xaml.cs`**:
  - Added **Master Bus Bar** with master volume fader, dual L/R VU meters, "CLIP" warning indicator, anti-clipping limiter toggle switch, and master mute.
  - Enhanced channel strips with:
    - Live dual L/R VU meters.
    - Stereo Pan slider ($-1.0$ to $+1.0$).
    - Solo button ("S" / "Solo On").
    - Channel volume fader and Mute toggle.

---

## 3. Exit Criteria

Phase 4 is considered complete when:
1. Solution compiles cleanly with zero warnings and zero errors across all projects.
2. `IAudioMixerService` and `AudioMixerDsp` provide sample summing, equal-power stereo panning, solo/mute matrix routing, and soft-knee saturation limiting.
3. Thread-safe `SoftwareAudioMixerService` publishes live $30\text{ Hz}$ VU metering telemetry.
4. WinUI 3 dashboard renders interactive channel strips (fader, pan, solo, mute, VU meters) and master bus controls.
