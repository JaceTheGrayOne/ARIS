using System.Security.Cryptography;
using Aris.Tools.Manifest;
using Microsoft.Extensions.Logging;

namespace Aris.Infrastructure.Tools;

public interface IDependencyValidator
{
    Task<DependencyValidationResult> ValidateAllAsync(CancellationToken cancellationToken = default);
    Task<ToolValidationResult> ValidateToolAsync(string toolId, CancellationToken cancellationToken = default);
}

public class DependencyValidator : IDependencyValidator
{
    private readonly ILogger<DependencyValidator> _logger;
    private readonly string _extractionRoot;
    private readonly ToolManifest _manifest;

    public DependencyValidator(ILogger<DependencyValidator> logger)
    {
        _logger = logger;
        _manifest = ToolManifestLoader.Load();

        var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        _extractionRoot = Path.Combine(localAppData, "ARIS", "tools", _manifest.Version);
    }

    public async Task<DependencyValidationResult> ValidateAllAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Validating all {ToolCount} tools", _manifest.Tools.Count);

        var results = new List<ToolValidationResult>();

        foreach (var tool in _manifest.Tools)
        {
            var result = await ValidateToolAsync(tool.Id, cancellationToken);
            results.Add(result);
        }

        var validationResult = new DependencyValidationResult { ToolResults = results };

        _logger.LogInformation(
            "Validation complete: {ValidCount}/{TotalCount} tools valid",
            validationResult.ValidCount,
            validationResult.TotalCount);

        return validationResult;
    }

    public async Task<ToolValidationResult> ValidateToolAsync(string toolId, CancellationToken cancellationToken = default)
    {
        var tool = _manifest.Tools.FirstOrDefault(t => t.Id == toolId);

        if (tool == null)
        {
            _logger.LogWarning("Tool {ToolId} not found in manifest", toolId);
            return new ToolValidationResult
            {
                ToolId = toolId,
                Status = DependencyStatus.Unknown,
                ErrorMessage = "Tool not found in manifest"
            };
        }

        var expectedPath = Path.Combine(_extractionRoot, tool.RelativePath);

        // Check if file exists
        if (!File.Exists(expectedPath))
        {
            _logger.LogWarning("Tool {ToolId} missing at {ExpectedPath}", toolId, expectedPath);
            return new ToolValidationResult
            {
                ToolId = toolId,
                Status = DependencyStatus.Missing,
                ExpectedPath = expectedPath,
                ExpectedHash = tool.Sha256,
                ErrorMessage = $"File not found at {expectedPath}"
            };
        }

        // Compute and verify hash
        string actualHash;
        try
        {
            actualHash = await ComputeFileHashAsync(expectedPath, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to compute hash for {ToolId}", toolId);
            return new ToolValidationResult
            {
                ToolId = toolId,
                Status = DependencyStatus.Unknown,
                ExpectedPath = expectedPath,
                ExpectedHash = tool.Sha256,
                ErrorMessage = $"Failed to compute hash: {ex.Message}"
            };
        }

        if (!string.Equals(actualHash, tool.Sha256, StringComparison.OrdinalIgnoreCase))
        {
            _logger.LogWarning(
                "Hash mismatch for {ToolId}: expected {ExpectedHash}, got {ActualHash}",
                toolId,
                tool.Sha256[..8],
                actualHash[..8]);

            return new ToolValidationResult
            {
                ToolId = toolId,
                Status = DependencyStatus.HashMismatch,
                ExpectedPath = expectedPath,
                ExpectedHash = tool.Sha256,
                ActualHash = actualHash,
                ErrorMessage = $"Hash mismatch: expected {tool.Sha256[..8]}..., got {actualHash[..8]}..."
            };
        }

        _logger.LogDebug("Tool {ToolId} validated successfully", toolId);

        return new ToolValidationResult
        {
            ToolId = toolId,
            Status = DependencyStatus.Valid,
            ExpectedPath = expectedPath,
            ExpectedHash = tool.Sha256,
            ActualHash = actualHash
        };
    }

    private static async Task<string> ComputeFileHashAsync(string filePath, CancellationToken cancellationToken)
    {
        using var sha256 = SHA256.Create();
        await using var stream = File.OpenRead(filePath);
        var hashBytes = await sha256.ComputeHashAsync(stream, cancellationToken);
        return Convert.ToHexString(hashBytes).ToLowerInvariant();
    }
}
