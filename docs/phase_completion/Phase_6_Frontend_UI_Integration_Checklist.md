# Phase 6 Checklist – Frontend UI Integration & UX Refinement (ARIS C# Rewrite)

This is a **human checklist** mirroring `Phase_6_Frontend_UI_Integration.md`.

Use it to verify the UI is truly user-ready, not just “technically works”.

---

## 1. Preconditions

- [ ] Phase 5 checklist is fully complete:
  - [ ] Backend HTTP/JSON bridge stable (`/health`, `/info`, `/operations/*`, per-tool endpoints)
  - [ ] WebView2 host + minimal tool flows work end-to-end
- [ ] Frontend already has:
  - [ ] Basic shell (sidebar, header, main content)
  - [ ] Minimal tool pages for Retoc, UAsset, DLL, UWP
  - [ ] Minimal Dashboard, Settings, Logs pages

---

## 2. Visual Structure, Panels & Theming

**Shell:**

- [ ] Left sidebar with:
  - [ ] Icons + labels
  - [ ] Active route highlighting
  - [ ] Optional collapse/expand behavior works (or is explicitly out of scope)
- [ ] Header bar with:
  - [ ] Workspace indicator
  - [ ] Backend status chip (Ready / Starting / Error)
  - [ ] Quick actions (Open Workspace, Self-check, Refresh status)

**Panels/Cards:**

- [ ] Each tool page uses panel/card layouts, roughly:
  - [ ] “Context / Configuration” panel
  - [ ] “Inputs” panel
  - [ ] “Actions” panel
  - [ ] “Progress & Logs” panel
  - [ ] “Results” panel
- [ ] Shared card component exists (title, optional subtitle, body, footer) and is reused consistently

**Theming:**

- [ ] Dark, high-contrast theme across the app
- [ ] Single accent color used consistently for primary actions/highlights
- [ ] Theme controlled by tokens (CSS vars / Tailwind design tokens), not hard-coded one-off styles

---

## 3. Tool Forms – Design, Inputs & Validation

### Common Form Behavior

- [ ] Fields grouped with clear headings and short helper text
- [ ] Each field has:
  - [ ] Label
  - [ ] Control
  - [ ] Helper text (where helpful)
  - [ ] Space for inline error
- [ ] Client-side validation for:
  - [ ] Required fields
  - [ ] Basic path shape (no obviously broken text)
  - [ ] Enum selections (modes, UE versions, etc.)
- [ ] Invalid fields:
  - [ ] Show inline error messages
  - [ ] Prevent submission until required fields are fixed
- [ ] Backend `ValidationError` responses:
  - [ ] Map to specific field errors when possible
  - [ ] Otherwise show a clear summary error block

### IoStore / Retoc Page

- [ ] Form includes:
  - [ ] Mode: `Pak → IoStore`, `IoStore → Pak`, `Repack` (or SDD-specified set)
  - [ ] Source file(s) (multi-file support)
  - [ ] Output folder
  - [ ] UE version selector
  - [ ] AES key(s) input
  - [ ] Advanced options section:
    - [ ] Include/exclude filters
    - [ ] Compression options
- [ ] Optional “Validate inputs” action that:
  - [ ] Either calls a dedicated backend validation endpoint OR
  - [ ] Shows backend validation errors cleanly when operation is started

### UAsset Page

- [ ] Form supports modes:
  - [ ] `Deserialize`
  - [ ] `Serialize`
  - [ ] `Inspect`
- [ ] Fields:
  - [ ] Input path (asset or JSON, depending on mode)
  - [ ] Output path
  - [ ] UE version selector
  - [ ] Schema version selector (with sane defaults)
  - [ ] Include bulk data toggle (if applicable)
- [ ] Mode-specific validation:
  - [ ] Deserialize requires asset path + output JSON path
  - [ ] Serialize requires JSON input + asset output path
  - [ ] Inspect requires asset path; output may be optional or fixed

### DLL Injector Page

- [ ] Form supports:
  - [ ] Action: `Inject` / `Eject`
  - [ ] Process selection:
    - [ ] PID input and/or process list dialog (filterable by name)
  - [ ] Payload DLL path (under workspace payloads)
  - [ ] Injection method dropdown (only allowed methods)
  - [ ] Optional arguments (if exposed, allowlisted)
  - [ ] Elevation toggle / indication (especially when required)
- [ ] Validation:
  - [ ] Cannot submit with no process selected
  - [ ] Payload must be chosen for `Inject`
  - [ ] Show warning if elevation required but toggle is off

### UWP Dumper Page

- [ ] Form includes:
  - [ ] PFN (package family name) and/or AppId
  - [ ] Mode: `Full dump` / `Metadata only` (or SDD variants)
  - [ ] Output folder
  - [ ] Include symbols toggle (if applicable)
  - [ ] Elevation toggle
- [ ] PFN/AppId help:
  - [ ] Brief explanation of what PFN is and how to get it
  - [ ] At least basic client-side format checking (not obviously empty/garbage)

---

## 4. Workspace-Aware File & Path Handling

- [ ] Current workspace stored in global state (root path)
- [ ] Workspace subfolders understood in UI:
  - [ ] `input/`
  - [ ] `output/`
  - [ ] `temp/`
  - [ ] `logs/`
  - [ ] `backups/` (if applicable)
- [ ] File/folder pickers:
  - [ ] Use backend/OS dialogs via WebView2 or equivalent bridge
  - [ ] Default to appropriate workspace subfolders for each tool:
    - [ ] Retoc: `input/paks/`, `output/ios/` etc.
    - [ ] UAsset: `input/assets/`, `output/uasset/`
    - [ ] DLL: `input/payloads/`
    - [ ] UWP: `output/uwp/`
- [ ] Path display:
  - [ ] Shows workspace-relative paths in the UI (e.g. `input/paks/Game.pak`)
  - [ ] Full path available via tooltip or smaller secondary text
- [ ] Values sent to backend:
  - [ ] Correctly match backend’s workspace model (no accidental absolute/foreign paths)
- [ ] Recent paths:
  - [ ] Per-tool cached recent paths (assets, payloads, output folders)
  - [ ] Scoped to current workspace

---

## 5. Progress, Logs & Operation Results

**Per-operation progress on each tool page:**

- [ ] Progress panel shows:
  - [ ] Operation status: Pending / Running / Succeeded / Failed
  - [ ] Current step name
  - [ ] Optional percent when provided
  - [ ] Elapsed time
- [ ] Step timeline:
  - [ ] Shows sequence of steps as they’re reported
  - [ ] Completed steps visually distinct from current/pending

**Live log viewer:**

- [ ] Collapsible log panel exists under Progress or Results
- [ ] While operation runs:
  - [ ] Logs stream into the viewer
  - [ ] Auto-scroll enabled by default, with a toggle to pause auto-scroll
- [ ] Display is buffered:
  - [ ] Does not show unbounded logs (sensible line/size cap)

**Results display:**

- [ ] On success:
  - [ ] Status clearly marked as Success
  - [ ] Shows key output paths and summary (e.g. counts, major artifacts)
  - [ ] Shows warnings list (if any)
  - [ ] Offers:
    - [ ] “Open output folder” button
    - [ ] “View full log” button
- [ ] On failure:
  - [ ] Status clearly marked as Failed
  - [ ] Error message from backend shown clearly
  - [ ] Operation id and a “View log” entry are visible

**Operations history:**

- [ ] Global/per-workspace operation history list exists
- [ ] Each entry tracks:
  - [ ] Operation id
  - [ ] Tool
  - [ ] Workspace
  - [ ] Start/end time
  - [ ] Final status

---

## 6. Logs Page & Diagnostics

**Logs list:**

- [ ] Logs/Operations page shows a table with:
  - [ ] Operation id
  - [ ] Tool
  - [ ] Workspace
  - [ ] Start/end times
  - [ ] Status
- [ ] Filters:
  - [ ] Filter by tool
  - [ ] Filter by status
  - [ ] Filter by time range (even simple “Last N hours/days” is fine)

**Per-operation log viewer:**

- [ ] Clicking a row:
  - [ ] Loads that operation’s log
  - [ ] Shows in scrollable text area
  - [ ] Shows summary metadata (duration, exit code, warnings)
- [ ] Actions:
  - [ ] “Copy log to clipboard”
  - [ ] “Open log in Explorer” (via backend bridge, or clearly stubbed)

**Diagnostics export:**

- [ ] A “Export diagnostics” or similar button exists
- [ ] Clicking it:
  - [ ] Calls backend to bundle logs/operation summaries, OR
  - [ ] Shows a placeholder dialog with clear “not yet implemented” messaging if partially stubbed
- [ ] User gets either:
  - [ ] A file (zip/json/etc.), or
  - [ ] A clear path forward when not fully implemented

---

## 7. Settings & Preference Persistence

**Settings model:**

- [ ] Frontend `UserSettings` model includes:
  - [ ] Theme preference
  - [ ] Last workspace
  - [ ] Per-tool defaults (e.g. default UE version, default Retoc mode)
  - [ ] Logging verbosity level
  - [ ] “Keep temp on failure” preference
- [ ] Settings that map to backend options are clearly identified

**Persistence:**

- [ ] On startup:
  - [ ] Settings are loaded from user-scoped storage (file via backend or local storage)
- [ ] On change:
  - [ ] Settings are saved back to storage
- [ ] Where applicable:
  - [ ] Backend is updated to reflect settings (e.g. logging level, temp retention)
  - [ ] Or there’s a clear “frontend-only” indicator where they don’t affect backend

**Settings UI:**

- [ ] Settings page lets user adjust:
  - [ ] Theme
  - [ ] Logging verbosity
  - [ ] Keep-temp behavior
  - [ ] Common tool defaults (at least UE version for relevant tools)
- [ ] Helper text explains each setting and its scope (frontend-only vs backend-affecting)

---

## 8. Accessibility, Keyboard Navigation & i18n Readiness

**Keyboard navigation:**

- [ ] All interactive elements reachable via Tab/Shift+Tab
- [ ] Sidebar navigation usable via keyboard
- [ ] Buttons and toggles respond to Enter/Space as expected

**Semantics:**

- [ ] Main structure uses semantic elements:
  - [ ] `<header>`, `<nav>`, `<main>`, or ARIA role equivalents
- [ ] Inputs have proper labels and `for`/`id` bindings
- [ ] ARIA attributes used where needed (e.g. for dialogs, toasts)

**High-contrast & scaling:**

- [ ] Text contrast is sufficient in dark theme
- [ ] App remains usable with browser/OS text scaling bumped up
- [ ] No critical UI crushed or hidden at moderate window sizes

**Internationalization readiness:**

- [ ] User-facing strings mostly centralized (basic `t()` or message map)
- [ ] Little to no hard-coded English strings buried in JSX that can’t be swapped later

---

## 9. Frontend Testing & Regression Protection

**Unit/form tests:**

- [ ] Validation tests for:
  - [ ] Retoc form (required fields, modes, basic path checks)
  - [ ] UAsset form (mode-dependent required fields)
  - [ ] DLL form (PID/process selection, payload required for inject)
  - [ ] UWP form (PFN required, basic format)
- [ ] DTO shape tests:
  - [ ] Commands built in the frontend match expected backend DTOs (fields, enums)

**API client tests (mocked backend):**

- [ ] Health/info mock tests:
  - [ ] Loading backend config into app state
- [ ] Operations + progress:
  - [ ] Mock operation start + progress events and verify UI state updates correctly
- [ ] Problem Details mapping:
  - [ ] Error responses mapped into `AppError` and shown in UI

**Visual regression / snapshot tests:**

- [ ] Baseline screenshots (or snapshots) captured for:
  - [ ] Dashboard
  - [ ] Each tool page (idle state)
  - [ ] Logs page with some fake data
  - [ ] Typical operation in-progress screen
- [ ] Test suite fails on unexpected big layout/visual changes

**Manual golden-path flows:**

- [ ] For each tool (Retoc, UAsset, DLL, UWP):
  - [ ] Use a small test workspace
  - [ ] Set reasonable inputs, including some advanced options
  - [ ] Run the operation from UI
  - [ ] Observe progress + logs
  - [ ] Confirm outputs + logs are correct and accessible

---

## 10. Phase 6 “Done” Snapshot

Tick ALL of these before you declare Phase 6 complete:

- [ ] UI visually matches dark, high-contrast, panelized design with sidebar + header
- [ ] Each tool page exposes all primary inputs/options from the SDD with sane grouping and helper copy
- [ ] Workspace-aware pickers work and produce backend-acceptable paths
- [ ] Per-operation progress, logs, and results panels are functional and understandable
- [ ] Logs page shows recent operations and supports per-operation log viewing
- [ ] Settings persist across restarts and influence defaults/behavior where expected
- [ ] Backend Problem Details map to good user-visible error states (inline + toasts)
- [ ] Basic diagnostics/export path exists (even if minimal)
- [ ] Keyboard navigation and focus behavior are decent; app doesn’t implode with modest scaling
- [ ] Unit + API client + minimal visual regression tests exist and pass
- [ ] Manual golden-path flows for all tools are smoke-tested and passing
- [ ] Frontend code adheres to the “human-style” rules (no AI/meta comments, no weird overengineering)

When this entire checklist is truly green, the ARIS frontend is **user-ready**, not just “dev-only demo ready”.
