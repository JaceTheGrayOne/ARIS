using System.Text.RegularExpressions;
using Aris.ToolDocsGen.Schema;

namespace Aris.ToolDocsGen.Parsers;

/// <summary>
/// Parses usage lines to extract positional arguments.
/// </summary>
public partial class UsageLineParser
{
    // Match required positionals: <NAME>
    [GeneratedRegex(@"<([A-Z_][A-Z0-9_]*)>", RegexOptions.IgnoreCase)]
    private static partial Regex RequiredPositionalRegex();

    // Match optional positionals: [NAME] (but not [--flag])
    [GeneratedRegex(@"\[([A-Z_][A-Z0-9_]*)\]", RegexOptions.IgnoreCase)]
    private static partial Regex OptionalPositionalRegex();

    // Match optional positionals with ellipsis: [NAME...]
    [GeneratedRegex(@"\[([A-Z_][A-Z0-9_]*)\.\.\.\]", RegexOptions.IgnoreCase)]
    private static partial Regex OptionalVariadicRegex();

    /// <summary>
    /// Parses a usage line and extracts positional arguments.
    /// </summary>
    public List<ToolPositionalSchema> Parse(string usageLine)
    {
        var positionals = new List<ToolPositionalSchema>();
        var index = 0;

        // First pass: find all required positionals with their positions in the string
        var requiredMatches = RequiredPositionalRegex().Matches(usageLine)
            .Cast<Match>()
            .Select(m => new { m.Index, Name = m.Groups[1].Value, Required = true })
            .ToList();

        // Second pass: find optional positionals
        // Skip [OPTIONS] placeholder and option-like patterns
        var optionalMatches = OptionalPositionalRegex().Matches(usageLine)
            .Cast<Match>()
            .Where(m => !m.Value.StartsWith("[--") && !m.Value.StartsWith("[-"))
            .Where(m => !m.Groups[1].Value.Equals("OPTIONS", StringComparison.OrdinalIgnoreCase))
            .Select(m => new { m.Index, Name = m.Groups[1].Value, Required = false })
            .ToList();

        // Third pass: find variadic optionals
        var variadicMatches = OptionalVariadicRegex().Matches(usageLine)
            .Cast<Match>()
            .Select(m => new { m.Index, Name = m.Groups[1].Value, Required = false })
            .ToList();

        // Combine and sort by position in the usage line
        var allPositionals = requiredMatches
            .Concat(optionalMatches)
            .Concat(variadicMatches)
            .DistinctBy(p => p.Index)
            .OrderBy(p => p.Index)
            .ToList();

        foreach (var pos in allPositionals)
        {
            positionals.Add(new ToolPositionalSchema
            {
                Name = pos.Name.ToUpperInvariant(),
                Index = index++,
                Required = pos.Required,
                TypeHint = InferTypeHint(pos.Name)
            });
        }

        return positionals;
    }

    /// <summary>
    /// Infers a type hint from the argument name.
    /// </summary>
    public string InferTypeHint(string argName)
    {
        var upper = argName.ToUpperInvariant();

        // Path-related names
        if (upper.Contains("PATH") || upper.Contains("FILE") || upper.Contains("DIR") ||
            upper.Contains("INPUT") || upper.Contains("OUTPUT"))
        {
            return "path";
        }

        // Integer-related names
        if (upper.Contains("INDEX") || upper.Contains("ID") || upper.Contains("NUM") ||
            upper.Contains("COUNT") || upper.Contains("CHUNK"))
        {
            return "integer";
        }

        // Default to string
        return "string";
    }
}
