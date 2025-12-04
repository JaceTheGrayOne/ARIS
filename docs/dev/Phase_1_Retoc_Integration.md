# Execution Document – Phase 1: Retoc Integration

Status: Draft  
Audience: ARIS C# Implementation Engineer (Claude Code), human reviewers  
Related docs:  
- ARIS_High_Level_Design_SDD.md  
- ARIS_Backend_SDD.md  
- ARIS_Retoc_Integration_SDD.md  
- Phase_0_Environment_And_Scaffolding.md  

---

## 1. Purpose and Scope

This document defines **Phase 1 – Retoc Integration** for the ARIS C# rewrite.

**Goal of this phase:**  
Turn Retoc from a theoretical tool in the SDD into a **working, testable integration** inside the ARIS backend.

By the end of Phase 1, ARIS should be able to:

- Package and extract the Retoc CLI and its dependencies as part of the normal tool lifecycle. :contentReference[oaicite:0]{index=0}  
- Expose a strongly-typed `IRetocAdapter` API in the backend. :contentReference[oaicite:1]{index=1}  
- Build and execute `RetocCommand` instances via `IProcessRunner`. :contentReference[oaicite:2]{index=2}  
- Produce typed `RetocResult` values and meaningful error types for failures. :contentReference[oaicite:3]{index=3}  
- Respect workspace flows (input/output/temp), key handling, and configuration described in the SDD. :contentReference[oaicite:4]{index=4}  

UI wiring and full UX around Retoc happen in later phases; here we focus on **backend behavior plus tests**.

---

## 2. Preconditions

Do **not** start Phase 1 until:

- Phase 0 is complete:
  - Solution + projects exist and build.
  - Logging + configuration pipeline established.
  - Tool extraction subsystem stub (`Aris.Tools` + `DependencyExtractor`) exists and is called on startup.
  - WebView2 host and frontend scaffold exist (even though frontend won’t yet expose Retoc).  

- `ARIS_Retoc_Integration_SDD.md` is present in the `docs/` directory and has been skimmed for understanding.

If any of these are missing, return to Phase 0.

---

## 3. High-Level Outcomes for Phase 1

By the end of this phase, we want:

1. **Retoc packaged & extracted**
   - Retoc binaries and supporting DLLs are defined in the tools manifest with `id=retoc`, `version`, `sha256`, `relativePath`, `executable=true`. :contentReference[oaicite:5]{index=5}  
   - They are extracted to `%LOCALAPPDATA%/ARIS/tools/{version}/retoc/` with hash verification, lock file, and repair support. :contentReference[oaicite:6]{index=6}  

2. **Adapter + command DTOs implemented**
   - `IRetocAdapter` and `RetocAdapter` implemented as per SDD with `ConvertAsync` and `ValidateAsync`. :contentReference[oaicite:7]{index=7}  
   - `RetocCommand`, `RetocMode`, and supporting DTOs/enums exist and are validated.

3. **Process execution via shared wrapper**
   - Retoc is invoked via `IProcessRunner` with proper working directory, timeout, and output capture. :contentReference[oaicite:8]{index=8}  

4. **Typed results + errors**
   - `RetocResult` and the error types (`ValidationError`, `DependencyMissingError`, `ToolExecutionError`, `ChecksumMismatchError`) are wired and used properly. :contentReference[oaicite:9]{index=9}  

5. **Configuration + workspace usage**
   - `RetocOptions` bound from configuration and validated at startup. :contentReference[oaicite:10]{index=10}  
   - Retoc operations respect workspace input/output/temp folder conventions and key management rules. :contentReference[oaicite:11]{index=11}  

6. **Tests**
   - Unit tests for command validation, options binding, and error mapping.
   - At least one integration test hitting a sample Retoc binary (or stub/fake layer if real binary is unavailable in this environment).

---

## 4. Implementation Steps

### 4.1 Extend Tool Packaging and Extraction for Retoc

**Objective:** Make Retoc a first-class tool entry in the dependency system.

**Steps:**

1. **Tools manifest entry (`Aris.Tools`)**

   - Extend the tools manifest (e.g., `tools.manifest.json` or equivalent) with a Retoc entry:

     - `id`: `retoc`
     - `version`: version string consistent with the shipped binary
     - `sha256`: expected hash of `retoc.exe`
     - `relativePath`: path under the extracted directory (e.g., `retoc/retoc.exe`)
     - `executable`: true

   - If Retoc uses supporting DLLs, they should also be declared with hashes and paths.

2. **Extraction path**

   - Ensure `DependencyExtractor` extracts Retoc to:

     - `%LOCALAPPDATA%/ARIS/tools/{version}/retoc/`

   - Implement:
     - Manifest hash lock file to detect changes.
     - Skip identical files and write via temp-then-move for atomicity.
     - Hash verification during extraction and on demand. :contentReference[oaicite:12]{index=12}  

3. **Validator hook**

   - Add a Retoc-specific validation call in `DependencyValidator` (or equivalent) to:

     - Verify presence of `retoc.exe`.
     - Verify hash matches the manifest entry.

**Acceptance criteria:**

- After backend startup, logs confirm Retoc has been extracted and validated (or is already correct).
- Calling a Retoc-specific validation path (e.g., `IRetocAdapter.ValidateAsync`) returns a success status when binaries are correct.

---

### 4.2 Command DTOs and Command Construction

**Objective:** Implement `RetocCommand` and the rules for constructing a safe, validated command line.

**Steps:**

1. **Define DTOs (likely in `Aris.Contracts` or `Aris.Core`)**

   - Implement `RetocCommand` with fields from the SDD:  

     - `InputPath` (required)
     - `OutputPath` (required)
     - `Mode` (`RetocMode` enum: `PakToIoStore`, `IoStoreToPak`, `Repack`, `Validate`)
     - `MountKeys` (collection, resolved by KeyStore)
     - `GameVersion` / `UEVersion`
     - `Compression` options (format, level, block size)
     - `Filters` (include/exclude globs)
     - `AdditionalArgs` (allowlist-based)
     - `WorkingDirectory` (optional; default to workspace `temp/retoc-{operationId}/`)
     - `Timeout` (per-invocation) :contentReference[oaicite:13]{index=13}  

2. **Validation rules (in a builder or validator class)**

   - Normalize `InputPath`/`OutputPath` to absolute paths.
   - Validate:
     - Input file exists.
     - Output parent folder exists or is created.
   - Ensure:
     - `Mode` is set and maps to one of the supported Retoc subcommands.
     - `MountKeys` are present when required (depending on mode).
   - Map `Filters` to repeated `--include/--exclude` arguments.
     - Validate globs to prevent injection.
   - Enforce `AdditionalArgs` allowlist:
     - Reject commands with disallowed extra args and return a `ValidationError`. :contentReference[oaicite:14]{index=14}  

3. **Command line construction**

   - Implement a command builder that:
     - Constructs the final `ProcessStartInfo` (or equivalent) for `retoc.exe`.
     - Injects keys via the appropriate Retoc syntax (`--aes-key` or equivalent).
     - Maps `Mode` to Retoc’s CLI shape (subcommands + flags).
     - Applies compression options and filters.

**Acceptance criteria:**

- Unit tests cover:
  - Valid/invalid paths.
  - Required keys missing.
  - Disallowed additional args.
- Builder produces a safe, deterministic command line for each mode.

---

### 4.3 Adapter and Process Wrapper Integration

**Objective:** Implement `IRetocAdapter` on top of the shared `IProcessRunner` abstraction.

**Steps:**

1. **Define `IRetocAdapter` interface**

   - Methods (as per SDD):

     - `Task<RetocResult> ConvertAsync(RetocCommand command, CancellationToken ct, IProgress<ProgressEvent> progress)`
     - `Task<DependencyStatus> ValidateAsync(CancellationToken ct)` :contentReference[oaicite:15]{index=15}  

2. **Implement `RetocAdapter`**

   - Responsibilities:
     - Use `DependencyValidator` to ensure Retoc is present and valid before each run.
     - Build `ProcessStartInfo` for `retoc.exe` based on `RetocCommand`.
     - Set working directory to operation-specific staging folder under `workspace/temp/retoc-{operationId}/`. :contentReference[oaicite:16]{index=16}  
     - Use `IProcessRunner` to:
       - Start the process with given timeout.
       - Capture stdout/stderr (bounded buffers).
       - Optionally tee logs to `workspace/logs/retoc-{operationId}.log`. :contentReference[oaicite:17]{index=17}  

   - Environment:
     - Optionally set `REToc_LOG_JSON=1` if structured logging is supported and enabled via `RetocOptions`. :contentReference[oaicite:18]{index=18}  

3. **Exit and error handling**

   - Non-zero exit codes => `ToolExecutionError` including:
     - Exit code.
     - Command line (with keys redacted).
     - Captured output (truncated). :contentReference[oaicite:19]{index=19}  

   - Missing binary or hash mismatch => `DependencyMissingError`.
   - Validation failures before execution => `ValidationError`.

4. **Progress events**

   - Emit `ProgressEvent` instances for major milestones:
     - Staging
     - Decrypting
     - Converting
     - Re-encrypting
     - Finalizing :contentReference[oaicite:20]{index=20}  

   - Map process output or phases in the adapter into these step-level events.

**Acceptance criteria:**

- A happy-path call to `IRetocAdapter.ConvertAsync`:
  - Validates dependencies.
  - Executes Retoc.
  - Returns `RetocResult` with expected fields.
- Non-zero exit, missing binary, invalid args each produce the correct error type.

---

### 4.4 Result Model and Error Types

**Objective:** Implement `RetocResult`, `ProgressEvent`, and Retoc-specific error types and ensure they’re used.

**Steps:**

1. **`RetocResult`**

   - Implement with fields in SDD:

     - `ExitCode`
     - `OutputPath`
     - `OutputFormat`
     - `Duration`
     - `Warnings` (collection)
     - `ProducedFiles` (metadata + hashes)
     - `LogExcerpt` (truncated) :contentReference[oaicite:21]{index=21}  

2. **Progress model**

   - Implement `ProgressEvent` with:

     - `Step` (enum/string: staging/decrypt/convert/re-encrypt/finalize)
     - `Message`
     - `Percent?` (optional)
     - `Detail` (optional) :contentReference[oaicite:22]{index=22}  

   - Ensure `IRetocAdapter` publishes progress in a way that downstream layers (application/front-end) can map to UI steps later.

3. **Error types**

   - Implement and/or wire the following:

     - `ValidationError`
     - `DependencyMissingError`
     - `ToolExecutionError`
     - `ChecksumMismatchError` :contentReference[oaicite:23]{index=23}  

   - Decide whether these live in a shared error namespace (e.g., `Aris.Core.Errors`) or a Retoc-specific namespace, but keep them consistent with the Backend SDD’s error model.

**Acceptance criteria:**

- Application layer API that calls Retoc returns either:
  - `RetocResult` on success, or
  - One of the defined error types.
- Unit tests verify error-type mapping for:
  - Validation failure.
  - Missing binary.
  - Non-zero exit.
  - Hash mismatch on outputs (if implemented in this phase; otherwise stub with TODO but error type present).

---

### 4.5 Configuration and Options Binding

**Objective:** Implement `RetocOptions` and its binding from configuration, including startup validation.

**Steps:**

1. **Define `RetocOptions`**

   - Properties:

     - `DefaultTimeoutSeconds`
     - `DefaultCompression`
     - `AllowedAdditionalArgs`
     - `MaxLogBytes`
     - `StagingRoot` (override)
     - `EnableStructuredLogs` :contentReference[oaicite:24]{index=24}  

2. **Configuration binding**

   - In `Aris.Hosting` (or configuration layer), bind `RetocOptions` from:
     - `appsettings.json` → `Retoc` section.
     - User overrides/environment-specific settings.

   - Add a validation step at startup to:
     - Enforce non-negative timeouts.
     - Validate `MaxLogBytes` sensible range.
     - Ensure `AllowedAdditionalArgs` are sanitized.

3. **Usage in adapter**

   - `RetocAdapter` uses `RetocOptions` for:
     - Default timeout (unless overridden by `RetocCommand`).
     - Allowed arguments filter.
     - Log truncation size.
     - Staging root override (fallback to workspace temp if not set).
     - Structured logging toggle.

**Acceptance criteria:**

- Misconfigured `RetocOptions` cause a clear startup error (or logged warning + safe defaults), not silent misbehavior.
- Unit tests confirm binding + validation.

---

### 4.6 Workspace Flow and Key Management Hooks

**Objective:** Ensure Retoc’s use of paths and keys matches ARIS’s workspace and security model.

**Steps:**

1. **Workspace structure**

   - Ensure Retoc operations adhere to:

     - Inputs: `workspace/input/`
     - Outputs: `workspace/output/retoc/{operationId}/`
     - Temp/staging: `workspace/temp/retoc-{operationId}/` :contentReference[oaicite:25]{index=25}  

   - Any paths in `RetocCommand` that fall outside the workspace should be rejected or normalized, enforcing allowlisting.

2. **Key management integration**

   - Integrate `RetocCommand` with `KeyStore`:

     - Mount keys resolved by game/UE version.
     - AES keys never logged in plaintext; redact in command logs/error payloads. :contentReference[oaicite:26]{index=26}  

3. **Post-operation validation**

   - Implement (or stub with clear TODO, depending on scope) post-checks:

     - Verify expected combos: `pak/utoc/ucas` relationships.
     - Hash outputs if the SDD mandates it; else design the hook so it can be filled later. :contentReference[oaicite:27]{index=27}  

**Acceptance criteria:**

- Any attempt to run Retoc outside the workspace is prevented or clearly flagged.
- Key resolution is required for relevant operations; missing keys produce a `ValidationError` and do not attempt to call Retoc.

---

### 4.7 Logging and Diagnostics

**Objective:** Make Retoc executions observable and debuggable without leaking secrets.

**Steps:**

1. **Execution logs**

   - For each operation, log:

     - Operation id.
     - Mode.
     - UE/Game version.
     - Input/output path hashes (not raw paths if that’s the chosen policy).
     - Exit code. :contentReference[oaicite:28]{index=28}  

   - Write `logs/retoc-{operationId}.log` within the workspace with:
     - Command line (keys redacted).
     - First and last N lines of stderr on failure. :contentReference[oaicite:29]{index=29}  

2. **Error payloads**

   - Error objects sent up to the application/frontend layer should include:
     - High-level failure reason.
     - Human-readable remediation hints (e.g., “verify AES key / game selection”). :contentReference[oaicite:30]{index=30}  

**Acceptance criteria:**

- On success, logs clearly show what was done and how long it took.
- On failure, logs contain enough context to debug without re-running blindly, while still protecting keys.

---

### 4.8 Testing

**Objective:** Validate Retoc integration through unit, integration, and fault-injection tests.

**Steps:**

1. **Unit tests (likely in `Aris.Core.Tests` or a Retoc-specific test project)**

   - Command builder validation:
     - Path validation.
     - Mode → flags mapping.
     - Filters → args.
     - Additional args allowlist enforcement.
   - Options binding:
     - Bad values cause validation failures.
     - Missing sections use sensible defaults.

2. **Integration tests**

   - If a real Retoc binary can be run in the CI/dev environment:

     - Use small fixture inputs in a temp workspace.
     - Run `IRetocAdapter.ConvertAsync` end-to-end.
     - Assert:
       - Outputs exist.
       - Exit code is zero.
       - Result DTO fields are populated.

   - If real binary is unavailable, define a **fake Retoc binary** or `IProcessRunner` test double that simulates success/failure and verifies the adapter behavior.

3. **Fault injection**

   - Simulate:
     - Missing Retoc binary (e.g., remove file or force `DependencyMissingError`).
     - Non-zero exit via test double.
     - Timeout.
     - Hash mismatch on extraction or output.

**Acceptance criteria:**

- Unit tests cover key validation logic and configuration.
- Integration tests (real or faked) demonstrate that adapter and process wrapper behave as specified.
- Fault injection tests exercise all major error types.

---

## 5. Definition of Done (Phase 1)

Phase 1 is complete when:

1. **Tool packaging & validation**
   - Retoc is fully represented in the tools manifest.
   - Retoc extraction and hash validation work via the existing dependency system.

2. **Backend API**
   - `IRetocAdapter`, `RetocAdapter`, and `RetocCommand` are implemented and used by the application layer.
   - The adapter uses `IProcessRunner` and workspace conventions.

3. **Results & errors**
   - `RetocResult` and error types are wired and returned from a public API.
   - Progress events are generated for major steps.

4. **Configuration**
   - `RetocOptions` is bound and validated at startup.
   - Retoc behavior respects configured defaults and overrides.

5. **Tests**
   - Unit tests exist and pass for command/option validation.
   - Integration/fault-injection tests exist and pass for adapter behavior.

6. **Code style compliance**
   - Code adheres to the project-wide coding/commenting rules and does not introduce unnecessary abstractions or AI-style boilerplate.

At this point, ARIS has a robust backend integration for Retoc and is ready for higher-level workflows and UI wiring in later phases.
