using Aris.Infrastructure.Configuration;
using Aris.Infrastructure.Tools;
using Microsoft.Extensions.Options;

namespace Aris.Hosting;

public class Worker : BackgroundService
{
    private readonly ILogger<Worker> _logger;
    private readonly IDependencyExtractor _dependencyExtractor;
    private readonly RetocOptions _retocOptions;

    public Worker(
        ILogger<Worker> logger,
        IDependencyExtractor dependencyExtractor,
        IOptions<RetocOptions> retocOptions)
    {
        _logger = logger;
        _dependencyExtractor = dependencyExtractor;
        _retocOptions = retocOptions.Value; // Triggers validation at startup
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("ARIS backend worker initialized (Phase 0 - scaffolding only)");

        await _dependencyExtractor.PrepareToolsAsync(stoppingToken);

        _logger.LogInformation("Tool preparation complete");
        _logger.LogInformation(
            "Retoc configured with timeout: {TimeoutSeconds}s, max log: {MaxLogBytes} bytes",
            _retocOptions.DefaultTimeoutSeconds,
            _retocOptions.MaxLogBytes);
    }
}
