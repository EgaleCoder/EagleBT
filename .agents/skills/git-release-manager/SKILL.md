---
name: git-release-manager
description: >-
  Manages Git repository operations, commits, branches, releases, and versioning for EagleBT.
  Activates ONLY when the user explicitly asks to commit, push, create branches, cut releases,
  initialize Git, or manage repository workflows.
---

# Git & Release Manager Skill

This skill governs all source control and versioning operations for the EagleBT repository. It is executed strictly on demand.

---

## Operational Constraint
> [!IMPORTANT]
> **Do NOT run Git commits or staging operations automatically.**
> Only invoke this workflow when the user explicitly requests a Git operation in their prompt (e.g. *"commit my changes"*, *"create a git repo"*, *"cut a release"*, *"switch branches"*).

---

## Standard Procedures

### 1. Repository Initialization & .gitignore Setup
When the user asks to initialize or setup the repository:
1. Verify if git is initialized (`git status`).
2. If not initialized, execute `git init`.
3. Ensure a comprehensive `.gitignore` exists at the repository root covering:
   - Visual Studio & VS Code directories: `.vs/`, `.vscode/`, `*.user`, `*.suo`
   - Build outputs: `bin/`, `obj/`, `AppPackages/`, `BundleArtifacts/`
   - Test results: `TestResults/`, `*.trx`
   - OS generated files: `Thumbs.db`, `desktop.ini`
   - Antigravity brain / scratch directories if temporary.

### 2. Daily & Incremental Commit Workflow
When the user instructs to commit changes:
1. Review modified files:
   ```bash
   git status --short
   ```
2. Inspect diff to confirm only intended changes are staged:
   ```bash
   git diff --stat
   ```
3. Stage intended files:
   ```bash
   git add <file-paths>
   ```
4. Formulate a **Conventional Commit** message in the format:
   `<type>(<scope>): <short imperative description>`

   **Allowed Types**:
   - `feat`: A new user-facing feature or domain capability.
   - `fix`: A bug fix.
   - `refactor`: Code change that neither fixes a bug nor adds a feature.
   - `docs`: Documentation updates only.
   - `chore`: Maintenance, build scripts, or dependency updates.
   - `test`: Adding or updating unit/integration tests.
   - `spike`: Exploratory or feasibility proof of concept code.

   **Examples**:
   - `feat(bluetooth): implement WindowsBluetoothDeviceService enumeration`
   - `docs(arch): add Phase 1 architecture and technical spike plan`
   - `chore(git): add C# WinUI 3 gitignore and initialize repository`

### 3. Branch Management Strategy
When managing branches:
- **`main`**: Production-ready, fully tested, and stable code.
- **`dev`**: Integration branch for current milestone/phase.
- **`feature/<name>`**: Specific feature development (e.g. `feature/audio-endpoint-watcher`).
- **`spike/<name>`**: Exploratory spikes testing Windows Bluetooth/WASAPI capabilities (e.g. `spike/a2dp-sink-test`).

### 4. Releases & Versioning
When the user asks to create a version or release:
1. Adhere to **Semantic Versioning** (`vMAJOR.MINOR.PATCH`):
   - `MAJOR`: Breaking changes or major architectural redesigns.
   - `MINOR`: New features or milestone phase completions (e.g. Phase 1 foundation complete -> `v0.1.0`).
   - `PATCH`: Bug fixes, minor adjustments, and documentation corrections.
2. Generate a clean release entry in `CHANGELOG.md` detailing:
   - Added features
   - Fixed bugs
   - Known limitations / Windows constraints
3. Create an annotated Git tag:
   ```bash
   git tag -a v0.1.0 -m "Release v0.1.0: Phase 1 Foundation and Device Discovery"
   ```
