# Phase 4 Checklist – DLL Injector Integration (ARIS C# Rewrite)

This is a **human checklist** mirroring `Phase_4_DLLInjector_Integration.md`.

Use it to verify that the DLL injector is actually integrated, controlled, and safe — not just mentioned in the code.

---

## 1. Preconditions

- [ ] Phase 0 checklist is complete
- [ ] Phase 1 (Retoc) checklist is complete
- [ ] Phase 2 (UAssetAPI) checklist is complete
- [ ] Phase 3 (UWPDumper) checklist is complete
- [ ] Backend builds cleanly (`dotnet build` from `src/`)
- [ ] `ARIS_DLLInjector_Integration_SDD.md` exists in `docs/` and has been read at least once
- [ ] Dev environment is Windows x64 and can run elevated processes when needed

---

## 2. Tool Packaging & Extraction

**Manifest:**

- [ ] Tools manifest (e.g. in `Aris.Tools`) contains a **DLL injector** entry with:
  - [ ] `id = "dllinjector"`
  - [ ] `version` set
  - [ ] `sha256` hash for `dllinjector.exe`
  - [ ] `relativePath` (e.g. `dllinjector/dllinjector.exe`)
  - [ ] Any support DLLs listed with their own hashes

**Extraction:**

- [ ] `DependencyExtractor` (or equivalent) extracts DLL injector to:
  - [ ] `%LOCALAPPDATA%/ARIS/tools/{version}/dllinjector/`
- [ ] Extraction uses temp-then-move semantics (no partial files on failure)
- [ ] Hash verification is performed for each extracted file
- [ ] A lock/marker file is written that ties the extraction to a manifest hash

**Validation:**

- [ ] Pre-run validation for DLL injector exists (either in adapter or shared validator)
- [ ] Missing/corrupt injector binary → clear `DependencyMissingError` (or equivalent)
- [ ] Startup logs clearly indicate injector readiness/validation status

---

## 3. Command DTOs & Validation

**DTOs exist:**

- [ ] `DllInjectCommand` with fields roughly like:
  - [ ] `ProcessId` and/or `ProcessName`
  - [ ] `DllPath` (payload DLL)
  - [ ] `DllInjectionMethod` enum (e.g. `CreateRemoteThread`, `APC`, etc.)
  - [ ] Optional `Timeout`
  - [ ] Optional `WorkingDirectory`
  - [ ] Optional `RequireElevation` override
  - [ ] `Arguments` collection for DLL entrypoint (allowlisted)

- [ ] `DllEjectCommand` with fields roughly like:
  - [ ] `ProcessId` and/or `ProcessName`
  - [ ] `DllPathOrModuleName`
  - [ ] Optional `Timeout`
  - [ ] Optional `WorkingDirectory`

**Validation rules:**

- [ ] At least one of `ProcessId` / `ProcessName` required
- [ ] Target process resolution:
  - [ ] Resolves to a single running process
  - [ ] Process is x64
  - [ ] Process is **not** in `DllInjectorOptions.DeniedTargets`
  - [ ] Process is in `AllowedTargets` if an allowlist is used
- [ ] `DllPath`:
  - [ ] Absolute path
  - [ ] Under an allowed root (e.g. `workspace/input/payloads/` or a shipped payloads directory)
  - [ ] File exists
  - [ ] Hash matches expected value (manifest or payload metadata)
- [ ] `DllInjectionMethod`:
  - [ ] Must be in `DllInjectorOptions.AllowedMethods`
- [ ] `Arguments`:
  - [ ] Enforced via allowlist / templates (no arbitrary raw strings)
- [ ] `Timeout` validated against `DllInjectorOptions.DefaultTimeoutSeconds` and sane bounds
- [ ] `WorkingDirectory` under workspace temp (or configured staging root)

**Tests:**

- [ ] Unit tests cover:
  - [ ] Bad/missing PIDs
  - [ ] Ambiguous or invalid process names
  - [ ] Denied targets
  - [ ] Payload path outside allowed roots
  - [ ] Hash mismatch for payload
  - [ ] Disallowed injection method
  - [ ] Unsafe/invalid arguments
  - [ ] Timeout rules

---

## 4. Adapter & Process Integration

**Interface:**

- [ ] `IDllInjectorAdapter` exists with methods like:
  - [ ] `InjectAsync(DllInjectCommand, CancellationToken, IProgress<ProgressEvent>)`
  - [ ] `EjectAsync(DllEjectCommand, CancellationToken, IProgress<ProgressEvent>)`
  - [ ] `ValidateAsync(CancellationToken)`

**Implementation:**

- [ ] `DllInjectorAdapter` class exists
- [ ] `ValidateAsync`:
  - [ ] Confirms injector binary + payload root are present and valid
- [ ] `InjectAsync`:
  - [ ] Validates command and target process
  - [ ] Validates payload hash/arch
  - [ ] Builds allowlisted command line:
    - [ ] `dllinjector.exe inject ...`
  - [ ] Uses shared `IProcessRunner`
  - [ ] Uses operation-specific working directory (e.g. `workspace/temp/inject-{operationId}/`)
  - [ ] Captures stdout/stderr (bounded buffers)
  - [ ] Tees output to `workspace/logs/dllinjector-{operationId}.log`
- [ ] `EjectAsync`:
  - [ ] Similar pattern using `dllinjector.exe eject ...`

**Progress:**

- [ ] Adapter emits progress events for:
  - [ ] Resolving process
  - [ ] Validating payload
  - [ ] Injecting/Ejecting
  - [ ] Verifying
  - [ ] Finalizing

---

## 5. Elevation Handling

**Options-driven behavior:**

- [ ] `DllInjectorOptions.RequireElevation` exists (default `true`)
- [ ] `DllInjectCommand.RequireElevation` can override default when allowed
- [ ] When elevation is required:
  - [ ] `IProcessRunner` is configured to run `dllinjector.exe` elevated
  - [ ] If elevation is denied/unavailable:
    - [ ] Operation fails with `ElevationRequiredError` (or equivalent)
    - [ ] No non-elevated attempt is made for that operation

**Tests:**

- [ ] Unit/fault tests simulate:
  - [ ] RequireElevation = true, elevation denied → `ElevationRequiredError`
  - [ ] RequireElevation = false, allowed mode → non-elevated run succeeds (using safe test process)

---

## 6. Result Models & Errors

**Result types:**

- [ ] `DllInjectResult` exists with fields like:
  - [ ] `ExitCode`
  - [ ] `ProcessId`
  - [ ] `ProcessName`
  - [ ] `DllPath`
  - [ ] `DllInjectionMethod`
  - [ ] `Duration`
  - [ ] `Warnings`
  - [ ] Optional `LogExcerpt`

- [ ] `DllEjectResult` exists with fields like:
  - [ ] `ExitCode`
  - [ ] `ProcessId`
  - [ ] `ProcessName`
  - [ ] `DllPath` or module name
  - [ ] `Duration`
  - [ ] `Warnings`
  - [ ] Optional `LogExcerpt`

**Error types:**

- [ ] `ValidationError` for bad commands/targets/payloads
- [ ] `DependencyMissingError` when injector binary is missing or hash-mismatched
- [ ] `ElevationRequiredError` when elevation needed but not available
- [ ] `ToolExecutionError` for non-zero exit / failures from injector
- [ ] `ChecksumMismatchError` for payload hash mismatches (or output validation, if implemented)

**Behavior:**

- [ ] Successful injection/ejection returns respective result types
- [ ] Failures return typed errors, not generic exceptions
- [ ] Errors contain enough context + remediation hints (e.g. “run as admin”, “target process denied by policy”, “payload hash mismatch”)

---

## 7. DllInjectorOptions Configuration

**Options object:**

- [ ] `DllInjectorOptions` exists with fields like:
  - [ ] `DefaultTimeoutSeconds`
  - [ ] `RequireElevation`
  - [ ] `AllowedTargets` (list/regex)
  - [ ] `DeniedTargets`
  - [ ] `AllowedMethods`
  - [ ] `MaxLogBytes`
  - [ ] Optional `StagingRoot`

**Binding & validation:**

- [ ] `DllInjectorOptions` bound from config (e.g. `appsettings.json` → `DllInjector` section)
- [ ] Startup-time validation checks:
  - [ ] `DefaultTimeoutSeconds` sane
  - [ ] `MaxLogBytes` sane
  - [ ] `AllowedMethods` non-empty & only valid enum values
  - [ ] `DeniedTargets` includes obvious “never touch” system processes
- [ ] Misconfigurations produce clear logs or startup failure, not silent fallback

**Usage:**

- [ ] Adapter obeys:
  - [ ] `RequireElevation`
  - [ ] `AllowedTargets` / `DeniedTargets` when resolving targets
  - [ ] `AllowedMethods`
  - [ ] `DefaultTimeoutSeconds` when command doesn’t override
  - [ ] `MaxLogBytes` when capturing log excerpts
  - [ ] `StagingRoot` override for temp dir (if present)

---

## 8. Workspace & Safety

**Workspace layout:**

- [ ] Payloads:
  - [ ] Under `workspace/input/payloads/` (or dedicated shipped path)
- [ ] Temp:
  - [ ] Under `workspace/temp/inject-{operationId}/` (or configured staging root)
- [ ] Logs:
  - [ ] Under `workspace/logs/dllinjector-{operationId}.log`

**Behavior:**

- [ ] All injector-related file I/O stays within configured workspace roots
- [ ] Target process selection is re-validated server-side against allow/deny policies, even if UI filters targets
- [ ] Optional verification step confirms payload module presence in target process (if injector supports it)

---

## 9. Logging & Diagnostics

**Per-run logging:**

- [ ] Each inject/eject operation produces a log file:
  - [ ] `workspace/logs/dllinjector-{operationId}.log`
- [ ] Log file includes:
  - [ ] Redacted command line (no secrets/raw paths if policy requires)
  - [ ] Exit code
  - [ ] Duration
  - [ ] Bounded stdout/stderr excerpts respecting `MaxLogBytes`

**Structured logging:**

- [ ] Application logs emit structured entries with:
  - [ ] `operationId`
  - [ ] `processId`
  - [ ] `processName`
  - [ ] `dllHash` (for payload)
  - [ ] `method`
  - [ ] `elevated` (true/false)
  - [ ] `exitCode`
  - [ ] `duration`

**Redaction:**

- [ ] Sensitive info (e.g. user-specific paths, detailed arguments) is redacted or normalized according to policy
- [ ] Log sizes are capped; no huge uncontrolled log blobs

---

## 10. Tests

**Unit tests:**

- [ ] DTO validation:
  - [ ] PID / process name resolution behavior
  - [ ] Allowed/denied targets
  - [ ] Payload rooting + hash checks
  - [ ] Allowed methods enforcement
- [ ] Options:
  - [ ] `DllInjectorOptions` binding & validation tests
- [ ] Elevation logic:
  - [ ] RequireElevation on/off, overrides
- [ ] Command-line construction:
  - [ ] Only allowlisted flags are emitted, no arbitrary pass-through

**Integration tests:**

- [ ] A safe **test harness process** exists (simple .NET process) specifically for injection tests
- [ ] A benign test DLL exists that can signal successful load (e.g. via event/log/IPC)
- [ ] Integration tests:
  - [ ] Inject test DLL into harness process and confirm success
  - [ ] Eject test DLL (if supported) and confirm unload
  - [ ] Verify results (`DllInjectResult` / `DllEjectResult`) match expectations

**Fault injection tests:**

- [ ] Simulated bad PID / non-existent process → appropriate error
- [ ] Wrong architecture target → appropriate error
- [ ] Payload hash mismatch → `ChecksumMismatchError`
- [ ] Denied elevation → `ElevationRequiredError`
- [ ] Timeout → `ToolExecutionError` / explicit timeout error
- [ ] Forced non-zero exit from injector → `ToolExecutionError`

**Safety:**

- [ ] Tests never attempt injection into non-test, real-world processes
- [ ] Denylist behavior is exercised and confirmed

---

## 11. Phase 4 “Done” Snapshot

Check all before declaring Phase 4 complete:

- [ ] DLL injector is packaged, extracted, and hash-validated
- [ ] `IDllInjectorAdapter`, `DllInjectorAdapter`, `DllInjectCommand`, and `DllEjectCommand` implemented
- [ ] Elevation, allowlist, and denylist policies are enforced
- [ ] `DllInjectResult` / `DllEjectResult` and error types are wired and used correctly
- [ ] `DllInjectorOptions` configured, validated, and respected in behavior
- [ ] Workspace usage for payloads/temp/logs is correct and safe
- [ ] Logs + structured diagnostics are audit-friendly and bounded
- [ ] Unit, integration, and fault-injection tests exist and pass
- [ ] Code follows project-wide “human-style” conventions (no AI meta, no pointless abstraction tangles)

Once all of the above is truly green, the DLL injector is a controlled, auditable part of the ARIS backend and ready to be orchestrated by higher-level workflows and UI phases.
