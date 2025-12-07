using Aris.Adapters.Retoc;
using Microsoft.Extensions.DependencyInjection;

namespace Aris.Adapters;

public static class DependencyInjection
{
    public static IServiceCollection AddAdapters(this IServiceCollection services)
    {
        services.AddSingleton<IRetocAdapter, RetocAdapter>();

        return services;
    }
}
