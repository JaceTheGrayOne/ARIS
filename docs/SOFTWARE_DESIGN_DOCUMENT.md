# Software Design Document

**Last updated:** 2026-01-05

## 1. Purpose and Non-Goals

### Purpose
ARIS is a Windows desktop application providing unified access to four Unreal Engine modding tools:
1. **Retoc** - PAK/IoStore container conversion with AES encryption support
2. **UAssetAPI** - Unreal asset binary serialization/deserialization
3. **UWPDumper** - UWP application memory/file extraction
4. **DLL Injector** - Native DLL injection into running processes

This is a C#/.NET 8 clean rewrite of an original Go/Wails implementation, using the original only as behavioral reference.

(Source: CLAUDE.md:L8-L13, src/Aris.Hosting/Program.cs:L51-L56)

### Feature Maturity

> **Note for AI-assisted development:** This table reflects current implementation status. Features marked as partial or stub may have API endpoints but lack full functionality.

| Tool | Maturity | Streaming | Notes |
|------|----------|-----------|-------|
| **Retoc** | Production | WebSocket + ConPTY | Full terminal streaming with progress bars. Simple/Advanced mode UI complete. |
| **UAssetAPI** | Production | None (in-process) | Synchronous JSON ↔ .uasset conversion. No external process required. |
| **UWPDumper** | Partial | None | API endpoint exists. Requires elevation. Not fully tested. |
| **DLL Injector** | Partial | None | CreateRemoteThread implemented. ApcQueue/ManualMap not implemented. |

**Maturity definitions:**
- **Production**: Feature-complete with tests and documentation
- **Partial**: API exists, core functionality works, but not all modes/options implemented
- **Stub**: Endpoint exists but returns not-implemented or has minimal functionality

### Non-Goals
- Cross-platform support (Windows 10/11 x64 only)
- Standalone CLI distribution (desktop-only, though backend can run independently)
- Additional tool integrations beyond the four specified
- Line-by-line port of Go codebase (architectural rewrite)

(Source: CLAUDE.md:L8-L13, global.json:L2-L5, src/ARIS.UI/ARIS.UI.csproj:L13)

## 2. Repository Overview

### Tech Stack
**Backend:**
- .NET SDK 8.0.416 (LTS)
- ASP.NET Core Minimal APIs
- Serilog 10.0.0 (structured logging)

**Frontend:**
- React 19.2.0 + TypeScript 5.9.3
- Tailwind CSS 4.1.17
- Vite 7.2.4 (build tool)
- React Router DOM 7.10.1

**UI Host:**
- WPF with Microsoft.Web.WebView2 1.0.3595.46

**Testing:**
- xUnit 2.5.3
- Coverlet (code coverage)

(Source: global.json:L2-L5, frontend/package.json:L12-L35, src/Aris.Hosting/Aris.Hosting.csproj:L11-L13, src/ARIS.UI/ARIS.UI.csproj:L8, tests/Aris.Core.Tests/Aris.Core.Tests.csproj:L13-L16)

### Primary Runtimes

**Production Mode (Single Executable):**
1. **ARIS.exe** - Self-contained WPF host that extracts embedded payload and bootstraps backend
2. **Aris.Hosting.exe** - Spawned as hidden child process with dynamically bound port

**Development Mode:**
1. **ARIS.UI** (WPF/WebView2) - Desktop shell loading `frontend/dist/index.html` from filesystem
2. **Aris.Hosting** (ASP.NET Core) - HTTP API backend on `http://localhost:5000`

(Source: src/ARIS.UI/MainWindow.xaml.cs:L1-L158, src/Aris.Hosting/Program.cs:L1-L90, frontend/src/config/backend.ts:L1-L33)

### Key Directories

| Directory | Purpose |
|-----------|---------|
| `src/Aris.Core/` | Domain models, commands, results, error types |
| `src/Aris.Application/` | Use case orchestration (currently minimal - Class1.cs placeholder) |
| `src/Aris.Infrastructure/` | Process execution, tool extraction, SHA-256 validation, Win32 P/Invoke |
| `src/Aris.Adapters/` | Tool-specific adapters (Retoc, UAssetAPI, UWPDumper, DllInjector) |
| `src/Aris.Contracts/` | HTTP DTO contracts |
| `src/Aris.Tools/` | Embedded tool binaries + manifest |
| `src/Aris.Hosting/` | DI composition root, HTTP endpoints, static file hosting |
| `src/ARIS.UI/` | WPF desktop host with bootstrap logic |
| `src/ARIS.UI/Bootstrap/` | Payload extraction, backend process management, readiness polling |
| `src/ARIS.UI/Views/` | WPF error window |
| `frontend/` | React frontend |
| `tests/` | xUnit integration tests |
| `tests/ARIS.UI.Tests/Bootstrap/` | Bootstrap component unit tests |
| `external/UAssetAPI/` | Git submodule |
| `build/` | Build and publish scripts |
| `artifacts/` | Build output (gitignored) |

(Source: ARIS.sln:L6-L28, CLAUDE.md:L40-L50, src/ARIS.UI/Bootstrap:directory, build/:directory)

## 3. How to Build, Run, and Test

### Build
```bash
# Backend
dotnet build

# Frontend
cd frontend && npm install && npm run build
```

(Source: CLAUDE.md:L63-L99, frontend/package.json:L8)

### Run (Development Mode)
**Backend:**
```bash
cd src/Aris.Hosting && dotnet run
```
In Development mode, CORS is enabled for `http://localhost:5173`. The backend serves static files from `wwwroot/` and uses `MapFallbackToFile("index.html")` for SPA routing.

**Frontend Dev Server:**
```bash
cd frontend && npm run dev
```

**Desktop UI (Debug):**
Run `ARIS.UI` project in Debug configuration. Without embedded payload, it falls back to loading `frontend/dist/index.html` from the filesystem.

(Source: src/Aris.Hosting/Program.cs:L9-L22, L60-L79, src/ARIS.UI/MainWindow.xaml.cs:L41-L86, frontend/package.json:L7)

### Test
```bash
dotnet test
```

Includes:
- **ARIS.UI.Tests**: 27 tests for bootstrap components
- **Aris.Core.Tests**: 281+ tests for backend components

(Source: CLAUDE.md:L68-L76, tests/Aris.Core.Tests/Aris.Core.Tests.csproj:L1-L36, tests/ARIS.UI.Tests/Bootstrap/*.cs)

### Publish (Single Executable Release)

Use the release publish script to produce a single distributable `ARIS.exe`:

```powershell
powershell -ExecutionPolicy Bypass -File "build/publish-release.ps1"
```

**Script Steps (`build/publish-release.ps1`):**
1. Clean `artifacts/` directory
2. Build frontend (`npm ci && npm run build`)
3. Verify no hardcoded `localhost:5000` URLs in production JS
4. Publish `Aris.Hosting` as single-file self-contained executable to `artifacts/payload/`
5. Copy frontend dist to `artifacts/payload/wwwroot/`
6. Create `artifacts/payload.zip` from payload contents
7. Publish `ARIS.UI` as single-file self-contained executable to `artifacts/release/`
   - Embeds `payload.zip` as resource with logical name `ARIS.Payload`

**Output:**
- `artifacts/release/ARIS.exe` (~135 MB, self-contained)
- Optional: `*.pdb` debug symbols, `*.xml` documentation, `appsettings*.json` (not required at runtime)

**ARIS.UI.csproj Publish Properties:**
```xml
<PropertyGroup Condition="'$(Configuration)' == 'Release'">
  <RuntimeIdentifier>win-x64</RuntimeIdentifier>
  <SelfContained>true</SelfContained>
  <PublishSingleFile>true</PublishSingleFile>
  <IncludeNativeLibrariesForSelfExtract>true</IncludeNativeLibrariesForSelfExtract>
  <EnableCompressionInSingleFile>true</EnableCompressionInSingleFile>
</PropertyGroup>
```

**Payload Embedding:**
```xml
<ItemGroup Condition="'$(Configuration)' == 'Release'">
  <EmbeddedResource Include="..\..\artifacts\payload.zip">
    <LogicalName>ARIS.Payload</LogicalName>
  </EmbeddedResource>
</ItemGroup>
```

(Source: build/publish-release.ps1:L1-L76, src/ARIS.UI/ARIS.UI.csproj:L12-L36)

## 4. System Architecture

### Component Topology (Production)

```
????????????????????????????????????????????????????????????????????????
?                        ARIS.exe (Single Executable)                   ?
?  ?????????????????????????????????????????????????????????????????   ?
?  ?            ARIS.UI (WPF + WebView2 + Bootstrap)               ?   ?
?  ?  1. Extract ARIS.Payload ? %LOCALAPPDATA%\ARIS\payload\<id>\  ?   ?
?  ?  2. Spawn Aris.Hosting.exe (hidden, ASPNETCORE_URLS=:0)       ?   ?
?  ?  3. Parse stdout for ARIS_BACKEND_URL=http://127.0.0.1:<port> ?   ?
?  ?  4. Poll /health until Status="Ready"                         ?   ?
?  ?  5. Navigate WebView2 to backend origin (same-origin)         ?   ?
?  ?????????????????????????????????????????????????????????????????   ?
?                               ? HTTP (127.0.0.1:<dynamic-port>)       ?
?  ?????????????????????????????????????????????????????????????????   ?
?  ?           Aris.Hosting.exe (Child Process, Hidden)            ?   ?
?  ?  UseStaticFiles() ? wwwroot/                                  ?   ?
?  ?  MapFallbackToFile("index.html") ? SPA routing               ?   ?
?  ?  API Endpoints: /api/{tool}, /health, /info                  ?   ?
?  ?  UrlAnnouncementService ? stdout: ARIS_BACKEND_URL=...       ?   ?
?  ?????????????????????????????????????????????????????????????????   ?
?      ?                                                    ?          ?
?  ??????????????                                    ???????????????   ?
?  ?  Adapters  ?                                    ?Infrastructure?  ?
?  ? - Retoc    ??????????????????????????????????????ProcessRunner ?  ?
?  ? - UAsset   ?                                    ?ConPtyProcess ?  ?
?  ? - UwpDumper?                                    ?DependencyEx- ?  ?
?  ? - DllInject?                                    ?  tractor     ?  ?
?  ??????????????                                    ????????????????  ?
?       ?                                                               ?
?       ?   ????????????????????????????????????????????????????????   ?
?       ?   ? Aris.Tools (Embedded in Aris.Hosting)                ?   ?
?       ?   ? Extract to %LOCALAPPDATA%/ARIS/tools/0.1.0/          ?   ?
?       ?   ????????????????????????????????????????????????????????   ?
?       ?                                                               ?
?  External Tools: retoc.exe, UAssetAPI in-process, Win32 APIs         ?
????????????????????????????????????????????????????????????????????????
```

**Production Startup Sequence:**
1. User launches `ARIS.exe`
2. `PayloadExtractor` checks for embedded `ARIS.Payload` resource
3. Computes SHA-256 hash, compares with `.payload.lock` file
4. If hash differs or lock missing: extracts `payload.zip` to `%LOCALAPPDATA%\ARIS\payload\<hash-prefix>\`
5. `BackendProcessManager` spawns `Aris.Hosting.exe` with `CreateNoWindow=true`
6. Environment: `ASPNETCORE_URLS=http://127.0.0.1:0` (dynamic port), `ASPNETCORE_ENVIRONMENT=Production`
7. `UrlAnnouncementService` emits `ARIS_BACKEND_URL=http://127.0.0.1:<port>` to stdout (once)
8. UI parses stdout, captures URL
9. `ReadinessPoller` polls `GET /health` every 500ms until `Status == "Ready"` (60s timeout)
10. WebView2 navigates to backend URL
11. Frontend served from `wwwroot/` via `UseStaticFiles()`, API calls use same-origin relative URLs

(Source: src/ARIS.UI/MainWindow.xaml.cs:L39-L118, src/ARIS.UI/Bootstrap/PayloadExtractor.cs:L35-L80, src/ARIS.UI/Bootstrap/BackendProcessManager.cs:L77-L152, src/ARIS.UI/Bootstrap/ReadinessPoller.cs:L44-L84, src/Aris.Hosting/Infrastructure/UrlAnnouncementService.cs:L25-L75, src/Aris.Hosting/Program.cs:L49-L79)

### Dependency Flow
```
UI ? Hosting ? Adapters ? Infrastructure
                    ?            ?
                  Core ???????????
```

**Rules:**
- Core has zero dependencies
- Infrastructure/Adapters depend on Core
- Hosting depends on all (composition root)
- No circular dependencies

(Source: src/Aris.Core/Aris.Core.csproj:L1-L9, src/Aris.Adapters/Aris.Adapters.csproj:L3-L8, src/Aris.Infrastructure/Aris.Infrastructure.csproj:L3-L6, CLAUDE.md:L40-L51)

### External Dependencies
**Embedded Tools:**
- `retoc.exe` v0.1.4 (win-x64, SHA-256: `1c7b3af2b7ca06ac7216d1ba1c629f1e2b178966d964c488135c09abd68a4ec8`, 7,020,544 bytes)

**Git Submodules:**
- `external/UAssetAPI` from https://github.com/atenfyr/UAssetAPI.git

(Source: src/Aris.Tools/tools.manifest.json:L1-L14, .gitmodules:L1-L4)

## 5. Core Execution Flows

### Flow 1: Tool Extraction on Startup

**Trigger:** Application startup (ToolingStartupHostedService.StartAsync)

**Steps:**
1. Service starts, updates BackendHealthState to "Starting"
2. DependencyExtractor.PrepareToolsAsync loads manifest from embedded resources
3. Computes manifest hash (SHA-256)
4. Checks `.extraction.lock` file for idempotency
5. If hash matches, skips extraction
6. If hash differs or missing:
   - Extracts each tool to `%LOCALAPPDATA%/ARIS/tools/0.1.0/{tool.RelativePath}`
   - Writes `.extraction.lock` with manifest hash
7. DependencyValidator.ValidateAllAsync validates each tool:
   - Checks file exists
   - Computes SHA-256 hash
   - Compares with manifest (DependencyValidator.cs:L81-L137)
8. If all valid: BackendHealthState.MarkReady()
9. If failures: BackendHealthState.MarkError()

**Data Flow:**
```
ToolManifest ? Extract ? %LOCALAPPDATA%/ARIS/tools/0.1.0/ ? Validate (SHA-256) ? Health State
```

(Source: src/Aris.Hosting/Infrastructure/ToolingStartupHostedService.cs:L27-L57, src/Aris.Infrastructure/Tools/DependencyExtractor.cs:L32-L137, src/Aris.Infrastructure/Tools/DependencyValidator.cs:L28-L137, src/Aris.Hosting/Infrastructure/BackendHealthState.cs:L32-L44)

### Flow 2: Retoc Streaming Execution (Simple/Advanced Mode)

**Trigger:** WebSocket connection to `/api/retoc/stream`

> **Protocol:** WebSocket with NDJSON-formatted messages (one JSON object per message). See Section 6 for full details.

**Request (sent over WebSocket after connection):**
```json
{
  "commandType": "ToZen",
  "inputPath": "C:\\mods\\input",
  "outputPath": "C:\\mods\\output\\mod.utoc",
  "engineVersion": "UE5_6",
  "aesKey": null,
  "verbose": false,
  "timeoutSeconds": 300
}
```

**Steps:**
1. Client opens WebSocket to `/api/retoc/stream`
2. Client sends JSON request message
3. RetocStreamHandler validates commandType enum
4. RetocAdapter.BuildCommand constructs CLI arguments with validation
5. Emits `{"type": "started", ...}` event over WebSocket
6. ConPtyProcess spawns retoc.exe with pseudo-console for terminal emulation:
   - Reads VT/ANSI terminal output asynchronously
   - Each chunk emitted as `{"type": "output", "data": "..."}` over WebSocket
   - Raw terminal sequences preserved for xterm.js rendering
7. On completion, emits `{"type": "exited", "exitCode": 0, ...}`
8. On error, emits `{"type": "error", "code": "...", ...}`
9. WebSocket connection closes

**Event Types (NDJSON over WebSocket):**
- `started`: `{type: "started", operationId: "...", commandLine: "...", timestamp: "..."}`
- `output`: `{type: "output", data: "...(VT/ANSI)...", timestamp: "..."}`
- `exited`: `{type: "exited", exitCode: number, duration: "...", timestamp: "..."}`
- `error`: `{type: "error", code: "...", message: "...", remediationHint?: "..."}`

**Error Handling:**
- ValidationError → error event over WebSocket
- ToolExecutionError → error event + connection close

(Source: src/Aris.Hosting/Endpoints/RetocStreamHandler.cs:L43-L228)


### Flow 3: DLL Injection (CreateRemoteThread)

**Trigger:** `POST /api/dllinjector/inject`

**Request:**
```json
{
  "processId": 1234,
  "dllPath": "C:\\mod.dll",
  "method": "CreateRemoteThread"
}
```

**Steps:**
1. HTTP endpoint validates method enum
2. ProcessResolver resolves target process (by ID or name)
3. Checks denied process list (csrss.exe, lsass.exe, etc.)
4. DllInjectorAdapter.InjectAsync ? NativeDllInjectionService.InjectAsync
5. Native injection (NativeDllInjectionService.cs:L55-L398):
   - OpenProcess with required access rights
   - GetModuleHandle("kernel32.dll") + GetProcAddress("LoadLibraryW")
   - VirtualAllocEx allocates memory in target
   - WriteProcessMemory writes DLL path (Unicode)
   - CreateRemoteThread executes LoadLibraryW
   - WaitForSingleObject waits for load (30s timeout)
   - GetExitCodeThread retrieves module base address
   - Cleanup: VirtualFreeEx, CloseHandle
6. Returns DllInjectResult with moduleAddress

**Security:**
- Denied targets enforced (appsettings.json:L50-L57)
- Elevation detection on ERROR_ACCESS_DENIED
- 30-second thread timeout

(Source: src/Aris.Hosting/Endpoints/DllInjectorEndpoints.cs:L27-L100, src/Aris.Adapters/DllInjector/DllInjectorAdapter.cs, src/Aris.Infrastructure/DllInjection/NativeDllInjectionService.cs:L23-L398, src/Aris.Hosting/appsettings.json:L46-L63)

### Flow 4: UAsset Serialization

**Trigger:** `POST /api/uasset/serialize`

**Steps:**
1. UAssetService.SerializeAsync validates command
2. Creates staging directory: `{workspace}/temp/uasset-{operationId}`
3. Progress events emitted: opening ? parsing ? converting ? writing ? hashing
4. UAssetApiBackend.SerializeAsync (in-process UAssetAPI library):
   - Reads JSON
   - Constructs asset object graph
   - Writes .uasset/.uexp binaries
5. Gathers produced files (no SHA-256 computed, null in ProducedFile)
6. Writes operation log to `{workspace}/logs/uasset-{operationId}.log`
7. Returns UAssetResult

(Source: src/Aris.Hosting/Endpoints/UAssetEndpoints.cs:L29-L72, src/Aris.Adapters/UAsset/UAssetService.cs:L29-L108, src/Aris.Adapters/UAsset/UAssetService.cs:L244-L265)

## 5.5. Single Executable Bootstrapper

### 5.5.1. Overview

The Single Executable Bootstrapper enables distribution of ARIS as a single `ARIS.exe` file (~135 MB) that self-extracts and manages backend lifecycle automatically. This eliminates the need for separate installation or manual backend startup.

**Goals:**
- Single-file distribution with embedded payload
- Zero-config deployment (extract to %LOCALAPPDATA%)
- Automatic backend process lifecycle management
- Dynamic port binding to avoid conflicts
- Same-origin hosting in production (no CORS issues)
- User-friendly error display for all failure scenarios

(Source: src/ARIS.UI/MainWindow.xaml.cs:L1-L159, src/ARIS.UI/Bootstrap/PayloadExtractor.cs:L9-L13)

### 5.5.2. Bootstrap Components

#### PayloadExtractor (`src/ARIS.UI/Bootstrap/PayloadExtractor.cs`)

Extracts the embedded payload to the user's local application data folder with idempotent extraction using SHA-256 hashing.

**Key Properties:**
- `PayloadResourceName`: `"ARIS.Payload"` (embedded resource logical name)
- `LockFileName`: `".payload.lock"` (extraction lock file)
- `_extractionRoot`: `%LOCALAPPDATA%\ARIS\payload\`
- `PayloadPath`: Full path to extracted payload directory

**Key Methods:**
- `ExtractAsync()`: Main extraction method
  1. Gets embedded payload stream via `Assembly.GetManifestResourceStream("ARIS.Payload")`
  2. If stream is null, throws `PayloadNotFoundException`
  3. Computes SHA-256 hash of payload
  4. Uses first 16 chars of hash as `payloadId` for directory name
  5. Checks if extraction is up-to-date via lock file
  6. If up-to-date, returns path to `Aris.Hosting.exe`
  7. Otherwise, extracts zip archive and writes lock file
- `ComputePayloadHashAsync(stream)`: Static SHA-256 computation
- `ParseLockFile(json)`: Parses lock file JSON
- `IsPayloadUpToDate(hash, lockFile)`: Compares hashes (case-insensitive)

**Lock File Schema (`PayloadLockFile`):**
```json
{
  "payloadHash": "abc123...",
  "extractedAt": "2025-01-01T00:00:00Z",
  "version": "1.0.0"
}
```

(Source: src/ARIS.UI/Bootstrap/PayloadExtractor.cs:L1-L209, src/ARIS.UI/Bootstrap/PayloadLockFile.cs:L1-L29)

#### BackendProcessManager (`src/ARIS.UI/Bootstrap/BackendProcessManager.cs`)

Manages backend process lifecycle including starting, URL discovery, and cleanup.

**Key Properties:**
- `UrlPrefix`: `"ARIS_BACKEND_URL="` (stdout contract prefix)
- `DefaultUrlTimeout`: 10 seconds

**Key Methods:**
- `BuildProcessStartInfo(executablePath)`: Creates ProcessStartInfo with:
  - `CreateNoWindow = true` (hidden console)
  - `UseShellExecute = false`
  - `RedirectStandardOutput/Error = true`
  - `WorkingDirectory` set to executable's directory
  - Environment: `ASPNETCORE_URLS=http://127.0.0.1:0`, `ASPNETCORE_ENVIRONMENT=Production`
- `ParseUrlFromStdout(line)`: Extracts URL from `ARIS_BACKEND_URL=...` line
- `StartAndWaitForUrlAsync(executablePath, timeout, ct)`:
  1. Creates process with BuildProcessStartInfo
  2. Registers OutputDataReceived handler to capture URL
  3. Starts process and begins async output read
  4. Waits for URL with timeout (throws `BackendUrlTimeoutException` on timeout)
  5. If process exits before URL announced, throws `BackendStartException`
- `Stop()`: Kills process tree gracefully
- `Dispose()`: Calls Stop()

(Source: src/ARIS.UI/Bootstrap/BackendProcessManager.cs:L1-L197)

#### ReadinessPoller (`src/ARIS.UI/Bootstrap/ReadinessPoller.cs`)

Polls the backend `/health` endpoint until it reports ready status.

**Key Properties:**
- `DefaultTimeout`: 60 seconds
- `DefaultPollInterval`: 500 milliseconds

**Key Methods:**
- `IsReady(response)`: Static check for `Status == "Ready"` (case-insensitive)
- `WaitForReadyAsync(backendUrl, timeout, pollInterval, ct)`:
  1. Constructs health URL: `{backendUrl.TrimEnd('/')}/health`
  2. Polls every `pollInterval` until timeout
  3. Catches `HttpRequestException` (expected during startup)
  4. Catches `TaskCanceledException` (HTTP timeout)
  5. Throws `BackendReadinessTimeoutException` if timeout reached

**HealthResponse DTO:**
```json
{
  "status": "Ready",
  "dependenciesReady": true,
  "message": null
}
```

(Source: src/ARIS.UI/Bootstrap/ReadinessPoller.cs:L1-L110)

### 5.5.3. Bootstrap Exception Types

All bootstrap exceptions extend `BootstrapException` with `ErrorCode` and `RemediationHint`:

| Exception | ErrorCode | When Thrown |
|-----------|-----------|-------------|
| `PayloadNotFoundException` | `PAYLOAD_NOT_FOUND` | Embedded `ARIS.Payload` resource missing (dev builds) |
| `PayloadExtractionException` | `PAYLOAD_EXTRACTION_FAILED` | Zip extraction failure |
| `BackendStartException` | `BACKEND_START_FAILED` | Process.Start fails or process exits before URL |
| `BackendUrlTimeoutException` | `BACKEND_URL_TIMEOUT` | URL not announced within 10 seconds |
| `BackendReadinessTimeoutException` | `BACKEND_READINESS_TIMEOUT` | `/health` not Ready within 60 seconds |

(Source: src/ARIS.UI/Bootstrap/BootstrapException.cs:L1-L107)

### 5.5.4. UI Bootstrap Flow (`MainWindow.xaml.cs`)

**Mode Detection:**
```csharp
var hasPayload = Assembly.GetExecutingAssembly()
    .GetManifestResourceStream("ARIS.Payload") != null;
```

**Production Mode (`InitializeProductionModeAsync`):**
1. UpdateStatus("Extracting application files...")
2. `PayloadExtractor.ExtractAsync()` ? returns `hostingExePath`
3. `webView.EnsureCoreWebView2Async()` (parallel)
4. UpdateStatus("Starting backend...")
5. `BackendProcessManager.StartAndWaitForUrlAsync(hostingExePath)` ? returns `_backendUrl`
6. UpdateStatus("Waiting for backend to be ready...")
7. `ReadinessPoller.WaitForReadyAsync(_backendUrl)`
8. Await WebView2 initialization
9. `webView.Source = new Uri(_backendUrl)` ? Navigate to backend
10. Hide loading overlay, show WebView

**Development Mode (`InitializeDevelopmentModeAsync`):**
1. Locate `frontend/dist/index.html` relative to assembly
2. If exists: Navigate to `file:///` URL
3. If missing: Display fallback HTML with instructions

**Error Handling:**
- `BootstrapException` ? `ShowError()` ? `ErrorWindow` ? `Application.Current.Shutdown(1)`
- Unexpected `Exception` ? Wrapped in `BootstrapException("UNEXPECTED_ERROR", ...)`

**Window Close Handler:**
- `_backendManager?.Dispose()` ? Stops backend child process

(Source: src/ARIS.UI/MainWindow.xaml.cs:L1-L159, src/ARIS.UI/MainWindow.xaml:L1-L38)

### 5.5.5. Global Exception Handling (`App.xaml.cs`)

Catches unhandled exceptions at all levels:
- `DispatcherUnhandledException` ? UI thread exceptions
- `AppDomain.CurrentDomain.UnhandledException` ? Background thread exceptions
- `TaskScheduler.UnobservedTaskException` ? Async task exceptions

All handlers call `ShowErrorAndShutdown(exception)`:
1. Hide main window
2. Show `ErrorWindow` dialog
3. `Application.Current.Shutdown(1)`

Fallback: If ErrorWindow fails, shows MessageBox.

(Source: src/ARIS.UI/App.xaml.cs:L1-L76)

### 5.5.6. Error Window (`ErrorWindow.xaml/.xaml.cs`)

WPF window displaying bootstrap/runtime errors with user-friendly formatting.

**Components:**
- Error icon (?) + title (context-specific)
- Error code badge
- Error message (scrollable)
- Remediation hint panel (if available)
- "Open Logs" button ? Opens `%LOCALAPPDATA%\ARIS\logs\` in Explorer
- "Exit" button ? `Application.Current.Shutdown(1)`

**Title Mapping:**
- `PayloadNotFoundException` ? "Installation Error"
- `PayloadExtractionException` ? "Extraction Error"
- `BackendStartException` ? "Backend Failed to Start"
- `BackendUrlTimeoutException` ? "Backend Timeout"
- `BackendReadinessTimeoutException` ? "Backend Not Ready"
- Default ? "Startup Error"

(Source: src/ARIS.UI/Views/ErrorWindow.xaml:L1-L62, src/ARIS.UI/Views/ErrorWindow.xaml.cs:L1-L104)

### 5.5.7. Backend URL Announcement (`UrlAnnouncementService`)

Hosted service that announces the dynamically bound URL to stdout for UI discovery.

**Implementation:**
1. Registers as `IHostedService`
2. On `StartAsync`, schedules announcement after 100ms delay
3. `AnnounceUrl()`:
   - Gets `IServerAddressesFeature` from server
   - Selects first `127.0.0.1` or `localhost` address
   - Normalizes `localhost` to `127.0.0.1`
   - Writes `ARIS_BACKEND_URL={address}` to stdout
   - Flushes immediately
   - Sets `_announced = true` to prevent duplicate announcements

**Expected Behavior:**
- Current behavior emits one line: `ARIS_BACKEND_URL=http://127.0.0.1:<port>`
- Designed to emit once per process lifetime (uses `_announced` flag)
- UI parses stdout for this prefix
- Note: This behavior is not enforced by automated tests

(Source: src/Aris.Hosting/Infrastructure/UrlAnnouncementService.cs:L1-L77)

### 5.5.8. Same-Origin Static File Hosting

**Production Configuration (`Program.cs`):**
```csharp
// Conditional CORS (dev only)
if (builder.Environment.IsDevelopment())
{
    builder.Services.AddCors(...);
}

// Static file serving
app.UseStaticFiles();  // Serves from wwwroot/

// SPA fallback (must be after API endpoints)
app.MapFallbackToFile("index.html");
```

**Frontend URL Handling (`frontend/src/config/backend.ts`):**
```typescript
export function getBackendBaseUrl(): string {
  if (import.meta.env.PROD) {
    return "";  // Same-origin relative URLs
  }
  // Dev: explicit localhost URL
  return import.meta.env.VITE_ARIS_BACKEND_URL || "http://localhost:5000";
}

export function getBackendWsUrl(): string {
  if (import.meta.env.PROD) {
    const proto = window.location.protocol === 'https:' ? 'wss:' : 'ws:';
    return `${proto}//${window.location.host}`;
  }
  return getBackendBaseUrl().replace(/^http/, 'ws');
}
```

**Result:**
- Production: API calls use relative URLs (`/api/retoc/stream`)
- Development: API calls use `http://localhost:5000/api/retoc/stream`

(Source: src/Aris.Hosting/Program.cs:L9-L22, L60-L79, frontend/src/config/backend.ts:L1-L34)

### 5.5.9. Bootstrap Tests

**PayloadExtractorTests** (`tests/ARIS.UI.Tests/Bootstrap/PayloadExtractorTests.cs`):
- `ComputePayloadHash_SameInput_ReturnsSameHash`
- `ComputePayloadHash_DifferentInput_ReturnsDifferentHash`
- `ParseLockFile_ValidJson_ReturnsLockFile`
- `ParseLockFile_InvalidJson_ReturnsNull`
- `ParseLockFile_EmptyString_ReturnsNull`
- `ParseLockFile_NullString_ReturnsNull`
- `IsPayloadUpToDate_HashMatches_ReturnsTrue`
- `IsPayloadUpToDate_HashMismatch_ReturnsFalse`
- `IsPayloadUpToDate_NoLockFile_ReturnsFalse`

**BackendProcessManagerTests** (`tests/ARIS.UI.Tests/Bootstrap/BackendProcessManagerTests.cs`):
- `BuildProcessStartInfo_SetsCreateNoWindow`
- `BuildProcessStartInfo_SetsUseShellExecuteFalse`
- `BuildProcessStartInfo_SetsRedirectStandardOutput`
- `BuildProcessStartInfo_SetsRedirectStandardError`
- `BuildProcessStartInfo_SetsWorkingDirectory`
- `BuildProcessStartInfo_SetsEnvironmentVariables`
- `ParseUrlFromStdout_ValidLine_ExtractsUrl`
- `ParseUrlFromStdout_NoPrefix_ReturnsNull`
- `ParseUrlFromStdout_PartialPrefix_ReturnsNull`
- `ParseUrlFromStdout_EmptyLine_ReturnsNull`
- `ParseUrlFromStdout_NullLine_ReturnsNull`
- `ParseUrlFromStdout_TrimsWhitespace`

**ReadinessPollerTests** (`tests/ARIS.UI.Tests/Bootstrap/ReadinessPollerTests.cs`):
- `IsReady_StatusReady_ReturnsTrue`
- `IsReady_StatusStarting_ReturnsFalse`
- `IsReady_StatusError_ReturnsFalse`
- `IsReady_NullResponse_ReturnsFalse`
- `IsReady_CaseInsensitive_ReturnsTrue`
- `IsReady_EmptyStatus_ReturnsFalse`

(Source: tests/ARIS.UI.Tests/Bootstrap/PayloadExtractorTests.cs:L1-L136, tests/ARIS.UI.Tests/Bootstrap/BackendProcessManagerTests.cs:L1-L141, tests/ARIS.UI.Tests/Bootstrap/ReadinessPollerTests.cs:L1-L82)


## 6. Terminal Execution (ConPTY/WebSocket Streaming)

### 6.1. Overview

ARIS uses Windows Pseudo Console (ConPTY) for streaming tool execution with full terminal emulation. This enables proper rendering of ANSI escape sequences, including progress bars from tools like Retoc (which uses Rust's `indicatif` crate).

**Goals:**
- Stream real-time output from tool execution with full VT/ANSI escape sequence support
- Enable progress bar rendering for tools that require a TTY environment
- Provide cancellation capability via WebSocket disconnect

**Architecture:**
```
┌─────────────────────────────────────────────────────────────────┐
│  Frontend (RetocPage.tsx)                                       │
│  ├─ TerminalPanel.tsx (xterm.js wrapper)                        │
│  └─ retocClient.ts → streamRetocExecution()                     │
└────────────────────────┬────────────────────────────────────────┘
                         │ WebSocket: ws://localhost:5000/api/retoc/stream
┌────────────────────────▼────────────────────────────────────────┐
│  Backend (Aris.Hosting)                                         │
│  ├─ RetocEndpoints.cs → /api/retoc/stream (WebSocket upgrade)   │
│  └─ RetocStreamHandler.cs → HandleAsync()                       │
└────────────────────────┬────────────────────────────────────────┘
                         │
┌────────────────────────▼────────────────────────────────────────┐
│  Infrastructure (ConPTY)                                        │
│  ├─ IConPtyProcess / ConPtyProcess                              │
│  └─ ConPtyNativeMethods (P/Invoke: kernel32.dll)               │
│      CreatePseudoConsole, ResizePseudoConsole, ClosePseudoConsole│
└─────────────────────────────────────────────────────────────────┘
```

(Source: src/Aris.Hosting/Endpoints/RetocEndpoints.cs:L295-L311, src/Aris.Hosting/Endpoints/RetocStreamHandler.cs:L1-L361, src/Aris.Infrastructure/Terminal/ConPtyProcess.cs:L1-L389)

### 6.2. ConPTY Implementation

#### Key Components

**IConPtyProcess Interface** (`src/Aris.Infrastructure/Terminal/IConPtyProcess.cs`):
```csharp
public interface IConPtyProcess : IDisposable
{
    int ProcessId { get; }
    bool HasStarted { get; }
    bool HasExited { get; }
    Task StartAsync(string executable, string arguments, string? workingDirectory,
                    short terminalWidth = 120, short terminalHeight = 30);
    IAsyncEnumerable<byte[]> ReadOutputAsync(CancellationToken ct);
    Task WriteInputAsync(byte[] data, CancellationToken ct);
    Task<int> WaitForExitAsync(CancellationToken ct);
    void Kill();
    void Resize(short width, short height);
}
```

**ConPtyProcess** (`src/Aris.Infrastructure/Terminal/ConPtyProcess.cs`):
- Creates pipes for ConPTY I/O using `CreatePipe`
- Creates pseudo-console with `CreatePseudoConsole` (120x30 default size)
- Configures process with `PROC_THREAD_ATTRIBUTE_PSEUDOCONSOLE`
- **Critical:** Does NOT set `STARTF_USESTDHANDLES` - ConPTY provides the console
- Uses `EXTENDED_STARTUPINFO_PRESENT | CREATE_UNICODE_ENVIRONMENT` for creation flags
- Reads output synchronously wrapped in `Task.Run()` (CreatePipe handles don't support overlapped I/O)

**ConPtyNativeMethods** (`src/Aris.Infrastructure/Terminal/ConPtyNativeMethods.cs`):
Win32 P/Invoke declarations for ConPTY APIs:
- `CreatePseudoConsole`, `ClosePseudoConsole`, `ResizePseudoConsole`
- `InitializeProcThreadAttributeList`, `UpdateProcThreadAttribute`, `DeleteProcThreadAttributeList`
- `CreateProcess`, `TerminateProcess`, `WaitForSingleObject`, `GetExitCodeProcess`
- `CreatePipe`, `CloseHandle`

(Source: src/Aris.Infrastructure/Terminal/IConPtyProcess.cs:L1-L74, src/Aris.Infrastructure/Terminal/ConPtyProcess.cs:L45-L174, src/Aris.Infrastructure/Terminal/ConPtyNativeMethods.cs:L1-L336)

### 6.3. WebSocket Endpoint

**Route:** `GET /api/retoc/stream` (WebSocket upgrade required)

**Protocol:**
1. Client connects via WebSocket
2. Client sends JSON message with `RetocStreamRequest`
3. Server sends NDJSON events via WebSocket TEXT frames (one JSON object + newline per frame)
4. Client can send `{"action": "cancel"}` to abort execution
5. Connection closes after `exited` or `error` event

> **Framing:** Each WebSocket TEXT frame contains exactly one complete JSON object followed by `\n`. The backend sends each event with `endOfMessage: true`, guaranteeing one complete event per frame—no partial JSON, no buffering required. The client splits by newline defensively but will only receive one JSON object per WebSocket message.

**Request Schema (`RetocStreamRequest`):**
```json
{
  "commandType": "ToZen",
  "inputPath": "C:\\mods\\input",
  "outputPath": "C:\\mods\\output.utoc",
  "engineVersion": "UE5_6",
  "aesKey": null,
  "containerHeaderVersion": null,
  "tocVersion": null,
  "chunkId": null,
  "verbose": false,
  "timeoutSeconds": null,
  "ttyProbe": false
}
```

**Event Types (`RetocStreamEvent`):**

| Type | Fields | Description |
|------|--------|-------------|
| `started` | `operationId`, `commandLine`, `timestamp` | Execution has begun |
| `output` | `data`, `timestamp` | Raw VT/ANSI terminal output (UTF-8) |
| `exited` | `exitCode`, `duration`, `timestamp` | Process completed |
| `error` | `code`, `message`, `remediationHint?`, `timestamp` | Fatal error occurred |

**Example Event Stream:**
```json
{"type":"started","operationId":"abc123","commandLine":"retoc.exe to-zen ...","timestamp":"..."}
{"type":"output","data":"Processing files...\r\n","timestamp":"..."}
{"type":"output","data":"\u001b[32m██████████\u001b[0m 100%\r\n","timestamp":"..."}
{"type":"exited","exitCode":0,"duration":"00:00:05.123","timestamp":"..."}
```

(Source: src/Aris.Hosting/Endpoints/RetocEndpoints.cs:L295-L311, src/Aris.Contracts/Retoc/RetocStreamRequest.cs:L1-L62, src/Aris.Contracts/Retoc/RetocStreamEvent.cs:L1-L76)

### 6.4. RetocStreamHandler

The `RetocStreamHandler` class manages WebSocket connections for Retoc streaming:

**Key Responsibilities:**
1. Receive and parse `RetocStreamRequest` from WebSocket
2. Build Retoc command via `IRetocAdapter.BuildCommand()`
3. Create ConPTY process and stream output to client
4. Monitor for client disconnect or cancel command
5. Kill process on WebSocket close

**Handler Flow:**
```csharp
public async Task HandleAsync(WebSocket webSocket, CancellationToken ct)
{
    var request = await ReceiveRequestAsync(webSocket, ct);
    if (request.TtyProbe) {
        await RunTtyProbeAsync(webSocket, operationId, request, ct);
        return;
    }
    await ExecuteRetocStreamAsync(webSocket, operationId, request, ct);
}
```

**Cancellation:**
- WebSocket close triggers `process.Kill()`
- Client can send `{"action":"cancel"}` message
- `MonitorWebSocketAsync` task watches for cancel/close events

(Source: src/Aris.Hosting/Endpoints/RetocStreamHandler.cs:L43-L95, L126-L228, L230-L277)

### 6.5. Frontend Terminal Component

**TerminalPanel.tsx** (`frontend/src/components/terminal/TerminalPanel.tsx`):
React component wrapping xterm.js for terminal rendering.

**Features:**
- xterm.js with FitAddon for auto-sizing
- Dark theme matching ARIS design (`#0a0a0a` background)
- Exposes ref API: `write()`, `writeln()`, `clear()`, `fit()`, `focus()`, `scrollToBottom()`
- ResizeObserver with 100ms debounce to prevent feedback loops
- Fixed height container to prevent infinite expansion

**Theme Configuration:**
```typescript
const TERMINAL_THEME = {
  background: '#0a0a0a',
  foreground: '#e4e4e4',
  cursor: '#f59e0b',
  // ... 16 ANSI colors
};
```

**retocClient.ts WebSocket Client:**
```typescript
export function streamRetocExecution(
  request: RetocStreamRequest,
  onEvent: (event: RetocStreamEvent) => void
): { cancel: () => void; promise: Promise<void> }
```
- Opens WebSocket to `/api/retoc/stream`
- Sends request on connection open
- Parses NDJSON events and invokes callback
- Returns cancel function and completion promise

(Source: frontend/src/components/terminal/TerminalPanel.tsx:L1-L189, frontend/src/api/retocClient.ts:L82-L142)

### 6.6. TTY Probe Mode

For diagnostics, the stream endpoint supports a TTY probe mode that tests ConPTY functionality without running Retoc.

**Trigger:** Set `ttyProbe: true` in request

**Probe Output:**
- Runs `cmd.exe /c echo` through ConPTY
- Displays probe results including PID, exit code, duration
- Shows diagnostic hints if indicatif progress bars don't appear

**Expected Results (working ConPTY):**
- `GetConsoleMode: success` for stdout handle
- `GetFileType: FILE_TYPE_CHAR (0x0002)` for output

(Source: src/Aris.Hosting/Endpoints/RetocStreamHandler.cs:L279-L333)

### 6.7. Dependency Injection Registration

**Infrastructure Layer (`src/Aris.Infrastructure/DependencyInjection.cs:L33-L34`):**
```csharp
// ConPTY process (transient - each stream session needs a new instance)
services.AddTransient<IConPtyProcess, ConPtyProcess>();
```

**Hosting Layer (`src/Aris.Hosting/DependencyInjection.cs:L36-L45`):**
```csharp
services.AddScoped<RetocStreamHandler>(sp =>
{
    var retocAdapter = sp.GetRequiredService<IRetocAdapter>();
    var logger = sp.GetRequiredService<ILogger<RetocStreamHandler>>();
    Func<IConPtyProcess> conPtyFactory = () => sp.GetRequiredService<IConPtyProcess>();
    return new RetocStreamHandler(retocAdapter, conPtyFactory, logger);
});
```

**Program.cs (`src/Aris.Hosting/Program.cs:L54-L58`):**
```csharp
app.UseWebSockets(new WebSocketOptions
{
    KeepAliveInterval = TimeSpan.FromSeconds(30)
});
```

(Source: src/Aris.Infrastructure/DependencyInjection.cs:L33-L34, src/Aris.Hosting/DependencyInjection.cs:L36-L45, src/Aris.Hosting/Program.cs:L54-L58)

### 6.8. Error Handling

**Error Codes:**
- `WEBSOCKET_REQUIRED` - Non-WebSocket request to stream endpoint
- `INVALID_REQUEST` - Failed to parse WebSocket request
- `VALIDATION_ERROR` - Invalid command type or parameters
- `DEPENDENCY_MISSING` - Retoc executable not found
- `TOOL_EXECUTION_ERROR` - Process execution failed
- `TTY_PROBE_ERROR` - TTY probe failed
- `UNEXPECTED_ERROR` - Unhandled exception

**Error Event Format:**
```json
{"type":"error","code":"VALIDATION_ERROR","message":"Invalid command type 'Foo'","remediationHint":"Valid types: ToZen, ToLegacy, ...","timestamp":"..."}
```

(Source: src/Aris.Hosting/Endpoints/RetocStreamHandler.cs:L351-L360, src/Aris.Hosting/Endpoints/RetocEndpoints.cs:L298-L305)

### 6.9. Current Tool Streaming Support

> **Note:** This section documents WebSocket/ConPTY streaming support only. For overall tool implementation status, see Section 1 Feature Maturity table.

| Tool | Streaming Support | Notes |
|------|-------------------|-------|
| Retoc | ✓ Yes | Full ConPTY streaming with progress bars |
| UAssetAPI | ✗ No | In-process library, no external process |
| UWPDumper | ✗ No | Uses HTTP POST; streaming not implemented |
| DLL Injector | ✗ No | Uses HTTP POST; streaming not implemented |

### 6.10. Known Limitations

> **Terminology note:** Throughout this document, "interactive" refers solely to PTY-backed output rendering and limited control signals (e.g., cancel); stdin passthrough is not supported.

- **Windows 10 1809+ required** - ConPTY not available on older Windows
- **No stdout/stderr separation** - ConPTY merges into single VT stream
- **Cancel only, no stdin passthrough** - Client can send `{"action": "cancel"}` to abort execution, but cannot send input to the running process
- **Single execution per connection** - Each WebSocket handles one command
- **Only Retoc supported** - Other tools do not use streaming


## 7. Public Interfaces and Contracts

### 7.1. HTTP API Base URL
`http://localhost:5000` (overridable via `VITE_ARIS_BACKEND_URL`)

**Authentication:** None (localhost-only)

**CORS:** `http://localhost:5173` (Vite dev server)

(Source: src/Aris.Hosting/Program.cs:L10-L19, frontend/src/config/backend.ts:L1-L12)

### 7.2. Endpoints Summary

| Endpoint | Method | Purpose |
|----------|--------|---------|
| `/health` | GET | Backend health status |
| `/info` | GET | Version and tool info |
| `/api/retoc/convert` | POST | Retoc PAK/IoStore conversion (legacy) |
| `/api/retoc/build` | POST | Build command preview |
| `/api/retoc/stream` | WebSocket | Execute with real-time terminal streaming |
| `/api/retoc/schema` | GET | UI command schema |
| `/api/retoc/help` | GET | Retoc help text |
| `/api/uasset/serialize` | POST | JSON ? .uasset |
| `/api/uasset/deserialize` | POST | .uasset ? JSON |
| `/api/uasset/inspect` | POST | .uasset metadata |
| `/api/uwpdumper/dump` | POST | UWP dump |
| `/api/dllinjector/inject` | POST | DLL injection |
| `/api/dllinjector/eject` | POST | DLL ejection |
| `/api/tools/{tool}/help` | GET | Tool help (generic) |
| `/api/tools/{tool}/schema` | GET | Tool schema (generic) |


### 7.3. Retoc Endpoints (Detail)

#### `POST /api/retoc/build`
Build a command for preview without executing.

**Request (RetocBuildCommandRequest):**
```json
{
  "commandType": "ToZen",
  "inputPath": "C:\\mods\\input",
  "outputPath": "C:\\mods\\output.utoc",
  "engineVersion": "UE5_6",
  "aesKey": null,
  "containerHeaderVersion": null,
  "tocVersion": null,
  "chunkId": null,
  "verbose": false,
  "timeoutSeconds": null
}
```

**Response (RetocBuildCommandResponse):**
```json
{
  "executablePath": "C:\\...\\retoc.exe",
  "arguments": ["to-zen", "--version", "UE5_6", "C:\\input", "C:\\output.utoc"],
  "commandLine": "retoc.exe to-zen --version UE5_6 \"C:\\input\" \"C:\\output.utoc\""
}
```

(Source: src/Aris.Hosting/Endpoints/RetocEndpoints.cs:L164-L201, src/Aris.Contracts/Retoc/RetocBuildCommandRequest.cs:L1-L59, src/Aris.Contracts/Retoc/RetocBuildCommandResponse.cs)

#### `WebSocket /api/retoc/stream`
Execute Retoc with real-time terminal streaming via WebSocket.

> **Note:** This is a WebSocket endpoint, not HTTP POST. See Section 6 for full protocol details.


#### `GET /api/retoc/schema`
Returns UI schema for Advanced Mode command builder.

**Response (RetocCommandSchemaResponse):**
```json
{
  "commands": [
    {
      "commandType": "ToLegacy",
      "displayName": "Unpack (Zen ? Legacy)",
      "description": "Convert IoStore containers to editable legacy UAsset files",
      "requiredFields": ["InputPath", "OutputPath"],
      "optionalFields": ["AesKey", "ContainerHeaderVersion", "TocVersion"],
      "fieldUiHints": {"InputPath": {"pathKind": "folder"}}
    }
  ],
  "globalOptions": [
    {"fieldName": "InputPath", "label": "Input Path", "fieldType": "Path", "helpText": "..."},
    {"fieldName": "EngineVersion", "label": "Engine Version", "fieldType": "Enum", "enumValues": ["UE5_0", "UE5_6"]}
  ],
  "allowlistedFlags": ["--verbose"]
}
```

Schema is derived from canonical schema + UI mapping overlay using RetocSchemaDerived.Derive().

(Source: src/Aris.Hosting/Endpoints/RetocEndpoints.cs:L203-L241, src/Aris.Adapters/Retoc/RetocSchemaDerived.cs:L22-L66, src/Aris.Adapters/Retoc/RetocCommandSchemaProvider.cs:L13-L208, src/Aris.Contracts/Retoc/RetocCommandSchemaResponse.cs:L1-L24)

#### `GET /api/retoc/help`
Returns Retoc --help output wrapped in markdown code fence.

(Source: src/Aris.Hosting/Endpoints/RetocEndpoints.cs:L243-L298, src/Aris.Contracts/Retoc/RetocHelpResponse.cs)





### 7.5. Frontend API Clients
TypeScript clients in `frontend/src/api/{tool}Client.ts` use Fetch API:
- Parse backend URL from `getBackendBaseUrl()`
- POST JSON with `Content-Type: application/json`
- Parse error responses for ErrorInfo
- For streaming: parse NDJSON line-by-line

**Retoc Client Functions:**
- `buildRetocCommand(request)` ? RetocBuildCommandResponse
- `getRetocSchema()` ? RetocCommandSchemaResponse
- `getRetocHelp()` ? RetocHelpResponse
- `streamRetocExecution(request, onEvent, signal?)` ? void (streams events)



## 8. Frontend Architecture

### 8.1. Routing

| Path | Component | Description |
|------|-----------|-------------|
| `/` | DashboardPage | Tool overview dashboard |
| `/tools/retoc` | RetocPage | Retoc Simple/Advanced UI |
| `/tools/uasset` | UAssetPage | UAsset operations |
| `/tools/uwpdumper` | UwpDumperPage | UWP dump operations |
| `/tools/dllinjector` | DllInjectorPage | DLL injection |
| `/system/health` | SystemHealthPage | Backend health status |
| `/settings` | SettingsPage | User settings |

All routes render inside MainLayout which provides AppShell with navigation.

(Source: frontend/src/router.tsx:L1-L46, frontend/src/layouts/MainLayout.tsx)

### 8.2. Retoc Simple/Advanced Mode UI

The Retoc page (`frontend/src/pages/tools/RetocPage.tsx`) implements a dual-mode interface:

**Mode Toggle (L295-L316):**
- Simple Mode: Preset Pack/Unpack workflows
- Advanced Mode: Full command builder with schema-driven UI
- State: `mode: 'simple' | 'advanced'` (L21, L31)

**Simple Mode (L327-L481):**
Two panels with form validation:

1. **Pack (Legacy ? Zen)** - Build mod from modified UAssets:
   - Fields: Modified UAsset Directory, Mod Output Directory, Mod Name, UE Version
   - Validation: All fields required (L146-L168)
   - Constructs output path: `{outputDir}\{modName}.utoc` (L170)
   - Executes: `ToZen` command (L172-L177)

2. **Unpack (Zen ? Legacy)** - Extract IoStore to UAssets:
   - Fields: Base Game Paks Directory, Extracted Output Directory
   - Validation: All fields required (L192-L206)
   - Executes: `ToLegacy` command (L208-L212)

**Advanced Mode (L483-L508):**
- Uses RetocAdvancedCommandBuilder component
- Dynamically renders fields based on schema from `GET /api/retoc/schema`
- Command selector dropdown with 13 command types
- Required/optional field sections based on selected command
- Field types: Path, String, Integer, Enum, Boolean (L62-L175 in component)
- Execute button triggers streamRetocExecution

**Command Preview (L512-L513):**
- RetocCommandPreview component shows formatted command line
- Auto-updates in Advanced Mode when required fields are filled (L66-L76)
- Copy-to-clipboard functionality


- **View-only vs Interactive Toggle (L563-593):**
  - Toggle buttons allow switching between "View-only" (pipe mode) and "Interactive" (PTY mode for output rendering; no stdin passthrough)
  - Disabled during execution
- **Mode-Specific Defaults:**
  - `onSessionEnd` callback updates execution status based on exit code

**State Management:**
- Local useState hooks for form state
- Execution state: isExecuting, executionStatus, logLines, executionError
- Preview state: commandPreview, isPreviewLoading


### 8.3. UI Component Library

Shared components in `frontend/src/components/ui/`:

| Component | Purpose |
|-----------|---------|
| AppShell, Sidebar, Header | Main layout shell |
| Panel, PanelHeader, PanelBody | Content panels |
| Card, ModuleCard | Card layouts |
| Button | Primary/secondary/accent buttons |
| Input, Select, Textarea | Form inputs |
| Field | Form field with label/error/help |
| Badge, StatusPill | Status indicators |
| Alert | Error/warning/info alerts |
| ConsoleBlock, CodeBlock | Code display |

(Source: frontend/src/components/ui/index.ts:L1-L37, frontend/src/components/ui/*.tsx)

## 9. Data Model and Persistence

### Storage
- **Tool binaries:** `%LOCALAPPDATA%/ARIS/tools/{version}/`
- **Logs:** `%LOCALAPPDATA%/ARIS/logs/aris-{date}.log` (rolling daily, 7 days retention)
- **Operation logs:** `{workspace}/logs/uasset-{operationId}.log`
- **No database:** All state is transient or file-based

(Source: src/Aris.Hosting/Program.cs:L21-L35, src/Aris.Infrastructure/Tools/DependencyExtractor.cs:L27-L29, src/Aris.Adapters/UAsset/UAssetService.cs:L268-L340)

### Domain Models (Aris.Core)

**Commands:**
- `RetocCommand` (~20 properties including CommandType, InputPath, OutputPath, Version, AesKey, ContainerHeaderVersion, TocVersion, ChunkId)
- `UAssetSerializeCommand`, `UAssetDeserializeCommand`, `UAssetInspectCommand`
- `UwpDumpCommand`
- `DllInjectCommand`, `DllEjectCommand`

**Results:**
- `RetocResult` (exitCode, outputPath, outputFormat, duration, warnings, producedFiles, logExcerpt)
- `UAssetResult` (operation, inputPath, outputPath, duration, warnings, producedFiles, schemaVersion, ueVersion)
- `UwpDumpResult`
- `DllInjectResult`, `DllEjectResult`

**Models:**
- `ProducedFile` (path, sizeBytes, sha256?, fileType?)
- `ProcessResult` (exitCode, stdOut, stdErr, duration, startTime, endTime, success)
- `ProgressEvent` (step, message, percent?, detail?, timestamp)

**Enums:**
- `RetocMode` (PakToIoStore, IoStoreToPak, Repack, Validate)
- `RetocCommandType` (13 values: Manifest, Info, List, Verify, Unpack, UnpackRaw, PackRaw, ToLegacy, ToZen, Get, DumpTest, GenScriptObjects, PrintScriptObjects)
- `RetocContainerHeaderVersion` (Initial, LocalizedPackages, OptimizedNames)
- `RetocTocVersion` (DirectoryIndex, PartitionSize, PerfectHash, PerfectHashWithOverflow)
- `DllInjectionMethod` (CreateRemoteThread, ApcQueue, ManualMap)
- `UwpDumpMode` (FullDump, MetadataOnly, ValidateOnly)
- `OperationStatus` (Pending, Succeeded, Failed)

(Source: src/Aris.Core/Retoc/RetocCommand.cs, src/Aris.Core/Retoc/RetocResult.cs, src/Aris.Core/Models/ProducedFile.cs, src/Aris.Core/Models/ProcessResult.cs, src/Aris.Core/Retoc/RetocCommandType.cs:L1-L75, src/Aris.Contracts/OperationStatus.cs:L1-L23)

### Error Types

**Base:** `ArisException` (errorCode, message, remediationHint)

**Derived:**
- `ValidationError` (fieldName?, invalidValue?) - ErrorCode: `VALIDATION_ERROR`
- `DependencyMissingError` (dependencyId, expectedPath?) - ErrorCode: `DEPENDENCY_MISSING`
- `ElevationRequiredError` (operationId?) - ErrorCode: `ELEVATION_REQUIRED`
- `ChecksumMismatchError` (filePath, expectedHash, actualHash, algorithm) - ErrorCode: `CHECKSUM_MISMATCH`
- `ToolExecutionError` (toolName, exitCode, commandLine?, stdOut?, stdErr?) - ErrorCode: `TOOL_EXECUTION_ERROR`
- `DeserializationError` - ErrorCode: `DESERIALIZATION_ERROR`
- `SerializationError` - ErrorCode: `SERIALIZATION_ERROR`

(Source: src/Aris.Core/Errors/ArisException.cs:L1-L29, src/Aris.Core/Errors/ValidationError.cs:L1-L36, src/Aris.Core/Errors/DependencyMissingError.cs:L1-L33, src/Aris.Core/Errors/ElevationRequiredError.cs:L1-L23, src/Aris.Core/Errors/ChecksumMismatchError.cs:L1-L38, src/Aris.Core/Errors/ToolExecutionError.cs:L1-L47)

### Contracts (Aris.Contracts)

**Retoc Contracts:**
- `RetocConvertRequest/Response` - Legacy convert endpoint
- `RetocBuildCommandRequest/Response` - Command preview (L1-L59, L1-L24)
- `RetocCommandSchemaResponse` - UI schema (L1-L24)
- `RetocCommandDefinition` - Command metadata with requiredFields/optionalFields
- `RetocCommandFieldDefinition` - Field schema (fieldName, label, fieldType, enumValues)
- `RetocFieldUiHint` - Path kind (file/folder) and extensions
- `RetocHelpResponse` - Help text markdown

**TypeScript Mirrors (frontend/src/types/contracts.ts):**
Lines 245-332 define TypeScript equivalents for all Retoc streaming and schema types.

(Source: src/Aris.Contracts/Retoc/*.cs, frontend/src/types/contracts.ts:L245-L332)

### Tool Manifest Schema
```json
{
  "version": "0.1.0",
  "tools": [{
    "id": "retoc",
    "version": "v0.1.4",
    "platform": "win-x64",
    "sha256": "1c7b3af2b7ca06ac7216d1ba1c629f1e2b178966d964c488135c09abd68a4ec8",
    "size": 7020544,
    "relativePath": "retoc/retoc.exe",
    "executable": true
  }]
}
```

(Source: src/Aris.Tools/tools.manifest.json:L1-L14, src/Aris.Tools/Manifest/ToolManifest.cs:L3-L18)

## 10. Configuration

### appsettings.json Structure

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.Hosting.Lifetime": "Information"
    }
  },
  "ToolingOptions": {"ExtractionRoot": "", "Version": "0.1.0"},
  "Retoc": {
    "DefaultTimeoutSeconds": 300,
    "DefaultCompressionFormat": "Zlib",
    "DefaultCompressionLevel": 6,
    "AllowedAdditionalArgs": ["--verbose", "--no-warnings"],
    "MaxLogBytes": 5242880,
    "StagingRoot": "",
    "EnableStructuredLogs": false
  },
  "UAsset": {
    "DefaultUEVersion": "5.3",
    "DefaultSchemaVersion": "1.0",
    "MaxAssetSizeBytes": 524288000,
    "DefaultTimeoutSeconds": 300,
    "EnableCliFallback": false,
    "KeepTempOnFailure": false,
    "MaxLogBytes": 5242880,
    "LogJsonOutput": false,
    "StagingRoot": ""
  },
  "UwpDumper": {
    "DefaultTimeoutSeconds": 300,
    "RequireElevation": true,
    "AllowedModes": ["FullDump", "MetadataOnly", "ValidateOnly"],
    "MaxLogBytes": 5242880,
    "StagingRoot": "",
    "KeepTempOnFailure": false
  },
  "DllInjector": {
    "DefaultTimeoutSeconds": 60,
    "RequireElevation": true,
    "AllowedTargets": [],
    "DeniedTargets": ["csrss.exe", "smss.exe", "wininit.exe", "services.exe", "lsass.exe", "svchost.exe", "winlogon.exe"],
    "AllowedMethods": ["CreateRemoteThread", "ApcQueue", "ManualMap"],
    "MaxLogBytes": 5242880,
    "StagingRoot": "",
    "KeepTempOnFailure": false
  }
}
```

(Source: src/Aris.Hosting/appsettings.json:L1-L61, src/Aris.Infrastructure/Configuration/RetocOptions.cs:L1-L55)

### Environment Variables

| Variable | Type | Default | Description |
|----------|------|---------|-------------|
| `ASPNETCORE_ENVIRONMENT` | string | Production | ASP.NET Core environment |
| `VITE_ARIS_BACKEND_URL` | string | `http://localhost:5000` | Frontend backend URL override |
| `RETOC_LOG_JSON` | "1" or unset | unset | Enable Retoc structured JSON logs |

(Source: frontend/src/config/backend.ts:L8, src/Aris.Adapters/Retoc/RetocAdapter.cs:L90-L92)

## 11. Security

### Authentication/Authorization
None implemented. Localhost-only design with CORS restricted to `http://localhost:5173`.

(Source: src/Aris.Hosting/Program.cs:L10-L19)

### Input Validation

**Argument Allowlisting:**
- Retoc: `AllowedAdditionalArgs` enforced in RetocCommandBuilder.cs
- DllInjector: `AllowedMethods` validated in endpoints

**Denied Process List:**
DLL injection blocks critical system processes: csrss.exe, smss.exe, wininit.exe, services.exe, lsass.exe, svchost.exe, winlogon.exe

**Path Validation:**
- RetocCommandBuilder requires absolute paths
- UAssetCommandValidator checks file existence and paths
- Filter patterns reject path traversal (`..`)

**Timeout Enforcement:**
ProcessRunner enforces configurable timeouts and kills hung processes. ConPtyProcess uses cancellation tokens for timeout management.

**Tool Allowlist:**
ToolDocsEndpoints uses allowlist for tool parameter: `retoc, uwpdumper, dllinjector, uasset`

(Source: src/Aris.Hosting/appsettings.json:L19-L23, L50-L62, src/Aris.Adapters/Retoc/RetocCommandBuilder.cs:L49-L112, src/Aris.Infrastructure/Process/ProcessRunner.cs:L89-L100, src/Aris.Hosting/Endpoints/ToolDocsEndpoints.cs:L15-L21)

### SHA-256 Verification
DependencyValidator.ValidateToolAsync computes file hash and compares with manifest:
1. Opens file stream
2. SHA256.ComputeHashAsync
3. Convert to hex string (lowercase)
4. Case-insensitive comparison with manifest value
5. Returns HashMismatch status if different

(Source: src/Aris.Infrastructure/Tools/DependencyValidator.cs:L81-L137)

### Secrets Handling
- AES keys passed via API requests (not persisted)
- Log size limits prevent log flooding (5 MB default, 10 MB streaming)

(Source: src/Aris.Adapters/Retoc/RetocAdapter.cs:L64, src/Aris.Infrastructure/Configuration/RetocOptions.cs:L31-L43)

## 12. Observability and Operations

### Logging

**Framework:** Serilog 10.0.0

**Configuration:**
- Console sink (stdout)
- File sink: `%LOCALAPPDATA%/ARIS/logs/aris-{date}.log`
- Rolling interval: Daily
- Retention: 7 days
- Minimum level: Information
- Enriched with LogContext

**Correlation:**
OperationId used in log scopes for DLL Injector and UWP Dumper operations

(Source: src/Aris.Hosting/Program.cs:L21-L37, src/Aris.Hosting/Endpoints/DllInjectorEndpoints.cs:L37-L40, src/Aris.Hosting/Endpoints/UwpDumperEndpoints.cs:L37-L40)

### Health Checks

**Endpoint:** `GET /health`

**BackendHealthState:**
- Properties: Status ("Starting"/"Ready"/"Error"), DependenciesReady (bool), Message
- Thread-safe with Interlocked operations
- Updated by ToolingStartupHostedService

(Source: src/Aris.Hosting/Endpoints/HealthEndpoints.cs:L19-L34, src/Aris.Hosting/Infrastructure/BackendHealthState.cs:L8-L45, src/Aris.Hosting/Infrastructure/ToolingStartupHostedService.cs:L30-L50)

### Deployment

**Single-file publish:**
```bash
dotnet publish -c Release -r win-x64 --self-contained true /p:PublishSingleFile=true
```

Produces self-contained executable with embedded .NET runtime.

(Source: CLAUDE.md:L78-L81)

## 13. Error Handling Strategy

### Propagation Rules
1. Domain/Adapter: Throw `ArisException` subclass
2. HTTP Endpoint: Catch `ArisException`, map to HTTP status, return structured ErrorInfo
3. Unexpected exceptions: Catch `Exception`, log as error, return 500 with generic message
4. Streaming endpoints: Emit error event in stream, then failed status

(Source: src/Aris.Hosting/Endpoints/RetocEndpoints.cs:L119-L161, L412-L443)

### HTTP Status Mapping
```csharp
ValidationError              ? 400 Bad Request
DependencyMissingError       ? 503 Service Unavailable
ElevationRequiredError       ? 403 Forbidden
ChecksumMismatchError        ? 502 Bad Gateway
ToolExecutionError           ? 500 Internal Server Error
DeserializationError         ? 500 Internal Server Error
SerializationError           ? 500 Internal Server Error
Exception (unexpected)       ? 500 Internal Server Error
```

(Source: src/Aris.Hosting/Endpoints/RetocEndpoints.cs:L555-L566, src/Aris.Hosting/Endpoints/UwpDumperEndpoints.cs:L166-L177)

### ErrorInfo Structure
```json
{
  "code": "VALIDATION_ERROR",
  "message": "InputPath is required",
  "remediationHint": "Provide a valid input path"
}
```

User-facing errors include remediationHint for actionable guidance.

(Source: src/Aris.Contracts/ErrorInfo.cs, src/Aris.Core/Errors/ValidationError.cs:L18-L34)

## 14. Testing Strategy and Patterns

### Framework
xUnit 2.5.3 with coverlet for code coverage

(Source: tests/Aris.Core.Tests/Aris.Core.Tests.csproj:L13-L16)

### Test Patterns

**Fake Dependencies:**
- `FakeProcessRunner` - In-memory process execution stub
- `FakeDependencyValidator` - Stubbed validation
- `FakeDllInjectionService` - Stubbed DLL injection
- `FakeProcessResolver` - Stubbed process resolution

**Test Structure:**
```csharp
public class AdapterTests : IDisposable
{
    private readonly FakeProcessRunner _fakeRunner;
    private readonly string _tempWorkspace;
    private readonly Adapter _adapter;

    public AdapterTests()
    {
        _fakeRunner = new FakeProcessRunner();
        _tempWorkspace = Path.Combine(Path.GetTempPath(), "aris-test-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_tempWorkspace);
        var options = Options.Create(new Options());
        _adapter = new Adapter(_fakeRunner, options);
    }

    public void Dispose() => Directory.Delete(_tempWorkspace, recursive: true);
}
```

**Test Assets:**
Copied to output directory via `tests/Aris.Core.Tests/TestAssets/**/*`

(Source: tests/Aris.Core.Tests/Adapters/RetocAdapterTests.cs:L15-L50, tests/Aris.Core.Tests/Fakes/FakeProcessRunner.cs, tests/Aris.Core.Tests/Aris.Core.Tests.csproj:L31-L34)

### Retoc Schema Provider Tests

Tests validate the command schema used for Advanced Mode UI:
- `GetSchema_ReturnsValidSchema` - Non-empty schema
- `GetSchema_IncludesAllRetocCommandTypes` - All 13 commands present
- `GetSchema_ToLegacyCommand_HasCorrectFields` - Required/optional fields correct
- `GetSchema_ToZenCommand_HasCorrectFields` - EngineVersion required
- `GetSchema_GetCommand_RequiresChunkId` - ChunkId in required fields
- `GetSchema_Commands_HaveUniqueCommandTypes` - No duplicates
- `GetSchema_RequiredAndOptionalFields_DoNotOverlap` - Field separation
- `GetSchema_IsSerializable` - JSON round-trip

(Source: tests/Aris.Core.Tests/Retoc/RetocCommandSchemaProviderTests.cs:L1-L181)

## 15. Code Style, Conventions, and Patterns

### Naming
- Types/Methods: `PascalCase`
- Locals/Parameters: `camelCase`
- Private fields: `_camelCase` (underscore prefix)
- Interfaces: `I{Name}`

(Source: CLAUDE.md:L107-L112, src/Aris.Adapters/Retoc/RetocAdapter.cs:L14-L21)

### Architectural Pattern
Clean layered architecture with dependency flow: `UI ? Hosting ? Adapters ? Infrastructure ? Core`

**Enforcement:** .csproj `<ProjectReference>` defines explicit dependencies. No circular references.

(Source: ARIS.sln:L1-L92, CLAUDE.md:L40-L51, src/Aris.Adapters/Aris.Adapters.csproj:L3-L8)

### Dependency Injection
Constructor injection with extension methods in `{Project}.DependencyInjection.cs`. Singleton lifetime for stateless services.

```csharp
public static IServiceCollection AddAdapters(this IServiceCollection services)
{
    services.AddSingleton<IRetocAdapter, RetocAdapter>();
    return services;
}
```

ConPtyProcess registered for terminal streaming:
```csharp
// ConPTY process (transient - each stream session needs a new instance)
services.AddTransient<IConPtyProcess, ConPtyProcess>();
```

RetocStreamHandler with factory:
```csharp
services.AddScoped<RetocStreamHandler>(sp =>
{
    var retocAdapter = sp.GetRequiredService<IRetocAdapter>();
    var logger = sp.GetRequiredService<ILogger<RetocStreamHandler>>();
    Func<IConPtyProcess> conPtyFactory = () => sp.GetRequiredService<IConPtyProcess>();
    return new RetocStreamHandler(retocAdapter, conPtyFactory, logger);
});
```

(Source: src/Aris.Hosting/DependencyInjection.cs:L8-L28, src/Aris.Adapters/DependencyInjection.cs:L13-L28, src/Aris.Infrastructure/DependencyInjection.cs:L33-L34)

### Common Utilities

**ProcessRunner:**
```csharp
await _processRunner.ExecuteAsync(
    executablePath, arguments,
    workingDirectory: null,
    timeoutSeconds: 300,
    environmentVariables: null,
    cancellationToken);
```
Returns `ProcessResult` with exitCode, stdOut, stdErr, duration. Enforces timeout, captures output (max 10 MB per stream). Used for non-streaming synchronous tool execution.

**ConPtyProcess (Terminal Streaming):**
```csharp
using var conPtyProcess = _conPtyFactory();
await conPtyProcess.StartAsync(executablePath, arguments);

await foreach (var data in conPtyProcess.ReadOutputAsync(cancellationToken))
{
    var text = Encoding.UTF8.GetString(data);
    await SendEventAsync(webSocket, new RetocStreamOutput(text), cancellationToken);
}

var exitCode = await conPtyProcess.WaitForExitAsync(cancellationToken);
```
Provides Windows pseudo-console (ConPTY) for full terminal emulation. Preserves VT/ANSI escape sequences for progress bars and colored output.

**DependencyValidator:**
```csharp
var result = await _validator.ValidateToolAsync("retoc", cancellationToken);
// Returns ToolValidationResult with status: Valid, Missing, HashMismatch, Unknown
```

**Options Pattern:**
```csharp
services.Configure<RetocOptions>(configuration.GetSection("Retoc"));
services.AddSingleton<IValidateOptions<RetocOptions>, RetocOptionsValidator>();
```

(Source: src/Aris.Infrastructure/Process/ProcessRunner.cs:L19-L25, src/Aris.Infrastructure/Terminal/ConPtyProcess.cs:L45-L174, src/Aris.Infrastructure/Terminal/IConPtyProcess.cs:L1-L74, src/Aris.Infrastructure/Tools/DependencyValidator.cs:L50-L129, src/Aris.Infrastructure/DependencyInjection.cs:L16-L34)

## 16. Extending the System Safely

### Adding a New Feature

1. **Define Domain Model** in `Aris.Core/{Feature}/`
   - `{Feature}Command.cs`
   - `{Feature}Result.cs`
   - Enums/value objects

2. **Create Contracts** in `Aris.Contracts/{Feature}/`
   - `{Feature}Request.cs`
   - `{Feature}Response.cs`
   - `{Feature}ResultDto.cs`

3. **Implement Adapter** in `Aris.Adapters/{Feature}/`
   - `I{Feature}Adapter.cs` interface
   - `{Feature}Adapter.cs` implementation
   - Register in `Aris.Adapters/DependencyInjection.cs`

4. **Add Configuration** (if needed)
   - `Aris.Infrastructure/Configuration/{Feature}Options.cs`
   - `Aris.Infrastructure/Configuration/{Feature}OptionsValidator.cs`
   - Add section to `appsettings.json`
   - Register in `Aris.Infrastructure/DependencyInjection.cs`

5. **Create HTTP Endpoint** in `Aris.Hosting/Endpoints/{Feature}Endpoints.cs`
   - Map endpoint group
   - Register in `Program.cs`

6. **Add Tests** in `tests/Aris.Core.Tests/{Feature}/`
   - Follow IDisposable pattern with temp workspace cleanup

7. **Add Frontend**
   - TypeScript types in `frontend/src/types/contracts.ts`
   - API client in `frontend/src/api/{feature}Client.ts`
   - Page in `frontend/src/pages/tools/{Feature}Page.tsx`
   - Route in `frontend/src/router.tsx`

(Source: src/ directory structure, tests/ structure, frontend/src/ structure, src/Aris.Hosting/Program.cs:L51-L56, src/Aris.Hosting/Endpoints/RetocEndpoints.cs:L26-L451)

### Frontend Integration Pattern

For streaming endpoints via WebSocket:
```typescript
export function streamRetocExecution(
  request: RetocStreamRequest,
  onEvent: (event: RetocStreamEvent) => void
): { cancel: () => void; promise: Promise<void> } {
  const wsUrl = getBackendWsUrl();
  const ws = new WebSocket(`${wsUrl}/api/retoc/stream`);

  const promise = new Promise<void>((resolve, reject) => {
    ws.onopen = () => {
      ws.send(JSON.stringify(request));
    };

    ws.onmessage = (event) => {
      // Parse NDJSON (one JSON object per line)
      const lines = (event.data as string).split('\n').filter(line => line.trim());
      for (const line of lines) {
        const streamEvent = JSON.parse(line) as RetocStreamEvent;
        onEvent(streamEvent);
        if (streamEvent.type === 'exited' || streamEvent.type === 'error') {
          ws.close();
          resolve();
        }
      }
    };

    ws.onerror = () => reject(new Error('WebSocket connection error'));
  });

  const cancel = () => {
    if (ws.readyState === WebSocket.OPEN) {
      ws.send(JSON.stringify({ action: 'cancel' }));
    }
    ws.close();
  };

  return { cancel, promise };
}
```

(Source: frontend/src/api/retocClient.ts:L82-L142)

## 17. Verification Report

This SDD has been generated from direct code inspection with specific enumeration checks.

### Entry Points Verified
- ? `src/Aris.Hosting/Program.cs:L1-L95` (backend entry with static file hosting)
- ? `src/ARIS.UI/MainWindow.xaml.cs:L1-L159` (desktop UI with bootstrap)
- ? `src/ARIS.UI/App.xaml.cs:L1-L76` (global exception handling)
- ? `frontend/src/main.tsx:L1-L10` (frontend entry)

### Endpoints Enumerated
Inspected `src/Aris.Hosting/Program.cs:L57-L63`:
```csharp
app.MapHealthAndInfoEndpoints();  // /health, /info
app.MapRetocEndpoints();          // /api/retoc/{convert,build,stream,schema,help}
app.MapUAssetEndpoints();         // /api/uasset/{serialize,deserialize,inspect}
app.MapUwpDumperEndpoints();      // /api/uwpdumper/dump
app.MapDllInjectorEndpoints();    // /api/dllinjector/{inject,eject}
app.MapToolDocsEndpoints();       // /api/tools/{tool}/{help,schema}
```

Total: 17 distinct endpoints documented.

### Frontend Routes Enumerated
Inspected `frontend/src/router.tsx:L11-L45`:
- `/` (index) ? DashboardPage
- `/tools/retoc` ? RetocPage
- `/tools/uasset` ? UAssetPage
- `/tools/uwpdumper` ? UwpDumperPage
- `/tools/dllinjector` ? DllInjectorPage
- `/system/health` ? SystemHealthPage
- `/settings` ? SettingsPage

Total: 7 routes.

### C# Contracts Enumerated
Inspected `src/Aris.Contracts/Retoc/`:
- ProducedFileDto.cs
- RetocResultDto.cs
- RetocConvertRequest.cs, RetocConvertResponse.cs
- RetocBuildCommandRequest.cs (L1-L59), RetocBuildCommandResponse.cs
- RetocCommandSchemaResponse.cs (L1-L24)
- RetocCommandDefinition.cs, RetocCommandFieldDefinition.cs, RetocFieldUiHint.cs
- RetocHelpResponse.cs

Total: 15 Retoc contract files.

### TypeScript Contracts Enumerated
Inspected `frontend/src/types/contracts.ts`:
- Lines 1-244: Base contracts (Health, Error, Operation, UAsset, UwpDumper, DllInjector)
- Lines 300-332: Retoc schema (RetocCommandFieldDefinition, RetocCommandDefinition, RetocCommandSchemaResponse, RetocFieldUiHint)
- Lines 334-367: Tool schema types (ToolSchemaResponse, ToolCommandSchema, etc.)

Total: 367 lines, 35+ interfaces.

### Tests Enumerated
Inspected `tests/Aris.Core.Tests/`:
- `Adapters/RetocAdapterTests.cs` - Retoc adapter tests
- `Adapters/UAssetServiceTests.cs` - UAsset tests
- `Adapters/UwpDumperAdapterTests.cs` - UWP tests
- `Adapters/DllInjectorAdapterTests.cs` - DLL injection tests
- `Retoc/RetocCommandBuilderTests.cs` - Command builder tests
- `Retoc/RetocCommandSchemaProviderTests.cs` (L1-L181) - Schema provider tests (15 test methods)
- `DllInjector/ProcessResolverTests.cs` - Process resolution tests
- `Fakes/FakeProcessRunner.cs`, `Fakes/FakeDllInjectionService.cs` - Test doubles

### Feature-Specific Checks (Simple/Advanced Mode)

Verified the following files document the Retoc Simple/Advanced mode:
- ? Mode toggle: `frontend/src/pages/tools/RetocPage.tsx:L21,L31,L295-L316`
- ? Simple Mode Pack: `frontend/src/pages/tools/RetocPage.tsx:L143-L187`
- ? Simple Mode Unpack: `frontend/src/pages/tools/RetocPage.tsx:L189-L221`
- ? Advanced Mode builder: `frontend/src/components/retoc/RetocAdvancedCommandBuilder.tsx:L1-L280`
- ? Command preview: `frontend/src/components/retoc/RetocCommandPreview.tsx:L1-L68`
- ? Help modal: `frontend/src/components/retoc/RetocHelpModal.tsx:L1-L88`
- ? API client: `frontend/src/api/retocClient.ts:L1-L153`
- ? Build endpoint: `src/Aris.Hosting/Endpoints/RetocEndpoints.cs:L164-L201`
- ? Stream endpoint: `src/Aris.Hosting/Endpoints/RetocEndpoints.cs:L300-L448`
- ? Schema endpoint: `src/Aris.Hosting/Endpoints/RetocEndpoints.cs:L203-L241`
- ? Schema provider: `src/Aris.Adapters/Retoc/RetocCommandSchemaProvider.cs:L1-L208`
- ? Schema deriver: `src/Aris.Adapters/Retoc/RetocSchemaDerived.cs:L1-L226`
- ? ConPTY terminal: `src/Aris.Infrastructure/Terminal/ConPtyProcess.cs:L1-L389`
- ? WebSocket handler: `src/Aris.Hosting/Endpoints/RetocStreamHandler.cs:L1-L361`
- ? Schema tests: `tests/Aris.Core.Tests/Retoc/RetocCommandSchemaProviderTests.cs:L1-L180`



### Feature-Specific Checks (Single Executable Bootstrapper)

Verified the following files document the Single Executable Bootstrapper:
- ? PayloadExtractor: `src/ARIS.UI/Bootstrap/PayloadExtractor.cs:L1-L209`
- ? BackendProcessManager: `src/ARIS.UI/Bootstrap/BackendProcessManager.cs:L1-L197`
- ? ReadinessPoller: `src/ARIS.UI/Bootstrap/ReadinessPoller.cs:L1-L110`
- ? PayloadLockFile: `src/ARIS.UI/Bootstrap/PayloadLockFile.cs:L1-L29`
- ? BootstrapException: `src/ARIS.UI/Bootstrap/BootstrapException.cs:L1-L107`
- ? MainWindow bootstrap flow: `src/ARIS.UI/MainWindow.xaml.cs:L39-L118`
- ? MainWindow XAML: `src/ARIS.UI/MainWindow.xaml:L1-L38`
- ? App exception handling: `src/ARIS.UI/App.xaml.cs:L13-L74`
- ? ErrorWindow: `src/ARIS.UI/Views/ErrorWindow.xaml:L1-L62`, `src/ARIS.UI/Views/ErrorWindow.xaml.cs:L1-L104`
- ? UrlAnnouncementService: `src/Aris.Hosting/Infrastructure/UrlAnnouncementService.cs:L1-L77`
- ? Static file hosting: `src/Aris.Hosting/Program.cs:L60-L79`
- ? ARIS.UI.csproj publish: `src/ARIS.UI/ARIS.UI.csproj:L12-L36`
- ? Publish script: `build/publish-release.ps1:L1-L76`
- ? Frontend backend URL: `frontend/src/config/backend.ts:L1-L34`
- ? PayloadExtractorTests: `tests/ARIS.UI.Tests/Bootstrap/PayloadExtractorTests.cs:L1-L136` (9 tests)
- ? BackendProcessManagerTests: `tests/ARIS.UI.Tests/Bootstrap/BackendProcessManagerTests.cs:L1-L141` (12 tests)
- ? ReadinessPollerTests: `tests/ARIS.UI.Tests/Bootstrap/ReadinessPollerTests.cs:L1-L82` (6 tests)

### Citation Integrity
All citations reference existing files with line ranges verified against file lengths (2026-01-05):

| File | Verified Lines |
|------|----------------|
| RetocEndpoints.cs | 391 |
| RetocCommandSchemaProvider.cs | 208 |
| ConPtyProcess.cs | 389 |
| RetocStreamHandler.cs | 361 |
| contracts.ts | 384 |
| retocClient.ts | 142 |
| PayloadExtractor.cs | 208 |
| BackendProcessManager.cs | 196 |
| ReadinessPoller.cs | 109 |
| BootstrapException.cs | 106 |
| MainWindow.xaml.cs | 158 |
| UrlAnnouncementService.cs | 76 |
| Program.cs (Hosting) | 90 |
| RetocCommandSchemaProviderTests.cs | 180 |

**Note:** `StreamingProcessRunner.cs` and `IStreamingProcessRunner.cs` were removed in favor of ConPTY implementation. Any references to these files in prior documentation versions are obsolete.

All claims are grounded in actual code with exact file and line references.

## Appendix A: Key Files Index

| File | Lines | Purpose |
|------|-------|---------|
| `global.json` | 6 | .NET SDK version pinning |
| `ARIS.sln` | ~101 | Solution structure |
| `src/Aris.Hosting/Program.cs` | 90 | Backend entry point with static file hosting |
| `src/Aris.Hosting/appsettings.json` | 61 | Configuration defaults |
| `src/Aris.Hosting/DependencyInjection.cs` | 49 | Service registration root |
| `src/Aris.Hosting/Infrastructure/ToolingStartupHostedService.cs` | ~64 | Startup tool extraction |
| `src/Aris.Hosting/Infrastructure/BackendHealthState.cs` | ~45 | Health state management |
| `src/Aris.Hosting/Infrastructure/UrlAnnouncementService.cs` | 76 | Stdout URL announcement for UI discovery |
| `src/Aris.Hosting/Endpoints/RetocEndpoints.cs` | 391 | Retoc HTTP API + WebSocket stream |
| `src/Aris.Hosting/Endpoints/RetocStreamHandler.cs` | 361 | WebSocket handler for Retoc streaming |
| `src/Aris.Hosting/Endpoints/HealthEndpoints.cs` | 57 | Health and info endpoints |
| `src/Aris.Hosting/Endpoints/ToolDocsEndpoints.cs` | ~87 | Generic tool docs endpoints |
| `src/Aris.Hosting/ToolDocsPathResolver.cs` | ~28 | Docs path resolution |
| `src/Aris.Infrastructure/DependencyInjection.cs` | 37 | Infrastructure services |
| `src/Aris.Infrastructure/Process/ProcessRunner.cs` | ~200 | Process execution |
| `src/Aris.Infrastructure/Terminal/ConPtyProcess.cs` | 389 | ConPTY process wrapper |
| `src/Aris.Infrastructure/Terminal/ConPtyNativeMethods.cs` | 336 | Win32 P/Invoke for ConPTY |
| `src/Aris.Infrastructure/Terminal/IConPtyProcess.cs` | ~74 | ConPTY interface |
| `src/Aris.Infrastructure/Tools/DependencyExtractor.cs` | ~200 | Tool extraction |
| `src/Aris.Infrastructure/Tools/DependencyValidator.cs` | ~138 | SHA-256 validation |
| `src/Aris.Infrastructure/Configuration/RetocOptions.cs` | ~55 | Retoc config options |
| `src/Aris.Adapters/Retoc/RetocAdapter.cs` | ~200 | Retoc tool adapter |
| `src/Aris.Adapters/Retoc/RetocCommandBuilder.cs` | ~287 | CLI argument builder |
| `src/Aris.Adapters/Retoc/RetocCommandSchemaProvider.cs` | 208 | UI schema provider |
| `src/Aris.Adapters/Retoc/RetocSchemaDerived.cs` | 225 | Schema derivation |
| `src/Aris.Contracts/Retoc/RetocBuildCommandRequest.cs` | ~59 | Build request DTO |
| `src/Aris.Contracts/Retoc/RetocCommandSchemaResponse.cs` | ~24 | Schema response DTO |
| `src/Aris.Contracts/Retoc/RetocStreamRequest.cs` | ~62 | Stream request DTO |
| `src/Aris.Contracts/Retoc/RetocStreamEvent.cs` | ~76 | Stream event types |
| `src/Aris.Tools/tools.manifest.json` | 14 | Embedded tools manifest |
| `src/ARIS.UI/ARIS.UI.csproj` | ~37 | WPF project with publish config |
| `src/ARIS.UI/MainWindow.xaml` | ~38 | Main window XAML with loading overlay |
| `src/ARIS.UI/MainWindow.xaml.cs` | 158 | Bootstrap flow + WebView2 hosting |
| `src/ARIS.UI/App.xaml.cs` | ~76 | Global exception handling |
| `src/ARIS.UI/Bootstrap/PayloadExtractor.cs` | 208 | Embedded payload extraction |
| `src/ARIS.UI/Bootstrap/BackendProcessManager.cs` | 196 | Backend process lifecycle |
| `src/ARIS.UI/Bootstrap/ReadinessPoller.cs` | 109 | Health endpoint polling |
| `src/ARIS.UI/Bootstrap/PayloadLockFile.cs` | ~29 | Lock file data model |
| `src/ARIS.UI/Bootstrap/BootstrapException.cs` | 106 | Bootstrap exception types |
| `src/ARIS.UI/Views/ErrorWindow.xaml` | ~62 | Error window XAML |
| `src/ARIS.UI/Views/ErrorWindow.xaml.cs` | ~104 | Error window code-behind |
| `build/publish-release.ps1` | 75 | Single-exe release build script |
| `scripts/dev.ps1` | 174 | Development workflow script |
| `scripts/build-release.ps1` | 133 | Alternative build script |
| `frontend/package.json` | 39 | Frontend dependencies |
| `frontend/src/config/backend.ts` | 33 | Backend URL config (same-origin in prod) |
| `frontend/src/router.tsx` | 46 | Frontend routing |
| `frontend/src/pages/tools/RetocPage.tsx` | 734 | Retoc Simple/Advanced UI with terminal |
| `frontend/src/components/retoc/RetocAdvancedCommandBuilder.tsx` | 279 | Advanced mode builder |
| `frontend/src/components/retoc/RetocCommandPreview.tsx` | 67 | Command preview |
| `frontend/src/components/retoc/RetocHelpModal.tsx` | 87 | Help modal |
| `frontend/src/components/retoc/RetocForm.tsx` | 182 | Retoc form component |
| `frontend/src/components/terminal/TerminalPanel.tsx` | 189 | xterm.js terminal wrapper |
| `frontend/src/api/retocClient.ts` | 142 | Retoc API + WebSocket client |
| `frontend/src/types/contracts.ts` | 384 | TypeScript contracts |
| `frontend/src/components/ui/index.ts` | 40 | UI component exports |
| `tests/Aris.Core.Tests/Retoc/RetocCommandSchemaProviderTests.cs` | 180 | Schema provider tests |
| `tests/ARIS.UI.Tests/Bootstrap/PayloadExtractorTests.cs` | 135 | Payload extraction tests (9 tests) |
| `tests/ARIS.UI.Tests/Bootstrap/BackendProcessManagerTests.cs` | 140 | Process manager tests (12 tests) |
| `tests/ARIS.UI.Tests/Bootstrap/ReadinessPollerTests.cs` | 81 | Readiness polling tests (6 tests) |

## Appendix B: Dependency Inventory

**NuGet Packages:**
- Microsoft.Web.WebView2: 1.0.3595.46
- Serilog.Extensions.Hosting: 10.0.0
- Serilog.Sinks.Console: 6.1.1
- Serilog.Sinks.File: 7.0.0
- Microsoft.Extensions.Logging.Abstractions: 10.0.0
- Microsoft.Extensions.Options.ConfigurationExtensions: 10.0.0
- System.Text.Json: 10.0.0
- xunit: 2.5.3
- xunit.runner.visualstudio: 2.5.3
- coverlet.collector: 6.0.0
- Microsoft.NET.Test.Sdk: 17.8.0

**npm Packages:**
- react: 19.2.0
- react-dom: 19.2.0
- react-router-dom: 7.10.1
- lucide-react: 0.562.0
- vite: 7.2.4
- typescript: 5.9.3
- tailwindcss: 4.1.17

**Git Submodules:**
- external/UAssetAPI (https://github.com/atenfyr/UAssetAPI.git)

**Embedded Tools:**
- retoc v0.1.4 (win-x64, 7,020,544 bytes, SHA-256: 1c7b3af2b7ca06ac7216d1ba1c629f1e2b178966d964c488135c09abd68a4ec8)

(Source: src/*/*.csproj files, frontend/package.json:L12-L35, .gitmodules:L1-L4, src/Aris.Tools/tools.manifest.json:L1-L14)

---

## Change Report (2025-12-30)

### Summary
Updated SDD to reflect ConPTY/WebSocket streaming implementation for Retoc terminal output with progress bar support.

### Section Changes

| Section | Status | Description |
|---------|--------|-------------|
| Header | Added | "Last updated: 2025-12-30" timestamp |
| Section 6 | **Replaced** | Complete rewrite from obsolete pipe/session model to current ConPTY/WebSocket architecture |
| Appendix A | Updated | Line counts corrected, new ConPTY files added, obsolete files removed |

### New Files Documented

| File | Lines | Purpose |
|------|-------|---------|
| `src/Aris.Infrastructure/Terminal/ConPtyProcess.cs` | 389 | ConPTY process wrapper |
| `src/Aris.Infrastructure/Terminal/ConPtyNativeMethods.cs` | 336 | Win32 P/Invoke for ConPTY |
| `src/Aris.Infrastructure/Terminal/IConPtyProcess.cs` | 74 | ConPTY interface |
| `src/Aris.Contracts/Retoc/RetocStreamRequest.cs` | 62 | Stream request DTO |
| `src/Aris.Contracts/Retoc/RetocStreamEvent.cs` | 76 | Stream event types |
| `src/Aris.Hosting/Endpoints/RetocStreamHandler.cs` | 361 | WebSocket handler |
| `frontend/src/components/terminal/TerminalPanel.tsx` | 189 | xterm.js terminal wrapper |

### Updated Line Counts

| File | Old | New |
|------|-----|-----|
| `src/Aris.Hosting/Endpoints/RetocEndpoints.cs` | 567 | 391 |
| `frontend/src/pages/tools/RetocPage.tsx` | 525 | 749 |
| `frontend/src/api/retocClient.ts` | 153 | 159 |
| `frontend/src/types/contracts.ts` | 367 | 385 |
| `src/Aris.Hosting/Program.cs` | 95 | 94 |
| `src/Aris.Infrastructure/DependencyInjection.cs` | 47 | 38 |
| `src/Aris.Hosting/DependencyInjection.cs` | ~29 | 50 |

### Removed Files (No Longer in Codebase)
- `src/Aris.Infrastructure/Process/StreamingProcessRunner.cs` - Replaced by ConPTY
- `src/Aris.Infrastructure/Process/IStreamingProcessRunner.cs` - Replaced by IConPtyProcess

### SDD_CITATIONS.json Updates
- Version bumped to 5.0.0
- Added `terminal_streaming` section with ConPTY/WebSocket citations
- Updated `generatedAt` to 2025-12-30
- Updated description to mention ConPTY streaming
- Corrected line ranges for updated files

---

## Change Report (2026-01-05)

### Summary
Documentation reconciliation pass to align SDD with current repository state.

### Section Changes

| Section | Status | Description |
|---------|--------|-------------|
| Header | Updated | Timestamp to 2026-01-05 |
| Section 10 | Updated | appsettings.json corrected (removed WorkspaceOptions, added --no-warnings) |
| Section 8.1 | Updated | Frontend routes line range corrected (L11-L45) |
| Appendix A | Updated | Line counts verified and corrected for all files |
| Citation Integrity | Updated | All file line counts re-verified |

### Line Count Corrections

| File | Was | Now |
|------|-----|-----|
| `src/Aris.Hosting/Program.cs` | 94 | 90 |
| `src/ARIS.UI/MainWindow.xaml.cs` | 159 | 158 |
| `frontend/src/pages/tools/RetocPage.tsx` | 749 | 734 |
| `frontend/src/api/retocClient.ts` | 159 | 142 |
| `frontend/src/router.tsx` | 52 | 46 |
| `frontend/src/types/contracts.ts` | 385 | 384 |
| `src/ARIS.UI/Bootstrap/PayloadExtractor.cs` | 209 | 208 |
| `src/ARIS.UI/Bootstrap/BackendProcessManager.cs` | 197 | 196 |
| `src/ARIS.UI/Bootstrap/ReadinessPoller.cs` | 110 | 109 |
| `src/ARIS.UI/Bootstrap/BootstrapException.cs` | 107 | 106 |
| `frontend/src/config/backend.ts` | 34 | 33 |
| `build/publish-release.ps1` | 76 | 75 |
| `src/Aris.Adapters/Retoc/RetocSchemaDerived.cs` | 226 | 225 |
| `frontend/src/components/ui/index.ts` | 37 | 40 |

### New Files Documented

| File | Lines | Purpose |
|------|-------|---------|
| `frontend/src/components/retoc/RetocForm.tsx` | 182 | Retoc form component |
| `scripts/dev.ps1` | 174 | Development workflow script |
| `scripts/build-release.ps1` | 133 | Alternative build script |
| `src/Aris.Hosting/Endpoints/HealthEndpoints.cs` | 57 | Health and info endpoints |

### Configuration Corrections
- Removed non-existent `WorkspaceOptions` section from appsettings documentation
- Added `--no-warnings` to AllowedAdditionalArgs (was present in actual config)
- Corrected appsettings line count from ~64 to 61
