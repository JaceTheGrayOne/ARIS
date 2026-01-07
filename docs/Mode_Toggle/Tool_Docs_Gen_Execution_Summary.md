# Tool Docs Generator - Execution Summary

## Overview

Successfully implemented the Tool Docs Generator feature as specified in `docs/plans/Tool_Docs_Gen_Plan.md`. The implementation generates standardized JSON schema from CLI tool help output and serves it through backend endpoints.

## Deliverables

### A. ToolDocsGen Console Project (`tools/Aris.ToolDocsGen/`)

**Created Files:**
- `Aris.ToolDocsGen.csproj` - .NET 8 console app with System.CommandLine
- `Program.cs` - Entry point with `generate` command
- `Schema/ToolSchema.cs` - Top-level schema container
- `Schema/ToolCommandSchema.cs` - Command definition model
- `Schema/ToolPositionalSchema.cs` - Positional argument model
- `Schema/ToolOptionSchema.cs` - Option/flag model
- `Schema/SchemaMerger.cs` - Overlay merge logic (annotation-only)
- `Schema/SchemaNormalizer.cs` - Schema normalization
- `Parsers/HelpParser.cs` - Conservative help text parser
- `Parsers/UsageLineParser.cs` - Usage line positional extraction
- `Collectors/HelpCollector.cs` - Process execution and help capture
- `Commands/GenerateCommand.cs` - System.CommandLine command handler

**Key Design Decisions:**
- Uses `ToolManifestLoader` from `Aris.Tools` for tool resolution
- Parses help text conservatively - preserves raw usage lines
- Type hints inferred from argument names (path, integer, string)
- Manual overlay can only annotate existing elements, not add new ones
- Filters `[OPTIONS]` placeholder from positional parsing

### B. Docs Structure (`docs/tools/`)

**Created Files:**
- `docs/tools/README.md` - Structure documentation
- `docs/tools/retoc/help.txt` - Raw captured help output
- `docs/tools/retoc/manifest.json` - Tool metadata (version, hash, commands)
- `docs/tools/retoc/schema.generated.json` - Parser-generated schema
- `docs/tools/retoc/schema.effective.json` - Merged final schema

**Schema Stats for Retoc v0.1.4:**
- 13 commands discovered and documented
- All commands have usage lines and positionals
- Options captured with short names and value hints where applicable

### C. Backend Endpoints

**Modified Files:**
- `src/Aris.Hosting/Endpoints/ToolDocsEndpoints.cs` (new)
- `src/Aris.Hosting/Program.cs` (added endpoint registration)
- `src/Aris.Hosting/Aris.Hosting.csproj` (added docs content)

**Endpoints:**
- `GET /api/tools/{tool}/help` - Returns raw help.txt content
- `GET /api/tools/{tool}/schema` - Returns schema.effective.json

**Security:**
- Tool name allowlist: `retoc`, `uwpdumper`, `dllinjector`, `uasset`
- Uses `IWebHostEnvironment.ContentRootPath` for file resolution
- Returns 400 for unknown tools, 404 for missing files

### D. Frontend Migration

**Modified Files:**
- `frontend/src/api/retocClient.ts`

**Changes:**
- `getRetocHelp()` now fetches from `/api/tools/retoc/help`
- Wraps response in markdown code block for UI compatibility
- `getRetocSchema()` retained on `/api/retoc/command-schema` for existing UI

### E. Tests

**Created Files:**
- `tests/Aris.Core.Tests/ToolDocs/ToolSchemaCoverageTests.cs`

**Test Coverage (7 tests):**
1. `AllRetocCommandTypes_HaveSchemaEntry` - Enum to schema coverage
2. `SchemaCommands_MapToValidRetocCommandType` - Schema to enum validity
3. `EachCommand_HasAtLeastOneUsageLine` - Usage line presence
4. `RequiredPositionals_HaveValidTypeHints` - Type hint validation
5. `RequiredPositionalCount_IsRepresentableByDomainModel` - Max 5 positionals
6. `OptionalPositionals_AreMarkedCorrectly` - Optional after required only
7. `PositionalTypeHints_AreConsistentWithDomainModelTypes` - Valid hint values

**Test Behavior:**
- Uses `SkippableFact` for graceful skip when schema not generated
- Normalizes enum names (UnpackRaw to unpack-raw) for comparison
- No hardcoded positional names - validates structure only

## Issues Resolved

1. **Help parser CRLF handling**: Normalized line endings before parsing
2. **`[OPTIONS]` as positional**: Added filter to skip this placeholder
3. **Enum name format mismatch**: Added bidirectional normalization (PascalCase to hyphenated)

## Verification

```
Build:        dotnet build                    SUCCESS (0 errors, 3 warnings)
Tests:        dotnet test                     SUCCESS (238 passed, 16 skipped)
Frontend:     npm run build                   SUCCESS (built in 2.74s)
Generator:    dotnet run generate retoc       SUCCESS (13 commands)
```

## Usage

Generate docs for a tool:
```bash
dotnet run --project tools/Aris.ToolDocsGen generate --tool retoc --out docs/tools
```

Generate docs for all configured tools:
```bash
dotnet run --project tools/Aris.ToolDocsGen generate --all --out docs/tools
```

## Files Changed Summary

| Category | Files |
|----------|-------|
| New project | 12 files in `tools/Aris.ToolDocsGen/` |
| Docs | 5 files in `docs/tools/` |
| Backend | 3 files modified |
| Frontend | 1 file modified |
| Tests | 1 new file |

---

## Addendum: Options Parsing Discipline Documentation (2025-12-24)

**Change:** Added explicit documentation about options parsing rules in `docs/tools/README.md`.

**Files Modified:**
- `docs/tools/README.md` - Added "Options Parsing Discipline" section

**New Section:** "Options Parsing Discipline"

**Key Rules:**
1. Options **only emitted when explicitly present** in captured help output
2. Parser uses conservative regex matching for option syntax: `-f, --flag` or `--option <VALUE>`
3. Manual overlays **cannot introduce new options**
4. Ensures schema reflects actual tool capabilities

**Enforcement Mechanisms:**
- `SchemaMerger` rejects overlay entries for non-existent options
- `ToolSchemaCoverageTests` validate structure without hardcoding names
- Traceability: every option in `schema.effective.json` traces to `help.txt`

**Why This Matters:**
- Prevents schema drift from tool behavior
- Frontend UI only exposes documented options
- Generated commands guaranteed not to fail due to unknown flags

**Note on Canonical Schema Source:**
- `/api/tools/{tool}/schema` serves the generated schema (canonical for documentation)
- `/api/retoc/schema` serves domain-specific schema (used by Advanced Command Builder)
- These are intentionally separate: generated schema is help-text-derived with generic positionals, while Advanced Mode requires domain model field mappings

### Verification (Follow-Up)

```
Build:        dotnet build                    SUCCESS (0 errors, 0 warnings)
Tests:        dotnet test                     SUCCESS (238 passed, 16 skipped)
```

---

## Addendum: Semantics Overlay Implementation (2025-12-25)

### Overview

Implemented semantics overlay for retoc schema that enables Advanced Mode to derive its domain schema from the canonical generated schema plus a UI mapping overlay.

### Files Created/Modified

**New Files:**
- `docs/tools/retoc/ui.mapping.json` - Semantics overlay with positional mappings, command overrides, display names, and field UI hints
- `src/Aris.Contracts/Retoc/RetocFieldUiHint.cs` - DTO for path kind and extensions
- `src/Aris.Adapters/Retoc/RetocSchemaDerived.cs` - Derivation logic from canonical + overlay
- `tests/Aris.Core.Tests/ToolDocs/RetocSchemaDerivedTests.cs` - 9 tests for derived schema

**Modified Files:**
- `src/Aris.Contracts/Retoc/RetocCommandFieldDefinition.cs` - Added PathKind and Extensions properties
- `src/Aris.Contracts/Retoc/RetocCommandDefinition.cs` - Added FieldUiHints dictionary
- `src/Aris.Hosting/Endpoints/RetocEndpoints.cs` - Updated /api/retoc/schema to derive from canonical + overlay
- `frontend/src/types/contracts.ts` - Added RetocFieldUiHint interface and fieldUiHints to RetocCommandDefinition
- `frontend/src/components/retoc/RetocAdvancedCommandBuilder.tsx` - Updated to use field UI hints for placeholder and help text

### Key Features

1. **Positional Mappings** (ui.mapping.json):
   - `INPUT` → `InputPath`
   - `OUTPUT` → `OutputPath`
   - `CHUNK_ID` → `ChunkIndex`
   - `VERSION` → `EngineVersion`
   - etc.

2. **Command Overrides** (ui.mapping.json):
   - `get`: required [InputPath, ChunkIndex], optional [OutputPath]
   - `info`, `list`, `manifest`, `verify`: required [InputPath] only

3. **Per-Command Field UI Hints** (ui.mapping.json):
   - `to-legacy`: InputPath=folder, OutputPath=folder
   - `to-zen`: InputPath=folder, OutputPath=folder
   - `get`: InputPath=file (.utoc), OutputPath=folder
   - `unpack`/`unpack-raw`: InputPath=file (.utoc), OutputPath=folder
   - etc.

4. **Derivation Rules**:
   - Overlay CANNOT add commands/options not present in canonical schema
   - Overlay can only rename/annotate existing elements
   - AllowlistedFlags derived from canonical schema options

### Test Coverage (9 new tests)

1. `DerivedCommandSet_EqualsCanonicalCommandSet` - Command set parity
2. `GetCommand_HasCorrectRequiredAndOptionalFields` - get command field validation
3. `DerivedSchema_DoesNotContainInventedOptions` - No invented options
4. `ToLegacyCommand_HasFolderPathKindForBothPaths` - UI hints for to-legacy
5. `ToZenCommand_HasFolderPathKindForBothPaths` - UI hints for to-zen
6. `GetCommand_HasFilePathKindWithUtocExtension` - UI hints for get (.utoc)
7. `InfoCommand_HasOnlyInputPathRequired` - Info command field validation
8. `AllDerivedCommands_HaveValidCommandType` - Enum validation
9. `AllDerivedCommands_HaveDisplayNameAndDescription` - Display text validation

### Verification

```
Build:        dotnet build        SUCCESS (0 errors, 0 warnings)
Tests:        dotnet test         SUCCESS (247 passed, 16 skipped)
Frontend:     npm run build       SUCCESS (built in 3.38s)
```

### Architecture Notes

- Canonical schema remains at `/api/tools/retoc/schema` (unchanged)
- `/api/retoc/schema` now derives from canonical + overlay (was: RetocCommandSchemaProvider)
- Advanced Mode continues to work unchanged (schema shape preserved)
- UI hints flow through to frontend for improved placeholder/help text

---
*Initial: 2025-12-24*
*Follow-Up: 2025-12-24*
*Semantics Overlay: 2025-12-25*
