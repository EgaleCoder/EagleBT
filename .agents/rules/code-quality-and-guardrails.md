# Code Quality, Tech Stack & Strict Guardrails Rule

This rule is mandatory and active across the entire workspace for all code inspection, creation, and modification.

---

## 1. Absolute Scope Guardrails (Zero Unauthorized Modifications)

1. **Prompt-Only Scope**:
   - You must make **ONLY** the specific changes explicitly asked in the user's prompt.
   - Do **NOT** touch, edit, format, or clean up any code, files, or comments outside the exact boundaries requested by the user.
2. **Zero Unauthorized Changes**:
   - Without explicit permission from the user, not even a single letter, variable, or comment should be modified in this project.
   - If you notice a bug, potential improvement, or obsolete code outside what the user requested, **do NOT fix or alter it silently**. You may only report it as an observation in your textual response and wait for user permission.
3. **No Unsolicited Refactoring**:
   - Never reformat entire files when making localized changes.
   - Use surgical edits (`replace_file_content` or `multi_replace_file_content`) to change only the relevant lines.
   - Preserve existing code comments and docstrings.

---

## 2. Tech Stack & Standards

- **Language & Runtime**: C# with modern supported .NET (prefer LTS version).
- **UI Framework**: WinUI 3 (Windows App SDK) using the MVVM pattern.
- **Audio & Bluetooth Subsystems**:
  - Windows SDK / WinRT Bluetooth APIs (`Windows.Devices.Bluetooth`, `Windows.Devices.Enumeration`).
  - WASAPI (`Windows.Media.Devices`, native endpoints) and NAudio (where useful and appropriate; never blind wrapping).
- **Configuration & Storage**: JSON configuration initially. No databases (SQLite/SQL Server) unless explicitly needed and approved.
- **Testing**: xUnit or mature .NET test framework.
- **Logging**: Microsoft.Extensions.Logging structured logging. Never log sensitive user information or raw audio buffer contents.

---

## 3. Code Quality & Readability Requirements

- **SOLID Principles**: Each class must have a single, well-defined responsibility.
- **Async Hygiene**: Use `async`/`await` consistently for I/O and device communication. Always pass and propagate `CancellationToken`.
- **Null Safety**: Nullable reference types must be enabled (`<Nullable>enable</Nullable>`) and strictly adhered to without spurious null suppressions (`!`).
- **No Global Mutable State**: Avoid static mutable state or service locator patterns. Use Microsoft.Extensions.DependencyInjection.
- **Error Handling**: Catch specific exceptions at appropriate boundaries. Never swallow exceptions with empty catch blocks.
- **Readability**:
  - Self-documenting code with clear variable and method names.
  - XML documentation on all public service interfaces and domain contracts.
  - Keep methods small, focused, and free of cyclomatic bloat.

---

## 4. Folder Structure & Architectural Boundary Enforcement

Maintain strict separation of concerns across the project directories:

```text
src/
├── AudioHub.App/            # WinUI 3 Views, ViewModels, UI resources. NO raw Bluetooth/audio business logic.
├── AudioHub.Core/           # Domain models, service interfaces, core routing logic. ZERO UI or platform dependencies.
├── AudioHub.Windows/        # Windows SDK, WinRT Bluetooth, WASAPI implementations of Core interfaces.
└── AudioHub.Infrastructure/ # Dependency injection setup, configuration providers, structured logging.
tests/
├── AudioHub.Core.Tests/     # Unit tests for domain models and routing logic.
└── AudioHub.Windows.Tests/  # Integration/mock tests for Windows subsystem services.
docs/                        # Technical documentation, architectural decisions, and findings.
scripts/                     # Build, deployment, and test scripts.
```

- `AudioHub.Core` must NEVER reference `AudioHub.App`, `AudioHub.Windows`, or any UI package.
- `AudioHub.App` communicates with `AudioHub.Windows` implementations exclusively through `AudioHub.Core` interfaces registered in DI.
