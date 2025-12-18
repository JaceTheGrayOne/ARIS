# Phase 5 Plan: DI Switch and Cleanup

## Purpose

Complete the UAssetAPI integration by switching the production dependency injection configuration from `StubUAssetBackend` to `UAssetApiBackend`, ensuring the real UAssetAPI implementation is used at runtime.

---

## Scope

**In scope:**
- Change production DI registration to use `UAssetApiBackend`
- Update `StubUAssetBackend` documentation to clarify test-only usage
- Verify logging and error handling consistency with other adapters
- Run all tests to validate the switch
- Perform manual endpoint verification

**Out of scope:**
- Deleting `StubUAssetBackend` (it remains for legitimate test usage)
- Modifying test files that use `StubUAssetBackend`
- Performance optimization or feature additions
- Frontend changes
- Documentation beyond code comments (Phase 6 will handle comprehensive docs)

---

## Current State Analysis

### Production DI Configuration
**File:** `src/Aris.Adapters/DependencyInjection.cs`
**Line 15:** `services.AddSingleton<IUAssetBackend, StubUAssetBackend>();`

Currently registers the stub implementation for production use.

### Test Usage
**File:** `tests/Aris.Core.Tests/Adapters/UAssetServiceTests.cs`
**Line 43:** `var backend = new StubUAssetBackend();`

Tests explicitly instantiate `StubUAssetBackend` to test the `UAssetService` orchestration layer independently of the real UAssetAPI. This is legitimate test design and should not be changed.

### Outdated Documentation
**File:** `src/Aris.Adapters/UAsset/StubUAssetBackend.cs`
**Line 8:** `/// This will be replaced with a real UAssetAPI backend when the library is integrated.`

This comment is now inaccurate. The real backend is integrated, and the stub remains for testing purposes only.

---

## Step-by-Step Plan

### Step 1: Update DI Registration
**File:** `src/Aris.Adapters/DependencyInjection.cs`

Change line 15 from:
```csharp
services.AddSingleton<IUAssetBackend, StubUAssetBackend>();
```

To:
```csharp
services.AddSingleton<IUAssetBackend, UAssetApiBackend>();
```

**Rationale:** This makes `UAssetApiBackend` the production implementation while keeping `StubUAssetBackend` available for test usage.

**Impact:** All runtime calls to `IUAssetBackend` will now use the real UAssetAPI library.

---

### Step 2: Update StubUAssetBackend Documentation
**File:** `src/Aris.Adapters/UAsset/StubUAssetBackend.cs`

Update the XML documentation comment (lines 6-9) to:
```csharp
/// <summary>
/// Stub implementation of IUAssetBackend for testing purposes.
/// Returns plausible data without calling real UAssetAPI.
/// Used by tests to validate UAssetService orchestration independently of UAssetAPI.
/// </summary>
```

**Rationale:** Clarifies the current role of the stub (testing only) and removes the outdated "will be replaced" language.

---

### Step 3: Verify Build Success
**Command:** `dotnet build`

**Expected Outcome:** Clean build with no errors or warnings.

**What This Validates:**
- DI registration is syntactically correct
- All dependencies resolve correctly

---

### Step 4: Run All Tests
**Command:** `dotnet test`

**Expected Outcome:** All existing tests pass.

**What This Validates:**
- Tests that use `StubUAssetBackend` directly still work (no accidental breakage)
- Integration tests that rely on DI will now use `UAssetApiBackend`
- No regressions in other adapters

**If Tests Fail:**
- Investigate whether failure is due to:
  - Real bug in `UAssetApiBackend`
  - Missing test assets
  - Environmental issue (missing files, permissions)
- Do NOT mark Phase 5 complete until all tests pass

---

### Step 5: Manual Endpoint Verification
**Prerequisites:**
- Aris.Hosting is running: `dotnet run --project src/Aris.Hosting`
- Test assets are available (valid .uasset files)

**Verification Steps:**

#### 5.1 Verify Inspect Endpoint
```bash
curl -X POST http://localhost:5000/api/uasset/inspect \
  -H "Content-Type: application/json" \
  -d '{"assetPath": "path/to/test.uasset", "fields": ["exports", "imports"]}'
```

**Expected:** Real metadata from UAssetAPI (not stub placeholders like "Export1", "Export2").

#### 5.2 Verify Deserialize Endpoint
```bash
curl -X POST http://localhost:5000/api/uasset/deserialize \
  -H "Content-Type: application/json" \
  -d '{"inputAssetPath": "path/to/test.uasset", "outputJsonPath": "output/test.json"}'
```

**Expected:** Valid JSON file produced containing real UAsset structure (not `{"stub": "json content"}`).

#### 5.3 Verify Serialize Endpoint
```bash
curl -X POST http://localhost:5000/api/uasset/serialize \
  -H "Content-Type: application/json" \
  -d '{"inputJsonPath": "output/test.json", "outputAssetPath": "output/test.uasset"}'
```

**Expected:** Valid .uasset file produced (not `"Stub .uasset content"`).

#### 5.4 Round-Trip Verification
Combine deserialize and serialize to verify data integrity:
1. Deserialize a known good .uasset to JSON
2. Serialize that JSON back to .uasset
3. Inspect both original and reconstructed assets
4. Compare key metadata (export count, import count, name count)

**Success Criteria:** Reconstructed asset has matching structure to original.

---

### Step 6: Verify Logging Consistency
**Action:** Review logs produced during Step 5 manual verification.

**Check:**
- Log messages use consistent tool name `"uassetapi"` (lowercase, matching other adapters)
- Error messages follow the pattern used by `RetocAdapter` and other adapters
- Operation correlation IDs are logged where applicable
- No stack traces or raw exceptions in normal operation

**Reference:** Compare with `RetocAdapter` logging patterns in `src/Aris.Adapters/Retoc/RetocAdapter.cs`.

---

### Step 7: Verify Error Handling Consistency
**Action:** Intentionally trigger error conditions and verify error responses.

**Test Cases:**
1. **Missing input file:** Request with non-existent file path
   - Expected: `ValidationError` or `FileNotFoundException` wrapped in `ToolExecutionError`
2. **Invalid UAsset format:** Request with corrupted or non-UAsset file
   - Expected: `ToolExecutionError` with descriptive message
3. **Cancellation:** Send request and immediately cancel
   - Expected: `OperationCanceledException` handled gracefully

**Verification:** Error types and messages match the patterns used by other adapters.

---

## Files Expected to Change

### Modified Files (2)
1. `src/Aris.Adapters/DependencyInjection.cs`
   - Line 15: Change from `StubUAssetBackend` to `UAssetApiBackend`

2. `src/Aris.Adapters/UAsset/StubUAssetBackend.cs`
   - Lines 6-9: Update XML documentation comment

### Unchanged Files (Important)
- `tests/Aris.Core.Tests/Adapters/UAssetServiceTests.cs` (still uses stub directly)
- `src/Aris.Adapters/UAsset/UAssetApiBackend.cs` (no changes needed)
- `src/Aris.Adapters/UAsset/IUAssetBackend.cs` (no changes needed)

---

## Risk Analysis

### Risk 1: Runtime Failures in UAssetApiBackend
**Likelihood:** Low (Phases 2-4 implemented and tested all operations)
**Impact:** High (production functionality broken)

**Mitigation:**
- All automated tests must pass before marking Phase 5 complete
- Manual verification of all three endpoints required
- If failures occur, investigate and fix before proceeding

**Rollback:** Revert DI change to `StubUAssetBackend` if critical issues found.

---

### Risk 2: Test Assets Missing or Invalid
**Likelihood:** Medium (tests may require specific .uasset files)
**Impact:** Medium (manual verification blocked)

**Mitigation:**
- Use test assets from `tests/Aris.Core.Tests/TestAssets/` if available
- If missing, source minimal valid .uasset files from UAssetAPI's test suite
- Document asset requirements in checklist

**Fallback:** Use UAssetAPI's own test assets from the submodule.

---

### Risk 3: Performance Degradation
**Likelihood:** Medium (real UAssetAPI operations are slower than stub)
**Impact:** Low (expected behavior, not a bug)

**Mitigation:**
- This is expected and acceptable
- Document performance characteristics in Phase 6 documentation
- No action required in Phase 5 unless performance is unacceptably slow (>5s for small assets)

**Note:** Performance optimization is out of scope for Phase 5.

---

### Risk 4: Inconsistent Error Messages
**Likelihood:** Low (UAssetApiBackend was implemented following adapter patterns)
**Impact:** Low (UX inconsistency)

**Mitigation:**
- Step 7 explicitly verifies error handling consistency
- Minor message adjustments acceptable if discovered

---

## Verification Strategy

### Automated Verification
**Command:** `dotnet test`

**What It Validates:**
- All existing unit and integration tests pass
- No regressions in other adapters
- Test code that uses `StubUAssetBackend` directly still works

**Success Criteria:** Zero test failures.

---

### Manual Verification
**Commands:** See Step 5 above (curl commands for each endpoint)

**What It Validates:**
- Real UAssetAPI operations produce valid outputs
- Endpoints return proper HTTP status codes
- Error handling works correctly
- Round-trip operations preserve asset integrity

**Success Criteria:**
- All three endpoints produce real (non-stub) outputs
- Round-trip test shows matching metadata
- Error cases handled gracefully

---

### Verification Checklist
Detailed checklist items are in the companion document `Checklist_Phase5_DI_Switch.md`.

Key verification points:
- [ ] Build succeeds (`dotnet build`)
- [ ] All tests pass (`dotnet test`)
- [ ] Inspect endpoint produces real metadata
- [ ] Deserialize endpoint produces valid JSON
- [ ] Serialize endpoint produces valid .uasset
- [ ] Round-trip test preserves asset integrity
- [ ] Logging follows adapter patterns
- [ ] Error handling is consistent

---

## Done Means

Phase 5 is complete when ALL of the following are true:

1. **DI Change Applied**
   - `DependencyInjection.cs` registers `UAssetApiBackend` (not `StubUAssetBackend`)

2. **Documentation Updated**
   - `StubUAssetBackend.cs` XML comment accurately describes test-only usage

3. **Build Success**
   - `dotnet build` completes with zero errors and zero warnings

4. **All Tests Pass**
   - `dotnet test` shows 100% pass rate
   - No flaky or intermittent failures

5. **Manual Verification Complete**
   - All three endpoints tested with real requests
   - Each endpoint produces real (non-stub) outputs
   - Round-trip test validates data integrity

6. **Consistency Verified**
   - Logging uses consistent patterns with other adapters
   - Error handling matches `RetocAdapter` and other adapters
   - Tool name is `"uassetapi"` (lowercase) in all logs/errors

7. **Checklist Complete**
   - All items in `Checklist_Phase5_DI_Switch.md` marked as done
   - Justification provided for any items that cannot be completed

---

## Rollback Strategy

If critical issues are discovered during Phase 5 execution:

### Immediate Rollback
**Action:** Revert `DependencyInjection.cs` to use `StubUAssetBackend`

**When to Rollback:**
- All tests fail after DI switch
- Runtime crashes or unhandled exceptions occur
- Data corruption detected in round-trip tests

### Investigate and Fix Forward
**Action:** Keep DI change, fix discovered bugs in `UAssetApiBackend`

**When to Fix Forward:**
- Isolated test failures in specific scenarios
- Minor error message inconsistencies
- Performance issues (unless unacceptably slow)

---

## Notes

### Why Keep StubUAssetBackend?
The stub is NOT dead code. It serves a legitimate testing purpose:
- `UAssetServiceTests` uses it to test orchestration logic independently
- Tests can run fast without requiring real .uasset files
- Tests remain deterministic (stub always returns same results)

Keeping test-only implementations is standard practice in layered architectures.

---

### Why Phase 5 is Low Risk
- Phases 2-4 already implemented and tested all UAssetAPI operations
- This phase only changes which implementation is used at runtime
- `IUAssetBackend` abstraction provides clear boundary
- Rollback is trivial (one-line change)

---

### Connection to Phase 6
Phase 5 focuses on switching to the real implementation. Phase 6 (Documentation & Verification) will:
- Write comprehensive integration notes
- Document all supported/unsupported options
- Perform exhaustive manual testing
- Create usage examples

Keep Phase 5 focused on the DI switch only.
