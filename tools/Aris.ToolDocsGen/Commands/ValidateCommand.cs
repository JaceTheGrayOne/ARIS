using System.Text.Json;
using Aris.ToolDocsGen.Schema;

namespace Aris.ToolDocsGen.Commands;

/// <summary>
/// Implements the 'validate' command for checking schema completeness.
/// </summary>
public class ValidateCommand
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    /// <summary>
    /// Validates a tool's schema for completeness.
    /// </summary>
    public async Task<int> ExecuteAsync(string toolId, string docsPath, CancellationToken ct = default)
    {
        Console.WriteLine($"Validating schema for tool: {toolId}");

        var toolDir = Path.Combine(docsPath, toolId.ToLowerInvariant());
        var effectivePath = Path.Combine(toolDir, "schema.effective.json");

        if (!File.Exists(effectivePath))
        {
            Console.Error.WriteLine($"Error: Schema not found at {effectivePath}");
            Console.Error.WriteLine("Run 'generate' first to create the schema.");
            return 1;
        }

        try
        {
            var json = await File.ReadAllTextAsync(effectivePath, ct);
            var schema = JsonSerializer.Deserialize<ToolSchema>(json, JsonOptions);

            if (schema == null)
            {
                Console.Error.WriteLine("Error: Failed to parse schema.");
                return 1;
            }

            int issues = 0;

            // Check that all commands have at least one usage line
            foreach (var cmd in schema.Commands)
            {
                if (cmd.Usages.Count == 0)
                {
                    Console.WriteLine($"  Warning: Command '{cmd.Name}' has no usage lines.");
                    issues++;
                }

                // Check positional indices are contiguous
                var indices = cmd.Positionals.Select(p => p.Index).OrderBy(i => i).ToList();
                for (int i = 0; i < indices.Count; i++)
                {
                    if (indices[i] != i)
                    {
                        Console.WriteLine($"  Warning: Command '{cmd.Name}' has non-contiguous positional indices.");
                        issues++;
                        break;
                    }
                }

                // Check required positionals come before optional
                bool sawOptional = false;
                foreach (var pos in cmd.Positionals.OrderBy(p => p.Index))
                {
                    if (!pos.Required)
                    {
                        sawOptional = true;
                    }
                    else if (sawOptional)
                    {
                        Console.WriteLine($"  Warning: Command '{cmd.Name}' has required positional after optional.");
                        issues++;
                        break;
                    }
                }
            }

            if (issues == 0)
            {
                Console.WriteLine($"  Schema for {toolId} is valid.");
            }
            else
            {
                Console.WriteLine($"  Found {issues} issue(s) in schema for {toolId}.");
            }

            return issues > 0 ? 1 : 0;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Error validating schema: {ex.Message}");
            return 1;
        }
    }
}
