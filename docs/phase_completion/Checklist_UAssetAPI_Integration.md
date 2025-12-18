# Checklist: UAssetAPI Real Integration

Project: ARIS
Started: 2025-12-16
Completed: __________

---

## Phase 1 — Dependency Setup
- [x] UAssetAPI added as git submodule under `external/UAssetAPI`
- [x] Required UAssetAPI projects referenced by `Aris.Adapters`
- [x] `dotnet build` succeeds with no runtime behavior change

---

## Phase 2 — Inspect Endpoint
- [x] `UAssetApiBackend` created
- [x] Inspect loads real `.uasset` via UAssetAPI
- [x] Inspect DTO populated with real metadata
- [x] No generic JSON serialization of `UAsset`
- [x] Inspect unit/integration test added
- [x] Inspect endpoint verified manually

### Phase 2 Verification
- [x] Contract compatibility verified between DTOs, backend, and frontend
- [x] Field name mismatch fixed (inputPath → assetPath)
- [x] Missing fields added to TypeScript (operationId, logExcerpt)
- [x] Frontend component updated to use correct field name
- [x] Error mapping consistency verified with other adapters
- [x] Tool name convention fixed (UAssetAPI → uassetapi)
- [x] UEVersion option handling documented (N/A for Inspect, auto-detect only)
- [x] Compatibility report created at `docs/dev/UAsset_Inspect_Compatibility_Report.md`
- [x] All tests pass after verification fixes

---

## Phase 3 — Deserialize Endpoint
- [x] Deserialize produces real output files (via UAssetApiBackend.DeserializeAsync)
- [x] Sidecar files (.uexp/.ubulk) handled correctly (auto-loaded by UAssetAPI)
- [x] Output paths returned via existing contracts
- [x] Deserialize tests added (4 core tests in UAssetApiBackendTests.cs)
- [x] TimeoutSeconds cancellation support implemented with CancellationTokenSource
- [x] IncludeBulkData limitation documented with code comment and runtime warning

---

## Phase 4 — Serialize Endpoint
- [x] Serialize reconstructs valid `.uasset`
- [x] Sidecar rules preserved
- [x] Round-trip test added (deserialize → serialize → inspect)
- [x] Serialize endpoint verified manually

---

## Phase 5 — DI Switch & Cleanup
- [ ] `StubUAssetBackend` removed from production DI
- [ ] No stub logic reachable at runtime
- [ ] Logging and error handling consistent with other adapters

---

## Phase 6 — Documentation & Verification
- [ ] `docs/dev/UAssetAPI_Integration_Notes.md` written
- [ ] Options mapping documented (supported vs ignored)
- [ ] `dotnet test` passes
- [ ] `dotnet run --project src/Aris.Hosting` verified
- [ ] Manual API calls confirmed working

---

## Final Review
- [ ] Checklist fully reviewed and marked
- [ ] Summary written describing:
  - Files changed
  - Commands run
  - How correctness was verified
