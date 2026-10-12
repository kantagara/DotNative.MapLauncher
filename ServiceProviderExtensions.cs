using System;
using Microsoft.Extensions.DependencyInjection;

namespace DotNative.MapLauncher;

public static class MapLauncherServiceProviderExtensions
{
#if NET10_0_OR_GREATER
    extension(IServiceProvider services)
    {
        /// <summary>Resolves the registered plugin using the provider's DI lifetime.</summary>
        public IMapLauncher MapLauncher => services.GetRequiredService<IMapLauncher>();
    }
#else
    /// <summary>Resolves the registered plugin using the provider's DI lifetime.</summary>
    public static IMapLauncher MapLauncher(this IServiceProvider services) =>
        services.GetRequiredService<IMapLauncher>();
#endif
}
