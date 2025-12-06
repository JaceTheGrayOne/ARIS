# ARIS Frontend Software Design Document
Last updated: 2025-12-04  
Audience: Frontend engineers, UX designers, integrators

## 1. Purpose and Scope
- Define a clean, modern frontend for ARIS without trying to replicate the existing UI, built as a single cohesive stack instead of the previous hybrid (vanilla HTML/JS/CSS + React/TypeScript/Tailwind) structure.
- Describe user-facing functions, layout, navigation, and backend wiring at a high level so the UI can be rebuilt cleanly.
- Avoid prescribing specific framework markup/stylesheet implementation details; focus on structure, behavior, and integration.

## 2. Target Experience
- Windows desktop UI presented in a single application window with a dark, high-contrast aesthetic, sidebar navigation, panelized content areas, card-like operation sections, accent highlights, and compact controls.
- Fast startup, responsive interactions, and consistent status/progress feedback for long-running operations.
- Clear separation between global status (extraction, readiness), workspace context, and tool-specific workflows.

## 3. Architecture (Frontend Perspective)
- **Host**: Desktop shell (e.g., WebView2 or native web runtime) loading the frontend bundle.
- **Integration with Backend**:
  - Communicate via local HTTP/JSON or message bridge exposed by the backend host.
  - Use typed request/response DTOs shared via generated client models; errors follow Problem Details shape.
  - Subscribe to progress streams (SSE/WebSocket) for long operations; map operation ids to UI tasks.
- **State**:
  - Global app state: session info, backend readiness, workspace selection, theme, user settings.
  - Feature state per tool: form values, validation errors, recent runs, progress, results.
  - Cache of recent workspaces and outputs for quick access; persisted in local storage/user settings file.
- **Routing/Layout**:
  - Top-level sections: Dashboard, IoStore/Retoc, UAsset Serialization, DLL Injector, UWP Dumper, Settings, Logs.
  - Each section is a routed view; navigation via persistent sidebar with icons + labels.

## 4. Visual Structure and Layout
- **Shell**: Left sidebar (navigation + status), right main content area. Header bar with active workspace indicator, backend status (ready/extracting/error), and quick actions (open workspace, refresh).
- **Panels/Cards**: Each tool page uses stacked cards: Configuration, Inputs, Actions, Progress/Logs, Results. Compact spacing, dark backgrounds, subtle borders/shadows, accent color for primary actions.
- **Forms**: Grouped controls with labels and helper text; inline validation messages; dropdowns for UE version/mode, file pickers, toggles for advanced options.
- **Progress and Logs**: Collapsible live log viewer and progress timeline per operation; status chips (running/success/failure).
- **Tables/Lists**: Recent operations/results with sortable columns (timestamp, workspace, status, output).
- **Dialogs**: File picker overlays for workspace selection and payload/asset selection; confirmation prompts for elevated actions (DLL injection, UWPDumper).
- **Responsive**: Adapts to typical desktop widths; sidebar collapsible to icons-only; panels wrap vertically on narrower widths.

## 5. User Functions by Section
- **Dashboard**:
  - Shows backend readiness, dependency extraction status, recent operations across tools, quick links to workspaces.
  - Global actions: open workspace, view logs, run self-check.
- **IoStore / Retoc**:
  - Inputs: mode (Pak->IoStore, IoStore->Pak, Repack, Validate), source files, output destination, UE version, AES keys, include/exclude filters, compression options.
  - Actions: Validate inputs, Run conversion.
  - Output: Operation progress, log tail, produced files summary, warnings.
- **UAsset Serialization**:
  - Inputs: asset or JSON path, target output path, UE version, schema version, include bulk data toggle.
  - Actions: Deserialize to JSON, Serialize to asset, Inspect.
  - Output: Progress, log tail, produced files, inspection summary.
- **DLL Injector**:
  - Inputs: target process (PID/name picker), payload DLL selection, injection method, optional arguments, elevation requirement toggle.
  - Actions: Inject, Eject, Refresh process list.
  - Output: Progress, verification status, logs, warnings.
- **UWP Dumper**:
  - Inputs: package family name/AppId, mode (full dump/metadata), output folder, include symbols toggle, elevation requirement (usually on).
  - Actions: Validate target, Run dump.
  - Output: Progress, log tail, artifact summary.
- **Settings**:
  - Theme (keep existing dark styling baseline), language (if applicable), defaults per tool (timeouts, staging retention), logging verbosity, backend endpoint selection (if configurable), toggle for retaining temp on failure.
- **Logs**:
  - Centralized view of recent operation logs; filter by tool/status/time; view details per operation id.

## 6. Backend Wiring
- **API Client**: Thin client wrapping backend HTTP/JSON endpoints:
  - Operations endpoints per tool (`/retoc`, `/uasset`, `/dll`, `/uwp`), health (`/health`), info (`/info`), logs (`/logs/{id}`).
  - Uses shared DTOs for commands and results; maps Problem Details to UI-friendly errors.
- **Progress Streaming**: Subscribe to `/operations/{id}/events` via SSE/WebSocket; update progress timeline and log view in real time.
- **Authentication/Trust**: Local-only; include per-session token from backend boot info; handle 401/403 by prompting to restart session.
- **Error Handling**: Display contextual inline errors on forms; show toast/banner for operation-level failures with remediation hints.

## 7. Data and Interaction Flows (Conceptual)
- User selects workspace → App updates global state → tool pages use workspace context for defaults and output paths.
- User configures tool form → client validates locally (paths, required fields, patterns) → on submit, POST command to backend → receive operation id.
- Client subscribes to progress stream → updates progress/log panels → on completion, fetches result summary → shows produced files with links to open folder.
- Logs page pulls recent operation metadata from backend and displays summaries; allows opening full log per operation id.

## 8. UX Guidelines (Cohesive Dark UI, Simplify Implementation)
- Maintain dark, high-contrast palette with a single accent color for primary buttons and highlights.
- Use consistent panel shapes, spacing, and typography scale to create a clear, readable layout, without tying the design to the legacy styling code.
- Keep controls compact and information-dense while maintaining consistent vertical rhythm and alignment.
- Use icons in the sidebar and in status chips to clearly communicate status; avoid mixing multiple component libraries.
- Provide clear status indicators for elevation-required actions (DLL injection, UWP dump) with confirmation prompts.

## 9. Accessibility and Internationalization
- Keyboard navigation for all primary actions; focus outlines visible.
- High-contrast defaults; support adjustable font size scaling.
- Text labels for icons; tooltips for abbreviations.
- Prepare copy for localization even if shipped initially in English.

## 10. Settings and Persistence
- Persist user preferences (theme, default paths, last workspace, verbosity) to user-scoped settings file; load at startup.
- Remember recent operations and workspaces for quick re-entry; cap list length for performance.
- Respect backend-provided defaults for timeouts and staging retention; allow user overrides where safe.

## 11. Telemetry and Diagnostics (Frontend)
- Capture client-side errors and surface them in a diagnostics pane; include correlation id/operation id when available.
- Allow user to export a support bundle: recent logs + client diagnostics (no sensitive data).

## 12. Testing Strategy (Frontend)
- Unit tests for form validation, DTO shaping, and error mapping.
- Integration tests for API client against mocked backend responses and progress streams.
- Visual regression (screenshot) tests to catch unintended UI regressions as the stack evolves.
- Manual smoke flows per tool: configure, run, observe progress, view results, open logs.

## 13. Implementation Notes (What Not to Recreate)
- Do not split between vanilla HTML/JS and React/TypeScript/Tailwind; rebuild as a single cohesive stack of your choice.
- Avoid legacy build artifacts (mixed bundlers, duplicate stylesheets). Use one build pipeline and one component system.
- Do not attempt to reproduce the legacy visual layout or DOM/CSS structure; treat this as a fresh UI with its own layout and styling.

---
This SDD defines the desired frontend structure, behaviors, and user flows for the new ARIS UI, without recreating the previous hybrid implementation.***
