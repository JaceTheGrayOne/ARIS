using System.Text.Json;
using Aris.Core.Retoc;

namespace Aris.Core.Tests.ToolDocs;

/// <summary>
/// Tests that ensure schema coverage and alignment with domain models.
/// These tests validate the generated schema without hardcoding positional names.
/// </summary>
public class ToolSchemaCoverageTests
{
    private readonly ToolSchema? _schema;
    private readonly string _schemaPath;

    public ToolSchemaCoverageTests()
    {
        _schemaPath = Path.Combine(
            GetRepoRoot(),
            "docs", "tools", "retoc", "schema.effective.json");

        if (File.Exists(_schemaPath))
        {
            var json = File.ReadAllText(_schemaPath);
            _schema = JsonSerializer.Deserialize<ToolSchema>(json,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        }
    }

    [SkippableFact]
    public void AllRetocCommandTypes_HaveSchemaEntry()
    {
        Skip.If(_schema == null, $"Schema not found at {_schemaPath}. Run ToolDocsGen first.");

        // Normalize enum names: UnpackRaw -> unpack-raw
        var enumNames = Enum.GetNames<RetocCommandType>()
            .Select(NormalizeToHyphenated)
            .ToHashSet();

        var schemaCommands = _schema!.Commands
            .Select(c => c.Name.ToLowerInvariant())
            .ToHashSet();

        var missing = enumNames.Except(schemaCommands).ToList();

        Assert.Empty(missing);
    }

    [SkippableFact]
    public void SchemaCommands_MapToValidRetocCommandType()
    {
        Skip.If(_schema == null, $"Schema not found at {_schemaPath}. Run ToolDocsGen first.");

        foreach (var cmd in _schema!.Commands)
        {
            // Normalize: unpack-raw -> UnpackRaw
            var normalized = NormalizeToEnumName(cmd.Name);
            var parsed = Enum.TryParse<RetocCommandType>(normalized, ignoreCase: true, out _);
            Assert.True(parsed, $"Schema command '{cmd.Name}' (normalized: '{normalized}') has no matching RetocCommandType");
        }
    }

    /// <summary>
    /// Converts PascalCase enum name to hyphenated lowercase: UnpackRaw -> unpack-raw
    /// </summary>
    private static string NormalizeToHyphenated(string enumName)
    {
        var result = new System.Text.StringBuilder();
        for (int i = 0; i < enumName.Length; i++)
        {
            var c = enumName[i];
            if (i > 0 && char.IsUpper(c))
            {
                result.Append('-');
            }
            result.Append(char.ToLowerInvariant(c));
        }
        return result.ToString();
    }

    /// <summary>
    /// Converts hyphenated lowercase to PascalCase: unpack-raw -> UnpackRaw
    /// </summary>
    private static string NormalizeToEnumName(string hyphenated)
    {
        var parts = hyphenated.Split('-');
        return string.Concat(parts.Select(p =>
            string.IsNullOrEmpty(p) ? "" : char.ToUpperInvariant(p[0]) + p[1..]));
    }

    [SkippableFact]
    public void EachCommand_HasAtLeastOneUsageLine()
    {
        Skip.If(_schema == null, $"Schema not found at {_schemaPath}. Run ToolDocsGen first.");

        foreach (var cmd in _schema!.Commands)
        {
            Assert.NotEmpty(cmd.Usages);
        }
    }

    [SkippableFact]
    public void RequiredPositionals_HaveValidTypeHints()
    {
        Skip.If(_schema == null, $"Schema not found at {_schemaPath}. Run ToolDocsGen first.");

        var validTypeHints = new HashSet<string> { "path", "integer", "string", "enum" };

        foreach (var cmd in _schema!.Commands)
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

    [SkippableFact]
    public void RequiredPositionalCount_IsRepresentableByDomainModel()
    {
        Skip.If(_schema == null, $"Schema not found at {_schemaPath}. Run ToolDocsGen first.");

        // RetocCommand has: InputPath, OutputPath, ChunkId, Version, AesKey
        // This means at most 5 distinct positional bindings are supported
        const int maxSupportedPositionals = 5;

        foreach (var cmd in _schema!.Commands)
        {
            var requiredCount = cmd.Positionals.Count(p => p.Required);
            Assert.True(requiredCount <= maxSupportedPositionals,
                $"Command '{cmd.Name}' has {requiredCount} required positionals, " +
                $"but RetocCommand can only represent {maxSupportedPositionals}");
        }
    }

    [SkippableFact]
    public void OptionalPositionals_AreMarkedCorrectly()
    {
        Skip.If(_schema == null, $"Schema not found at {_schemaPath}. Run ToolDocsGen first.");

        foreach (var cmd in _schema!.Commands)
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

    [SkippableFact]
    public void PositionalTypeHints_AreConsistentWithDomainModelTypes()
    {
        Skip.If(_schema == null, $"Schema not found at {_schemaPath}. Run ToolDocsGen first.");

        // Validate that type hints align with what RetocCommand can represent
        var validHints = new HashSet<string> { "path", "integer", "string", "enum" };

        foreach (var cmd in _schema!.Commands)
        {
            foreach (var pos in cmd.Positionals)
            {
                if (string.IsNullOrEmpty(pos.TypeHint)) continue;

                var hint = pos.TypeHint.ToLowerInvariant();
                Assert.True(
                    validHints.Contains(hint),
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

    // Schema model classes for deserialization
    private sealed class ToolSchema
    {
        public string Tool { get; set; } = "";
        public string? Version { get; set; }
        public DateTimeOffset GeneratedAtUtc { get; set; }
        public List<ToolCommandSchema> Commands { get; set; } = [];
        public List<ToolOptionSchema> GlobalOptions { get; set; } = [];
    }

    private sealed class ToolCommandSchema
    {
        public string Name { get; set; } = "";
        public string? Summary { get; set; }
        public List<string> Usages { get; set; } = [];
        public List<ToolPositionalSchema> Positionals { get; set; } = [];
        public List<ToolOptionSchema> Options { get; set; } = [];
    }

    private sealed class ToolPositionalSchema
    {
        public string Name { get; set; } = "";
        public int Index { get; set; }
        public bool Required { get; set; }
        public string? TypeHint { get; set; }
        public string? Description { get; set; }
    }

    private sealed class ToolOptionSchema
    {
        public string Name { get; set; } = "";
        public string? ShortName { get; set; }
        public string? Description { get; set; }
        public bool TakesValue { get; set; }
        public string? ValueHint { get; set; }
    }
}
