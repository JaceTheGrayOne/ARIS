# ARIS UAssetAPI Integration Software Design Document
Last updated: 2025-12-04  
Audience: Backend engineers, build engineers

## 1. Purpose and Scope
- Describe how ARIS integrates UAssetAPI for Unreal asset serialization/deserialization and metadata inspection.
- Cover dependency packaging/extraction, command construction/invocation, process or in-proc wrapper, typed results/errors, configuration, and project-specific workflows.
- Includes migration note on UAssetBridge: why it existed and why the .NET 8 rewrite can consume UAssetAPI natively.

## 2. Role of UAssetAPI in ARIS
- Provides programmatic access to Unreal `*.uasset`/`*.uexp`/`*.umap` files for converting between binary and JSON and inspecting asset metadata.
- Enables round-trip conversions, validation, and schema-normalized JSON outputs used by ARIS tooling and UI.

## 3. UAssetBridge Rationale (Legacy) and Decommission (New)
- **Legacy (Go/Wails)**: UAssetAPI is a .NET library; the Go backend could not load it directly, so `UAssetBridge` (a thin .NET CLI) acted as a bridge to perform conversions and emit JSON/stdout.
- **New (.NET 8)**: The backend is native C#/.NET 8, so UAssetAPI can be referenced as a library. `UAssetBridge` is no longer needed; integration is in-process with richer type safety, better performance, and fewer marshaling steps.

## 4. Dependency Handling
- **Acquisition**: Reference UAssetAPI via NuGet (preferred) pinned to a specific version; optionally embed the package in offline installers.
- **Packaging**:
  - If NuGet restore is allowed at build time, ship the compiled assembly within the app publish output.
  - If offline, include the NuGet package or assembly as an embedded resource with manifest entry (`id=uassetapi`, `version`, `sha256`).
- **Extraction (if embedded)**:
  - Extract to `%LOCALAPPDATA%/ARIS/tools/{version}/uassetapi/` on first run or manifest change, hash-verify, then load via `AssemblyLoadContext`.
- **Versioning**: Single pinned version per release to avoid runtime divergence; ABI stability ensured by binding redirects as needed.

## 5. Integration Approach
- **Primary**: In-process library usage via strongly typed service `IUAssetService`.
- **Fallback (optional)**: CLI wrapper mode if process isolation is required for rare cases (e.g., crash containment); disabled by default.
- **Contracts**:
  - `Task<UAssetResult> SerializeAsync(UAssetSerializeCommand cmd, CancellationToken ct, IProgress<ProgressEvent> progress)`
  - `Task<UAssetResult> DeserializeAsync(UAssetDeserializeCommand cmd, CancellationToken ct, IProgress<ProgressEvent> progress)`
  - `Task<UAssetInspection> InspectAsync(UAssetInspectCommand cmd, CancellationToken ct)`

## 6. Command Models (DTOs)
- `UAssetSerializeCommand`
  - `InputJsonPath` (required) — source JSON to convert to binary assets.
  - `OutputAssetPath` (required) — target `*.uasset` (and `*.uexp` if applicable).
  - `Game`/`UEVersion` (enum/string) — informs version-specific behaviors.
  - `SchemaVersion` — JSON schema variant used by ARIS.
  - `Compression` options (if supported by UAssetAPI version).
  - `WorkingDirectory` (defaults to workspace temp staging).
- `UAssetDeserializeCommand`
  - `InputAssetPath` (required) — `*.uasset` (and `*.uexp` if needed).
  - `OutputJsonPath` (required) — normalized JSON output location.
  - `Game`/`UEVersion`
  - `SchemaVersion`
  - `IncludeBulkData` flag (controls large binary extraction).
- `UAssetInspectCommand`
  - `InputAssetPath`
  - `Fields`: optional list for focused inspection (e.g., exports, imports, names).
- **Validation rules**:
  - Require absolute paths; ensure inputs exist; ensure output parents exist.
  - Reject traversal and non-workspace paths unless explicitly allowed.
  - Validate UE version and schema compatibility before invocation.

## 7. Execution Flow (In-Process)
1. Validate command DTO and options.
2. Prepare staging directory under `workspace/temp/uasset-{opId}/`.
3. Load UAssetAPI assembly (already referenced) and create context for the specified UE version.
4. For deserialize:
   - Open asset (and sidecar files) with UAssetAPI.
   - Normalize to ARIS JSON schema; emit progress milestones (open, parse, convert, write).
   - Write JSON atomically (temp-then-move) to `OutputJsonPath`.
5. For serialize:
   - Read JSON, validate against schema version.
   - Construct asset objects; write `*.uasset` (and `*.uexp`/`*.ubulk` if produced) to staging, then move to `OutputAssetPath`.
6. For inspect:
   - Read asset and project requested fields into `UAssetInspection`.
7. Post-ops: hash outputs, record metadata, emit progress completion.

## 8. Optional CLI Wrapper Mode (Fallback)
- Command: `uassetbridge.exe serialize|deserialize|inspect ...`
- Process runner: same as shared `IProcessRunner` with bounded buffers, timeouts, cancellation.
- Used only if configured for isolation; default path is in-process.
- Typed results parsed from JSON stdout; errors mapped as described below.

## 9. Results and Error Handling
- **Result types**:
  - `UAssetResult`
    - `Operation` (Serialize|Deserialize)
    - `InputPath`
    - `OutputPath`/`OutputPaths` (list for multi-file outputs)
    - `Duration`
    - `Warnings` (collection)
    - `ProducedFiles` (metadata + hashes)
    - `SchemaVersion`
    - `UEVersion`
  - `UAssetInspection`
    - `InputPath`
    - `Summary` (name table size, export count, import count, versions)
    - `Exports` (optional filtered data)
    - `Imports` (optional)
- **Errors**:
  - `ValidationError`: bad paths, missing files, schema/UE mismatch.
  - `DependencyMissingError`: assembly not found or hash mismatch.
  - `ToolExecutionError`: for CLI fallback non-zero exit.
  - `SerializationError`/`DeserializationError`: thrown by UAssetAPI; wrapped with context.
  - `ChecksumMismatchError`: post-write hash mismatch.
- Errors are returned as Problem Details to the frontend with remediation hints (e.g., "update mappings for UE 5.3").

## 10. Configuration
- Options class: `UAssetOptions`
  - `DefaultSchemaVersion`
  - `DefaultUEVersion`
  - `MaxAssetSizeMB` (guard rails)
  - `EnableCliFallback` (bool, default false)
  - `TimeoutSeconds`
  - `KeepTempOnFailure` (bool)
  - `LogJsonOutput` (bool) — if emitting normalized JSON logs for diagnostics
- Bound from `appsettings.json` + user overrides; validated at startup.
- Per-command overrides allowed where safe (schema version, UE version, include bulk data, timeout).

## 11. Workspace and File Handling
- Inputs typically in `workspace/input/assets/`; outputs in `workspace/output/uasset/`.
- Temp/staging in `workspace/temp/uasset-{opId}/`; temp retained on failure if configured.
- Atomic writes: temp file then move; hash verification after move.
- Sidecar file handling: ensure `*.uexp` and `*.ubulk` stay co-located and version-consistent.

## 12. Progress and Logging
- Progress events for UI: "Opening", "Parsing", "Converting", "Writing", "Hashing", "Finalizing".
- Logging fields: operationId, inputHash, outputHash, ueVersion, schemaVersion, duration, warnings count.
- Redact paths if configured; keep log size bounded.

## 13. Testing Strategy
- **Unit**: DTO validation, schema selection, path normalization, hash verification, error mapping.
- **Integration (in-proc)**: Round-trip known fixtures (serialize->deserialize->compare), version-specific assets (UE4.27, UE5.x).
- **Fallback CLI tests**: Only if `EnableCliFallback` is on—simulate non-zero exit, timeout, malformed JSON.
- **Property tests**: Optional: ensure idempotent serialize/deserialize on stable fixtures.

## 14. Security and Safety
- Restrict file access to workspace by default.
- Bound maximum asset size; reject oversized inputs.
- Avoid loading untrusted plugins; only load UAssetAPI and signed/hashed dependencies.
- Redact sensitive paths in logs when configured.

## 15. Open Decisions
- Whether to ship CLI fallback binary or rely solely on in-proc for simplicity.
- Default schema versioning strategy when UAssetAPI introduces breaking changes.
- Policy for retaining temp artifacts on failure for user debugging.

---
This SDD defines the integration of UAssetAPI into the C#/.NET 8 ARIS backend, emphasizing native in-process usage, deterministic file handling, typed contracts, and safe, validated operations tailored to ARIS workflows. UAssetBridge is retired because UAssetAPI can now be consumed directly.***
