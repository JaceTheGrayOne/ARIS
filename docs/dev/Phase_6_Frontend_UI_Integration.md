# Execution Document – Phase 6: Frontend UI Integration & UX Refinement

Status: Draft  
Audience: ARIS Frontend Engineers, UX Designers, Backend Integrators  
Related docs:  
- ARIS_Frontend_SDD.md :contentReference[oaicite:0]{index=0}  
- ARIS_Backend_SDD.md :contentReference[oaicite:1]{index=1}  
- Phase_5_Minimal_UI_And_Wiring.md  

---

## 1. Purpose and Scope

This document defines **Phase 6 – Frontend UI Integration & UX Refinement** for the ARIS C# rewrite.

**Goal of this phase:**  
Evolve the Phase 5 “minimal but working” UI into a **fully integrated, user-ready frontend** that:

- Preserves the **current ARIS visual feel** (dark, high-contrast, sidebar, panelized UI) while being implemented as a clean, modern, single-stack frontend. :contentReference[oaicite:2]{index=2}  
- Fully leverages the backend IPC/HTTP bridge and DTO contracts for all tools, logs, and settings.   
- Provides robust validation, progress/log visualization, workspace-aware workflows, and persistent user preferences. :contentReference[oaicite:4]{index=4}  

Phase 6 focuses on **UX completeness, polish, and behavior**, not new backend features.

---

## 2. Preconditions

Do **not** start Phase 6 until:

- Phase 5 is complete:
  - Backend HTTP/JSON bridge and progress endpoints are implemented and stable. :contentReference[oaicite:5]{index=5}  
  - WebView2 host + frontend shell + minimal tool flows are working end-to-end. :contentReference[oaicite:6]{index=6}  

- The following are in place:
  - Basic API client wrappers for `/health`, `/info`, `/operations/*`, and per-tool endpoints.   
  - Minimal Dashboard, Settings, Logs, and per-tool pages exist and do not crash.

If these conditions are not met, complete Phase 5 first.

---

## 3. High-Level Outcomes for Phase 6

By the end of this phase, we want:

1. **Visual + Layout Parity**
   - Dark, high-contrast UI with **sidebar navigation**, **panelized content**, and compact controls that align with the Frontend SDD’s target experience. :contentReference[oaicite:8]{index=8}  

2. **Rich Tool Workflows**
   - Each tool page (IoStore/Retoc, UAsset, DLL Injector, UWP Dumper) exposes **all core inputs and options** from the SDD (modes, filters, keys, toggles) with grouped forms, helper text, and inline validation. :contentReference[oaicite:9]{index=9}  

3. **Workspace-Aware File & Path Handling**
   - Integrated file picker dialogs (for workspace, assets, output folders, payloads).
   - Path controls respect the workspace model and backend expectations. :contentReference[oaicite:10]{index=10}  

4. **Progress, Logs, and Operations Timeline**
   - Per-operation **progress panels** with step timeline, live log tail, and status chips.
   - Logs page shows recent operations with filters and per-operation log viewer. :contentReference[oaicite:11]{index=11}  

5. **Settings & Persistence**
   - User preferences (theme, last workspace, defaults per tool, logging verbosity, temp retention) persisted to a user-scoped settings store and restored at startup.   

6. **Error Handling & Diagnostics**
   - Consistent mapping of backend Problem Details to form errors, contextual messages, and toasts.
   - Basic client-side diagnostics and “support bundle” export path wired, even if partially stubbed.   

7. **Accessibility and Keyboard Flows**
   - Reasonable keyboard navigation, focus outlines, and high-contrast defaults across primary workflows. :contentReference[oaicite:14]{index=14}  

8. **Testing**
   - Frontend validation tests, API integration tests with mocked responses, and basic visual regression/screenshot tests to preserve the look and feel. :contentReference[oaicite:15]{index=15}  

---

## 4. Implementation Steps

### 4.1 Visual Structure, Panels, and Styling

**Objective:** Bring the UI in line with the visual/layout guidelines in the Frontend SDD. :contentReference[oaicite:16]{index=16}  

**Steps:**

1. **Shell refinement**
   - Ensure the shell uses:
     - **Left sidebar** with icons + labels, active item highlighting, and optional collapse mode.
     - **Header bar** with workspace indicator, backend status chip, and quick actions (Open Workspace, Self-check, Refresh).  

2. **Panels/Cards**
   - On each tool page, organize content into stacked panels:
     - **Configuration / Context** (e.g., workspace, UE version, tool defaults).
     - **Inputs** (paths, modes, options).
     - **Actions** (primary buttons, validation).
     - **Progress & Logs** (timeline, live log).
     - **Results** (outputs, links, warnings).
   - Implement a shared “card” component (title, optional subtitle, body, footer) to keep visuals consistent.

3. **Theming**
   - Implement a dark theme with:
     - High-contrast background + foreground.
     - A single accent color for primary CTAs and highlights.
   - Provide a theme token system (CSS vars/Tailwind config) so future tweaks don’t require massive refactors.

**Acceptance criteria:**

- All primary pages follow the shell + panel structure.
- The UI visually matches the described layout and density (dark, panelized, compact). :contentReference[oaicite:17]{index=17}  

---

### 4.2 Form Design, Validation, and Helper Text

**Objective:** Upgrade forms from “minimal inputs” to full, UX-friendly, workspace-aware workflows.   

**Steps (general):**

1. **Form grouping**
   - Group fields with headings and helper text.
   - Use consistent patterns:
     - Label
     - Control
     - Sub-label/helper (short explanation)
     - Inline error space

2. **Client-side validation**
   - Implement per-field validators matching backend rules where possible:
     - Required fields.
     - Path format (basic checks, workspace-relative hints).
     - Selection of valid enum values (modes, UE versions).
   - Show validation errors:
     - Inline below the control.
     - Prevent submission until required fields are valid.

3. **Server-side validation mapping**
   - Map backend Problem Details of type `ValidationError` to:
     - Specific field errors when `extensions` include field names.
     - A summary error panel when errors are general.

**Tool-specific details:**

- **IoStore / Retoc**
  - Inputs: mode, source files (multi), output destination, UE version, AES keys, include/exclude filters, compression options. :contentReference[oaicite:19]{index=19}  
  - Provide:
    - Add/remove for multiple source paths.
    - Small “Advanced options” section for filters and compression.
    - “Validate inputs” button that calls a backend dry-run/validate endpoint or uses `ValidationError` path.

- **UAsset**
  - Inputs: asset/JSON path, output path, UE version, schema version, include bulk data toggle, mode (Serialize/Deserialize/Inspect). :contentReference[oaicite:20]{index=20}  
  - Provide:
    - Clear explanation snippets for each mode.
    - Simple schema version selector with common defaults.

- **DLL Injector**
  - Inputs: process selector, payload selector, method, optional arguments, elevation toggle. :contentReference[oaicite:21]{index=21}  
  - Provide:
    - Process list dialog (PID + name) with filter box.
    - Warnings for elevation-required actions.

- **UWP Dumper**
  - Inputs: PFN/AppId, mode, output folder, include symbols, elevation toggle. :contentReference[oaicite:22]{index=22}  
  - Provide:
    - PFN lookup helper (optional stub in Phase 6: show basic validation hints).
    - “Validate target” action separate from “Run dump”.

**Acceptance criteria:**

- All primary inputs described in the SDD are available in the UI.
- Forms have basic client-side validation + good helper copy.
- Validation errors from backend show up clearly and consistently.

---

### 4.3 Workspace-Aware Path & File Selection

**Objective:** Make file and folder selection **workspace-aware and ergonomic**.   

**Steps:**

1. **Workspace model in UI**
   - Represent the current workspace as:
     - A root path.
     - Derived standard subfolders: `input/`, `output/`, `temp/`, `logs/`, `backups/`. :contentReference[oaicite:24]{index=24}  

2. **File/folder pickers**
   - Implement shared file/folder picker dialogs via:
     - Backend-mediated “open dialog” call, or
     - WebView2-compatible file dialog bridge.
   - For tool forms:
     - Default pickers to workspace subfolders (e.g., `input/assets/`, `output/uasset/`, `input/payloads/`).

3. **Path representation**
   - Display both:
     - A friendly, workspace-relative path (e.g., `input/paks/Game.pak`).
     - A tooltip or expandable “full path” view when needed.
   - Ensure values posted to the backend match the workspace model expected by it (`WorkspacePath` semantics). :contentReference[oaicite:25]{index=25}  

4. **Recent paths**
   - Maintain small caches of:
     - Recent asset paths.
     - Recent payload DLLs.
     - Recent output folders.
   - Store per-tool, tied to the current workspace.

**Acceptance criteria:**

- Users can pick files and folders via dialogs instead of typing full paths.
- Paths are clearly associated with the workspace and accepted by backend APIs.

---

### 4.4 Progress, Logs, and Operations Timeline

**Objective:** Provide rich progress and logging UX tied to the backend operations model.   

**Steps:**

1. **Per-operation progress panel**
   - For each tool page:
     - Show a **timeline** of steps, driven by `ProgressEvent` data (step, message, percent?). :contentReference[oaicite:27]{index=27}  
     - Use status chips/tracks: Pending, Running, Succeeded, Failed.
   - Display:
     - Current step.
     - Optional percent (when provided).
     - Elapsed time.

2. **Live log viewer**
   - Embed a collapsible log panel that:
     - Streams log lines or summaries from the backend or uses operation log fetch endpoints (`/logs/{id}` or similar).   
     - Auto-scrolls to bottom while running (with user toggle to pause auto-scroll).
     - Truncates display to a reasonable buffer size.

3. **Results panel**
   - When an operation completes:
     - Show a summary card with:
       - Status (success/failure).
       - Key outputs (paths, counts).
       - Warnings list.
       - Buttons:
         - “Open output folder in Explorer”.
         - “View full log”.

4. **Operations history (per workspace)**
   - Maintain a per-workspace list of recent operations (tool, when, status, id).
   - Feed this into:
     - Dashboard.
     - Logs page.

**Acceptance criteria:**

- While a job runs, the user sees stepwise progress and log output updating.
- After completion, the user has a clear summary + navigation to outputs/logs.

---

### 4.5 Logs Page and Diagnostics

**Objective:** Make the Logs page a **central place for recent operations and diagnostics**.   

**Steps:**

1. **Logs table**
   - Implement a table with:
     - Operation id.
     - Tool.
     - Workspace.
     - Start time / end time.
     - Status.
   - Provide filters:
     - By tool.
     - By status.
     - By time range.

2. **Per-operation log view**
   - Clicking a row:
     - Fetches the log for that operation (via a dedicated backend endpoint or file-based API).
     - Shows:
       - Log text in a scrollable pane.
       - Basic metadata (duration, exit codes, warnings).
   - Provide:
     - “Copy log to clipboard”.
     - “Open raw log file” (via backend bridge calling Explorer).

3. **Diagnostics / support bundle**
   - Add a basic “Export diagnostics” action:
     - Calls backend to bundle:
       - Recent logs.
       - Operation summaries.
     - Optionally includes client diagnostics (error counts, environment info) as a small JSON snippet.   

**Acceptance criteria:**

- Logs page shows recent operations and supports per-operation log viewing.
- “Export diagnostics” works or has a clearly marked partial implementation.

---

### 4.6 Settings and Preference Persistence

**Objective:** Wire Settings page to a real user-scoped settings store and integrate with backend where appropriate.   

**Steps:**

1. **Settings model**
   - Define a frontend `UserSettings` model including:
     - Theme (dark variants).
     - Last workspace.
     - Per-tool defaults (e.g., default UE version, default Retoc mode).
     - Logging verbosity.
     - “Keep temp on failure” toggle.
   - Mirror key backend options where user overrides are allowed.

2. **Persistence**
   - On startup:
     - Load saved settings from a local storage mechanism:
       - Either user-scoped settings file via backend API, or local storage for WebView2.
   - On change:
     - Persist settings back through the same mechanism.
   - Keep in sync with backend:
     - For options that have backend equivalents (e.g., temp retention, logging verbosity), call appropriate backend endpoints or reload configuration as needed. :contentReference[oaicite:32]{index=32}  

3. **Settings UI**
   - For each setting:
     - Provide clear labels and helper text.
     - Indicate when a setting is a frontend-only preference vs. one that affects backend behavior.

**Acceptance criteria:**

- Settings persist across app restarts.
- Tool forms and defaults respond to user settings (e.g., default UE version is pre-selected).

---

### 4.7 Accessibility, Keyboard Navigation, and Internationalization Readiness

**Objective:** Improve basic accessibility and set up for future localization. :contentReference[oaicite:33]{index=33}  

**Steps:**

1. **Keyboard navigation**
   - Ensure:
     - All interactive controls are reachable via Tab/Shift+Tab.
     - Focus outlines are visible and not hidden by custom styling.
   - Sidebar navigation:
     - Supports arrow key navigation (optional but ideal).
   - Buttons and forms:
     - Provide keyboard activation (Enter/Space) as expected.

2. **Semantics**
   - Use proper landmarks:
     - `<header>`, `<nav>`, `<main>`, etc. (or role equivalents).
   - Ensure labels are bound to inputs and ARIA attributes are used where needed.

3. **Internationalization readiness**
   - Centralize user-facing strings into a simple message map or i18n solution.
   - Avoid hard-coded English in the JSX/TSX where possible; use a `t()`-like helper, even if only English is shipped at first. :contentReference[oaicite:34]{index=34}  

4. **High-contrast and font scaling**
   - Verify contrast ratios for text vs background.
   - Provide at least one font-size scaling option or honor OS/browser scaling gracefully.

**Acceptance criteria:**

- Basic keyboard and focus flows feel sane.
- UI doesn’t visibly break when text scale or window size changes moderately.

---

### 4.8 Frontend Testing & Visual Regression

**Objective:** Add tests that keep UI behavior and appearance stable as the implementation evolves. :contentReference[oaicite:35]{index=35}  

**Steps:**

1. **Unit tests**
   - Form validation:
     - Retoc form: required fields, basic path/mode checks.
     - UAsset form: mode-dependent requirements.
     - DLL injector: PID/process selection + payload path basics.
     - UWP dumper: PFN format basics.
   - DTO shaping:
     - Ensure commands sent to the backend match `Aris.Contracts` expectations (modes, enums, fields). :contentReference[oaicite:36]{index=36}  

2. **API client integration tests (mocked backend)**
   - Mock HTTP responses for:
     - Health/info.
     - Operation start + events.
     - Problem Details errors.
   - Verify:
     - Correct mapping into app state.
     - Error mapping to `AppError` and UI.

3. **Visual regression**
   - Set up screenshot tests against:
     - Dashboard.
     - Each tool page with default state.
     - Log view panel.
   - Ensure layout + major visual elements remain stable across refactors.

4. **Manual smoke flows**
   - One “golden path” per tool:
     - Configure minimal + advanced options.
     - Run.
     - Observe progress.
     - Check results & logs.
   - Record them as documented QA steps.

**Acceptance criteria:**

- Tests run as part of CI or at least a repeatable local test command.
- Visual diffs only break when intentional UI changes occur.

---

## 5. Definition of Done (Phase 6)

Phase 6 is complete when:

1. **Visual + Layout**
   - UI matches the **dark, high-contrast, panelized** design with sidebar navigation, header, and card-based tool pages. :contentReference[oaicite:37]{index=37}  

2. **Tool Workflows**
   - IoStore/Retoc, UAsset, DLL Injector, and UWP Dumper pages expose all primary inputs and options from the Frontend SDD with helpful grouping and validation. :contentReference[oaicite:38]{index=38}  

3. **Workspace Integration**
   - File pickers respect workspace structure and produce paths accepted by the backend workspace model. :contentReference[oaicite:39]{index=39}  

4. **Progress & Logs**
   - Every operation has a usable progress timeline, live log tail, and results panel.
   - Logs page surfaces recent operations and per-operation log views.

5. **Settings & Persistence**
   - User settings persist across restarts and influence defaults and behavior where expected.

6. **Error Handling & Diagnostics**
   - Backend Problem Details are displayed as sensible form errors, toasts, and log entries.
   - A basic diagnostics/export path exists.

7. **Accessibility & i18n Readiness**
   - Keyboard navigation and focus are workable.
   - Text and structure are ready for future localization.

8. **Testing**
   - Unit tests for validation and DTO shaping.
   - API client tests against mocked backend.
   - Visual regression or snapshot tests for key screens.
   - Manual smoke flows documented and passing.

9. **Code Quality**
   - Frontend code remains a **single cohesive stack** (no reintroduction of hybrid legacy structure). :contentReference[oaicite:40]{index=40}  
   - No AI/meta comments; comments focus on explaining non-obvious behavior or constraints.

At this point, ARIS has a **fully integrated, user-ready frontend** that respects the SDDs and is ready for final refinement phases (telemetry tuning, advanced diagnostics, and future features).
