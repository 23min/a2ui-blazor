using A2UI.Blazor.Server.Agents;
using A2UI.Blazor.Server.Streaming;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

namespace A2UI.Blazor.Server;

public static class A2UIServerExtensions
{
    /// <summary>
    /// Register an A2UI agent implementation.
    /// </summary>
    public static IServiceCollection AddA2UIAgent<TAgent>(this IServiceCollection services)
        where TAgent : class, IA2UIAgent
    {
        services.AddSingleton<IA2UIAgent, TAgent>();
        return services;
    }

    /// <summary>
    /// Register all A2UI server services and agents.
    /// </summary>
    public static IServiceCollection AddA2UIServer(this IServiceCollection services)
    {
        // Base services are registered; agents are added via AddA2UIAgent<T>()
        return services;
    }

    /// <summary>
    /// Add the A2UI streaming middleware to the pipeline.
    /// This should be called after UseRouting() if used with other middleware.
    /// </summary>
    public static IApplicationBuilder UseA2UIAgents(this IApplicationBuilder app)
    {
        app.UseMiddleware<A2UIStreamMiddleware>();
        return app;
    }
}
