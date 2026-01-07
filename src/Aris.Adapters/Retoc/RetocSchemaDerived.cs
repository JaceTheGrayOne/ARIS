using System.Text.Json;
using System.Text.Json.Serialization;
using Aris.Contracts.Retoc;
using Aris.Core.Retoc;

namespace Aris.Adapters.Retoc;

/// <summary>
/// Derives RetocCommandSchemaResponse from canonical schema + UI mapping overlay.
/// </summary>
public static class RetocSchemaDerived
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    /// <summary>
    /// Derives the domain schema from canonical schema and UI mapping overlay.
    /// </summary>
    public static RetocCommandSchemaResponse Derive(
        string canonicalSchemaJson,
        string uiMappingJson)
    {
        var canonical = JsonSerializer.Deserialize<CanonicalSchema>(canonicalSchemaJson, JsonOptions)
            ?? throw new InvalidOperationException("Failed to parse canonical schema");

        var mapping = JsonSerializer.Deserialize<UiMapping>(uiMappingJson, JsonOptions)
            ?? throw new InvalidOperationException("Failed to parse UI mapping");

        var commands = new List<RetocCommandDefinition>();

        foreach (var cmd in canonical.Commands)
        {
            var commandDef = DeriveCommand(cmd, mapping);
            if (commandDef != null)
            {
                commands.Add(commandDef);
            }
        }

        // Collect allowlisted flags from canonical schema options
        var allowlistedFlags = new HashSet<string>();
        foreach (var cmd in canonical.Commands)
        {
            if (cmd.Options == null) continue;
            foreach (var opt in cmd.Options)
            {
                if (!opt.TakesValue && opt.Name != "--help")
                {
                    allowlistedFlags.Add(opt.Name);
                }
            }
        }

        // Use GlobalOptions from the legacy schema provider (manually curated field definitions)
        var globalOptions = RetocCommandSchemaProvider.GetGlobalOptions();

        return new RetocCommandSchemaResponse
        {
            Commands = commands.ToArray(),
            GlobalOptions = globalOptions,
            AllowlistedFlags = allowlistedFlags.ToArray()
        };
    }

    private static RetocCommandDefinition? DeriveCommand(CanonicalCommand cmd, UiMapping mapping)
    {
        // Convert command name to enum name: "to-legacy" -> "ToLegacy"
        var commandType = NormalizeToEnumName(cmd.Name);

        // Verify the command type exists in RetocCommandType enum
        if (!Enum.TryParse<RetocCommandType>(commandType, ignoreCase: true, out _))
        {
            // Command not in domain model, skip
            return null;
        }

        // Get display name and description from mapping, fallback to derived values
        var displayName = mapping.CommandDisplayNames?.GetValueOrDefault(cmd.Name)
            ?? DeriveDisplayName(cmd.Name);
        var description = mapping.CommandDescriptions?.GetValueOrDefault(cmd.Name)
            ?? cmd.Summary ?? "";

        // Determine required and optional fields
        List<string> requiredFields;
        List<string> optionalFields;

        if (mapping.CommandOverrides?.TryGetValue(cmd.Name, out var overrides) == true)
        {
            // Use explicit overrides
            requiredFields = overrides.RequiredFields?.ToList() ?? new List<string>();
            optionalFields = overrides.OptionalFields?.ToList() ?? new List<string>();
        }
        else
        {
            // Derive from canonical positionals
            requiredFields = new List<string>();
            optionalFields = new List<string>();

            if (cmd.Positionals != null)
            {
                foreach (var pos in cmd.Positionals.OrderBy(p => p.Index))
                {
                    var domainField = MapPositionalToField(pos.Name, mapping);
                    if (domainField == null) continue;

                    if (pos.Required)
                        requiredFields.Add(domainField);
                    else
                        optionalFields.Add(domainField);
                }
            }
        }

        // Get per-command field UI hints
        Dictionary<string, RetocFieldUiHint>? fieldUiHints = null;
        if (mapping.CommandFieldUi?.TryGetValue(cmd.Name, out var cmdFieldUi) == true)
        {
            fieldUiHints = new Dictionary<string, RetocFieldUiHint>();
            foreach (var (fieldName, uiHint) in cmdFieldUi)
            {
                fieldUiHints[fieldName] = new RetocFieldUiHint
                {
                    PathKind = uiHint.PathKind,
                    Extensions = uiHint.Extensions
                };
            }
        }

        return new RetocCommandDefinition
        {
            CommandType = commandType,
            DisplayName = displayName,
            Description = description,
            RequiredFields = requiredFields.ToArray(),
            OptionalFields = optionalFields.ToArray(),
            FieldUiHints = fieldUiHints
        };
    }

    private static string? MapPositionalToField(string positionalName, UiMapping mapping)
    {
        if (mapping.PositionalMappings?.TryGetValue(positionalName, out var field) == true)
        {
            return field;
        }
        // No mapping found
        return null;
    }

    private static string NormalizeToEnumName(string hyphenated)
    {
        var parts = hyphenated.Split('-');
        return string.Concat(parts.Select(p =>
            string.IsNullOrEmpty(p) ? "" : char.ToUpperInvariant(p[0]) + p[1..]));
    }

    private static string DeriveDisplayName(string commandName)
    {
        // "to-legacy" -> "To Legacy"
        var parts = commandName.Split('-');
        return string.Join(" ", parts.Select(p =>
            string.IsNullOrEmpty(p) ? "" : char.ToUpperInvariant(p[0]) + p[1..]));
    }

    #region JSON DTOs for canonical schema and mapping

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
        public List<string> Usages { get; set; } = new();
        public List<CanonicalPositional>? Positionals { get; set; }
        public List<CanonicalOption>? Options { get; set; }
    }

    private sealed class CanonicalPositional
    {
        public string Name { get; set; } = "";
        public int Index { get; set; }
        public bool Required { get; set; }
        public string? TypeHint { get; set; }
    }

    private sealed class CanonicalOption
    {
        public string Name { get; set; } = "";
        public string? ShortName { get; set; }
        public bool TakesValue { get; set; }
        public string? ValueHint { get; set; }
    }

    private sealed class UiMapping
    {
        public Dictionary<string, string>? PositionalMappings { get; set; }
        public Dictionary<string, CommandOverride>? CommandOverrides { get; set; }
        public Dictionary<string, string>? CommandDisplayNames { get; set; }
        public Dictionary<string, string>? CommandDescriptions { get; set; }
        public Dictionary<string, Dictionary<string, FieldUiHint>>? CommandFieldUi { get; set; }
        public Dictionary<string, string>? GlobalOptionsMappings { get; set; }
    }

    private sealed class CommandOverride
    {
        public string[]? RequiredFields { get; set; }
        public string[]? OptionalFields { get; set; }
    }

    private sealed class FieldUiHint
    {
        public string? PathKind { get; set; }
        public string[]? Extensions { get; set; }
    }

    #endregion
}
