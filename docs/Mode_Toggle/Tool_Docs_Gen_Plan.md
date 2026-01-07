# Tool Docs Generator Implementation Plan

This plan establishes a universal Tool Docs + Schema framework for ARIS, backed by ground-truth tool help output.

---

## 1) Scope

### Goals

1. Create a .NET console generator (`Aris.ToolDocsGen`) that captures tool help output from the same binaries ARIS runs at runtime.
2. Parse help output into a standardized schema JSON with conservative extraction rules.
3. Support a manual overlay file for annotating existing schema elements (descriptions, type hints, etc.).
4. Expose help text and effective schema via backend endpoints so the frontend can consume them dynamically.
5. Prevent schema drift via tests that enforce alignment between `RetocCommandType` enum and generated schema.

### Non-Goals

- Perfect parsing of every CLI tool's option formatting.
- Runtime dependency on tool help availability (docs are generated offline and committed).
- Replacing ARIS's tool execution logic (we only document/describe tools).
- Supporting tools beyond the ARIS manifest (retoc, uasset, uwpdumper, dllinjector).
- Tool extraction, validation, or state mutation (the generator is read-only).

### Initial Supported Tool

- **retoc** is the initial target.
- The architecture generalizes to other tools (uwpdumper, dllinjector, uasset) via the same generator and schema format.

### Generalization Strategy

Each tool follows the same pattern:
- Generator runs `<tool> --help` and `<tool> <cmd> --help` for discoverable commands.
- Outputs are written to `docs/tools/<tool>/` with consistent file naming.
- Backend serves via generic `/api/tools/{tool}/help` and `/api/tools/{tool}/schema` endpoints.
- Allowlisted tool names prevent arbitrary file reads.

---

## 2) Delta List (File-by-File)

### New Files

| Path | Description |
|------|-------------|
| `tools/Aris.ToolDocsGen/Aris.ToolDocsGen.csproj` | .NET 8 console project for the generator |
| `tools/Aris.ToolDocsGen/Program.cs` | Entry point with CLI command parsing |
| `tools/Aris.ToolDocsGen/Commands/GenerateCommand.cs` | `generate` command implementation |
| `tools/Aris.ToolDocsGen/Collectors/ToolHelpCollector.cs` | Runs tool binaries and captures help output |
| `tools/Aris.ToolDocsGen/Parsers/HelpParser.cs` | Parses help text into structured schema |
| `tools/Aris.ToolDocsGen/Parsers/UsageLineParser.cs` | Extracts positionals from usage lines |
| `tools/Aris.ToolDocsGen/Schema/ToolSchema.cs` | Schema model classes |
| `tools/Aris.ToolDocsGen/Schema/SchemaEmitter.cs` | Writes JSON schema files |
| `tools/Aris.ToolDocsGen/Schema/SchemaMerger.cs` | Merges generated + manual overlays |
| `tools/Aris.ToolDocsGen/Normalization/OutputNormalizer.cs` | CRLF, path redaction, determinism |
| `docs/tools/README.md` | Workflow documentation for running the generator |
| `docs/tools/retoc/help.txt` | Generated: top-level retoc help output |
| `docs/tools/retoc/commands/*.txt` | Generated: per-command help output |
| `docs/tools/retoc/manifest.json` | Generated: tool metadata (version, hash, commands) |
| `docs/tools/retoc/schema.generated.json` | Generated: parsed schema from help |
| `docs/tools/retoc/schema.manual.json` | Manual overlay (initially empty or minimal) |
| `docs/tools/retoc/schema.effective.json` | Generated: merged effective schema |
| `src/Aris.Hosting/Endpoints/ToolDocsEndpoints.cs` | Generic endpoints for tool help/schema |
| `tests/Aris.Core.Tests/ToolDocs/ToolSchemaCoverageTests.cs` | Coverage tests for enum ↔ schema alignment |
| `tests/Aris.ToolDocsGen.Tests/Aris.ToolDocsGen.Tests.csproj` | Optional: generator unit tests |
| `tests/Aris.ToolDocsGen.Tests/HelpParserTests.cs` | Optional: parser tests with fixture data |

### Modified Files

| Path | Change |
|------|--------|
| `ARIS.sln` | Add `tools/Aris.ToolDocsGen` project (new solution folder: `tools`) |
| `ARIS.sln` | Add `tests/Aris.ToolDocsGen.Tests` project (optional) |
| `src/Aris.Hosting/Program.cs` | Register `ToolDocsEndpoints.MapEndpoints(app)` |
| `frontend/src/api/retocClient.ts` | Migrate to use `/api/tools/retoc/schema` and `/api/tools/retoc/help` |
| `frontend/src/types/contracts.ts` | Add `ToolSchemaResponse` type (or reuse existing `RetocCommandSchemaResponse` shape) |

### Deprecated (Keep but Unused)

| Path | Notes |
|------|-------|
| `src/Aris.Adapters/Retoc/RetocCommandSchemaProvider.cs` | Keep for fallback; frontend migrates to canonical endpoint. Mark with `[Obsolete]` after migration. |

### No Changes Required

| Path | Notes |
|------|-------|
| `src/Aris.Tools/tools.manifest.json` | Read-only by generator |
| `src/Aris.Tools/Manifest/ToolManifestLoader.cs` | Reused by generator |
| `src/Aris.Infrastructure/Tools/DependencyValidator.cs` | Not used by generator (generator does not validate) |
| `src/Aris.Infrastructure/Tools/DependencyExtractor.cs` | Not used by generator (generator does not extract) |

---

## 3) Generator Design (`tools/`)

### Project Location

```
tools/
└── Aris.ToolDocsGen/
    ├── Aris.ToolDocsGen.csproj
    ├── Program.cs
    ├── Commands/
    │   └── GenerateCommand.cs
    ├── Collectors/
    │   └── ToolHelpCollector.cs
    ├── Parsers/
    │   ├── HelpParser.cs
    │   └── UsageLineParser.cs
    ├── Schema/
    │   ├── ToolSchema.cs
    │   ├── SchemaEmitter.cs
    │   └── SchemaMerger.cs
    └── Normalization/
        └── OutputNormalizer.cs
```

### CLI Commands and Arguments

```
Aris.ToolDocsGen generate --tool <name> --out <path>
Aris.ToolDocsGen generate --all --out <path>
Aris.ToolDocsGen validate --tool <name>  # Optional: check schema completeness
```

**Arguments:**

| Argument | Required | Description |
|----------|----------|-------------|
| `--tool <name>` | Yes (unless `--all`) | Tool ID from manifest: `retoc`, `uwpdumper`, `dllinjector` |
| `--all` | No | Generate docs for all tools in manifest |
| `--out <path>` | Yes | Output directory (e.g., `docs/tools`) |

### Generator Responsibility Boundaries

The generator is a **read-only documentation tool**. It has the following responsibilities and constraints:

**What the generator DOES:**

1. Reads the tool manifest from the embedded resource via `ToolManifestLoader.Load()`.
2. Resolves paths to already-extracted tool binaries at `%LOCALAPPDATA%/ARIS/tools/{version}/{relativePath}`.
3. Executes tool binaries with `--help` arguments to capture help output.
4. Parses help output into structured schema.
5. Writes documentation and schema files to the output directory.

**What the generator DOES NOT do:**

1. **Does NOT download tool binaries** — tools must already be present.
2. **Does NOT extract tool binaries** — extraction is handled by `DependencyExtractor` at ARIS runtime.
3. **Does NOT validate tool hashes** — validation is handled by `DependencyValidator` at ARIS runtime.
4. **Does NOT modify tool state** — the generator is purely read-only.

**Missing Binary Behavior:**

If a tool binary is not found at the expected path, the generator MUST fail immediately with a clear error:

```csharp
public string ResolveToolPath(string toolId)
{
    var manifest = ToolManifestLoader.Load();
    var entry = manifest.Tools.FirstOrDefault(t => t.Id == toolId)
        ?? throw new InvalidOperationException($"Tool '{toolId}' not found in manifest");

    var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
    var toolsRoot = Path.Combine(localAppData, "ARIS", "tools", manifest.Version);
    var toolPath = Path.Combine(toolsRoot, entry.RelativePath);

    if (!File.Exists(toolPath))
    {
        throw new FileNotFoundException(
            $"Tool binary not found at '{toolPath}'. " +
            $"Ensure ARIS has been run at least once to extract tools, or manually extract the tool.");
    }

    return toolPath;
}
```

### Tool Resolution (Reusing ARIS Mechanism)

The generator references `Aris.Tools` to load the manifest and compute tool paths using the same logic as the runtime:

```csharp
// In ToolHelpCollector.cs
using Aris.Tools.Manifest;

public class ToolHelpCollector
{
    public string ResolveToolPath(string toolId)
    {
        var manifest = ToolManifestLoader.Load();
        var entry = manifest.Tools.FirstOrDefault(t => t.Id == toolId)
            ?? throw new InvalidOperationException($"Tool '{toolId}' not found in manifest");

        var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        var toolsRoot = Path.Combine(localAppData, "ARIS", "tools", manifest.Version);
        var toolPath = Path.Combine(toolsRoot, entry.RelativePath);

        if (!File.Exists(toolPath))
        {
            throw new FileNotFoundException(
                $"Tool binary not found at '{toolPath}'. " +
                $"Run ARIS at least once to extract tools before running the generator.");
        }

        return toolPath;
    }
}
```

**Key classes reused:**
- `Aris.Tools.Manifest.ToolManifestLoader` — loads `tools.manifest.json` from embedded resource
- `Aris.Tools.Manifest.ToolManifest` — manifest model with version and tool entries
- `Aris.Tools.Manifest.ToolEntry` — individual tool metadata (id, version, sha256, relativePath)

### Output Files

For each tool, the generator writes to `docs/tools/<tool>/`:

| File | Description |
|------|-------------|
| `help.txt` | Raw output of `<tool> --help` |
| `commands/<cmd>.txt` | Raw output of `<tool> <cmd> --help` for each discovered command |
| `manifest.json` | Metadata: tool name, version, exe hash, generatedAtUtc, commands list |
| `schema.generated.json` | Parsed schema from help output |
| `schema.manual.json` | Manual overlay (created if missing, empty object by default) |
| `schema.effective.json` | Merged schema (generated + manual) |

### Normalization Rules for Determinism

```csharp
// In OutputNormalizer.cs
public static class OutputNormalizer
{
    public static string Normalize(string content)
    {
        // 1. Convert line endings to CRLF (Windows standard)
        content = content.Replace("\r\n", "\n").Replace("\r", "\n").Replace("\n", "\r\n");

        // 2. Trim trailing whitespace from each line
        var lines = content.Split("\r\n");
        lines = lines.Select(l => l.TrimEnd()).ToArray();

        // 3. Ensure single trailing newline
        return string.Join("\r\n", lines).TrimEnd() + "\r\n";
    }

    public static string RedactAbsolutePaths(string content)
    {
        // Replace user-specific paths with placeholder
        // e.g., C:\Users\john\AppData\Local\ARIS\tools\... → <TOOLS_ROOT>\...
        var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        var toolsRoot = Path.Combine(localAppData, "ARIS", "tools");
        return content.Replace(toolsRoot, "<TOOLS_ROOT>");
    }
}
```

**JSON normalization:**
- Use `JsonSerializerOptions` with `WriteIndented = true`
- Sort object keys alphabetically for stable diffs
- Use `JsonNamingPolicy.CamelCase`

---

## 4) Schema Format

### Standardized JSON Schema Structure

```json
{
  "tool": "retoc",
  "version": "v0.1.4",
  "generatedAtUtc": "2025-12-24T12:00:00Z",
  "commands": [
    {
      "name": "get",
      "summary": "Extract a single chunk from an IoStore container",
      "usages": [
        "retoc.exe get <INPUT> <CHUNK_ID> [OUTPUT]"
      ],
      "positionals": [
        {
          "name": "INPUT",
          "index": 0,
          "required": true,
          "typeHint": "path",
          "description": "Path to the IoStore container"
        },
        {
          "name": "CHUNK_ID",
          "index": 1,
          "required": true,
          "typeHint": "integer",
          "description": "Chunk index to extract"
        },
        {
          "name": "OUTPUT",
          "index": 2,
          "required": false,
          "typeHint": "path",
          "description": "Output path (optional, defaults to stdout)"
        }
      ],
      "options": []
    }
  ],
  "globalOptions": []
}
```

### Required vs Optional Fields

| Field | Required | Notes |
|-------|----------|-------|
| `tool` | Yes | Tool ID matching manifest |
| `version` | No | Parsed from help output if available |
| `generatedAtUtc` | Yes | ISO 8601 timestamp |
| `commands` | Yes | Array (may be empty if no subcommands) |
| `commands[].name` | Yes | Command name (lowercase) |
| `commands[].summary` | No | Brief description (from manual overlay if not parseable) |
| `commands[].usages` | Yes | Raw usage lines (always preserved) |
| `commands[].positionals` | Yes | Array of positional arguments |
| `commands[].positionals[].name` | Yes | Argument name as shown in usage |
| `commands[].positionals[].index` | Yes | Zero-based position |
| `commands[].positionals[].required` | Yes | `true` for `<ARG>`, `false` for `[ARG]` |
| `commands[].positionals[].typeHint` | No | `path`, `integer`, `string`, `enum` |
| `commands[].positionals[].description` | No | From manual overlay |
| `commands[].options` | Yes | Array (empty if not confidently parseable) |
| `globalOptions` | No | Options available for all commands (only from captured help) |

### Conservative Parsing Rules

1. **Always preserve raw usage lines** — `usages[]` is never synthesized.
2. **Parse positionals from usage line** using heuristics:
   - `<ARG>` → required, name = "ARG"
   - `[ARG]` → optional, name = "ARG"
   - `[ARG...]` → optional, variadic
3. **Parse options only when confident**:
   - Match patterns: `--flag`, `-f, --flag`, `--option <VALUE>`
   - If help text uses non-standard formatting, leave `options` empty and rely on manual overlay for annotation only.
4. **Type hints are inferred conservatively**:
   - Names containing `PATH`, `FILE`, `DIR` → `path`
   - Names like `INDEX`, `ID`, `NUM`, `COUNT` → `integer`
   - Otherwise → `string`

### Manual Overlay File (`schema.manual.json`)

The manual overlay allows annotating existing schema elements with additional metadata. It is **strictly additive for annotations** but **cannot introduce new structural elements**.

**Example overlay (annotation-only):**

```json
{
  "commands": {
    "get": {
      "summary": "Extract a single chunk from an IoStore container",
      "positionals": {
        "CHUNK_ID": {
          "typeHint": "integer",
          "description": "Zero-based chunk index"
        }
      }
    }
  }
}
```

### Manual Overlay Rules

**CRITICAL CONSTRAINT:** Manual overlays exist to annotate and enrich the generated schema, not to extend it with invented elements.

| Allowed | Not Allowed |
|---------|-------------|
| Add `summary` to an existing command | Add a command not in `schema.generated.json` |
| Add `description` to an existing positional | Add a positional not in `schema.generated.json` |
| Override `typeHint` for an existing positional | Add options not present in captured help output |
| Add `description` to an existing option | Invent new options or flags |
| Override `required` for edge cases | Add `globalOptions` not in captured help output |

The merge logic MUST enforce these rules:

```csharp
// In SchemaMerger.cs
public ToolSchema MergeSchemas(ToolSchema generated, ManualOverlay manual)
{
    // 1. Start with generated schema as base
    var effective = generated.DeepClone();

    // 2. For each command in manual overlay:
    foreach (var (cmdName, cmdOverlay) in manual.Commands)
    {
        var cmd = effective.Commands.FirstOrDefault(c => c.Name == cmdName);
        if (cmd == null)
        {
            // RULE: Cannot add commands not in generated schema
            Console.WriteLine($"Warning: Overlay references unknown command '{cmdName}', skipping.");
            continue;
        }

        // Override summary if provided (annotation allowed)
        if (!string.IsNullOrEmpty(cmdOverlay.Summary))
            cmd.Summary = cmdOverlay.Summary;

        // Merge positionals (annotation only - cannot add new positionals)
        foreach (var (posName, posOverlay) in cmdOverlay.Positionals)
        {
            var pos = cmd.Positionals.FirstOrDefault(p => p.Name == posName);
            if (pos == null)
            {
                // RULE: Cannot add positionals not in generated schema
                Console.WriteLine($"Warning: Overlay references unknown positional '{posName}' in command '{cmdName}', skipping.");
                continue;
            }

            // Annotations allowed
            if (!string.IsNullOrEmpty(posOverlay.TypeHint))
                pos.TypeHint = posOverlay.TypeHint;
            if (!string.IsNullOrEmpty(posOverlay.Description))
                pos.Description = posOverlay.Description;
            if (posOverlay.Required.HasValue)
                pos.Required = posOverlay.Required.Value;
        }

        // RULE: Options in overlay can only annotate existing options, not add new ones
        if (cmdOverlay.Options != null)
        {
            foreach (var optOverlay in cmdOverlay.Options)
            {
                var existing = cmd.Options.FirstOrDefault(o => o.Name == optOverlay.Name);
                if (existing == null)
                {
                    Console.WriteLine($"Warning: Overlay references unknown option '{optOverlay.Name}' in command '{cmdName}', skipping.");
                    continue;
                }

                // Annotation allowed
                if (!string.IsNullOrEmpty(optOverlay.Description))
                    existing.Description = optOverlay.Description;
            }
        }
    }

    // 3. Global options: annotation only, no additions
    if (manual.GlobalOptions != null)
    {
        foreach (var optOverlay in manual.GlobalOptions)
        {
            var existing = effective.GlobalOptions.FirstOrDefault(o => o.Name == optOverlay.Name);
            if (existing == null)
            {
                Console.WriteLine($"Warning: Overlay references unknown global option '{optOverlay.Name}', skipping.");
                continue;
            }

            if (!string.IsNullOrEmpty(optOverlay.Description))
                existing.Description = optOverlay.Description;
        }
    }

    return effective;
}
```

### Output Artifacts

| File | Content |
|------|---------|
| `schema.generated.json` | Parser output only; regenerated on each run |
| `schema.manual.json` | Human-edited overlay; created if missing with empty structure |
| `schema.effective.json` | Merged result; this is what the backend serves |

---

## 5) Backend Integration

### File Resolution Strategy

The backend resolves `docs/tools/` files using `IWebHostEnvironment.ContentRootPath`. This approach is preferred over `AppContext.BaseDirectory` because:

1. **ASP.NET Core standard** — `ContentRootPath` is the idiomatic way to locate content files in ASP.NET Core applications.
2. **Development vs Production parity** — Works correctly in both `dotnet run` (source directory) and published scenarios.
3. **Testability** — `IWebHostEnvironment` can be mocked in tests.

**Alternative considered:** Embedded resources. This would eliminate file system access but would require rebuilding the host project after regenerating docs, making the dev workflow slower. Files on disk are preferred for offline regeneration.

### New Endpoint File

**Location:** `src/Aris.Hosting/Endpoints/ToolDocsEndpoints.cs`

```csharp
using Microsoft.AspNetCore.Mvc;

namespace Aris.Hosting.Endpoints;

public static class ToolDocsEndpoints
{
    private static readonly HashSet<string> AllowedTools = new(StringComparer.OrdinalIgnoreCase)
    {
        "retoc",
        "uwpdumper",
        "dllinjector",
        "uasset"
    };

    public static void MapEndpoints(WebApplication app)
    {
        var group = app.MapGroup("/api/tools/{tool}");

        group.MapGet("/help", GetToolHelp);
        group.MapGet("/schema", GetToolSchema);
    }

    private static IResult GetToolHelp(
        string tool,
        IWebHostEnvironment env)
    {
        // Validate tool name (security: allowlist only)
        if (!AllowedTools.Contains(tool))
        {
            return Results.BadRequest(new ErrorInfo(
                Code: "INVALID_TOOL",
                Message: $"Tool '{tool}' is not recognized.",
                Suggestion: $"Valid tools: {string.Join(", ", AllowedTools)}"));
        }

        // Resolve help file path via ContentRootPath
        var helpPath = Path.Combine(
            env.ContentRootPath,
            "docs", "tools", tool.ToLowerInvariant(), "help.txt");

        if (!File.Exists(helpPath))
        {
            return Results.NotFound(new ErrorInfo(
                Code: "HELP_NOT_FOUND",
                Message: $"Help documentation for '{tool}' not found.",
                Suggestion: "Run the ToolDocsGen generator to create help files."));
        }

        var content = File.ReadAllText(helpPath);
        return Results.Text(content, "text/plain");
    }

    private static IResult GetToolSchema(
        string tool,
        IWebHostEnvironment env)
    {
        // Validate tool name (security: allowlist only)
        if (!AllowedTools.Contains(tool))
        {
            return Results.BadRequest(new ErrorInfo(
                Code: "INVALID_TOOL",
                Message: $"Tool '{tool}' is not recognized.",
                Suggestion: $"Valid tools: {string.Join(", ", AllowedTools)}"));
        }

        // Resolve schema file path via ContentRootPath
        var schemaPath = Path.Combine(
            env.ContentRootPath,
            "docs", "tools", tool.ToLowerInvariant(), "schema.effective.json");

        if (!File.Exists(schemaPath))
        {
            return Results.NotFound(new ErrorInfo(
                Code: "SCHEMA_NOT_FOUND",
                Message: $"Schema for '{tool}' not found.",
                Suggestion: "Run the ToolDocsGen generator to create schema files."));
        }

        var content = File.ReadAllText(schemaPath);
        return Results.Content(content, "application/json");
    }
}
```

### Endpoints Summary

| Endpoint | Method | Description | Response |
|----------|--------|-------------|----------|
| `/api/tools/{tool}/help` | GET | Raw help text | `text/plain` |
| `/api/tools/{tool}/schema` | GET | Effective schema JSON | `application/json` |

### Security Measures

1. **Tool name allowlist** — Only `retoc`, `uwpdumper`, `dllinjector`, `uasset` are valid.
2. **Path traversal protection** — Tool name is validated against allowlist before path construction; no user input reaches filesystem paths directly.
3. **Error mapping with ErrorInfo** — Consistent error responses using existing `ErrorInfo` pattern.

### Registration in Program.cs

```csharp
// In src/Aris.Hosting/Program.cs
// After existing endpoint registrations:
ToolDocsEndpoints.MapEndpoints(app);
```

### Copy Docs to Output (for Published Scenarios)

For `dotnet publish`, ensure `docs/tools/` is included. Add to `Aris.Hosting.csproj`:

```xml
<ItemGroup>
  <Content Include="..\..\docs\tools\**\*" CopyToOutputDirectory="PreserveNewest" LinkBase="docs\tools" />
</ItemGroup>
```

Note: During development (`dotnet run`), `ContentRootPath` points to the source directory, so no copy is needed.

---

## 6) Frontend Integration

### Migration Path

**Current state:**
- `frontend/src/api/retocClient.ts` calls `/api/retoc/schema` and `/api/retoc/help`
- `RetocCommandSchemaProvider` in `src/Aris.Adapters/Retoc/` manually defines the schema

**Target state:**
- Frontend calls `/api/tools/retoc/schema` and `/api/tools/retoc/help` (canonical endpoints)
- `RetocCommandSchemaProvider` is marked `[Obsolete]` but retained for fallback

### Frontend Changes

Update `frontend/src/api/retocClient.ts` to use the canonical endpoints:

```typescript
// Before:
const response = await fetch(`${baseUrl}/api/retoc/schema`, ...);
const helpResponse = await fetch(`${baseUrl}/api/retoc/help`, ...);

// After:
const response = await fetch(`${baseUrl}/api/tools/retoc/schema`, ...);
const helpResponse = await fetch(`${baseUrl}/api/tools/retoc/help`, ...);
```

Alternatively, create a generic `toolsClient.ts` for reuse across tools.

### Schema-Driven UI Requirements

The Retoc Advanced Mode UI must enforce the following behavioral requirements:

1. **Command selection** — Only commands present in `schema.commands[]` are available for selection.
2. **Required field enforcement** — Fields corresponding to positionals with `required: true` must be validated as non-empty before submission.
3. **Optional field handling** — Fields corresponding to positionals with `required: false` may be left empty.
4. **Option/flag restriction** — The UI must not allow users to select or emit options/flags that are not present in `schema.commands[].options[]` or `schema.globalOptions[]`.
5. **No invented flags** — If the schema has empty `options` and `globalOptions`, the UI must not offer any flags.

These requirements ensure the frontend stays aligned with the tool's actual capabilities and prevents drift.

### Deprecation of Hand-Maintained Provider

**`src/Aris.Adapters/Retoc/RetocCommandSchemaProvider.cs`:**

```csharp
[Obsolete("Use /api/tools/retoc/schema endpoint instead. This provider is retained for backward compatibility.")]
public static class RetocCommandSchemaProvider
{
    // ... existing implementation
}
```

The existing `/api/retoc/schema` endpoint in `RetocEndpoints.cs` can remain as a redirect or be removed after frontend migration is verified.

---

## 7) Testing Plan

### Schema Coverage Tests

**Location:** `tests/Aris.Core.Tests/ToolDocs/ToolSchemaCoverageTests.cs`

These tests validate alignment between the `RetocCommandType` enum and the generated schema without hardcoding positional names.

```csharp
using System.Text.Json;
using Aris.Core.Retoc;

namespace Aris.Core.Tests.ToolDocs;

public class ToolSchemaCoverageTests
{
    private readonly ToolSchema _schema;

    public ToolSchemaCoverageTests()
    {
        // Load effective schema from docs/tools/retoc/schema.effective.json
        var schemaPath = Path.Combine(
            GetRepoRoot(),
            "docs", "tools", "retoc", "schema.effective.json");

        var json = File.ReadAllText(schemaPath);
        _schema = JsonSerializer.Deserialize<ToolSchema>(json,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true })!;
    }

    [Fact]
    public void AllRetocCommandTypes_HaveSchemaEntry()
    {
        var enumNames = Enum.GetNames<RetocCommandType>()
            .Select(n => n.ToLowerInvariant())
            .ToHashSet();

        var schemaCommands = _schema.Commands
            .Select(c => c.Name.ToLowerInvariant())
            .ToHashSet();

        var missing = enumNames.Except(schemaCommands).ToList();

        Assert.Empty(missing);
    }

    [Fact]
    public void SchemaCommands_MapToValidRetocCommandType()
    {
        foreach (var cmd in _schema.Commands)
        {
            var parsed = Enum.TryParse<RetocCommandType>(cmd.Name, ignoreCase: true, out _);
            Assert.True(parsed, $"Schema command '{cmd.Name}' has no matching RetocCommandType");
        }
    }

    [Fact]
    public void EachCommand_HasAtLeastOneUsageLine()
    {
        foreach (var cmd in _schema.Commands)
        {
            Assert.NotEmpty(cmd.Usages);
        }
    }

    [Fact]
    public void RequiredPositionals_HaveValidTypeHints()
    {
        var validTypeHints = new HashSet<string> { "path", "integer", "string", "enum" };

        foreach (var cmd in _schema.Commands)
        {
            foreach (var pos in cmd.Positionals.Where(p => p.Required))
            {
                // TypeHint is optional, but if present must be valid
                if (!string.IsNullOrEmpty(pos.TypeHint))
                {
                    Assert.Contains(pos.TypeHint.ToLowerInvariant(), validTypeHints);
                }
            }
        }
    }

    [Fact]
    public void RequiredPositionalCount_IsRepresentableByDomainModel()
    {
        // RetocCommand has: InputPath, OutputPath, ChunkIndex, Version, AesKey
        // This means at most 5 distinct positional bindings are supported
        const int maxSupportedPositionals = 5;

        foreach (var cmd in _schema.Commands)
        {
            var requiredCount = cmd.Positionals.Count(p => p.Required);
            Assert.True(requiredCount <= maxSupportedPositionals,
                $"Command '{cmd.Name}' has {requiredCount} required positionals, " +
                $"but RetocCommand can only represent {maxSupportedPositionals}");
        }
    }

    [Fact]
    public void OptionalPositionals_AreMarkedCorrectly()
    {
        foreach (var cmd in _schema.Commands)
        {
            // Verify no positional has index gaps (they should be contiguous from 0)
            var indices = cmd.Positionals.Select(p => p.Index).OrderBy(i => i).ToList();
            for (int i = 0; i < indices.Count; i++)
            {
                Assert.Equal(i, indices[i]);
            }

            // Verify optional positionals come after required ones
            bool sawOptional = false;
            foreach (var pos in cmd.Positionals.OrderBy(p => p.Index))
            {
                if (!pos.Required)
                    sawOptional = true;
                else if (sawOptional)
                    Assert.Fail($"Command '{cmd.Name}' has required positional after optional one");
            }
        }
    }

    [Fact]
    public void PositionalTypeHints_AreConsistentWithDomainModelTypes()
    {
        // Validate that type hints align with what RetocCommand can represent
        var pathTypeHint = new HashSet<string> { "path" };
        var integerTypeHint = new HashSet<string> { "integer" };

        foreach (var cmd in _schema.Commands)
        {
            foreach (var pos in cmd.Positionals)
            {
                if (string.IsNullOrEmpty(pos.TypeHint)) continue;

                // If typeHint is "path", domain model should have a string/path property
                // If typeHint is "integer", domain model should have an int property
                // This is a structural validation, not name-based
                var hint = pos.TypeHint.ToLowerInvariant();
                Assert.True(
                    pathTypeHint.Contains(hint) || integerTypeHint.Contains(hint) || hint == "string" || hint == "enum",
                    $"Positional '{pos.Name}' in command '{cmd.Name}' has unrecognized typeHint '{hint}'");
            }
        }
    }

    private static string GetRepoRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir != null && !File.Exists(Path.Combine(dir.FullName, "ARIS.sln")))
        {
            dir = dir.Parent;
        }
        return dir?.FullName ?? throw new InvalidOperationException("Could not find repo root");
    }
}
```

### Generator Tests (Optional)

**Location:** `tests/Aris.ToolDocsGen.Tests/HelpParserTests.cs`

```csharp
public class HelpParserTests
{
    [Theory]
    [InlineData("tool cmd <A> <B> [C]", 2, 1)]  // 2 required, 1 optional
    [InlineData("tool cmd <A>", 1, 0)]           // 1 required, 0 optional
    [InlineData("tool cmd [A] [B]", 0, 2)]       // 0 required, 2 optional
    [InlineData("tool cmd", 0, 0)]               // 0 positionals
    public void ParseUsageLine_ExtractsCorrectPositionalCounts(
        string usage,
        int expectedRequired,
        int expectedOptional)
    {
        var parser = new UsageLineParser();
        var positionals = parser.Parse(usage);

        var requiredCount = positionals.Count(p => p.Required);
        var optionalCount = positionals.Count(p => !p.Required);

        Assert.Equal(expectedRequired, requiredCount);
        Assert.Equal(expectedOptional, optionalCount);
    }

    [Theory]
    [InlineData("<INPUT_PATH>", "path")]
    [InlineData("<FILE>", "path")]
    [InlineData("<OUTPUT_DIR>", "path")]
    [InlineData("<INDEX>", "integer")]
    [InlineData("<CHUNK_ID>", "integer")]
    [InlineData("<COUNT>", "integer")]
    [InlineData("<NAME>", "string")]
    [InlineData("<VALUE>", "string")]
    public void InferTypeHint_ReturnsCorrectHint(string argName, string expectedHint)
    {
        var parser = new UsageLineParser();
        var hint = parser.InferTypeHint(argName.Trim('<', '>', '[', ']'));

        Assert.Equal(expectedHint, hint);
    }

    [Fact]
    public void ParseUsageLine_PositionalsHaveContiguousIndices()
    {
        var parser = new UsageLineParser();
        var positionals = parser.Parse("tool cmd <A> <B> [C] [D]");

        for (int i = 0; i < positionals.Count; i++)
        {
            Assert.Equal(i, positionals[i].Index);
        }
    }

    [Fact]
    public void ParseUsageLine_OptionalPositionalsAfterRequired()
    {
        var parser = new UsageLineParser();
        var positionals = parser.Parse("tool cmd <REQ1> <REQ2> [OPT1]");

        Assert.True(positionals[0].Required);
        Assert.True(positionals[1].Required);
        Assert.False(positionals[2].Required);
    }
}
```

### Determinism Verification

Add a test that re-runs the generator and asserts no diff:

```csharp
[Fact]
[Trait("Category", "Integration")]
public void Generator_ProducesDeterministicOutput()
{
    // Run generator twice, compare outputs byte-for-byte
    var output1 = RunGenerator("retoc");
    var output2 = RunGenerator("retoc");

    Assert.Equal(output1.SchemaJson, output2.SchemaJson);
    Assert.Equal(output1.ManifestJson, output2.ManifestJson);
    Assert.Equal(output1.HelpText, output2.HelpText);
}
```

---

## 8) Verification Checklist

### Build & Test Commands

```powershell
# From repo root

# 1. Build entire solution (includes new generator project)
dotnet build

# 2. Run all tests
dotnet test

# 3. Build generator specifically
dotnet build tools/Aris.ToolDocsGen/Aris.ToolDocsGen.csproj

# 4. Run generator for retoc (requires tools to be extracted first)
# If tools not extracted, run ARIS once: dotnet run --project src/Aris.Hosting
dotnet run --project tools/Aris.ToolDocsGen -- generate --tool retoc --out docs/tools

# 5. Verify outputs exist
Test-Path docs/tools/retoc/help.txt
Test-Path docs/tools/retoc/schema.effective.json
Test-Path docs/tools/retoc/manifest.json

# 6. Build and run backend
dotnet run --project src/Aris.Hosting

# 7. Frontend build
cd frontend
npm install
npm run build
npm run dev
```

### Manual Verification Steps

1. **Generator output:**
   - [ ] `docs/tools/retoc/help.txt` contains retoc help output
   - [ ] `docs/tools/retoc/commands/get.txt` contains `get` command help
   - [ ] `docs/tools/retoc/schema.effective.json` is valid JSON

2. **Schema correctness:**
   - [ ] Schema only contains options/flags present in captured help output
   - [ ] No invented options appear in schema (e.g., no flags that aren't in help)
   - [ ] All `RetocCommandType` enum values have schema entries

3. **Generator error handling:**
   - [ ] Generator fails with clear error if tool binary not found
   - [ ] Error message includes expected path and remediation steps

4. **Backend endpoints:**
   - [ ] `GET /api/tools/retoc/help` returns plain text help
   - [ ] `GET /api/tools/retoc/schema` returns JSON schema
   - [ ] `GET /api/tools/invalid/help` returns 400 with ErrorInfo

5. **Frontend integration:**
   - [ ] Retoc Advanced Mode loads schema from `/api/tools/retoc/schema`
   - [ ] Command builder only shows options from schema
   - [ ] Invalid flags cannot be selected/emitted

6. **Tests pass:**
   - [ ] `dotnet test` completes with no failures
   - [ ] Coverage test confirms all RetocCommandType values are in schema

---

## Appendix: File References

### Existing Classes Used

| Class | Location | Usage |
|-------|----------|-------|
| `ToolManifestLoader` | `src/Aris.Tools/Manifest/ToolManifestLoader.cs` | Load manifest from embedded resource |
| `ToolManifest` | `src/Aris.Tools/Manifest/ToolManifest.cs` | Manifest model |
| `ToolEntry` | `src/Aris.Tools/Manifest/ToolEntry.cs` | Tool entry with id, version, sha256, relativePath |
| `RetocCommandType` | `src/Aris.Core/Retoc/RetocCommandType.cs` | Enum for coverage tests |
| `RetocCommand` | `src/Aris.Core/Retoc/RetocCommand.cs` | Model for field mapping validation |
| `ErrorInfo` | `src/Aris.Contracts/Shared/ErrorInfo.cs` | Error response format |

### Classes NOT Used by Generator

| Class | Location | Reason |
|-------|----------|--------|
| `DependencyValidator` | `src/Aris.Infrastructure/Tools/DependencyValidator.cs` | Generator does not validate hashes |
| `DependencyExtractor` | `src/Aris.Infrastructure/Tools/DependencyExtractor.cs` | Generator does not extract tools |

### New Contract Types

```csharp
// src/Aris.Contracts/Tools/ToolSchemaResponse.cs
namespace Aris.Contracts.Tools;

public sealed class ToolSchemaResponse
{
    public required string Tool { get; init; }
    public string? Version { get; init; }
    public required DateTimeOffset GeneratedAtUtc { get; init; }
    public required ToolCommandDefinition[] Commands { get; init; }
    public ToolOptionDefinition[]? GlobalOptions { get; init; }
}

public sealed class ToolCommandDefinition
{
    public required string Name { get; init; }
    public string? Summary { get; init; }
    public required string[] Usages { get; init; }
    public required ToolPositionalDefinition[] Positionals { get; init; }
    public ToolOptionDefinition[]? Options { get; init; }
}

public sealed class ToolPositionalDefinition
{
    public required string Name { get; init; }
    public required int Index { get; init; }
    public required bool Required { get; init; }
    public string? TypeHint { get; init; }
    public string? Description { get; init; }
}

public sealed class ToolOptionDefinition
{
    public required string Name { get; init; }
    public string? ShortName { get; init; }
    public string? Description { get; init; }
    public bool TakesValue { get; init; }
    public string? ValueHint { get; init; }
}
```
