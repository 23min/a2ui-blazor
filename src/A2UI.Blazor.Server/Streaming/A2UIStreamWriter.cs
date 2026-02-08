using System.Text.Json;
using System.Text.Json.Serialization;
using A2UI.Blazor.Server.Builders;

namespace A2UI.Blazor.Server.Streaming;

/// <summary>
/// Writes A2UI messages as JSONL to an output stream (typically an HTTP response).
/// Supports both raw JSONL and SSE (Server-Sent Events) format.
/// </summary>
public sealed class A2UIStreamWriter
{
    private readonly StreamWriter _writer;
    private readonly bool _useSse;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public A2UIStreamWriter(Stream stream, bool useSse = true)
    {
        _writer = new StreamWriter(stream);
        _useSse = useSse;
    }

    public async Task WriteCreateSurfaceAsync(
        string surfaceId,
        string? catalogId = null,
        bool sendDataModel = false)
    {
        var message = new Dictionary<string, object>
        {
            ["type"] = "createSurface",
            ["surfaceId"] = surfaceId
        };
        if (catalogId is not null) message["catalogId"] = catalogId;
        if (sendDataModel) message["sendDataModel"] = true;
        await WriteMessageAsync(message);
    }

    public async Task WriteUpdateComponentsAsync(
        string surfaceId,
        List<Dictionary<string, object>> components)
    {
        var message = new Dictionary<string, object>
        {
            ["type"] = "updateComponents",
            ["surfaceId"] = surfaceId,
            ["components"] = components
        };
        await WriteMessageAsync(message);
    }

    public async Task WriteUpdateDataModelAsync(
        string surfaceId,
        string? path = null,
        object? value = null)
    {
        var message = new Dictionary<string, object>
        {
            ["type"] = "updateDataModel",
            ["surfaceId"] = surfaceId
        };
        if (path is not null) message["path"] = path;
        if (value is not null) message["value"] = value;
        await WriteMessageAsync(message);
    }

    public async Task WriteDeleteSurfaceAsync(string surfaceId)
    {
        var message = new Dictionary<string, object>
        {
            ["type"] = "deleteSurface",
            ["surfaceId"] = surfaceId
        };
        await WriteMessageAsync(message);
    }

    /// <summary>
    /// Write a complete surface (createSurface + updateComponents) using a SurfaceBuilder.
    /// </summary>
    public async Task WriteSurfaceAsync(SurfaceBuilder builder)
    {
        await WriteCreateSurfaceAsync(
            builder.SurfaceId,
            builder.CatalogIdValue,
            builder.SendDataModelValue);

        var components = builder.BuildComponents();
        if (components.Count > 0)
        {
            await WriteUpdateComponentsAsync(builder.SurfaceId, components);
        }
    }

    private async Task WriteMessageAsync(object message)
    {
        var json = JsonSerializer.Serialize(message, JsonOptions);

        if (_useSse)
        {
            await _writer.WriteLineAsync($"data: {json}");
            await _writer.WriteLineAsync(); // blank line per SSE spec
        }
        else
        {
            await _writer.WriteLineAsync(json);
        }

        await _writer.FlushAsync();
    }
}
