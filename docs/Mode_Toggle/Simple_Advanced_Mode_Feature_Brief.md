## Feature Brief

### Goal

Add an explicit **Simple | Advanced** mode toggle to the **Retoc tool pane**.

* **Simple Mode:** two guided workflows only:

  * `to-legacy` (Unpack: Zen → Legacy)
  * `to-zen` (Pack: Legacy → Zen)
* **Advanced Mode:** a structured **full Retoc command builder** exposing **all supported Retoc commands/options**.
* Both modes must:

  * show an **exact command preview** (the same invocation the backend will run)
  * execute Retoc via the **existing ARIS backend patterns**
  * **stream live stdout/stderr** to a console log UI

Key constraint: current Retoc flow shown in the SDD is request/response with captured output (non-streaming), so streaming will require a small, repo-consistent extension to the Hosting/Infrastructure execution flow. 

---

## Impact Analysis (Repo Areas)

### Backend

* **Hosting**

  * Add a **streaming execution endpoint** for Retoc (Minimal API).
  * Add a **command preview/build endpoint** so the UI can display the exact invocation (and avoid frontend drift).
  * Optionally add a **help endpoint** that returns Markdown-wrapped Retoc help text.
* **Adapters**

  * Extend Retoc adapter surface to support **command-type-based execution** (e.g., `ToLegacy`, `ToZen`, and all other `RetocCommandType` values).
  * Reuse existing `RetocCommandBuilder` for validation/quoting/allowlisting.
* **Infrastructure**

  * Add a streaming-capable process runner path (or a thin streaming wrapper alongside existing `ProcessRunner`), while preserving the existing buffered execution path.
* **Core**

  * Likely minimal/no changes unless the current `RetocCommand` cannot represent all needed command-type-specific fields.

### Frontend

* Retoc page/pane: add **mode toggle** and conditional rendering:

  * Simple Mode: two fixed forms + preview + per-form execute
  * Advanced Mode: command builder UI + help modal + preview + execute
* Add **Retoc API client** functions:

  * build/preview
  * stream execute (SSE/EventSource or fetch streaming)
  * help fetch
* Update shared console log component (if exists) or implement a Retoc-specific log panel consistent with other tools’ logging UX.

### Contracts

Add/extend DTOs in:

* `src/Aris.Contracts/Retoc/*`
* `frontend/src/types/contracts.ts`

### Tests

* Adapter + builder mapping tests (xUnit) using `FakeProcessRunner` patterns.
* Endpoint-level tests if the repo already has minimal API tests (otherwise keep to adapter/builder + targeted infrastructure tests for streaming line parsing).

---

## Design Decisions

### 1) Single source of truth for “exact command preview”

To guarantee the preview matches execution:

* **Backend builds the command** (via `RetocCommandBuilder`) and returns:

  * `executablePath`
  * `arguments`
  * a human-readable `commandLine` string (e.g., `"<path-to-retoc.exe>" to-legacy UE5.3 "<input>" "<output>" ...`)

Frontend displays `commandLine` verbatim.

### 2) Streaming strategy (repo-consistent, minimal)

Implement **Server-Sent Events (SSE)** for streaming stdout/stderr lines:

* `POST /api/retoc/stream` responds as `text/event-stream`
* Backend spawns process and writes events:

  * `event: line` with JSON payload `{ stream: "stdout"|"stderr", text, timestamp }`
  * `event: status` for `started|completed|failed` plus exit code
* SSE fits Minimal APIs well and avoids introducing heavier frameworks.

Keep the existing buffered execution endpoint intact (if needed elsewhere); Simple/Advanced mode use streaming by default.

### 3) Advanced “100% Retoc support” without duplicating CLI semantics

Expose a **capabilities/schema endpoint** that the frontend uses to render structured controls:

* commands list (from `RetocCommandType`)
* per-command required/optional fields
* argument types (path, enum dropdown, int, string, multi-value)
* constraints (mutual exclusivity, allowed ranges)

This schema should live in backend (near adapter/builder) so it stays aligned with what the adapter can actually run.

### 4) Help output as Markdown-rendered content

Provide:

* `GET /api/retoc/help` → returns Markdown string (backend wraps the raw help output in a Markdown code fence, or maps known sections if already available).

---

## Implementation Plan (Step-by-step, file-level intent)

### 0) Inventory (first coding step)

Confirm existing frontend Retoc page and any console/log components, plus current Retoc API client presence, and whether anything already streams output. (The SDD documents buffered capture only.) 

---

### 1) Contracts (C# + TS)

**Create/modify (C# DTOs):**

* `src/Aris.Contracts/Retoc/RetocBuildCommandRequest.cs` (new)

  * `commandType` (string/enum)
  * `engineVersion`
  * `inputPaths` / `inputPath` (depending on command)
  * `outputPath`
  * `options` (structured options object; avoid “additionalArgs” free-form beyond existing allowlist)
  * `timeoutSeconds?`
* `src/Aris.Contracts/Retoc/RetocBuildCommandResponse.cs` (new)

  * `commandLine`
  * `executablePath`
  * `arguments`
* `src/Aris.Contracts/Retoc/RetocStreamStartRequest.cs` (new; can reuse build request shape)
* `src/Aris.Contracts/Retoc/RetocCommandSchemaResponse.cs` (new)

  * `commands: RetocCommandDefinition[]` etc.
* `src/Aris.Contracts/Retoc/RetocHelpResponse.cs` (new)

  * `markdown`

**Update TS:**

* `frontend/src/types/contracts.ts` (modify)

  * add the above request/response types.

Acceptance criteria:

* Frontend can render Simple/Advanced entirely from these contracts.
* Preview always sourced from `RetocBuildCommandResponse.commandLine`.

---

### 2) Core + Adapters

**Likely modify:**

* `src/Aris.Adapters/Retoc/IRetocAdapter.cs`

  * add methods:

    * `BuildCommand(...)` (pure build/validate, no execution)
    * `ExecuteStreamingAsync(...)` (or keep streaming in Hosting/Infrastructure and reuse adapter for build + tool path)
* `src/Aris.Adapters/Retoc/RetocAdapter.cs`

  * add build method that calls `RetocCommandBuilder.Build(...)` and returns `commandLine/exe/args`
  * add mapping from “command builder DTO” → `RetocCommand` with `RetocCommandType` and relevant fields
* `src/Aris.Adapters/Retoc/RetocCommandBuilder.cs`

  * ensure it supports `ToLegacy` / `ToZen` and other commands already represented by `RetocCommandType` (SDD indicates enum already includes `ToLegacy` and `ToZen`). 
  * add any missing validations needed for folder-based inputs vs file-based inputs (as required by your Simple mode paths).

**Optional (schema):**

* add a small “schema provider” class in `src/Aris.Adapters/Retoc/` or `src/Aris.Core/Retoc/` (if you want it dependency-free) describing supported commands/args that the frontend uses to render controls.

Acceptance criteria:

* Building a command for Simple Mode produces correct `to-legacy`/`to-zen` argument ordering and quoting.
* The same build function is used by streaming execution to prevent drift.

---

### 3) Infrastructure: streaming process execution

**Add new streaming runner (preferred minimal change):**

* `src/Aris.Infrastructure/Process/StreamingProcessRunner.cs` (new)

  * starts process with redirected stdout/stderr
  * reads output line-by-line asynchronously
  * exposes callbacks/events (e.g., `OnStdOutLine`, `OnStdErrLine`)
  * enforces timeout and kill-on-timeout consistent with existing `ProcessRunner` patterns
  * produces a final `ProcessResult` summary for completion status

Keep existing `ProcessRunner` unchanged for other endpoints.

Acceptance criteria:

* Output lines are delivered in near real time.
* Timeout behavior matches existing policy.
* No unbounded memory growth (do not buffer entire streams).

---

### 4) Hosting: endpoints

**Modify/create:**

* `src/Aris.Hosting/Endpoints/RetocEndpoints.cs` (modify)

  * Add:

    * `POST /api/retoc/build` → returns `RetocBuildCommandResponse`
    * `GET /api/retoc/schema` → returns `RetocCommandSchemaResponse`
    * `GET /api/retoc/help` → returns `RetocHelpResponse`
    * `POST /api/retoc/stream` → SSE streaming endpoint
  * Map errors using existing `ArisException → ErrorInfo + status code` strategy. 

**Streaming endpoint behavior:**

* Validate request
* Call adapter build to obtain `exePath/args/commandLine`
* Start streaming runner
* Emit:

  * `status: started`
  * `line` events for stdout/stderr
  * `status: completed` (include exit code + duration)
  * On exceptions: `status: failed` + an error event payload consistent with `ErrorInfo`

Acceptance criteria:

* Frontend can connect once per execution and receive all output and completion state without polling.

---

### 5) Frontend: Retoc pane UI

**Modify/add likely files:**

* `frontend/src/pages/tools/RetocPage.tsx` (or equivalent) (modify)

  * add mode toggle state (persist to local storage optional)
  * Simple Mode UI:

    * Unpack section (to-legacy): UE version dropdown, input/output folder pickers, preview, Extract button
    * Pack section (to-zen): same pattern, Pack button
  * Advanced Mode UI:

    * help button + modal rendering Markdown
    * command selector dropdown
    * dynamic fields based on `/api/retoc/schema`
    * preview + Execute button
  * shared console log panel (same in both modes)
* `frontend/src/api/retocClient.ts` (new or modify existing)

  * `getRetocSchema()`
  * `getRetocHelp()`
  * `buildRetocCommand(req)`
  * `streamRetoc(req, onEvent)` using `EventSource` (SSE)
* If folder pickers use existing patterns (likely via browser + host integration), reuse the same component(s) used elsewhere.

Acceptance criteria:

* Preview updates on every relevant input change (debounced optional).
* Execute triggers stream; UI shows running state; buttons disabled while running; completion/error indicated.
* Console log supports long-running output and error output.

---

## Tests

### Backend tests (xUnit)

Add/extend:

* `tests/Aris.Core.Tests/Adapters/RetocAdapterTests.cs` (modify)

  * verify `ToLegacy` build produces expected ordered args and quotes
  * verify `ToZen` build produces expected ordered args and quotes
  * verify invalid paths (non-absolute) produce `ValidationError` (per SDD). 
* `tests/Aris.Core.Tests/Retoc/RetocCommandBuilderTests.cs` (new if not present)

  * per-command argument validation and allowlist checks
* `tests/Aris.Core.Tests/Infrastructure/StreamingProcessRunnerTests.cs` (new)

  * simulate process output (or use a tiny test executable/script if repo already uses such fixtures)
  * verify stdout/stderr events delivered and timeout enforced

### Frontend tests

Only if a test framework is already present; otherwise rely on manual verification + TypeScript compile.

---

## Verification Plan

### Build + Test

```bash
dotnet build
dotnet test
cd frontend
npm install
npm run build
```

(Use the repo’s standard commands; SDD documents these as the baseline.) 

### Manual behavior checks

1. **Simple Mode → Extract**

   * select UE version, choose folders
   * preview matches backend `commandLine`
   * click Extract → live output appears; completion state updates
2. **Simple Mode → Pack**

   * same expectations
3. **Advanced Mode**

   * schema loads; selecting different command changes fields
   * invalid combinations are prevented where schema defines constraints
   * help modal renders Markdown
4. **Error handling**

   * invalid path → 400 with `ErrorInfo` shown in UI
   * missing dependency → 503 surfaced correctly
   * retoc failure exit code → completion state indicates failed and includes stderr lines

---