using System.Text.Json;
using A2UI.Blazor.Protocol;
using A2UI.Blazor.Services;
using Bunit;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;

namespace A2UI.Blazor.Tests.Helpers;

/// <summary>
/// Shared bUnit test context with A2UI services pre-registered.
/// </summary>
public class SurfaceTestContext : BunitContext
{
    public SurfaceManager SurfaceManager { get; }
    public MessageDispatcher Dispatcher { get; }
    public ComponentRegistry Registry { get; }

    public SurfaceTestContext()
    {
        Registry = new ComponentRegistry(NullLogger<ComponentRegistry>.Instance);
        Registry.RegisterStandardComponents();

        SurfaceManager = new SurfaceManager(NullLogger<SurfaceManager>.Instance);
        Dispatcher = new MessageDispatcher(SurfaceManager, NullLogger<MessageDispatcher>.Instance);

        Services.AddSingleton(Registry);
        Services.AddSingleton(SurfaceManager);
        Services.AddSingleton(Dispatcher);
        Services.AddTransient<JsonlStreamReader>();
    }

    /// <summary>
    /// Create a component data bag from a type name and property dictionary.
    /// </summary>
    public static A2UIComponentData MakeComponent(string id, string component, Dictionary<string, object>? props = null)
    {
        var json = new Dictionary<string, JsonElement>();
        if (props is not null)
        {
            foreach (var (key, value) in props)
            {
                var element = JsonSerializer.SerializeToElement(value);
                json[key] = element;
            }
        }

        return new A2UIComponentData
        {
            Id = id,
            Component = component,
            Properties = json
        };
    }

    /// <summary>
    /// Create a surface with components and optional data model, ready for rendering.
    /// </summary>
    public A2UISurfaceState SetupSurface(string surfaceId, List<A2UIComponentData> components, object? dataModel = null)
    {
        SurfaceManager.CreateSurface(surfaceId, null, dataModel is not null);

        if (dataModel is not null)
        {
            var json = JsonSerializer.SerializeToElement(dataModel);
            SurfaceManager.UpdateDataModel(surfaceId, "/", json);
        }

        SurfaceManager.UpdateComponents(surfaceId, components);

        return SurfaceManager.GetSurface(surfaceId)!;
    }
}
