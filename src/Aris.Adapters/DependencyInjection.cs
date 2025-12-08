using Aris.Adapters.Retoc;
using Aris.Adapters.UAsset;
using Microsoft.Extensions.DependencyInjection;

namespace Aris.Adapters;

public static class DependencyInjection
{
    public static IServiceCollection AddAdapters(this IServiceCollection services)
    {
        services.AddSingleton<IRetocAdapter, RetocAdapter>();

        services.AddSingleton<IUAssetBackend, StubUAssetBackend>();
        services.AddSingleton<IUAssetService, UAssetService>();

        return services;
    }
}
