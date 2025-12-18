# UAsset Inspect Endpoint - Contract Compatibility Report

**Date**: 2025-12-17
**Phase**: Phase 2 Verification
**Scope**: Inspect endpoint only (Serialize/Deserialize not yet implemented)

---

## Executive Summary

The Phase 2 Inspect implementation has **one critical contract incompatibility** between C# backend and TypeScript frontend, plus one minor naming convention issue. Both require fixes.

**Status**: ❌ **Incompatible** - Frontend will fail to display asset path

---

## Contract Analysis

### A) Request Contract: `UAssetInspectRequest`

#### C# Definition (`src/Aris.Contracts/UAsset/UAssetInspectRequest.cs`)
```csharp
public sealed record UAssetInspectRequest(
    string InputAssetPath,
    IReadOnlyList<string>? Fields  // Optional
);
```

#### TypeScript Definition (`frontend/src/types/contracts.ts:90-93`)
```typescript
export interface UAssetInspectRequest {
  inputAssetPath: string;
  fields?: string[] | null;  // Optional
}
```

#### Endpoint Mapping (`src/Aris.Hosting/Endpoints/UAssetEndpoints.cs:219-224`)
```csharp
var command = new UAssetInspectCommand
{
    OperationId = operationId,
    InputAssetPath = request.InputAssetPath,
    Fields = request.Fields ?? Array.Empty<string>()
};
```

**Status**: ✅ **Compatible**
- Field names match (PascalCase in C#, camelCase in TypeScript - standard serialization convention)
- Optional `Fields` handled correctly with null coalescing
- No UEVersion field in request (correctly omitted for auto-detection behavior)

---

### B) Response Contract: `UAssetInspectResponse`

#### C# Definition (`src/Aris.Contracts/UAsset/UAssetInspectResponse.cs`)
```csharp
public sealed record UAssetInspectResponse(
    string OperationId,
    OperationStatus Status,
    UAssetInspectionDto? Result,  // Nullable on failure
    ErrorInfo? Error,
    DateTimeOffset StartedAt,
    DateTimeOffset CompletedAt
);
```

#### TypeScript Definition (`frontend/src/types/contracts.ts:140-147`)
```typescript
export interface UAssetInspectResponse {
  operationId: string;
  status: OperationStatus;
  result?: UAssetInspectionDto | null;
  error?: ErrorInfo | null;
  startedAt: string;
  completedAt: string;
}
```

**Status**: ✅ **Compatible**
- Standard response envelope matches across all endpoints
- Nullable Result/Error handled correctly

---

### C) Inspection Result Contract: `UAssetInspectionDto` ❌

#### C# Definition (`src/Aris.Contracts/UAsset/UAssetInspectionDto.cs`)
```csharp
public sealed record UAssetInspectionDto(
    string OperationId,          // Present
    string AssetPath,            // ⚠️ Field name: AssetPath
    UAssetSummaryDto Summary,
    IReadOnlyList<string>? Exports,   // Nullable
    IReadOnlyList<string>? Imports,   // Nullable
    IReadOnlyList<string>? Names,     // Nullable
    string? LogExcerpt            // Present, nullable
);
```

#### TypeScript Definition (`frontend/src/types/contracts.ts:107-120`)
```typescript
export interface UAssetInspectionDto {
  inputPath: string,           // ⚠️ Field name: inputPath (WRONG)
  summary: {
    ueVersion?: string | null;
    licenseeVersion: number;
    customVersionCount: number;
    nameCount: number;
    exportCount: number;
    importCount: number;
  };
  exports?: string[] | null;   // Nullable
  imports?: string[] | null;   // Nullable
  names?: string[] | null;     // Nullable
  // ⚠️ Missing: operationId
  // ⚠️ Missing: logExcerpt
}
```

#### Backend Mapping (`src/Aris.Hosting/Endpoints/UAssetEndpoints.cs:316-336`)
```csharp
return new UAssetInspectionDto(
    OperationId: operationId,
    AssetPath: inspection.InputPath,  // Maps to AssetPath
    Summary: summaryDto,
    Exports: inspection.Exports,
    Imports: inspection.Imports,
    Names: inspection.Names,
    LogExcerpt: null
);
```

#### Frontend Usage (`frontend/src/components/uasset/UAssetResultPanel.tsx`)
```typescript
// Line 223: BROKEN - accesses wrong field name
{result.inputPath}  // ❌ Should be result.assetPath

// Lines 268-311: Correctly handles nullable fields
{result.exports && result.exports.length > 0 && (...)}
{result.imports && result.imports.length > 0 && (...)}
{result.names && result.names.length > 0 && (...)}
```

**Status**: ❌ **INCOMPATIBLE**

**Issues**:
1. **Critical**: C# backend sends `assetPath`, TypeScript expects `inputPath`
   - **Impact**: Frontend will display `undefined` for asset path (line UAssetResultPanel.tsx:223)
   - **Fix Required**: Update TypeScript to use `assetPath` to match C# contract

2. **Minor**: TypeScript missing `operationId` and `logExcerpt` fields
   - **Impact**: No current usage in frontend, but creates contract drift
   - **Fix Required**: Add missing fields to TypeScript definition

---

## Field-Level Compatibility Matrix

| Field | C# Type | TypeScript Type | Backend Populates | Frontend Accesses | Compatible |
|-------|---------|-----------------|-------------------|-------------------|------------|
| **operationId** | `string` | ❌ Missing | ✅ Yes | ❌ No | ⚠️ Missing in TS |
| **assetPath / inputPath** | `string` | `string` | ✅ Yes | ❌ Wrong name | ❌ **BROKEN** |
| **summary** | `UAssetSummaryDto` | `object` | ✅ Yes | ✅ Yes | ✅ |
| **summary.ueVersion** | `string` | `string?` | ✅ Yes | ✅ Yes (optional) | ✅ |
| **summary.licenseeVersion** | `int` | `number` | ✅ Yes | ✅ Yes | ✅ |
| **summary.customVersionCount** | `int` | `number` | ✅ Yes | ✅ Yes | ✅ |
| **summary.nameCount** | `int` | `number` | ✅ Yes | ✅ Yes | ✅ |
| **summary.exportCount** | `int` | `number` | ✅ Yes | ✅ Yes | ✅ |
| **summary.importCount** | `int` | `number` | ✅ Yes | ✅ Yes | ✅ |
| **exports** | `IReadOnlyList<string>?` | `string[]?` | ✅ Yes (when requested) | ✅ Yes (safe check) | ✅ |
| **imports** | `IReadOnlyList<string>?` | `string[]?` | ✅ Yes (when requested) | ✅ Yes (safe check) | ✅ |
| **names** | `IReadOnlyList<string>?` | `string[]?` | ✅ Yes (when requested) | ✅ Yes (safe check) | ✅ |
| **logExcerpt** | `string?` | ❌ Missing | ✅ Yes (null) | ❌ No | ⚠️ Missing in TS |

---

## Error Handling Analysis

### Current UAssetApiBackend Error Behavior (`src/Aris.Adapters/UAsset/UAssetApiBackend.cs:103-111`)

```csharp
catch (Exception ex) when (ex is not ArisException)
{
    _logger.LogError(ex, "Failed to inspect asset: {Path}", command.InputAssetPath);

    throw new ToolExecutionError(
        "UAssetAPI",  // ⚠️ Tool name
        -1,
        $"Failed to inspect asset '{Path.GetFileName(command.InputAssetPath)}': {ex.Message}");
}
```

### Comparison with Other Adapters

**Retoc** (`src/Aris.Adapters/Retoc/RetocAdapter.cs:132`):
```csharp
throw new ToolExecutionError("retoc", processResult.ExitCode, "Retoc conversion failed")
```

**UWPDumper** (`src/Aris.Adapters/UwpDumper/UwpDumperAdapter.cs:139`):
```csharp
throw new ToolExecutionError("uwpdumper", processResult.ExitCode, "UWPDumper dump operation failed")
```

**DLL Injector** (`src/Aris.Adapters/DllInjector/DllInjectorAdapter.cs:145`):
```csharp
throw new ToolExecutionError("dllinjector", processResult.ExitCode, "DLL injection operation failed")
```

### Error Handling Status

**Issue**: Tool name inconsistency
- **Current**: `"UAssetAPI"` (PascalCase)
- **Convention**: All other adapters use lowercase tool names (`"retoc"`, `"uwpdumper"`, `"dllinjector"`)
- **Fix Required**: Change to `"uassetapi"` for consistency

**Endpoint Error Mapping** (`src/Aris.Hosting/Endpoints/UAssetEndpoints.cs:246-288`):
- ✅ Catches `ArisException` and maps to `ErrorInfo` correctly
- ✅ Uses standard response envelope with null Result, populated Error
- ✅ Maps exception types to HTTP status codes via `MapExceptionToStatusCode`
- ✅ No sensitive data leakage (uses `Path.GetFileName`, not full paths in user messages)
- ✅ Logs full exception details server-side

**Status**: ⚠️ **Minor Fix Required** - Tool name convention only

---

## UEVersion Option Handling

### Request Contract Analysis

**C# Serialize/Deserialize Requests** (`src/Aris.Contracts/UAsset/`):
```csharp
// UAssetSerializeRequest has UEVersion
public sealed record UAssetSerializeRequest(..., string? UEVersion, ...);

// UAssetDeserializeRequest has UEVersion
public sealed record UAssetDeserializeRequest(..., string? UEVersion, ...);

// UAssetInspectRequest does NOT have UEVersion
public sealed record UAssetInspectRequest(string InputAssetPath, IReadOnlyList<string>? Fields);
```

### Backend Behavior

**Current Implementation** (`src/Aris.Adapters/UAsset/UAssetApiBackend.cs:48`):
```csharp
var asset = new global::UAssetAPI.UAsset(command.InputAssetPath, EngineVersion.UNKNOWN);
```

- **Inspect**: Always uses `EngineVersion.UNKNOWN` for auto-detection
- **Serialize/Deserialize**: Not yet implemented (Phase 3/4)

### Status

✅ **Correct Design**
- Inspect requests do NOT include UEVersion option (by design)
- UAssetAPI auto-detects version from versioned assets
- Serialize/Deserialize requests WILL support explicit UEVersion when implemented in Phase 3/4
- No test case needed for Inspect UEVersion handling (not applicable)

---

## Recommendations

### Option 1: Update Frontend to Match C# Contracts (RECOMMENDED)

**Changes Required**:

1. **Fix TypeScript contract** (`frontend/src/types/contracts.ts:107-120`):
   ```typescript
   export interface UAssetInspectionDto {
     operationId: string;         // ADD
     assetPath: string;           // RENAME from inputPath
     summary: {
       ueVersion?: string | null;
       licenseeVersion: number;
       customVersionCount: number;
       nameCount: number;
       exportCount: number;
       importCount: number;
     };
     exports?: string[] | null;
     imports?: string[] | null;
     names?: string[] | null;
     logExcerpt?: string | null;  // ADD
   }
   ```

2. **Fix frontend usage** (`frontend/src/components/uasset/UAssetResultPanel.tsx:223`):
   ```typescript
   {result.assetPath}  // Change from result.inputPath
   ```

**Pros**:
- Aligns frontend with canonical C# contracts
- No backend changes required
- Fixes current broken behavior
- Completes contract alignment

**Cons**:
- Requires frontend changes

---

### Option 2: Update C# to Match Frontend (NOT RECOMMENDED)

**Changes Required**:

1. Rename `AssetPath` → `InputPath` in C# DTO
2. Remove `OperationId` and `LogExcerpt` from DTO

**Pros**: None

**Cons**:
- C# contracts are canonical source of truth per architecture
- Creates inconsistency with Serialize/Deserialize result DTOs (which use `InputPath`/`OutputPath`)
- Loses useful fields (`operationId`, `logExcerpt`)
- Violates architecture principle that C# defines contracts

---

## Decision

**Selected**: Option 1 - Update Frontend to Match C# Contracts

**Rationale**:
- C# backend is the canonical contract definition per ARIS architecture
- Fixes broken asset path display in UI
- Completes contract alignment for Phase 2
- Minimal scope: 2 file changes in frontend only

---

## Required Changes Summary

### 1. Frontend Contract Fix (CRITICAL)
- **File**: `frontend/src/types/contracts.ts`
- **Line**: 107-120
- **Change**: Rename `inputPath` → `assetPath`, add `operationId` and `logExcerpt`

### 2. Frontend Component Fix (CRITICAL)
- **File**: `frontend/src/components/uasset/UAssetResultPanel.tsx`
- **Line**: 223
- **Change**: Update `result.inputPath` → `result.assetPath`

### 3. Backend Tool Name Fix (MINOR)
- **File**: `src/Aris.Adapters/UAsset/UAssetApiBackend.cs`
- **Line**: 108
- **Change**: Update tool name `"UAssetAPI"` → `"uassetapi"`

---

## Test Verification Plan

After fixes:
1. ✅ Run `dotnet test` - should still pass (backend unchanged)
2. ✅ Run `npm run build` in `frontend/` - should compile without errors
3. ✅ Manual verification:
   - Start backend: `dotnet run --project src/Aris.Hosting`
   - Start frontend: `cd frontend && npm run dev`
   - Submit Inspect request with real asset
   - Verify asset path displays correctly in result panel
   - Verify exports/imports/names display when requested

---

## Phase 2 Verification Status

| Item | Status | Notes |
|------|--------|-------|
| **A) Contract Compatibility** | ❌ Incompatible | Field name mismatch requires frontend fix |
| **B) UEVersion Handling** | ✅ Correct | No UEVersion in Inspect by design (auto-detect) |
| **C) Error Mapping** | ⚠️ Minor Fix | Tool name convention (lowercase) |
| **Overall Phase 2** | ⚠️ **Requires Fixes** | 3 changes needed before Phase 3 |

---

## Files Referenced

### Backend (C#)
- `src/Aris.Contracts/UAsset/UAssetInspectRequest.cs`
- `src/Aris.Contracts/UAsset/UAssetInspectResponse.cs`
- `src/Aris.Contracts/UAsset/UAssetInspectionDto.cs`
- `src/Aris.Contracts/UAsset/UAssetSummaryDto.cs`
- `src/Aris.Adapters/UAsset/UAssetApiBackend.cs`
- `src/Aris.Hosting/Endpoints/UAssetEndpoints.cs`

### Frontend (TypeScript)
- `frontend/src/types/contracts.ts:90-147`
- `frontend/src/api/uassetClient.ts:73-102`
- `frontend/src/pages/tools/UAssetPage.tsx:66-73`
- `frontend/src/components/uasset/UAssetResultPanel.tsx:214-315`

### Tests
- `tests/Aris.Core.Tests/Adapters/UAssetApiBackendTests.cs`
