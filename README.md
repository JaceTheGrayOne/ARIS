# ARIS – C# Rewrite

ARIS is a Windows desktop application providing unified access to Unreal Engine modding tools:
- **Retoc** - PAK/IoStore container conversion with AES encryption support
- **UAssetAPI** - Unreal asset binary serialization/deserialization
- **UWPDumper** - UWP application memory/file extraction
- **DLL Injector** - Native DLL injection into running processes

This is a C#/.NET 8 clean rewrite of an original Go/Wails implementation.

> **Feature Status:** Retoc and UAssetAPI are production-ready. UWPDumper and DLL Injector have partial implementations. See `docs/SOFTWARE_DESIGN_DOCUMENT.md` Section 1 for detailed maturity status.

## Tech Stack

| Layer | Technology |
|-------|------------|
| Backend | .NET 8, ASP.NET Core Minimal APIs, Serilog |
| Frontend | React 19, TypeScript 5.9, Tailwind CSS 4, Vite 7 |
| Desktop | WPF + WebView2 |
| Testing | xUnit, Coverlet |

## Project Structure

```
src/
├── Aris.Core/           # Domain models, commands, results, errors
├── Aris.Application/    # Use case orchestration (minimal)
├── Aris.Infrastructure/ # Process execution, tool extraction, Win32 P/Invoke
├── Aris.Adapters/       # Tool-specific adapters (Retoc, UAssetAPI, UWPDumper, DllInjector)
├── Aris.Contracts/      # HTTP DTO contracts
├── Aris.Tools/          # Embedded tool binaries + manifest
├── Aris.Hosting/        # DI composition root, HTTP endpoints, static file hosting
└── ARIS.UI/             # WPF desktop host with bootstrap logic

frontend/                # React frontend
tests/                   # xUnit tests
external/UAssetAPI/      # Git submodule
scripts/                 # Development and build scripts
build/                   # Release publish scripts
```

(Source: ARIS.sln:L6-L28, src/ directory structure)

## Build

**Backend:**
```bash
dotnet build
```

**Frontend:**
```bash
cd frontend
npm install
npm run build
```

(Source: frontend/package.json:L8)

## Run (Development)

**Backend (API server):**
```bash
cd src/Aris.Hosting
dotnet run
```
Starts on `http://localhost:5000`. CORS enabled for `http://localhost:5173` in development.

**Frontend (dev server):**
```bash
cd frontend
npm run dev
```
Starts on `http://localhost:5173`.

**Desktop UI:**
Run `ARIS.UI` project in Debug configuration. Falls back to loading `frontend/dist/index.html` from filesystem.

(Source: src/Aris.Hosting/Program.cs:L9-L21, L56-L59)

## Test

```bash
dotnet test
```

Runs tests in:
- `tests/Aris.Core.Tests/` - Backend adapter and domain tests
- `tests/ARIS.UI.Tests/` - Bootstrap component tests

(Source: tests/ directory)

## Publish (Single Executable)

```powershell
powershell -ExecutionPolicy Bypass -File "build/publish-release.ps1"
```

Produces `artifacts/release/ARIS.exe` (~135 MB) - self-contained executable with embedded backend and frontend.

(Source: build/publish-release.ps1:L1-L75)

## Configuration

**Backend:** `src/Aris.Hosting/appsettings.json`
- Tool timeouts (Retoc, UAsset, UwpDumper, DllInjector)
- Allowed/denied targets for DLL injection
- Log size limits

**Runtime paths:**
- Tool binaries: `%LOCALAPPDATA%/ARIS/tools/0.1.0/`
- Logs: `%LOCALAPPDATA%/ARIS/logs/aris-{date}.log`

(Source: src/Aris.Hosting/appsettings.json:L1-L61)

## Environment Variables

| Variable | Default | Description |
|----------|---------|-------------|
| `ASPNETCORE_ENVIRONMENT` | Production | Backend environment |
| `VITE_ARIS_BACKEND_URL` | `http://localhost:5000` | Frontend backend URL override (dev) |

(Source: frontend/src/config/backend.ts:L14-L17)

## Troubleshooting

**Backend not starting:**
- Check `%LOCALAPPDATA%/ARIS/logs/` for error logs
- Verify .NET 8 SDK installed: `dotnet --version`

**Frontend build fails:**
- Ensure Node.js installed: `node --version`
- Clear node_modules: `rm -rf node_modules && npm install`

**Tool extraction fails:**
- Check write permissions to `%LOCALAPPDATA%/ARIS/`
- Verify SHA-256 hash of embedded tools matches manifest

## Documentation

- `docs/SOFTWARE_DESIGN_DOCUMENT.md` - System architecture and design
- `docs/SDD_CITATIONS.json` - Evidence anchors for SDD claims
- `CLAUDE.md` - AI assistant instructions

## Platform Requirements

- Windows 10/11 x64 only
- .NET SDK 8.0.416+ (see `global.json`)
- Node.js for frontend development

(Source: global.json:L2-L5, src/ARIS.UI/ARIS.UI.csproj:L13)
