# ARIS Retoc Integration Software Design Document
Last updated: 2025-12-04  
Audience: Backend engineers, build engineers

## 1. Purpose and Scope
- Describe how ARIS integrates the Retoc toolchain for IoStore/PAK conversions and key handling.
- Cover dependency packaging/extraction, command construction, execution wrapper, typed results/errors, configuration, and project-specific workflows.
- Assumes C#/.NET 8 backend per `ARIS_Backend_SDD.md`.

## 2. Role of Retoc in ARIS
- Performs Unreal IoStore/PAK conversions, decryption/encryption, mount key application, and format normalization.
- Acts as a black-box CLI invoked by ARIS; outputs packages and logs that are post-processed by ARIS.
- Run inside workspace context with deterministic staging to preserve source data and enable rollback.

## 3. Dependency Handling
- **Packaging**: Retoc binaries (CLI and supporting DLLs) are embedded as resources with manifest entries (`id=retoc`, `version`, `sha256`, `relativePath`, `executable=true`).
- **Extraction**:
  - On startup or manifest change, Retoc files are extracted to `%LOCALAPPDATA%/ARIS/tools/{version}/retoc/`.
  - Extraction verifies SHA-256 per manifest; skips identical files; writes via temp-then-move for atomicity.
  - Lock file records manifest hash; `DependencyValidator` can repair missing/corrupt Retoc files.
- **Execution preflight**:
  - Verify extracted binary hash before each launch.
  - Ensure working directory points to workspace or temp staging to avoid writes to binary location.
  - Optionally set `PATH` to include Retoc folder only for the launched process (not globally).

## 4. Adapter Design
- Interface: `IRetocAdapter`
  - `Task<RetocResult> ConvertAsync(RetocCommand command, CancellationToken ct, IProgress<ProgressEvent> progress)`
  - `Task<DependencyStatus> ValidateAsync(CancellationToken ct)` (hash/availability check)
- Implementation: `RetocAdapter : IRetocAdapter`
  - Builds command line from typed `RetocCommand`.
  - Uses `IProcessRunner` to execute with stdout/stderr capture, timeouts, and cancellation.
  - Parses structured output (if available) and falls back to line scanning for key events.
  - Publishes step-level progress events (staging, decrypt, convert, re-encrypt, finalize).

## 5. Command Construction
- **Command DTO**: `RetocCommand`
  - `InputPath` (required) — source pak/ucas/utoc.
  - `OutputPath` (required) — target pak/ucas/utoc.
  - `Mode` (enum): `PakToIoStore`, `IoStoreToPak`, `Repack`, `Validate`.
  - `MountKeys` (collection) — key IDs/values; resolved via `KeyStore`.
  - `GameVersion`/`UEVersion` (enum or string) — affects key selection and flags.
  - `Compression` options: format, level, block size.
  - `Filters`: include/exclude globs for selective repack.
  - `AdditionalArgs`: vetted allowlist of extra switches.
  - `WorkingDirectory`: defaults to workspace `temp/retoc-{opId}/`.
  - `Timeout`: per-invocation max duration.
- **Construction rules**:
  - Paths are normalized to absolute, verified to exist (inputs), and output parent directories ensured.
  - `Mode` drives subcommand selection and specific flags.
  - Mount keys injected via `--aes-key` or Retoc-supported key syntax; reject if missing or mismatched.
  - Filters mapped to repeated `--include/--exclude` args after validation to prevent shell injection.
  - Additional args only from allowlist; otherwise rejected with `ValidationError`.

## 6. Process Wrapper
- Uses shared `IProcessRunner` (see backend SDD) with:
  - Command: `retoc.exe {args}`.
  - Environment: minimal; may set `REToc_LOG_JSON=1` if Retoc supports structured logs.
  - Working directory: per-operation staging folder.
  - Timeouts: command-level timeout; hard kill with graceful cancel when `ct` triggered.
  - Output capture: bounded stdout/stderr buffers; optional tee to rolling file in workspace `logs/retoc-{opId}.log`.
  - Exit policy: non-zero exit => `ToolExecutionError` with captured output, exit code, and offending command line.

## 7. Typed Results and Error Handling
- **Result model**: `RetocResult`
  - `ExitCode`
  - `OutputPath`
  - `OutputFormat` (pak/iostore)
  - `Duration`
  - `Warnings` (collection)
  - `ProducedFiles` (list of file metadata + hashes)
  - `LogExcerpt` (truncated)
- **Progress events**: `ProgressEvent { Step, Message, Percent?, Detail }` emitted at major milestones.
- **Errors**:
  - `ValidationError`: bad args, missing paths, invalid keys.
  - `DependencyMissingError`: Retoc binary missing or hash mismatch.
  - `ToolExecutionError`: non-zero exit; includes `ProcessResult`.
  - `ChecksumMismatchError`: post-op hash check failed.
  - All errors surfaced to frontend as Problem Details with remediation hints (e.g., "verify AES key").

## 8. Configuration
- Options class: `RetocOptions`
  - `DefaultTimeoutSeconds`
  - `DefaultCompression`
  - `AllowedAdditionalArgs` (allowlist)
  - `MaxLogBytes`
  - `StagingRoot` override (else workspace temp)
  - `EnableStructuredLogs` flag
- Bound from `appsettings.json` + user overrides; validated at startup.
- Per-command overrides allowed where safe (e.g., timeout, filters), subject to guards.

## 9. Project-Specific Usage Patterns
- **Workspace flow**:
  - Inputs placed in `workspace/input/`.
  - Outputs written to `workspace/output/retoc/` with operation-scoped folder.
  - Temp/staging in `workspace/temp/retoc-{opId}/`; cleaned on success or retained on failure for forensics (configurable).
- **Key management**:
  - AES keys stored in `KeyStore`; resolved by game/UE version; never logged in plaintext.
  - Validation step ensures required keys exist before invoking Retoc.
- **Validation and post-checks**:
  - After conversion, ARIS verifies output hashes and expected files (`*.pak`/`*.utoc`/`*.ucas` combos).
  - For `Validate` mode, Retoc output is parsed and reported as structured `Warnings`/`Errors`.
- **Progress/UI**:
  - Progress events mapped to UI steps: "Staging", "Decrypting", "Converting", "Re-encrypting", "Finalizing".
  - Log tail streamed on demand for troubleshooting.

## 10. Logging and Diagnostics
- All executions logged to `logs/retoc-{opId}.log` with command line (arguments redacted for keys), exit code, duration.
- Structured fields: operationId, mode, ueVersion, inputPath hash, outputPath hash, exitCode.
- On failure, include first/last N lines of stderr in the error payload.

## 11. Testing Strategy
- **Unit**: Command builder validation (paths, keys, allowlist), option binding, progress mapping.
- **Integration**: Run Retoc against sample fixtures in a temp workspace; assert outputs and hashes match expectations.
- **Fault injection**: Simulate non-zero exits, timeouts, missing keys, and hash mismatch during extraction.

## 12. Security Considerations
- Enforce path allowlisting to workspace; block traversal.
- Redact AES keys from logs; never echo in stdout/stderr forwarded to UI.
- Hash-verify Retoc binaries before each run; refuse execution on mismatch.
- Disallow arbitrary extra args; only allowlist.

## 13. Open Decisions
- Whether to default to structured logging (`REToc_LOG_JSON`) if supported.
- Policy for retaining staging folders on failure (always, size-capped, or user toggle).
- Maximum default timeout for large conversions; may scale with input size heuristics.

---
This SDD defines how ARIS will integrate and operate Retoc in the C# backend, ensuring deterministic packaging, validated execution, typed results, and project-specific workflow alignment.
