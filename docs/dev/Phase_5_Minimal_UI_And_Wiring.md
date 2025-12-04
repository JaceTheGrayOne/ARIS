# Execution Document – Phase 5: Minimal UI + Wiring

Status: Draft  
Audience: ARIS C# Implementation Engineer (Claude Code), Frontend Engineers, human reviewers  
Related docs:  
- ARIS_Frontend_SDD.md :contentReference[oaicite:0]{index=0}  
- ARIS_Backend_SDD.md :contentReference[oaicite:1]{index=1}  
- Phase_0_Environment_And_Scaffolding.md  
- Phase_1_Retoc_Integration.md  
- Phase_2_UAssetAPI_Integration.md  
- Phase_3_UWPDumper_Integration.md  
- Phase_4_DLLInjector_Integration.md  

---

## 1. Purpose and Scope

This document defines **Phase 5 – Minimal UI + Wiring** for the ARIS C# rewrite.

**Goal of this phase:**  
Stand up a **functional, minimal frontend** and backend IPC surface so that:

- Each major backend capability (Retoc, UAssetAPI, DLL Injector, UWPDumper) can be invoked from the UI **end-to-end**, using real commands, operations, and progress. :contentReference[oaicite:2]{index=2}  
- The **basic layout** (shell, sidebar, main content, per-tool views) matches the Frontend SDD at a coarse level without full UX polish. :contentReference[oaicite:3]{index=3}  
- Backend **HTTP/JSON endpoints** and **progress streaming** are wired up according to the Backend SDD’s IPC/HTTP bridge. :contentReference[oaicite:4]{index=4}  

Phase 5 focuses on **plumbing and minimal flows**, not full styling or all advanced options. Detailed UX, settings, logs view, and advanced validation are Phase 6 concerns.

---

## 2. Preconditions

Do **not** start Phase 5 until:

- Phases 0–4 are complete:
  - Backend layering, DI, logging, configuration, workspace handling are in place. :contentReference[oaicite:5]{index=5}  
  - Tool adapters for Retoc, UAssetAPI, UWPDumper, and DLL injector exist and are tested at the backend level.
  - WebView2 host (`ARIS.UI`) loads the static frontend bundle (Phase 0) but may not yet be wired to backend.

- `ARIS_Frontend_SDD.md` and `ARIS_Backend_SDD.md` are present in `docs/` and have been read at least once. :contentReference[oaicite:6]{index=6} :contentReference[oaicite:7]{index=7}  

---

## 3. High-Level Outcomes for Phase 5

By the end of this phase, we want:

1. **Backend IPC Surface**
   - Kestrel-hosted HTTP/JSON running on localhost with:
     - Health + info endpoints: `GET /health`, `GET /info`. :contentReference[oaicite:8]{index=8}  
     - Tool operation endpoints: `POST /retoc`, `POST /uasset`, `POST /dll`, `POST /uwp`. :contentReference[oaicite:9]{index=9}  
     - Operation result and progress endpoints: `GET /operations/{id}`, `GET /operations/{id}/events` (SSE or WebSocket). :contentReference[oaicite:10]{index=10}  
   - DTOs for commands/results in `Aris.Contracts`, consistent with the tool SDDs.

2. **Frontend Shell + Navigation**
   - Single-window, dark, sidebar-based layout with routed views:
     - Dashboard
     - IoStore/Retoc
     - UAsset Serialization
     - DLL Injector
     - UWP Dumper
     - Settings (minimal stub)
     - Logs (minimal stub) :contentReference[oaicite:11]{index=11}  

3. **Minimal Tool Flows**
   - Each major tool has a **simple, working form** that:
     - Collects minimum required inputs.
     - Calls the corresponding backend endpoint.
     - Subscribes to progress events.
     - Shows basic progress + completion status + summary. :contentReference[oaicite:12]{index=12}  

4. **Workspace & Status Wiring**
   - Global workspace selector in the shell (reads/writes workspace path to backend).
   - Global backend status indicator (ready/extracting/error) from `/health`/`/info`. :contentReference[oaicite:13]{index=13}  

5. **Error Handling**
   - Problem Details responses mapped to inline form errors and a simple global toast/banner. :contentReference[oaicite:14]{index=14} :contentReference[oaicite:15]{index=15}  

6. **Basic Testing**
   - Lightweight automated tests for the API client and routing.
   - Manual smoke tests for each tool’s minimal path.

---

## 4. Implementation Steps

### 4.1 Backend IPC/HTTP Bridge (Minimal Implementation)

**Objective:** Implement the minimal HTTP/JSON API and progress streaming required by the frontend.

**Steps:**

1. **Configure Kestrel host**

   - In `Aris.Hosting`:
     - Use the Backend SDD’s IPC strategy: Kestrel HTTP/JSON on a random localhost port with an auth token. :contentReference[oaicite:16]{index=16}  
     - Expose:
       - `GET /health` → reports readiness, dependency preparation status, current workspace (if any).
       - `GET /info` → returns version, tool versions, IPC token, and `operationsBaseUrl`.

2. **Operations API Surface**

   - Implement the following minimal endpoints, using DTOs from `Aris.Contracts`:

     - `POST /retoc` → starts an IoStore/PAK conversion; returns `{ operationId }`.
     - `POST /uasset` → starts serialize/deserialize/inspect based on a `mode` field and the UAsset command DTOs. :contentReference[oaicite:17]{index=17}  
     - `POST /dll` → inject/eject based on `mode` (`inject`/`eject`) and DLL command DTOs. :contentReference[oaicite:18]{index=18}  
     - `POST /uwp` → starts a UWP dump job. :contentReference[oaicite:19]{index=19}  

   - Each endpoint:
     - Validates the request body (basic checks).
     - Dispatches to the corresponding application command handler (`ConvertPackage`, `SerializeAsset`, `InjectDll`, `DumpUwpSdk`, etc.). :contentReference[oaicite:20]{index=20}  
     - Creates an operation record with a unique `operationId`.
     - Returns 202/201 with `{ operationId }` (plus maybe a simple status URL).

3. **Operations and Progress Endpoints**

   - Implement:
     - `GET /operations/{id}` → returns current status + final result when complete.
     - `GET /operations/{id}/events` → SSE or WebSocket stream of `ProgressEvent` objects from the backend pipelines. :contentReference[oaicite:21]{index=21}  

   - Map from the existing backend `ProgressEvent` model to a JSON envelope suitable for the frontend (step name, message, percent, timestamp).

4. **Errors and Problem Details**

   - For all tool endpoints:
     - On validation failure, dependency issues, tool errors, etc., return RFC-9457 Problem Details with:
       - `type`, `title`, `status`, `detail`, and `extensions.operationId`. :contentReference[oaicite:22]{index=22}  
     - Keep the shape consistent across tools so the frontend can handle them uniformly.

**Acceptance criteria:**

- Backend can be started as a host process.
- `GET /health` and `GET /info` respond with sensible JSON.
- Tool endpoints accept minimal commands and return operation IDs.
- `GET /operations/{id}/events` streams progress for a running operation.

---

### 4.2 WebView2 Host and Backend Coordination

**Objective:** Ensure `ARIS.UI` starts the backend, obtains IPC details, and passes them to the frontend in a predictable way.

**Steps:**

1. **Backend startup from ARIS.UI**

   - In `ARIS.UI`:
     - Start the backend host process as a child process (if not in-proc).
     - Wait for `/info` to become available (poll for a short time, with timeout).
     - Extract:
       - Base URL (host + port).
       - IPC token (if used).
       - Any additional configuration needed by the frontend.

2. **Configuration handoff to frontend**

   - Provide backend config to the WebView2 instance via:
     - Query string parameters (e.g., `index.html?baseUrl=...&token=...`), **or**
     - A `window.external`/`postMessage`-based bridge that sends a configuration object after WebView2 loads. :contentReference[oaicite:23]{index=23}  

   - The frontend should read this configuration once at boot and store it in a central app config store.

3. **Shutdown handling**

   - When `ARIS.UI` closes:
     - Gracefully stop the backend host process (send a shutdown signal or kill after timeout).
   - Handle backend crash by:
     - Showing a simple error overlay in the frontend.
     - Optionally asking user to restart ARIS.

**Acceptance criteria:**

- Running `ARIS.UI` launches both backend and frontend.
- Frontend receives base URL + token and uses them for API calls.
- Closing ARIS shuts down the backend in a controlled way.

---

### 4.3 Frontend Shell, Routing, and Global State

**Objective:** Implement the basic app shell, routing, and global state containers described in the Frontend SDD. :contentReference[oaicite:24]{index=24}  

**Steps:**

1. **App shell layout**

   - In `frontend/src/`:
     - Implement a root `App` component that renders:
       - Left sidebar navigation (Dashboard, IoStore, UAsset, DLL, UWP, Settings, Logs).
       - Top bar/header with:
         - Active workspace indicator.
         - Backend status indicator (Ready / Starting / Error).
         - “Open Workspace” and “Refresh status” buttons. :contentReference[oaicite:25]{index=25}  
       - Main routed content area.

   - Use Tailwind for layout and dark theme, but keep styling minimal in this phase.

2. **Routing**

   - Use React Router (or chosen router) with routes:
     - `/` → Dashboard
     - `/retoc`
     - `/uasset`
     - `/dll`
     - `/uwp`
     - `/settings`
     - `/logs` :contentReference[oaicite:26]{index=26}  

3. **Global state**

   - Implement a simple global state mechanism (Context + reducer or a minimal state library) to hold:
     - `backendConfig` (base URL, token).
     - `backendStatus` (enum + last checked time).
     - `currentWorkspacePath`.
     - `recentOperations` (small in-memory cache for Phase 5). :contentReference[oaicite:27]{index=27}  

**Acceptance criteria:**

- Sidebar + header + main content render correctly.
- Navigating between sections updates the URL and active nav item.
- Backend status indicator reflects `/health` responses.

---

### 4.4 Frontend API Client and Progress Wiring

**Objective:** Implement a thin API client that matches the backend endpoints and supports progress streams. :contentReference[oaicite:28]{index=28} :contentReference[oaicite:29]{index=29}  

**Steps:**

1. **API client module**

   - Create `frontend/src/api/client.ts` that provides:

     - `getHealth()`
     - `getInfo()`
     - `startRetocOperation(command)`
     - `startUAssetOperation(command)`
     - `startDllOperation(command)`
     - `startUwpOperation(command)`
     - `getOperationStatus(id)`
     - `subscribeToOperationEvents(id, onEvent)` (SSE or WebSocket)

   - These functions:
     - Use `backendConfig.baseUrl` and attach the IPC token (e.g., auth header or query param).
     - Parse JSON responses and map Problem Details into a unified error shape.

2. **Progress streaming**

   - Implement `subscribeToOperationEvents` using:
     - **SSE** via `EventSource`, or
     - WebSocket (if the backend is configured that way). :contentReference[oaicite:30]{index=30} :contentReference[oaicite:31]{index=31}  
   - Normalize progress messages to a shape like:
     - `{ operationId, step, message, percent?, timestamp }`.

3. **Error mapping**

   - Define a small `AppError` type for the frontend with:
     - `kind` (validation, dependency, tool, network, unknown).
     - `title`, `detail`, and optional `operationId`.
   - Map Problem Details (type/title/status/detail) to `AppError`. :contentReference[oaicite:32]{index=32}  

**Acceptance criteria:**

- API client functions can successfully hit backend endpoints.
- Progress subscriptions deliver events through the callback.
- Errors are consistently mapped to `AppError`.

---

### 4.5 Minimal Per-Tool UI Flows

**Objective:** For each tool, implement a simple but functional form + progress display that exercises the full backend path. :contentReference[oaicite:33]{index=33}  

> Note: In Phase 5, **only required fields** and essential behaviors are implemented. Advanced options, rich validation, and UX refinements are Phase 6 work.

#### 4.5.1 IoStore / Retoc Page

- Route: `/retoc`.
- Minimal form fields:
  - Mode (select: `Pak → IoStore`, `IoStore → Pak`, `Repack`).
  - Source path(s) (text field for now; later, integrate file picker).
  - Output baseline folder (defaults from workspace).
  - UE version (simple dropdown).
  - AES key (single text input; later integrate key store UI). :contentReference[oaicite:34]{index=34}  

- Behavior:
  - On submit:
    - Build minimal `RetocCommand`.
    - Call `startRetocOperation`.
    - Subscribe to `operations/{id}/events`.
    - Update a small progress panel (step list + spinner/status).
  - On completion:
    - Fetch final result via `getOperationStatus`.
    - Show simple summary: status, produced files count/path.

#### 4.5.2 UAsset Serialization Page

- Route: `/uasset`.
- Minimal form fields:
  - Mode: `Deserialize`, `Serialize`, `Inspect`.
  - Input path (asset for deserialize/inspect; JSON for serialize).
  - Output path (JSON for deserialize; asset for serialize).
  - UE version (dropdown).
  - Schema version (dropdown or free text with defaults). :contentReference[oaicite:35]{index=35}  

- Behavior:
  - On submit:
    - Build `UAssetSerializeCommand` / `UAssetDeserializeCommand` / `UAssetInspectCommand`.
    - Call `startUAssetOperation`.
    - Subscribe to progress and show step log.
  - On completion:
    - Show summary: operation type, outputs, warnings.

#### 4.5.3 DLL Injector Page

- Route: `/dll`.
- Minimal form fields:
  - Target process:
    - Input for PID (or simple dropdown populated from a `/processes` endpoint if available; stub allowed in Phase 5).
  - Payload DLL path (under workspace payloads).
  - Action: `Inject` or `Eject`.
  - Injection method (basic dropdown – one or two safe defaults).
  - Elevation required (checkbox reflecting backend options). :contentReference[oaicite:36]{index=36}  

- Behavior:
  - On submit:
    - Build `DllInjectCommand` or `DllEjectCommand` DTO.
    - Call `startDllOperation`.
    - Subscribe to progress & show a compact log + verification status (“Injected”, “Ejected” or “Failed”).

#### 4.5.4 UWP Dumper Page

- Route: `/uwp`.
- Minimal form fields:
  - Package family name (PFN).
  - Mode: `Full dump` / `Metadata only`.
  - Output folder (within workspace).
  - Elevation required (checkbox, default on). :contentReference[oaicite:37]{index=37}  

- Behavior:
  - On submit:
    - Build `UwpDumpCommand`.
    - Call `startUwpOperation`.
    - Subscribe to progress; show active step + overall status.
  - On completion:
    - Show summary of artifacts (folders, counts).

**Acceptance criteria (per tool page):**

- Basic form renders and is usable.
- Required fields enforced with simple frontend checks.
- Submitting triggers backend operation and progress streaming.
- Completion shows a concise result summary or clear error.

---

### 4.6 Dashboard, Settings, and Logs (Minimal Stubs)

**Objective:** Provide minimal but useful versions of Dashboard, Settings, and Logs pages that match the SDD’s intent without full feature depth. :contentReference[oaicite:38]{index=38}  

1. **Dashboard (`/`)**

   - Shows:
     - Backend readiness (from `/health`).
     - Current workspace.
     - A simple list of recent operations (`recentOperations` from global state), with:
       - Tool name
       - Time
       - Status (success/failure/running)
   - Button: “Run self-check” which calls a simple backend validation endpoint (or uses `/health` + dependency check).

2. **Settings (`/settings`)**

   - Minimal controls:
     - Theme toggle (dark only vs dark+slightly-brighter variant).
     - Logging verbosity (low/normal/high – maps to a setting in user config but can be stubbed).
     - Keep temp on failure (checkbox mapping to backend options). :contentReference[oaicite:39]{index=39}  

   - In Phase 5, updating settings may just update local state and log to console; real persistence is Phase 6.

3. **Logs (`/logs`)**

   - Minimal list:
     - Table of recent operations (from `recentOperations` + simple backend `GET /logs`/`/operations` summary endpoint if available).
   - Clicking a row:
     - For Phase 5, simply opens the log file location via a “Show in Explorer” call through the backend, or shows a placeholder message.

**Acceptance criteria:**

- Dashboard, Settings, Logs pages exist and don’t crash.
- Dashboard reflects backend readiness and recent operations.
- Settings and Logs are minimally functional stubs, not broken links.

---

### 4.7 Error Handling, Toasts, and Basic UX Glue

**Objective:** Provide a minimal but coherent error/notification experience across the app. :contentReference[oaicite:40]{index=40}  

**Steps:**

1. **Global error boundary**

   - Wrap routes with a React error boundary that:
     - Logs errors to console (Phase 5).
     - Shows a simple “Something went wrong” screen with a reset button.

2. **Toast/notification system**

   - Implement a small toast component and context.
   - Use it to surface:
     - Network errors (failed fetches).
     - Backend Problem Details (operation-level failures).
     - Backend offline conditions.

3. **Inline form errors**

   - For each tool form, map validation errors from backend (`ValidationError` Problem Details) to inline field messages where possible; otherwise show them as a summary block at the top of the form.

**Acceptance criteria:**

- Failed operations show understandable error messages.
- Network/backend failures don’t hard-crash the app.

---

### 4.8 Testing Strategy for Phase 5

**Objective:** Add basic safety nets for the new UI + wiring.

**Steps:**

1. **API client tests**

   - Unit tests for:
     - Mapping Problem Details → `AppError`.
     - Progress subscription handling (mock EventSource/WebSocket and ensure events propagate to callers).

2. **Routing and shell tests**

   - Simple tests to:
     - Ensure routes render the correct page component.
     - Ensure sidebar navigation updates the route.

3. **Manual smoke tests**

   - For each tool:
     - Configure minimal valid inputs pointing at a test workspace.
     - Run an operation from the UI.
     - Confirm:
       - Backend receives the operation.
       - Progress appears in the UI.
       - Results summary is shown.

**Acceptance criteria:**

- Basic tests pass.
- Manual smoke tests complete successfully for all four tool flows.

---

## 5. Definition of Done (Phase 5)

Phase 5 is complete when **all** of the following are true:

1. **Backend IPC**
   - Kestrel HTTP/JSON host is running and exposes:
     - `/health`, `/info`, `/operations/*`, and per-tool endpoints as described. :contentReference[oaicite:41]{index=41}  
   - Operations can be started, monitored, and queried via HTTP.

2. **Frontend Shell & Routing**
   - WebView2 host starts the backend and passes configuration to the frontend.
   - Frontend shell (sidebar + header + main area) renders correctly.
   - All main routes (Dashboard, IoStore, UAsset, DLL, UWP, Settings, Logs) are reachable. :contentReference[oaicite:42]{index=42}  

3. **Minimal Tool Flows**
   - Each tool page (Retoc, UAsset, DLL, UWP):
     - Accepts minimal required inputs.
     - Starts a backend operation.
     - Shows progress and a final summary.

4. **Workspace & Status**
   - Global workspace selector exists and the current workspace is visible.
   - Backend readiness/status is visible in the UI and reflects `/health`.

5. **Error Handling**
   - Problem Details are mapped to user-visible error messages.
   - Backend/network failures don’t crash the app; errors are surfaced via toasts/banners.

6. **Testing**
   - API client and routing tests exist and pass.
   - Manual smoke tests for each end-to-end operation are documented and passing.

7. **Code Quality**
   - Frontend and backend code follows the project’s human-style rules:
     - No AI/meta comments.
     - No gratuitous abstractions or over-engineering.
     - Comments only where they clarify non-obvious behavior.

With Phase 5 complete, ARIS has a **functional, minimal UI** that exercises all major backend capabilities end-to-end, ready for Phase 6’s deeper UX, validation, and visual refinement work.
