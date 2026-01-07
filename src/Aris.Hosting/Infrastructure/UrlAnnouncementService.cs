using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Aris.Hosting.Infrastructure;

/// <summary>
/// Hosted service that announces the bound backend URL to stdout once Kestrel is ready.
/// The UI bootstrapper captures this to discover the dynamically assigned port.
/// </summary>
public sealed class UrlAnnouncementService : IHostedService
{
    private const string UrlPrefix = "ARIS_BACKEND_URL=";
    private readonly IServer _server;
    private readonly ILogger<UrlAnnouncementService> _logger;
    private bool _announced;

    public UrlAnnouncementService(IServer server, ILogger<UrlAnnouncementService> logger)
    {
        _server = server;
        _logger = logger;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        // The server features may not be available until after the server starts,
        // so we schedule announcement for immediately after startup completes.
        // We use Task.Run to allow the host to continue starting.
        _ = Task.Run(async () =>
        {
            // Small delay to ensure Kestrel has bound the port
            await Task.Delay(100, cancellationToken);
            AnnounceUrl();
        }, cancellationToken);

        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    private void AnnounceUrl()
    {
        if (_announced)
            return;

        var addressFeature = _server.Features.Get<IServerAddressesFeature>();
        if (addressFeature == null)
        {
            _logger.LogWarning("Could not get server addresses feature for URL announcement");
            return;
        }

        var addresses = addressFeature.Addresses.ToList();
        if (addresses.Count == 0)
        {
            _logger.LogWarning("No server addresses available for announcement");
            return;
        }

        // Prefer 127.0.0.1 addresses, normalize localhost to 127.0.0.1
        var address = addresses.FirstOrDefault(a => a.Contains("127.0.0.1"))
                      ?? addresses.FirstOrDefault(a => a.Contains("localhost"))
                      ?? addresses[0];

        // Normalize localhost to 127.0.0.1 for consistency
        address = address.Replace("localhost", "127.0.0.1");

        // Emit the URL announcement line (must be exactly one line, flushed immediately)
        Console.WriteLine($"{UrlPrefix}{address}");
        Console.Out.Flush();

        _announced = true;
        _logger.LogInformation("Announced backend URL: {Url}", address);
    }
}
