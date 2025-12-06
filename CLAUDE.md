# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

---

## Project Context

ARIS is a C#/.NET 8 rewrite of an Unreal Engine modding toolkit. This is a clean rewrite, not a line-by-line port.  
The original Go/Wails implementation is a **behavioral reference only**.

**Stack:**

- Backend: C# / .NET 8 (see `global.json`)
- Frontend: React + TypeScript + Tailwind in `frontend/`
- UI Host: WebView2-based desktop application
- Target Platform: Windows 10/11 x64 only

> **Do not modify this file or the docs in `docs/` unless the user explicitly asks you to.**

---

## Essential Reading (Canonical Sources)

Before making any code changes, always read or re-open:

1. `docs/dev/CLAUDE_ARIS_Initial_Prompt.md` – **primary project instruction and coding rules**
2. `docs/legacy_resources/ARIS_High_Level_Design_SDD.md` – overall system architecture
3. `docs/legacy_resources/ARIS_Backend_SDD.md` – backend layering and design

For specific tool integrations, use the relevant SDD:

- `docs/legacy_resources/ARIS_Retoc_Integration_SDD.md`
- `docs/legacy_resources/ARIS_UAssetAPI_Integration_SDD.md`
- `docs/legacy_resources/ARIS_UWPDumper_Integration_SDD.md`
- `docs/legacy_resources/ARIS_DLLInjector_Integration_SDD.md`
- `docs/legacy_resources/ARIS_Frontend_SDD.md`

**Never contradict these documents.**  
If code and docs disagree, call it out explicitly and propose a fix instead of silently choosing one.

---

## Development Commands

All commands below assume the **repo root** as the working directory.

### Backend

Build the entire solution:

```
dotnet build
```

Run all tests:

```
dotnet test
```

Run a specific test project:

```
cd tests/Aris.Core.Tests
dotnet test
```

Run backend host in development:

```
cd src/Aris.Hosting
dotnet run
```

Single-file publish for Windows x64:

```
cd src/ARIS.UI  # or Aris.Hosting, depending on the chosen entry point
dotnet publish -c Release -r win-x64 --self-contained true /p:PublishSingleFile=true
```

### Frontend

From the `frontend/` directory:

```
npm install          # Install dependencies
npm run dev          # Start dev server
npm run build        # Production build to dist/
npm run preview      # Preview production build
```

---

## Architecture Overview

### Layered Backend Structure

The backend follows a clean, layered architecture under `src/`:

* **Aris.Core** – Domain primitives, value objects, core business rules
* **Aris.Application** – Use cases, orchestration, progress tracking, operation handlers
* **Aris.Infrastructure** – Filesystem, process execution, logging, settings, crypto, compression
* **Aris.Adapters** – Tool-specific adapters (Retoc, UAssetAPI, UWPDumper, DLL Injector)
* **Aris.Contracts** – DTOs shared with the frontend
* **Aris.Tools** – Embedded tool binaries/resources and extraction helpers
* **Aris.Hosting** – Composition root, DI container, IPC/HTTP bridge
* **ARIS.UI** – WebView2 desktop host for the frontend

**Dependency Flow:**
UI → Hosting → Application/Adapters → Infrastructure/Core

* Infrastructure and Adapters may depend on Core.
* Application orchestrates but does not directly handle raw IO beyond its defined ports.
* Do not create cyclic or “reach-around” dependencies that bypass this flow.

### Key Architectural Concepts

**Tool Embedding & Extraction:**

* Third-party binaries are embedded as resources in `Aris.Tools`
* At runtime, tools are extracted to `%LOCALAPPDATA%/ARIS/tools/{version}/`
* SHA-256 verification on all extracted tools before execution
* Idempotent extraction with lock files and manifest versioning

**Process Execution:**

* All external processes run through validated wrappers
* Argument allowlisting per tool to prevent injection
* Bounded timeouts and cancellation support
* Elevation (UAC) only when explicitly required

**Operations & Progress:**

* Long-running operations are modeled as commands with handlers
* Step-level progress events with cancellation tokens
* Transactional workspace writes (temp staging, then atomic move)
* Structured logging with correlation IDs

**Configuration Layers:**

* `appsettings.json` – shipped defaults
* `appsettings.{Environment}.json` – environment overrides
* `%APPDATA%/ARIS/settings.json` – user-scoped settings

Always align implementation with the SDDs; do not invent new layers or configuration sources.

---

## Phased Implementation Model

Implementation follows a strict phase order. Work on **one phase at a time**.

**Before starting a phase:**

1. Read `docs/dev/Phase_X_*.md` for the specification
2. Review the checklist in `docs/phase_completion/Phase_X_*_Checklist.md`
3. Consult the relevant SDD in `docs/legacy_resources/`

**While working:**

* Make small, coherent changes (scaffold → logic → tests)
* Run `dotnet build` and `dotnet test` regularly
* Do **not** mix features from multiple phases in the same work chunk

**When complete:**

* Walk through the entire checklist
* Confirm each item explicitly
* If any item is incomplete, the phase is **not** done

**Phase Order:**

* Phase 0: Environment and scaffolding
* Phase 1: Retoc integration
* Phase 2: UAssetAPI integration
* Phase 3: UWPDumper integration
* Phase 4: DLL Injector integration
* Phase 5: Minimal UI and wiring
* Phase 6: Frontend UI (full UX)
* Phase 7: Final wiring and parity pass

---

## Code Style & Constraints

### General Rules

* Use idiomatic C#:

  * `PascalCase` for types/methods
  * `camelCase` for locals/parameters
* Enable nullable reference types and implicit usings in all projects
* Keep code **human-readable and straightforward**
* Prefer clear, simple solutions over clever abstractions

### Comments

* Comment only non-obvious behavior
* Explain **why**, not what
* Focus on trade-offs, invariants, and edge cases
* Never add AI meta-comments or “generated by” markers

### What NOT to Invent

Do **not** add:

* New tools, binaries, or external dependencies not described in the SDDs
* New settings, flags, or environment variables
* New user-visible features or flows
* Design patterns that aren’t justified by the existing architecture

When something is underspecified, propose 1–2 options and ask for direction.

### Error Handling

* Prefer typed results or domain-specific exceptions
* Follow the error strategy described in the Backend and integration SDDs
* Errors must carry enough context for logging and UI surfacing
* Never leak sensitive internals in user-visible messages

### Testing

* Use xUnit for all tests
* Cover success paths and key failure/edge cases
* Prefer integration-style tests over excessive mocking where appropriate
* Tests live in `tests/Aris.Core.Tests/`, `tests/ARIS.UI.Tests/`, and other `tests/Aris.*.Tests/` projects as they are created

---

## File System Behavior

**Safe boundaries:**

* Only read/write within:

  * This repo
  * Explicitly configured ARIS workspaces
* Use `%LOCALAPPDATA%/ARIS/` for tools and app-level data as described in the SDDs

**Never:**

* Delete or modify user files outside documented workspace behavior
* Use hard-coded absolute paths outside `%LOCALAPPDATA%/ARIS/` or workspace directories

When uncertain about file operations, describe intent and wait for confirmation.

---

## Grounding Rules

* Treat SDDs in `docs/legacy_resources/` as **canonical**
* If an SDD and a phase doc conflict, call it out and propose a fix
* Use the original Go/Wails ARIS only as a behavioral reference, not code to translate
* When docs are ambiguous, explicitly note it and offer options
* Never silently guess on important design decisions (APIs, contracts, error policies)

---

## Anti-Patterns to Avoid

* Line-by-line porting from the Go codebase
* Over-engineering with unnecessary abstractions
* Fragmented code that obscures the flow of an operation
* Mixing features from multiple implementation phases
* Adding “AI-scented” patterns or repetitive boilerplate comments
* Creating files named `Manager`, `Helper`, or `Service` without clear justification