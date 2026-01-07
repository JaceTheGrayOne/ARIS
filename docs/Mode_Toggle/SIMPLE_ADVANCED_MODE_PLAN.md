# Simple | Advanced Mode Feature Implementation Plan

## 1. Scope

### Goals

**Simple Mode:**
- Provide two guided workflows: `to-legacy` (Unpack: Zen → Legacy) and `to-zen` (Pack: Legacy → Zen)
- Minimal input fields: engine version, input path, output path
- Clear labels and UX optimized for common modding workflows
- Live command preview showing exact Retoc invocation
- Streaming stdout/stderr to console log via NDJSON over fetch ReadableStream

**Advanced Mode:**
- Expose 100% of supported Retoc functionality via structured UI controls (no free-form additionalArgs field)
- Command selector for all 13 `RetocCommandType` values (Manifest, Info, List, Verify, Unpack, UnpackRaw, PackRaw, ToLegacy, ToZen, Get, DumpTest, GenScriptObjects, PrintScriptObjects)
- Schema-driven dynamic field rendering based on selected command
- All options modeled as explicit fields:
  - Required parameters (e.g., Get chunk index) as Integer fields with validation
  - Allowlisted flags from `RetocOptions.AllowedAdditionalArgs` as boolean toggles
  - Global options (AES key, container header version, TOC version) as structured fields
- Help modal with Markdown-rendered Retoc documentation
- Live command preview (same as Simple Mode)
- Streaming stdout/stderr to console log (same as Simple Mode)

**Both Modes:**
- Single source of truth: backend builds command preview to guarantee preview == execution
- Streaming live output via NDJSON over fetch ReadableStream (not EventSource/SSE)
- Reuse existing adapter/builder/infrastructure patterns
- Follow existing error handling (`ArisException` → `ErrorInfo` + HTTP status codes)
- Respect existing limits: ProcessRunner's 10 MB per stream, 100k lines max

### Non-Goals

- Do NOT replace the existing `/api/retoc/convert` endpoint (keep for backward compatibility)
- Do NOT add new external dependencies or heavy frameworks
- Do NOT invent new Retoc commands not present in `RetocCommandType` enum
- Do NOT implement custom Retoc CLI wrappers outside the existing adapter pattern
- Do NOT expose free-form `additionalArgs` text field in UI (all options must be structured)

### User Stories

1. **Simple Mode - Pack:**
   - As a modder, I want to quickly pack modified UAsset files into an IoStore container by selecting a folder, choosing UE version, and clicking "Build Mod"
   - I want to see the exact command that will be executed before running it
   - I want to watch live output as Retoc processes my files

2. **Simple Mode - Unpack:**
   - As a modder, I want to extract IoStore containers to editable UAsset files by selecting the Paks directory and output folder
   - I want to see the exact command preview and live output

3. **Advanced Mode - Full Access:**
   - As an advanced user, I want to use all Retoc commands through a structured UI without memorizing CLI syntax
   - I want to see what options each command supports (required/optional fields, types, constraints)
   - I want inline help documentation to understand command behavior
   - I want the same command preview and streaming output

---

## 2. Delta List (File-by-File)

### Backend - Contracts (`src/Aris.Contracts/Retoc/`)

- **CREATE** `RetocBuildCommandRequest.cs`: Request DTO for building/previewing Retoc command
- **CREATE** `RetocBuildCommandResponse.cs`: Response DTO containing `commandLine`, `executablePath`, `arguments`
- **CREATE** `RetocStreamRequest.cs`: Request DTO for streaming execution (identical structure to `RetocBuildCommandRequest`)
- **CREATE** `RetocCommandSchemaResponse.cs`: Response DTO containing schema of all supported commands/options
- **CREATE** `RetocCommandDefinition.cs`: Schema model for individual command metadata
- **CREATE** `RetocCommandFieldDefinition.cs`: Schema model for command-specific field metadata
- **CREATE** `RetocHelpResponse.cs`: Response DTO containing Markdown help content
- **CREATE** `RetocStreamLineEvent.cs`: NDJSON event DTO for streaming output lines
- **CREATE** `RetocStreamStatusEvent.cs`: NDJSON event DTO for status changes
- **CREATE** `RetocStreamErrorEvent.cs`: NDJSON event DTO for errors

### Backend - Core (`src/Aris.Core/Retoc/`)

- **NO CHANGES** to `RetocCommand.cs` (already supports all `RetocCommandType` values and fields)
- **NO CHANGES** to `RetocCommandType.cs` (already contains all 13 command types)

### Backend - Adapters (`src/Aris.Adapters/Retoc/`)

- **MODIFY** `IRetocAdapter.cs`: Add `BuildCommand(RetocCommand command)` method for preview-only builds
- **MODIFY** `RetocAdapter.cs`: Implement `BuildCommand` method (delegates to `RetocCommandBuilder.Build`)
- **CREATE** `RetocCommandSchemaProvider.cs`: Provides schema metadata for all `RetocCommandType` commands and their fields (maps to existing RetocCommand domain model properties)
- **NO CHANGES** to `RetocCommandBuilder.cs` (already supports all commands via `GetSubcommand` and `AppendSubcommandArgs`)

### Backend - Infrastructure (`src/Aris.Infrastructure/Process/`)

- **CREATE** `StreamingProcessRunner.cs`: Line-by-line streaming process executor with callback-based event delivery
- **CREATE** `IStreamingProcessRunner.cs`: Interface for streaming process execution
- **MODIFY** `DependencyInjection.cs` (in Infrastructure): Register `IStreamingProcessRunner` as singleton

### Backend - Configuration (`src/Aris.Infrastructure/Configuration/`)

- **MODIFY** `RetocOptions.cs`: Add `MaxStreamingOutputBytes` and `MaxStreamingOutputLines` (defaults aligned with ProcessRunner: 10 MB, 100k lines)

### Backend - Hosting (`src/Aris.Hosting/Endpoints/`)

- **MODIFY** `RetocEndpoints.cs`: Add four new endpoints:
  - `POST /api/retoc/build` - Build command preview
  - `GET /api/retoc/schema` - Get command schema
  - `GET /api/retoc/help` - Get Retoc help documentation
  - `POST /api/retoc/stream` - Execute command with NDJSON streaming over response body

### Frontend - Contracts (`frontend/src/types/contracts.ts`)

- **MODIFY** `contracts.ts`: Add TypeScript interfaces for:
  - `RetocBuildCommandRequest`
  - `RetocBuildCommandResponse`
  - `RetocStreamRequest`
  - `RetocCommandSchemaResponse`
  - `RetocCommandDefinition`
  - `RetocCommandFieldDefinition`
  - `RetocHelpResponse`
  - `RetocStreamEvent` (union type for NDJSON events)
  - `RetocStreamLineEvent`
  - `RetocStreamStatusEvent`
  - `RetocStreamErrorEvent`

### Frontend - API Client (`frontend/src/api/`)

- **CREATE** `retocClient.ts`: API client functions:
  - `buildRetocCommand(request: RetocBuildCommandRequest): Promise<RetocBuildCommandResponse>`
  - `getRetocSchema(): Promise<RetocCommandSchemaResponse>`
  - `getRetocHelp(): Promise<RetocHelpResponse>`
  - `streamRetocExecution(request: RetocStreamRequest, onEvent: (event: RetocStreamEvent) => void, signal?: AbortSignal): Promise<void>`

### Frontend - Components (`frontend/src/components/retoc/`)

- **MODIFY** `RetocResultPanel.tsx`: Update to handle streaming events and display live console log
- **CREATE** `RetocCommandPreview.tsx`: Displays `commandLine` string in monospace font with copy button
- **CREATE** `RetocConsoleLog.tsx`: Live streaming console with stdout/stderr color-coding and auto-scroll
- **CREATE** `RetocAdvancedCommandBuilder.tsx`: Schema-driven dynamic form for building commands
- **CREATE** `RetocHelpModal.tsx`: Modal rendering Markdown help content

### Frontend - Pages (`frontend/src/pages/tools/`)

- **MODIFY** `RetocPage.tsx`: Complete rewrite to support:
  - Mode toggle (Simple | Advanced)
  - Simple Mode: Two sections (Pack/Unpack) with guided forms
  - Advanced Mode: Command builder + help button
  - Shared command preview panel
  - Shared console log panel
  - Execution state management (idle, building, running, completed, error)
  - Abort controller for canceling streaming requests

### Tests - Backend (`tests/Aris.Core.Tests/`)

- **MODIFY** `Adapters/RetocAdapterTests.cs`: Add tests for:
  - `BuildCommand_ToLegacy_GeneratesCorrectCommandLine`
  - `BuildCommand_ToZen_GeneratesCorrectCommandLine`
  - `BuildCommand_AllCommandTypes_GenerateCorrectSubcommand` (test all 13 command types)
- **CREATE** `Retoc/RetocCommandSchemaProviderTests.cs`: Test schema provider accuracy and completeness:
  - Verify all 13 `RetocCommandType` enum values have schema definitions
  - Verify schema fields map to actual `RetocCommand` properties
  - Verify allowlisted args from `RetocOptions.AllowedAdditionalArgs` are represented as schema fields
- **CREATE** `Infrastructure/StreamingProcessRunnerTests.cs`: Test streaming behavior:
  - Line-by-line delivery using PowerShell deterministic scripts
  - Timeout enforcement
  - Cancellation handling
  - Bounded memory (no unbounded buffering)

### Tests - Frontend (if framework exists)

- **CREATE** `components/retoc/RetocPage.test.tsx`: Test mode toggle, form validation, preview updates
- **CREATE** `api/retocClient.test.ts`: Test API client functions, NDJSON parsing, abort signal handling

---

## 3. Contracts

### C# DTOs

#### RetocBuildCommandRequest.cs

```csharp
namespace Aris.Contracts.Retoc;

/// <summary>
/// Request to build/preview a Retoc command without executing it.
/// All options are explicit structured fields - no free-form additionalArgs.
/// </summary>
public sealed record RetocBuildCommandRequest(
    /// <summary>
    /// Retoc command type (e.g., "ToLegacy", "ToZen", "Info", "List").
    /// Maps to RetocCommandType enum.
    /// </summary>
    string CommandType,

    /// <summary>
    /// Absolute input path (file or directory depending on command).
    /// </summary>
    string InputPath,

    /// <summary>
    /// Absolute output path (file or directory depending on command).
    /// Optional for commands that don't produce output (e.g., Info, List).
    /// </summary>
    string? OutputPath,

    /// <summary>
    /// Engine version for to-zen (e.g., "UE5_6").
    /// Required for ToZen command.
    /// </summary>
    string? EngineVersion,

    /// <summary>
    /// AES encryption key (hex string).
    /// Optional global option.
    /// </summary>
    string? AesKey,

    /// <summary>
    /// Container header version override (enum value from RetocContainerHeaderVersion).
    /// Optional global option.
    /// </summary>
    string? ContainerHeaderVersion,

    /// <summary>
    /// TOC version override (enum value from RetocTocVersion).
    /// Optional global option.
    /// </summary>
    string? TocVersion,

    /// <summary>
    /// Chunk index for Get command (required for Get, ignored for others).
    /// </summary>
    int? ChunkIndex,

    /// <summary>
    /// Enable verbose output (maps to --verbose if allowlisted in RetocOptions).
    /// </summary>
    bool? Verbose,

    /// <summary>
    /// Disable warnings (maps to --no-warnings if allowlisted in RetocOptions).
    /// </summary>
    bool? NoWarnings,

    /// <summary>
    /// Timeout in seconds (optional, defaults to RetocOptions.DefaultTimeoutSeconds).
    /// </summary>
    int? TimeoutSeconds
);
```

**Note on AllowedAdditionalArgs:** The `RetocOptions.AllowedAdditionalArgs` list is configurable. The schema provider will dynamically expose allowlisted flags as boolean fields in the schema. If the allowlist is empty (default), these boolean fields are omitted from the schema. Tests use `["--verbose", "--no-warnings"]` as examples.

#### RetocBuildCommandResponse.cs

```csharp
namespace Aris.Contracts.Retoc;

/// <summary>
/// Response containing built Retoc command for preview.
/// </summary>
public sealed record RetocBuildCommandResponse(
    /// <summary>
    /// Full path to retoc.exe (resolved from tool manifest).
    /// </summary>
    string ExecutablePath,

    /// <summary>
    /// Command-line arguments string.
    /// </summary>
    string Arguments,

    /// <summary>
    /// Human-readable command line (for UI display).
    /// Format: "<executable>" <arguments>
    /// </summary>
    string CommandLine
);
```

#### RetocStreamRequest.cs

```csharp
namespace Aris.Contracts.Retoc;

/// <summary>
/// Request to execute a Retoc command with streaming output.
/// Identical structure to RetocBuildCommandRequest.
/// </summary>
public sealed record RetocStreamRequest(
    string CommandType,
    string InputPath,
    string? OutputPath,
    string? EngineVersion,
    string? AesKey,
    string? ContainerHeaderVersion,
    string? TocVersion,
    int? ChunkIndex,
    bool? Verbose,
    bool? NoWarnings,
    int? TimeoutSeconds
);
```

#### RetocCommandSchemaResponse.cs

```csharp
namespace Aris.Contracts.Retoc;

/// <summary>
/// Schema describing all supported Retoc commands and their options.
/// </summary>
public sealed record RetocCommandSchemaResponse(
    /// <summary>
    /// List of all supported commands (all 13 RetocCommandType enum values).
    /// </summary>
    List<RetocCommandDefinition> Commands,

    /// <summary>
    /// Global options available for all commands (AES key, version overrides).
    /// </summary>
    List<RetocCommandFieldDefinition> GlobalOptions,

    /// <summary>
    /// Allowlisted flags from RetocOptions.AllowedAdditionalArgs exposed as boolean fields.
    /// </summary>
    List<RetocCommandFieldDefinition> AllowlistedFlags
);

public sealed record RetocCommandDefinition(
    /// <summary>
    /// Command type identifier (e.g., "ToLegacy", "ToZen").
    /// Maps to RetocCommandType enum value.
    /// </summary>
    string CommandType,

    /// <summary>
    /// Display name for UI (e.g., "To Legacy (Zen → PAK)").
    /// </summary>
    string DisplayName,

    /// <summary>
    /// Command description.
    /// </summary>
    string Description,

    /// <summary>
    /// Required fields for this command.
    /// </summary>
    List<RetocCommandFieldDefinition> RequiredFields,

    /// <summary>
    /// Optional fields for this command.
    /// </summary>
    List<RetocCommandFieldDefinition> OptionalFields
);

public sealed record RetocCommandFieldDefinition(
    /// <summary>
    /// Field identifier matching RetocCommand property name (e.g., "InputPath", "EngineVersion", "ChunkIndex").
    /// </summary>
    string FieldName,

    /// <summary>
    /// Display label for UI.
    /// </summary>
    string Label,

    /// <summary>
    /// Field type (e.g., "Path", "Enum", "String", "Integer", "Boolean").
    /// </summary>
    string FieldType,

    /// <summary>
    /// Help text for this field.
    /// </summary>
    string? HelpText,

    /// <summary>
    /// For Enum fields: allowed values (e.g., ["UE5_6", "UE5_5", ...]).
    /// </summary>
    List<string>? EnumValues,

    /// <summary>
    /// For Integer fields: minimum value.
    /// </summary>
    int? MinValue,

    /// <summary>
    /// For Integer fields: maximum value.
    /// </summary>
    int? MaxValue
);
```

#### RetocHelpResponse.cs

```csharp
namespace Aris.Contracts.Retoc;

/// <summary>
/// Response containing Retoc help documentation.
/// </summary>
public sealed record RetocHelpResponse(
    /// <summary>
    /// Markdown-formatted help content.
    /// </summary>
    string Markdown
);
```

#### Streaming Event DTOs

```csharp
namespace Aris.Contracts.Retoc;

/// <summary>
/// Base type for NDJSON streaming events.
/// Discriminated by EventType field.
/// </summary>
public abstract record RetocStreamEvent(string EventType);

/// <summary>
/// Output line event (stdout or stderr).
/// </summary>
public sealed record RetocStreamLineEvent(
    /// <summary>
    /// Stream source ("stdout" or "stderr").
    /// </summary>
    string Stream,

    /// <summary>
    /// Line text.
    /// </summary>
    string Text,

    /// <summary>
    /// Timestamp (ISO 8601).
    /// </summary>
    string Timestamp
) : RetocStreamEvent("line");

/// <summary>
/// Status change event (started, completed, failed).
/// </summary>
public sealed record RetocStreamStatusEvent(
    /// <summary>
    /// Status ("started", "completed", "failed").
    /// </summary>
    string Status,

    /// <summary>
    /// Exit code (for completed/failed).
    /// </summary>
    int? ExitCode,

    /// <summary>
    /// Duration (ISO 8601 duration string, for completed/failed).
    /// </summary>
    string? Duration
) : RetocStreamEvent("status");

/// <summary>
/// Error event (validation/dependency error before streaming starts).
/// </summary>
public sealed record RetocStreamErrorEvent(
    /// <summary>
    /// Error information.
    /// </summary>
    ErrorInfo Error
) : RetocStreamEvent("error");
```

### TypeScript Interfaces (frontend/src/types/contracts.ts)

```typescript
export interface RetocBuildCommandRequest {
  commandType: string;
  inputPath: string;
  outputPath?: string | null;
  engineVersion?: string | null;
  aesKey?: string | null;
  containerHeaderVersion?: string | null;
  tocVersion?: string | null;
  chunkIndex?: number | null;
  verbose?: boolean | null;
  noWarnings?: boolean | null;
  timeoutSeconds?: number | null;
}

export interface RetocBuildCommandResponse {
  executablePath: string;
  arguments: string;
  commandLine: string;
}

export interface RetocStreamRequest {
  commandType: string;
  inputPath: string;
  outputPath?: string | null;
  engineVersion?: string | null;
  aesKey?: string | null;
  containerHeaderVersion?: string | null;
  tocVersion?: string | null;
  chunkIndex?: number | null;
  verbose?: boolean | null;
  noWarnings?: boolean | null;
  timeoutSeconds?: number | null;
}

export interface RetocCommandSchemaResponse {
  commands: RetocCommandDefinition[];
  globalOptions: RetocCommandFieldDefinition[];
  allowlistedFlags: RetocCommandFieldDefinition[];
}

export interface RetocCommandDefinition {
  commandType: string;
  displayName: string;
  description: string;
  requiredFields: RetocCommandFieldDefinition[];
  optionalFields: RetocCommandFieldDefinition[];
}

export interface RetocCommandFieldDefinition {
  fieldName: string;
  label: string;
  fieldType: string;
  helpText?: string | null;
  enumValues?: string[] | null;
  minValue?: number | null;
  maxValue?: number | null;
}

export interface RetocHelpResponse {
  markdown: string;
}

// Streaming event types
export type RetocStreamEvent =
  | RetocStreamLineEvent
  | RetocStreamStatusEvent
  | RetocStreamErrorEvent;

export interface RetocStreamLineEvent {
  eventType: 'line';
  stream: 'stdout' | 'stderr';
  text: string;
  timestamp: string;
}

export interface RetocStreamStatusEvent {
  eventType: 'status';
  status: 'started' | 'completed' | 'failed';
  exitCode?: number;
  duration?: string;
}

export interface RetocStreamErrorEvent {
  eventType: 'error';
  error: ErrorInfo;
}
```

### NDJSON Wire Format

The `/api/retoc/stream` endpoint responds with `Content-Type: application/x-ndjson` (newline-delimited JSON). Each event is a single JSON object followed by `\n`.

**Example stream:**

```
{"eventType":"status","status":"started"}
{"eventType":"line","stream":"stdout","text":"Processing file 1 of 100...","timestamp":"2025-12-24T10:30:45.123Z"}
{"eventType":"line","stream":"stdout","text":"Processing file 2 of 100...","timestamp":"2025-12-24T10:30:45.456Z"}
{"eventType":"line","stream":"stderr","text":"Warning: Missing metadata","timestamp":"2025-12-24T10:30:46.789Z"}
{"eventType":"status","status":"completed","exitCode":0,"duration":"00:00:15.1234567"}
```

**Error before streaming starts:**

```
{"eventType":"error","error":{"code":"VALIDATION_ERROR","message":"InputPath must be absolute","remediationHint":"Provide a full path"}}
```

**Failure during execution:**

```
{"eventType":"status","status":"started"}
{"eventType":"line","stream":"stderr","text":"Error: Invalid file format","timestamp":"2025-12-24T10:30:47.000Z"}
{"eventType":"status","status":"failed","exitCode":1,"duration":"00:00:02.5000000"}
```

---

## 4. Endpoint Mapping

### New Endpoints

#### `POST /api/retoc/build`

**Purpose:** Build and preview a Retoc command without executing it.

**Request:** `RetocBuildCommandRequest`

**Success Response:** `200 OK` with `RetocBuildCommandResponse`

**Error Responses:**
- `400 Bad Request` - Validation error (e.g., invalid command type, missing required fields, non-absolute path)
  - Maps from: `ValidationError` (thrown by `RetocCommandBuilder.Build`)
  - Error body: `ErrorInfo` with `code: "VALIDATION_ERROR"`
- `503 Service Unavailable` - Retoc binary not found or invalid
  - Maps from: `DependencyMissingError` (thrown by `RetocAdapter` if tool manifest entry missing)
  - Error body: `ErrorInfo` with `code: "DEPENDENCY_MISSING"`
- `500 Internal Server Error` - Unexpected errors
  - Maps from: Generic exceptions
  - Error body: `ErrorInfo` with `code: "UNEXPECTED_ERROR"`

**Implementation Pattern:**
```csharp
group.MapPost("/build", (
    RetocBuildCommandRequest request,
    IRetocAdapter adapter) =>
{
    try
    {
        var command = MapRequestToRetocCommand(request);
        var (exePath, args) = adapter.BuildCommand(command);

        return Results.Ok(new RetocBuildCommandResponse(
            ExecutablePath: exePath,
            Arguments: args,
            CommandLine: $"\"{exePath}\" {args}"
        ));
    }
    catch (ValidationError ex)
    {
        var error = new ErrorInfo(ex.ErrorCode, ex.Message, ex.RemediationHint);
        return Results.Json(error, statusCode: StatusCodes.Status400BadRequest);
    }
    catch (DependencyMissingError ex)
    {
        var error = new ErrorInfo(ex.ErrorCode, ex.Message, ex.RemediationHint);
        return Results.Json(error, statusCode: StatusCodes.Status503ServiceUnavailable);
    }
    catch (Exception ex)
    {
        var error = new ErrorInfo("UNEXPECTED_ERROR", ex.Message, "Check logs");
        return Results.Json(error, statusCode: StatusCodes.Status500InternalServerError);
    }
});
```

**Helper: MapRequestToRetocCommand**
```csharp
private static RetocCommand MapRequestToRetocCommand(RetocBuildCommandRequest request)
{
    if (!Enum.TryParse<RetocCommandType>(request.CommandType, ignoreCase: true, out var commandType))
    {
        throw new ValidationError($"Invalid command type: {request.CommandType}", nameof(request.CommandType));
    }

    var additionalArgs = new List<string>();
    if (request.Verbose == true) additionalArgs.Add("--verbose");
    if (request.NoWarnings == true) additionalArgs.Add("--no-warnings");

    RetocContainerHeaderVersion? containerHeaderVersion = null;
    if (!string.IsNullOrEmpty(request.ContainerHeaderVersion))
    {
        if (!Enum.TryParse<RetocContainerHeaderVersion>(request.ContainerHeaderVersion, out var v))
        {
            throw new ValidationError($"Invalid ContainerHeaderVersion: {request.ContainerHeaderVersion}");
        }
        containerHeaderVersion = v;
    }

    RetocTocVersion? tocVersion = null;
    if (!string.IsNullOrEmpty(request.TocVersion))
    {
        if (!Enum.TryParse<RetocTocVersion>(request.TocVersion, out var v))
        {
            throw new ValidationError($"Invalid TocVersion: {request.TocVersion}");
        }
        tocVersion = v;
    }

    // For Get command, chunk index is required - validate if needed
    if (commandType == RetocCommandType.Get && request.ChunkIndex == null)
    {
        throw new ValidationError("ChunkIndex is required for Get command", nameof(request.ChunkIndex));
    }

    // Add chunk index to additional args for Get command
    if (commandType == RetocCommandType.Get && request.ChunkIndex.HasValue)
    {
        additionalArgs.Add(request.ChunkIndex.Value.ToString());
    }

    return new RetocCommand
    {
        OperationId = Guid.NewGuid().ToString("N"),
        CommandType = commandType,
        InputPath = request.InputPath,
        OutputPath = request.OutputPath ?? string.Empty,
        Version = request.EngineVersion,
        AesKey = request.AesKey,
        ContainerHeaderVersion = containerHeaderVersion,
        TocVersion = tocVersion,
        AdditionalArgs = additionalArgs,
        TimeoutSeconds = request.TimeoutSeconds
    };
}
```

---

#### `GET /api/retoc/schema`

**Purpose:** Return schema of all supported Retoc commands and their fields.

**Request:** None

**Success Response:** `200 OK` with `RetocCommandSchemaResponse`

**Error Responses:**
- `500 Internal Server Error` - Schema provider failure
  - Error body: `ErrorInfo` with `code: "SCHEMA_ERROR"`

**Implementation:**
```csharp
group.MapGet("/schema", (IOptions<RetocOptions> options) =>
{
    try
    {
        var schema = RetocCommandSchemaProvider.GetSchema(options.Value);
        return Results.Ok(schema);
    }
    catch (Exception ex)
    {
        var error = new ErrorInfo("SCHEMA_ERROR", "Failed to generate schema", ex.Message);
        return Results.Json(error, statusCode: StatusCodes.Status500InternalServerError);
    }
});
```

**Note:** Schema is static per configuration (depends only on `RetocOptions.AllowedAdditionalArgs`), so it can be cached on frontend.

---

#### `GET /api/retoc/help`

**Purpose:** Return Retoc help documentation as Markdown.

**Request:** None

**Success Response:** `200 OK` with `RetocHelpResponse`

**Error Responses:**
- `503 Service Unavailable` - Retoc binary not found
  - Maps from: `DependencyMissingError`
  - Error body: `ErrorInfo` with `code: "DEPENDENCY_MISSING"`
- `500 Internal Server Error` - Help execution failed
  - Error body: `ErrorInfo` with `code: "TOOL_EXECUTION_ERROR"`

**Implementation (using adapter's tool resolution):**
```csharp
group.MapGet("/help", async (
    IRetocAdapter adapter,
    IProcessRunner processRunner,
    CancellationToken cancellationToken) =>
{
    try
    {
        // Validate tool availability using adapter pattern
        var isValid = await adapter.ValidateAsync(cancellationToken);
        if (!isValid)
        {
            throw new DependencyMissingError("retoc", "Retoc binary not found or invalid");
        }

        // Get retoc.exe path using same pattern as RetocAdapter constructor
        // (load from manifest, resolve path)
        var manifest = ToolManifestLoader.Load();
        var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        var toolsRoot = Path.Combine(localAppData, "ARIS", "tools", manifest.Version);
        var retocEntry = manifest.Tools.FirstOrDefault(t => t.Id == "retoc")
            ?? throw new DependencyMissingError("retoc", "Retoc entry not found in tool manifest");
        var retocExePath = Path.Combine(toolsRoot, retocEntry.RelativePath);

        var result = await processRunner.ExecuteAsync(
            retocExePath,
            "--help",
            workingDirectory: null,
            timeoutSeconds: 5,
            environmentVariables: null,
            cancellationToken
        );

        if (result.ExitCode != 0)
        {
            throw new ToolExecutionError("retoc", result.ExitCode, "Failed to retrieve help");
        }

        // Wrap in Markdown code fence
        var markdown = $"```\n{result.StdOut}\n```";

        return Results.Ok(new RetocHelpResponse(markdown));
    }
    catch (DependencyMissingError ex)
    {
        var error = new ErrorInfo(ex.ErrorCode, ex.Message, ex.RemediationHint);
        return Results.Json(error, statusCode: StatusCodes.Status503ServiceUnavailable);
    }
    catch (ToolExecutionError ex)
    {
        var error = new ErrorInfo(ex.ErrorCode, ex.Message, ex.RemediationHint);
        return Results.Json(error, statusCode: StatusCodes.Status500InternalServerError);
    }
});
```

**Note:** This reuses the exact tool resolution pattern from `RetocAdapter.cs` lines 36-43. Do NOT introduce a parallel helper that bypasses manifest loading.

---

#### `POST /api/retoc/stream`

**Purpose:** Execute a Retoc command with NDJSON streaming of stdout/stderr over response body.

**Request:** `RetocStreamRequest`

**Success Response:** `200 OK` with `Content-Type: application/x-ndjson`

**Response Body:** NDJSON stream of `RetocStreamEvent` objects (one JSON object per line)

**Event Flow:**
1. Validate request → if error, write `RetocStreamErrorEvent` NDJSON and close stream
2. Build command → if error, write `RetocStreamErrorEvent` NDJSON and close stream
3. Start process → write `RetocStreamStatusEvent` with `status: "started"`
4. For each stdout/stderr line → write `RetocStreamLineEvent`
5. On completion → write `RetocStreamStatusEvent` with `status: "completed"` or `"failed"` + exit code + duration
6. On exception during execution → write `RetocStreamErrorEvent` + close stream

**Error Handling:**
- Validation/dependency errors before streaming starts: Write error event as NDJSON, return 200 (stream already started)
- Errors during execution: Write error event, close stream
- Timeout: Write `status: "failed"` with exitCode -1
- Client disconnect: Detect via `HttpContext.RequestAborted` and kill process

**Implementation Pattern:**
```csharp
group.MapPost("/stream", async (
    HttpContext httpContext,
    RetocStreamRequest request,
    IRetocAdapter retocAdapter,
    IStreamingProcessRunner streamingRunner,
    ILogger<RetocEndpoints> logger,
    CancellationToken cancellationToken) =>
{
    httpContext.Response.ContentType = "application/x-ndjson";
    httpContext.Response.Headers.Append("Cache-Control", "no-cache");
    httpContext.Response.Headers.Append("X-Accel-Buffering", "no");

    var writer = new StreamWriter(httpContext.Response.Body, leaveOpen: false);

    async Task WriteEvent(object eventObj)
    {
        var json = JsonSerializer.Serialize(eventObj);
        await writer.WriteLineAsync(json);
        await writer.FlushAsync();
    }

    try
    {
        // Validate and build command
        var command = MapRequestToRetocCommand(request);
        var (exePath, args) = retocAdapter.BuildCommand(command);

        logger.LogInformation("Starting streaming execution for operation {OperationId}", command.OperationId);

        // Write started event
        await WriteEvent(new RetocStreamStatusEvent("started", null, null));

        // Execute with streaming callbacks
        var startTime = DateTimeOffset.UtcNow;
        var result = await streamingRunner.ExecuteAsync(
            exePath,
            args,
            onStdOutLine: async (line) =>
            {
                await WriteEvent(new RetocStreamLineEvent("stdout", line, DateTimeOffset.UtcNow.ToString("O")));
            },
            onStdErrLine: async (line) =>
            {
                await WriteEvent(new RetocStreamLineEvent("stderr", line, DateTimeOffset.UtcNow.ToString("O")));
            },
            workingDirectory: null,
            timeoutSeconds: command.TimeoutSeconds ?? 300,
            environmentVariables: null,
            cancellationToken: httpContext.RequestAborted
        );

        var duration = (DateTimeOffset.UtcNow - startTime).ToString();
        var status = result.ExitCode == 0 ? "completed" : "failed";
        await WriteEvent(new RetocStreamStatusEvent(status, result.ExitCode, duration));

        logger.LogInformation("Streaming execution completed with exit code {ExitCode}", result.ExitCode);
    }
    catch (ValidationError ex)
    {
        logger.LogWarning(ex, "Validation error during streaming execution");
        await WriteEvent(new RetocStreamErrorEvent(new ErrorInfo(ex.ErrorCode, ex.Message, ex.RemediationHint)));
    }
    catch (DependencyMissingError ex)
    {
        logger.LogWarning(ex, "Dependency missing during streaming execution");
        await WriteEvent(new RetocStreamErrorEvent(new ErrorInfo(ex.ErrorCode, ex.Message, ex.RemediationHint)));
    }
    catch (TimeoutException ex)
    {
        logger.LogWarning(ex, "Timeout during streaming execution");
        await WriteEvent(new RetocStreamStatusEvent("failed", -1, null));
    }
    catch (OperationCanceledException)
    {
        logger.LogInformation("Streaming execution cancelled by client");
        // Client disconnected, stream already closed
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Unexpected error during streaming execution");
        await WriteEvent(new RetocStreamErrorEvent(new ErrorInfo("UNEXPECTED_ERROR", ex.Message, "Check logs")));
    }
    finally
    {
        await writer.DisposeAsync();
    }

    return Results.Empty;
});
```

---

## 5. Backend Design

### Command Building Guarantee (Preview == Execution)

**Single Source of Truth:** `RetocCommandBuilder.Build(RetocCommand, RetocOptions, string retocExePath)`

**Pattern:**
1. Both `/api/retoc/build` (preview) and `/api/retoc/stream` (execute) call the same `RetocCommandBuilder.Build()` method via `IRetocAdapter.BuildCommand()`
2. Build method returns tuple: `(string executablePath, string arguments)`
3. Response includes `commandLine` = `$"\"{executablePath}\" {arguments}"` for UI display
4. Frontend displays `commandLine` verbatim in preview panel
5. Execution uses exact `executablePath` and `arguments` from build

**Adapter Changes:**

Add to `IRetocAdapter.cs`:
```csharp
/// <summary>
/// Builds command-line arguments for a Retoc operation without executing.
/// Uses the same RetocCommandBuilder.Build logic as ConvertAsync to guarantee preview == execution.
/// </summary>
/// <param name="command">The Retoc command to build.</param>
/// <returns>Tuple of (executablePath, arguments).</returns>
(string ExecutablePath, string Arguments) BuildCommand(RetocCommand command);
```

Implement in `RetocAdapter.cs`:
```csharp
public (string ExecutablePath, string Arguments) BuildCommand(RetocCommand command)
{
    // Use the exact same builder that ConvertAsync uses
    return RetocCommandBuilder.Build(command, _options, _retocExePath);
}
```

**No Changes to RetocCommandBuilder.cs:** It already supports all 13 `RetocCommandType` values via `CommandTypeToString` (lines 176-194) and `AppendSubcommandArgs` (lines 200-273).

---

### Streaming Process Runner Integration

**New Component:** `StreamingProcessRunner : IStreamingProcessRunner`

**Location:** `src/Aris.Infrastructure/Process/StreamingProcessRunner.cs`

**Interface:**
```csharp
namespace Aris.Infrastructure.Process;

/// <summary>
/// Process runner that streams output line-by-line via callbacks.
/// Aligned with ProcessRunner's limits: 10 MB per stream, 100k lines max.
/// </summary>
public interface IStreamingProcessRunner
{
    /// <summary>
    /// Executes a process and invokes callbacks for each output line.
    /// </summary>
    /// <param name="executablePath">Full path to executable.</param>
    /// <param name="arguments">Command-line arguments.</param>
    /// <param name="onStdOutLine">Callback invoked for each stdout line.</param>
    /// <param name="onStdErrLine">Callback invoked for each stderr line.</param>
    /// <param name="workingDirectory">Working directory (optional).</param>
    /// <param name="timeoutSeconds">Timeout in seconds (0 = infinite).</param>
    /// <param name="environmentVariables">Environment variables (optional).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>ProcessResult summary (with truncated output for logging).</returns>
    Task<ProcessResult> ExecuteAsync(
        string executablePath,
        string arguments,
        Func<string, Task> onStdOutLine,
        Func<string, Task> onStdErrLine,
        string? workingDirectory = null,
        int timeoutSeconds = 0,
        IReadOnlyDictionary<string, string>? environmentVariables = null,
        CancellationToken cancellationToken = default);
}
```

**Implementation (aligned with ProcessRunner patterns):**
```csharp
namespace Aris.Infrastructure.Process;

public class StreamingProcessRunner : IStreamingProcessRunner
{
    private readonly ILogger<StreamingProcessRunner> _logger;
    private readonly int _maxOutputBytes;
    private readonly int _maxOutputLines;

    public StreamingProcessRunner(
        ILogger<StreamingProcessRunner> logger,
        IOptions<RetocOptions> options)
    {
        _logger = logger;
        // Use configured limits, defaulting to ProcessRunner's hardcoded values
        _maxOutputBytes = options.Value.MaxStreamingOutputBytes;
        _maxOutputLines = options.Value.MaxStreamingOutputLines;
    }

    public async Task<ProcessResult> ExecuteAsync(
        string executablePath,
        string arguments,
        Func<string, Task> onStdOutLine,
        Func<string, Task> onStdErrLine,
        string? workingDirectory = null,
        int timeoutSeconds = 0,
        IReadOnlyDictionary<string, string>? environmentVariables = null,
        CancellationToken cancellationToken = default)
    {
        var startTime = DateTimeOffset.UtcNow;

        _logger.LogInformation("Starting streaming process: {Executable} {Arguments}", executablePath, arguments);

        var processStartInfo = new ProcessStartInfo
        {
            FileName = executablePath,
            Arguments = arguments,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true,
            WorkingDirectory = workingDirectory ?? Directory.GetCurrentDirectory()
        };

        if (environmentVariables != null)
        {
            foreach (var kvp in environmentVariables)
            {
                processStartInfo.Environment[kvp.Key] = kvp.Value;
            }
        }

        using var process = new System.Diagnostics.Process { StartInfo = processStartInfo };

        // Bounded buffers for final ProcessResult (not unbounded)
        var stdOutBuilder = new BoundedStringBuilder(_maxOutputBytes, _maxOutputLines);
        var stdErrBuilder = new BoundedStringBuilder(_maxOutputBytes, _maxOutputLines);

        var stdOutTask = ReadStreamAsync(
            process.StandardOutput,
            async (line) =>
            {
                stdOutBuilder.AppendLine(line);
                await onStdOutLine(line);
            },
            cancellationToken);

        var stdErrTask = ReadStreamAsync(
            process.StandardError,
            async (line) =>
            {
                stdErrBuilder.AppendLine(line);
                await onStdErrLine(line);
            },
            cancellationToken);

        process.Start();
        var processId = process.Id;

        _logger.LogDebug("Streaming process started with PID {ProcessId}", processId);

        try
        {
            var hasTimeout = timeoutSeconds > 0;
            var timeout = hasTimeout ? TimeSpan.FromSeconds(timeoutSeconds) : Timeout.InfiniteTimeSpan;

            var completedTask = await WaitForExitAsync(process, timeout, cancellationToken);

            if (!completedTask)
            {
                _logger.LogWarning("Process {ProcessId} timed out after {TimeoutSeconds}s, killing process", processId, timeoutSeconds);
                KillProcessTree(process);
                throw new TimeoutException($"Process {Path.GetFileName(executablePath)} timed out after {timeoutSeconds} seconds");
            }

            // Wait for stream readers to finish
            await Task.WhenAll(stdOutTask, stdErrTask);

            var endTime = DateTimeOffset.UtcNow;
            var duration = endTime - startTime;
            var exitCode = process.ExitCode;

            _logger.LogInformation("Streaming process {ProcessId} exited with code {ExitCode} after {Duration}ms",
                processId, exitCode, duration.TotalMilliseconds);

            return new ProcessResult
            {
                ExitCode = exitCode,
                StdOut = stdOutBuilder.ToString(),
                StdErr = stdErrBuilder.ToString(),
                Duration = duration,
                StartTime = startTime,
                EndTime = endTime
            };
        }
        catch (Exception ex) when (ex is not TimeoutException)
        {
            _logger.LogError(ex, "Error executing streaming process {Executable}", executablePath);
            throw;
        }
    }

    private async Task ReadStreamAsync(
        StreamReader reader,
        Func<string, Task> onLine,
        CancellationToken cancellationToken)
    {
        int lineCount = 0;
        while (!reader.EndOfStream && lineCount < _maxOutputLines)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var line = await reader.ReadLineAsync();
            if (line != null)
            {
                await onLine(line);
                lineCount++;
            }
        }

        if (lineCount >= _maxOutputLines)
        {
            _logger.LogWarning("Stream reading stopped: max line count ({MaxLines}) reached", _maxOutputLines);
        }
    }

    private static async Task<bool> WaitForExitAsync(
        System.Diagnostics.Process process,
        TimeSpan timeout,
        CancellationToken cancellationToken)
    {
        // Same pattern as ProcessRunner.cs lines 134-164
        var tcs = new TaskCompletionSource<bool>();

        void ProcessExited(object? sender, EventArgs e) => tcs.TrySetResult(true);

        process.EnableRaisingEvents = true;
        process.Exited += ProcessExited;

        try
        {
            if (process.HasExited)
            {
                return true;
            }

            using var registration = cancellationToken.Register(() => tcs.TrySetCanceled(cancellationToken));

            var completedTask = timeout == Timeout.InfiniteTimeSpan
                ? await tcs.Task
                : await Task.WhenAny(tcs.Task, Task.Delay(timeout, CancellationToken.None)) == tcs.Task && await tcs.Task;

            return completedTask;
        }
        finally
        {
            process.Exited -= ProcessExited;
        }
    }

    private void KillProcessTree(System.Diagnostics.Process process)
    {
        // Same pattern as ProcessRunner.cs lines 167-181
        try
        {
            if (!process.HasExited)
            {
                process.Kill(entireProcessTree: true);
                _logger.LogDebug("Killed process tree for PID {ProcessId}", process.Id);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to kill process tree for PID {ProcessId}", process.Id);
        }
    }

    // Reuse BoundedStringBuilder pattern from ProcessRunner.cs lines 183-217
    private class BoundedStringBuilder
    {
        private readonly StringBuilder _builder = new();
        private readonly int _maxBytes;
        private readonly int _maxLines;
        private int _currentBytes;
        private int _currentLines;
        private bool _truncated;

        public BoundedStringBuilder(int maxBytes, int maxLines)
        {
            _maxBytes = maxBytes;
            _maxLines = maxLines;
        }

        public void AppendLine(string line)
        {
            if (_truncated) return;

            var lineBytes = Encoding.UTF8.GetByteCount(line) + Environment.NewLine.Length;

            if (_currentBytes + lineBytes > _maxBytes || _currentLines >= _maxLines)
            {
                _builder.AppendLine("... [output truncated due to size limits]");
                _truncated = true;
                return;
            }

            _builder.AppendLine(line);
            _currentBytes += lineBytes;
            _currentLines++;
        }

        public override string ToString() => _builder.ToString();
    }
}
```

**Configuration (src/Aris.Infrastructure/Configuration/RetocOptions.cs):**

Add these properties:
```csharp
/// <summary>
/// Maximum streaming output size in bytes per stream (stdout/stderr).
/// Defaults to ProcessRunner's limit: 10 MB.
/// </summary>
public int MaxStreamingOutputBytes { get; set; } = 10 * 1024 * 1024;

/// <summary>
/// Maximum streaming output lines per stream.
/// Defaults to ProcessRunner's limit: 100,000 lines.
/// </summary>
public int MaxStreamingOutputLines { get; set; } = 100000;
```

**DI Registration (src/Aris.Infrastructure/DependencyInjection.cs):**
```csharp
services.AddSingleton<IStreamingProcessRunner, StreamingProcessRunner>();
```

---

### Schema Provider Design

**Component:** `RetocCommandSchemaProvider`

**Location:** `src/Aris.Adapters/Retoc/RetocCommandSchemaProvider.cs`

**Purpose:** Provide metadata for all `RetocCommandType` commands using hardcoded schema that maps to `RetocCommand` domain model properties.

**Schema Generation Strategy:**
- Hardcode schema for all 13 `RetocCommandType` enum values
- Map field names to `RetocCommand` properties (e.g., "InputPath", "OutputPath", "EngineVersion", "ChunkIndex")
- Include global options (AES key, version overrides) available for all commands
- Dynamically include allowlisted flags from `RetocOptions.AllowedAdditionalArgs` as boolean fields

**Implementation:**

```csharp
using Aris.Contracts.Retoc;
using Aris.Core.Retoc;
using Aris.Infrastructure.Configuration;

namespace Aris.Adapters.Retoc;

/// <summary>
/// Provides schema metadata for all supported Retoc commands.
/// Schema is hardcoded and aligned with RetocCommand domain model properties.
/// </summary>
public static class RetocCommandSchemaProvider
{
    public static RetocCommandSchemaResponse GetSchema(RetocOptions options)
    {
        var globalOptions = new List<RetocCommandFieldDefinition>
        {
            new("AesKey", "AES Encryption Key", "String",
                "Hex string encryption key (e.g., 0x1234567890ABCDEF)", null, null, null),
            new("ContainerHeaderVersion", "Container Header Version", "Enum",
                "Override container header version",
                Enum.GetNames<RetocContainerHeaderVersion>().ToList(), null, null),
            new("TocVersion", "TOC Version", "Enum",
                "Override TOC version",
                Enum.GetNames<RetocTocVersion>().ToList(), null, null)
        };

        var allowlistedFlags = options.AllowedAdditionalArgs
            .Select(flag => new RetocCommandFieldDefinition(
                FieldName: ToCamelCase(flag.TrimStart('-')),
                Label: ToDisplayLabel(flag),
                FieldType: "Boolean",
                HelpText: $"Enable {flag} flag",
                EnumValues: null,
                MinValue: null,
                MaxValue: null))
            .ToList();

        var commands = new List<RetocCommandDefinition>
        {
            // ToLegacy
            new("ToLegacy", "To Legacy (Zen → PAK)",
                "Convert assets and shaders from Zen (IoStore) to Legacy (PAK) format",
                RequiredFields: new List<RetocCommandFieldDefinition>
                {
                    new("InputPath", "Input Directory", "Path",
                        "Directory containing .utoc/.ucas files", null, null, null),
                    new("OutputPath", "Output Directory", "Path",
                        "Directory for extracted .pak files", null, null, null)
                },
                OptionalFields: new List<RetocCommandFieldDefinition>()),

            // ToZen
            new("ToZen", "To Zen (PAK → IoStore)",
                "Convert assets and shaders from Legacy (PAK) to Zen (IoStore) format",
                RequiredFields: new List<RetocCommandFieldDefinition>
                {
                    new("InputPath", "Input Directory", "Path",
                        "Directory containing .pak files", null, null, null),
                    new("OutputPath", "Output File", "Path",
                        "Output .utoc file path", null, null, null),
                    new("EngineVersion", "UE Version", "Enum",
                        "Unreal Engine version",
                        new List<string> { "UE5_6", "UE5_5", "UE5_4", "UE5_3", "UE5_2", "UE5_1", "UE5_0" },
                        null, null)
                },
                OptionalFields: new List<RetocCommandFieldDefinition>()),

            // Manifest
            new("Manifest", "Manifest",
                "Extract manifest data from .utoc file",
                RequiredFields: new List<RetocCommandFieldDefinition>
                {
                    new("InputPath", "Input File", "Path",
                        ".utoc file to read", null, null, null)
                },
                OptionalFields: new List<RetocCommandFieldDefinition>()),

            // Info
            new("Info", "Info",
                "Display container information",
                RequiredFields: new List<RetocCommandFieldDefinition>
                {
                    new("InputPath", "Input File", "Path",
                        ".utoc file to inspect", null, null, null)
                },
                OptionalFields: new List<RetocCommandFieldDefinition>()),

            // List
            new("List", "List",
                "List files in .utoc directory index",
                RequiredFields: new List<RetocCommandFieldDefinition>
                {
                    new("InputPath", "Input File", "Path",
                        ".utoc file to list", null, null, null)
                },
                OptionalFields: new List<RetocCommandFieldDefinition>()),

            // Verify
            new("Verify", "Verify",
                "Validate IO Store container integrity",
                RequiredFields: new List<RetocCommandFieldDefinition>
                {
                    new("InputPath", "Input File", "Path",
                        ".utoc file to verify", null, null, null)
                },
                OptionalFields: new List<RetocCommandFieldDefinition>()),

            // Unpack
            new("Unpack", "Unpack",
                "Extract chunks (files) from .utoc container",
                RequiredFields: new List<RetocCommandFieldDefinition>
                {
                    new("InputPath", "Input File", "Path",
                        ".utoc file to unpack", null, null, null),
                    new("OutputPath", "Output Directory", "Path",
                        "Directory for extracted files", null, null, null)
                },
                OptionalFields: new List<RetocCommandFieldDefinition>()),

            // UnpackRaw
            new("UnpackRaw", "Unpack Raw",
                "Extract raw chunks from container",
                RequiredFields: new List<RetocCommandFieldDefinition>
                {
                    new("InputPath", "Input File", "Path",
                        ".utoc file to unpack", null, null, null),
                    new("OutputPath", "Output Directory", "Path",
                        "Directory for raw chunks", null, null, null)
                },
                OptionalFields: new List<RetocCommandFieldDefinition>()),

            // PackRaw
            new("PackRaw", "Pack Raw",
                "Pack directory of raw chunks into container",
                RequiredFields: new List<RetocCommandFieldDefinition>
                {
                    new("InputPath", "Input Directory", "Path",
                        "Directory containing raw chunks", null, null, null),
                    new("OutputPath", "Output File Prefix", "Path",
                        "Output file prefix (without extension)", null, null, null)
                },
                OptionalFields: new List<RetocCommandFieldDefinition>()),

            // Get
            new("Get", "Get",
                "Retrieve chunk by index and output to stdout",
                RequiredFields: new List<RetocCommandFieldDefinition>
                {
                    new("InputPath", "Input File", "Path",
                        ".utoc file", null, null, null),
                    new("ChunkIndex", "Chunk Index", "Integer",
                        "Zero-based chunk index to retrieve", null, 0, int.MaxValue)
                },
                OptionalFields: new List<RetocCommandFieldDefinition>()),

            // DumpTest
            new("DumpTest", "Dump Test",
                "Execute dump test operation",
                RequiredFields: new List<RetocCommandFieldDefinition>
                {
                    new("InputPath", "Input File", "Path",
                        "Input file for dump test", null, null, null)
                },
                OptionalFields: new List<RetocCommandFieldDefinition>()),

            // GenScriptObjects
            new("GenScriptObjects", "Generate Script Objects",
                "Generate script objects global container from UE reflection data (.jmap)",
                RequiredFields: new List<RetocCommandFieldDefinition>
                {
                    new("InputPath", "Input .jmap File", "Path",
                        "UE reflection data file", null, null, null),
                    new("OutputPath", "Output Directory", "Path",
                        "Output directory for generated container", null, null, null)
                },
                OptionalFields: new List<RetocCommandFieldDefinition>()),

            // PrintScriptObjects
            new("PrintScriptObjects", "Print Script Objects",
                "Output script objects from container",
                RequiredFields: new List<RetocCommandFieldDefinition>
                {
                    new("InputPath", "Input File", "Path",
                        "Script objects container file", null, null, null)
                },
                OptionalFields: new List<RetocCommandFieldDefinition>())
        };

        return new RetocCommandSchemaResponse(commands, globalOptions, allowlistedFlags);
    }

    private static string ToCamelCase(string flag)
    {
        // Convert "verbose" → "verbose", "no-warnings" → "noWarnings"
        var parts = flag.Split('-');
        if (parts.Length == 1) return parts[0];
        return parts[0] + string.Concat(parts.Skip(1).Select(p =>
            char.ToUpper(p[0]) + p.Substring(1)));
    }

    private static string ToDisplayLabel(string flag)
    {
        // Convert "--verbose" → "Verbose", "--no-warnings" → "No Warnings"
        var clean = flag.TrimStart('-');
        var parts = clean.Split('-');
        return string.Join(" ", parts.Select(p => char.ToUpper(p[0]) + p.Substring(1)));
    }
}
```

**Note:** This schema is manually maintained. Tests (see section 7) will verify completeness.

---

### Help Endpoint Implementation

**Implementation Notes:**
- Use the exact tool resolution pattern from `RetocAdapter.cs` constructor (lines 36-43)
- Load tool manifest, resolve path from `%LOCALAPPDATA%/ARIS/tools/{version}/`
- Execute `retoc.exe --help` via `IProcessRunner`
- Wrap output in Markdown code fence

**See Endpoint Mapping section 4 for full implementation code.**

---

## 6. Frontend Design

### Mode Toggle Component

**Location:** `RetocPage.tsx`

**State:**
```typescript
type RetocMode = 'simple' | 'advanced';
const [mode, setMode] = useState<RetocMode>('simple');
```

**UI:**
```tsx
<div className="flex items-center gap-2 mb-4">
  <button
    onClick={() => setMode('simple')}
    className={mode === 'simple' ? 'btn-active' : 'btn'}
  >
    Simple
  </button>
  <button
    onClick={() => setMode('advanced')}
    className={mode === 'advanced' ? 'btn-active' : 'btn'}
  >
    Advanced
  </button>
</div>
```

**Persistence (optional):**
```typescript
useEffect(() => {
  const saved = localStorage.getItem('retocMode');
  if (saved === 'simple' || saved === 'advanced') {
    setMode(saved);
  }
}, []);

useEffect(() => {
  localStorage.setItem('retocMode', mode);
}, [mode]);
```

---

### Simple Mode UI

**Layout:** Two sections side-by-side (or stacked on mobile):

1. **Pack Section (to-zen)**
   - Modified UAsset Directory (text input with folder picker)
   - Mod Output Directory (text input with folder picker)
   - Mod Name (text input)
   - UE Version (dropdown: UE5_6, UE5_5, UE5_4, UE5_3, UE5_2, UE5_1, UE5_0)
   - Preview (read-only command line display)
   - Build Mod button

2. **Unpack Section (to-legacy)**
   - Base Game Paks Directory (text input with folder picker)
   - Extracted Output Directory (text input with folder picker)
   - Preview (read-only command line display)
   - Extract Files button

**Behavior:**
- On field change → debounce 300ms → call `/api/retoc/build` → update preview
- On button click → disable buttons → call `/api/retoc/stream` → show console log → stream events → enable on completion
- Validation errors show inline (red border + error message below field)

**State Management:**
```typescript
const [packForm, setPackForm] = useState({
  modifiedDir: '',
  outputDir: '',
  modName: '',
  engineVersion: 'UE5_6'
});

const [unpackForm, setUnpackForm] = useState({
  paksDir: '',
  outputDir: ''
});

const [packPreview, setPackPreview] = useState<string>('');
const [unpackPreview, setUnpackPreview] = useState<string>('');

const [isRunning, setIsRunning] = useState(false);
const [executionState, setExecutionState] = useState<'idle' | 'running' | 'completed' | 'failed'>('idle');
const [consoleLog, setConsoleLog] = useState<RetocStreamLineEvent[]>([]);
const [abortController, setAbortController] = useState<AbortController | null>(null);
```

**Preview Update (Pack):**
```typescript
useEffect(() => {
  const timer = setTimeout(async () => {
    if (packForm.modifiedDir && packForm.outputDir && packForm.modName && packForm.engineVersion) {
      try {
        const request: RetocBuildCommandRequest = {
          commandType: 'ToZen',
          inputPath: packForm.modifiedDir,
          outputPath: `${packForm.outputDir}\\${packForm.modName}.utoc`,
          engineVersion: packForm.engineVersion
        };

        const response = await buildRetocCommand(request);
        setPackPreview(response.commandLine);
      } catch (err) {
        setPackPreview('Error building command: ' + (err instanceof Error ? err.message : 'Unknown'));
      }
    } else {
      setPackPreview('');
    }
  }, 300);

  return () => clearTimeout(timer);
}, [packForm]);
```

**Execute (Pack):**
```typescript
const handlePackExecute = async () => {
  setIsRunning(true);
  setExecutionState('running');
  setConsoleLog([]);

  const request: RetocStreamRequest = {
    commandType: 'ToZen',
    inputPath: packForm.modifiedDir,
    outputPath: `${packForm.outputDir}\\${packForm.modName}.utoc`,
    engineVersion: packForm.engineVersion
  };

  const controller = new AbortController();
  setAbortController(controller);

  try {
    await streamRetocExecution(request, (event) => {
      if (event.eventType === 'status') {
        if (event.status === 'started') {
          // Already in running state
        } else if (event.status === 'completed') {
          setExecutionState('completed');
          setIsRunning(false);
        } else if (event.status === 'failed') {
          setExecutionState('failed');
          setIsRunning(false);
        }
      } else if (event.eventType === 'line') {
        setConsoleLog(prev => [...prev, event]);
      } else if (event.eventType === 'error') {
        setError(event.error);
        setExecutionState('failed');
        setIsRunning(false);
      }
    }, controller.signal);
  } catch (err) {
    if (err.name !== 'AbortError') {
      setError({ code: 'NETWORK_ERROR', message: err.message });
      setExecutionState('failed');
    }
  } finally {
    setIsRunning(false);
    setAbortController(null);
  }
};

const handleCancel = () => {
  if (abortController) {
    abortController.abort();
    setExecutionState('idle');
    setIsRunning(false);
  }
};
```

---

### Advanced Mode UI

**Layout:**

1. **Help Button** (top-right corner)
   - Opens modal with Markdown-rendered help content
   - Fetch `/api/retoc/help` on first click (cache)

2. **Command Selector** (dropdown)
   - Populated from `/api/retoc/schema`
   - Displays `commandDefinition.displayName`
   - Shows `commandDefinition.description` below dropdown

3. **Dynamic Fields**
   - Rendered based on selected command's `requiredFields` + `optionalFields` + `globalOptions` + `allowlistedFlags`
   - Field types:
     - **Path**: Text input with folder/file picker button
     - **Enum**: Dropdown with `enumValues`
     - **String**: Text input
     - **Integer**: Number input with `minValue`/`maxValue` validation
     - **Boolean**: Checkbox (for allowlisted flags)
   - Required fields marked with asterisk
   - Help text shown below each field

4. **Command Preview**
   - Same as Simple Mode
   - Updates on field change (debounced)

5. **Execute Button**
   - Calls `/api/retoc/stream`
   - Disabled while running
   - Cancel button appears when running

6. **Console Log**
   - Same as Simple Mode

**State Management:**
```typescript
const [schema, setSchema] = useState<RetocCommandSchemaResponse | null>(null);
const [selectedCommand, setSelectedCommand] = useState<string>('ToLegacy');
const [fieldValues, setFieldValues] = useState<Record<string, any>>({});
const [commandPreview, setCommandPreview] = useState<string>('');

useEffect(() => {
  getRetocSchema().then(setSchema);
}, []);

const currentCommandDef = schema?.commands.find(c => c.commandType === selectedCommand);

const handleFieldChange = (fieldName: string, value: any) => {
  setFieldValues(prev => ({ ...prev, [fieldName]: value }));
};

useEffect(() => {
  // Rebuild preview on field change
  const timer = setTimeout(async () => {
    try {
      const request = buildRequestFromFields(selectedCommand, fieldValues, schema);
      const response = await buildRetocCommand(request);
      setCommandPreview(response.commandLine);
    } catch (err) {
      setCommandPreview('Error: ' + (err instanceof Error ? err.message : 'Unknown'));
    }
  }, 300);
  return () => clearTimeout(timer);
}, [selectedCommand, fieldValues, schema]);

function buildRequestFromFields(
  commandType: string,
  fields: Record<string, any>,
  schema: RetocCommandSchemaResponse | null
): RetocBuildCommandRequest {
  // Map UI field values to request DTO
  return {
    commandType,
    inputPath: fields.InputPath || '',
    outputPath: fields.OutputPath || null,
    engineVersion: fields.EngineVersion || null,
    aesKey: fields.AesKey || null,
    containerHeaderVersion: fields.ContainerHeaderVersion || null,
    tocVersion: fields.TocVersion || null,
    chunkIndex: fields.ChunkIndex || null,
    verbose: fields.verbose || null,
    noWarnings: fields.noWarnings || null,
    timeoutSeconds: fields.TimeoutSeconds || null
  };
}
```

**Dynamic Field Rendering:**
```tsx
{currentCommandDef?.requiredFields.map(field => (
  <Field key={field.fieldName} label={field.label} required error={errors[field.fieldName]}>
    {renderFieldInput(field, fieldValues[field.fieldName], handleFieldChange)}
    {field.helpText && <p className="text-sm text-muted">{field.helpText}</p>}
  </Field>
))}

{currentCommandDef?.optionalFields.map(field => (
  <Field key={field.fieldName} label={field.label}>
    {renderFieldInput(field, fieldValues[field.fieldName], handleFieldChange)}
    {field.helpText && <p className="text-sm text-muted">{field.helpText}</p>}
  </Field>
))}

{schema?.globalOptions.map(field => (
  <Field key={field.fieldName} label={field.label}>
    {renderFieldInput(field, fieldValues[field.fieldName], handleFieldChange)}
    {field.helpText && <p className="text-sm text-muted">{field.helpText}</p>}
  </Field>
))}

{schema?.allowlistedFlags.map(field => (
  <Field key={field.fieldName} label={field.label}>
    <input
      type="checkbox"
      checked={!!fieldValues[field.fieldName]}
      onChange={(e) => handleFieldChange(field.fieldName, e.target.checked)}
    />
    {field.helpText && <span className="text-sm text-muted">{field.helpText}</span>}
  </Field>
))}

function renderFieldInput(
  field: RetocCommandFieldDefinition,
  value: any,
  onChange: (name: string, value: any) => void
) {
  switch (field.fieldType) {
    case 'Path':
      return <Input type="text" value={value || ''} onChange={(e) => onChange(field.fieldName, e.target.value)} />;
    case 'Enum':
      return (
        <Select value={value || field.enumValues?.[0]} onChange={(e) => onChange(field.fieldName, e.target.value)}>
          {field.enumValues?.map(val => <option key={val} value={val}>{val}</option>)}
        </Select>
      );
    case 'Integer':
      return (
        <Input
          type="number"
          min={field.minValue}
          max={field.maxValue}
          value={value || ''}
          onChange={(e) => onChange(field.fieldName, parseInt(e.target.value) || null)}
        />
      );
    case 'String':
      return <Input type="text" value={value || ''} onChange={(e) => onChange(field.fieldName, e.target.value)} />;
    case 'Boolean':
      return <input type="checkbox" checked={!!value} onChange={(e) => onChange(field.fieldName, e.target.checked)} />;
    default:
      return <Input type="text" value={value || ''} onChange={(e) => onChange(field.fieldName, e.target.value)} />;
  }
}
```

---

### API Client (retocClient.ts)

**Location:** `frontend/src/api/retocClient.ts`

**Pattern:** Follows existing `getBackendBaseUrl()` + `fetch()` pattern from `RetocPage.tsx` lines 45-57.

**Implementation:**

```typescript
import { getBackendBaseUrl } from '../config/backend';
import type {
  RetocBuildCommandRequest,
  RetocBuildCommandResponse,
  RetocStreamRequest,
  RetocCommandSchemaResponse,
  RetocHelpResponse,
  RetocStreamEvent
} from '../types/contracts';

export async function buildRetocCommand(
  request: RetocBuildCommandRequest
): Promise<RetocBuildCommandResponse> {
  const baseUrl = getBackendBaseUrl();
  const response = await fetch(`${baseUrl}/api/retoc/build`, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify(request)
  });

  if (!response.ok) {
    const error = await response.json();
    throw new Error(error.message || `HTTP ${response.status}`);
  }

  return await response.json();
}

export async function getRetocSchema(): Promise<RetocCommandSchemaResponse> {
  const baseUrl = getBackendBaseUrl();
  const response = await fetch(`${baseUrl}/api/retoc/schema`);

  if (!response.ok) {
    throw new Error(`HTTP ${response.status}: Failed to fetch schema`);
  }

  return await response.json();
}

export async function getRetocHelp(): Promise<RetocHelpResponse> {
  const baseUrl = getBackendBaseUrl();
  const response = await fetch(`${baseUrl}/api/retoc/help`);

  if (!response.ok) {
    const error = await response.json();
    throw new Error(error.message || `HTTP ${response.status}`);
  }

  return await response.json();
}

export async function streamRetocExecution(
  request: RetocStreamRequest,
  onEvent: (event: RetocStreamEvent) => void,
  signal?: AbortSignal
): Promise<void> {
  const baseUrl = getBackendBaseUrl();
  const response = await fetch(`${baseUrl}/api/retoc/stream`, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify(request),
    signal
  });

  if (!response.ok) {
    throw new Error(`HTTP ${response.status}: ${response.statusText}`);
  }

  if (!response.body) {
    throw new Error('Response body is null');
  }

  // Read NDJSON stream
  const reader = response.body.getReader();
  const decoder = new TextDecoder();
  let buffer = '';

  try {
    while (true) {
      const { done, value } = await reader.read();
      if (done) break;

      buffer += decoder.decode(value, { stream: true });

      // Split by newlines and process complete JSON objects
      const lines = buffer.split('\n');
      buffer = lines.pop() || ''; // Keep incomplete line in buffer

      for (const line of lines) {
        if (line.trim()) {
          try {
            const event = JSON.parse(line) as RetocStreamEvent;
            onEvent(event);
          } catch (err) {
            console.error('Failed to parse NDJSON line:', line, err);
          }
        }
      }
    }

    // Process any remaining buffer
    if (buffer.trim()) {
      try {
        const event = JSON.parse(buffer) as RetocStreamEvent;
        onEvent(event);
      } catch (err) {
        console.error('Failed to parse final NDJSON:', buffer, err);
      }
    }
  } finally {
    reader.releaseLock();
  }
}
```

---

### Console Log Component

**Component:** `RetocConsoleLog.tsx`

```tsx
interface RetocConsoleLogProps {
  entries: RetocStreamLineEvent[];
  executionState: 'idle' | 'running' | 'completed' | 'failed';
}

export function RetocConsoleLog({ entries, executionState }: RetocConsoleLogProps) {
  const logRef = useRef<HTMLDivElement>(null);

  useEffect(() => {
    // Auto-scroll to bottom on new entries
    if (logRef.current) {
      logRef.current.scrollTop = logRef.current.scrollHeight;
    }
  }, [entries]);

  return (
    <Panel>
      <PanelHeader title="Console Log">
        <StatusPill status={
          executionState === 'running' ? 'info' :
          executionState === 'completed' ? 'success' :
          executionState === 'failed' ? 'error' : 'default'
        }>
          {executionState.toUpperCase()}
        </StatusPill>
      </PanelHeader>
      <PanelBody padding="none">
        <div
          ref={logRef}
          className="bg-black text-white font-mono text-xs p-4 h-96 overflow-y-auto"
        >
          {entries.length === 0 && (
            <span className="text-gray-500">Waiting for execution...</span>
          )}
          {entries.map((entry, i) => (
            <div
              key={i}
              className={entry.stream === 'stderr' ? 'text-red-400' : 'text-green-400'}
            >
              <span className="text-gray-600">[{new Date(entry.timestamp).toLocaleTimeString()}]</span>{' '}
              {entry.text}
            </div>
          ))}
        </div>
      </PanelBody>
    </Panel>
  );
}
```

---

### Command Preview Component

**Component:** `RetocCommandPreview.tsx`

```tsx
interface RetocCommandPreviewProps {
  commandLine: string;
}

export function RetocCommandPreview({ commandLine }: RetocCommandPreviewProps) {
  const [copied, setCopied] = useState(false);

  const handleCopy = () => {
    navigator.clipboard.writeText(commandLine);
    setCopied(true);
    setTimeout(() => setCopied(false), 2000);
  };

  return (
    <div className="bg-[var(--bg-inset)] border border-[var(--border-default)] rounded p-3">
      <div className="flex items-center justify-between mb-2">
        <span className="text-xs font-semibold text-[var(--text-secondary)]">
          Command Preview
        </span>
        <button onClick={handleCopy} className="text-xs">
          {copied ? 'Copied!' : 'Copy'}
        </button>
      </div>
      <pre className="text-xs font-mono text-[var(--text-primary)] whitespace-pre-wrap break-all">
        {commandLine || 'Complete the form to see command preview'}
      </pre>
    </div>
  );
}
```

---

### Help Modal Component

**Component:** `RetocHelpModal.tsx`

**Dependency:** Install `react-markdown`:
```bash
npm install react-markdown
```

```tsx
import ReactMarkdown from 'react-markdown';

interface RetocHelpModalProps {
  isOpen: boolean;
  onClose: () => void;
}

export function RetocHelpModal({ isOpen, onClose }: RetocHelpModalProps) {
  const [helpContent, setHelpContent] = useState<string>('');
  const [isLoading, setIsLoading] = useState(false);

  useEffect(() => {
    if (isOpen && !helpContent) {
      setIsLoading(true);
      getRetocHelp()
        .then(response => setHelpContent(response.markdown))
        .catch(err => setHelpContent(`Error loading help: ${err.message}`))
        .finally(() => setIsLoading(false));
    }
  }, [isOpen, helpContent]);

  if (!isOpen) return null;

  return (
    <div className="modal-overlay">
      <div className="modal-content max-w-4xl">
        <div className="modal-header">
          <h2>Retoc Help</h2>
          <button onClick={onClose}>Close</button>
        </div>
        <div className="modal-body">
          {isLoading ? (
            <div>Loading help...</div>
          ) : (
            <ReactMarkdown>{helpContent}</ReactMarkdown>
          )}
        </div>
      </div>
    </div>
  );
}
```

---

## 7. Testing Plan

### Backend Tests

#### File: `tests/Aris.Core.Tests/Adapters/RetocAdapterTests.cs`

**Add Tests (using existing Fakes pattern from lines 1-4):**

```csharp
[Fact]
public void BuildCommand_ToLegacy_GeneratesCorrectCommandLine()
{
    // Arrange
    var adapter = CreateAdapter(); // Uses FakeProcessRunner, FakeDependencyValidator
    var command = new RetocCommand
    {
        CommandType = RetocCommandType.ToLegacy,
        InputPath = @"C:\Input\Paks",
        OutputPath = @"C:\Output\Extracted"
    };

    // Act
    var (exePath, args) = adapter.BuildCommand(command);

    // Assert
    Assert.Contains("retoc.exe", exePath);
    Assert.Contains("to-legacy", args);
    Assert.Contains(@"""C:\Input\Paks""", args);
    Assert.Contains(@"""C:\Output\Extracted""", args);
}

[Fact]
public void BuildCommand_ToZen_GeneratesCorrectCommandLine()
{
    // Arrange
    var adapter = CreateAdapter();
    var command = new RetocCommand
    {
        CommandType = RetocCommandType.ToZen,
        InputPath = @"C:\Input\Modified",
        OutputPath = @"C:\Output\MyMod.utoc",
        Version = "UE5_6"
    };

    // Act
    var (exePath, args) = adapter.BuildCommand(command);

    // Assert
    Assert.Contains("to-zen", args);
    Assert.Contains("--version UE5_6", args);
    Assert.Contains(@"""C:\Input\Modified""", args);
    Assert.Contains(@"""C:\Output\MyMod.utoc""", args);
}

[Theory]
[InlineData(RetocCommandType.Manifest, "manifest")]
[InlineData(RetocCommandType.Info, "info")]
[InlineData(RetocCommandType.List, "list")]
[InlineData(RetocCommandType.Verify, "verify")]
[InlineData(RetocCommandType.Unpack, "unpack")]
[InlineData(RetocCommandType.UnpackRaw, "unpack-raw")]
[InlineData(RetocCommandType.PackRaw, "pack-raw")]
[InlineData(RetocCommandType.Get, "get")]
[InlineData(RetocCommandType.DumpTest, "dump-test")]
[InlineData(RetocCommandType.GenScriptObjects, "gen-script-objects")]
[InlineData(RetocCommandType.PrintScriptObjects, "print-script-objects")]
public void BuildCommand_AllCommandTypes_GenerateCorrectSubcommand(
    RetocCommandType commandType,
    string expectedSubcommand)
{
    // Arrange
    var adapter = CreateAdapter();
    var command = new RetocCommand
    {
        CommandType = commandType,
        InputPath = @"C:\Input\file.utoc",
        OutputPath = commandType == RetocCommandType.Info || commandType == RetocCommandType.List
            ? null
            : @"C:\Output"
    };

    // Act
    var (exePath, args) = adapter.BuildCommand(command);

    // Assert
    Assert.Contains(expectedSubcommand, args);
}
```

---

#### File: `tests/Aris.Core.Tests/Retoc/RetocCommandSchemaProviderTests.cs` (NEW)

**Purpose:** Verify schema completeness and prevent drift.

```csharp
using Aris.Adapters.Retoc;
using Aris.Contracts.Retoc;
using Aris.Core.Retoc;
using Aris.Infrastructure.Configuration;
using Xunit;

namespace Aris.Core.Tests.Retoc;

public class RetocCommandSchemaProviderTests
{
    private readonly RetocOptions _defaultOptions = new RetocOptions
    {
        AllowedAdditionalArgs = new List<string> { "--verbose", "--no-warnings" }
    };

    [Fact]
    public void GetSchema_ReturnsAll13Commands()
    {
        // Act
        var schema = RetocCommandSchemaProvider.GetSchema(_defaultOptions);

        // Assert
        Assert.Equal(13, schema.Commands.Count);

        // Verify all RetocCommandType enum values have corresponding schema definitions
        var allCommandTypes = Enum.GetValues<RetocCommandType>();
        foreach (var commandType in allCommandTypes)
        {
            var commandTypeName = commandType.ToString();
            Assert.Contains(schema.Commands, c => c.CommandType == commandTypeName);
        }
    }

    [Fact]
    public void GetSchema_AllCommandTypes_HaveSchemaDefinitions()
    {
        // Arrange
        var schema = RetocCommandSchemaProvider.GetSchema(_defaultOptions);
        var allCommandTypes = Enum.GetValues<RetocCommandType>();

        // Act & Assert - Verify no RetocCommandType is missing from schema
        foreach (var commandType in allCommandTypes)
        {
            var commandTypeName = commandType.ToString();
            var schemaDef = schema.Commands.FirstOrDefault(c => c.CommandType == commandTypeName);

            Assert.NotNull(schemaDef); // Fail if enum value missing from schema
            Assert.NotEmpty(schemaDef.DisplayName);
            Assert.NotEmpty(schemaDef.Description);
        }
    }

    [Fact]
    public void GetSchema_AllSchemaFields_MapToRetocCommandProperties()
    {
        // Arrange
        var schema = RetocCommandSchemaProvider.GetSchema(_defaultOptions);
        var retocCommandType = typeof(RetocCommand);

        // Act & Assert - Verify all schema field names correspond to actual RetocCommand properties
        foreach (var command in schema.Commands)
        {
            foreach (var field in command.RequiredFields.Concat(command.OptionalFields))
            {
                var property = retocCommandType.GetProperty(field.FieldName);
                Assert.NotNull(property); // Fail if schema field doesn't map to domain model property
            }
        }

        // Verify global options map to RetocCommand properties
        foreach (var field in schema.GlobalOptions)
        {
            var property = retocCommandType.GetProperty(field.FieldName);
            Assert.NotNull(property);
        }
    }

    [Fact]
    public void GetSchema_AllowlistedFlags_MatchRetocOptions()
    {
        // Arrange
        var options = new RetocOptions
        {
            AllowedAdditionalArgs = new List<string> { "--verbose", "--no-warnings", "--custom-flag" }
        };

        // Act
        var schema = RetocCommandSchemaProvider.GetSchema(options);

        // Assert - Each allowlisted arg should have a corresponding boolean field
        Assert.Equal(3, schema.AllowlistedFlags.Count);
        Assert.Contains(schema.AllowlistedFlags, f => f.FieldName == "verbose");
        Assert.Contains(schema.AllowlistedFlags, f => f.FieldName == "noWarnings");
        Assert.Contains(schema.AllowlistedFlags, f => f.FieldName == "customFlag");

        // All should be Boolean type
        Assert.All(schema.AllowlistedFlags, f => Assert.Equal("Boolean", f.FieldType));
    }

    [Fact]
    public void GetSchema_ToZen_HasEngineVersionEnum()
    {
        // Act
        var schema = RetocCommandSchemaProvider.GetSchema(_defaultOptions);
        var toZen = schema.Commands.First(c => c.CommandType == "ToZen");

        // Assert
        var engineVersionField = toZen.RequiredFields.First(f => f.FieldName == "EngineVersion");
        Assert.Equal("Enum", engineVersionField.FieldType);
        Assert.Contains("UE5_6", engineVersionField.EnumValues);
        Assert.Contains("UE5_0", engineVersionField.EnumValues);
    }

    [Fact]
    public void GetSchema_Get_HasChunkIndexInteger()
    {
        // Act
        var schema = RetocCommandSchemaProvider.GetSchema(_defaultOptions);
        var getCommand = schema.Commands.First(c => c.CommandType == "Get");

        // Assert
        var chunkIndexField = getCommand.RequiredFields.First(f => f.FieldName == "ChunkIndex");
        Assert.Equal("Integer", chunkIndexField.FieldType);
        Assert.Equal(0, chunkIndexField.MinValue);
        Assert.True(chunkIndexField.MaxValue > 0);
    }

    [Fact]
    public void GetSchema_ToLegacy_RequiresInputAndOutput()
    {
        // Act
        var schema = RetocCommandSchemaProvider.GetSchema(_defaultOptions);
        var toLegacy = schema.Commands.First(c => c.CommandType == "ToLegacy");

        // Assert
        Assert.Equal(2, toLegacy.RequiredFields.Count);
        Assert.Contains(toLegacy.RequiredFields, f => f.FieldName == "InputPath");
        Assert.Contains(toLegacy.RequiredFields, f => f.FieldName == "OutputPath");
    }

    [Fact]
    public void GetSchema_Info_DoesNotRequireOutput()
    {
        // Act
        var schema = RetocCommandSchemaProvider.GetSchema(_defaultOptions);
        var info = schema.Commands.First(c => c.CommandType == "Info");

        // Assert - Info only requires input
        Assert.Single(info.RequiredFields);
        Assert.Contains(info.RequiredFields, f => f.FieldName == "InputPath");
        Assert.DoesNotContain(info.RequiredFields, f => f.FieldName == "OutputPath");
    }
}
```

---

#### File: `tests/Aris.Core.Tests/Infrastructure/StreamingProcessRunnerTests.cs` (NEW)

**Purpose:** Test streaming behavior using PowerShell deterministic scripts (Windows-only).

```csharp
using Aris.Infrastructure.Configuration;
using Aris.Infrastructure.Process;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;

namespace Aris.Core.Tests.Infrastructure;

public class StreamingProcessRunnerTests
{
    private readonly StreamingProcessRunner _runner;

    public StreamingProcessRunnerTests()
    {
        var options = Options.Create(new RetocOptions
        {
            MaxStreamingOutputBytes = 10 * 1024 * 1024,
            MaxStreamingOutputLines = 100000
        });

        _runner = new StreamingProcessRunner(NullLogger<StreamingProcessRunner>.Instance, options);
    }

    [Fact]
    public async Task ExecuteAsync_DeliversStdOutLinesInRealTime()
    {
        // Arrange
        var receivedLines = new List<string>();

        // PowerShell script that outputs 5 lines with delays
        var psScript = "1..5 | ForEach-Object { Write-Host \"Line $_\"; Start-Sleep -Milliseconds 50 }";

        // Act
        await _runner.ExecuteAsync(
            "powershell.exe",
            $"-NoProfile -Command \"{psScript}\"",
            onStdOutLine: async (line) => { receivedLines.Add(line); await Task.CompletedTask; },
            onStdErrLine: async (line) => await Task.CompletedTask
        );

        // Assert
        Assert.Equal(5, receivedLines.Count);
        Assert.Contains("Line 1", receivedLines[0]);
        Assert.Contains("Line 5", receivedLines[4]);
    }

    [Fact]
    public async Task ExecuteAsync_StdErrDeliversSeparately()
    {
        // Arrange
        var stdOutLines = new List<string>();
        var stdErrLines = new List<string>();

        var psScript = "Write-Host 'stdout line'; Write-Error 'stderr line'";

        // Act
        await _runner.ExecuteAsync(
            "powershell.exe",
            $"-NoProfile -Command \"{psScript}\"",
            onStdOutLine: async (line) => { stdOutLines.Add(line); await Task.CompletedTask; },
            onStdErrLine: async (line) => { stdErrLines.Add(line); await Task.CompletedTask; }
        );

        // Assert
        Assert.NotEmpty(stdOutLines);
        Assert.NotEmpty(stdErrLines);
        Assert.Contains(stdOutLines, l => l.Contains("stdout line"));
        Assert.Contains(stdErrLines, l => l.Contains("stderr line"));
    }

    [Fact]
    public async Task ExecuteAsync_TimeoutKillsProcess()
    {
        // Arrange - PowerShell script that sleeps for 10 seconds
        var psScript = "Start-Sleep -Seconds 10";

        // Act & Assert
        await Assert.ThrowsAsync<TimeoutException>(async () =>
        {
            await _runner.ExecuteAsync(
                "powershell.exe",
                $"-NoProfile -Command \"{psScript}\"",
                onStdOutLine: async (line) => await Task.CompletedTask,
                onStdErrLine: async (line) => await Task.CompletedTask,
                timeoutSeconds: 1
            );
        });
    }

    [Fact]
    public async Task ExecuteAsync_CancellationKillsProcess()
    {
        // Arrange
        var cts = new CancellationTokenSource();
        var psScript = "Start-Sleep -Seconds 10";

        // Act
        var task = _runner.ExecuteAsync(
            "powershell.exe",
            $"-NoProfile -Command \"{psScript}\"",
            onStdOutLine: async (line) => await Task.CompletedTask,
            onStdErrLine: async (line) => await Task.CompletedTask,
            cancellationToken: cts.Token
        );

        await Task.Delay(200); // Let process start
        cts.Cancel();

        // Assert
        await Assert.ThrowsAnyAsync<OperationCanceledException>(async () => await task);
    }

    [Fact]
    public async Task ExecuteAsync_RespectsMaxOutputLines()
    {
        // Arrange - Options with low line limit
        var options = Options.Create(new RetocOptions
        {
            MaxStreamingOutputBytes = 10 * 1024 * 1024,
            MaxStreamingOutputLines = 10
        });

        var runner = new StreamingProcessRunner(NullLogger<StreamingProcessRunner>.Instance, options);
        var receivedLines = new List<string>();

        // PowerShell script that outputs 100 lines
        var psScript = "1..100 | ForEach-Object { Write-Host \"Line $_\" }";

        // Act
        await runner.ExecuteAsync(
            "powershell.exe",
            $"-NoProfile -Command \"{psScript}\"",
            onStdOutLine: async (line) => { receivedLines.Add(line); await Task.CompletedTask; },
            onStdErrLine: async (line) => await Task.CompletedTask
        );

        // Assert - Should stop after 10 lines (max limit)
        Assert.True(receivedLines.Count <= 10, $"Expected <= 10 lines, got {receivedLines.Count}");
    }

    [Fact]
    public async Task ExecuteAsync_ReturnsProcessResult()
    {
        // Arrange
        var psScript = "Write-Host 'test'; exit 0";

        // Act
        var result = await _runner.ExecuteAsync(
            "powershell.exe",
            $"-NoProfile -Command \"{psScript}\"",
            onStdOutLine: async (line) => await Task.CompletedTask,
            onStdErrLine: async (line) => await Task.CompletedTask
        );

        // Assert
        Assert.Equal(0, result.ExitCode);
        Assert.True(result.Success);
        Assert.NotEmpty(result.StdOut);
        Assert.True(result.Duration.TotalMilliseconds > 0);
    }
}
```

**Note:** Tests use PowerShell with deterministic scripts (fixed output lines, sleep durations). This is Windows-specific but acceptable since the repo targets Windows 10/11 x64 only.

---

### Frontend Tests (if framework exists)

#### File: `frontend/src/api/retocClient.test.ts`

```typescript
import { buildRetocCommand, streamRetocExecution } from './retocClient';
import type { RetocBuildCommandRequest, RetocStreamRequest, RetocStreamEvent } from '../types/contracts';

describe('retocClient', () => {
  beforeEach(() => {
    global.fetch = jest.fn();
  });

  it('buildRetocCommand makes POST request to /api/retoc/build', async () => {
    const mockResponse = {
      executablePath: 'C:\\retoc.exe',
      arguments: 'to-legacy "C:\\Input" "C:\\Output"',
      commandLine: '"C:\\retoc.exe" to-legacy "C:\\Input" "C:\\Output"'
    };

    (global.fetch as jest.Mock).mockResolvedValue({
      ok: true,
      json: async () => mockResponse
    });

    const request: RetocBuildCommandRequest = {
      commandType: 'ToLegacy',
      inputPath: 'C:\\Input',
      outputPath: 'C:\\Output'
    };

    const result = await buildRetocCommand(request);

    expect(result).toEqual(mockResponse);
    expect(global.fetch).toHaveBeenCalledWith(
      expect.stringContaining('/api/retoc/build'),
      expect.objectContaining({ method: 'POST' })
    );
  });

  it('streamRetocExecution parses NDJSON stream', async () => {
    const ndjsonLines = [
      '{"eventType":"status","status":"started"}',
      '{"eventType":"line","stream":"stdout","text":"Processing...","timestamp":"2025-12-24T10:00:00Z"}',
      '{"eventType":"status","status":"completed","exitCode":0,"duration":"00:00:05"}'
    ];

    const ndjsonStream = ndjsonLines.join('\n') + '\n';
    const encoder = new TextEncoder();
    const stream = new ReadableStream({
      start(controller) {
        controller.enqueue(encoder.encode(ndjsonStream));
        controller.close();
      }
    });

    (global.fetch as jest.Mock).mockResolvedValue({
      ok: true,
      body: stream
    });

    const events: RetocStreamEvent[] = [];
    const request: RetocStreamRequest = {
      commandType: 'ToLegacy',
      inputPath: 'C:\\Input',
      outputPath: 'C:\\Output'
    };

    await streamRetocExecution(request, (event) => {
      events.push(event);
    });

    expect(events.length).toBe(3);
    expect(events[0].eventType).toBe('status');
    expect(events[1].eventType).toBe('line');
    expect(events[2].eventType).toBe('status');
  });

  it('streamRetocExecution handles abort signal', async () => {
    const controller = new AbortController();

    (global.fetch as jest.Mock).mockImplementation(() => {
      controller.abort();
      return Promise.reject(new DOMException('Aborted', 'AbortError'));
    });

    const request: RetocStreamRequest = {
      commandType: 'ToLegacy',
      inputPath: 'C:\\Input',
      outputPath: 'C:\\Output'
    };

    await expect(
      streamRetocExecution(request, () => {}, controller.signal)
    ).rejects.toThrow('Aborted');
  });
});
```

---

## 8. Verification Checklist

### Build & Test Commands

**Backend:**
```bash
cd G:\Development\ARIS(CS)\ARIS
dotnet build
dotnet test
```

**Expected:** All projects build successfully, all tests pass (including new `RetocCommandSchemaProviderTests` and `StreamingProcessRunnerTests`).

---

**Frontend:**
```bash
cd G:\Development\ARIS(CS)\ARIS\frontend
npm install
npm run build
```

**Expected:** No TypeScript errors, successful production build to `dist/`.

---

**Run Backend:**
```bash
cd G:\Development\ARIS(CS)\ARIS\src\Aris.Hosting
dotnet run
```

**Expected:** Backend starts on configured port (e.g., http://localhost:5000).

---

**Run Frontend Dev Server:**
```bash
cd G:\Development\ARIS(CS)\ARIS\frontend
npm run dev
```

**Expected:** Frontend dev server starts (e.g., http://localhost:5173), connects to backend.

---

### Manual Verification Scenarios

#### Scenario 1: Simple Mode - Pack (to-zen)

1. Navigate to Retoc page
2. Ensure "Simple" mode is active
3. Fill in Pack section:
   - Modified UAsset Directory: `C:\TestData\Modified`
   - Mod Output Directory: `C:\TestData\Output`
   - Mod Name: `TestMod`
   - UE Version: `UE5_6`
4. **Verify:** Command preview updates and shows: `"<retoc.exe>" to-zen --version UE5_6 "C:\TestData\Modified" "C:\TestData\Output\TestMod.utoc"`
5. Click "Build Mod"
6. **Verify:** Console log appears, shows "RUNNING" status
7. **Verify:** Live stdout lines appear as Retoc processes files (streamed via NDJSON)
8. **Verify:** On completion, status changes to "COMPLETED" with exit code 0
9. **Verify:** "Build Mod" button re-enables

**Expected Behavior:**
- Preview matches executed command exactly
- Streaming output appears in real-time (not buffered)
- Completion state is clear (green/success indicator)

---

#### Scenario 2: Simple Mode - Unpack (to-legacy)

1. Fill in Unpack section:
   - Base Game Paks Directory: `C:\Game\Paks`
   - Extracted Output Directory: `C:\TestData\Extracted`
2. **Verify:** Command preview shows: `"<retoc.exe>" to-legacy "C:\Game\Paks" "C:\TestData\Extracted"`
3. Click "Extract Files"
4. **Verify:** Streaming output, completion state

---

#### Scenario 3: Advanced Mode - ToLegacy

1. Switch to "Advanced" mode
2. Select "To Legacy (Zen → PAK)" from command selector
3. **Verify:** Dynamic fields appear (Input Path, Output Path only - no EngineVersion for ToLegacy)
4. Fill fields:
   - Input Path: `C:\IoStore\global.utoc`
   - Output Path: `C:\Extracted`
5. **Verify:** Command preview updates to show to-legacy command
6. Click "Execute"
7. **Verify:** Streaming output via NDJSON, completion

---

#### Scenario 4: Advanced Mode - Get Command with ChunkIndex

1. Select "Get" from command selector
2. **Verify:** Fields appear: Input Path (required), Chunk Index (required Integer field)
3. Fill fields:
   - Input Path: `C:\IoStore\global.utoc`
   - Chunk Index: `42`
4. **Verify:** Command preview includes chunk index as argument
5. Click "Execute"
6. **Verify:** Retoc executes `get <input> 42`, stdout shows chunk data

---

#### Scenario 5: Advanced Mode - Info Command (No Output Path)

1. Select "Info" from command selector
2. **Verify:** Only "Input Path" field appears (no Output Path in required or optional fields)
3. Fill Input Path: `C:\IoStore\global.utoc`
4. Click "Execute"
5. **Verify:** stdout shows container info (no file output)

---

#### Scenario 6: Advanced Mode - Global Options

1. Select any command (e.g., "ToLegacy")
2. **Verify:** Global options section appears with:
   - AES Encryption Key (String field)
   - Container Header Version (Enum dropdown)
   - TOC Version (Enum dropdown)
3. Fill AES Key: `0x1234567890ABCDEF`
4. **Verify:** Command preview includes `--aes-key 0x1234567890ABCDEF` before subcommand

---

#### Scenario 7: Advanced Mode - Allowlisted Flags (if configured)

**Setup:** Configure `RetocOptions.AllowedAdditionalArgs = ["--verbose", "--no-warnings"]` in appsettings.

1. **Verify:** Schema includes allowlisted flags as boolean checkboxes:
   - Verbose (checkbox)
   - No Warnings (checkbox)
2. Check "Verbose"
3. **Verify:** Command preview includes `--verbose` at end
4. Check "No Warnings"
5. **Verify:** Command preview includes `--verbose --no-warnings`

**Note:** If `AllowedAdditionalArgs` is empty (default), no flag checkboxes appear.

---

#### Scenario 8: Advanced Mode - Help Modal

1. Click "Help" button
2. **Verify:** Modal opens with Markdown-rendered Retoc help
3. **Verify:** Help content is readable (code-fenced output from `retoc --help`)
4. Close modal

---

#### Scenario 9: Error Handling - Validation (Non-Absolute Path)

1. Simple Mode - Pack
2. Fill "Modified UAsset Directory" with relative path: `ModFolder`
3. Click "Build Mod"
4. **Verify:** Error appears immediately (either inline or in error event from stream): "InputPath must be absolute"
5. **Verify:** HTTP 400 or NDJSON error event with `code: "VALIDATION_ERROR"`

---

#### Scenario 10: Error Handling - Retoc Failure

1. Simple Mode - Pack
2. Fill with invalid paths (non-existent directory)
3. Click "Build Mod"
4. **Verify:** Retoc starts, stderr shows errors (red lines in console)
5. **Verify:** Completion status is "FAILED" with exit code > 0
6. **Verify:** Console log shows stderr lines in red

---

#### Scenario 11: Error Handling - Timeout

**Setup:** Temporarily lower timeout in backend or use very large input directory.

1. Advanced Mode
2. Select "ToLegacy" with a large input
3. Set timeout to 5 seconds (if timeout field exposed, or modify RetocOptions.DefaultTimeoutSeconds)
4. Click "Execute"
5. **Verify:** After 5 seconds, streaming stops
6. **Verify:** Status event shows `"status":"failed"` with exitCode -1
7. **Verify:** Error message indicates timeout

---

#### Scenario 12: Streaming Cancellation (Abort)

1. Simple Mode - Pack (use large directory for long execution)
2. Click "Build Mod"
3. **Verify:** Streaming starts, console log shows lines
4. Click "Cancel" button (if implemented) or close browser tab
5. **Verify:** Backend detects `HttpContext.RequestAborted` and kills process
6. **Verify:** No zombie Retoc processes remain (check Task Manager)

---

#### Scenario 13: Schema Completeness Verification

1. Advanced Mode
2. Iterate through all 13 commands in dropdown:
   - Manifest, Info, List, Verify, Unpack, UnpackRaw, PackRaw, ToLegacy, ToZen, Get, DumpTest, GenScriptObjects, PrintScriptObjects
3. For each command, **verify:**
   - Correct required fields appear
   - Correct optional fields appear
   - Field types match (Path, Enum, Integer, String, Boolean)
   - Enum dropdowns have correct values (e.g., EngineVersion for ToZen)
   - Integer fields have min/max constraints (e.g., ChunkIndex >= 0)
   - Help text is present and useful

---

#### Scenario 14: Preview == Execution Guarantee

1. Simple Mode - Pack
2. Fill form
3. Copy command preview string verbatim
4. Click "Build Mod"
5. Watch backend logs for actual executed command (check `ILogger` output)
6. **Verify:** Logged command matches copied preview exactly (character-for-character, ignoring whitespace/quoting differences if normalized)

---

#### Scenario 15: NDJSON Parsing Correctness

1. Simple Mode - Pack
2. Execute with verbose output (if verbose flag enabled via AllowedAdditionalArgs)
3. **Verify:** Each stdout/stderr line arrives as separate `{"eventType":"line",...}` NDJSON object
4. **Verify:** No partial lines or malformed JSON
5. **Verify:** Frontend console log shows all lines in order
6. **Verify:** Final status event arrives after all line events

---

### Acceptance Criteria Summary

**Must Pass:**
- [ ] All backend tests pass (`dotnet test`)
- [ ] All frontend tests pass (if applicable, `npm test`)
- [ ] Backend builds without errors (`dotnet build`)
- [ ] Frontend builds without TypeScript errors (`npm run build`)
- [ ] Simple Mode - Pack workflow completes successfully
- [ ] Simple Mode - Unpack workflow completes successfully
- [ ] Advanced Mode - All 13 commands can be selected and executed
- [ ] Advanced Mode - Get command requires ChunkIndex and includes it in args
- [ ] Advanced Mode - Info command does not require OutputPath
- [ ] Advanced Mode - Global options (AES key, version overrides) work for all commands
- [ ] Advanced Mode - Allowlisted flags appear as checkboxes (if configured)
- [ ] Command preview updates in real-time (debounced 300ms)
- [ ] Preview matches executed command exactly (verified via logs)
- [ ] NDJSON streaming delivers stdout/stderr lines in real-time (not buffered)
- [ ] Execution state (running/completed/failed) is clear in UI
- [ ] Validation errors are surfaced with 400 status or error event
- [ ] Dependency missing errors surfaced with 503 status
- [ ] Timeout enforced (kills process, shows failed status with exitCode -1)
- [ ] Cancellation works (abort signal kills process, no zombies)
- [ ] Help modal renders Markdown help content
- [ ] Schema endpoint returns all 13 commands with correct metadata
- [ ] Schema tests fail if RetocCommandType enum value missing from schema
- [ ] Schema tests fail if schema field doesn't map to RetocCommand property
- [ ] Console log supports long-running output without crashing/freezing (respects 10 MB / 100k lines limits)
- [ ] Buttons are disabled during execution and re-enabled on completion
- [ ] No memory leaks from unbounded streaming (verified by bounded buffers)
- [ ] No free-form additionalArgs field in UI (all options are structured)

---

## Summary

This revised plan provides a complete, file-by-file roadmap for implementing Simple | Advanced mode toggle for the Retoc tool in ARIS, with all blocking fixes addressed:

1. **Streaming transport:** NDJSON over fetch ReadableStream (not EventSource)
2. **Advanced Mode:** 100% functionality via structured fields (no free-form additionalArgs)
3. **Help endpoint:** Routes through adapter's existing tool resolution pattern
4. **Buffering/limits:** Aligned with ProcessRunner's 10 MB / 100k lines, configurable via RetocOptions
5. **Schema drift prevention:** Tests verify enum completeness and field mapping
6. **Streaming tests:** Use PowerShell deterministic scripts (Windows-only, repo-consistent)

All contracts, endpoints, components, and tests are explicitly defined with code examples citing existing repo patterns. Implementation can proceed file-by-file following the delta list and contract specifications.
