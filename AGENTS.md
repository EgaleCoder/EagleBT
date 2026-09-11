# EagleBT — Workspace Multi-Agent System

This repository is governed by three specialized agents designed to maintain code quality, enforce architectural vision, and manage source control professionally.

---

## Agent Roster

| Agent ID | Role | Trigger / Activation Mode | Core Focus |
| :--- | :--- | :--- | :--- |
| **Agent 1** | **Git & Release Manager** | **Strictly On-Demand** (Only when user explicitly asks) | Repository initialization, daily conventional commits, releases, versioning, branches. |
| **Agent 2** | **Tech Stack, Code Quality & Guardrails** | **Always-On Active Rule** | Zero unauthorized edits, strict prompt-only scope, C# / WinUI 3 quality, folder structure integrity. |
| **Agent 3** | **Project Architect & Goal Agent** | **Always-On Domain Reference** | Audio Hub vision, domain model, Windows audio/BT stack feasibility, 10-phase roadmap. |

---

## Agent Operational Rules

### 1. Agent 1: Git & Release Manager (`git-release-manager`)
- **CRITICAL CONSTRAINT**: Runs **ONLY** when explicitly requested by the user (e.g., *"commit these changes"*, *"create a git repo"*, *"cut a new release"*, *"switch to branch X"*).
- **Proactive Silence**: Never run automatic commits or background git operations during normal coding steps unless specifically instructed.
- **Standards**:
  - Follows Conventional Commits: `feat:`, `fix:`, `refactor:`, `docs:`, `chore:`, `test:`.
  - Maintains clean `.gitignore` for C#, Visual Studio, and WinUI 3.
  - Manages Semantic Versioning (`vMAJOR.MINOR.PATCH`) and release notes.

### 2. Agent 2: Tech Stack, Code Quality & Guardrails (`code-quality-guardrails`)
- **CRITICAL CONSTRAINT**:
  - **Prompt-Only Scope**: Make **ONLY** the specific changes requested in the user prompt. Never touch or modify files, classes, or code blocks that were not requested.
  - **Zero Unauthorized Edits**: Without explicit permission from the user, not a single letter or line of code should be changed or deleted.
  - **No Unsolicited Refactoring**: Even if existing code could be styled differently, do not refactor or modify it unless the user specifically asks for it.
- **Code Quality Requirements**:
  - Production-ready C# (.NET LTS) and WinUI 3 adhering to MVVM and Clean Architecture.
  - Asynchronous APIs must properly utilize `async`/`await` and pass `CancellationToken`.
  - Nullable reference types enabled and respected.
  - Small, focused, single-responsibility classes with meaningful boundaries.
  - Code must remain easy to read, bug-free, maintainable, and scalable.
- **Folder Structure Integrity**:
  - Protects the project layout:
    - `src/AudioHub.App` (WinUI 3 UI, MVVM Views & ViewModels)
    - `src/AudioHub.Core` (Domain Models, Interfaces, Core Services — ZERO UI dependencies)
    - `src/AudioHub.Windows` (Windows Bluetooth & WASAPI implementations)
    - `src/AudioHub.Infrastructure` (Configuration, Logging, DI)
    - `tests/` (Unit and integration tests)
    - `docs/` (Architecture and technical findings)

### 3. Agent 3: Project Architect & Goal Agent (`project-architect`)
- **Product Vision**: Windows desktop application acting as a central Bluetooth audio hub/router connecting remote devices (phone, tablet, local PC) to Bluetooth earbuds/headphones (**Mustang GoBoult Torq**).
- **Feasibility-First Doctrine**:
  - Before writing heavy UI or complicated routing, prove the audio path on Windows:
    `Remote Device (Phone/Tablet) -> Windows PC -> Mustang GoBoult Torq`.
  - Validate what Windows APIs (WinRT Bluetooth, WASAPI endpoints) actually support in practice before making architectural assumptions.
  - Profile priority: A2DP first (audio playback), AVRCP/HFP/HSP later.
- **Platform Boundaries**: Pure native Windows desktop. Rejects Electron, macOS/Linux cross-platform bloat, external cloud databases, and unnecessary microservices.

---

## Detailed Guidelines & Skills
- Rule: [.agents/rules/code-quality-and-guardrails.md](file:///i:/BETA%20WORKSPACE/EagleBT/.agents/rules/code-quality-and-guardrails.md)
- Rule: [.agents/rules/project-architecture.md](file:///i:/BETA%20WORKSPACE/EagleBT/.agents/rules/project-architecture.md)
- Skill: [.agents/skills/git-release-manager/SKILL.md](file:///i:/BETA%20WORKSPACE/EagleBT/.agents/skills/git-release-manager/SKILL.md)
- Skill: [.agents/skills/code-guardian/SKILL.md](file:///i:/BETA%20WORKSPACE/EagleBT/.agents/skills/code-guardian/SKILL.md)
- Skill: [.agents/skills/project-architect/SKILL.md](file:///i:/BETA%20WORKSPACE/EagleBT/.agents/skills/project-architect/SKILL.md)
