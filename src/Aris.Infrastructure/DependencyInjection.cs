using Aris.Infrastructure.Configuration;
using Aris.Infrastructure.Tools;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Aris.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.Configure<ToolingOptions>(configuration.GetSection(nameof(ToolingOptions)));
        services.Configure<WorkspaceOptions>(configuration.GetSection(nameof(WorkspaceOptions)));

        services.AddSingleton<IDependencyExtractor, DependencyExtractor>();

        return services;
    }
}
