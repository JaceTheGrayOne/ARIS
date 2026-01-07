Objective
Plan a new feature for ARIS: a universal Tool Docs + Schema framework backed by ground-truth tool help output.
We want a .NET console generator that captures tool help, parses it into a standardized schema JSON, supports a manual overlay, and exposes help/schema via backend endpoints so the frontend can stay aligned.

Repo constraints (must follow)
- Layering: UI → Hosting → Adapters → Infrastructure → Core (Core dependency-free).
- Backend: ASP.NET Core minimal APIs; endpoints live under `src/Aris.Hosting/Endpoints/*`.
- Contracts/DTOs: `src/Aris.Contracts/*` and `frontend/src/types/contracts.ts` if new API contracts are needed.
- Tool extraction/resolution is via manifest + SHA-256 validation per SDD; do not hardcode tool paths or versions.
- Tests: xUnit with repo patterns in `tests/*`.

Required plan output
Create a single Markdown file named Tool_Docs_Gen_Plan.md containing:

1) Scope
- Goals/non-goals, initial supported tool(s) (at least retoc), and how it generalizes to others.

2) Delta list (file-by-file)
- Explicit create/modify/delete with exact file paths.

3) Generator design (tools/)
- New .NET console project location under `tools/`.
- CLI commands and arguments (generate --tool / --all / --out).
- How it resolves tool exe using the same mechanism ARIS uses.
- What files it writes under `docs/tools/<tool>/` and the normalization rules for determinism.

4) Schema format
- Define a standardized JSON schema structure that can cover multiple tools.
- Specify which fields are required, and the conservative parsing rules (always keep raw usage lines; parse positionals; parse options only when confident).
- Manual overlay file and merge rules.
- Output artifacts: schema.generated.json, schema.manual.json, schema.effective.json.

5) Backend integration
- Endpoints to serve:
  - GET /api/tools/{tool}/help
  - GET /api/tools/{tool}/schema
- Security: tool name allowlist; path traversal protection; error mapping with ErrorInfo.

6) Frontend integration
- Retoc Advanced Mode should read schema/help from the new endpoints.
- Describe migration from current hand-maintained retoc schema provider (remove or keep but unused).
- Ensure UI cannot emit flags not present in schema.

7) Testing plan
- Tests that enforce schema coverage vs RetocCommandType and prevent drift.
- If adding generator tests, where and how.
- Verification steps for generator output determinism.

8) Verification checklist
- Exact commands to run: dotnet build/test, generator run, frontend build/dev.
- Manual checks: confirm get usage shows optional output; invalid flags are absent.

Rules
- Do not guess. Cite existing paths/classes used for tool resolution and current retoc command/schema approach.
- Follow repo conventions for DI, options, error mapping, and tests.
- Output ONLY Tool_Docs_Gen_Plan.md content in Markdown.
