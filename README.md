# EagleBT — Windows Bluetooth Audio Hub

[![Platform](https://img.shields.io/badge/platform-Windows%2010%20%2F%2011-blue.svg)](https://www.microsoft.com/windows)
[![Technology](https://img.shields.io/badge/.NET-LTS-purple.svg)](https://dotnet.microsoft.com/)
[![UI](https://img.shields.io/badge/UI-WinUI%203-0078D7.svg)](https://learn.microsoft.com/windows/apps/winui/winui3/)
[![Audio](https://img.shields.io/badge/Audio-WASAPI-teal.svg)](https://learn.microsoft.com/windows/win32/coreaudio/wasapi)

> A lightweight, native Windows desktop application that acts as a central Bluetooth audio router, enabling multiple remote devices (phone, tablet, local PC) to share a single Bluetooth audio output (**Mustang GoBoult Torq**) simultaneously.

---

## 🎯 Product Vision

Instead of requiring expensive multipoint earbuds, **EagleBT** turns your Windows PC into a central audio router/mixer:

```text
Mobile Phone ───┐
Tablet ─────────┼── Bluetooth ──► Windows PC (EagleBT)
Local PC Audio ─┘                      │
                                       ▼
                              Audio Router / Mixer
                                       │
                                       ▼
                            Bluetooth Earbuds/Headphones
                            (Mustang GoBoult Torq)
```

---

## 🛠️ Technology Stack

- **Language & Runtime**: C# (.NET LTS)
- **UI Framework**: WinUI 3 (Windows App SDK) with MVVM Architecture
- **Bluetooth Subsystem**: Windows SDK / WinRT Bluetooth APIs (`Windows.Devices.Bluetooth`, `Windows.Devices.Enumeration`)
- **Audio Subsystem**: WASAPI (Windows Audio Session API) / Core Audio APIs, with NAudio where appropriate
- **Logging**: `Microsoft.Extensions.Logging` (structured, privacy-preserving)
- **Packaging**: MSIX

---

## 📐 Architecture & Domain Model

The system operates around a clean, decoupled **Source → Route → Output** model:

- **Devices**: Physical or logical entities (`Phone`, `Tablet`, `This PC`, `Mustang GoBoult Torq`).
- **Sources**: Audio origin streams (`Phone Media`, `Phone Calls`, `Tablet Media`, `PC YouTube`, `PC System Audio`).
- **Outputs**: Target audio playback endpoints (`Mustang GoBoult Torq`).
- **Routes**: Active connections routing audio sources to outputs with gain, mute, and balance control.

### Planned Project Structure

```text
src/
├── AudioHub.App/            # WinUI 3 Views & ViewModels (MVVM)
├── AudioHub.Core/           # Platform-agnostic Domain Models & Interfaces (Zero UI dependencies)
├── AudioHub.Windows/        # Windows SDK, WinRT Bluetooth & WASAPI implementations
└── AudioHub.Infrastructure/ # Dependency Injection, Logging & Configuration
tests/
├── AudioHub.Core.Tests/     # Unit tests for domain routing logic
└── AudioHub.Windows.Tests/  # Integration tests for Windows audio/BT services
docs/                        # Architecture decisions, Bluetooth findings & spike logs
```

---

## 🗺️ 10-Phase Roadmap

1. **Phase 1 (Current)**: Foundation + Bluetooth/Audio device discovery + Technical Feasibility Spikes.
2. **Phase 2**: Single remote device audio stream → Windows PC → Earbuds.
3. **Phase 3**: Multiple remote devices audio stream → Windows PC → Earbuds.
4. **Phase 4**: Software audio mixing engine.
5. **Phase 5**: Full Audio Hub routing UI (WinUI 3).
6. **Phase 6**: AVRCP media control integration (Play/Pause/Track Skip).
7. **Phase 7**: HFP / Call routing & microphone management.
8. **Phase 8**: User presets and connection profiles.
9. **Phase 9**: System tray daemon and Windows auto-startup.
10. **Phase 10**: Low-latency buffering and performance optimization.

---

## 🤖 Multi-Agent Governance

This project is governed by three internal Antigravity agents configured in [`AGENTS.md`](./AGENTS.md):

1. **Git & Release Manager (`git-release-manager`)**: Strictly on-demand source control, conventional commits, SemVer, and releases.
2. **Code Quality & Guardrails (`code-quality-guardrails`)**: Zero unauthorized edits rule—modifies only code explicitly requested in prompt.
3. **Project Architect (`project-architect`)**: Maintains the feasibility-first doctrine, audio routing domain model, and 10-phase roadmap.

---

## 📄 License
Private / Proprietary. All rights reserved.
