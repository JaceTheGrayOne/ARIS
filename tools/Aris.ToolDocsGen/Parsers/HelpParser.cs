using System.Text.RegularExpressions;
using Aris.ToolDocsGen.Schema;

namespace Aris.ToolDocsGen.Parsers;

/// <summary>
/// Parses tool help output to extract schema information.
/// Uses conservative parsing: only extracts what can be reliably determined.
/// </summary>
public partial class HelpParser
{
    private readonly UsageLineParser _usageParser = new();

    // Match "Commands:" or "Available Commands:" section headers
    [GeneratedRegex(@"(?:Available\s+)?Commands?:", RegexOptions.IgnoreCase)]
    private static partial Regex CommandsSectionRegex();

    // Match command lines like "  command-name    Description here"
    [GeneratedRegex(@"^\s{2,}([a-z][-a-z0-9]*)\s{2,}(.*)$", RegexOptions.IgnoreCase | RegexOptions.Multiline)]
    private static partial Regex CommandLineRegex();

    // Match "Usage:" lines
    [GeneratedRegex(@"^Usage:\s*(.*)$", RegexOptions.IgnoreCase | RegexOptions.Multiline)]
    private static partial Regex UsageLineRegex();

    // Match options like "--flag" or "-f, --flag" or "--option <VALUE>"
    [GeneratedRegex(@"^\s+(-[a-zA-Z],\s+)?--([a-z][-a-z0-9]*)(?:\s+<([^>]+)>)?", RegexOptions.IgnoreCase | RegexOptions.Multiline)]
    private static partial Regex OptionLineRegex();

    /// <summary>
    /// Discovers command names from the main help output.
    /// </summary>
    public List<string> DiscoverCommands(string mainHelp)
    {
        var commands = new List<string>();

        // Normalize line endings
        mainHelp = mainHelp.Replace("\r\n", "\n").Replace("\r", "\n");

        // Look for "Commands:" section
        var commandsSectionMatch = CommandsSectionRegex().Match(mainHelp);
        if (!commandsSectionMatch.Success)
        {
            return commands;
        }

        // Get text after "Commands:" header
        var afterCommands = mainHelp[(commandsSectionMatch.Index + commandsSectionMatch.Length)..];

        // Find all command lines until we hit an empty line or different section
        var lines = afterCommands.Split('\n');
        foreach (var line in lines)
        {
            var trimmed = line.Trim();

            // Check if this looks like a section header (ends with : and no spaces before it)
            if (trimmed.EndsWith(':') && !trimmed.Contains(' '))
            {
                break;
            }

            // Skip empty lines only after we've found at least one command
            if (string.IsNullOrEmpty(trimmed))
            {
                if (commands.Count > 0)
                    break; // End of commands section
                continue;
            }

            // Try to match command line pattern: "  command-name    Description"
            var match = CommandLineRegex().Match(line);
            if (match.Success)
            {
                var commandName = match.Groups[1].Value.Trim();
                if (!string.IsNullOrEmpty(commandName) && commandName != "help")
                {
                    commands.Add(commandName);
                }
            }
        }

        return commands;
    }

    /// <summary>
    /// Parses command help output into a schema.
    /// </summary>
    public ToolCommandSchema ParseCommandHelp(string commandName, string helpOutput)
    {
        var usages = ExtractUsageLines(helpOutput);
        var positionals = new List<ToolPositionalSchema>();
        var options = new List<ToolOptionSchema>();

        // Parse positionals from usage lines
        foreach (var usage in usages)
        {
            var parsed = _usageParser.Parse(usage);
            foreach (var pos in parsed)
            {
                // Avoid duplicates by name
                if (!positionals.Any(p => p.Name == pos.Name))
                {
                    positionals.Add(pos);
                }
            }
        }

        // Re-index positionals to be contiguous
        for (int i = 0; i < positionals.Count; i++)
        {
            positionals[i].Index = i;
        }

        // Parse options (conservative: only when pattern is clear)
        options = ExtractOptions(helpOutput);

        return new ToolCommandSchema
        {
            Name = commandName.ToLowerInvariant(),
            Summary = ExtractSummary(helpOutput),
            Usages = usages,
            Positionals = positionals,
            Options = options
        };
    }

    /// <summary>
    /// Parses global options from main help output.
    /// </summary>
    public List<ToolOptionSchema> ParseGlobalOptions(string mainHelp)
    {
        // Look for options that appear before "Commands:" section
        var commandsMatch = CommandsSectionRegex().Match(mainHelp);
        if (commandsMatch.Success)
        {
            var beforeCommands = mainHelp[..commandsMatch.Index];
            return ExtractOptions(beforeCommands);
        }

        return ExtractOptions(mainHelp);
    }

    private List<string> ExtractUsageLines(string helpOutput)
    {
        var usages = new List<string>();
        var matches = UsageLineRegex().Matches(helpOutput);

        foreach (Match match in matches)
        {
            var usage = match.Groups[1].Value.Trim();
            if (!string.IsNullOrEmpty(usage))
            {
                usages.Add(usage);
            }
        }

        // If no explicit Usage: line found, try to extract from first line
        if (usages.Count == 0)
        {
            var firstLine = helpOutput.Split('\n').FirstOrDefault()?.Trim();
            if (!string.IsNullOrEmpty(firstLine) &&
                (firstLine.Contains('<') || firstLine.Contains('[')))
            {
                usages.Add(firstLine);
            }
        }

        return usages;
    }

    private List<ToolOptionSchema> ExtractOptions(string helpOutput)
    {
        var options = new List<ToolOptionSchema>();
        var matches = OptionLineRegex().Matches(helpOutput);

        foreach (Match match in matches)
        {
            var shortName = match.Groups[1].Success
                ? match.Groups[1].Value.Trim().TrimEnd(',').Trim()
                : null;
            var longName = "--" + match.Groups[2].Value;
            var valueHint = match.Groups[3].Success ? match.Groups[3].Value : null;

            // Avoid duplicates
            if (!options.Any(o => o.Name == longName))
            {
                options.Add(new ToolOptionSchema
                {
                    Name = longName,
                    ShortName = shortName,
                    TakesValue = !string.IsNullOrEmpty(valueHint),
                    ValueHint = valueHint
                });
            }
        }

        return options;
    }

    private static string? ExtractSummary(string helpOutput)
    {
        // Try to get the first non-empty line that isn't a usage line
        var lines = helpOutput.Split('\n')
            .Select(l => l.Trim())
            .Where(l => !string.IsNullOrEmpty(l))
            .Where(l => !l.StartsWith("Usage:", StringComparison.OrdinalIgnoreCase))
            .Where(l => !l.StartsWith("-"))
            .ToList();

        if (lines.Count > 0)
        {
            var firstLine = lines[0];
            // Skip if it looks like a command line
            if (!firstLine.Contains('<') && !firstLine.Contains('[') && firstLine.Length < 200)
            {
                return firstLine;
            }
        }

        return null;
    }
}
