# ARIS UWPDumper Integration Software Design Document
Last updated: 2025-12-04  
Audience: Backend engineers, build engineers

## 1. Purpose and Scope
- Describe how ARIS integrates UWPDumper to extract SDKs/mappings from protected UWP games.
- Cover dependency packaging/extraction, command construction, execution wrapper, typed results/errors, configuration, and project-specific workflows.
- Focused on C#/.NET 8 backend per `ARIS_Backend_SDD.md`.

## 2. Role of UWPDumper in ARIS
- Performs UWP app package dumping and metadata extraction required for Unreal-based UWP titles.
- Outputs SDK-like artifacts (headers/mappings) consumed by other ARIS workflows (e.g., conversions).
- Runs as an external elevated process; isolation and auditing are critical.

## 3. Dependency Handling
- **Packaging**: UWPDumper binaries embedded as resources with manifest entries (`id=uwpdumper`, `version`, `sha256`, `relativePath`, `executable=true`).
- **Extraction**:
  - Extract to `%LOCALAPPDATA%/ARIS/tools/{version}/uwpdumper/` on first run or manifest change.
  - Hash-verify each file; temp-then-move for atomicity; lock file records manifest hash.
- **Execution prerequisites**:
  - Windows 10/11 with UWP subsystem present.
  - Elevation (UAC) often required; deny run if elevation unavailable unless explicitly configured for non-elevated diagnostics mode.
  - Verify binary hash before each launch; refuse on mismatch.

## 4. Adapter Design
- Interface: `IUwpDumperAdapter`
  - `Task<UwpDumpResult> DumpAsync(UwpDumpCommand cmd, CancellationToken ct, IProgress<ProgressEvent> progress)`
  - `Task<DependencyStatus> ValidateAsync(CancellationToken ct)`
- Implementation: `UwpDumperAdapter : IUwpDumperAdapter`
  - Builds vetted command line from `UwpDumpCommand`.
  - Uses `IProcessRunner` with elevation support (if required) and strict environment isolation.
  - Streams progress from stdout parsing or step emission; captures logs to workspace.

## 5. Command Construction
- **Command DTO**: `UwpDumpCommand`
  - `PackageFamilyName` or `AppId` (required) — UWP target identifier.
  - `OutputPath` (required) — destination folder under workspace.
  - `Mode` (enum) — e.g., `FullDump`, `MetadataOnly`, `Validate`.
  - `IncludeSymbols` (bool) — dump symbols if supported.
  - `Timeout` — per-invocation cap.
  - `WorkingDirectory` — defaults to workspace `temp/uwp-{opId}/`.
- **Construction rules**:
  - Resolve PFN/AppId via Windows APIs if partial data provided; fail fast if ambiguous.
  - Ensure output directory exists; ensure sufficient free space if estimable.
  - Allow only documented UWPDumper switches; reject arbitrary extras.
  - If elevation is required and not granted, abort with `ElevationRequiredError`.

## 6. Process Wrapper
- `IProcessRunner` configured with:
  - Command: `uwpdumper.exe {args}` (or equivalent main binary).
  - Working directory: per-operation staging; not the binary directory.
  - Environment: minimal, optionally with logging flags (if supported).
  - Timeouts and cancellation: cooperative cancel; hard kill on timeout.
  - Output capture: bounded stdout/stderr buffers; tee to `logs/uwpdumper-{opId}.log`.
  - Elevation: optional flag to trigger UAC prompt; fail closed if denied.

## 7. Typed Results and Error Handling
- **Result model**: `UwpDumpResult`
  - `ExitCode`
  - `PackageFamilyName`
  - `AppId` (if resolved)
  - `OutputPath`
  - `Artifacts` (list of produced files/directories + hashes)
  - `Duration`
  - `Warnings`
  - `LogExcerpt`
- **Progress events**: `ProgressEvent { Step, Message, Percent?, Detail }` at milestones: "Locating package", "Preparing", "Dumping", "Finalizing".
- **Errors**:
  - `ValidationError`: missing PFN/AppId, invalid output path.
  - `DependencyMissingError`: binary missing or hash mismatch.
  - `ElevationRequiredError`: user declined or no elevation available.
  - `ToolExecutionError`: non-zero exit, with `ProcessResult`.
  - `ChecksumMismatchError`: post-dump verification failed.
  - Errors surface as Problem Details with remediation hints (e.g., "run as admin", "verify PFN").

## 8. Configuration
- Options class: `UwpDumperOptions`
  - `DefaultTimeoutSeconds`
  - `RequireElevation` (bool; default true)
  - `AllowedModes`
  - `MaxLogBytes`
  - `StagingRoot` override
- Bound from `appsettings.json` + user overrides; validated at startup.
- Per-command overrides: timeout, mode, include symbols—subject to allowlist.

## 9. Project-Specific Usage Patterns
- **Workspace flow**:
  - Outputs under `workspace/output/uwp/` with operation-scoped folder.
  - Temp/staging under `workspace/temp/uwp-{opId}/`; optionally retained on failure.
- **Downstream use**:
  - Produced SDK/mappings consumed by other ARIS features (e.g., retoc, serialization).
  - Metadata logged and optionally indexed for reuse across sessions.
- **Validation**:
  - Post-run hash outputs; basic structure check (expected dirs/files present).
  - Optional sanity checks: minimal size thresholds to detect empty dumps.

## 10. Logging and Diagnostics
- Per-run log file `logs/uwpdumper-{opId}.log` containing command line (redacted), exit code, duration, and stderr tail on failure.
- Structured fields: operationId, pfn, mode, elevated=true/false, exitCode.
- Optional verbose flag for deeper diagnostics, size-bounded.

## 11. Testing Strategy
- **Unit**: DTO validation, PFN/AppId resolution stubs, allowlist enforcement, elevation requirement logic.
- **Integration**: (where feasible) run against sample or mock UWP package; otherwise simulate with fake process runner.
- **Fault injection**: simulate denied elevation, timeouts, corrupted outputs, non-zero exits.

## 12. Security and Safety
- Enforce path allowlisting to workspace; block traversal.
- Require hash verification of UWPDumper binaries before execution.
- Default to elevation required; explicit opt-out documented and guarded.
- Redact sensitive package identifiers from logs if configured.

## 13. Open Decisions
- Whether to support non-elevated "metadata-only" mode by default.
- Policy for retaining staging/logs on success vs. failure.
- Level of downstream validation for produced artifacts (lightweight vs deep inspection).

---
This SDD defines the integration of UWPDumper into the C#/.NET 8 ARIS backend, emphasizing validated dependency handling, safe elevated execution, typed contracts, and workspace-aligned outputs for downstream Unreal modding workflows.***
