namespace Aris.ToolDocsGen.Schema;

/// <summary>
/// Root schema for a tool's documentation.
/// </summary>
public sealed class ToolSchema
{
    public required string Tool { get; set; }
    public string? Version { get; set; }
    public required DateTimeOffset GeneratedAtUtc { get; set; }
    public required List<ToolCommandSchema> Commands { get; set; }
    public List<ToolOptionSchema> GlobalOptions { get; set; } = [];

    public ToolSchema DeepClone()
    {
        return new ToolSchema
        {
            Tool = Tool,
            Version = Version,
            GeneratedAtUtc = GeneratedAtUtc,
            Commands = Commands.Select(c => c.DeepClone()).ToList(),
            GlobalOptions = GlobalOptions.Select(o => o.DeepClone()).ToList()
        };
    }
}

/// <summary>
/// Schema for a single command.
/// </summary>
public sealed class ToolCommandSchema
{
    public required string Name { get; set; }
    public string? Summary { get; set; }
    public required List<string> Usages { get; set; }
    public required List<ToolPositionalSchema> Positionals { get; set; }
    public List<ToolOptionSchema> Options { get; set; } = [];

    public ToolCommandSchema DeepClone()
    {
        return new ToolCommandSchema
        {
            Name = Name,
            Summary = Summary,
            Usages = [.. Usages],
            Positionals = Positionals.Select(p => p.DeepClone()).ToList(),
            Options = Options.Select(o => o.DeepClone()).ToList()
        };
    }
}

/// <summary>
/// Schema for a positional argument.
/// </summary>
public sealed class ToolPositionalSchema
{
    public required string Name { get; set; }
    public required int Index { get; set; }
    public required bool Required { get; set; }
    public string? TypeHint { get; set; }
    public string? Description { get; set; }

    public ToolPositionalSchema DeepClone()
    {
        return new ToolPositionalSchema
        {
            Name = Name,
            Index = Index,
            Required = Required,
            TypeHint = TypeHint,
            Description = Description
        };
    }
}

/// <summary>
/// Schema for an option/flag.
/// </summary>
public sealed class ToolOptionSchema
{
    public required string Name { get; set; }
    public string? ShortName { get; set; }
    public string? Description { get; set; }
    public bool TakesValue { get; set; }
    public string? ValueHint { get; set; }

    public ToolOptionSchema DeepClone()
    {
        return new ToolOptionSchema
        {
            Name = Name,
            ShortName = ShortName,
            Description = Description,
            TakesValue = TakesValue,
            ValueHint = ValueHint
        };
    }
}

/// <summary>
/// Manifest metadata for generated docs.
/// </summary>
public sealed class ToolDocsManifest
{
    public required string Tool { get; set; }
    public string? Version { get; set; }
    public string? ExeHash { get; set; }
    public required DateTimeOffset GeneratedAtUtc { get; set; }
    public required List<string> Commands { get; set; }
}

/// <summary>
/// Manual overlay structure for annotation.
/// </summary>
public sealed class ManualOverlay
{
    public Dictionary<string, CommandOverlay> Commands { get; set; } = [];
    public List<OptionOverlay>? GlobalOptions { get; set; }
}

public sealed class CommandOverlay
{
    public string? Summary { get; set; }
    public Dictionary<string, PositionalOverlay> Positionals { get; set; } = [];
    public List<OptionOverlay>? Options { get; set; }
}

public sealed class PositionalOverlay
{
    public string? TypeHint { get; set; }
    public string? Description { get; set; }
    public bool? Required { get; set; }
}

public sealed class OptionOverlay
{
    public required string Name { get; set; }
    public string? Description { get; set; }
}
