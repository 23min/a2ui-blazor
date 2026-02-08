using System.Text.Json;

namespace A2UI.Blazor.Services;

/// <summary>
/// Resolves JSON Pointer (RFC 6901) paths against a JSON data model.
/// Supports absolute paths like "/user/profile/name" and relative paths
/// like "firstName" (resolved within a provided scope element).
/// </summary>
public sealed class DataBindingResolver
{
    /// <summary>
    /// Resolve a JSON Pointer path against a root element.
    /// Returns null if the path cannot be resolved.
    /// </summary>
    public JsonElement? Resolve(JsonElement root, string path)
    {
        if (string.IsNullOrEmpty(path))
            return root;

        // Absolute path starts with /
        if (!path.StartsWith('/'))
            return ResolveRelative(root, path);

        var segments = path.Split('/', StringSplitOptions.RemoveEmptyEntries);
        return WalkPath(root, segments);
    }

    /// <summary>
    /// Resolve a relative path (no leading /) against a scope element.
    /// </summary>
    public JsonElement? ResolveRelative(JsonElement scope, string relativePath)
    {
        var segments = relativePath.Split('/', StringSplitOptions.RemoveEmptyEntries);
        return WalkPath(scope, segments);
    }

    private static JsonElement? WalkPath(JsonElement current, string[] segments)
    {
        foreach (var rawSegment in segments)
        {
            // RFC 6901: unescape ~1 → / and ~0 → ~
            var segment = rawSegment.Replace("~1", "/").Replace("~0", "~");

            if (current.ValueKind == JsonValueKind.Object)
            {
                if (current.TryGetProperty(segment, out var prop))
                    current = prop;
                else
                    return null;
            }
            else if (current.ValueKind == JsonValueKind.Array)
            {
                if (int.TryParse(segment, out var index))
                {
                    var len = current.GetArrayLength();
                    if (index >= 0 && index < len)
                        current = current[index];
                    else
                        return null;
                }
                else
                {
                    return null;
                }
            }
            else
            {
                return null;
            }
        }

        return current;
    }

    /// <summary>
    /// Set a value at a JSON Pointer path, returning a new root element.
    /// Creates intermediate objects as needed.
    /// </summary>
    public static JsonElement SetValueAtPath(JsonElement? root, string path, JsonElement value)
    {
        var segments = path.Split('/', StringSplitOptions.RemoveEmptyEntries);

        if (segments.Length == 0)
            return value;

        using var stream = new MemoryStream();
        using var writer = new Utf8JsonWriter(stream);

        WriteWithUpdate(writer, root ?? default, segments, 0, value);

        writer.Flush();
        stream.Position = 0;
        using var doc = JsonDocument.Parse(stream);
        return doc.RootElement.Clone();
    }

    private static void WriteWithUpdate(
        Utf8JsonWriter writer,
        JsonElement current,
        string[] segments,
        int depth,
        JsonElement value)
    {
        var segment = segments[depth].Replace("~1", "/").Replace("~0", "~");
        var isLast = depth == segments.Length - 1;

        if (current.ValueKind == JsonValueKind.Object)
        {
            writer.WriteStartObject();
            var found = false;
            foreach (var prop in current.EnumerateObject())
            {
                if (prop.Name == segment)
                {
                    found = true;
                    writer.WritePropertyName(prop.Name);
                    if (isLast)
                        value.WriteTo(writer);
                    else
                        WriteWithUpdate(writer, prop.Value, segments, depth + 1, value);
                }
                else
                {
                    prop.WriteTo(writer);
                }
            }
            if (!found)
            {
                writer.WritePropertyName(segment);
                if (isLast)
                    value.WriteTo(writer);
                else
                    WriteWithUpdate(writer, default, segments, depth + 1, value);
            }
            writer.WriteEndObject();
        }
        else
        {
            // Current isn't an object — create one
            writer.WriteStartObject();
            writer.WritePropertyName(segment);
            if (isLast)
                value.WriteTo(writer);
            else
                WriteWithUpdate(writer, default, segments, depth + 1, value);
            writer.WriteEndObject();
        }
    }
}
