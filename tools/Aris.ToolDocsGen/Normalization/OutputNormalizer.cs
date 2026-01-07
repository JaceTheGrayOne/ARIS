namespace Aris.ToolDocsGen.Normalization;

/// <summary>
/// Normalizes output for deterministic, diff-friendly results.
/// </summary>
public static class OutputNormalizer
{
    /// <summary>
    /// Normalizes text content: CRLF line endings, trimmed trailing whitespace, single trailing newline.
    /// </summary>
    public static string Normalize(string content)
    {
        // Convert all line endings to LF first, then to CRLF
        content = content.Replace("\r\n", "\n").Replace("\r", "\n");

        // Trim trailing whitespace from each line
        var lines = content.Split('\n');
        lines = lines.Select(l => l.TrimEnd()).ToArray();

        // Join with CRLF and ensure single trailing newline
        return string.Join("\r\n", lines).TrimEnd() + "\r\n";
    }

    /// <summary>
    /// Redacts absolute paths in tool output to make it machine-independent.
    /// </summary>
    public static string RedactAbsolutePaths(string content)
    {
        var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        var toolsRoot = Path.Combine(localAppData, "ARIS", "tools");

        // Replace the tools root path with placeholder
        return content.Replace(toolsRoot, "<TOOLS_ROOT>");
    }
}
