using Aris.Infrastructure.Configuration;
using Aris.Infrastructure.Process;
using Aris.Infrastructure.Tools;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Aris.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.Configure<ToolingOptions>(configuration.GetSection(nameof(ToolingOptions)));
        services.Configure<WorkspaceOptions>(configuration.GetSection(nameof(WorkspaceOptions)));
        services.Configure<RetocOptions>(configuration.GetSection("Retoc"));
        services.Configure<UAssetOptions>(configuration.GetSection("UAsset"));

        services.AddSingleton<IValidateOptions<RetocOptions>, RetocOptionsValidator>();
        services.AddSingleton<IValidateOptions<UAssetOptions>, UAssetOptionsValidator>();

        services.AddSingleton<IDependencyExtractor, DependencyExtractor>();
        services.AddSingleton<IDependencyValidator, DependencyValidator>();
        services.AddSingleton<IProcessRunner, ProcessRunner>();

        return services;
    }
}
