using System.Diagnostics;
using Aris.Tools.Manifest;
using Aris.ToolDocsGen.Normalization;

namespace Aris.ToolDocsGen.Collectors;

/// <summary>
/// Collects help output from tool binaries.
/// Read-only: does not extract, validate, or mutate tool state.
/// </summary>
public class ToolHelpCollector
{
    /// <summary>
    /// Resolves the path to a tool binary based on the manifest.
    /// Fails fast with FileNotFoundException if binary is missing.
    /// </summary>
    public string ResolveToolPath(string toolId)
    {
        var manifest = ToolManifestLoader.Load();
        var entry = manifest.Tools.FirstOrDefault(t =>
            string.Equals(t.Id, toolId, StringComparison.OrdinalIgnoreCase))
            ?? throw new InvalidOperationException(
                $"Tool '{toolId}' not found in manifest. " +
                $"Available tools: {string.Join(", ", manifest.Tools.Select(t => t.Id))}");

        var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        var toolsRoot = Path.Combine(localAppData, "ARIS", "tools", manifest.Version);
        var toolPath = Path.Combine(toolsRoot, entry.RelativePath);

        if (!File.Exists(toolPath))
        {
            throw new FileNotFoundException(
                $"Tool binary not found at '{toolPath}'. " +
                $"Ensure ARIS has been run at least once to extract tools, or manually extract the tool.",
                toolPath);
        }

        return toolPath;
    }

    /// <summary>
    /// Gets the tool entry from manifest for metadata purposes.
    /// </summary>
    public ToolEntry? GetToolEntry(string toolId)
    {
        var manifest = ToolManifestLoader.Load();
        return manifest.Tools.FirstOrDefault(t =>
            string.Equals(t.Id, toolId, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// Gets all tool IDs from manifest.
    /// </summary>
    public IEnumerable<string> GetAllToolIds()
    {
        var manifest = ToolManifestLoader.Load();
        return manifest.Tools.Select(t => t.Id);
    }

    /// <summary>
    /// Captures the main help output from a tool.
    /// </summary>
    public async Task<string> CaptureMainHelpAsync(string toolPath, CancellationToken ct = default)
    {
        var output = await RunToolAsync(toolPath, "--help", ct);
        return OutputNormalizer.Normalize(OutputNormalizer.RedactAbsolutePaths(output));
    }

    /// <summary>
    /// Captures help output for a specific command.
    /// Returns null if the command doesn't support --help or errors.
    /// </summary>
    public async Task<string?> CaptureCommandHelpAsync(string toolPath, string command, CancellationToken ct = default)
    {
        try
        {
            var output = await RunToolAsync(toolPath, $"{command} --help", ct);
            return OutputNormalizer.Normalize(OutputNormalizer.RedactAbsolutePaths(output));
        }
        catch
        {
            // Command may not support --help, return null
            return null;
        }
    }

    private static async Task<string> RunToolAsync(string toolPath, string arguments, CancellationToken ct)
    {
        using var process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = toolPath,
                Arguments = arguments,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            }
        };

        process.Start();

        var stdout = await process.StandardOutput.ReadToEndAsync(ct);
        var stderr = await process.StandardError.ReadToEndAsync(ct);

        await process.WaitForExitAsync(ct);

        // Combine stdout and stderr, preferring stdout if available
        return !string.IsNullOrWhiteSpace(stdout) ? stdout : stderr;
    }
}
