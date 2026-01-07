namespace Aris.ToolDocsGen.Schema;

/// <summary>
/// Merges generated schema with manual overlay.
/// Enforces that overlays can only annotate existing elements, not add new ones.
/// </summary>
public class SchemaMerger
{
    /// <summary>
    /// Merges a manual overlay into a generated schema.
    /// Returns the effective schema.
    /// </summary>
    public ToolSchema MergeSchemas(ToolSchema generated, ManualOverlay manual)
    {
        // Start with a deep clone of the generated schema
        var effective = generated.DeepClone();

        // Process command overlays
        foreach (var (cmdName, cmdOverlay) in manual.Commands)
        {
            var cmd = effective.Commands.FirstOrDefault(c =>
                string.Equals(c.Name, cmdName, StringComparison.OrdinalIgnoreCase));

            if (cmd == null)
            {
                // RULE: Cannot add commands not in generated schema
                Console.WriteLine($"Warning: Overlay references unknown command '{cmdName}', skipping.");
                continue;
            }

            // Override summary if provided (annotation allowed)
            if (!string.IsNullOrEmpty(cmdOverlay.Summary))
            {
                cmd.Summary = cmdOverlay.Summary;
            }

            // Merge positionals (annotation only - cannot add new positionals)
            foreach (var (posName, posOverlay) in cmdOverlay.Positionals)
            {
                var pos = cmd.Positionals.FirstOrDefault(p =>
                    string.Equals(p.Name, posName, StringComparison.OrdinalIgnoreCase));

                if (pos == null)
                {
                    // RULE: Cannot add positionals not in generated schema
                    Console.WriteLine($"Warning: Overlay references unknown positional '{posName}' in command '{cmdName}', skipping.");
                    continue;
                }

                // Annotations allowed
                if (!string.IsNullOrEmpty(posOverlay.TypeHint))
                {
                    pos.TypeHint = posOverlay.TypeHint;
                }
                if (!string.IsNullOrEmpty(posOverlay.Description))
                {
                    pos.Description = posOverlay.Description;
                }
                if (posOverlay.Required.HasValue)
                {
                    pos.Required = posOverlay.Required.Value;
                }
            }

            // RULE: Options in overlay can only annotate existing options, not add new ones
            if (cmdOverlay.Options != null)
            {
                foreach (var optOverlay in cmdOverlay.Options)
                {
                    var existing = cmd.Options.FirstOrDefault(o =>
                        string.Equals(o.Name, optOverlay.Name, StringComparison.OrdinalIgnoreCase));

                    if (existing == null)
                    {
                        Console.WriteLine($"Warning: Overlay references unknown option '{optOverlay.Name}' in command '{cmdName}', skipping.");
                        continue;
                    }

                    // Annotation allowed
                    if (!string.IsNullOrEmpty(optOverlay.Description))
                    {
                        existing.Description = optOverlay.Description;
                    }
                }
            }
        }

        // Global options: annotation only, no additions
        if (manual.GlobalOptions != null)
        {
            foreach (var optOverlay in manual.GlobalOptions)
            {
                var existing = effective.GlobalOptions.FirstOrDefault(o =>
                    string.Equals(o.Name, optOverlay.Name, StringComparison.OrdinalIgnoreCase));

                if (existing == null)
                {
                    Console.WriteLine($"Warning: Overlay references unknown global option '{optOverlay.Name}', skipping.");
                    continue;
                }

                if (!string.IsNullOrEmpty(optOverlay.Description))
                {
                    existing.Description = optOverlay.Description;
                }
            }
        }

        return effective;
    }
}
