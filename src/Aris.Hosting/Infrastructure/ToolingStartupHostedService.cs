using Aris.Infrastructure.Tools;

namespace Aris.Hosting.Infrastructure;

/// <summary>
/// Runs once on startup to prepare and validate embedded tools, then updates backend health.
/// </summary>
public sealed class ToolingStartupHostedService : IHostedService
{
    private readonly IDependencyExtractor _dependencyExtractor;
    private readonly IDependencyValidator _dependencyValidator;
    private readonly BackendHealthState _healthState;
    private readonly ILogger<ToolingStartupHostedService> _logger;

    public ToolingStartupHostedService(
        IDependencyExtractor dependencyExtractor,
        IDependencyValidator dependencyValidator,
        BackendHealthState healthState,
        ILogger<ToolingStartupHostedService> logger)
    {
        _dependencyExtractor = dependencyExtractor;
        _dependencyValidator = dependencyValidator;
        _healthState = healthState;
        _logger = logger;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Tooling startup initialization starting");
        _healthState.Status = "Starting";
        _healthState.DependenciesReady = false;
        _healthState.Message = "Preparing tools";

        try
        {
            await _dependencyExtractor.PrepareToolsAsync(cancellationToken);

            var validation = await _dependencyValidator.ValidateAllAsync(cancellationToken);
            if (!validation.AllValid)
            {
                var invalidCount = validation.TotalCount - validation.ValidCount;
                _logger.LogError("Tool validation failed. {ValidCount} valid, {InvalidCount} invalid tools.",
                    validation.ValidCount,
                    invalidCount);

                _healthState.MarkError("Tool validation failed. See logs for details.");
                return;
            }

            _healthState.MarkReady("Tools prepared and validated.");
            _logger.LogInformation("Tooling startup initialization completed successfully.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Tooling startup initialization failed.");
            _healthState.MarkError("Tool initialization failed. See logs for details.");
        }
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }
}
