using A2UI.Blazor.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace A2UI.Blazor;

public static class A2UIBlazorServiceExtensions
{
    /// <summary>
    /// Registers A2UI Blazor services: SurfaceManager, MessageDispatcher,
    /// JsonlStreamReader, A2UIStreamClient, ComponentRegistry (with all standard components).
    /// </summary>
    public static IServiceCollection AddA2UIBlazor(
        this IServiceCollection services,
        Action<ComponentRegistry>? configureComponents = null,
        Action<LocalActionRegistry>? configureLocalActions = null)
    {
        services.AddSingleton<SurfaceManager>();
        services.AddSingleton<MessageDispatcher>();
        services.AddTransient<JsonlStreamReader>();
        services.AddTransient<A2UIStreamClient>();

        services.AddSingleton(sp =>
        {
            var logger = sp.GetRequiredService<ILogger<ComponentRegistry>>();
            var registry = new ComponentRegistry(logger);
            registry.RegisterStandardComponents();
            configureComponents?.Invoke(registry);
            return registry;
        });

        services.AddSingleton(sp =>
        {
            var registry = new LocalActionRegistry();
            configureLocalActions?.Invoke(registry);
            return registry;
        });

        return services;
    }
}
