---
name: code-guardian
description: >-
  Audits proposed code changes against user instructions to guarantee zero unauthorized
  modifications. Verifies C#, WinUI 3, MVVM, and architectural boundaries before completing tasks.
---

# Code Guardian Skill

This skill enforces strict code hygiene, zero unauthorized modifications, and project architecture boundaries across all code changes in the EagleBT workspace.

---

## 1. Pre-Modification Scope Checklist

Before touching any file, verify:
- [ ] Is this file explicitly requested or strictly necessary to fulfill the user's prompt?
- [ ] Are we touching any line, method, or file outside the requested scope? If yes, **HALT** and exclude that file.
- [ ] Did the user ask for refactoring? If **NO**, do not touch or reformat existing code.

---

## 2. Post-Modification Audit Checklist

After making any edit, run this verification:
1. **Scope Inspection**:
   - Check the file diff.
   - Confirm that only the targeted functionality was added or changed.
   - Confirm no unrelated comments, formatting, or methods were altered.
2. **C# Quality Checklist**:
   - [ ] Are nullable reference types respected without warnings?
   - [ ] Do asynchronous methods accept a `CancellationToken`?
   - [ ] Is error handling specific, logging errors without exposing raw stream data?
   - [ ] Is the code placed in the correct project layer (`AudioHub.App`, `AudioHub.Core`, `AudioHub.Windows`, `AudioHub.Infrastructure`)?
3. **Compilation Verification**:
   - Run `dotnet build` to ensure zero compile errors and zero new warnings.
