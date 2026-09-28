# Phase 5: Full Audio Hub Routing UI (WinUI 3 Interactive Channel Matrix)

## 1. Goal

Phase 5 elevates EagleBT into a professional studio-grade audio routing hub by introducing an interactive **2D Cross-Point Routing Matrix** and a unified multi-view navigation system in WinUI 3:

```text
                               ┌──────────────────────────────────────────────────────────┐
                               │            EAGLEBT ROUTING MATRIX (PHASE 5)              │
                               ├──────────────────────────┬───────────────────────────────┤
                               │ Audio Sources (Inputs)   │ Destination Outputs           │
                               │                          │ [Mustang GoBoult Torq] [PC SPK]
[Phone Media]    ─────────────►│ ● Route Active           │     [● ACTIVE]         [○ ROUTE]│
[Tablet Media]   ─────────────►│ ● Route Active           │     [● ACTIVE]         [○ ROUTE]│
[PC System Audio]─────────────►│ ● PC Loopback            │     [○ ROUTE]          [● ACTIVE]│
                               └──────────────────────────┴───────────────────────────────┘
                                                            │
                                                            ▼
                                             Real-Time Audio Mixer & DSP Bus
                                             (Faders, Pan, Solo, Limiter, VU)
```

Phase 5 gives the user instantaneous visual control over any audio input source (paired Bluetooth devices, Windows PC system audio) and any destination output endpoint (target Bluetooth earbuds **Mustang GoBoult Torq**, PC speakers, USB interfaces), allowing one-click cross-point routing, stream switching, and real-time status telemetry.

---

## 2. Phase 5 Deliverables

### Core Domain Abstractions (`AudioHub.Core`)
- **Dynamic Route Re-Targeting**:
  - `AudioRoute.Output`: Made mutable (`{ get; set; }`) to support real-time destination switching without stream interruption.
  - `IAudioRoutingService.UpdateRouteOutputAsync`:
    - Safely re-routes an existing active stream to a new `AudioOutput` destination.
    - Dispatches `RouteUpdated` with reason `"Output endpoint updated"`.
  - Implemented in `AudioRoutingService` using thread-safe concurrent route collections.

### Presentation Layer & Matrix Architecture (`AudioHub.App`)
- **Interactive Multi-View Navigation**:
  - Introduced a segmented navigation tab bar:
    1. **🎛️ Mixer & Channels View (`SelectedViewIndex == 0`)**:
       - Individual channel strips with volume faders, equal-power pan sliders, solo/mute toggles, L/R peak VU meters, and stop stream buttons.
       - Quick Stream Launcher for discovered Bluetooth devices.
    2. **🔀 Cross-Point Routing Matrix View (`SelectedViewIndex == 1`)**:
       - 2D Cross-Point routing grid.
       - Column headers for all active WASAPI render endpoints (with **(BT)** badge for **Mustang GoBoult Torq**).
       - Row headers for all audio sources (remote Bluetooth phones/tablets + Windows System Audio).
       - Interactive cross-point buttons indicating active connection status (`ACTIVE` vs `ROUTE`) and real-time tooltips.
    3. **📡 Hardware & Devices View (`SelectedViewIndex == 2`)**:
       - Detailed two-column hardware diagnostics for Bluetooth devices and WASAPI endpoints.
- **Master Bus & Limiter Header (Persistent)**:
  - Pinned at the top of the interface across all three tabs, displaying:
    - Live multi-stream routing status.
    - Master Volume fader and percentage readout.
    - Stereo L/R peak VU meters with dynamic analog ballistics.
    - Soft-knee anti-clipping limiter toggle.
    - Master Mute toggle.
    - Global "Stop All Streams" emergency shutdown button.
- **Matrix ViewModels**:
  - **`MatrixCellViewModel`**: Encapsulates state for an individual cross-point $(Source_i, Output_j)$, handling connect/disconnect states, connecting animations, and tooltips.
  - **`MatrixRowViewModel`**: Encapsulates state for an audio source row with its collection of cross-point cells, active route tallies, and glyphs.
  - **`MainViewModel`**:
    - Manages `MatrixEndpoints` and `MatrixRows`.
    - `RefreshMatrix()`: Thread-safe automatic synchronization called upon device discovery, connection state changes, or route updates.
    - `ToggleMatrixCrossPointAsync`: Dispatches connect/disconnect/re-route commands seamlessly.

### Automated Unit Test Suite (`tests/AudioHub.Core.Tests`)
- **`AudioRoutingServiceTests`**:
  - Validates `UpdateRouteOutputAsync` for valid routes, ensuring destination output is updated and `RouteUpdated` event is fired.
  - Validates `UpdateRouteOutputAsync` with non-existent route IDs gracefully returning `null`.
- **`AudioMixerDspTests`**:
  - Validates equal-power stereo panning at center ($-3\text{ dB}$, power sum $= 1.0$), hard left, and hard right.
  - Validates soft-knee saturation limiter bounding overdriven audio within $[-1.0, 1.0]$.
  - Validates stereo peak and RMS level calculations across audio sample buffers.

---

## 3. Exit Criteria Verification

Phase 5 is considered complete when:
1. Solution domain architecture supports dynamic route endpoint updating (`UpdateRouteOutputAsync`).
2. WinUI 3 interface provides a 3-view navigation model (Mixer & Channels, Cross-Point Matrix, Hardware & Endpoints).
3. The 2D cross-point routing matrix dynamically establishes, toggles, and switches audio routes across Bluetooth sources and output endpoints in real time.
4. Master Bus monitoring (VU meters, limiter, fader) remains visible and responsive across all views.
5. Unit tests verify dynamic output re-routing and core DSP mixing algorithms.
