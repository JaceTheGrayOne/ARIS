using Aris.Infrastructure.Tools;

namespace Aris.Hosting;

public class Worker : BackgroundService
{
    private readonly ILogger<Worker> _logger;
    private readonly IDependencyExtractor _dependencyExtractor;

    public Worker(ILogger<Worker> logger, IDependencyExtractor dependencyExtractor)
    {
        _logger = logger;
        _dependencyExtractor = dependencyExtractor;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("ARIS backend worker initialized (Phase 0 - scaffolding only)");

        await _dependencyExtractor.PrepareToolsAsync(stoppingToken);

        _logger.LogInformation("Tool preparation complete");
    }
}
