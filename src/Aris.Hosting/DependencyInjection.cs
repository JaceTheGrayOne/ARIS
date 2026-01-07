using Aris.Hosting.Endpoints;
using Aris.Infrastructure.Terminal;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;

namespace Aris.Hosting;

public static class DependencyInjection
{
    public static IServiceCollection AddArisBackend(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddInfrastructure(configuration);
        services.AddAdapters();
        services.AddHostingServices();

        return services;
    }

    private static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        return Aris.Infrastructure.DependencyInjection.AddInfrastructure(services, configuration);
    }

    private static IServiceCollection AddAdapters(this IServiceCollection services)
    {
        return Aris.Adapters.DependencyInjection.AddAdapters(services);
    }

    private static IServiceCollection AddHostingServices(this IServiceCollection services)
    {
        // Retoc streaming handler with factory for ConPTY process
        services.AddScoped<RetocStreamHandler>(sp =>
        {
            var retocAdapter = sp.GetRequiredService<Aris.Adapters.Retoc.IRetocAdapter>();
            var logger = sp.GetRequiredService<Microsoft.Extensions.Logging.ILogger<RetocStreamHandler>>();

            // Factory function to create new ConPtyProcess instances
            Func<IConPtyProcess> conPtyFactory = () => sp.GetRequiredService<IConPtyProcess>();

            return new RetocStreamHandler(retocAdapter, conPtyFactory, logger);
        });

        return services;
    }
}
