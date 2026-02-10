using System.Text.Json;
using A2UI.Blazor.Diagnostics;
using A2UI.Blazor.Protocol;
using Microsoft.Extensions.Logging;

namespace A2UI.Blazor.Services;

/// <summary>
/// Reads an HTTP response stream line-by-line, deserializing each JSONL line
/// into an A2UIMessage and yielding it as an async enumerable.
/// </summary>
public sealed class JsonlStreamReader
{
    private readonly ILogger<JsonlStreamReader> _logger;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public JsonlStreamReader(ILogger<JsonlStreamReader> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Read messages from a stream (typically an HTTP response body).
    /// Each non-empty line is parsed as a JSON object.
    /// </summary>
    public async IAsyncEnumerable<A2UIMessage> ReadMessagesAsync(
        Stream stream,
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        using var reader = new StreamReader(stream);

        while (!cancellationToken.IsCancellationRequested)
        {
            var line = await reader.ReadLineAsync(cancellationToken);

            if (line is null)
                yield break; // end of stream

            line = line.Trim();

            if (line.Length == 0 || line.StartsWith(':'))
                continue; // skip empty lines and SSE comments

            // SSE format: strip "data: " prefix if present
            if (line.StartsWith("data: ", StringComparison.Ordinal))
                line = line["data: ".Length..];
            else if (line.StartsWith("data:", StringComparison.Ordinal))
                line = line["data:".Length..];

            if (line.Length == 0 || line == "[DONE]")
                continue;

            A2UIMessage? message;
            try
            {
                message = JsonSerializer.Deserialize<A2UIMessage>(line, JsonOptions);
            }
            catch (JsonException ex)
            {
                _logger.LogWarning(LogEvents.ParseError, ex, "Failed to parse JSONL line: {Line}", line.Length > 100 ? line[..100] + "..." : line);
                continue; // skip malformed lines
            }

            if (message is not null)
                yield return message;
        }
    }
}
