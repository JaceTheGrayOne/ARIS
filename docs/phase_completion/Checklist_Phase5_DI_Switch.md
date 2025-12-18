# Checklist: Phase 5 - DI Switch and Cleanup

Project: ARIS
Phase: 5 (DI Switch and Cleanup)
Started: __________
Completed: __________

---

## Overview

This checklist tracks completion of Phase 5, which switches the production DI configuration from `StubUAssetBackend` to `UAssetApiBackend`.

See companion document: `docs/dev/Plan_Phase5_DI_Switch.md`

---

## Pre-Implementation Verification

- [ ] Phase 2 (Inspect) marked complete in `Checklist_UAssetAPI_Integration.md`
- [ ] Phase 3 (Deserialize) marked complete in `Checklist_UAssetAPI_Integration.md`
- [ ] Phase 4 (Serialize) marked complete in `Checklist_UAssetAPI_Integration.md`
- [ ] Current build is clean (`dotnet build` succeeds)
- [ ] Current tests pass (`dotnet test` succeeds)

**Note:** Do not proceed with Phase 5 if any Phase 2-4 items are incomplete.

---

## Step 1: Update DI Registration

- [ ] `src/Aris.Adapters/DependencyInjection.cs` modified
- [ ] Line 15 changed from `services.AddSingleton<IUAssetBackend, StubUAssetBackend>();`
- [ ] Line 15 now reads `services.AddSingleton<IUAssetBackend, UAssetApiBackend>();`
- [ ] No other lines in the file changed

---

## Step 2: Update StubUAssetBackend Documentation

- [ ] `src/Aris.Adapters/UAsset/StubUAssetBackend.cs` modified
- [ ] XML documentation comment (lines 6-9) updated
- [ ] Comment no longer says "will be replaced"
- [ ] Comment clearly states stub is for testing purposes only
- [ ] No functional code changed (only XML comment)

---

## Step 3: Verify Build Success

- [ ] `dotnet build` command executed
- [ ] Build completed with zero errors
- [ ] Build completed with zero warnings
- [ ] No missing dependencies reported

**Command:**
```bash
dotnet build
```

**Expected Output:** `Build succeeded. 0 Warning(s), 0 Error(s)`

---

## Step 4: Run All Tests

- [ ] `dotnet test` command executed
- [ ] All tests passed (100% pass rate)
- [ ] No flaky or intermittent failures observed
- [ ] Test output reviewed for unexpected warnings

**Command:**
```bash
dotnet test
```

**Expected Output:** All tests green, zero failures.

**If tests fail:** Investigate root cause. Do NOT mark this item complete until all tests pass.

---

## Step 5: Manual Endpoint Verification

### 5.1 Test Environment Setup
- [ ] Aris.Hosting started successfully (`dotnet run --project src/Aris.Hosting`)
- [ ] Test .uasset file(s) available for verification
- [ ] Test assets location documented: ____________________________

### 5.2 Inspect Endpoint
- [ ] Inspect endpoint tested with valid .uasset file
- [ ] Response contains real metadata (not stub placeholders)
- [ ] `summary.exportCount` is a real value (not hardcoded 50)
- [ ] `summary.importCount` is a real value (not hardcoded 75)
- [ ] `summary.nameCount` is a real value (not hardcoded 100)
- [ ] No errors or exceptions in logs

**Example Request:**
```json
{
  "assetPath": "tests/Aris.Core.Tests/TestAssets/sample.uasset",
  "fields": ["exports", "imports", "names"]
}
```

**Verification:** Response data matches actual asset contents, not stub values.

### 5.3 Deserialize Endpoint
- [ ] Deserialize endpoint tested with valid .uasset file
- [ ] Output JSON file created at requested path
- [ ] Output JSON contains real UAsset structure (not `{"stub": "json content"}`)
- [ ] Output JSON is valid (can be parsed by JSON validator)
- [ ] File size is reasonable (not trivially small like stub output)
- [ ] No errors or exceptions in logs

**Example Request:**
```json
{
  "inputAssetPath": "tests/Aris.Core.Tests/TestAssets/sample.uasset",
  "outputJsonPath": "temp/deserialize-output.json"
}
```

**Verification:** Output JSON file contains real asset data with detailed structure.

### 5.4 Serialize Endpoint
- [ ] Serialize endpoint tested with JSON from deserialize step
- [ ] Output .uasset file created at requested path
- [ ] Output .uasset is valid binary (not text "Stub .uasset content")
- [ ] Sidecar files (.uexp, .ubulk) created if applicable
- [ ] File size matches expectations (not trivially small)
- [ ] No errors or exceptions in logs

**Example Request:**
```json
{
  "inputJsonPath": "temp/deserialize-output.json",
  "outputAssetPath": "temp/serialize-output.uasset"
}
```

**Verification:** Output .uasset is valid binary file, not stub text.

### 5.5 Round-Trip Verification
- [ ] Round-trip test performed (deserialize → serialize → inspect)
- [ ] Original asset inspected and metadata recorded
- [ ] Asset deserialized to JSON
- [ ] JSON serialized back to .uasset
- [ ] Reconstructed asset inspected and metadata recorded
- [ ] Key metadata matches (exportCount, importCount, nameCount within tolerance)

**Tolerance:** Exact match expected for counts. Minor version differences acceptable if documented.

**Original Metadata:**
- Export Count: ________
- Import Count: ________
- Name Count: ________
- UE Version: ________

**Reconstructed Metadata:**
- Export Count: ________ (matches: ☐ Yes ☐ No)
- Import Count: ________ (matches: ☐ Yes ☐ No)
- Name Count: ________ (matches: ☐ Yes ☐ No)
- UE Version: ________ (matches: ☐ Yes ☐ No)

---

## Step 6: Verify Logging Consistency

- [ ] Logs from Step 5 reviewed
- [ ] Tool name is consistently `"uassetapi"` (lowercase)
- [ ] Log messages follow adapter patterns (similar to `RetocAdapter`)
- [ ] Operation correlation IDs logged where applicable
- [ ] No raw exception stack traces in normal operation
- [ ] Debug-level logs contain useful diagnostic info

**Comparison Reference:** Review `src/Aris.Adapters/Retoc/RetocAdapter.cs` for logging patterns.

**Sample Log Inspection:**
- [ ] "Inspecting asset with UAssetAPI: {Path}" message seen
- [ ] "Asset inspection complete: {ExportCount} exports, {ImportCount} imports, {NameCount} names" message seen
- [ ] Similar patterns for deserialize and serialize operations

---

## Step 7: Verify Error Handling Consistency

### 7.1 Missing Input File Test
- [ ] Tested inspect/deserialize with non-existent file path
- [ ] Received appropriate error response (ValidationError or ToolExecutionError)
- [ ] Error message is user-friendly and descriptive
- [ ] Error message does not expose internal paths or sensitive info

**Example Error Message Pattern:** `"Failed to inspect asset 'filename.uasset': [reason]"`

### 7.2 Invalid File Format Test
- [ ] Tested with corrupted or non-UAsset file
- [ ] Received ToolExecutionError with descriptive message
- [ ] Error logged appropriately (no crash)
- [ ] Error response structure matches other adapters

### 7.3 Cancellation Test (Optional)
- [ ] Tested operation cancellation (if feasible)
- [ ] Cancellation handled gracefully (no resource leaks)
- [ ] OperationCanceledException caught and handled

**Note:** Cancellation testing may be difficult for fast operations. Mark as N/A if not feasible.

---

## Code Review

- [ ] Only two files modified (DependencyInjection.cs and StubUAssetBackend.cs)
- [ ] No test files modified
- [ ] No unrelated changes included
- [ ] Git diff reviewed and looks correct

**Modified Files:**
1. `src/Aris.Adapters/DependencyInjection.cs` (1 line changed)
2. `src/Aris.Adapters/UAsset/StubUAssetBackend.cs` (XML comment updated)

**Unchanged Files (verify):**
- `tests/Aris.Core.Tests/Adapters/UAssetServiceTests.cs` (still uses stub directly)
- `src/Aris.Adapters/UAsset/UAssetApiBackend.cs` (no changes)
- `src/Aris.Adapters/UAsset/IUAssetBackend.cs` (no changes)

---

## Final Verification

- [ ] All checklist items above marked complete
- [ ] No known issues or blockers
- [ ] Phase 5 plan followed exactly
- [ ] Ready to proceed to Phase 6 (Documentation & Verification)

---

## Rollback Procedure (If Needed)

If critical issues discovered:

1. Revert `src/Aris.Adapters/DependencyInjection.cs` to register `StubUAssetBackend`
2. Run `dotnet build` and `dotnet test` to verify rollback
3. Document issue and investigation plan
4. Do NOT mark Phase 5 complete

---

## Completion Summary

**Phase 5 Completion Date:** __________

**Files Changed:**
- [ ] `src/Aris.Adapters/DependencyInjection.cs`
- [ ] `src/Aris.Adapters/UAsset/StubUAssetBackend.cs`

**Commands Run:**
- [ ] `dotnet build` (succeeded)
- [ ] `dotnet test` (all passed)
- [ ] Manual endpoint verification (all three endpoints tested)

**Verification Results:**
- Inspect endpoint: ☐ Real data ☐ Stub data (must be Real)
- Deserialize endpoint: ☐ Real JSON ☐ Stub JSON (must be Real)
- Serialize endpoint: ☐ Real binary ☐ Stub text (must be Real)
- Round-trip test: ☐ Passed ☐ Failed (must be Passed)

**Issues Encountered:**
(List any issues and how they were resolved, or mark N/A)

**Ready for Phase 6:** ☐ Yes ☐ No

---

## Sign-Off

**Engineer:** ____________________________
**Date:** __________
**Status:** ☐ Complete ☐ Incomplete ☐ Blocked

**Notes:**
