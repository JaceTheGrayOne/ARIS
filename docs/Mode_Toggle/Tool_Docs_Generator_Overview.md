### Plan components (minimum viable, scalable)

#### 1) Canonical tool help snapshots (ground truth)

* **Folder convention**

  * `docs/tools/<toolName>/`

    * `help.txt` (from `<tool> --help`)
    * `commands/<command>.txt` (from `<tool> <command> --help` when supported)
* Purpose: always available, versionable, reviewable “truth” even if parsing fails.

#### 2) Update script (one command to refresh everything)

* `scripts/update-tool-docs.ps1`

  * resolves tool binaries using ARIS’s existing tool-resolution mechanism
  * captures help outputs into `docs/tools/...`
  * writes a small `manifest.json` per tool (tool version, timestamp, command list discovered)
  * runs schema generation step (below)

#### 3) Parser + generator (help → standardized schema JSON)

* `scripts/tool-docs/parse-help.ps1` (or a small .NET console app under `tools/ToolDocsGen/`)
* Output:

  * `docs/tools/<toolName>/schema.generated.json`
* Parser should be conservative:

  * always store raw `usage` lines
  * extract only what it can confidently detect:

    * commands list
    * positional args + required/optional + order
    * optionally flags/options if reliably detectable

#### 4) Standardized schema format (your contract)

Define one schema format used by ARIS UI/backend for *any* tool:

* `tool`, `version`, `generatedAt`
* `commands[]`:

  * `name`
  * `summary`
  * `usages[]` (raw)
  * `positionals[]` (name/index/required/typeHint)
  * `options[]` (optional, only when parse confidence is high)

This is the schema you standardize—not the tool output.

#### 5) Fallback manual schema (when parsing is weak)

* `docs/tools/<toolName>/schema.manual.json`
* Merge rule:

  * effective schema = `manual` overlays `generated`
  * manual can:

    * correct positional names
    * add type hints
    * define options that help output can’t be parsed for
* This prevents “parser brittleness” from blocking adoption.

#### 6) Backend integration (expose schema/help via API)

* Endpoints per tool:

  * `GET /api/tools/<tool>/help` → returns Markdown/plain help from snapshot
  * `GET /api/tools/<tool>/schema` → returns merged effective schema
* Retoc-specific endpoint (`/api/retoc/schema`) can become a thin wrapper over the generic tool schema endpoint (or be replaced if you want).

#### 7) Frontend integration (schema-driven UI)

* Advanced Mode reads schema and renders:

  * required fields from `positionals.required`
  * optional fields from the optional positionals / options
* This eliminates drift like `get` requiring output or invalid `--verbose`.

#### 8) Drift prevention (tests + CI hooks)

* A test that fails if:

  * `RetocCommandType` has values not present in schema
  * schema requires fields not supported by your command model
  * help snapshots changed but schema wasn’t regenerated (hash check)
* Optional: a CI step that runs `update-tool-docs.ps1` and verifies git is clean (enforces regeneration).

---

### Additions

* A `docs/tools/README.md` describing the workflow (“run script, commit diffs”).
* Normalization in the script to reduce noisy diffs (strip absolute paths, collapse CRLF/LF consistently).
* A small “schema validator” utility that checks the JSON against your schema definition.

---