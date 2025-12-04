# Phase 7 Checklist – Final Wiring & Parity Pass (ARIS C# Rewrite)

This is a **human checklist** mirroring `Phase_7_Final_Wiring_And_Parity_Pass.md`.

Use it to decide if C# ARIS is truly ready to stand in for the original.

---

## 1. Preconditions

- [ ] Phases 0–6 checklists are fully complete
- [ ] All core tools are integrated and testable via backend + UI:
  - [ ] Retoc (IoStore/PAK)
  - [ ] UAssetAPI
  - [ ] UWPDumper
  - [ ] DLL injector
- [ ] Frontend:
  - [ ] Tool pages (Retoc, UAsset, DLL, UWP) implemented and wired
  - [ ] Dashboard, Logs, Settings implemented and wired
- [ ] Reference environment exists:
  - [ ] At least one machine with **legacy ARIS** installed and working
  - [ ] Reference workspaces:
    - [ ] Small synthetic workspace
    - [ ] Medium real-world-ish workspace
    - [ ] Large/stress-test workspace

---

## 2. Parity Matrix

- [ ] `docs/ARIS_Parity_Matrix.md` exists
- [ ] Legacy ARIS feature inventory created:
  - [ ] Workspaces & project handling
  - [ ] IoStore / PAK flows
  - [ ] UAsset serialization/inspection flows
  - [ ] UWP dump / SDK extraction flows
  - [ ] DLL injection / runtime tooling
  - [ ] Logs & diagnostics
  - [ ] Settings & preferences
  - [ ] Misc QoL features (recent paths, etc.)
- [ ] For **every** legacy feature/workflow:
  - [ ] Mapped to C# ARIS equivalent (screen/endpoint/flow)
  - [ ] Marked as one of:
    - [ ] `Equivalent`
    - [ ] `Improved`
    - [ ] `IntentionallyChanged` (with rationale noted)
    - [ ] `Dropped` (with rationale noted)
    - [ ] `Deferred` (with issue/ticket reference)
- [ ] High-impact gaps reviewed:
  - [ ] Any critical missing behavior either:
    - [ ] Implemented now, or
    - [ ] Explicitly `Deferred` with clear justification

---

## 3. End-to-End Workflow Sanity Pass

For each workflow: you should have a written test script, legacy ARIS reference behavior, and C# ARIS behavior.

### 3.1 IoStore / Retoc Pipeline

- [ ] PAK → IoStore → PAK round-trip (single small PAK)
  - [ ] Works in legacy ARIS
  - [ ] Works in C# ARIS via UI
  - [ ] Output structure comparable
- [ ] Multi-PAK configuration
  - [ ] Works in legacy ARIS
  - [ ] Works in C# ARIS
- [ ] AES key usage
  - [ ] Keys can be configured & used in C# ARIS
- [ ] Include/exclude filters
  - [ ] Behavior is at least understood and documented vs legacy

### 3.2 UAsset Round-Trip

- [ ] `uasset` → JSON via C# ARIS (Deserialize)
- [ ] Modify JSON minimally
- [ ] JSON → `uasset` via C# ARIS (Serialize)
- [ ] Result:
  - [ ] Game can load the resulting asset
  - [ ] Inspect in ARIS shows expected changes
  - [ ] No unexpected UAssetAPI errors

### 3.3 UWP Dumper → Retoc → UAsset Chain

- [ ] Use UWPDumper to dump a UWP app
- [ ] Verify dump output structure matches expectations
- [ ] Use dumped artifacts where appropriate (e.g., mappings/headers) in later flows:
  - [ ] Retoc/Iostore flows where applicable
  - [ ] UAsset-related workflows where applicable
- [ ] Paths and naming integrate cleanly into workspace model

### 3.4 DLL Injection Flows

- [ ] Test harness process exists (safe dummy app)
- [ ] Inject test DLL:
  - [ ] Operation succeeds
  - [ ] Harness confirms DLL loaded (log, flag, etc.)
- [ ] Eject test DLL (if supported):
  - [ ] Operation succeeds
  - [ ] Harness confirms module removed / behavior as expected
- [ ] All done via C# ARIS UI, not only via backend

---

## 4. Edge Cases, Error Paths & Recovery

For each of these, you should have run a real test and observed UI + logs.

**Tool dependency issues:**

- [ ] Missing Retoc binary → clear error, no crash
- [ ] Missing/corrupt UAssetAPI → clear error
- [ ] Missing/corrupt UWPDumper → clear error
- [ ] Missing/corrupt DLL injector → clear error

**Invalid inputs:**

- [ ] Nonexistent paths → user-friendly validation errors
- [ ] Unsupported UE versions → clear error
- [ ] Bad PFN/AppId → clear error with hint
- [ ] Disallowed DLL injection targets → blocked with policy message

**Resource issues:**

- [ ] Force out-of-disk-space in workspace:
  - [ ] Operation fails with a clear error
  - [ ] No catastrophic state or hidden temp explosion
- [ ] Timeouts for external tools:
  - [ ] Timeout errors are surfaced cleanly
  - [ ] Operations are marked as failed with logs

**Recovery behavior:**

- [ ] After an error:
  - [ ] App remains responsive
  - [ ] Form inputs remain (or are easily restorable) so user can fix and retry
  - [ ] Operation id + log link are available for debugging

---

## 5. Performance & Resource Behavior

**Scenarios set up:**

- [ ] Small workspace scenario defined and run
- [ ] Medium workspace scenario defined and run
- [ ] Large/stress scenario defined and run

**Measured & recorded (in `docs/Performance_Benchmarks.md`):**

For each scenario:

- [ ] Startup time (backend + UI) recorded
- [ ] Representative operation timings recorded:
  - [ ] PAK → IoStore conversion
  - [ ] Asset round-trip
  - [ ] UWP dump
- [ ] CPU & memory peaks noted (rough numbers)
- [ ] Any obvious slow points noted

**Resource behavior:**

- [ ] No obvious memory leak across multiple operations
- [ ] Temp directory behavior:
  - [ ] Cleans up on success (unless configured otherwise)
  - [ ] Retains temp on failure only when configured to
- [ ] Log size growth:
  - [ ] No unbounded log growth over normal use
  - [ ] Rotation or capping confirmed

---

## 6. Logging, Diagnostics & Supportability

**Log content & structure:**

- [ ] Logs include:
  - [ ] ARIS version
  - [ ] Operation ids
  - [ ] Tool names
  - [ ] Normalized workspace identifiers
  - [ ] Key parameters (sanitized; no secrets)
  - [ ] Exit codes and durations
- [ ] Sensitive values (keys, secret paths) are not logged in plaintext

**Correlation:**

- [ ] From UI (error/operation summary) you can:
  - [ ] Identify operation id
  - [ ] Locate corresponding log file quickly
- [ ] From a log file:
  - [ ] See which tool, workspace, and operation it belongs to

**Diagnostics bundle:**

- [ ] “Export diagnostics” command exists
- [ ] Bundle contains:
  - [ ] Recent operation summaries
  - [ ] Relevant logs
  - [ ] ARIS version + tool versions
- [ ] Test “fake support ticket”:
  - [ ] Use diagnostics bundle alone to reconstruct what happened in at least one failure scenario

---

## 7. Packaging, Versioning & Upgrade Path

**Build artifacts:**

- [ ] CI pipeline produces a distributable:
  - [ ] Installer (MSI/setup) **or**
  - [ ] Portable zip (with clear instructions)
- [ ] Artifact contains:
  - [ ] ARIS UI app
  - [ ] Backend assemblies
  - [ ] Tool binaries or signed tool payloads (Retoc, UAssetAPI, UWPDumper, DLL injector)
  - [ ] Default config/templates

**Versioning:**

- [ ] Semantic version (e.g. `1.0.0`, `1.0.0-rc.1`) defined
- [ ] Version appears in:
  - [ ] UI (About dialog/footer)
  - [ ] Backend `/info` endpoint
  - [ ] Logs at startup
- [ ] Version bump is wired into the build pipeline (not manual every time)

**Install/upgrade tests:**

- [ ] Clean machine/VM test:
  - [ ] Install ARIS
  - [ ] Run at least one core workflow per tool successfully
- [ ] Upgrade machine test:
  - [ ] Legacy ARIS already installed with existing workspaces
  - [ ] Install C# ARIS
  - [ ] Confirm:
    - [ ] Existing workspaces remain valid
    - [ ] C# ARIS can operate on those workspaces without breaking them

---

## 8. Documentation & Release Notes

**User docs:**

- [ ] User docs updated to match C# ARIS UI (screens & terminology)
- [ ] No references to legacy Go/Wails implementation specifics
- [ ] Quick-start guides exist for:
  - [ ] PAK → IoStore conversion
  - [ ] UAsset round-trip
  - [ ] UWP dump
  - [ ] DLL injection

**In-app text & help:**

- [ ] Helper text, tooltips, and error messages:
  - [ ] Match current behavior
  - [ ] Use consistent terminology (e.g., “workspace”, “operation”)
- [ ] Any built-in help links point to correct, updated docs

**Release notes:**

- [ ] `RELEASE_NOTES.md` exists
- [ ] Contains:
  - [ ] Summary of major changes vs legacy ARIS
  - [ ] Any new capabilities
  - [ ] Known limitations / deferrals
  - [ ] Links or references to parity matrix where useful

---

## 9. QA Matrix & Go/No-Go

**QA matrix:**

- [ ] QA matrix document exists (e.g., `docs/QA_Matrix.md`) with rows for:
  - [ ] OS versions tested (Win 10/11 variants)
  - [ ] Hardware tiers (low/mid/high)
  - [ ] Workspaces (small/medium/large)
  - [ ] Tools (Retoc, UAsset, DLL, UWP)
- [ ] For each row:
  - [ ] A defined smoke test exists
  - [ ] Pass/fail recorded with notes

**Go/No-Go checklist:**

- [ ] `docs/Release_Go_NoGo_Checklist.md` exists
- [ ] Includes at least:
  - [ ] All Phase 7 sections satisfied
  - [ ] No P0/P1 bugs open without explicit sign-off
  - [ ] Parity matrix has no unintentional regressions
  - [ ] Packaging/install validated on:
    - [ ] Fresh install
    - [ ] Upgrade install
- [ ] Final sign-offs:
  - [ ] Engineering sign-off completed
  - [ ] QA / “user advocate” sign-off completed

---

## 10. Phase 7 “Done” Snapshot

Tick **all** of these before you call Phase 7 complete:

- [ ] Parity matrix is complete; all legacy features/workflows mapped and classified
- [ ] No high-impact features missing or regressed without being `Deferred` or `IntentionallyChanged` with rationale
- [ ] Core workflows (Retoc, UAsset, UWP dump, DLL injection) are reliable on reference workspaces via the C# ARIS UI
- [ ] Error paths (validation, missing tools, timeouts, denied elevation, resource issues) are tested and produce clear, actionable feedback
- [ ] Performance benchmarks exist; no glaring performance/regression vs legacy for normal use
- [ ] Temp/log growth is controlled; no obvious resource leaks in normal runs
- [ ] Logs are structured, correlated with operations, and usable for debugging
- [ ] Diagnostics export produces a genuinely useful support bundle
- [ ] CI produces versioned, installable ARIS builds; install/upgrade flows tested
- [ ] User docs + release notes are aligned with the new implementation
- [ ] QA matrix filled out; Go/No-Go checklist satisfied and signed off
- [ ] Codebase is clean:
  - [ ] No AI/meta comments
  - [ ] No obvious dead code from earlier phases
  - [ ] Structure matches SDD intent

When **all** of the above are genuinely green, C# ARIS isn’t just “working” – it’s ready to replace the original in the wild.

