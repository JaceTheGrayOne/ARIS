using System.Text.Json;
using Aris.Adapters.Retoc;
using Aris.Core.Retoc;

namespace Aris.Core.Tests.ToolDocs;

/// <summary>
/// Tests that validate the derived schema from canonical + UI mapping overlay.
/// </summary>
public class RetocSchemaDerivedTests
{
    private readonly string _canonicalSchemaJson;
    private readonly string _uiMappingJson;

    public RetocSchemaDerivedTests()
    {
        var repoRoot = GetRepoRoot();
        var canonicalPath = Path.Combine(repoRoot, "docs", "tools", "retoc", "schema.effective.json");
        var mappingPath = Path.Combine(repoRoot, "docs", "tools", "retoc", "ui.mapping.json");

        _canonicalSchemaJson = File.Exists(canonicalPath) ? File.ReadAllText(canonicalPath) : "";
        _uiMappingJson = File.Exists(mappingPath) ? File.ReadAllText(mappingPath) : "";
    }

    [SkippableFact]
    public void DerivedCommandSet_EqualsCanonicalCommandSet()
    {
        Skip.If(string.IsNullOrEmpty(_canonicalSchemaJson), "Canonical schema not found");
        Skip.If(string.IsNullOrEmpty(_uiMappingJson), "UI mapping not found");

        var derived = RetocSchemaDerived.Derive(_canonicalSchemaJson, _uiMappingJson);

        // Parse canonical to get command names
        var canonical = JsonSerializer.Deserialize<CanonicalSchema>(_canonicalSchemaJson,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        Assert.NotNull(canonical);

        // Get canonical command names that map to valid enum values
        var canonicalCommands = canonical.Commands
            .Select(c => NormalizeToEnumName(c.Name))
            .Where(name => Enum.TryParse<RetocCommandType>(name, ignoreCase: true, out _))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var derivedCommands = derived.Commands
            .Select(c => c.CommandType)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        // Derived should contain exactly the commands that exist in both canonical and enum
        Assert.Equal(canonicalCommands.Count, derivedCommands.Count);

        foreach (var cmd in canonicalCommands)
        {
            Assert.Contains(cmd, derivedCommands, StringComparer.OrdinalIgnoreCase);
        }
    }

    [SkippableFact]
    public void GetCommand_HasCorrectRequiredAndOptionalFields()
    {
        Skip.If(string.IsNullOrEmpty(_canonicalSchemaJson), "Canonical schema not found");
        Skip.If(string.IsNullOrEmpty(_uiMappingJson), "UI mapping not found");

        var derived = RetocSchemaDerived.Derive(_canonicalSchemaJson, _uiMappingJson);
        var getCmd = derived.Commands.FirstOrDefault(c => c.CommandType == "Get");

        Assert.NotNull(getCmd);

        // get command should have InputPath and ChunkId as required
        Assert.Contains("InputPath", getCmd.RequiredFields);
        Assert.Contains("ChunkId", getCmd.RequiredFields);

        // OutputPath should be optional for get
        Assert.Contains("OutputPath", getCmd.OptionalFields);
        Assert.DoesNotContain("OutputPath", getCmd.RequiredFields);
    }

    [SkippableFact]
    public void DerivedSchema_DoesNotContainInventedOptions()
    {
        Skip.If(string.IsNullOrEmpty(_canonicalSchemaJson), "Canonical schema not found");
        Skip.If(string.IsNullOrEmpty(_uiMappingJson), "UI mapping not found");

        var derived = RetocSchemaDerived.Derive(_canonicalSchemaJson, _uiMappingJson);

        // Parse canonical to get all options
        var canonical = JsonSerializer.Deserialize<CanonicalSchema>(_canonicalSchemaJson,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        Assert.NotNull(canonical);

        var allCanonicalOptions = canonical.Commands
            .SelectMany(c => c.Options ?? new List<CanonicalOption>())
            .Select(o => o.Name)
            .ToHashSet();

        // AllowlistedFlags should only contain flags from canonical options
        foreach (var flag in derived.AllowlistedFlags)
        {
            Assert.Contains(flag, allCanonicalOptions);
        }
    }

    [SkippableFact]
    public void ToLegacyCommand_HasFolderPathKindForBothPaths()
    {
        Skip.If(string.IsNullOrEmpty(_canonicalSchemaJson), "Canonical schema not found");
        Skip.If(string.IsNullOrEmpty(_uiMappingJson), "UI mapping not found");

        var derived = RetocSchemaDerived.Derive(_canonicalSchemaJson, _uiMappingJson);
        var cmd = derived.Commands.FirstOrDefault(c => c.CommandType == "ToLegacy");

        Assert.NotNull(cmd);
        Assert.NotNull(cmd.FieldUiHints);

        Assert.True(cmd.FieldUiHints.TryGetValue("InputPath", out var inputHint));
        Assert.Equal("folder", inputHint.PathKind);

        Assert.True(cmd.FieldUiHints.TryGetValue("OutputPath", out var outputHint));
        Assert.Equal("folder", outputHint.PathKind);
    }

    [SkippableFact]
    public void ToZenCommand_HasFolderPathKindForBothPaths()
    {
        Skip.If(string.IsNullOrEmpty(_canonicalSchemaJson), "Canonical schema not found");
        Skip.If(string.IsNullOrEmpty(_uiMappingJson), "UI mapping not found");

        var derived = RetocSchemaDerived.Derive(_canonicalSchemaJson, _uiMappingJson);
        var cmd = derived.Commands.FirstOrDefault(c => c.CommandType == "ToZen");

        Assert.NotNull(cmd);
        Assert.NotNull(cmd.FieldUiHints);

        Assert.True(cmd.FieldUiHints.TryGetValue("InputPath", out var inputHint));
        Assert.Equal("folder", inputHint.PathKind);

        Assert.True(cmd.FieldUiHints.TryGetValue("OutputPath", out var outputHint));
        Assert.Equal("folder", outputHint.PathKind);
    }

    [SkippableFact]
    public void GetCommand_HasFilePathKindWithUtocExtension()
    {
        Skip.If(string.IsNullOrEmpty(_canonicalSchemaJson), "Canonical schema not found");
        Skip.If(string.IsNullOrEmpty(_uiMappingJson), "UI mapping not found");

        var derived = RetocSchemaDerived.Derive(_canonicalSchemaJson, _uiMappingJson);
        var cmd = derived.Commands.FirstOrDefault(c => c.CommandType == "Get");

        Assert.NotNull(cmd);
        Assert.NotNull(cmd.FieldUiHints);

        Assert.True(cmd.FieldUiHints.TryGetValue("InputPath", out var inputHint));
        Assert.Equal("file", inputHint.PathKind);
        Assert.NotNull(inputHint.Extensions);
        Assert.Contains(".utoc", inputHint.Extensions);
    }

    [SkippableFact]
    public void InfoCommand_HasOnlyInputPathRequired()
    {
        Skip.If(string.IsNullOrEmpty(_canonicalSchemaJson), "Canonical schema not found");
        Skip.If(string.IsNullOrEmpty(_uiMappingJson), "UI mapping not found");

        var derived = RetocSchemaDerived.Derive(_canonicalSchemaJson, _uiMappingJson);
        var cmd = derived.Commands.FirstOrDefault(c => c.CommandType == "Info");

        Assert.NotNull(cmd);

        // Info command should only require InputPath
        Assert.Single(cmd.RequiredFields);
        Assert.Contains("InputPath", cmd.RequiredFields);

        // No OutputPath required
        Assert.DoesNotContain("OutputPath", cmd.RequiredFields);
    }

    [SkippableFact]
    public void AllDerivedCommands_HaveValidCommandType()
    {
        Skip.If(string.IsNullOrEmpty(_canonicalSchemaJson), "Canonical schema not found");
        Skip.If(string.IsNullOrEmpty(_uiMappingJson), "UI mapping not found");

        var derived = RetocSchemaDerived.Derive(_canonicalSchemaJson, _uiMappingJson);

        foreach (var cmd in derived.Commands)
        {
            var isValidEnum = Enum.TryParse<RetocCommandType>(cmd.CommandType, ignoreCase: true, out _);
            Assert.True(isValidEnum, $"Command '{cmd.CommandType}' is not a valid RetocCommandType");
        }
    }

    [SkippableFact]
    public void AllDerivedCommands_HaveDisplayNameAndDescription()
    {
        Skip.If(string.IsNullOrEmpty(_canonicalSchemaJson), "Canonical schema not found");
        Skip.If(string.IsNullOrEmpty(_uiMappingJson), "UI mapping not found");

        var derived = RetocSchemaDerived.Derive(_canonicalSchemaJson, _uiMappingJson);

        foreach (var cmd in derived.Commands)
        {
            Assert.False(string.IsNullOrWhiteSpace(cmd.DisplayName),
                $"Command '{cmd.CommandType}' has no display name");
            Assert.False(string.IsNullOrWhiteSpace(cmd.Description),
                $"Command '{cmd.CommandType}' has no description");
        }
    }

    [SkippableFact]
    public void DerivedSchema_HasNonEmptyGlobalOptions()
    {
        Skip.If(string.IsNullOrEmpty(_canonicalSchemaJson), "Canonical schema not found");
        Skip.If(string.IsNullOrEmpty(_uiMappingJson), "UI mapping not found");

        var derived = RetocSchemaDerived.Derive(_canonicalSchemaJson, _uiMappingJson);

        // GlobalOptions must be non-empty for Advanced Mode to render fields
        Assert.NotNull(derived.GlobalOptions);
        Assert.NotEmpty(derived.GlobalOptions);

        // Must include at least InputPath and OutputPath
        var fieldNames = derived.GlobalOptions.Select(f => f.FieldName).ToHashSet();
        Assert.Contains("InputPath", fieldNames);
        Assert.Contains("OutputPath", fieldNames);
    }

    [SkippableFact]
    public void DerivedSchema_GlobalOptionsHaveRequiredProperties()
    {
        Skip.If(string.IsNullOrEmpty(_canonicalSchemaJson), "Canonical schema not found");
        Skip.If(string.IsNullOrEmpty(_uiMappingJson), "UI mapping not found");

        var derived = RetocSchemaDerived.Derive(_canonicalSchemaJson, _uiMappingJson);

        foreach (var field in derived.GlobalOptions)
        {
            Assert.False(string.IsNullOrWhiteSpace(field.FieldName),
                "Field must have a FieldName");
            Assert.False(string.IsNullOrWhiteSpace(field.Label),
                $"Field '{field.FieldName}' must have a Label");
            Assert.False(string.IsNullOrWhiteSpace(field.FieldType),
                $"Field '{field.FieldName}' must have a FieldType");
        }
    }

    private static string NormalizeToEnumName(string hyphenated)
    {
        var parts = hyphenated.Split('-');
        return string.Concat(parts.Select(p =>
            string.IsNullOrEmpty(p) ? "" : char.ToUpperInvariant(p[0]) + p[1..]));
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

    #region JSON DTOs for parsing canonical schema

    private sealed class CanonicalSchema
    {
        public string Tool { get; set; } = "";
        public string? Version { get; set; }
        public List<CanonicalCommand> Commands { get; set; } = new();
    }

    private sealed class CanonicalCommand
    {
        public string Name { get; set; } = "";
        public string? Summary { get; set; }
        public List<CanonicalOption>? Options { get; set; }
    }

    private sealed class CanonicalOption
    {
        public string Name { get; set; } = "";
        public bool TakesValue { get; set; }
    }

    #endregion
}
