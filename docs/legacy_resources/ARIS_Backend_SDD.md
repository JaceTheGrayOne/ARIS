# ARIS Backend Software Design Document (C# Rewrite)
Last updated: 2025-12-04  
Audience: Backend engineers, architects, DevOps

## 1. Purpose and Scope
- Define the target architecture and scaffolding for a full backend rewrite of ARIS in C#/.NET.
- Enable development without access to legacy Go/Wails sources by describing behaviors, responsibilities, and interfaces.
- Cover universal concerns (application core, dependency embedding/extraction, configuration, error handling, logging, security, packaging).
- Outline—but do not deep-dive—tool-specific integrations (retoc, UAssetAPI, UWPDumper, DLL Injector).

## 2. System Overview
- **Goal**: Windows-first desktop backend orchestrating Unreal Engine modding workflows (IoStore package mgmt, UAsset serialization, DLL injection, UWP dumping).
- **Runtime**: .NET 8 (LTS), C# 12, x64, Windows 10/11. Enable single-file, self-contained publish.
- **Process model**: Single host process with isolated service modules; each module may launch external tools with strict process sandboxing.
- **Frontend contract**: Expose a typed IPC/HTTP bridge consumable by a desktop UI (e.g., WebView2/React or future native UI). Transport may be:
  - Embedded Kestrel HTTP + JSON (default).
  - In-proc message bus abstraction to allow swapping to WebView2-hosted JS bridge if desired.
- **Key non-functionals**: Deterministic startup (<2s cold with extraction cache), idempotent dependency preparation, transactional workspace writes, traceable operations, resumable long-running tasks.

## 3. Architectural Style
- **Layered + Modular**: `Presentation (IPC) -> Application Services -> Domain -> Infrastructure`.
- **Ports and Adapters**: Define contracts for filesystem, process execution, crypto, compression, and tool adapters; provide default Windows adapters.
- **Pipelines for tasks**: Long-running operations modeled as command handlers with step-level progress + cancellation tokens.
- **Configuration-as-code**: Strongly typed options bound at startup; mutable user settings persisted separately from static app configuration.

## 4. Solution Structure (proposed)
```
src/
  Aris.Backend.sln
  Aris.Core/              // domain primitives, value objects, errors
  Aris.Application/       // use-cases, orchestrations, progress, transactions
  Aris.Infrastructure/    // filesystem, process, compression, crypto, logging, settings stores
  Aris.Adapters/          // tool-specific adapters (retoc, UAssetAPI, UWPDumper, DLL Injector)
  Aris.Hosting/           // composition root, DI, configuration, single-file host, IPC/HTTP bridge
  Aris.Contracts/         // DTOs shared with frontend (generated TS models optional)
  Aris.Tests/             // unit + integration
  Aris.Tools/             // embedded binaries/resources packaging helpers
```

## 5. Composition and Dependency Injection
- **Container**: Use `Microsoft.Extensions.DependencyInjection`.
- **Lifetime rules**:
  - Singleton: configuration providers, embedded resource catalog, extraction cache manager, logger factory.
  - Scoped: operation pipelines (per request/command), transactional workspace contexts.
  - Transient: pure utilities without state.
- **Registration**: Centralized in `Aris.Hosting/Startup` with extension methods per module (`AddCore`, `AddInfrastructure`, `AddToolingAdapters`).
- **Cross-cutting interceptors**: Logging, metrics, retries/timeouts, and circuit breakers applied via decorators (e.g., `Polly`).

## 6. Application Core
- **Domain model**:
  - `AssetPackage`, `ExtractionPlan`, `InjectionTarget`, `DumpJob`, `WorkspacePath`, `ToolHandle`, `DependencyId`, `Checksum`.
  - Use value objects for paths and IDs to prevent path traversal and invalid state.
- **Command/Query contracts**:
  - Commands: `PrepareDependencies`, `ConvertPackage`, `SerializeAsset`, `InjectDll`, `DumpUwpSdk`.
  - Queries: `ListWorkspaces`, `InspectPackage`, `ValidateDependencies`, `ListInstalledTools`.
- **Pipelines**:
  - Each command has a handler accepting a `CancellationToken` and progress reporter; steps emit `ProgressEvent` records.
  - Supports dry-run mode where filesystem/process actions are simulated for diagnostics.
- **Transactions**:
  - Use a lightweight "unit of work" per operation; writes occur in a temp staging area then atomically moved into workspace.
  - Rollback strategy: delete staging, restore backups of mutated files when possible.
- **Error model**:
  - Domain exceptions: `ValidationError`, `DependencyMissingError`, `ToolExecutionError`, `ChecksumMismatchError`.
  - Surface errors to frontend as typed problem details with operation id and remediation hints.

## 7. Dependency Embedding and Extraction
- **Packaging**:
  - Store third-party binaries/resources as embedded resources in `Aris.Tools` (or as content files) with a manifest:
    - `id`, `version`, `platform`, `sha256`, `size`, `relativePath`, `executable` flag.
  - Build step generates a manifest (`tools.manifest.json`) and compile-time resource index class for lookups.
  - Prefer single-file publish but keep extraction paths external to avoid locked-file issues during updates.
- **Extraction strategy**:
  - On first run or when manifest checksum changes:
    - Determine extraction root: `%LOCALAPPDATA%/ARIS/tools/{version}/`.
    - For each manifest entry: check existing file hash; skip if matches; otherwise extract stream to temp file, verify SHA-256, then move.
    - Mark extracted executables with `File.SetAttributes` ensuring no ADS or hidden flags unless required.
  - Maintain lock file with manifest hash to short-circuit on subsequent launches.
  - Provide `DependencyValidator` service to re-verify hashes and repair missing/corrupt files.
- **Updates**:
  - New release ships with updated manifest; old versions remain side-by-side to support rollback.
  - Optional `--clean-tools` CLI flag to purge older tool caches.

## 8. Dependency Execution Policy
- **Process runner**:
  - Wrapper over `System.Diagnostics.Process` with:
    - Explicit working directory.
    - Bounded timeouts and cancellation.
    - Environment isolation (whitelisted variables).
    - Stream capture (stdout/stderr) with size limits and line-level timestamps.
  - Return structured `ProcessResult { ExitCode, StdOut, StdErr, Duration, CommandLine }`.
- **Security**:
  - Deny execution if binary hash mismatches manifest.
  - Allowlist arguments per tool adapter to prevent arbitrary injection (especially for DLL injector).
  - Opt-in elevation path (UAC) only for features that require it; otherwise run unelevated.

## 9. Configuration and Settings
- **Layers**:
  - `appsettings.json` (read-only defaults, shipped with app).
  - `appsettings.{Environment}.json` (optional overrides).
  - User-scoped settings in `%APPDATA%/ARIS/settings.json`.
  - Command-line overrides for headless/CLI use.
- **Binding**: Strongly typed options classes (`ToolingOptions`, `WorkspaceOptions`, `LoggingOptions`) bound at startup and validated with `DataAnnotations` + custom validators.
- **Secrets**: Avoid storing secrets; if future credentials are required, use DPAPI user-protected store.
- **Reload**: Watch user settings file for changes; publish a settings-changed event to allow adapters to refresh.

## 10. Logging, Telemetry, and Diagnostics
- **Logging**:
  - `Microsoft.Extensions.Logging` with sinks: rolling file (default), console (diagnostic), and optional ETW.
  - Structured logs with operation ids and correlation ids flowing through command handlers.
  - Redact paths or PII-like data when logging user content.
- **Metrics (optional)**:
  - Lightweight in-process counters (operations, failures, durations) exposed via diagnostics endpoint or event source.
- **Tracing**:
  - Activity spans around process executions and file IO; propagate to frontend for user-facing progress.
- **Crash capture**:
  - Global exception handler producing minidumps and log bundles in `%LOCALAPPDATA%/ARIS/logs/`.

## 11. IPC/HTTP Bridge
- **Transport**: Kestrel-hosted HTTP/JSON on a random localhost port with auth token, or named pipes if embedding in a desktop host.
- **API surface**:
  - `/operations/*` for commands (POST with payload).
  - `/operations/{id}/events` for Server-Sent Events or WebSocket progress.
  - `/health` and `/info` endpoints for readiness/liveness.
- **Contracts**:
  - DTOs defined in `Aris.Contracts`; generate optional TypeScript definitions for frontend parity.
  - Use Problem Details (RFC 9457) for error responses.

## 12. Filesystem and Workspace Handling
- **Workspace model**:
  - A workspace is a root folder containing source assets, staging, output, and logs.
  - Standard subfolders: `input/`, `output/`, `temp/`, `logs/`, `backups/`.
  - Enforce canonical paths using `WorkspacePath` value object; reject traversal (`..`, UNC) unless explicitly allowed.
- **Atomicity**:
  - Write to `temp/` then move to `output/`; keep `backups/` for in-place transforms.
  - Hash verification before and after long operations to detect corruption.
- **Concurrency**:
  - File locks around workspace operations; advisory locks via lock files to coordinate multiple UI sessions.

## 13. Tool Adapter Outlines (no deep detail)
- **retoc (IoStore/PAK conversions)**:
  - Adapter interface: `IRetocAdapter { Task<ProcessResult> ConvertAsync(RetocCommand cmd, ...) }`.
  - Validates input paths, key files, and target UE version; streams progress.
- **UAssetAPI (serialization)**:
  - Wraps managed library calls if available; otherwise drives CLI converter.
  - Ensures correct version mappings; provides JSON schema versioning for outputs.
- **UWPDumper (SDK/mappings extraction)**:
  - Runs elevated only when required; captures output artifacts into workspace `output/uwp/`.
- **DLL Injector**:
  - Supports process discovery, optional handle elevation, and inject/detach flows.
  - Enforces signed binary validation (hash against manifest) before injection.

## 14. Testing Strategy
- **Unit**: Domain and application services with fake adapters; deterministic file system fakes.
- **Integration**: Process runner against embedded tools in a temp workspace; hash assertions on outputs.
- **Contract**: API surface tested with snapshot/contract tests; DTO compatibility with frontend models.
- **Performance**: Smoke benchmarks for dependency extraction and representative conversions.

## 15. Deployment and Packaging
- **Publishing**:
  - `dotnet publish -c Release -r win-x64 --self-contained true /p:PublishSingleFile=true /p:EnableCompressionInSingleFile=true`.
  - Include manifest and embedded resources; verify post-publish hashes.
- **Installer options**:
  - Zip (portable) or MSIX/Setup bootstrapper that places files under `Program Files/ARIS`.
- **First-run flow**:
  - Validate environment (Windows version, disk space), extract dependencies, run self-check, register IPC port, then accept frontend connections.

## 16. Security Considerations
- Restrict process execution to manifest-known binaries; enforce SHA-256 verification.
- Sanitize all user-supplied paths/arguments; block network access for child processes unless explicitly allowed.
- Prefer unelevated execution; require explicit consent before elevation.
- Sign distributed binaries; verify signature when starting helper processes if applicable.

## 17. Migration Notes from Legacy
- The legacy Go/Wails backend embedded dependencies and extracted on first run; preserve that behavior with manifest-driven extraction.
- Preserve four primary capability areas (IoStore conversions, UAsset serialization, DLL injection, UWP dumping) while modernizing orchestration and telemetry.
- Replace Wails bridge with HTTP/pipe bridge but keep a thin compatibility layer to minimize frontend changes (provide TS contract generation).

## 18. Open Questions / Decisions to Confirm
- Do we target only Windows or keep a stub for Linux/macOS (some tools are Windows-only)?
- Should IPC use HTTP+SSE or named pipes by default for local-only communication?
- Level of elevation support for DLL injection and UWPDumper; is UAC prompting acceptable in UX?
- Do we ship both single-file and split-file builds for faster cold start vs. simpler patching?

---
This document defines the scaffolding and universal behaviors required to rebuild the ARIS backend in C#. Module-level design docs for each tool integration can extend Section 13 with detailed flows and data contracts.
