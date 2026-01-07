using Aris.Adapters.DllInjector;
using Aris.Adapters.Retoc;
using Aris.Adapters.UAsset;
using Aris.Adapters.UwpDumper;
using Aris.Core.DllInjector;
using Aris.Infrastructure.DllInjection;
using Microsoft.Extensions.DependencyInjection;

namespace Aris.Adapters;

public static class DependencyInjection
{
    public static IServiceCollection AddAdapters(this IServiceCollection services)
    {
        services.AddSingleton<IRetocAdapter, RetocAdapter>();

        services.AddSingleton<IUAssetBackend, UAssetApiBackend>();
        services.AddSingleton<IUAssetService, UAssetService>();

        services.AddSingleton<IUwpDumperAdapter, UwpDumperAdapter>();

        // DLL Injector services
        services.AddSingleton<IDllInjectionService, NativeDllInjectionService>();
        services.AddSingleton<IProcessResolver, ProcessResolver>();
        services.AddSingleton<IDllInjectorAdapter, DllInjectorAdapter>();

        return services;
    }
}
