# ARIS High-Level Design SDD
Last updated: 2025-12-04  
Audience: Engineering leads, stakeholders, integrators

## 1. Purpose and Scope
- Provide a concise architectural overview of ARIS, its core capabilities, and guiding design methodology for the C#/.NET 8 rewrite.
- Summarize major components (backend, tooling integrations, frontend), cross-cutting concerns, and operational practices without deep dives (covered in module SDDs).
- Capture general project considerations: licensing posture, packaging, deployment, and support expectations.

## 2. Product Summary
- **Name**: ARIS (Asset Reconfiguration and Integration System)
- **Platform**: Windows 10/11 x64
- **Mission**: Provide a unified desktop experience for Unreal Engine modding workflows, packaging, asset conversion, runtime injection, and UWP extraction.
- **Primary Functions**:
  - IoStore/PAK conversions (Retoc)
  - UAsset serialization/inspection (UAssetAPI, now native)
  - DLL injection helper (custom injector)
  - UWP dumping for SDK/mappings (UWPDumper)
  - Centralized progress/logging, workspace management, and settings

## 3. Architectural Overview
- **Backend**: C#/.NET 8 service hosted in a single process; modular services per capability; IPC via local HTTP/JSON or named pipes; progress streaming via SSE/WebSocket.
- **Frontend**: Desktop web-based UI (e.g., WebView2) consuming typed contracts; dark, panelized layout with progress/log-centric workflows.
- **Integration Style**: Ports-and-adapters; external tools invoked through validated process wrappers; domain-centric application services coordinate orchestration, staging, and verification.
- **Deployment**: Self-contained single-file publish (preferred) with embedded dependencies extracted at first run to `%LOCALAPPDATA%/ARIS/tools/{version}/`.
- **Workspaces**: Standardized folder layout (input/output/temp/logs/backups); operations run transactionally with temp-then-move semantics.

## 4. Design Methodology and Principles
- **Safety first**: Hash verification for embedded tools; allowlist arguments; default to unelevated execution; explicit consent for elevation.
- **Determinism**: Idempotent dependency extraction; atomic writes; hash-based validation of outputs.
- **Observability**: Structured logs, progress events, correlation ids; per-operation logs persisted in workspace.
- **Separation of concerns**: Clear layers (presentation, application, domain, infrastructure) and explicit contracts for tool adapters.
- **User experience**: Provide a dark, high-contrast, panelized UI with consistent progress and log visibility and fast startup, without trying to preserve the exact visual feel of the legacy app.
- **Extensibility**: Module-specific SDDs define adapters; contracts generated for the frontend; new tools can register via DI extensions and manifests.

## 5. Major Components (Summary)
- **Core Services**: Workspace manager, dependency manager (embedding/extraction/validation), process runner, configuration/options loader, logging/telemetry.
- **Tool Adapters**: Retoc, UAssetAPI (in-proc), UWPDumper, DLL Injector—each with typed commands/results and guarded execution.
- **Frontend Client**: Routed desktop UI with sections for Dashboard, tool workflows, Settings, Logs; consumes backend APIs and progress streams.
- **Packaging**: Embedded manifests for all third-party binaries; optional NuGet caching; single installer or portable zip.

## 6. Licensing and Compliance (General)
- **Project License**: Refer to `LICENSE` (existing). Ensure the new C# codebase inherits the same license unless changed intentionally.
- **Third-Party Tools**:
  - **Retoc**: Respect upstream license; include notice in `docs/licenses/`.
  - **UAssetAPI**: MIT (at time of writing); include license text and version in notices.
  - **UWPDumper**: Include upstream license and attribution.
  - **Custom DLL Injector**: Internal; no third-party license beyond system APIs.
- **NuGet Packages**: Track licenses for dependencies (e.g., Microsoft.Extensions.*, Polly, logging sinks); generate a license report at build time and ship in `docs/licenses/`.
- **Attribution**: Maintain `docs/licenses/` with per-component notices; surface summary in About/Info UI page.

## 7. Configuration and Settings (Summary)
- Layered configuration: `appsettings.json`, environment-specific overrides, user-scoped settings in `%APPDATA%/ARIS/`, and command-line flags.
- Typed options per module with validation at startup; hot-reload user settings where safe.

## 8. Deployment, Updates, and First-Run
- Build: `dotnet publish -c Release -r win-x64 --self-contained true /p:PublishSingleFile=true`.
- First run: verify environment, extract dependencies, run self-check, expose IPC endpoint, update UI with readiness status.
- Updates: versioned tool cache paths permit side-by-side rollback; `--clean-tools` option to purge old caches.

## 9. Security Posture (Summary)
- Hash verification for all embedded tools before execution.
- Allowlist arguments for external tools; redaction of sensitive data in logs (keys, paths where configured).
- Elevation only when required (DLL injector, UWPDumper); user consent prompts; deny if not granted.
- Workspace path normalization to prevent traversal; default deny non-workspace targets.

## 10. Observability and Support
- Per-operation logs and progress events; central logs viewer in frontend.
- Crash handling: global exception capture and log bundle in `%LOCALAPPDATA%/ARIS/logs/`.
- Support bundle export: recent logs + operation metadata (no secrets).

## 11. Testing (Summary)
- Unit tests for command validation, configuration binding, process runner policies.
- Integration tests for each tool adapter with fixtures; hash verification on outputs.
- Contract tests for API surface and progress streaming; optional visual regression for UI.

## 12. Open/General Considerations
- Default IPC transport choice (HTTP+SSE vs named pipes) to be finalized.
- Elevation UX and policy confirmation for sensitive operations.
- Decide on optional telemetry/metrics exposure (local-only) and default logging verbosity.
- Confirm distribution format: installer vs portable, and code signing requirements for executables.

---
This document provides a top-level view of ARIS’s architecture, principles, and operational posture. Module-specific details are defined in their respective SDDs.***
