using Aris.Tools.Manifest;
using Microsoft.Extensions.Logging;

namespace Aris.Infrastructure.Tools;

public interface IDependencyExtractor
{
    Task PrepareToolsAsync(CancellationToken cancellationToken = default);
    Task<bool> ValidateToolsAsync(CancellationToken cancellationToken = default);
}

public class DependencyExtractor : IDependencyExtractor
{
    private readonly ILogger<DependencyExtractor> _logger;
    private readonly string _extractionRoot;

    public DependencyExtractor(ILogger<DependencyExtractor> logger)
    {
        _logger = logger;

        var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        var manifest = ToolManifestLoader.Load();
        _extractionRoot = Path.Combine(localAppData, "ARIS", "tools", manifest.Version);
    }

    public Task PrepareToolsAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Preparing tools extraction (Phase 0 stub)");
        _logger.LogInformation("Target extraction root: {ExtractionRoot}", _extractionRoot);

        if (!Directory.Exists(_extractionRoot))
        {
            Directory.CreateDirectory(_extractionRoot);
            _logger.LogInformation("Created extraction directory");
        }
        else
        {
            _logger.LogInformation("Extraction directory already exists");
        }

        var manifest = ToolManifestLoader.Load();
        _logger.LogInformation("Loaded manifest version {Version} with {ToolCount} tool entries",
            manifest.Version, manifest.Tools.Count);

        foreach (var tool in manifest.Tools)
        {
            _logger.LogInformation(
                "Tool entry: {ToolId} v{Version} at {RelativePath} (stub - not extracting)",
                tool.Id, tool.Version, tool.RelativePath);
        }

        return Task.CompletedTask;
    }

    public Task<bool> ValidateToolsAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Validating tools (Phase 0 stub - always returns true)");
        return Task.FromResult(true);
    }
}
