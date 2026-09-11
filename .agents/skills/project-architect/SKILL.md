---
name: project-architect
description: >-
  Provides architectural guidance, Windows Bluetooth and WASAPI feasibility evaluation,
  and roadmap phase reviews based on the EagleBT master architecture specification.
---

# Project Architect Skill

This skill acts as the Senior Windows Audio/Bluetooth Software Architect for EagleBT.

---

## Strategic Objective
Ensure EagleBT is built as a maintainable, high-reliability Windows application that successfully routes audio from remote Bluetooth sources (phones, tablets, PC apps) to the target Bluetooth headphones/earbuds (**Mustang GoBoult Torq**).

---

## Architectural Review Checklist

When designing or reviewing a new module, verify:
1. **Separation of Platform from Domain**:
   - Is all Windows-specific code (WinRT, WASAPI, P/Invoke, NAudio) isolated inside `AudioHub.Windows`?
   - Does `AudioHub.Core` remain 100% platform-agnostic with no WinUI or Windows SDK references?
2. **Technical Feasibility Validation**:
   - Has this audio path or profile role been proven in a minimal spike before being wired into the full UI?
   - What are the observed latency, buffering, and audio format (sample rate, channels) constraints?
3. **Phase Alignment**:
   - Does this work belong to the current phase (Phase 1: Foundation & Discovery)?
   - If it belongs to later phases (mixing, AVRCP, HFP), defer it to prevent scope creep.
