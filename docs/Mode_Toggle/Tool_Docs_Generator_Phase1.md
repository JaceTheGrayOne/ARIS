### Direction: use a small .NET console tool as the canonical generator

1. **Collector**: run tool binaries (`--help`, `<cmd> --help`) and save raw outputs
2. **Parser/Extractor**: extract commands + usage + positionals (+ options when confidently parseable)
3. **Emitter**: write `schema.generated.json` + per-tool manifest metadata
4. **Merger**: apply `schema.manual.json` overlay to produce `schema.effective.json` (what the app uses)

---

## Minimal components to implement

### 1) .NET tool project

* Create: `tools/Aris.ToolDocsGen/Aris.ToolDocsGen.csproj` (net8.0)
* Entry: `Program.cs`
* Commands:

  * `generate --tool retoc --out docs/tools/retoc`
  * `generate --all --out docs/tools`
  * Optional: `validate` (checks schema is consistent and complete)

### 2) Tool resolution

The generator must resolve the **actual binary ARIS uses** (not PATH). Reuse the same mechanism as runtime:

* tool manifest extraction/validation + installed tools directory
* or whatever provider ARIS already uses for `RetocAdapter` resolution

This keeps “ground truth” consistent.

### 3) Output layout (stable and diff-friendly)

For each tool:

* `docs/tools/<tool>/help.txt`
* `docs/tools/<tool>/commands/<cmd>.txt`
* `docs/tools/<tool>/manifest.json`

  * tool name
  * resolved exe path (optional, can redact)
  * detected version (if possible)
  * generated timestamp
  * list of commands discovered
* `docs/tools/<tool>/schema.generated.json`
* Optional:

  * `docs/tools/<tool>/schema.manual.json`
  * `docs/tools/<tool>/schema.effective.json`

### 4) Parsing strategy (conservative, stable)

Start with the 80/20:

* Detect commands list from the top-level help output (common patterns: “Commands:”, “Available Commands:”).
* For each command, capture a `Usage:` line (or first “usage” line).
* Parse **positionals** from usage line using simple rules:

  * `<ARG>` required, `[ARG]` optional, brackets indicate optional groups
  * Keep raw usage lines always
* Only parse options/flags when they match a strong pattern (e.g., `--flag`, `-f, --flag`).

If parsing confidence is low, you still emit raw usage and leave options empty.

### 5) Schema format (the standardized contract)

One schema across all tools:

```json
{
  "tool": "retoc",
  "version": "x.y.z",
  "generatedAtUtc": "2025-12-24T...",
  "commands": [
    {
      "name": "get",
      "summary": "...",
      "usages": ["retoc.exe get <INPUT> <CHUNK_ID> [OUTPUT]"],
      "positionals": [
        { "name": "INPUT", "index": 0, "required": true, "typeHint": "path" },
        { "name": "CHUNK_ID", "index": 1, "required": true, "typeHint": "int" },
        { "name": "OUTPUT", "index": 2, "required": false, "typeHint": "path" }
      ],
      "options": []
    }
  ]
}
```

### 6) ARIS integration points

* Backend: schema endpoint should serve **effective schema** from `docs/tools/...` (or embedded resource).
* Retoc Advanced Mode: driven by schema:

  * required/optional fields match usage (fixes the `get`/`--verbose` drift class permanently)

### 7) Drift prevention

Add tests that fail if:

* `RetocCommandType` contains a command not present in effective schema
* schema says a positional exists that your `RetocCommand` model can’t represent
* help snapshot hash changed without regenerating schema (optional but valuable)

---

