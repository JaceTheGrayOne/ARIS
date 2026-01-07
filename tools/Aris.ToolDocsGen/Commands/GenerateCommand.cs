using Aris.ToolDocsGen.Collectors;
using Aris.ToolDocsGen.Parsers;
using Aris.ToolDocsGen.Schema;
using Aris.ToolDocsGen.Normalization;

namespace Aris.ToolDocsGen.Commands;

/// <summary>
/// Implements the 'generate' command for capturing tool help and generating schema.
/// </summary>
public class GenerateCommand
{
    private readonly ToolHelpCollector _collector = new();
    private readonly HelpParser _parser = new();
    private readonly SchemaEmitter _emitter = new();
    private readonly SchemaMerger _merger = new();

    /// <summary>
    /// Generates documentation for a single tool.
    /// </summary>
    public async Task<int> ExecuteAsync(string toolId, string outputPath, CancellationToken ct = default)
    {
        Console.WriteLine($"Generating docs for tool: {toolId}");

        try
        {
            // Resolve tool path (fails fast if missing)
            var toolPath = _collector.ResolveToolPath(toolId);
            Console.WriteLine($"  Tool path: {toolPath}");

            var toolEntry = _collector.GetToolEntry(toolId);
            var toolDir = Path.Combine(outputPath, toolId.ToLowerInvariant());
            var commandsDir = Path.Combine(toolDir, "commands");

            Directory.CreateDirectory(toolDir);
            Directory.CreateDirectory(commandsDir);

            // Capture main help
            Console.WriteLine("  Capturing main help...");
            var mainHelp = await _collector.CaptureMainHelpAsync(toolPath, ct);
            await File.WriteAllTextAsync(Path.Combine(toolDir, "help.txt"), mainHelp, ct);

            // Discover commands from main help
            var commands = _parser.DiscoverCommands(mainHelp);
            Console.WriteLine($"  Discovered {commands.Count} commands: {string.Join(", ", commands)}");

            // Build schema
            var schema = new ToolSchema
            {
                Tool = toolId.ToLowerInvariant(),
                Version = toolEntry?.Version,
                GeneratedAtUtc = DateTimeOffset.UtcNow,
                Commands = [],
                GlobalOptions = _parser.ParseGlobalOptions(mainHelp)
            };

            // Capture help for each command
            foreach (var cmd in commands)
            {
                Console.WriteLine($"  Capturing help for command: {cmd}");
                var cmdHelp = await _collector.CaptureCommandHelpAsync(toolPath, cmd, ct);

                if (cmdHelp != null)
                {
                    // Save command help
                    var cmdFileName = cmd.Replace("-", "_") + ".txt";
                    await File.WriteAllTextAsync(Path.Combine(commandsDir, cmdFileName), cmdHelp, ct);

                    // Parse command schema
                    var cmdSchema = _parser.ParseCommandHelp(cmd, cmdHelp);
                    schema.Commands.Add(cmdSchema);
                }
                else
                {
                    Console.WriteLine($"    (command does not support --help, creating minimal schema)");
                    // Create minimal schema entry
                    schema.Commands.Add(new ToolCommandSchema
                    {
                        Name = cmd.ToLowerInvariant(),
                        Usages = [$"{Path.GetFileName(toolPath)} {cmd}"],
                        Positionals = []
                    });
                }
            }

            // Write schema.generated.json
            var generatedPath = Path.Combine(toolDir, "schema.generated.json");
            Console.WriteLine($"  Writing {generatedPath}");
            await _emitter.WriteSchemaAsync(schema, generatedPath, ct);

            // Ensure schema.manual.json exists
            var manualPath = Path.Combine(toolDir, "schema.manual.json");
            await _emitter.EnsureManualOverlayAsync(manualPath, ct);

            // Read manual overlay and merge
            var manualOverlay = await _emitter.ReadManualOverlayAsync(manualPath, ct);
            var effectiveSchema = _merger.MergeSchemas(schema, manualOverlay);

            // Write schema.effective.json
            var effectivePath = Path.Combine(toolDir, "schema.effective.json");
            Console.WriteLine($"  Writing {effectivePath}");
            await _emitter.WriteSchemaAsync(effectiveSchema, effectivePath, ct);

            // Write manifest.json
            var manifest = new ToolDocsManifest
            {
                Tool = toolId.ToLowerInvariant(),
                Version = toolEntry?.Version,
                ExeHash = toolEntry?.Sha256,
                GeneratedAtUtc = schema.GeneratedAtUtc,
                Commands = commands
            };
            var manifestPath = Path.Combine(toolDir, "manifest.json");
            Console.WriteLine($"  Writing {manifestPath}");
            await _emitter.WriteManifestAsync(manifest, manifestPath, ct);

            Console.WriteLine($"  Done generating docs for {toolId}");
            return 0;
        }
        catch (FileNotFoundException ex)
        {
            Console.Error.WriteLine($"Error: {ex.Message}");
            return 1;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Error generating docs for {toolId}: {ex.Message}");
            return 1;
        }
    }

    /// <summary>
    /// Generates documentation for all tools in the manifest.
    /// </summary>
    public async Task<int> ExecuteAllAsync(string outputPath, CancellationToken ct = default)
    {
        var toolIds = _collector.GetAllToolIds().ToList();
        Console.WriteLine($"Generating docs for {toolIds.Count} tools: {string.Join(", ", toolIds)}");

        int exitCode = 0;
        foreach (var toolId in toolIds)
        {
            var result = await ExecuteAsync(toolId, outputPath, ct);
            if (result != 0)
            {
                exitCode = result;
            }
        }

        return exitCode;
    }
}
