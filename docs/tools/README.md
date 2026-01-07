# Tool Documentation

This directory contains auto-generated documentation and schema files for ARIS tools.

## Directory Structure

```
docs/tools/
├── README.md                    # This file
└── <tool>/                      # Per-tool documentation
    ├── help.txt                 # Main tool help output
    ├── commands/                # Per-command help output
    │   └── <command>.txt
    ├── manifest.json            # Tool metadata
    ├── schema.generated.json    # Auto-generated schema
    ├── schema.manual.json       # Manual overlay (annotations only)
    └── schema.effective.json    # Merged schema (what ARIS serves)
```

## Generating Documentation

### Prerequisites

1. Ensure ARIS has been run at least once to extract tools to:
   `%LOCALAPPDATA%/ARIS/tools/{version}/`

2. Build the generator:
   ```powershell
   dotnet build tools/Aris.ToolDocsGen/Aris.ToolDocsGen.csproj
   ```

### Generate for a Single Tool

```powershell
dotnet run --project tools/Aris.ToolDocsGen -- generate --tool retoc --out docs/tools
```

### Generate for All Tools

```powershell
dotnet run --project tools/Aris.ToolDocsGen -- generate --all --out docs/tools
```

### Validate Schema

```powershell
dotnet run --project tools/Aris.ToolDocsGen -- validate --tool retoc --docs docs/tools
```

## Schema Files

### schema.generated.json

Auto-generated from tool help output. **Do not edit manually.** Regenerated on each run.

### schema.manual.json

Manual overlay for annotations. You may add:
- `summary` and `description` for commands and positionals
- `typeHint` overrides for positionals
- `required` overrides for edge cases

**You may NOT add:**
- New commands not in generated schema
- New positionals not in generated schema
- New options/flags not in captured help output

### schema.effective.json

Merged result of generated + manual. This is what the ARIS backend serves to the frontend.

## Options Parsing Discipline

**CRITICAL RULE:** Options are **only emitted when explicitly present** in captured help output.

The generator parses options conservatively:
1. Only matches lines with clear option syntax: `-f, --flag` or `--option <VALUE>`
2. Options not documented in help text are **never added** to the schema
3. Manual overlays **cannot introduce** new options
4. This ensures schema reflects actual tool capabilities

**Why this matters:**
- Prevents schema drift from actual tool behavior
- Ensures frontend UI only exposes documented options
- Guarantees generated commands will not fail due to unknown flags

**Enforcement:**
- `SchemaMerger` rejects overlay entries for non-existent options
- `ToolSchemaCoverageTests` validate schema structure without hardcoding option names
- Any option in `schema.effective.json` must trace back to `help.txt` or command-specific help

## Workflow

1. Run the generator to capture help and create schema
2. Optionally edit `schema.manual.json` to add annotations
3. Re-run the generator to regenerate `schema.effective.json`
4. Commit changes to the repository
5. Backend will serve the effective schema via `/api/tools/{tool}/schema`

## API Endpoints

| Endpoint | Description |
|----------|-------------|
| `GET /api/tools/{tool}/help` | Raw help text (`text/plain`) |
| `GET /api/tools/{tool}/schema` | Effective schema (`application/json`) |

Allowed tools: `retoc`, `uwpdumper`, `dllinjector`, `uasset`
