# Execution Document – Phase 7: Final Wiring & Parity Pass

Status: Draft  
Audience: ARIS Lead Engineer, Backend/Frontend Engineers, QA  
Related docs:  
- ARIS_High_Level_Design_SDD.md  
- ARIS_Backend_SDD.md  
- ARIS_Frontend_SDD.md  
- Phase_0_Environment_And_Scaffolding.md  
- Phase_1_Retoc_Integration.md  
- Phase_2_UAssetAPI_Integration.md  
- Phase_3_UWPDumper_Integration.md  
- Phase_4_DLLInjector_Integration.md  
- Phase_5_Minimal_UI_And_Wiring.md  
- Phase_6_Frontend_UI_Integration.md  

---

## 1. Purpose and Scope

This document defines **Phase 7 – Final Wiring & Parity Pass** for the ARIS C# rewrite.

**Goal of this phase:**  
Take the already-functional C# ARIS and:

- Confirm **functional parity** with the original Go/Wails ARIS where intended.
- Resolve remaining wiring gaps, edge cases, and “rough edges”.
- Stabilize performance, logging, packaging, and user-facing docs for a **release-ready build**.

This phase is about **integration, polish, and risk reduction** – not adding major new features.

---

## 2. Preconditions

Do **not** start Phase 7 until:

- Phases 0–6 are complete and stable:
  - All core tools (Retoc, UAssetAPI, UWPDumper, DLL injector) are integrated and testable via backend APIs.
  - Frontend tool pages, dashboard, logs, and settings are implemented and wired.
- A **reference environment** exists:
  - At least one machine with the **legacy ARIS** installed and working.
  - A set of **reference workspaces** covering:
    - Small synthetic projects.
    - Medium, real-world-ish game projects.
    - At least one “stress” workspace (large PAK/IoStore, many assets).
- Basic automated tests (unit/integration/frontend) are already running cleanly (from earlier phases).

If any of the above is not true, finish those phases first.

---

## 3. High-Level Outcomes for Phase 7

By the end of this phase, we want:

1. **Feature & Behavior Parity**
   - A written **parity matrix** listing all meaningful user-facing features and workflows:
     - Marked as `Equivalent`, `Improved`, `IntentionallyChanged`, `Dropped`, or `Deferred`.
   - No unintentional feature regressions compared to legacy ARIS.

2. **End-to-End Workflow Reliability**
   - All major workflows (IoStore, asset round-trip, UWPDumper → Retoc → UAsset, injection flows) run reliably on reference workspaces.
   - Non-happy paths (validation errors, missing tools, timeouts) behave consistently and predictably.

3. **Performance & Resource Behavior**
   - Reasonable timings on reference workspaces.
   - No obvious leaks, runaway temp folders, or unbounded CPU/memory usage for normal use.

4. **Logging, Diagnostics & Supportability**
   - Logs are structured, discoverable, and sufficient for debugging user issues.
   - Diagnostics export path works and includes the right data.

5. **Packaging & Versioning**
   - CI build produces a **versioned, installable ARIS** (installer or zip) with:
     - Correct tool dependencies included.
     - Version stamped into UI and logs.
     - Clean upgrade behavior (no orphaned old versions).

6. **Documentation & UX Cohesion**
   - Basic user-facing docs updated to reflect the C# ARIS (not the legacy Go/Wails architecture).
   - In-app terminology and docs match.

7. **Release Gate**
   - A simple, explicit **“go/no-go” checklist** is defined and satisfied.

---

## 4. Implementation Steps

### 4.1 Build the Parity Matrix

**Objective:** Make feature parity explicit and measurable, not a fuzzy feeling.

**Steps:**

1. **Feature inventory (legacy ARIS)**
   - From the old ARIS:
     - List all major features and workflows:
       - Tool-level: Retoc flows, UAsset operations, injection, UWP dumping.
       - UI-level: dashboards, workspace handling, logs, settings.
       - Quality-of-life: recent paths, error messages, progress types.
   - Group into logical categories, e.g.:
     - Workspaces & project handling
     - IoStore / PAK flows
     - UAsset serialization/inspection
     - UWP dumping & SDK extraction
     - Injection / runtime tooling
     - Logs & diagnostics
     - Settings & preferences

2. **C# ARIS parity mapping**
   - For each feature row, add:
     - C# ARIS equivalent (screen/endpoint/flow).
     - Status:
       - `Equivalent` – matches legacy behavior closely.
       - `Improved` – clearly better; note the improvement.
       - `IntentionallyChanged` – behavior is different on purpose; document rationale.
       - `Dropped` – deliberately not implemented; document rationale.
       - `Deferred` – planned but not required for initial C# release.

3. **Gap analysis**
   - For anything not `Equivalent` or `Improved`:
     - Decide whether to:
       - Implement now in Phase 7.
       - Mark as `Deferred` with a clear issue/ticket.
       - Accept as `IntentionallyChanged` with explanation.
   - Update doc with “high severity” gaps (things users relied on).

**Acceptance criteria:**

- Parity matrix exists as a markdown/Excel-style table in `docs/ARIS_Parity_Matrix.md`.
- Every legacy ARIS feature/workflow has a row with a clear status and mapping.

---

### 4.2 End-to-End Workflow Wiring & Sanity Pass

**Objective:** Confirm that all real-world workflows are **actually usable**, not just technically possible.

**Core workflows to exercise (at minimum):**

1. **IoStore/Retoc pipeline**
   - PAK → IoStore → PAK (round-trip) with:
     - A single small PAK.
     - A multi-PAK configuration.
   - Include AES key usage and simple include/exclude filters.

2. **UAsset round-trip**
   - `uasset` → JSON → modified JSON → `uasset` (and sidecars).
   - Validate that:
     - Game loads the resulting asset.
     - UAsset inspection reflects expected changes.

3. **UWP Dumper → Retoc → UAsset chain**
   - Dump UWP SDK/headers.
   - Use generated information (where relevant) in Retoc or other flows.
   - Make sure file locations and naming work in the workspace model.

4. **DLL injection flows**
   - Safe test harness process:
     - Inject test DLL.
     - Confirm success.
     - Eject (if supported) and confirm.

**Steps:**

- For each workflow:
  1. Define a **step-by-step test script** (inputs, expected outcomes).
  2. Run it in the legacy ARIS and capture:
     - Time.
     - Logs.
     - Output structure.
  3. Run it in C# ARIS:
     - Confirm the workflow is discoverable and usable via UI.
     - Compare outputs and logs vs legacy (within reason).
  4. Document any discrepancies:
     - Fix now if high impact.
     - Otherwise mark in parity matrix with notes.

**Acceptance criteria:**

- All core workflows are proven to work in C# ARIS, from UI to backend to filesystem.
- Any intentional differences versus legacy are documented.

---

### 4.3 Edge Cases, Error Paths & Recovery

**Objective:** Make error handling **boringly predictable** and non-destructive.

**Key scenarios:**

- Missing or corrupted tool binaries (Retoc, UAssetAPI, UWPDumper, injector).
- Invalid inputs:
  - Nonexistent paths.
  - Unsupported UE versions.
  - Bad PFN/AppId.
  - Disallowed targets for DLL injection.
- Resource issues:
  - Out-of-disk-space in workspace.
  - Timeout situations for external tools.
- Partial successes:
  - Some assets converted, others failed.
  - One step in a multi-step workflow fails.

**Steps:**

1. **Map error types → UI behavior**
   - For each backend error type (ValidationError, DependencyMissingError, ElevationRequiredError, ToolExecutionError, ChecksumMismatchError, etc.):
     - Confirm:
       - User-visible message.
       - Where it appears (inline, toast, log).
       - Suggested remediation (if any).
   - Ensure no backend error type produces a blank or generic “something failed” without detail.

2. **Simulate and test**
   - Using fault injection (e.g., toggling config, renaming binaries, injecting bad paths), trigger each key error type.
   - Observe:
     - Does ARIS stay responsive?
     - Is the workspace left in a sane state (no orphaned temp data explosion)?
     - Does the UI guide the user clearly?

3. **Recovery behavior**
   - After an error:
     - Can the user fix configuration and re-run?
     - Are inputs preserved in the form so they don’t have to start over?
     - Is there a reasonable breadcrumb (e.g., operation id + log link)?

**Acceptance criteria:**

- All major error types produce clear, actionable UI feedback.
- Operations failing do not corrupt the workspace or crash the app.
- User can recover and retry without restarting ARIS.

---

### 4.4 Performance & Resource Behavior

**Objective:** Ensure ARIS is fast enough and not a resource gremlin for typical use.

**Steps:**

1. **Define test scenarios**
   - At minimum:
     - Small workspace: “toy” project.
     - Medium workspace: representative real project.
     - Large workspace: stress test (large PAK/IoStore, many assets).

2. **Measure**
   - For each scenario:
     - Measure:
       - Startup time (backend + UI).
       - Time for representative operations:
         - PAK→IoStore conversion.
         - Asset round-trip.
         - UWP dump on a mid-size app.
       - CPU and memory peaks.
   - Capture a few key metrics in a `docs/Performance_Benchmarks.md`.

3. **Optimize / configure**
   - Look for:
     - Unnecessary repeated extractions or hash checks.
     - Overly chatty logging causing slowdowns.
     - Excessive concurrency (too many operations at once) or too little.
   - Adjust:
     - Default timeouts.
     - Max concurrent operations.
     - Logging verbosity defaults.

4. **Temp & logs lifecycle**
   - Confirm:
     - Temp folders are cleaned up per configuration (or retained only on failure if configured).
     - Logs are rotated or capped (no unbounded growth).

**Acceptance criteria:**

- C# ARIS is at least “not worse” than legacy ARIS for typical workflows, with any regressions understood.
- No obvious runaway resource behavior in normal usage.
- Temp and logs do not grow unboundedly over typical usage patterns.

---

### 4.5 Logging, Diagnostics & Supportability

**Objective:** Make sure future-you (or future-support-you) can actually debug issues.

**Steps:**

1. **Log structure audit**
   - Ensure logs contain:
     - Operation ids.
     - Tool names.
     - Workspace path (or normalized identifiers).
     - Key parameters (sanitized).
     - Outcome, exit codes, durations.
   - Confirm sensitive data (keys, personal paths, secrets) are not logged in plaintext.

2. **Correlation & discoverability**
   - From a user’s error popup or operation summary:
     - It should be possible to locate the relevant log quickly:
       - Operation id in the UI.
       - Operation id or timestamp in log file names and contents.

3. **Diagnostics bundle**
   - Finalize **export diagnostics** flow:
     - Bundle:
       - Recent operation summaries.
       - Relevant logs.
       - ARIS version + tool versions.
     - Produce a single archive (zip) suitable for attaching to a bug report.

4. **Verification**
   - Run through a “fake support ticket” scenario:
     - Simulate a user error.
     - Use only the diagnostics bundle + parity matrix to reason about what went wrong.

**Acceptance criteria:**

- Logs are structured, not random text walls.
- Given an operation id or error, you can find the log and understand what happened.
- Diagnostics export produces a useful bundle.

---

### 4.6 Packaging, Versioning & Upgrade Path

**Objective:** Produce a **shippable ARIS build** that installs/upgrades cleanly.

**Steps:**

1. **Build artifact definition**
   - Decide on primary distribution format:
     - Installer (MSI/Setup).
     - Portable zip.
   - Ensure artifact includes:
     - ARIS UI executable.
     - Backend assemblies.
     - Tool binaries (Retoc, UAssetAPI, UWPDumper, injector) or tool-packaging payloads.
     - Default config templates.

2. **Versioning**
   - Implement version stamping:
     - Semantic version (e.g., `1.0.0-rc.1`).
     - Embedded into:
       - UI (About dialog / footer).
       - Backend `/info` endpoint.
       - Logs (on startup).
   - Ensure version increments are part of the build pipeline.

3. **Installation & upgrade behavior**
   - Confirm:
     - Installing a new version does not break existing workspaces.
     - Temp/tools installations in `%LOCALAPPDATA%` are versioned or compatible.
   - If needed:
     - Provide a simple migration script for config or workspace structure changes.

4. **Smoke test install**
   - On a **clean machine** (or VM):
     - Install ARIS.
     - Run through at least one core workflow per tool.
   - On an **upgrade machine**:
     - Install legacy ARIS, run basic workflows.
     - Install new ARIS.
     - Confirm:
       - Old workspaces remain valid.
       - New ARIS can work with them.

**Acceptance criteria:**

- A reproducible CI pipeline creates ARIS distributables.
- Version information is visible and consistent.
- Install and upgrade flows are predictable and documented.

---

### 4.7 Documentation & Release Notes

**Objective:** Align docs and UX with the new architecture and behaviors.

**Steps:**

1. **Update user docs**
   - Ensure user-facing docs:
     - Reference the C# ARIS UX (screens, terms).
     - Do not mention legacy Go/Wails implementation details.
   - Add quick-start guides for the main workflows:
     - “Convert PAK → IoStore”
     - “Round-trip a UAsset”
     - “Dump a UWP app”
     - “Inject a debug DLL into a target process”

2. **In-app help & copy audit**
   - Check:
     - Tooltips, helper text, error messages match current behavior.
     - Terminology is consistent (e.g., “workspace”, “operation”, “tool”).

3. **Release notes**
   - Draft `RELEASE_NOTES.md` for the first C# release, summarizing:
     - Major differences vs legacy ARIS.
     - New capabilities.
     - Known limitations/deferrals.
   - Cross-link to parity matrix where relevant.

**Acceptance criteria:**

- Docs are not misleading or tied to legacy UI/architecture.
- Users can get from “Install ARIS” to “first useful result” using the docs alone.

---

### 4.8 Final QA Matrix & Go/No-Go Checklist

**Objective:** Establish a simple, explicit bar for “we’re okay shipping this”.

**Steps:**

1. **QA matrix**
   - Define rows:
     - OS versions (Win 10/11 variants).
     - Hardware tiers (low/mid/high).
     - Workspaces (small/medium/large).
     - Tools (Retoc, UAsset, DLL, UWP).
   - For each combo:
     - Identify a minimal smoke test to run.
     - Record pass/fail + notes.

2. **Go/No-Go checklist**
   - Summarize the critical conditions for release:
     - All Phase 7 sections satisfied.
     - No known P1/P0 issues open.
     - Parity matrix has no unintentional regressions left.
     - Packaging/install validated on at least one fresh and one upgrade install.
   - Put this in `docs/Release_Go_NoGo_Checklist.md`.

3. **Sign-off**
   - Have at least:
     - One engineering sign-off.
     - One “user advocate”/QA sign-off.

**Acceptance criteria:**

- QA matrix is filled out and stored in `docs/`.
- Go/No-Go checklist exists and is checked off before declaring “release-ready”.

---

## 5. Definition of Done (Phase 7)

Phase 7 is complete when **all** of the following are true:

1. **Parity**
   - `ARIS_Parity_Matrix.md` exists and every legacy ARIS feature/workflow has a mapped status.
   - No high-impact feature is missing or regressed without being `Deferred` or `IntentionallyChanged` with rationale.

2. **Workflows**
   - Core workflows (Retoc, UAsset, UWP dump flows, DLL injection) run reliably in C# ARIS on reference workspaces.
   - Differences vs legacy are documented and intentional.

3. **Error Handling**
   - Major error paths (validation, missing tools, timeouts, denied elevation, etc.) are tested and produce clear, actionable UI feedback.
   - Failures do not corrupt workspaces or require reinstall.

4. **Performance & Resources**
   - `Performance_Benchmarks.md` captures representative timings.
   - Resource usage is sane and temp/log growth is controlled.

5. **Logging & Diagnostics**
   - Logs are structured and correlate with operations visible in the UI.
   - Diagnostics export produces a useful bundle for debugging.

6. **Packaging & Versioning**
   - CI produces a versioned ARIS build (installer/zip).
   - Install and upgrade flows are tested and documented.

7. **Docs & UX**
   - User docs match the new UI and flows.
   - Release notes describe differences vs legacy and known limitations.

8. **QA & Release Gate**
   - QA matrix runs are completed and recorded.
   - Go/No-Go checklist is satisfied and signed off.

9. **Code Quality**
   - Codebase is clean, with:
     - No AI/meta comments.
     - No obvious dead code from prior phases.
     - Reasonable structure matching SDDs.

At that point, the C# ARIS rewrite is **release-ready** and can be treated as the primary ARIS going forward.
