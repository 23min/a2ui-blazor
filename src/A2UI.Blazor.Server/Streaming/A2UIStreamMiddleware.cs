using System.Text.Json;
using A2UI.Blazor.Server.Agents;
using Microsoft.AspNetCore.Http;

namespace A2UI.Blazor.Server.Streaming;

/// <summary>
/// ASP.NET Core middleware that handles A2UI streaming endpoints.
/// Routes GET requests to agent.HandleAsync (stream) and
/// POST requests to agent.HandleActionAsync (user actions).
/// </summary>
public sealed class A2UIStreamMiddleware
{
    private readonly RequestDelegate _next;
    private readonly Dictionary<string, IA2UIAgent> _agents = new();

    public A2UIStreamMiddleware(RequestDelegate next, IEnumerable<IA2UIAgent> agents)
    {
        _next = next;
        foreach (var agent in agents)
        {
            _agents[agent.Route.TrimEnd('/')] = agent;
        }
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var path = context.Request.Path.Value?.TrimEnd('/');

        if (path is not null && _agents.TryGetValue(path, out var agent))
        {
            if (context.Request.Method == "GET")
            {
                await HandleStream(context, agent);
                return;
            }

            if (context.Request.Method == "POST")
            {
                await HandleAction(context, agent);
                return;
            }
        }

        await _next(context);
    }

    private static async Task HandleStream(HttpContext context, IA2UIAgent agent)
    {
        context.Response.ContentType = "text/event-stream";
        context.Response.Headers.CacheControl = "no-cache";
        context.Response.Headers.Connection = "keep-alive";

        var writer = new A2UIStreamWriter(context.Response.Body, useSse: true);
        await agent.HandleAsync(writer, context.RequestAborted);
    }

    private static async Task HandleAction(HttpContext context, IA2UIAgent agent)
    {
        var action = await JsonSerializer.DeserializeAsync<UserActionRequest>(
            context.Request.Body,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true },
            context.RequestAborted);

        if (action is null)
        {
            context.Response.StatusCode = 400;
            return;
        }

        context.Response.ContentType = "text/event-stream";
        context.Response.Headers.CacheControl = "no-cache";

        var writer = new A2UIStreamWriter(context.Response.Body, useSse: true);
        await agent.HandleActionAsync(writer, action, context.RequestAborted);
    }
}
