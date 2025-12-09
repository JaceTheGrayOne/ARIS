using Aris.Adapters.DllInjector;
using Aris.Adapters.Retoc;
using Aris.Adapters.UAsset;
using Aris.Adapters.UwpDumper;
using Microsoft.Extensions.DependencyInjection;

namespace Aris.Adapters;

public static class DependencyInjection
{
    public static IServiceCollection AddAdapters(this IServiceCollection services)
    {
        services.AddSingleton<IRetocAdapter, RetocAdapter>();

        services.AddSingleton<IUAssetBackend, StubUAssetBackend>();
        services.AddSingleton<IUAssetService, UAssetService>();

        services.AddSingleton<IUwpDumperAdapter, UwpDumperAdapter>();

        services.AddSingleton<IProcessResolver, ProcessResolver>();
        services.AddSingleton<IDllInjectorAdapter, DllInjectorAdapter>();

        return services;
    }
}
