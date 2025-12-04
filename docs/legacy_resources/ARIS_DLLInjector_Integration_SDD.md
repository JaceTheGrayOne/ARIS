# ARIS DLL Injector Integration Software Design Document
Last updated: 2025-12-04  
Audience: Backend engineers, build engineers

## 1. Purpose and Scope
- Describe how ARIS integrates its custom DLL injector for runtime code injection into target game processes.
- Cover dependency packaging/extraction, command construction and validation, process wrapper, typed results/errors, configuration, and project-specific workflows.
- Target environment: C#/.NET 8 backend per `ARIS_Backend_SDD.md`.

## 2. Role of the DLL Injector in ARIS
- Injects ARIS-provided DLLs into running game processes to enable instrumentation, hooks, or runtime patches.
- Provides process discovery, validation, and safe injection/cleanup workflows.
- Runs as an external helper to isolate risk and support elevation where required.

## 3. Dependency Handling
- **Packaging**: Injector binary (and supporting DLLs if any) embedded as resources with manifest entries (`id=dllinjector`, `version`, `sha256`, `relativePath`, `executable=true`).
- **Extraction**:
  - Extract to `%LOCALAPPDATA%/ARIS/tools/{version}/dllinjector/` on first run or manifest change.
  - Verify SHA-256 per file; temp-then-move; lock file records manifest hash.
- **Pre-run checks**:
  - Hash-verify executable before each launch.
  - Ensure target injection DLLs (payload) are present, hashed, and signed if applicable.
  - Validate architecture compatibility (x64 only).

## 4. Adapter Design
- Interface: `IDllInjectorAdapter`
  - `Task<DllInjectResult> InjectAsync(DllInjectCommand cmd, CancellationToken ct, IProgress<ProgressEvent> progress)`
  - `Task<DllEjectResult> EjectAsync(DllEjectCommand cmd, CancellationToken ct, IProgress<ProgressEvent> progress)`
  - `Task<DependencyStatus> ValidateAsync(CancellationToken ct)`
- Implementation: `DllInjectorAdapter : IDllInjectorAdapter`
  - Builds vetted command lines for injection/ejection.
  - Uses `IProcessRunner` with optional elevation and environment isolation.
  - Parses stdout/stderr for status; emits step-level progress.

## 5. Command Construction
- **Injection DTO**: `DllInjectCommand`
  - `ProcessId` or `ProcessName` (required; resolves to PID).
  - `DllPath` (required) — payload to inject.
  - `Method` (enum): `CreateRemoteThread`, `APC`, `ManualMap` (depending on injector capabilities).
  - `Timeout`
  - `WorkingDirectory` (defaults to workspace `temp/inject-{opId}/`).
  - `RequireElevation` (bool; default true if target protected).
  - `Arguments` for injected DLL entrypoint (allowlist or empty).
- **Ejection DTO**: `DllEjectCommand`
  - `ProcessId` or `ProcessName`
  - `DllPath` or module name to unload.
  - `Timeout`
- **Construction rules**:
  - Resolve and validate PID; verify process architecture (x64) and state (running, not system-protected).
  - Validate payload DLL hash against manifest or provided expected hash.
  - Allow only safe entrypoint arguments; reject arbitrary strings to avoid injection abuse.
  - Deny if target is non-ARIS-allowed executable (allowlist/denylist).

## 6. Process Wrapper
- Command: `dllinjector.exe inject|eject {args}`.
- Working directory: per-operation staging folder; not binary directory.
- Environment: minimal; no inherited PATH modifications except for injector folder.
- Timeouts and cancellation: cooperative cancel; hard kill on timeout.
- Output capture: bounded stdout/stderr; tee to `logs/dllinjector-{opId}.log`.
- Elevation: optional flag; abort with `ElevationRequiredError` if denied when required.
- Exit policy: non-zero exit -> `ToolExecutionError` with captured output and command line (redacted).

## 7. Typed Results and Error Handling
- **Injection result**: `DllInjectResult`
  - `ExitCode`
  - `ProcessId`
  - `ProcessName`
  - `DllPath`
  - `Method`
  - `Duration`
  - `Warnings`
  - `LogExcerpt`
- **Ejection result**: `DllEjectResult`
  - `ExitCode`, `ProcessId`, `ProcessName`, `DllPath`, `Duration`, `Warnings`, `LogExcerpt`
- **Progress events**: `ProgressEvent { Step, Message, Percent?, Detail }` for "Resolving process", "Validating payload", "Injecting", "Verifying", "Finalizing".
- **Errors**:
  - `ValidationError`: bad PID/name, missing DLL, incompatible arch, disallowed target.
  - `DependencyMissingError`: injector binary missing/hash mismatch.
  - `ElevationRequiredError`: when elevation denied/absent.
  - `ToolExecutionError`: non-zero exit or unexpected stderr.
  - `ChecksumMismatchError`: payload hash mismatch.
  - Errors mapped to Problem Details with remediation hints (e.g., "run as admin", "use x64 payload").

## 8. Configuration
- Options class: `DllInjectorOptions`
  - `DefaultTimeoutSeconds`
  - `RequireElevation` (bool; default true)
  - `AllowedTargets` (allowlist/regex)
  - `DeniedTargets` (explicit denylist: system processes, services)
  - `AllowedMethods`
  - `MaxLogBytes`
  - `StagingRoot`
- Bound from `appsettings.json` + user overrides; validated at startup.
- Per-command overrides: timeout, method selection, elevation opt-in/out subject to guard rails.

## 9. Project-Specific Usage Patterns
- **Workspace flow**:
  - Payload DLLs stored under `workspace/input/payloads/` or shipped with app (`dependencies/payloads/`).
  - Logs under `workspace/logs/dllinjector-{opId}.log`.
  - Temporary files under `workspace/temp/inject-{opId}/`; optionally retained on failure.
- **Target resolution**:
  - UI provides process list filtered by allowlist; backend re-validates before executing.
  - For recurring targets, ARIS may store friendly names mapped to process names; re-validate on each run.
- **Post-injection verification**:
  - Optional: query target modules to confirm DLL loaded (using injector output or secondary check).
  - Record outcome and hashes for audit trail.

## 10. Logging and Diagnostics
- Structured logs per operation: operationId, pid, processName, dllHash, method, elevated flag, exitCode, duration.
- Redact sensitive command-line arguments; never log raw payload paths if configured to redact.
- On failure, include stderr tail and first/last N lines of stdout.

## 11. Testing Strategy
- **Unit**: DTO validation, allowlist/denylist enforcement, hash checks, elevation requirement logic.
- **Integration**: Use a test harness process to inject a benign test DLL; verify load/unload and module presence.
- **Fault injection**: simulate denied elevation, bad PID, wrong arch, hash mismatch, timeouts.
- **Safety checks**: ensure denylist blocks system processes in tests.

## 12. Security and Safety
- Strict allowlist/denylist for targets; block system/critical processes by default.
- Enforce payload hash verification; optionally require code signing.
- Prefer unelevated execution; request elevation only when necessary.
- No arbitrary arguments; only validated entrypoint parameters.
- Bounded logs and redaction to avoid leaking sensitive data.

## 13. Open Decisions
- Whether to enforce code-signing for payload DLLs in addition to hashing.
- Default injection method priority (manual map vs. CreateRemoteThread) per target type.
- Policy for retaining staging/logs on success.

---
This SDD defines how the custom DLL injector is integrated into the C#/.NET 8 ARIS backend with validated dependency handling, safe process execution, typed contracts, and workspace-aligned workflows for reliable runtime injection and ejection.***
