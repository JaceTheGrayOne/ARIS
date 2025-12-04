# Phase 2 Checklist – UAssetAPI Integration (ARIS C# Rewrite)

This is a **human checklist** mirroring `Phase_2_UAssetAPI_Integration.md`.

Use it to verify that UAssetAPI is actually integrated and behaving, not just name-dropped in the codebase.

---

## 1. Preconditions

- [ ] Phase 0 checklist is complete
- [ ] Phase 1 (Retoc) checklist is complete
- [ ] Backend builds cleanly (`dotnet build` from `src/`)
- [ ] `ARIS_UAssetAPI_Integration_SDD.md` exists in `docs/` and has been read at least once

---

## 2. Dependency Integration

**NuGet / assembly:**

- [ ] UAssetAPI is referenced via NuGet in the correct backend project (e.g. `Aris.Infrastructure` or similar)
- [ ] The UAssetAPI version is pinned (not floating)
- [ ] `dotnet publish` produces output where the UAssetAPI assembly is present and loadable

**Optional embedded packaging (if used):**

- [ ] Tools/dependency manifest has an entry for UAssetAPI (e.g. `id = "uassetapi"`, version, hash)
- [ ] Dependency extraction can place UAssetAPI assembly in a tools folder
- [ ] Hash verification for the UAssetAPI assembly is implemented and works

**Sanity:**

- [ ] No runtime assembly load errors when starting backend with UAssetAPI code paths enabled

---

## 3. Command DTOs & Validation

**DTOs exist:**

- [ ] `UAssetSerializeCommand` with fields like:
  - [ ] `InputJsonPath`
  - [ ] `OutputAssetPath`
  - [ ] UE/Game version
  - [ ] Schema version
  - [ ] Optional compression / flags
  - [ ] Optional working directory

- [ ] `UAssetDeserializeCommand` with fields like:
  - [ ] `InputAssetPath` (`*.uasset` / `*.uexp` etc.)
  - [ ] `OutputJsonPath`
  - [ ] UE/Game version
  - [ ] Schema version
  - [ ] Optional include-bulk-data flag

- [ ] `UAssetInspectCommand` with fields like:
  - [ ] `InputAssetPath`
  - [ ] Optional `Fields` list (exports/imports/names/etc.)

**Validation:**

- [ ] Paths normalized to absolute
- [ ] Inputs must exist
- [ ] Output parent directories created (or required)
- [ ] UE version / schema version are checked against known/supported values
- [ ] Asset size checks enforced (e.g. `MaxAssetSizeMB`)
- [ ] Attempts to operate outside workspace are rejected or explicitly controlled

**Tests:**

- [ ] Unit tests for:
  - [ ] Missing/invalid paths
  - [ ] Bad UE/schema versions
  - [ ] Oversized assets
  - [ ] Basic happy-path validation

---

## 4. IUAssetService & In-Process Implementation

**Interface:**

- [ ] `IUAssetService` exists with methods roughly like:
  - [ ] `SerializeAsync(UAssetSerializeCommand, ...)`
  - [ ] `DeserializeAsync(UAssetDeserializeCommand, ...)`
  - [ ] `InspectAsync(UAssetInspectCommand, ...)`

**Implementation:**

- [ ] `UAssetService` (or similarly named class) exists and:
  - [ ] Uses UAssetAPI **in-process** (default path)
  - [ ] Validates commands up front
  - [ ] Uses a per-operation staging dir like `workspace/temp/uasset-{operationId}/`
  - [ ] Measures duration of operations
  - [ ] Writes outputs atomically (temp → move)

**Behavior:**

- [ ] Deserialize:
  - [ ] Reads `*.uasset` (and sidecars if needed)
  - [ ] Produces JSON according to expected schema
- [ ] Serialize:
  - [ ] Reads JSON
  - [ ] Builds asset(s) via UAssetAPI
  - [ ] Writes `.uasset` + sidecars to output locations
- [ ] Inspect:
  - [ ] Reads asset
  - [ ] Returns inspection info (summary + optional exports/imports/etc.)

**DI:**

- [ ] `IUAssetService` registered in DI
- [ ] All required dependencies (options, workspace, logger) injected and used

---

## 5. Optional CLI Fallback (If Implemented)

**Configuration:**

- [ ] `UAssetOptions.EnableCliFallback` (or similar) exists (default `false`)

**When enabled:**

- [ ] There is a CLI wrapper binary (e.g. `uassetbridge.exe`) available
- [ ] CLI is invoked using shared process runner abstraction
- [ ] DTOs are translated into CLI args correctly
- [ ] JSON stdout from CLI is parsed into `UAssetResult` / `UAssetInspection`
- [ ] Errors from CLI map to `ToolExecutionError` / `DependencyMissingError` etc.

**Tests:**

- [ ] Integration tests (or higher-level tests) for CLI path:
  - [ ] Happy path
  - [ ] Non-zero exit
  - [ ] Timeout / malformed JSON handling

> If you don’t plan to support CLI fallback yet, mark this whole section as intentionally “not implemented”.

---

## 6. Result Models & Error Types

**Result types:**

- [ ] `UAssetResult` exists with fields like:
  - [ ] Operation type (serialize/deserialize)
  - [ ] Input path
  - [ ] Output path(s)
  - [ ] Duration
  - [ ] Warnings
  - [ ] Produced files metadata (incl. hashes where appropriate)
  - [ ] Schema + UE version

- [ ] `UAssetInspection` exists with fields like:
  - [ ] Input path
  - [ ] Summary (versions, counts, etc.)
  - [ ] Optional exports/imports/names/etc.

**Error types:**

- [ ] `ValidationError` used for bad commands/options
- [ ] `DependencyMissingError` for missing UAssetAPI / CLI binaries
- [ ] `ToolExecutionError` for CLI failures (if CLI path exists)
- [ ] `SerializationError` / `DeserializationError` for UAssetAPI issues
- [ ] `ChecksumMismatchError` for hash mismatches (if hashing implemented here)

**Behavior:**

- [ ] Public-facing API returns `UAssetResult` / `UAssetInspection` on success
- [ ] Failures use typed errors (not just `Exception`)

---

## 7. UAssetOptions Configuration

**Options:**

- [ ] `UAssetOptions` exists with values like:
  - [ ] `DefaultSchemaVersion`
  - [ ] `DefaultUEVersion`
  - [ ] `MaxAssetSizeMB`
  - [ ] `EnableCliFallback` (bool)
  - [ ] `TimeoutSeconds`
  - [ ] `KeepTempOnFailure` (bool)
  - [ ] `LogJsonOutput` (bool / similar)

**Binding & validation:**

- [ ] `UAssetOptions` bound from config (e.g. `appsettings.json` → `UAsset` section)
- [ ] Startup validation checks:
  - [ ] `MaxAssetSizeMB` sane range
  - [ ] `TimeoutSeconds` sane range
  - [ ] Default UE/schema versions recognized
- [ ] Misconfigurations produce clear logs or startup failure (not silent fail)

**Usage:**

- [ ] `UAssetService` uses options for:
  - [ ] Default UE/schema values
  - [ ] Max asset size checks
  - [ ] Timeouts (in-process + CLI)
  - [ ] Whether to keep temp files
  - [ ] Extra logging behavior

---

## 8. Workspace & File Handling

**Workspace locations:**

- [ ] Inputs (typical): under `workspace/input/assets/` or defined input root
- [ ] Outputs: under `workspace/output/uasset/` (plus per-operation naming/folders)
- [ ] Temp: per-operation `workspace/temp/uasset-{operationId}/`

**File behavior:**

- [ ] Writes are atomic (temp → move)
- [ ] Hashes computed for outputs and recorded in result metadata
- [ ] Sidecar files (`.uasset`, `.uexp`, `.ubulk` etc.) handled consistently
- [ ] Missing required sidecars produce clear errors

**Security:**

- [ ] Operations are restricted to workspace paths by default
- [ ] Attempts to operate outside workspace are rejected or clearly controlled/logged
- [ ] File size check enforced before loading huge assets

---

## 9. Progress & Logging

**Progress:**

- [ ] Serialize/deserialize emit progress events with steps like:
  - [ ] Opening
  - [ ] Parsing
  - [ ] Converting
  - [ ] Writing
  - [ ] Hashing
  - [ ] Finalizing

**Logging:**

- [ ] Logs per operation include:
  - [ ] Operation id
  - [ ] Input/output hashes (or paths according to policy)
  - [ ] UE version
  - [ ] Schema version
  - [ ] Duration
  - [ ] Warning count

- [ ] Logs are bounded in size (no dumping massive JSON or raw asset guts)
- [ ] Optional extra JSON diagnostics only when `LogJsonOutput` (or similar) is enabled

---

## 10. Tests

**Unit tests:**

- [ ] DTO validation tests
- [ ] `UAssetOptions` binding + validation tests
- [ ] Error mapping tests:
  - [ ] UAssetAPI exceptions → correct error type
  - [ ] Bad paths → `ValidationError`
  - [ ] Over-size assets → `ValidationError` / explicit “too large” error

**Integration tests (in-process):**

- [ ] Round-trip tests on fixture assets:
  - [ ] `Deserialize` → `Serialize` → compare / validate
- [ ] Version-specific tests (UE 4 vs UE 5, as applicable)
- [ ] Tests for inspect behavior returning reasonably-shaped data

**CLI fallback tests (if applicable):**

- [ ] Happy path
- [ ] Non-zero exit
- [ ] Timeout
- [ ] Malformed JSON

---

## 11. Phase 2 “Done” Snapshot

Check all before moving on:

- [ ] UAssetAPI dependency integrated and working at runtime
- [ ] Command DTOs + validation implemented and tested
- [ ] `IUAssetService` + implementation exist and work in-process
- [ ] Optional CLI fallback behaves as configured (or is explicitly deferred)
- [ ] Result models + error types are wired and used
- [ ] `UAssetOptions` configured, validated, and consumed correctly
- [ ] Workspace patterns (input/output/temp, hashing, sidecars) followed
- [ ] Logging + progress provide good visibility without leaking sensitive data
- [ ] Unit + integration tests exist and pass
- [ ] Code adheres to project-wide “human-style” rules (no AI boilerplate, no gratuitous abstractions)

When this entire checklist is truly green, ARIS has a serious, usable UAssetAPI integration and is ready to be wired into workflows and UI in later phases.
