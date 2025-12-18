# Plan: Real UAssetAPI Integration for ARIS

## Purpose
Replace the current `StubUAssetBackend` with a real UAssetAPI-backed implementation that fully supports the existing ARIS UAsset endpoints:

- POST /api/uasset/inspect
- POST /api/uasset/deserialize
- POST /api/uasset/serialize

This work must preserve all existing API contracts and frontend compatibility.

---

## Current State

### Backend
- `IUAssetBackend` abstraction exists.
- `StubUAssetBackend` returns placeholder results and dummy files.
- Endpoints are already wired and used by the frontend.

### Frontend
- `UAssetPage.tsx` and `uassetClient.ts` are complete.
- No frontend changes are expected unless a contract bug is discovered.

---

## Dependency Strategy

### Selected approach: **Git submodule (vendored source)**

**Reasoning**
- UAssetAPI NuGet package is unlisted and targets .NET Framework 4.7.2.
- ARIS targets modern .NET; source inclusion avoids runtime incompatibilities.
- UAssetAPI is actively maintained and suitable for direct reference.

### Actions
- Add UAssetAPI as a git submodule under:
  `external/UAssetAPI`
- Reference required UAssetAPI projects from `Aris.Adapters`.

---

## Architectural Constraints

- Keep `IUAssetBackend` as the integration boundary.
- Introduce `UAssetApiBackend : IUAssetBackend`.
- Do not change request/response DTOs unless a bug is discovered.
- Keep `Aris.Hosting` thin (DI + endpoints only).
- All file I/O must be explicit, deterministic, and documented.

---

## Endpoint Behavior Mapping

### Inspect
- Input: `.uasset` path (with sidecar detection).
- Behavior:
  - Load asset using UAssetAPI.
  - Extract metadata (engine version, name table size, export count, imports, etc.).
- Output:
  - Populate existing inspect DTO with real data.
  - No JSON dumping of the raw UAsset object.

### Deserialize
- Input: `.uasset` path.
- Behavior:
  - Use UAssetAPI-supported export/inspection mechanisms.
  - Produce a structured representation suitable for ARIS usage.
- Output:
  - Real output file(s) written to the requested output directory.
  - Paths returned via existing response contracts.

### Serialize
- Input: structured representation produced by Deserialize.
- Behavior:
  - Reconstruct asset using UAssetAPI-supported import/write APIs.
  - Respect sidecar rules (.uexp / .ubulk).
- Output:
  - Valid `.uasset` (+ sidecars if applicable).

⚠️ **Explicit rule**:  
Generic `JsonConvert.SerializeObject(UAsset)` / `DeserializeObject` MUST NOT be used.  
Only UAssetAPI-supported workflows or explicit mapping logic are allowed.

---

## Options Mapping Rules

| ARIS Field | Behavior |
|-----------|---------|
| UEVersion | If provided, force version; else auto-detect |
| Game | Map to UAssetAPI game enums if supported; else ignore with doc note |
| SchemaVersion | Documented no-op if unsupported |
| IncludeBulkData | Controls sidecar handling |
| Compression | Documented if unsupported |
| Timeout | Best-effort cancellation via CTS |

All ignored or partially supported options must be documented.

---

## Phases

### Phase 1 — Dependency Setup
- Add UAssetAPI submodule.
- Wire project references.
- Build passes with no runtime changes.

### Phase 2 — Inspect Implementation (FIRST)
- Implement inspect-only logic.
- Replace stub inspect path.
- Add tests validating real metadata extraction.

### Phase 3 — Deserialize Implementation
- Implement real deserialize behavior.
- Ensure sidecar handling.
- Add tests validating output files.

### Phase 4 — Serialize Implementation
- Implement serialize logic.
- Ensure round-trip correctness where possible.
- Add round-trip test (deserialize → serialize → inspect).

### Phase 5 — DI Switch + Cleanup
- Replace `StubUAssetBackend` in DI.
- Keep stub only if explicitly needed for testing.
- Remove dead code.

### Phase 6 — Documentation & Verification
- Write `docs/dev/UAssetAPI_Integration_Notes.md`.
- Manually verify all endpoints.

---

## Risks & Mitigations
- **UAssetAPI API surface mismatch** → Inspect-first phase to validate assumptions early.
- **Test asset licensing** → Prefer UAssetAPI’s own test assets or synthetic minimal assets.
- **Unsupported ARIS options** → Explicitly document behavior.

---

## Definition of Done
- All three endpoints produce real outputs.
- Tests pass (`dotnet test`).
- Manual endpoint verification performed.
- Checklist fully marked with justification.
