using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Aris.ToolDocsGen.Schema;

/// <summary>
/// Emits schema and manifest files with deterministic JSON output.
/// </summary>
public class SchemaEmitter
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
    };

    /// <summary>
    /// Writes the generated schema to a file.
    /// </summary>
    public async Task WriteSchemaAsync(ToolSchema schema, string filePath, CancellationToken ct = default)
    {
        var directory = Path.GetDirectoryName(filePath);
        if (!string.IsNullOrEmpty(directory))
        {
            Directory.CreateDirectory(directory);
        }

        // Sort commands alphabetically for determinism
        var sortedSchema = new ToolSchema
        {
            Tool = schema.Tool,
            Version = schema.Version,
            GeneratedAtUtc = schema.GeneratedAtUtc,
            Commands = schema.Commands.OrderBy(c => c.Name).ToList(),
            GlobalOptions = schema.GlobalOptions.OrderBy(o => o.Name).ToList()
        };

        // Sort positionals by index and options by name within each command
        foreach (var cmd in sortedSchema.Commands)
        {
            cmd.Positionals = cmd.Positionals.OrderBy(p => p.Index).ToList();
            cmd.Options = cmd.Options.OrderBy(o => o.Name).ToList();
        }

        var json = JsonSerializer.Serialize(sortedSchema, JsonOptions);
        await File.WriteAllTextAsync(filePath, json + "\r\n", ct);
    }

    /// <summary>
    /// Writes the manifest file.
    /// </summary>
    public async Task WriteManifestAsync(ToolDocsManifest manifest, string filePath, CancellationToken ct = default)
    {
        var directory = Path.GetDirectoryName(filePath);
        if (!string.IsNullOrEmpty(directory))
        {
            Directory.CreateDirectory(directory);
        }

        // Sort commands for determinism
        manifest.Commands = manifest.Commands.OrderBy(c => c).ToList();

        var json = JsonSerializer.Serialize(manifest, JsonOptions);
        await File.WriteAllTextAsync(filePath, json + "\r\n", ct);
    }

    /// <summary>
    /// Creates an empty manual overlay file if it doesn't exist.
    /// </summary>
    public async Task EnsureManualOverlayAsync(string filePath, CancellationToken ct = default)
    {
        if (File.Exists(filePath))
        {
            return;
        }

        var directory = Path.GetDirectoryName(filePath);
        if (!string.IsNullOrEmpty(directory))
        {
            Directory.CreateDirectory(directory);
        }

        var emptyOverlay = new ManualOverlay();
        var json = JsonSerializer.Serialize(emptyOverlay, JsonOptions);
        await File.WriteAllTextAsync(filePath, json + "\r\n", ct);
    }

    /// <summary>
    /// Reads a manual overlay from file.
    /// </summary>
    public async Task<ManualOverlay> ReadManualOverlayAsync(string filePath, CancellationToken ct = default)
    {
        if (!File.Exists(filePath))
        {
            return new ManualOverlay();
        }

        var json = await File.ReadAllTextAsync(filePath, ct);
        return JsonSerializer.Deserialize<ManualOverlay>(json, JsonOptions) ?? new ManualOverlay();
    }
}
