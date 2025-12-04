# Phase 5 Checklist – Minimal UI + Wiring (ARIS C# Rewrite)

This is a **human checklist** mirroring `Phase_5_Minimal_UI_And_Wiring.md`.

Use it to verify that the UI and backend are actually wired together end-to-end, not just theoretically compatible.

---

## 1. Preconditions

- [ ] Phase 0 checklist is complete
- [ ] Phase 1 (Retoc) checklist is complete
- [ ] Phase 2 (UAssetAPI) checklist is complete
- [ ] Phase 3 (UWPDumper) checklist is complete
- [ ] Phase 4 (DLL Injector) checklist is complete
- [ ] `ARIS_Frontend_SDD.md` exists and has been read
- [ ] `ARIS_Backend_SDD.md` exists and has been read
- [ ] `ARIS.UI` already loads the built frontend (Phase 0 baseline)

---

## 2. Backend IPC / HTTP Bridge

**Host & core endpoints:**

- [ ] Backend host (`Aris.Hosting`) runs Kestrel on localhost
- [ ] `GET /health` returns:
  - [ ] JSON with at least readiness + dependency/boot status
- [ ] `GET /info` returns:
  - [ ] Version info
  - [ ] Tool versions or similar metadata
  - [ ] Base operations URL and/or IPC token (if used)

**Tool endpoints:**

- [ ] `POST /retoc` starts a Retoc operation and returns an `operationId`
- [ ] `POST /uasset` starts a UAsset operation (`mode`-based) and returns an `operationId`
- [ ] `POST /dll` starts a DLL injector operation (`inject`/`eject`) and returns an `operationId`
- [ ] `POST /uwp` starts a UWP dump operation and returns an `operationId`

**Operations + progress:**

- [ ] `GET /operations/{id}` returns:
  - [ ] Current status
  - [ ] Final result when complete
- [ ] `GET /operations/{id}/events` (SSE or WebSocket) streams:
  - [ ] Progress events with at least `{ step, message, timestamp, percent? }`

**Errors:**

- [ ] Tool endpoints return Problem Details on error (RFC style), with:
  - [ ] `type`, `title`, `status`, `detail`
  - [ ] Optional `operationId` in extensions

---

## 3. ARIS.UI ↔ Backend Coordination

- [ ] Running `ARIS.UI`:
  - [ ] Starts the backend host (child process or in-proc)
  - [ ] Waits until `/info` is reachable (with timeout/failure path)
- [ ] `ARIS.UI` obtains:
  - [ ] Backend base URL
  - [ ] IPC token (if used)
- [ ] `ARIS.UI` successfully passes backend config to the frontend via:
  - [ ] Query string **or**
  - [ ] `postMessage` / `window.external` style bridge
- [ ] Closing `ARIS.UI`:
  - [ ] Shuts down the backend cleanly (or kills it after a short timeout)
- [ ] If backend dies unexpectedly:
  - [ ] Frontend shows an error state instead of silently breaking

---

## 4. Frontend Shell, Routing & Global State

**Shell layout:**

- [ ] Root `App` renders:
  - [ ] Left sidebar navigation
  - [ ] Header/top bar
  - [ ] Main content area

**Navigation entries present:**

- [ ] Dashboard
- [ ] IoStore / Retoc
- [ ] UAsset
- [ ] DLL Injector
- [ ] UWP Dumper
- [ ] Settings
- [ ] Logs

**Routing (e.g. with React Router):**

- [ ] `/` → Dashboard
- [ ] `/retoc` → IoStore/Retoc page
- [ ] `/uasset` → UAsset page
- [ ] `/dll` → DLL Injector page
- [ ] `/uwp` → UWP Dumper page
- [ ] `/settings` → Settings page
- [ ] `/logs` → Logs page

**Global state holds at least:**

- [ ] `backendConfig` (base URL, token)
- [ ] `backendStatus` (e.g. Ready / Starting / Error)
- [ ] `currentWorkspacePath`
- [ ] `recentOperations` (small, recent list)

---

## 5. Frontend API Client & Progress Wiring

**Client functions:**

- [ ] `getHealth()` works
- [ ] `getInfo()` works
- [ ] `startRetocOperation(command)` hits `/retoc` and returns `operationId`
- [ ] `startUAssetOperation(command)` hits `/uasset` and returns `operationId`
- [ ] `startDllOperation(command)` hits `/dll` and returns `operationId`
- [ ] `startUwpOperation(command)` hits `/uwp` and returns `operationId`
- [ ] `getOperationStatus(id)` hits `/operations/{id}`
- [ ] `subscribeToOperationEvents(id, onEvent)`:
  - [ ] Sets up SSE or WebSocket connection
  - [ ] Calls `onEvent` for each progress event
  - [ ] Handles disconnect/complete states sanely

**Error mapping:**

- [ ] Problem Details are converted into a consistent `AppError` shape with:
  - [ ] `kind` (validation/dependency/tool/network/etc.)
  - [ ] `title`, `detail`
  - [ ] Optional `operationId`
- [ ] Network errors are wrapped into `AppError` as well

---

## 6. Minimal Tool UI Flows

### 6.1 IoStore / Retoc (`/retoc`)

- [ ] Page renders without error
- [ ] Minimal form includes:
  - [ ] Mode (e.g. `Pak → IoStore`, `IoStore → Pak`, `Repack`)
  - [ ] Source path(s)
  - [ ] Output folder
  - [ ] UE version
  - [ ] AES key (single input)
- [ ] Required fields enforced in UI (cannot submit completely empty form)
- [ ] On submit:
  - [ ] Builds a minimal `RetocCommand`
  - [ ] Calls `startRetocOperation`
  - [ ] Subscribes to operation events
- [ ] UI shows:
  - [ ] Progress steps as they arrive
  - [ ] Final summary when done (status + basic output info or error)

### 6.2 UAsset (`/uasset`)

- [ ] Page renders without error
- [ ] Minimal form includes:
  - [ ] Mode: `Deserialize` / `Serialize` / `Inspect`
  - [ ] Input path
  - [ ] Output path
  - [ ] UE version
  - [ ] Schema version (with defaults)
- [ ] Submit:
  - [ ] Builds correct UAsset command DTO based on mode
  - [ ] Calls `startUAssetOperation`
  - [ ] Subscribes to operation events
- [ ] Shows:
  - [ ] Progress information (steps or log)
  - [ ] Summary of outputs / warnings on completion

### 6.3 DLL Injector (`/dll`)

- [ ] Page renders without error
- [ ] Minimal form includes:
  - [ ] Target process (PID or simple selector/stub)
  - [ ] Payload DLL path
  - [ ] Action (`Inject` / `Eject`)
  - [ ] Injection method (simple dropdown)
  - [ ] Elevation required checkbox/toggle
- [ ] Submit:
  - [ ] Builds `DllInjectCommand` or `DllEjectCommand`
  - [ ] Calls `startDllOperation`
  - [ ] Subscribes to events
- [ ] Shows:
  - [ ] Progress (e.g. “resolving process”, “injecting”)
  - [ ] Final result (success/failure, short message)

### 6.4 UWP Dumper (`/uwp`)

- [ ] Page renders without error
- [ ] Minimal form includes:
  - [ ] Package Family Name (PFN)
  - [ ] Mode (`Full dump` / `Metadata only`)
  - [ ] Output folder
  - [ ] Elevation required checkbox/toggle
- [ ] Submit:
  - [ ] Builds `UwpDumpCommand`
  - [ ] Calls `startUwpOperation`
  - [ ] Subscribes to events
- [ ] Shows:
  - [ ] Progress
  - [ ] Basic summary of dumped artifacts (e.g. output folder, counts)

---

## 7. Dashboard, Settings, Logs (Minimal)

### Dashboard (`/`)

- [ ] Shows backend readiness from `/health`
- [ ] Shows current workspace path
- [ ] Shows a short list of recent operations with:
  - [ ] Tool name
  - [ ] Timestamp
  - [ ] Status (running/succeeded/failed)
- [ ] “Self-check” or similar button:
  - [ ] Calls `/health` (or dedicated endpoint) and reflects result

### Settings (`/settings`)

- [ ] Page exists and doesn’t crash
- [ ] Includes minimal controls:
  - [ ] Theme toggle (dark variants)
  - [ ] Logging verbosity selector (even if stubbed)
  - [ ] “Keep temp on failure” toggle
- [ ] Changing settings at least updates local UI state (full persistence can be Phase 6)

### Logs (`/logs`)

- [ ] Page exists and doesn’t crash
- [ ] Shows:
  - [ ] Basic list of recent operations and/or log entries
- [ ] Clicking a log/row:
  - [ ] Either triggers a backend “open log” action **or**
  - [ ] Shows a stub/placeholder explaining log view coming later

---

## 8. Error Handling, Toasts, UX Glue

- [ ] A global error boundary exists:
  - [ ] Catches React render errors
  - [ ] Shows a basic fallback UI with a reset/reload option
- [ ] Toast/notification system exists:
  - [ ] Network errors surface as toasts
  - [ ] Backend Problem Details errors surface as toasts and/or inline messages
- [ ] Per-form:
  - [ ] Validation errors from backend appear as inline field errors where possible
  - [ ] Otherwise appear as a summary error block on the form

---

## 9. Tests & Smoke Checks

**Automated tests:**

- [ ] API client tests:
  - [ ] Problem Details → `AppError` mapping
  - [ ] Progress subscription logic (mock EventSource/WebSocket)
- [ ] Routing tests:
  - [ ] Each route renders the intended page component

**Manual smoke tests (end-to-end):**

For each tool:

- [ ] Configure a test workspace with tiny/safe fixtures
- [ ] Launch `ARIS.UI`
- [ ] Open tool page
- [ ] Fill in minimal valid inputs
- [ ] Submit operation
- [ ] Confirm:
  - [ ] UI shows progress
  - [ ] Backend logs show corresponding operation
  - [ ] UI shows success or failure summary at the end

---

## 10. Phase 5 “Done” Snapshot

Check all before declaring Phase 5 complete:

- [ ] Backend IPC host + endpoints working (`/health`, `/info`, `/operations/*`, `/retoc`, `/uasset`, `/dll`, `/uwp`)
- [ ] `ARIS.UI` reliably starts/stops the backend and passes config to the frontend
- [ ] Frontend shell + routing all render and navigate correctly
- [ ] Each tool page supports a **minimal end-to-end operation** with progress + result summary
- [ ] Dashboard, Settings, Logs exist and behave as basic but non-broken stubs
- [ ] Errors are surfaced clearly (toasts + inline messages); network/backend failure doesn’t crash the UI
- [ ] Automated tests (client + basic routing) exist and pass
- [ ] Manual smoke tests for each tool’s happy path are passing
- [ ] Code maintains the project’s “human-style” constraints (no AI meta, no noisy comments, no wild overengineering)

When all of these are true, you’ve got a **working, minimal ARIS UI** exercising all major backend capabilities, and you’re ready for Phase 6’s deep UX and polish work.
