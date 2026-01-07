Objective
You are implementing the approved G:\Development\ARIS(CS)\ARIS\docs\plans\SIMPLE_ADVANCED_MODE_PLAN.md for the ARIS repository.
This plan has been reviewed and approved. Treat it as BINDING.

Your task is to:
- Implement every backend, frontend, contract, and test change exactly as specified.
- Follow existing ARIS repo patterns, layering, and conventions.
- Produce a complete implementation and an execution summary.

You MUST NOT redesign, reinterpret, or simplify the plan.
You MUST NOT invent new APIs, commands, or UI behavior beyond what is explicitly described.

---

Repo Constraints (Non-Negotiable)
- Architecture layering: UI → Hosting → Adapters → Infrastructure → Core
  - Core must remain dependency-free.
- Backend:
  - ASP.NET Core Minimal APIs
  - Retoc endpoints in `src/Aris.Hosting/Endpoints/RetocEndpoints.cs`
- Contracts:
  - C# DTOs in `src/Aris.Contracts/**`
  - TypeScript contracts in `frontend/src/types/contracts.ts`
- Execution:
  - Retoc command building MUST go through `RetocCommandBuilder` via `IRetocAdapter.BuildCommand`
  - Preview == Execution must be guaranteed by using the same build path
- Streaming:
  - NDJSON over fetch ReadableStream
  - NO EventSource / SSE
- Error handling:
  - Use existing `ArisException` → `ErrorInfo` + HTTP status mapping
- Limits:
  - Respect ProcessRunner semantics (10 MB per stream, 100k lines)
  - Streaming limits configurable via `RetocOptions`
- Frontend:
  - Use existing API client patterns
  - Use React + TypeScript + Tailwind conventions already in repo
- Tests:
  - xUnit with existing fake/test asset patterns
  - Windows-only assumptions are acceptable (PowerShell tests allowed)

---

Implementation Scope (Must Complete All)

Backend:
- Contracts:
  - All DTOs defined in FEATURE_PLAN.md
  - Update both C# and TS contracts
- Adapters:
  - Extend `IRetocAdapter` with `BuildCommand`
  - Implement `BuildCommand` in `RetocAdapter`
  - Add `RetocCommandSchemaProvider`
- Infrastructure:
  - Implement `IStreamingProcessRunner`
  - Implement `StreamingProcessRunner`
  - Align limits with `ProcessRunner`
  - Register in DI
- Hosting:
  - Add endpoints:
    - POST /api/retoc/build
    - GET /api/retoc/schema
    - GET /api/retoc/help
    - POST /api/retoc/stream (NDJSON streaming)
  - Ensure correct flushing, cancellation, and disposal
- Configuration:
  - Extend `RetocOptions` with streaming limits

Frontend:
- Update `frontend/src/types/contracts.ts`
- Implement `retocClient.ts` with NDJSON parsing
- Implement / modify components:
  - RetocPage (mode toggle + orchestration)
  - Simple Mode (Pack / Unpack)
  - Advanced Mode (schema-driven command builder)
  - RetocConsoleLog
  - RetocCommandPreview
  - RetocHelpModal (using react-markdown)
- Ensure:
  - Preview updates via `/api/retoc/build`
  - Execution via `/api/retoc/stream`
  - Abort/cancel works
  - Execution state is clearly represented

Tests:
- Backend:
  - RetocAdapterTests (build correctness for all commands)
  - RetocCommandSchemaProviderTests (schema drift prevention)
  - StreamingProcessRunnerTests (PowerShell deterministic tests)
- Frontend (if test framework exists):
  - retocClient NDJSON parsing
  - Mode toggle + preview behavior

---

Strict Rules
- Do NOT skip files listed in the delta list.
- Do NOT introduce free-form `additionalArgs` UI fields.
- Do NOT change command semantics or Retoc CLI syntax.
- Do NOT downgrade streaming to buffered execution.
- Do NOT remove or weaken schema drift tests.
- Do NOT refactor unrelated code.

If you discover a discrepancy between the plan and the repo:
- Stop
- Document it clearly in EXECUTION_SUMMARY.md
- Apply the safest minimal fix consistent with existing patterns.

---

Output Requirements

After implementation, you MUST produce:

1) Changed Files List
   - Explicit list of every modified file
   - Explicit list of every new file

2) EXECUTION_SUMMARY.md (required)
   Include:
   - Overview of what was implemented
   - Confirmation that preview == execution is enforced
   - Streaming approach used (NDJSON over fetch)
   - Tests added/updated
   - Commands run (or exact commands to run):
     - dotnet build
     - dotnet test
     - npm install
     - npm run build
   - Results (pass/fail)
   - Any assumptions or follow-ups

3) No additional commentary outside the summary.
   No explanations in chat.
   No partial implementations.

---

Verification Checklist (You must self-verify before final output)

- [ ] All backend projects build
- [ ] All backend tests pass
- [ ] Frontend builds without TypeScript errors
- [ ] Simple Mode Pack works end-to-end
- [ ] Simple Mode Unpack works end-to-end
- [ ] Advanced Mode supports all 13 commands
- [ ] Get command enforces ChunkIndex
- [ ] Info command does not require OutputPath
- [ ] Command preview matches executed command exactly
- [ ] Streaming output is live, line-by-line
- [ ] Cancellation kills process
- [ ] No memory growth during long runs
- [ ] Help modal renders markdown correctly

---

Begin implementation now.
