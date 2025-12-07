using System.Text.Json;

namespace Aris.Tools.Manifest;

public static class ToolManifestLoader
{
    private static readonly JsonSerializerOptions JsonOptions = new JsonSerializerOptions
    {
        PropertyNameCaseInsensitive = true
    };

    public static ToolManifest Load()
    {
        var assembly = typeof(ToolManifestLoader).Assembly;
        using var stream = assembly.GetManifestResourceStream("Aris.Tools.tools.manifest.json");

        if (stream == null)
        {
            return new ToolManifest { Version = "0.1.0", Tools = [] };
        }

        var manifest = JsonSerializer.Deserialize<ToolManifest>(stream, JsonOptions);
        return manifest ?? new ToolManifest { Version = "0.1.0", Tools = [] };
    }
}
