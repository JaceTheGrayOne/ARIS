# Phase 0 Checklist – ARIS C# Rewrite (Environment & Scaffolding)

---

## 1. Environment Ready

- [ ] .NET 8 SDK installed and `dotnet --version` shows 8.x
- [ ] Node.js + npm installed
- [ ] VS Code installed
- [ ] Claude Code extension installed and working in this repo

---

## 2. Solution & Projects Created

From repo root, you should have:

- [ ] `src/ARIS.sln` exists
- [ ] Backend projects exist under `src/`:
  - [ ] `Aris.Core`
  - [ ] `Aris.Application`
  - [ ] `Aris.Infrastructure`
  - [ ] `Aris.Adapters`
  - [ ] `Aris.Contracts`
  - [ ] `Aris.Tools`
  - [ ] `Aris.Hosting`
  - [ ] `ARIS.UI`
- [ ] Test projects exist under `tests/`:
  - [ ] `Aris.Core.Tests`
  - [ ] `ARIS.UI.Tests`
- [ ] Each `.csproj` targets `net8.0` with nullable + implicit usings enabled
- [ ] `dotnet build` from `src/` succeeds

---

## 3. References & Layering Wired

In the solution:

- [ ] `Aris.Application` references `Aris.Core`
- [ ] `Aris.Infrastructure` references `Aris.Core`
- [ ] `Aris.Adapters` references whatever backend projects it needs (at least `Aris.Application` / `Aris.Core`)
- [ ] `Aris.Hosting` references:
  - [ ] `Aris.Application`
  - [ ] `Aris.Infrastructure`
  - [ ] `Aris.Adapters`
  - [ ] `Aris.Contracts`
  - [ ] `Aris.Tools`
- [ ] `ARIS.UI` references `Aris.Hosting` (or the correct host assembly)
- [ ] Test projects reference:
  - [ ] `Aris.Core.Tests` → `Aris.Core`, `Aris.Application`
  - [ ] `ARIS.UI.Tests` → `ARIS.UI` (and `Aris.Hosting` if needed)

---

## 4. Backend Host, Config, and Logging

In `Aris.Hosting`:

- [ ] Generic Host is set up (`Host.CreateDefaultBuilder`)
- [ ] `appsettings.json` exists with:
  - [ ] Logging section
  - [ ] Stub configuration (e.g., `ToolingOptions`, `WorkspaceOptions`)
- [ ] Logging configured via `Microsoft.Extensions.Logging`
  - [ ] Console logging works
  - [ ] File logging writes under something like `%LOCALAPPDATA%/ARIS/logs/`
- [ ] There is an extension or method for:
  - [ ] Registering ARIS services (`AddArisBackend` or equivalent)
- [ ] Running the host (debug or command line) starts and stops cleanly
- [ ] At least one structured log line is written on startup/shutdown

---

## 5. Tool Extraction Subsystem (Stub)

- [ ] `Aris.Tools` contains:
  - [ ] A stub manifest description (e.g., `tools.manifest.json` or equivalent)
  - [ ] A simple way to enumerate manifest entries
- [ ] `Aris.Infrastructure` contains:
  - [ ] A `DependencyExtractor` (or similar) service with methods like:
    - [ ] `PrepareToolsAsync(...)`
    - [ ] `ValidateToolsAsync(...)`
- [ ] Startup path:
  - [ ] `Aris.Hosting` calls `PrepareToolsAsync` at startup
  - [ ] Stub implementation:
    - [ ] Creates (or logs) a tools directory under `%LOCALAPPDATA%/ARIS/tools/{version}/`
    - [ ] Logs what it would do, without needing real binaries yet

---

## 6. Frontend Scaffold (React/TS/Tailwind)

In `frontend/`:

- [ ] `package.json` exists with:
  - [ ] React + ReactDOM
  - [ ] TypeScript
  - [ ] Tailwind CSS
  - [ ] Vite (or chosen bundler)
- [ ] `tsconfig.json` exists
- [ ] `tailwind.config.*` references `index.html` and `src/**/*`
- [ ] `postcss.config.*` exists (Tailwind + autoprefixer)
- [ ] `src/App.tsx` renders a simple “ARIS Frontend – Phase 0” UI
- [ ] NPM commands work:
  - [ ] `npm install`
  - [ ] `npm run build` (produces `frontend/dist/`)

---

## 7. WebView2 Host Wiring (Minimal)

In `ARIS.UI`:

- [ ] WebView2 NuGet package is referenced
- [ ] Main window/form has a WebView2 control
- [ ] WebView2 loads:
  - [ ] Either `frontend/dist/index.html` via `file:///` path
  - [ ] Or the dev server URL (e.g., `http://localhost:5173`) for now
- [ ] UI shows:
  - [ ] The Phase 0 frontend
  - [ ] Some tiny status indicator like “Backend: not connected (Phase 0)”

---

## 8. Single-File Publish Smoke Test

- [ ] Chosen entrypoint for publish:
  - [ ] `Aris.Hosting` **or**
  - [ ] `ARIS.UI`
- [ ] From the project folder, `dotnet publish` succeeds with:
  - [ ] `-c Release -r win-x64 --self-contained true /p:PublishSingleFile=true`
- [ ] Output folder contains:
  - [ ] A single main executable for the chosen project
- [ ] Running that executable:
  - [ ] Starts up without crashing
  - [ ] Reaches the same behavior as debug (host and/or UI with Phase 0 frontend)

---

## 9. Phase 0 “Done” Snapshot

When all are checked:

- [ ] Solution + projects exist and build
- [ ] Basic logging and config are in place
- [ ] Tool extraction subsystem skeleton exists and is called
- [ ] Frontend builds and is hosted by WebView2
- [ ] Single-file publish has been verified once

At this point, you’re ready to move into **Phase 1 – Retoc integration** without battling scaffolding or environment gremlins.
