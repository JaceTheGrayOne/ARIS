# Execution Document – Phase 4: DLL Injector Integration

Status: Draft  
Audience: ARIS C# Implementation Engineer (Claude Code), human reviewers  
Related docs:  
- ARIS_High_Level_Design_SDD.md  
- ARIS_Backend_SDD.md  
- ARIS_DLLInjector_Integration_SDD.md  
- Phase_0_Environment_And_Scaffolding.md  
- Phase_1_Retoc_Integration.md  
- Phase_2_UAssetAPI_Integration.md  
- Phase_3_UWPDumper_Integration.md  

---

## 1. Purpose and Scope

This document defines **Phase 4 – DLL Injector Integration** for the ARIS C# rewrite.

**Goal of this phase:**  
Integrate the custom **DLL injector** as a controlled, audited external tool that can safely inject and eject ARIS payload DLLs into target game processes.

By the end of Phase 4, ARIS should be able to:

- Package and extract the **DLL injector** binary (`dllinjector.exe`) and its dependencies with hashing and manifest-based validation. :contentReference[oaicite:0]{index=0}  
- Expose a strongly-typed `IDllInjectorAdapter` interface for injection and ejection operations. :contentReference[oaicite:1]{index=1}  
- Build, validate, and execute `DllInjectCommand` and `DllEjectCommand` instances via the shared `IProcessRunner` with optional elevation. :contentReference[oaicite:2]{index=2}  
- Enforce allowlist/denylist policies for targets, payload hashing, and safe argument handling. :contentReference[oaicite:3]{index=3}  
- Return typed results and errors with sufficient logging and diagnostics for audits and debugging. :contentReference[oaicite:4]{index=4}  

UI workflows for selecting targets and managing payloads will be handled in later phases.

---

## 2. Preconditions

Do **not** start Phase 4 until:

- Phases 0–3 are complete:
  - Tool packaging and extraction subsystem is implemented and used by Retoc/UAssetAPI/UWPDumper.
  - Logging, configuration, and workspace conventions are in place.
  - `IProcessRunner` abstraction exists and is used by other external tools.

- `ARIS_DLLInjector_Integration_SDD.md` is present in `docs/` and has been read at least once. :contentReference[oaicite:5]{index=5}  

- Development environment:
  - Windows x64.
  - Ability to run elevated processes (where required).

If any of these are missing, return to earlier phases.

---

## 3. High-Level Outcomes for Phase 4

By the end of this phase, we want:

1. **Dependency handling**
   - DLL injector and support DLLs are embedded with manifest entries:
     - `id = "dllinjector"`, `version`, `sha256`, `relativePath`, `executable = true`. :contentReference[oaicite:6]{index=6}  
   - Extracted to `%LOCALAPPDATA%/ARIS/tools/{version}/dllinjector/` with hash verification and lock file.

2. **Adapter + commands**
   - `IDllInjectorAdapter` interface and `DllInjectorAdapter` implementation exist. :contentReference[oaicite:7]{index=7}  
   - `DllInjectCommand` and `DllEjectCommand` DTOs implemented with strict validation.

3. **Process execution**
   - Injector runs via `IProcessRunner`, using `dllinjector.exe inject|eject {args}`.
   - Per-operation working directory, bounded stdout/stderr, optional elevation, strict environment. :contentReference[oaicite:8]{index=8}  

4. **Typed results & errors**
   - `DllInjectResult` and `DllEjectResult` are returned on success.
   - Failures return:
     - `ValidationError`
     - `DependencyMissingError`
     - `ElevationRequiredError`
     - `ToolExecutionError`
     - `ChecksumMismatchError` (for payload hash mismatches). :contentReference[oaicite:9]{index=9}  

5. **Configuration**
   - `DllInjectorOptions` bound from configuration and validated at startup.
   - Policies for allowed/denied targets, allowed methods, timeouts, elevation, and max log size enforced. :contentReference[oaicite:10]{index=10}  

6. **Workspace & project usage**
   - Payload DLLs and logs live under defined workspace paths:
     - Payloads: `workspace/input/payloads/` (or shipped dependencies).
     - Temp: `workspace/temp/inject-{operationId}/`.
     - Logs: `workspace/logs/dllinjector-{operationId}.log`. :contentReference[oaicite:11]{index=11}  

7. **Testing**
   - Unit tests for DTO validation, target allowlist/denylist, hash checks, and elevation logic.
   - Integration/fault tests using a test harness process and benign DLL payload. :contentReference[oaicite:12]{index=12}  

---

## 4. Implementation Steps

### 4.1 Tool Packaging and Extraction

**Objective:** Make the DLL injector a first-class, hash-validated tool in the dependency system.

**Steps:**

1. **Manifest entry (in `Aris.Tools`)**

   Add manifest entries for the injector:

   - `id = "dllinjector"`
   - `version`
   - `sha256`
   - `relativePath` (e.g., `dllinjector/dllinjector.exe`)
   - `executable = true`  

   Include any supporting DLLs with their own hashes and paths. :contentReference[oaicite:13]{index=13}  

2. **Extraction behavior**

   - Extend `DependencyExtractor` so that on startup/manifest change it:
     - Extracts injector files into `%LOCALAPPDATA%/ARIS/tools/{version}/dllinjector/`.
     - Uses temp-then-move semantics for atomic writes.
     - Hash-verifies each extracted file.
     - Writes a lock file containing manifest hash to detect changes. :contentReference[oaicite:14]{index=14}  

3. **Pre-run checks**

   - Before each injector run:
     - Re-verify `dllinjector.exe` hash against manifest.
     - Validate payload DLL presence and hash (see 4.2).
     - Ensure architecture compatibility (x64 only). :contentReference[oaicite:15]{index=15}  

**Acceptance criteria:**

- Startup logs show injector extraction and validation status.
- A pre-run validation API (via adapter or dependency validator) can confirm injector readiness.

---

### 4.2 Command DTOs and Validation

**Objective:** Implement `DllInjectCommand` and `DllEjectCommand` with strong validation rules.

**Steps:**

1. **Define `DllInjectCommand`** :contentReference[oaicite:16]{index=16}  

   Fields should include:

   - `int? ProcessId`
   - `string? ProcessName`
   - `string DllPath` (payload DLL path)
   - `DllInjectionMethod Method` (enum: `CreateRemoteThread`, `APC`, `ManualMap`, etc.)
   - `TimeSpan? Timeout`
   - `string? WorkingDirectory` (defaults to `workspace/temp/inject-{operationId}/`)
   - `bool? RequireElevation` (optional override; default from options)
   - `IReadOnlyList<string> Arguments` (for DLL entrypoint; allowlist-based)

2. **Define `DllEjectCommand`** :contentReference[oaicite:17]{index=17}  

   Fields should include:

   - `int? ProcessId`
   - `string? ProcessName`
   - `string DllPathOrModuleName`
   - `TimeSpan? Timeout`
   - `string? WorkingDirectory`

3. **Validation logic (shared helpers)**

   - **Target process resolution:**
     - Require at least one of `ProcessId` or `ProcessName`.
     - Resolve to a single PID.
     - Verify process is:
       - Running.
       - x64.
       - Not in `DllInjectorOptions.DeniedTargets`.
       - In `AllowedTargets` if allowlist is used. :contentReference[oaicite:18]{index=18}  

   - **Payload validation:**
     - Ensure `DllPath` is absolute and under an allowed root:
       - `workspace/input/payloads/` or a read-only `dependencies/payloads/` directory.
     - Check payload DLL exists.
     - Validate hash against:
       - Manifest entry or expected hash provided alongside payload. :contentReference[oaicite:19]{index=19}  
     - Optionally, check code signing if/when policy is decided (open decision).

   - **Method + arguments:**
     - Ensure `Method` is in `DllInjectorOptions.AllowedMethods`.
     - `Arguments` must be from a vetted subset or templates; reject arbitrary strings.

   - **Timeout and working directory:**
     - Normalize `Timeout` against `DllInjectorOptions.DefaultTimeoutSeconds`.
     - Ensure `WorkingDirectory` is under workspace temp (or `StagingRoot` override).

   - On any violation, **return `ValidationError`** with clear reason.

**Acceptance criteria:**

- Invalid PIDs/names, disallowed targets, mismatched hashes, disallowed methods, or unsafe args are caught before any process launch.
- Unit tests cover these validation paths.

---

### 4.3 Adapter and Process Wrapper Integration

**Objective:** Implement `IDllInjectorAdapter` and `DllInjectorAdapter` using `IProcessRunner` and the standard process wrapper rules.

**Steps:**

1. **Define interface** :contentReference[oaicite:20]{index=20}  

   ```
   public interface IDllInjectorAdapter
   {
       Task<DllInjectResult> InjectAsync(
           DllInjectCommand cmd,
           CancellationToken ct,
           IProgress<ProgressEvent> progress);

       Task<DllEjectResult> EjectAsync(
           DllEjectCommand cmd,
           CancellationToken ct,
           IProgress<ProgressEvent> progress);

       Task<DependencyStatus> ValidateAsync(CancellationToken ct);
   }
````

2. **Implement `DllInjectorAdapter`**

   Responsibilities:

   * `ValidateAsync`:

     * Check injector binary and payload directories using the dependency system.

   * `InjectAsync`:

     * Validate command and target process.
     * Validate payload DLL hash/arch.
     * Build command line:

       * `dllinjector.exe inject --pid <pid> --dll "<dllPath>" --method <method> ...`
       * Add validated entrypoint args only.
     * Configure `IProcessRunner`:

       * Working directory: `workspace/temp/inject-{operationId}/`.
       * Environment: minimal; optionally set logging flags.
       * Timeout: from command or `DllInjectorOptions.DefaultTimeoutSeconds`.
       * Capture stdout/stderr to bounded buffers.
       * Tee to `workspace/logs/dllinjector-{operationId}.log`. 

   * `EjectAsync`:

     * Similar pattern, using `dllinjector.exe eject ...` and `DllEjectCommand`.

3. **Elevation handling**

   * Use `DllInjectorOptions.RequireElevation` plus per-command `RequireElevation` overrides. 
   * If elevation is required:

     * Configure process runner to request elevation.
     * If elevation is denied/unavailable, **do not attempt non-elevated injection**; return `ElevationRequiredError`.

4. **Exit and error mapping**

   * Non-zero exit code → `ToolExecutionError` including:

     * Exit code.
     * Redacted command line.
     * Truncated stderr/stdout tail. 
   * Hash/payload mismatch → `ChecksumMismatchError`.
   * Missing injector binary → `DependencyMissingError`.

5. **Progress events**

   * Emit `ProgressEvent` steps:

     * `"ResolvingProcess"`
     * `"ValidatingPayload"`
     * `"Injecting"` / `"Ejecting"`
     * `"Verifying"`
     * `"Finalizing"` 

**Acceptance criteria:**

* `InjectAsync` and `EjectAsync` can be invoked end-to-end against a test harness process and benign DLL.
* Errors are mapped to the typed error classes.

---

### 4.4 Result Models and Error Handling

**Objective:** Implement result types and error flows that match the SDD.

**Steps:**

1. **`DllInjectResult`** 

   Fields:

   * `int ExitCode`
   * `int ProcessId`
   * `string ProcessName`
   * `string DllPath`
   * `DllInjectionMethod Method`
   * `TimeSpan Duration`
   * `IReadOnlyList<string> Warnings`
   * `string? LogExcerpt`

2. **`DllEjectResult`** 

   Fields:

   * `int ExitCode`
   * `int ProcessId`
   * `string ProcessName`
   * `string DllPath`
   * `TimeSpan Duration`
   * `IReadOnlyList<string> Warnings`
   * `string? LogExcerpt`

3. **Error types**

   Ensure these are present (or reused from shared error library) and used consistently:

   * `ValidationError`
   * `DependencyMissingError`
   * `ElevationRequiredError`
   * `ToolExecutionError`
   * `ChecksumMismatchError` 

4. **Remediation hints**

   * When mapping to higher-level Problem Details (for UI), provide hints like:

     * “Run ARIS as administrator.”
     * “Use an x64 payload DLL.”
     * “Target process is denied by policy; select another.” 

**Acceptance criteria:**

* Success paths return rich `DllInjectResult`/`DllEjectResult`.
* Each major failure scenario maps to the correct typed error with clear context.

---

### 4.5 Configuration – DllInjectorOptions

**Objective:** Implement and enforce `DllInjectorOptions`.

**Steps:**

1. **Define `DllInjectorOptions`** 

   Fields:

   * `int DefaultTimeoutSeconds`
   * `bool RequireElevation` (default `true`)
   * `IReadOnlyList<string> AllowedTargets` (process names/regex)
   * `IReadOnlyList<string> DeniedTargets` (critical/system processes)
   * `IReadOnlyList<DllInjectionMethod> AllowedMethods`
   * `int MaxLogBytes`
   * `string? StagingRoot`

2. **Binding**

   * Bind from `appsettings.json` → `DllInjector` section.
   * Allow environment-specific overrides for dev/test/prod.

3. **Validation at startup**

   * Validate:

     * `DefaultTimeoutSeconds` > 0 and reasonable.
     * `MaxLogBytes` > 0 and bounded.
     * `AllowedMethods` is non-empty and only valid enum values.
     * Denylist includes critical system processes from the SDD (e.g., `csrss.exe`, system services). 

4. **Usage**

   * Apply in adapter:

     * Determine whether elevation is required.
     * Enforce `AllowedTargets`/`DeniedTargets` when resolving target.
     * Enforce allowed methods.
     * Limit log size when capturing `LogExcerpt`.
     * Use `StagingRoot` as override for temp if provided.

**Acceptance criteria:**

* Misconfigurations cause clear startup errors or warnings, not silent misbehavior.
* Changing options has visible effect on adapter behavior.

---

### 4.6 Workspace Flow and Target Safety

**Objective:** Align injection workflows with workspace conventions and security rules.

**Steps:**

1. **Workspace directories** 

   * Payloads:

     * `workspace/input/payloads/` (or a `/dependencies/payloads/` shipped path).
   * Temp/staging per operation:

     * `workspace/temp/inject-{operationId}/`
   * Logs:

     * `workspace/logs/dllinjector-{operationId}.log`

2. **Target selection and allow/deny rules**

   * Ensure backend:

     * Re-validates target process against `AllowedTargets`/`DeniedTargets` each time (even if UI filtered list).
     * Blocks system processes and other explicitly denied executables.
   * Record:

     * Target `ProcessId` and `ProcessName` in result and logs.

3. **Post-injection verification (optional but preferred)**

   * If injector supports it:

     * Confirm that the payload module is present in target process (e.g., via injector output).
   * Record verification outcome and include in warnings or results. 

**Acceptance criteria:**

* All injector operations use workspace paths for payloads, temp, and logs.
* Denylist prevents obvious “nope” targets from being touched.

---

### 4.7 Logging and Diagnostics

**Objective:** Provide audit-friendly logs and diagnostics for each injection/ejection operation.

**Steps:**

1. **Per-operation log file** 

   * Write `workspace/logs/dllinjector-{operationId}.log` containing:

     * Redacted command line (no raw sensitive args).
     * Exit code.
     * Duration.
     * Bounded stdout/stderr (head + tail respecting `MaxLogBytes`).

2. **Structured logging**

   * Emit structured log events with fields:

     * `operationId`
     * `processId`
     * `processName`
     * `dllHash` (payload hash)
     * `method`
     * `elevated` (true/false)
     * `exitCode`
     * `duration` 

3. **Redaction and safety**

   * Ensure:

     * Payload paths are redacted or normalized if configured to avoid leaking personal paths.
     * Arguments are redacted when they might contain sensitive info.
   * Keep logs bounded and avoid dumping huge outputs.

**Acceptance criteria:**

* Logs are detailed enough for debugging and audit trails, but not leaking sensitive information.
* Log size is controlled by `MaxLogBytes`.

---

### 4.8 Testing Strategy

**Objective:** Validate DLL injector integration with unit tests, integration tests, and fault injection. 

**Steps:**

1. **Unit tests**

   * DTO validation:

     * Pid/name resolution rules.
     * Payload path + hash checks.
     * Allowed/denied targets enforcement.
     * Allowed methods enforcement.
   * Options:

     * `DllInjectorOptions` binding and validation.
   * Elevation logic:

     * `RequireElevation` and overrides.

2. **Integration tests**

   * Create a **test harness process** (simple .NET process) as injection target.
   * Use a benign test DLL that:

     * Can signal successful load (e.g., writes event/log or sets a flag).
   * Tests:

     * Inject test DLL into harness process.
     * Verify via:

       * Injector output or secondary check that module is loaded.
     * Eject test DLL (if supported) and verify unload.

3. **Fault injection**

   * Simulate:

     * Bad PID / non-existent process.
     * Wrong arch (e.g., trying to inject into incompatible process).
     * Hash mismatch for payload.
     * Denied elevation.
     * Timeout.
     * Forced non-zero exit codes with stderr output.

4. **Safety checks**

   * Confirm denylist prevents targeting known system processes in tests.
   * Ensure tests never attempt real injection into non-test processes.

**Acceptance criteria:**

* Unit tests cover critical validation and configuration.
* Integration tests demonstrate successful injection and ejection against a safe harness.
* Fault injection tests prove robust error handling.

---

## 5. Definition of Done (Phase 4)

Phase 4 is complete when:

1. **Tool packaging & validation**

   * DLL injector is represented in the tools manifest and extracted/validated at startup.
   * Hash checks and lock file mechanics are in place.

2. **Adapter & commands**

   * `IDllInjectorAdapter`, `DllInjectorAdapter`, `DllInjectCommand`, and `DllEjectCommand` are implemented and compile.
   * Command construction is fully allowlisted and validated.

3. **Execution & elevation**

   * Injector runs via `IProcessRunner` with proper working directories, timeouts, logging, and optional elevation.
   * Elevation behavior follows `DllInjectorOptions` and per-command flags.

4. **Results & errors**

   * `DllInjectResult` and `DllEjectResult` are returned on success with rich metadata.
   * Failures are mapped to `ValidationError`, `DependencyMissingError`, `ElevationRequiredError`, `ToolExecutionError`, or `ChecksumMismatchError`.

5. **Configuration**

   * `DllInjectorOptions` is bound from configuration, validated, and actually used.

6. **Workspace & safety**

   * Payloads, temp, and logs follow workspace conventions and security rules.
   * Allowlist/denylist effectively constrain target processes.

7. **Testing**

   * Unit + integration + fault-injection tests exist and pass.

8. **Code quality**

   * Implementation follows project-wide “human-style” rules:

     * No AI/meta comments.
     * No unnecessary abstractions.
     * Comments explain non-obvious behavior only.

Once all of the above are satisfied, the DLL injector is a controlled, auditable component of the ARIS backend and ready to be orchestrated by higher-level workflows and UI phases.