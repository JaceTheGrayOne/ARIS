# Execution Document – Phase 3: UWPDumper Integration

Status: Draft  
Audience: ARIS C# Implementation Engineer (Claude Code), human reviewers  
Related docs:  
- ARIS_High_Level_Design_SDD.md  
- ARIS_Backend_SDD.md  
- ARIS_UWPDumper_Integration_SDD.md 
- Phase_0_Environment_And_Scaffolding.md  
- Phase_1_Retoc_Integration.md  
- Phase_2_UAssetAPI_Integration.md  

---

## 1. Purpose and Scope

This document defines **Phase 3 – UWPDumper Integration** for the ARIS C# rewrite.

**Goal of this phase:**  
Integrate **UWPDumper** as an external, elevated tool for dumping UWP app packages and extracting SDK-like artifacts (headers/mappings) used by other ARIS workflows. 

By the end of Phase 3, ARIS should be able to:

- Package and extract the **UWPDumper** binary and its dependencies in a validated, hash-checked way. 
- Expose a strongly-typed `IUwpDumperAdapter` interface that runs UWPDumper via the shared `IProcessRunner` abstraction with optional elevation. 
- Construct and validate `UwpDumpCommand` instances with safe, allowlisted command-line arguments. 
- Produce `UwpDumpResult` with metadata about produced artifacts and logs.
- Enforce configuration-based policies for elevation, timeouts, modes, and logging. 
- Run basic unit, integration, and fault-injection tests around UWPDumper behavior.

UI/UX for invoking UWPDumper and consuming its outputs will be handled in later phases.

---

## 2. Preconditions

Do **not** start Phase 3 until:

- Phases 0, 1, and 2 are complete:
  - Backend projects and DI are in place.
  - Logging, configuration, and tool extraction subsystems exist.
  - Retoc and UAssetAPI integrations are implemented and tested.

- `ARIS_UWPDumper_Integration_SDD.md` is present in `docs/` and has been read at least once. 

- Development environment:
  - Windows 10 or 11 with UWP subsystem present.
  - Ability to run elevated processes (UAC prompts allowed).

If these conditions are not met, return to earlier phases or correct the environment.

---

## 3. High-Level Outcomes for Phase 3

By the end of this phase, we want:

1. **Dependency handling**
   - UWPDumper binaries are embedded/packaged with a manifest entry: `id=uwpdumper`, `version`, `sha256`, `relativePath`, `executable=true`.  
   - Extraction to `%LOCALAPPDATA%/ARIS/tools/{version}/uwpdumper/` with hash verification and lock file. 

2. **Adapter and DTO**
   - `UwpDumpCommand` and `IUwpDumperAdapter` / `UwpDumperAdapter` implemented as per SDD. 
   - Command-line construction is allowlisted and validated (no arbitrary args).

3. **Process execution**
   - UWPDumper runs through `IProcessRunner` with:
     - Per-operation working directory.
     - Bounded stdout/stderr capture.
     - Optional elevation and strict environment. 

4. **Typed results & errors**
   - `UwpDumpResult` implemented with artifacts metadata, duration, logs, warnings. 
   - Error types:
     - `ValidationError`
     - `DependencyMissingError`
     - `ElevationRequiredError`
     - `ToolExecutionError`
     - `ChecksumMismatchError`  
     are used consistently. 

5. **Configuration**
   - `UwpDumperOptions` bound from configuration, validated at startup.
   - Policies for `RequireElevation`, modes, timeouts, and max log size enforced. 

6. **Workspace & downstream use**
   - Dumps written under `workspace/output/uwp/{operationId}/`, with temp under `workspace/temp/uwp-{operationId}/`. 
   - Basic post-run validation and hashes are recorded for downstream tools.

7. **Tests**
   - Unit tests for DTO validation, elevation logic, allowlist enforcement.
   - Integration and fault-injection tests using a real or fake UWPDumper binary. 

---

## 4. Implementation Steps

### 4.1 Tool Packaging and Extraction

**Objective:** Make UWPDumper a fully managed, hash-verified external tool in the dependency system.

**Steps:**

1. **Manifest entry (in `Aris.Tools`)**

   - Add an entry for UWPDumper in the tools manifest with: 

     - `id = "uwpdumper"`
     - `version` (string)
     - `sha256` (for the main executable)
     - `relativePath` (e.g., `uwpdumper/uwpdumper.exe`)
     - `executable = true`

   - Include any necessary supporting binaries and their hashes.

2. **Extraction behavior**

   - Extend `DependencyExtractor` to:
     - Extract UWPDumper files into `%LOCALAPPDATA%/ARIS/tools/{version}/uwpdumper/`.
     - Use temp-then-move semantics for atomic writes.
     - Hash-verify each file post-extraction.
     - Create lock file containing the manifest hash, so changes trigger re-extraction. 

3. **Execution prerequisites**

   - Implement a UWPDumper-specific validation step that:
     - Confirms hashes match the manifest.
     - Confirms OS supports UWP (if detectable).
   - Refuse to run UWPDumper if hash mismatches occur (`DependencyMissingError`). 

**Acceptance criteria:**

- Backend startup logs indicate UWPDumper is extracted and valid (or already up-to-date).
- Manual or test-driven validation of UWPDumper dependency passes.

---

### 4.2 Command DTO and Validation

**Objective:** Implement `UwpDumpCommand` and enforce safe, validated inputs.

**Steps:**

1. **Define `UwpDumpCommand`** 

   Fields should include:

   - `string PackageFamilyName` (PFN) or
   - `string AppId`  
   (at least one required; both allowed if consistent)
   - `string OutputPath` (required; destination folder under workspace)
   - `UwpDumpMode Mode` (enum: e.g., `FullDump`, `MetadataOnly`, `Validate`)
   - `bool IncludeSymbols`
   - `TimeSpan? Timeout`
   - `string? WorkingDirectory` (defaults to workspace temp: `workspace/temp/uwp-{operationId}/`)

2. **Validation logic**

   - Resolve PFN/AppId:
     - If both provided, ensure they refer to the same app (where resolvable).
     - If partial data provided (friendly name, etc.), resolution code (or stub) should either:
       - Resolve unambiguously, or
       - Fail with `ValidationError` if ambiguous. 
   - Ensure:
     - `OutputPath` is under `workspace/output/uwp/` by default.
     - Output directory exists or is created.
   - Enforce `UwpDumperOptions.AllowedModes`:
     - Reject modes not in allowlist. 
   - Validate `Timeout` against `UwpDumperOptions.DefaultTimeoutSeconds` and acceptable bounds.
   - Check workspace free space if estimable (optional but preferred; log warnings if low). 

3. **Implementation details**

   - Provide a dedicated validator (class or static method) that:
     - Returns structured `ValidationError` instead of throwing on expected user mistakes.
     - Can be reused by application layer and `UwpDumperAdapter`.

**Acceptance criteria:**

- Invalid PFN/AppId combinations, bad output paths, disallowed modes, and unreasonable timeouts are caught as `ValidationError` before any process starts.
- Unit tests cover common error cases and happy paths.

---

### 4.3 Adapter and Process Wrapper Integration

**Objective:** Implement `IUwpDumperAdapter` and `UwpDumperAdapter` using `IProcessRunner` with elevation support. 

**Steps:**

1. **Define `IUwpDumperAdapter`**

   - Methods:

     ```csharp
     Task<UwpDumpResult> DumpAsync(
         UwpDumpCommand command,
         CancellationToken ct,
         IProgress<ProgressEvent> progress);

     Task<DependencyStatus> ValidateAsync(CancellationToken ct);
     ```

2. **Implement `UwpDumperAdapter`**

   Responsibilities:

   - Validate command via the validator from 4.2.
   - Call dependency validation to ensure UWPDumper is present and hashes match.
   - Construct the command line for `uwpdumper.exe`:
     - Map `Mode` → known switches.
     - Add PFN/AppId arguments as documented.
     - Add `IncludeSymbols` flag when true.
     - Reject any unsupported args; no free-form extras. 
   - Configure `IProcessRunner`:

     - Command: path to extracted `uwpdumper.exe` + vetted args.
     - Working directory: operation-specific `workspace/temp/uwp-{operationId}/`.
     - Environment: minimal; add logging flags only if supported and configured.
     - Timeout: from command or `UwpDumperOptions.DefaultTimeoutSeconds`.
     - Capture stdout/stderr to bounded buffers.
     - Tee output to `logs/uwpdumper-{operationId}.log`. 

3. **Elevation handling**

   - Use `UwpDumperOptions.RequireElevation` as the policy flag (default `true`). 
   - If elevation is required:
     - Configure `IProcessRunner` to request elevation (UAC prompt).
     - If elevation is denied or unavailable, return `ElevationRequiredError` without attempting to run. 
   - Support an explicitly configured **non-elevated diagnostics mode** (e.g., `MetadataOnly` in a lab environment) if allowed by options.

4. **Progress events**

   - Emit `ProgressEvent` milestones: 

     - `"Locating package"`
     - `"Preparing"`
     - `"Dumping"`
     - `"Finalizing"`

   - Include operation id, message, and optional percentage estimate.

**Acceptance criteria:**

- `DumpAsync` executes UWPDumper and returns a `UwpDumpResult` on success.
- Missing binary/hash mismatch => `DependencyMissingError`.
- Denied or unavailable elevation (when required) => `ElevationRequiredError`.
- Non-zero exit => `ToolExecutionError` with captured output and code.

---

### 4.4 Result Model and Error Handling

**Objective:** Implement result and error types and wire them into the adapter behavior. 

**Steps:**

1. **`UwpDumpResult`**

   Fields should include:

   - `int ExitCode`
   - `string PackageFamilyName`
   - `string? AppId`
   - `string OutputPath`
   - `IReadOnlyList<DumpArtifact> Artifacts` (paths + hashes + types)
   - `TimeSpan Duration`
   - `IReadOnlyList<string> Warnings`
   - `string? LogExcerpt` (bounded tail/head of logs)  

2. **`DumpArtifact` model**

   - For each produced file or directory:
     - `string Path`
     - `string Type` (e.g., `Headers`, `Metadata`, `Symbols`)
     - `string Hash` (if feasible)
     - Optional size metadata.

3. **Error types**

   Ensure the following are defined (or reused from shared error library) and used:

   - `ValidationError` – invalid PFN/AppId, output path, mode, etc.
   - `DependencyMissingError` – UWPDumper binary missing or hash mismatch.
   - `ElevationRequiredError` – required elevation unavailable or denied.
   - `ToolExecutionError` – non-zero exit with captured stdout/stderr.
   - `ChecksumMismatchError` – post-dump hash verification failed. 

4. **Surface to higher layers**

   - Ensure errors can be surfaced as Problem Details with remediation hints:
     - e.g., “Run ARIS as administrator”, “Verify package family name or AppId”, “Check free space in workspace output path.” 

**Acceptance criteria:**

- On success, `DumpAsync` returns a fully populated `UwpDumpResult`.
- On failure, adapter returns a specific error type with sufficient context and no leaked sensitive data.

---

### 4.5 Configuration and UwpDumperOptions

**Objective:** Implement `UwpDumperOptions` and enforce configuration-driven behavior. 

**Steps:**

1. **Define `UwpDumperOptions`**

   Include at least:

   - `int DefaultTimeoutSeconds`
   - `bool RequireElevation` (default `true`)
   - `IReadOnlyList<UwpDumpMode> AllowedModes`
   - `int MaxLogBytes`
   - `string? StagingRoot` (optional override for temp, else workspace default)
   - Optional flags for verbose logging/diagnostics

2. **Configuration binding**

   - Bind `UwpDumperOptions` from `appsettings.json` → `UwpDumper` section.
   - Allow environment-specific overrides.

3. **Validation at startup**

   - Validate:
     - `DefaultTimeoutSeconds` > 0 and within sane bounds.
     - `MaxLogBytes` > 0 and capped reasonably.
     - `AllowedModes` is non-empty, and only contains known enum values.
   - Fail-fast or clearly log misconfiguration (do not silently ignore). 

4. **Usage**

   - `UwpDumperAdapter` uses options for:
     - Elevation requirement.
     - Allowed modes enforcement.
     - Timeout when not overridden by command.
     - Log excerpt size and log file size limits.
     - Staging root override if provided.

**Acceptance criteria:**

- Misconfigurations for UWPDumper are detected early and clearly logged.
- Changing options genuinely affects adapter behavior (e.g., toggling elevation or allowed modes).

---

### 4.6 Workspace Flow and Downstream Use

**Objective:** Align UWPDumper I/O with ARIS workspace conventions and downstream workflows. 

**Steps:**

1. **Workspace locations**

   - Outputs:
     - Place operation-scoped output under `workspace/output/uwp/{operationId}/`.
   - Temp/staging:
     - Use `workspace/temp/uwp-{operationId}/` (or `UwpDumperOptions.StagingRoot` override). 

2. **Post-run validation**

   - After a successful run:

     - Hash key artifacts and populate `UwpDumpResult.Artifacts`.
     - Check that expected directories/files (SDK headers, mappings, etc.) are present.
     - Optionally enforce minimal size thresholds to catch empty/failed dumps. 

3. **Downstream metadata**

   - Log and/or persist minimal metadata for later reuse:
     - PFN/AppId
     - Output path
     - Artifact types
     - Hashes
   - Do not attempt full indexing yet; just the basics needed by later phases.

**Acceptance criteria:**

- UWPDumper outputs live entirely under the workspace.
- Results are hash-annotated and structurally validated enough to be trusted by downstream tools.

---

### 4.7 Logging and Diagnostics

**Objective:** Make UWPDumper runs observable and diagnosable, especially given elevation and external-process risk. 

**Steps:**

1. **Per-run log file**

   - For each operation, write `logs/uwpdumper-{operationId}.log` in the workspace.
   - Content should include:
     - Redacted command line.
     - Exit code.
     - Duration.
     - Selected stdout/stderr excerpts (tail/head), subject to `MaxLogBytes`. 

2. **Structured logs**

   - Emit structured log entries with fields:
     - `operationId`
     - `packageFamilyName`
     - `appId` (where applicable)
     - `mode`
     - `elevated` (true/false)
     - `exitCode` 

3. **Verbose diagnostics (optional)**

   - If a verbose flag in `UwpDumperOptions` is enabled:
     - Increase logging detail, but still enforce `MaxLogBytes`.
     - Possibly log additional UWPDumper diagnostics, with caution around PII.

**Acceptance criteria:**

- On success, logs capture enough detail to reconstruct what happened.
- On failure, logs provide actionable information (e.g., why elevation failed, which PFN was used) without leaking sensitive identifiers if configured to redact.

---

### 4.8 Testing Strategy

**Objective:** Test UWPDumper integration via unit, integration, and fault-injection tests. 

**Steps:**

1. **Unit tests**

   - DTO validation:
     - PFN/AppId rules.
     - Output path/workspace bounds.
     - Allowed modes.
     - Timeout and options validation.
   - Elevation logic:
     - `RequireElevation = true` → denies non-elevated runs and produces `ElevationRequiredError`.
     - Non-elevated diagnostics mode allowed only when explicitly configured.
   - Allowlist enforcement:
     - No unsupported switches sneak into the command line.

2. **Integration tests (where feasible)**

   - If possible, run UWPDumper against:
     - Sample or mock UWP packages (safe test case).
   - Validate that:
     - `UwpDumpResult` is populated.
     - Output folder structure matches expectations.
     - Artifacts list matches actual files.

   - If real UWPDumper execution is not feasible in CI:
     - Use a fake process runner or dummy UWPDumper binary that mimics success/failure patterns.

3. **Fault injection**

   - Simulate:
     - Denied elevation.
     - Missing UWPDumper binary (or corrupted hash) → `DependencyMissingError`.
     - Timeouts → `ToolExecutionError` or explicit timeout error.
     - Non-zero exit with stderr content.
     - Corrupted outputs triggering `ChecksumMismatchError`.

**Acceptance criteria:**

- Unit tests cover core validation, configuration, and elevation decisions.
- Integration tests (real or faked) verify end-to-end behavior.
- Fault injection tests prove correct error mapping and robust handling.

---

## 5. Definition of Done (Phase 3)

Phase 3 is complete when **all** of the following are true:

1. **Tool packaging & validation**
   - UWPDumper is represented in the tools manifest and extracted/validated on startup.
   - Hash verification and lock file behavior are implemented.

2. **Adapter & DTO**
   - `UwpDumpCommand`, `IUwpDumperAdapter`, and `UwpDumperAdapter` exist and compile.
   - Command-line construction is fully allowlisted and validated.

3. **Execution & elevation**
   - UWPDumper runs through `IProcessRunner` with proper working directory, timeout, logging, and optional elevation.
   - Elevation requirements and non-elevated modes behave per `UwpDumperOptions`.

4. **Results & errors**
   - `UwpDumpResult` is returned on success with artifacts and duration populated.
   - Failures produce typed errors: `ValidationError`, `DependencyMissingError`, `ElevationRequiredError`, `ToolExecutionError`, `ChecksumMismatchError`.

5. **Configuration**
   - `UwpDumperOptions` is bound from config, validated at startup, and actually used by the adapter.

6. **Workspace & downstream readiness**
   - Outputs are written to `workspace/output/uwp/{operationId}/` with hashes and basic structure checks.
   - Logs and metadata are sufficient for downstream tools and debugging.

7. **Testing**
   - Unit tests and integration/fault-injection tests exist and pass.

8. **Code quality**
   - Implementation follows the project’s “human-style” conventions:
     - No AI/meta comments.
     - No unnecessary abstractions.
     - Comments only where they explain non-obvious behavior.

When all of the above are satisfied, UWPDumper is a reliable, audited part of the ARIS backend and ready to be surfaced through higher-level workflows and UI phases.
