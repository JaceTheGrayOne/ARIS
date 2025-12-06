using System.Text.Json;

namespace Aris.Tools.Manifest;

public static class ToolManifestLoader
{
    public static ToolManifest Load()
    {
        var assembly = typeof(ToolManifestLoader).Assembly;
        using var stream = assembly.GetManifestResourceStream("Aris.Tools.tools.manifest.json");

        if (stream == null)
        {
            return new ToolManifest { Version = "0.1.0", Tools = [] };
        }

        var manifest = JsonSerializer.Deserialize<ToolManifest>(stream);
        return manifest ?? new ToolManifest { Version = "0.1.0", Tools = [] };
    }
}
