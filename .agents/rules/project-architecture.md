# Project Architecture & Domain Knowledge Rule

This rule defines the core architecture, domain model, platform constraints, and roadmap for the **Windows Bluetooth Audio Hub (EagleBT)** project.

---

## 1. Product Vision & Concept

The application transforms a Windows PC into a central Bluetooth audio router/hub:
```text
Mobile Phone ───┐
                │
Tablet ─────────┼── Bluetooth ──► Windows PC (EagleBT)
                │                      │
Local PC Audio ─┘                      │
                                       ▼
                              Audio Router / Mixer
                                       │
                                       ▼
                            Bluetooth Earbuds/Headphones
                            (Mustang GoBoult Torq)
```

The user should not need multipoint-capable earbuds; the Windows PC manages multiple audio streams, mixes or routes them, and plays them into a single Bluetooth endpoint.

---

## 2. Target Hardware & Bluetooth Profile Roadmap

- **Primary Target Device**: **Mustang GoBoult Torq** (Bluetooth 5 or newer).
- **Bluetooth Profile Implementation Strategy**:
  1. **A2DP (Advanced Audio Distribution Profile)**: First priority. Validate audio playback from remote sources to PC, and PC to earbuds.
  2. **AVRCP (Audio/Video Remote Control Profile)**: Later phase for transport controls (play/pause/volume).
  3. **HFP (Hands-Free Profile) / HSP (Headset Profile)**: Later phase for bidirectional phone calls.
- **Rule of Feasibility**: Do NOT assume Windows Bluetooth stack exposes every profile or role automatically. Always validate actual Windows API capabilities before building architectural abstractions.

---

## 3. Strict Platform Constraints

- **Windows-Only Native Desktop Application**:
  - Language: C# (.NET LTS)
  - UI: WinUI 3 (Windows App SDK)
  - Audio: WASAPI / Windows Core Audio APIs, NAudio where appropriate
  - Packaging: MSIX (or self-contained unpackaged if required for development spikes)
- **Forbidden Technologies**:
  - No Electron / web shells
  - No cross-platform abstractions (MAUI/Avalonia/Qt/macOS/Linux)
  - No cloud backends, microservices, databases, or Docker containers
  - Keep the app lean, native, and lightweight.

---

## 4. Core Domain Model

All components must conceptualize the audio routing pipeline through these domain models:

1. **Device**: Physical or logical entity (`Phone`, `Tablet`, `This PC`, `Mustang GoBoult Torq`).
2. **AudioSource**: Audio origin (`Phone Media`, `Phone Calls`, `Tablet Media`, `PC YouTube`, `PC System Audio`).
3. **AudioOutput**: Audio destination endpoint (`Mustang GoBoult Torq`, `Speakers`).
4. **AudioRoute**: Active mapping connecting an `AudioSource` to an `AudioOutput`, with volume, mute, and balance controls.

---

## 5. Development Roadmap & Phase Gating

All development must proceed phase-by-phase without premature feature implementation:

- **Phase 1**: Technical Foundation + Device Discovery (WinUI 3 basic shell, `BluetoothDeviceService`, `AudioDeviceService`, logging, xUnit tests, technical spike).
- **Phase 2**: Single remote device audio stream → Windows PC → Earbuds.
- **Phase 3**: Multiple remote devices audio stream → Windows PC → Earbuds.
- **Phase 4**: Real-time audio mixer (software mixing of multiple active streams).
- **Phase 5**: Full Audio Hub routing UI (WinUI 3 interactive channel matrix).
- **Phase 6**: AVRCP media control integration.
- **Phase 7**: HFP / Call routing & microphone management.
- **Phase 8**: User presets and device connection profiles.
- **Phase 9**: System tray integration, background daemon, and Windows startup.
- **Phase 10**: Low-latency buffering and audio optimization.

---

## 6. Golden Engineering Hierarchy

When making architectural trade-offs, adhere strictly to this priority order:
```text
1. Correctness
2. Technical Feasibility
3. Simplicity
4. Maintainability
5. Performance
6. Features
```
