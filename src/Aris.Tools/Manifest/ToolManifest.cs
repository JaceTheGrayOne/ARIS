namespace Aris.Tools.Manifest;

public class ToolManifest
{
    public string Version { get; set; } = string.Empty;
    public List<ToolEntry> Tools { get; set; } = [];
}

public class ToolEntry
{
    public string Id { get; set; } = string.Empty;
    public string Version { get; set; } = string.Empty;
    public string Platform { get; set; } = string.Empty;
    public string Sha256 { get; set; } = string.Empty;
    public long Size { get; set; }
    public string RelativePath { get; set; } = string.Empty;
    public bool Executable { get; set; }
}
