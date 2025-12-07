using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Aris.Tools.Manifest;
using Microsoft.Extensions.Logging;

namespace Aris.Infrastructure.Tools;

public interface IDependencyExtractor
{
    Task PrepareToolsAsync(CancellationToken cancellationToken = default);
}

public class DependencyExtractor : IDependencyExtractor
{
    private readonly ILogger<DependencyExtractor> _logger;
    private readonly string _extractionRoot;
    private readonly ToolManifest _manifest;
    private readonly string _lockFilePath;

    public DependencyExtractor(ILogger<DependencyExtractor> logger)
    {
        _logger = logger;
        _manifest = ToolManifestLoader.Load();

        var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        _extractionRoot = Path.Combine(localAppData, "ARIS", "tools", _manifest.Version);
        _lockFilePath = Path.Combine(_extractionRoot, ".extraction.lock");
    }

    public async Task PrepareToolsAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Preparing tools extraction to {ExtractionRoot}", _extractionRoot);

        // Ensure extraction root exists
        if (!Directory.Exists(_extractionRoot))
        {
            Directory.CreateDirectory(_extractionRoot);
            _logger.LogInformation("Created extraction directory");
        }

        // Check if extraction is needed
        var manifestHash = ComputeManifestHash();
        if (IsExtractionUpToDate(manifestHash))
        {
            _logger.LogInformation("Tools already extracted and up-to-date (manifest hash: {Hash})", manifestHash[..8]);
            return;
        }

        _logger.LogInformation("Extracting {ToolCount} tools from manifest version {Version}",
            _manifest.Tools.Count, _manifest.Version);

        // Extract each tool
        foreach (var tool in _manifest.Tools)
        {
            await ExtractToolAsync(tool, cancellationToken);
        }

        // Write lock file with manifest hash
        await WriteLockFileAsync(manifestHash, cancellationToken);

        _logger.LogInformation("Tool extraction complete");
    }

    private async Task ExtractToolAsync(ToolEntry tool, CancellationToken cancellationToken)
    {
        var targetPath = Path.Combine(_extractionRoot, tool.RelativePath);
        var targetDir = Path.GetDirectoryName(targetPath);

        _logger.LogDebug("Extracting tool {ToolId} to {TargetPath}", tool.Id, targetPath);

        // Ensure target directory exists
        if (!string.IsNullOrEmpty(targetDir) && !Directory.Exists(targetDir))
        {
            Directory.CreateDirectory(targetDir);
        }

        // Try to extract from embedded resources
        var assembly = Assembly.GetAssembly(typeof(ToolManifest));
        if (assembly == null)
        {
            _logger.LogWarning("Could not load Aris.Tools assembly, skipping extraction for {ToolId}", tool.Id);
            return;
        }

        // Resource name pattern: Aris.Tools.EmbeddedTools.{relativePath with / replaced by .}
        var resourceName = $"Aris.Tools.EmbeddedTools.{tool.RelativePath.Replace('/', '.').Replace('\\', '.')}";

        using var resourceStream = assembly.GetManifestResourceStream(resourceName);

        if (resourceStream == null)
        {
            _logger.LogWarning(
                "Tool {ToolId} not found as embedded resource '{ResourceName}', skipping extraction",
                tool.Id,
                resourceName);
            return;
        }

        // Extract to temp file first (atomic write pattern)
        var tempPath = targetPath + ".tmp";

        try
        {
            await using (var fileStream = File.Create(tempPath))
            {
                await resourceStream.CopyToAsync(fileStream, cancellationToken);
            }

            // Verify hash
            var actualHash = await ComputeFileHashAsync(tempPath, cancellationToken);

            if (!string.Equals(actualHash, tool.Sha256, StringComparison.OrdinalIgnoreCase))
            {
                _logger.LogWarning(
                    "Hash mismatch for {ToolId}: expected {ExpectedHash}, got {ActualHash}",
                    tool.Id,
                    tool.Sha256[..8],
                    actualHash[..8]);

                File.Delete(tempPath);
                return;
            }

            // Move temp file to final location
            if (File.Exists(targetPath))
            {
                File.Delete(targetPath);
            }

            File.Move(tempPath, targetPath);

            _logger.LogInformation(
                "Extracted {ToolId} ({SizeBytes} bytes, hash verified)",
                tool.Id,
                tool.Size);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to extract tool {ToolId}", tool.Id);

            if (File.Exists(tempPath))
            {
                try
                {
                    File.Delete(tempPath);
                }
                catch
                {
                    // Ignore cleanup errors
                }
            }
        }
    }

    private bool IsExtractionUpToDate(string currentManifestHash)
    {
        if (!File.Exists(_lockFilePath))
        {
            _logger.LogDebug("Lock file not found, extraction needed");
            return false;
        }

        try
        {
            var lockContent = File.ReadAllText(_lockFilePath);
            var lockData = JsonSerializer.Deserialize<LockFileData>(lockContent);

            if (lockData == null || lockData.ManifestHash != currentManifestHash)
            {
                _logger.LogDebug("Lock file manifest hash mismatch, re-extraction needed");
                return false;
            }

            _logger.LogDebug("Lock file valid, extraction up-to-date");
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to read lock file, will re-extract");
            return false;
        }
    }

    private async Task WriteLockFileAsync(string manifestHash, CancellationToken cancellationToken)
    {
        var lockData = new LockFileData
        {
            ManifestHash = manifestHash,
            ExtractedAt = DateTimeOffset.UtcNow,
            ManifestVersion = _manifest.Version
        };

        var json = JsonSerializer.Serialize(lockData, new JsonSerializerOptions { WriteIndented = true });
        await File.WriteAllTextAsync(_lockFilePath, json, cancellationToken);

        _logger.LogDebug("Wrote lock file with manifest hash {Hash}", manifestHash[..8]);
    }

    private string ComputeManifestHash()
    {
        var json = JsonSerializer.Serialize(_manifest);
        using var sha256 = SHA256.Create();
        var hashBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(json));
        return Convert.ToHexString(hashBytes).ToLowerInvariant();
    }

    private static async Task<string> ComputeFileHashAsync(string filePath, CancellationToken cancellationToken)
    {
        using var sha256 = SHA256.Create();
        await using var stream = File.OpenRead(filePath);
        var hashBytes = await sha256.ComputeHashAsync(stream, cancellationToken);
        return Convert.ToHexString(hashBytes).ToLowerInvariant();
    }

    private class LockFileData
    {
        public string ManifestHash { get; set; } = string.Empty;
        public DateTimeOffset ExtractedAt { get; set; }
        public string ManifestVersion { get; set; } = string.Empty;
    }
}
