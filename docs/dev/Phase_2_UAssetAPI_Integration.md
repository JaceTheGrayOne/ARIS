# Execution Document – Phase 2: UAssetAPI Integration

Status: Draft  
Audience: ARIS C# Implementation Engineer (Claude Code), human reviewers  
Related docs:  
- ARIS_High_Level_Design_SDD.md  
- ARIS_Backend_SDD.md  
- ARIS_UAssetAPI_Integration_SDD.md  
- Phase_0_Environment_And_Scaffolding.md  
- Phase_1_Retoc_Integration.md  

---

## 1. Purpose and Scope

This document defines **Phase 2 – UAssetAPI Integration** for the ARIS C# rewrite.

**Goal of this phase:**  
Integrate **UAssetAPI** as an in-process Unreal asset serialization/inspection engine within the ARIS backend.

By the end of Phase 2, ARIS should be able to:

- Use **UAssetAPI** directly from C# to serialize (`JSON → uasset`) and deserialize (`uasset → JSON`) Unreal asset files. 
- Inspect asset metadata with a typed `UAssetInspection` model. 
- Expose a strongly typed `IUAssetService` to the application layer, with well-defined DTOs and result models. 
- Handle configuration, workspace paths, and error mapping in a way consistent with the rest of the ARIS backend (Retoc, future tools).

The **default integration is in-process**. The optional CLI fallback is implemented behind configuration and off by default. 

---

## 2. Preconditions

Do **not** start Phase 2 until:

- Phase 0 and Phase 1 are complete:
  - Solution + layered projects exist and build.
  - Logging + configuration pipeline is working.
  - Tool extraction/validation system exists (for external tools like Retoc).
  - Retoc integration is implemented and tested.

- `ARIS_UAssetAPI_Integration_SDD.md` is present in `docs/` and has been read at least once. 

If any of these are missing, go back and complete the earlier phases before proceeding.

---

## 3. High-Level Outcomes for Phase 2

By the end of this phase, we want:

1. **Dependency integration**
   - UAssetAPI is referenced via **NuGet** (preferred) and pinned to a specific version. 
   - Optional embedding/extraction path exists for offline installers, using the same tool/dependency pattern as other components.

2. **Command DTOs & validation**
   - `UAssetSerializeCommand`, `UAssetDeserializeCommand`, and `UAssetInspectCommand` implemented with validation rules for paths, UE versions, schema versions, and workspace boundaries. 

3. **Service implementation**
   - `IUAssetService` interface and implementation provide:
     - `SerializeAsync`
     - `DeserializeAsync`
     - `InspectAsync`  
     using **in-process** UAssetAPI. 

4. **Optional CLI fallback**
   - CLI wrapper mode (`uassetbridge.exe` or equivalent) can be used **only if configured**, using `IProcessRunner` and JSON-stdout contracts. Default remains in-process. 

5. **Result models & errors**
   - `UAssetResult` and `UAssetInspection` implemented, with errors mapped to:
     - `ValidationError`
     - `DependencyMissingError`
     - `ToolExecutionError` (for CLI mode)
     - `SerializationError` / `DeserializationError`
     - `ChecksumMismatchError` (for hash failures) 

6. **Configuration**
   - `UAssetOptions` bound and validated at startup; used to control schema defaults, UE version, limits, timeout, and optional CLI mode. 

7. **Workspace usage**
   - Inputs/outputs/temp follow the workspace conventions for asset operations (`input/assets`, `output/uasset`, `temp/uasset-{operationId}`) with atomic writes and hash verification. 

8. **Testing**
   - Unit tests cover DTO validation, path normalization, options binding, and error mapping.  
   - Integration tests perform round-trip operations on fixtures and exercise CLI fallback if enabled. 

---

## 4. Implementation Steps

### 4.1 UAssetAPI Dependency Acquisition and Packaging

**Objective:** Bring UAssetAPI into the solution in a controlled, versioned way.

**Steps:**

1. **NuGet reference (primary path)**

   - In `Aris.Infrastructure` (or an appropriate backend assembly), add a NuGet reference to UAssetAPI:
     - Pin to a specific version as defined in the SDD.
   - Confirm that the assembly is copied into the publish output under normal `dotnet publish` operations. 

2. **Optional embedded/manifest-based packaging**

   - If the project’s packaging strategy requires offline deployment:
     - Add an entry to the tool/dependency manifest (e.g., in `Aris.Tools`) with:
       - `id = "uassetapi"`
       - `version`
       - `sha256`
       - path for the assembly or NuGet package payload. 
     - Extend the dependency extraction system to:
       - Extract the assembly (or package contents) to `%LOCALAPPDATA%/ARIS/tools/{version}/uassetapi/`.
       - Hash-verify the assembly.
     - Load the assembly using a dedicated `AssemblyLoadContext` when needed. 

3. **Versioning**

   - Ensure only one pinned version of UAssetAPI is used per release.
   - If binding redirects are needed (multi-assembly references), ensure they are configured and tested in publish output. 

**Acceptance criteria:**

- Build and publish succeed with UAssetAPI present.
- The running backend can load and reference UAssetAPI without runtime assembly errors.
- Optional extraction path (if used) passes hash verification.

---

### 4.2 Command DTOs and Validation

**Objective:** Implement the command models that drive UAssetAPI, with strong validation.

**Steps:**

1. **Define DTOs**

   Implement the following DTOs (likely in `Aris.Contracts` or `Aris.Core`):

   - `UAssetSerializeCommand`
     - `InputJsonPath` (required)
     - `OutputAssetPath` (required)
     - `Game` / `UEVersion` (enum/string)
     - `SchemaVersion`
     - `Compression` options (if supported)
     - `WorkingDirectory` (defaults to workspace temp staging) 

   - `UAssetDeserializeCommand`
     - `InputAssetPath` (required – `*.uasset`/`*.uexp`)
     - `OutputJsonPath` (required)
     - `Game` / `UEVersion`
     - `SchemaVersion`
     - `IncludeBulkData` flag 

   - `UAssetInspectCommand`
     - `InputAssetPath`
     - `Fields` (optional list of sections: exports, imports, names, etc.) 

2. **Validation rules**

   - Require **absolute paths**:
     - Normalize to absolute.
     - Reject or normalize non-workspace paths unless explicitly allowed. 
   - Enforce file presence:
     - Input JSON/asset paths must exist.
     - Output parents must exist or be created.
   - Validate Unreal/Schema:
     - Ensure UE version and schema version are recognized.
     - Validate compatibility against `UAssetOptions` default and supported versions. 
   - Guard rails:
     - Ensure assets respect size limits (`MaxAssetSizeMB`).

3. **Implementation**

   - Create validator classes or methods (e.g., extension methods) that:
     - Return structured `ValidationError` results (not just exceptions) when validation fails.
     - Are usable from both the application layer and `IUAssetService`.

**Acceptance criteria:**

- Unit tests confirm validation behavior:
  - Missing files, invalid paths, invalid UE/schema versions, oversized assets.
- DTOs and validators are easy to use from service methods and the application layer.

---

### 4.3 IUAssetService Interface and In-Process Implementation

**Objective:** Implement the main service that uses UAssetAPI in-process.

**Steps:**

1. **Define `IUAssetService`**

   Interface should provide at least:

   - `Task<UAssetResult> SerializeAsync(UAssetSerializeCommand cmd, CancellationToken ct, IProgress<ProgressEvent> progress)`
   - `Task<UAssetResult> DeserializeAsync(UAssetDeserializeCommand cmd, CancellationToken ct, IProgress<ProgressEvent> progress)`
   - `Task<UAssetInspection> InspectAsync(UAssetInspectCommand cmd, CancellationToken ct)` 

2. **Implement `UAssetService` (in-process path)**

   - In the implementation:

     - **Common flow (all methods):**
       1. Validate command + options (using the validators from 4.2 and `UAssetOptions`). 
       2. Create operation-specific staging folder under `workspace/temp/uasset-{operationId}/`. 
       3. Start timer/stopwatch for duration tracking.
       4. Use UAssetAPI to open/serialize/inspect the asset.
       5. Write outputs atomically (temp-then-move).
       6. Compute hashes and build `UAssetResult` or `UAssetInspection`.

     - **Deserialize flow:**
       - Open `*.uasset` (+ `*.uexp`/`*.ubulk` if present).
       - Normalize to ARIS JSON schema.
       - Emit progress: Opening → Parsing → Converting → Writing → Hashing → Finalizing. 
       - Write JSON to `OutputJsonPath` atomically.

     - **Serialize flow:**
       - Read JSON from `InputJsonPath`.
       - Validate JSON against the expected schema version (structural checks as feasible).
       - Construct asset objects with UAssetAPI.
       - Write `*.uasset` (and sidecar files) to staging, then move to `OutputAssetPath`.
       - Hash outputs and fill `ProducedFiles` metadata. 

     - **Inspect flow:**
       - Open asset.
       - Populate `UAssetInspection` with:
         - Summary (versions, counts, etc.).
         - Optional exports/imports/names based on `Fields`. 

3. **DI registration**

   - Register `IUAssetService` in the backend composition root (e.g., `Aris.Hosting` or an infrastructure module).
   - Ensure configuration (`UAssetOptions`) and workspace services are injected.

**Acceptance criteria:**

- In-process `IUAssetService` compiles and can run against small, known-good test assets.
- Serialize/deserialize/inspect flows can complete end-to-end in a dev environment.

---

### 4.4 Optional CLI Fallback Integration

**Objective:** Implement the optional CLI wrapper mode for UAssetAPI, disabled by default.

**Steps:**

1. **Configuration switch**

   - Add `UAssetOptions.EnableCliFallback` (default `false`). 
   - When `true`, `IUAssetService` should route work through the CLI wrapper instead of direct in-process calls.

2. **CLI wrapper definition**

   - CLI tool (e.g., `uassetbridge.exe`) supports subcommands:
     - `serialize`
     - `deserialize`
     - `inspect`  
     and communicates via JSON on stdout. 

3. **Process integration**

   - Use the shared `IProcessRunner` abstraction (same as Retoc) with:
     - Bounded stdout/stderr buffers.
     - Timeout using `UAssetOptions.TimeoutSeconds`.
     - Cancellation support.
   - Build CLI arguments from the same command DTOs (or adapted versions).
   - Parse JSON stdout into `UAssetResult` or `UAssetInspection`.
   - Map non-zero exit codes to `ToolExecutionError`, with log excerpts. 

4. **Error handling**

   - If CLI binary is missing or hash mismatch → `DependencyMissingError`.
   - Malformed JSON → `DeserializationError` or general `SerializationError`, with clear remediation hints.

**Acceptance criteria:**

- When `EnableCliFallback = false`, all operations use in-process UAssetAPI.
- When `EnableCliFallback = true`, operations go through CLI wrapper and work for simple fixtures.
- Tests cover both paths where feasible.

---

### 4.5 Result Models and Error Mapping

**Objective:** Implement result types and ensure errors are mapped consistently.

**Steps:**

1. **Result types**

   - `UAssetResult` with fields: 

     - `Operation` (Serialize | Deserialize)
     - `InputPath`
     - `OutputPath` / `OutputPaths` (list for multi-file outputs)
     - `Duration`
     - `Warnings` (collection)
     - `ProducedFiles` (metadata + hashes)
     - `SchemaVersion`
     - `UEVersion`

   - `UAssetInspection` with fields: 

     - `InputPath`
     - `Summary` (name table size, export count, import count, versions)
     - `Exports` (optional, filtered)
     - `Imports` (optional)

2. **Error types**

   Implement or wire up:

   - `ValidationError` – command options and path issues.
   - `DependencyMissingError` – missing assembly or CLI binary.
   - `ToolExecutionError` – CLI fallback non-zero exit.
   - `SerializationError` / `DeserializationError` – thrown from UAssetAPI, wrapped with context. 
   - `ChecksumMismatchError` – post-write hash mismatch (assets or JSON). 

3. **Frontend-facing errors**

   - Make sure errors can be translated into Problem Details for the UI with remediation hints (e.g., “UE version mismatch”, “Asset exceeds configured max size”, “Schema mismatch; update mappings”). 

**Acceptance criteria:**

- Happy-path operations return `UAssetResult`/`UAssetInspection`.
- All failure modes map to one of the typed errors, not just generic exceptions.
- Tests assert error mappings for key scenarios.

---

### 4.6 UAssetOptions Configuration

**Objective:** Implement `UAssetOptions` and bind it from configuration with validation.

**Steps:**

1. **Define `UAssetOptions`**

   Include at least: 

   - `DefaultSchemaVersion`
   - `DefaultUEVersion`
   - `MaxAssetSizeMB`
   - `EnableCliFallback` (bool, default `false`)
   - `TimeoutSeconds`
   - `KeepTempOnFailure` (bool)
   - `LogJsonOutput` (bool) – controls extra JSON diagnostics logging

2. **Bind from configuration**

   - In `Aris.Hosting` or configuration module:
     - Bind from `appsettings.json` → `UAsset` section.
     - Allow environment-specific overrides.

3. **Validate**

   - At startup, run options validation:
     - `MaxAssetSizeMB` > 0 and within a sensible upper bound.
     - `TimeoutSeconds` > 0 and within reasonable range.
     - Default schema and UE version are recognized.
   - Failure mode:
     - Either hard fail startup (preferred for bad configuration), or log error and fall back to safe defaults with explicit warnings.

4. **Usage in service**

   - `UAssetService` uses `UAssetOptions` for:
     - Default UE/schema when commands don’t override.
     - Max asset size checks.
     - Timeout selection (in-process and CLI).
     - Whether to retain temp files on failure.
     - Whether to emit extra JSON logs.

**Acceptance criteria:**

- Configuration binding works across dev/prod settings.
- Misconfigurations are detected early and reported clearly.

---

### 4.7 Workspace and File Handling

**Objective:** Ensure UAsset operations use the ARIS workspace model correctly and safely.

**Steps:**

1. **Workspace conventions**

   Follow the conventions from the SDD: 

   - Inputs:
     - Typically under `workspace/input/assets/`
   - Outputs:
     - `workspace/output/uasset/` (with per-operation subfolders or naming)
   - Temp:
     - `workspace/temp/uasset-{operationId}/`

2. **Atomic writes and hashing**

   - For JSON outputs and asset outputs:
     - Write to a temp file in the staging directory.
     - Compute hash on the final file.
     - Move atomically to the target path.
   - Record hashes in `UAssetResult.ProducedFiles`.

3. **Sidecar files**

   - Ensure `*.uasset`, `*.uexp`, and `*.ubulk` remain co-located and consistent.
   - When serializing:
     - Keep related files in sync (same UE version/configuration).
   - When deserializing:
     - Ensure all necessary sidecar files are present or fail with a meaningful error. 

4. **Security**

   - Restrict operations to workspace paths by default.
   - Reject inputs outside workspace unless explicitly allowed (and logged).
   - Enforce `MaxAssetSizeMB` based on actual file size before full processing. 

**Acceptance criteria:**

- UAsset operations do not escape the workspace unintentionally.
- Hashes and metadata are available for downstream processes.
- Sidecar files are handled consistently across serialize/deserialize.

---

### 4.8 Progress and Logging

**Objective:** Provide useful progress events and logs for UAsset operations.

**Steps:**

1. **Progress events**

   - For serialize/deserialize, emit progress events: 

     - `Opening`
     - `Parsing`
     - `Converting`
     - `Writing`
     - `Hashing`
     - `Finalizing`

   - Include:
     - Operation id
     - Human-readable message
     - Optional percentage and detail

2. **Logging**

   - Log fields:
     - Operation id
     - Input hash
     - Output hash
     - UE version
     - Schema version
     - Duration
     - Warnings count 

   - Respect redaction options:
     - Paths may be redacted or normalized when configured.
     - Keep log size bounded, especially for large assets.

3. **Diagnostics**

   - When `UAssetOptions.LogJsonOutput` is enabled:
     - Optionally emit normalized JSON snapshots for debugging (with careful size limits and workspace-safe locations).

**Acceptance criteria:**

- Progress stream and logs allow a future UI to show meaningful “what’s happening now” states.
- Logs are helpful for debugging but do not leak sensitive information.

---

### 4.9 Testing Strategy

**Objective:** Validate UAssetAPI integration via unit and integration tests.

**Steps:**

1. **Unit tests**

   - DTO validation:
     - Paths, UE/schema versions, size limits.
   - Options binding:
     - `UAssetOptions` binding and validation.
   - Error mapping:
     - Simulated UAssetAPI exceptions → `SerializationError`/`DeserializationError`.
     - Invalid paths → `ValidationError`. 

2. **Integration tests (in-process)**

   - Round-trip fixtures:
     - Known-good `*.uasset` assets:
       - `Deserialize` to JSON.
       - `Serialize` back to assets.
       - Compare with original (or at least structural equivalence).
   - Version-specific tests:
     - Assets for UE 4.x and UE 5.x where possible.

3. **CLI fallback tests (if enabled)**

   - Only when `EnableCliFallback` is on:
     - Simulate CLI success with small fixtures.
     - Simulate:
       - Non-zero exit.
       - Timeout.
       - Malformed JSON.
     - Confirm error mapping and logging. 

4. **Property/round-trip tests (optional)**

   - Property-based tests for selected fixture sets:
     - `deserialize → serialize → deserialize` should be stable for given schema/UE version.

**Acceptance criteria:**

- Unit tests pass and cover the critical code paths.
- Integration tests confirm real UAssetAPI behavior on at least a small set of representative assets.
- CLI fallback tests exist if that mode is supported.

---

## 5. Definition of Done (Phase 2)

Phase 2 is complete when:

1. **Dependency integration**
   - UAssetAPI is properly referenced (NuGet) and loaded in-process.
   - Optional embedded/manifest path is implemented if required by packaging.

2. **Service + DTOs**
   - `IUAssetService` and its implementation exist.
   - `UAssetSerializeCommand`, `UAssetDeserializeCommand`, `UAssetInspectCommand` are implemented and validated.

3. **Results and errors**
   - `UAssetResult` and `UAssetInspection` are returned on success.
   - Errors are mapped to the defined typed error classes.

4. **Configuration**
   - `UAssetOptions` is bound from configuration, validated, and used by the service.

5. **Workspace behavior**
   - UAsset operations follow workspace conventions for input/output/temp, atomic writes, and hashing.

6. **CLI fallback**
   - When enabled, CLI wrapper mode works and is tested; default remains in-process.

7. **Testing**
   - Unit + integration tests exist and pass for in-process flows.
   - CLI fallback tests exist if the feature is enabled.

8. **Code quality**
   - Implementation follows the project’s “human-style” coding rules: no AI meta, no unnecessary abstractions, comments only where they add real value.

At this point, ARIS has a fully functional, in-process UAssetAPI integration and can be wired into higher-level workflows and UI flows in later phases.
