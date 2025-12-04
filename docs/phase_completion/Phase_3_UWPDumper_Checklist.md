# Phase 3 Checklist – UWPDumper Integration (ARIS C# Rewrite)

This is a **human checklist** mirroring `Phase_3_UWPDumper_Integration.md`.

Use it to verify that UWPDumper is actually integrated, testable, and not just name-dropped in the codebase.

---

## 1. Preconditions

- [ ] Phase 0 checklist is complete
- [ ] Phase 1 (Retoc) checklist is complete
- [ ] Phase 2 (UAssetAPI) checklist is complete
- [ ] Backend builds cleanly (`dotnet build` from `src/`)
- [ ] `ARIS_UWPDumper_Integration_SDD.md` exists in `docs/` and has been read at least once
- [ ] Dev environment can run elevated processes (UAC prompts work)

---

## 2. Tool Packaging & Extraction

**Manifest:**

- [ ] Tools manifest (e.g. in `Aris.Tools`) contains a **UWPDumper** entry with:
  - [ ] `id = "uwpdumper"`
  - [ ] `version` set
  - [ ] `sha256` for the main executable
  - [ ] `relativePath` (e.g. `uwpdumper/uwpdumper.exe`)
  - [ ] Any required support binaries listed with hashes

**Extraction:**

- [ ] `DependencyExtractor` (or equivalent) extracts UWPDumper to:
  - [ ] `%LOCALAPPDATA%/ARIS/tools/{version}/uwpdumper/`
- [ ] Extracted files use temp-then-move semantics
- [ ] Hash verification is performed after extraction
- [ ] A lock/marker file is written so future runs can skip re-extracting unchanged files

**Validation:**

- [ ] A UWPDumper-specific validation step exists
- [ ] Missing/corrupted UWPDumper triggers a clear `DependencyMissingError` or equivalent
- [ ] Startup logs clearly indicate whether UWPDumper is valid and ready

---

## 3. Command DTO & Validation

**DTO:**

- [ ] `UwpDumpCommand` exists with fields roughly like:
  - [ ] `PackageFamilyName` and/or `AppId`
  - [ ] `OutputPath`
  - [ ] `Mode` (`UwpDumpMode` enum, e.g. `FullDump`, `MetadataOnly`, etc.)
  - [ ] `IncludeSymbols`
  - [ ] Optional `Timeout`
  - [ ] Optional `WorkingDirectory`

**Validation rules:**

- [ ] At least one of `PackageFamilyName` / `AppId` required
- [ ] If both are provided, they must be consistent (no obvious mismatch)
- [ ] `OutputPath` is under `workspace/output/uwp/` (or a clearly defined, allowed root)
- [ ] Output directory exists or is created
- [ ] Mode must be in `UwpDumperOptions.AllowedModes`
- [ ] Timeout is validated against sensible bounds / defaults
- [ ] Any attempt to use unsupported/unknown modes is rejected as `ValidationError`

**Tests:**

- [ ] Unit tests cover:
  - [ ] Missing PFN/AppId
  - [ ] Invalid/ambiguous PFN/AppId cases
  - [ ] Disallowed mode
  - [ ] Bad output path (outside workspace)
  - [ ] Timeout out-of-range

---

## 4. Adapter & Process Integration

**Interface:**

- [ ] `IUwpDumperAdapter` exists with methods like:
  - [ ] `DumpAsync(UwpDumpCommand, CancellationToken, IProgress<ProgressEvent>)`
  - [ ] `ValidateAsync(CancellationToken)`

**Implementation:**

- [ ] `UwpDumperAdapter` class exists
- [ ] Adapter:
  - [ ] Validates `UwpDumpCommand` before running
  - [ ] Validates UWPDumper dependency (hashes, existence)
  - [ ] Builds a **fully allowlisted** command line (no arbitrary passthrough args)
  - [ ] Uses shared `IProcessRunner` abstraction
  - [ ] Uses per-operation working directory (e.g. `workspace/temp/uwp-{operationId}/`)
  - [ ] Captures stdout/stderr with bounded buffers
  - [ ] Tees logs to an operation-specific log file (e.g. `logs/uwpdumper-{operationId}.log`)

**Progress:**

- [ ] `DumpAsync` emits progress events for steps like:
  - [ ] Locating package
  - [ ] Preparing
  - [ ] Dumping
  - [ ] Finalizing

---

## 5. Elevation Handling

**Options-driven behavior:**

- [ ] `UwpDumperOptions.RequireElevation` exists (default `true`)
- [ ] When elevation is required:
  - [ ] Adapter attempts to run UWPDumper elevated
  - [ ] If elevation is denied/unavailable, operation fails with `ElevationRequiredError` (or equivalent) and **does not** run a non-elevated dump
- [ ] If a non-elevated diagnostics mode is supported:
  - [ ] It’s only allowed when explicitly configured in options
  - [ ] Behavior is clearly distinct from full dump

**Tests:**

- [ ] Unit/fault tests simulate:
  - [ ] Elevation required + denied → `ElevationRequiredError`
  - [ ] Elevation not required → runs non-elevated successfully (in allowed modes)

---

## 6. Result Model & Errors

**Result type:**

- [ ] `UwpDumpResult` exists with fields along the lines of:
  - [ ] `ExitCode`
  - [ ] `PackageFamilyName`
  - [ ] `AppId` (if applicable)
  - [ ] `OutputPath`
  - [ ] A list of `Artifacts` (path, type, hash, possibly size)
  - [ ] `Duration`
  - [ ] `Warnings`
  - [ ] Optional `LogExcerpt` (bounded)

**Artifact model:**

- [ ] Each artifact entry describes:
  - [ ] Path
  - [ ] Type (headers, mappings, symbols, etc.)
  - [ ] Hash (if computed)
  - [ ] Optional size

**Error types used:**

- [ ] `ValidationError` for bad command/options
- [ ] `DependencyMissingError` when UWPDumper is missing or hash-mismatched
- [ ] `ElevationRequiredError` when elevation is needed but not available
- [ ] `ToolExecutionError` for non-zero exit codes/timeouts
- [ ] `ChecksumMismatchError` for post-run hash mismatches (if implemented)

**Behavior:**

- [ ] Successful dump returns a populated `UwpDumpResult`
- [ ] Failure returns one of the typed errors, not a generic exception blob
- [ ] Errors carry enough context to debug (reason, hints) without exposing sensitive data

---

## 7. UwpDumperOptions Configuration

**Options object:**

- [ ] `UwpDumperOptions` exists with fields like:
  - [ ] `DefaultTimeoutSeconds`
  - [ ] `RequireElevation`
  - [ ] `AllowedModes` list
  - [ ] `MaxLogBytes`
  - [ ] Optional `StagingRoot`
  - [ ] Optional verbose/diagnostic flags

**Binding & validation:**

- [ ] `UwpDumperOptions` is bound from config (e.g. `appsettings.json` → `UwpDumper` section)
- [ ] Startup-time validation checks:
  - [ ] `DefaultTimeoutSeconds` sane
  - [ ] `MaxLogBytes` sane
  - [ ] `AllowedModes` non-empty and only contains known values
- [ ] Misconfigurations are logged clearly or cause startup failure (not silently ignored)

**Usage:**

- [ ] Adapter actually obeys:
  - [ ] `RequireElevation`
  - [ ] `AllowedModes`
  - [ ] Timeout behavior
  - [ ] Log size limits
  - [ ] `StagingRoot` override, if provided

---

## 8. Workspace & Outputs

**Workspace locations:**

- [ ] Outputs:
  - [ ] Under `workspace/output/uwp/{operationId}/`
- [ ] Temp:
  - [ ] Under `workspace/temp/uwp-{operationId}/` (or configured staging root)

**Post-run behavior:**

- [ ] Expected directories/files are present (headers/mappings/etc., as per SDD)
- [ ] Artifacts have hashes computed (where feasible)
- [ ] Optional extra validations:
  - [ ] Non-empty directories / files
  - [ ] Structural checks (basic sanity)

**Security:**

- [ ] UWPDumper output does not escape workspace boundaries unintentionally
- [ ] Paths are normalized and validated before use

---

## 9. Logging & Diagnostics

**Per-run logging:**

- [ ] For each run, a log file is created (e.g. `logs/uwpdumper-{operationId}.log`)
- [ ] Contains:
  - [ ] Redacted command line (no secrets)
  - [ ] Exit code
  - [ ] Duration
  - [ ] Selected stdout/stderr excerpts (bounded by `MaxLogBytes`)

**Structured logging:**

- [ ] Application logs include structured fields like:
  - [ ] Operation id
  - [ ] PackageFamilyName
  - [ ] AppId (if used)
  - [ ] Mode
  - [ ] Elevation used (true/false)
  - [ ] Exit code / result

**Diagnostics flags:**

- [ ] Optional verbose/diagnostic mode increases detail without blowing past log size caps or leaking sensitive data

---

## 10. Tests

**Unit tests:**

- [ ] `UwpDumpCommand` validation:
  - [ ] PFN/AppId combinations
  - [ ] Output path / workspace checks
  - [ ] Allowed modes enforcement
  - [ ] Timeout rules
- [ ] `UwpDumperOptions` binding and validation tests
- [ ] Elevation logic:
  - [ ] `RequireElevation` and override scenarios
- [ ] Command-line allowlist:
  - [ ] No unknown switches or user-supplied arbitrary args

**Integration / fault injection:**

- [ ] Integration tests (real or fake UWPDumper) cover:
  - [ ] Successful dump → valid `UwpDumpResult`, artifacts, logs
- [ ] Fault injection tests cover:
  - [ ] Missing binary → `DependencyMissingError`
  - [ ] Hash mismatch
  - [ ] Denied elevation → `ElevationRequiredError`
  - [ ] Timeout → appropriate error
  - [ ] Non-zero exit → `ToolExecutionError`

---

## 11. Phase 3 “Done” Snapshot

Check all before moving on:

- [ ] UWPDumper is packaged, extracted, and hash-validated
- [ ] `UwpDumpCommand` + `IUwpDumperAdapter` + `UwpDumperAdapter` implemented
- [ ] Elevation rules enforced according to `UwpDumperOptions`
- [ ] `UwpDumpResult` + error types wired and used
- [ ] `UwpDumperOptions` configured, validated, and respected
- [ ] Outputs live under workspace and carry artifact metadata/hashes
- [ ] Logging + diagnostics are useful for debugging failures and audits
- [ ] Unit + integration/fault tests exist and pass
- [ ] Code follows the project’s “human-style” rules (no AI meta, no gratuitous abstractions, comments only when they explain real complexity)

Once this whole thing is green, UWPDumper is a first-class, trustworthy tool in ARIS’s backend, and you’re ready to move on to the DLL injector phase and start wiring the rest of the pipeline together.
