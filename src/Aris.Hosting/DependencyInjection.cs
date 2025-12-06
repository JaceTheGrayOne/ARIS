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

        return services;
    }

    private static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        return Aris.Infrastructure.DependencyInjection.AddInfrastructure(services, configuration);
    }
}
