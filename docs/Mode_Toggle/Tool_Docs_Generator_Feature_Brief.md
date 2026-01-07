## Foundational architecture + implementation plan: Tool Docs Generator + Canonical CLI Schema

### Feature Brief

**Goal**
Create a repo-native, repeatable system that:

1. captures **ground-truth tool help output** from the *same binaries ARIS runs* (tool extraction/validation path),
2. generates a **standardized schema JSON** per tool (commands + usage + positionals; options when confidently parseable),
3. provides a **manual overlay** for non-parseable details,
4. exposes the resulting docs/schema via the backend so the frontend (Advanced Mode) can be schema-driven and stay aligned.

**Non-goals**

* Perfect parsing of every tool’s option formatting across all CLIs.
* Replacing ARIS’s tool execution logic (we only document/describe it).
* Making runtime behavior depend on tool help availability (docs are generated offline and committed).

**User stories**

* As a dev, I can run one command to refresh tool docs/schema and commit the diff.
* As a user, Advanced Mode reflects actual tool syntax and does not offer invalid flags.
* As a maintainer, schema drift is caught by tests (enum coverage, required positional mapping, etc.).

---

## Design decisions

### 1) Source of truth

* **Help snapshots** are authoritative and versioned in repo: `docs/tools/<tool>/help.txt`, `docs/tools/<tool>/commands/<cmd>.txt`.
* **Schema generated** from snapshots: `schema.generated.json`.
* **Schema manual overlay** optional: `schema.manual.json`.
* **Effective schema** is the merge of generated + manual and is what ARIS serves and the UI consumes.

### 2) Where logic lives (layering)

* **Tool extraction/resolution** stays in Infrastructure/Adapters as-is.
* The **generator** is a separate .NET console under `tools/` (does not run as part of ARIS runtime).
* Backend **serves** static docs/schemas from `docs/` (or embedded resources later if desired).

### 3) Parser strategy (conservative)

Parse only what can be extracted reliably:

* commands list (from help sections or by probing known command names)
* `Usage:` lines
* positional args required vs optional using `<>` and `[]` heuristics

Options parsing is “best-effort” only when patterns match strongly; otherwise omit and let manual overlay fill.

### 4) Determinism

Generator must normalize:

* line endings (CRLF)
* strip volatile lines if needed (timestamps, absolute paths) via normalization rules
* stable JSON ordering

---

## Impact analysis (repo areas)

### New / modified areas

**New: ToolDocsGen .NET console**

* `tools/Aris.ToolDocsGen/` (new project)

**Docs**

* `docs/tools/retoc/…` (and later other tools)
* `docs/tools/README.md` describing workflow

**Backend**

* Add generic endpoints:

  * `GET /api/tools/{tool}/help`
  * `GET /api/tools/{tool}/schema`
* (Optional) `GET /api/retoc/help` and `/api/retoc/schema` can remain but should be thin wrappers or replaced by generic route.

**Frontend**

* Retoc Advanced Mode reads schema from `/api/tools/retoc/schema` (or existing retoc schema endpoint if you keep it).

**Tests**

* xUnit tests for:

  * schema coverage vs command enum (RetocCommandType)
  * schema required fields match domain model capabilities
  * schema/help files exist and are parseable

---

## Step-by-step implementation plan

### Phase 1: Establish docs + schema format

1. Create `docs/tools/README.md` describing:

   * how to run generator
   * expected outputs
   * how manual overlay works
2. Add initial folder structure:

   * `docs/tools/retoc/` (empty placeholders OK initially)

**Acceptance**

* Repo contains a clear canonical location for tool docs.

---

### Phase 2: Create ToolDocsGen (.NET) generator

**Create**

* `tools/Aris.ToolDocsGen/Aris.ToolDocsGen.csproj` (net8.0)
* `tools/Aris.ToolDocsGen/Program.cs`

**Behavior**

* CLI:

  * `Aris.ToolDocsGen generate --tool retoc --out docs/tools`
  * `Aris.ToolDocsGen generate --all --out docs/tools`
* Resolves tool executable **using the same tool resolution as ARIS**:

  * preferred: reuse Infrastructure classes by referencing `Aris.Infrastructure` (ok: tool lives outside layering of runtime app)
  * call dependency extraction/validation if needed or reuse the resolved tool directory layout described in SDD (`%LOCALAPPDATA%/ARIS/tools/<version>/...`) (but do not hardcode version; ask resolver)
* Runs:

  * `<tool> --help`
  * for each command:

    * `<tool> <cmd> --help` (if supported; fallback to top-level extraction if not)
* Writes:

  * `docs/tools/<tool>/help.txt`
  * `docs/tools/<tool>/commands/<cmd>.txt`
  * `docs/tools/<tool>/manifest.json` (version, exe hash if available, generatedAtUtc, commands list discovered)

**Normalization**

* Convert all output to CRLF
* Trim trailing spaces
* Optional: redact absolute paths (only if they appear in help)

**Acceptance**

* Running generator produces deterministic files for retoc.

---

### Phase 3: Parse help → schema.generated.json

**Create**

* Parsing module inside ToolDocsGen:

  * `HelpParser` and `SchemaEmitter`
* Output:

  * `docs/tools/<tool>/schema.generated.json`

**Schema format**

* `tool`, `version?`, `generatedAtUtc`
* `commands[]`:

  * `name`
  * `summary?`
  * `usages[]` (raw)
  * `positionals[]`:

    * `name`, `index`, `required`, `typeHint?`
  * `options[]` (only if confidently parseable)

**Acceptance**

* retoc schema marks:

  * `get <INPUT> <CHUNK_ID> [OUTPUT]` (output optional)
  * no unsupported flags invented

---

### Phase 4: Manual overlay + effective schema

**Add**

* `docs/tools/<tool>/schema.manual.json` (optional; can be empty initially)
* Generator emits:

  * `schema.effective.json` = merge(manual over generated)

**Merge rules**

* Manual can:

  * add/override `typeHint`, summaries
  * add `options` entries when parsing can’t extract
  * override requiredness (only if explicitly set)

**Acceptance**

* ToolDocsGen produces effective schema deterministically.

---

### Phase 5: Backend endpoints to serve docs/schema

**Modify**

* `src/Aris.Hosting/Endpoints/*`:

  * Create `ToolDocsEndpoints.cs` (or similar) with:

    * `GET /api/tools/{tool}/help` → returns Markdown or plain text from `docs/tools/<tool>/help.txt`
    * `GET /api/tools/{tool}/schema` → returns parsed JSON from `schema.effective.json`

**Notes**

* Add allowlist for `{tool}` names (retoc/uasset/uwpdumper/dllinjector) to avoid arbitrary file reads.
* Use existing `ErrorInfo` mapping for missing docs (404) and invalid tool name (400).

**Acceptance**

* UI can fetch schema/help from backend without touching filesystem directly.

---

### Phase 6: Retoc Advanced Mode consumes schema from canonical endpoint

**Modify**

* `frontend/src/api/retocClient.ts` or schema-fetch layer:

  * use `/api/tools/retoc/schema` and `/api/tools/retoc/help`
* Remove/stop using hand-maintained retoc schema provider if present (or keep temporarily but ensure frontend uses canonical tool schema).

**Acceptance**

* The UI no longer invents invalid flags (e.g., `--verbose`) because they won’t be present in schema.

---

## Tests

### Backend tests (xUnit)

Add:

* `tests/Aris.Core.Tests/ToolDocs/ToolSchemaCoverageTests.cs` (or similar)

  * Assert every `RetocCommandType` has a corresponding schema command entry.
  * Assert schema required positionals map to fields representable by `RetocCommand` (InputPath/OutputPath/ChunkIndex etc.).
* `tests/Aris.Hosting.Tests` only if such a project exists; otherwise skip endpoint tests and rely on unit + manual verification.

### ToolDocsGen tests (optional but valuable)

* If you add `tools/Aris.ToolDocsGen.Tests`:

  * feed fixture help text and assert parsed schema.

---

## Verification plan

### Generator

1. Build generator:

```bash
dotnet build tools/Aris.ToolDocsGen/Aris.ToolDocsGen.csproj
```

2. Generate retoc docs:

```bash
dotnet run --project tools/Aris.ToolDocsGen -- generate --tool retoc --out docs/tools
```

3. Confirm outputs exist:

* `docs/tools/retoc/help.txt`
* `docs/tools/retoc/schema.effective.json`

### Backend + frontend

```bash
dotnet build
dotnet test
cd frontend
npm install
npm run build
```

Manual:

* Start backend + frontend dev server
* Confirm `GET /api/tools/retoc/schema` returns expected usage for `get` (output optional)
* Confirm Advanced Mode renders fields correctly for `get` and does not emit invalid flags.

---