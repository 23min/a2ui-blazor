using System.Text.Json;
using System.Text.RegularExpressions;

namespace A2UI.Blazor.Services;

/// <summary>
/// Resolves A2UI v0.9 formatString interpolation expressions.
/// Processes ${expression} placeholders in template strings,
/// resolving data model paths and performing type coercion.
/// </summary>
public sealed partial class FormatStringResolver
{
    private readonly DataBindingResolver _resolver = new();

    /// <summary>
    /// Matches ${...} but not \${...} (escaped).
    /// </summary>
    [GeneratedRegex(@"(?<!\\)\$\{([^}]*)\}")]
    private static partial Regex ExpressionPattern();

    /// <summary>
    /// Resolve a formatString template, substituting ${expression} placeholders.
    /// </summary>
    /// <param name="template">The template string with ${...} expressions</param>
    /// <param name="dataModelRoot">Root of the surface data model (for absolute paths)</param>
    /// <param name="scopeElement">Scope element (for relative paths in list iteration)</param>
    /// <returns>The interpolated string, or null if template is null</returns>
    public string? Resolve(string? template, JsonElement? dataModelRoot, JsonElement? scopeElement)
    {
        if (template is null) return null;
        if (template.Length == 0) return string.Empty;

        var result = ExpressionPattern().Replace(template, match =>
        {
            var expression = match.Groups[1].Value;
            var resolved = ResolveExpression(expression, dataModelRoot, scopeElement);
            return CoerceToString(resolved);
        });

        // Unescape \${ → ${
        result = result.Replace("\\${", "${");

        return result;
    }

    private JsonElement? ResolveExpression(string expression, JsonElement? dataModelRoot, JsonElement? scopeElement)
    {
        if (string.IsNullOrEmpty(expression)) return null;

        // Absolute path: starts with /
        if (expression.StartsWith('/') && dataModelRoot.HasValue)
            return _resolver.Resolve(dataModelRoot.Value, expression);

        // Relative path: resolve against scope
        if (scopeElement.HasValue)
            return _resolver.ResolveRelative(scopeElement.Value, expression);

        return null;
    }

    private static string CoerceToString(JsonElement? element)
    {
        if (!element.HasValue) return string.Empty;

        return element.Value.ValueKind switch
        {
            JsonValueKind.Null or JsonValueKind.Undefined => string.Empty,
            JsonValueKind.String => element.Value.GetString() ?? string.Empty,
            JsonValueKind.True => "true",
            JsonValueKind.False => "false",
            _ => element.Value.GetRawText(), // Number, Object, Array
        };
    }
}
