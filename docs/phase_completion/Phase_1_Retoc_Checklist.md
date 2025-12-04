# Phase 1 Checklist – Retoc Integration (ARIS C# Rewrite)

This is a **human checklist** mirroring `Phase_1_Retoc_Integration.md`.

Use it to verify that Retoc is actually integrated, testable, and not just “mentioned in the docs”.

---

## 1. Preconditions

- [ ] Phase 0 checklist is fully complete
- [ ] Backend builds cleanly (`dotnet build` from `src/`)
- [ ] `ARIS_Retoc_Integration_SDD.md` exists in `docs/` and has been read at least once

---

## 2. Tools Manifest & Extraction

**In `Aris.Tools`:**

- [ ] Tools manifest (e.g., `tools.manifest.json`) contains a **Retoc** entry with:
  - [ ] `id = "retoc"`
  - [ ] `version` set
  - [ ] `sha256` hash for `retoc.exe`
  - [ ] `relativePath` to `retoc.exe` (e.g., `retoc/retoc.exe`)
  - [ ] Any required DLLs also listed with hashes

**In extraction logic (`DependencyExtractor` or equivalent):**

- [ ] Retoc is extracted to `%LOCALAPPDATA%/ARIS/tools/{version}/retoc/`
- [ ] Hash verification is performed against the manifest
- [ ] Lock/marker file is written so future runs can detect “already extracted”
- [ ] Re-running startup does **not** unnecessarily re-extract identical files

**Validation:**

- [ ] There is a Retoc-specific validation step (e.g., in `DependencyValidator`)
- [ ] Logs on startup clearly say Retoc is present + valid (or describe why not)

---

## 3. Command DTOs & Builder

**Types present:**

- [ ] `RetocCommand` exists with fields for:
  - [ ] Input path
  - [ ] Output path
  - [ ] Mode (`RetocMode` enum)
  - [ ] Keys / key references
  - [ ] Game/UE version
  - [ ] Compression options
  - [ ] Filters (include/exclude)
  - [ ] Additional args (but allowlisted)
  - [ ] Working directory
  - [ ] Timeout

- [ ] `RetocMode` enum exists with expected modes (e.g. `PakToIoStore`, `IoStoreToPak`, `Repack`, `Validate`)

**Validation:**

- [ ] Input path must exist (or is validated before run)
- [ ] Output path is valid / parent dir exists or is created
- [ ] Required keys must be present when demanded by mode
- [ ] Filters are validated (no obvious injection / path traversal)
- [ ] Additional args are checked against an allowlist

**Command builder:**

- [ ] There is a builder or method that:
  - [ ] Maps `RetocMode` → concrete CLI args
  - [ ] Maps compression options → CLI args
  - [ ] Maps filters → `--include/--exclude` or equivalent
  - [ ] Injects keys safely (no plaintext keys in logs)
  - [ ] Returns a usable `ProcessStartInfo` (or equivalent) for `retoc.exe`

**Tests:**

- [ ] Unit tests cover:
  - [ ] Valid command construction
  - [ ] Rejection of bad additional args
  - [ ] Missing/invalid paths
  - [ ] Missing required keys

---

## 4. Adapter & Process Integration

**Interface:**

- [ ] `IRetocAdapter` interface exists with something like:
  - [ ] `ConvertAsync(RetocCommand, CancellationToken, IProgress<ProgressEvent>)`
  - [ ] `ValidateAsync(CancellationToken)`

**Implementation:**

- [ ] `RetocAdapter` class exists
- [ ] Adapter uses the shared `IProcessRunner` / process wrapper
- [ ] Working directory for each run is under a temp/staging path like:
  - [ ] `workspace/temp/retoc-{operationId}/`
- [ ] Adapter:
  - [ ] Invokes dependency validation before running
  - [ ] Builds command using the command builder
  - [ ] Captures stdout/stderr with sane limits
  - [ ] Redacts keys in logs/diagnostics

**Error mapping:**

- [ ] Non-zero exit → `ToolExecutionError`
- [ ] Missing binary / hash mismatch → `DependencyMissingError`
- [ ] Input/command issues → `ValidationError`

**Progress:**

- [ ] Adapter publishes progress events for:
  - [ ] Staging
  - [ ] Decrypting
  - [ ] Converting
  - [ ] Re-encrypting
  - [ ] Finalizing

---

## 5. Results & Error Types

**Types present:**

- [ ] `RetocResult` exists with fields like:
  - [ ] Exit code
  - [ ] Output path
  - [ ] Output format
  - [ ] Duration
  - [ ] Warnings list
  - [ ] Produced files metadata (and optionally hashes)
  - [ ] Log excerpt (truncated)

- [ ] `ProgressEvent` exists with:
  - [ ] Step identifier (enum/string)
  - [ ] Message
  - [ ] Optional percent
  - [ ] Optional detail

**Error types:**

- [ ] Retoc-specific or shared:
  - [ ] `ValidationError`
  - [ ] `DependencyMissingError`
  - [ ] `ToolExecutionError`
  - [ ] `ChecksumMismatchError` (if implemented now, or at least type exists)

**Behavior:**

- [ ] Public-facing API layer returns `RetocResult` on success
- [ ] Failures come back as one of the explicit error types (not just generic exceptions)

---

## 6. Configuration & Options

**Options class:**

- [ ] `RetocOptions` exists with:
  - [ ] Default timeout
  - [ ] Default compression options
  - [ ] Allowed additional args
  - [ ] Max log bytes
  - [ ] Staging root (optional override)
  - [ ] Structured logging toggle

**Binding:**

- [ ] `RetocOptions` is bound from configuration (e.g., `appsettings.json` → `Retoc` section)
- [ ] Startup-time validation:
  - [ ] Timeout sane (not negative, not absurd)
  - [ ] Max log size sane
  - [ ] Allowed args list is sanitised

**Usage:**

- [ ] `RetocAdapter` uses `RetocOptions` for:
  - [ ] Default timeout when command doesn’t override
  - [ ] Enforcing allowed args
  - [ ] Log truncation
  - [ ] Staging root override (or default workspace temp)
  - [ ] Structured logging flag

---

## 7. Workspace & Key Management

**Workspace:**

- [ ] Retoc:
  - [ ] Reads from `workspace/input/` (or defined input root)
  - [ ] Writes to `workspace/output/retoc/{operationId}/`
  - [ ] Uses `workspace/temp/retoc-{operationId}/` for staging

- [ ] Paths are:
  - [ ] Normalized
  - [ ] Prevented from escaping workspace (no arbitrary absolute paths unless explicitly allowed)

**Keys:**

- [ ] Integration with key management:
  - [ ] Workflows use `KeyStore` (or equivalent) to resolve keys by game/UE version
  - [ ] Missing keys produce a **validation-time failure**, not an attempted Retoc call
  - [ ] Keys are never logged in plaintext (redacted in logs and errors)

---

## 8. Logging & Diagnostics

**Logging behavior:**

- [ ] Each Retoc operation logs:
  - [ ] Operation id
  - [ ] Mode
  - [ ] Game/UE version
  - [ ] Paths/hashes in a privacy-compliant way
  - [ ] Exit code

- [ ] Per-operation log file (or similar) is created, e.g.:
  - [ ] `logs/retoc-{operationId}.log` inside the workspace

- [ ] Logs contain:
  - [ ] Command line with secrets redacted
  - [ ] Relevant stderr excerpts on failure

**Error payloads:**

- [ ] Error objects exposed up-stack have:
  - [ ] Clear reason
  - [ ] Human-understandable hints (e.g., “missing AES key”, “invalid input file”, etc.)
  - [ ] No secret material

---

## 9. Tests

**Unit tests:**

- [ ] Command builder tests:
  - [ ] Valid command shapes
  - [ ] Mode → args mapping
  - [ ] Filters mapping
  - [ ] Additional args allowlist

- [ ] Options tests:
  - [ ] `RetocOptions` binding from config
  - [ ] Invalid values get caught/handled

- [ ] Error mapping tests:
  - [ ] Validation failures → `ValidationError`
  - [ ] Missing binary → `DependencyMissingError`
  - [ ] Non-zero exit → `ToolExecutionError`

**Integration / fault-injection tests:**

- [ ] Either:
  - [ ] Real Retoc binary used with tiny fixtures, OR
  - [ ] Fake Retoc / process runner used to simulate behavior

- [ ] Integration tests assert:
  - [ ] Successful run produces outputs and a valid `RetocResult`
  - [ ] Faults (missing binary, timeout, bad args) produce correct error types
  - [ ] Logs and progress events are emitted

---

## 10. Phase 1 “Done” Snapshot

Check all before moving on:

- [ ] Retoc is fully represented in tools manifest and extraction/validation pipeline
- [ ] `IRetocAdapter` + `RetocAdapter` + `RetocCommand` implemented
- [ ] Back-end API returns `RetocResult` / typed errors
- [ ] `RetocOptions` configured and wired
- [ ] Workspace + key handling enforced
- [ ] Logging and diagnostics provide enough info to debug Retoc runs
- [ ] Unit + integration tests exist and pass
- [ ] Code follows project-wide “human-style” conventions (no AI boilerplate, no weird overengineering)

When every box here is truly checked, Retoc is no longer hypothetical — it’s a real, testable part of ARIS and ready for higher-level workflows + UI in later phases.
