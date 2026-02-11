using System.Text.Json;
using A2UI.Blazor.Services;

namespace A2UI.Blazor.Tests.Services;

public class FormatStringResolverTests
{
    private readonly FormatStringResolver _resolver = new();

    private static JsonElement Parse(string json) =>
        JsonDocument.Parse(json).RootElement;

    // ── Basic interpolation ──────────────────────────────────────────

    [Fact]
    public void Resolve_SingleAbsolutePath_ReturnsInterpolatedString()
    {
        var root = Parse("""{"user":{"name":"Alice"}}""");
        var result = _resolver.Resolve("Hello, ${/user/name}!", root, null);
        Assert.Equal("Hello, Alice!", result);
    }

    [Fact]
    public void Resolve_MultipleExpressions_ReturnsAllInterpolated()
    {
        var root = Parse("""{"first":"Alice","last":"Smith"}""");
        var result = _resolver.Resolve("${/first} ${/last}", root, null);
        Assert.Equal("Alice Smith", result);
    }

    [Fact]
    public void Resolve_AdjacentExpressions_BothResolve()
    {
        var root = Parse("""{"a":"Hello","b":"World"}""");
        var result = _resolver.Resolve("${/a}${/b}", root, null);
        Assert.Equal("HelloWorld", result);
    }

    [Fact]
    public void Resolve_OnlyExpression_ReturnsResolvedValue()
    {
        var root = Parse("""{"name":"Alice"}""");
        var result = _resolver.Resolve("${/name}", root, null);
        Assert.Equal("Alice", result);
    }

    // ── Relative paths ───────────────────────────────────────────────

    [Fact]
    public void Resolve_RelativePath_ResolvesAgainstScope()
    {
        var scope = Parse("""{"name":"Bob"}""");
        var result = _resolver.Resolve("Name: ${name}", null, scope);
        Assert.Equal("Name: Bob", result);
    }

    [Fact]
    public void Resolve_MixedAbsoluteAndRelative_BothResolve()
    {
        var root = Parse("""{"greeting":"Hello"}""");
        var scope = Parse("""{"name":"Bob"}""");
        var result = _resolver.Resolve("${/greeting}, ${name}!", root, scope);
        Assert.Equal("Hello, Bob!", result);
    }

    // ── No expressions ───────────────────────────────────────────────

    [Fact]
    public void Resolve_NoExpressions_ReturnsLiteralString()
    {
        var result = _resolver.Resolve("No interpolation here", null, null);
        Assert.Equal("No interpolation here", result);
    }

    // ── Escaping ─────────────────────────────────────────────────────

    [Fact]
    public void Resolve_EscapedDollarBrace_NotInterpolated()
    {
        var result = _resolver.Resolve(@"Price: \${100}", null, null);
        Assert.Equal("Price: ${100}", result);
    }

    [Fact]
    public void Resolve_MixedEscapedAndReal_BothHandled()
    {
        var root = Parse("""{"name":"Alice"}""");
        var result = _resolver.Resolve(@"\${literal} ${/name}", root, null);
        Assert.Equal("${literal} Alice", result);
    }

    // ── Unresolvable paths ───────────────────────────────────────────

    [Fact]
    public void Resolve_UnresolvablePath_ReturnsEmptyString()
    {
        var root = Parse("""{"name":"Alice"}""");
        var result = _resolver.Resolve("Hello, ${/nonexistent}!", root, null);
        Assert.Equal("Hello, !", result);
    }

    [Fact]
    public void Resolve_NoDataModel_ExpressionsResolveToEmpty()
    {
        var result = _resolver.Resolve("Hello, ${/name}!", null, null);
        Assert.Equal("Hello, !", result);
    }

    // ── Type coercion ────────────────────────────────────────────────

    [Fact]
    public void Resolve_NullValue_CoercesToEmptyString()
    {
        var root = Parse("""{"x":null}""");
        var result = _resolver.Resolve("Value: ${/x}", root, null);
        Assert.Equal("Value: ", result);
    }

    [Fact]
    public void Resolve_NumberValue_CoercesToString()
    {
        var root = Parse("""{"count":42}""");
        var result = _resolver.Resolve("Count: ${/count}", root, null);
        Assert.Equal("Count: 42", result);
    }

    [Fact]
    public void Resolve_BooleanTrue_CoercesToLowercaseString()
    {
        var root = Parse("""{"active":true}""");
        var result = _resolver.Resolve("Active: ${/active}", root, null);
        Assert.Equal("Active: true", result);
    }

    [Fact]
    public void Resolve_BooleanFalse_CoercesToLowercaseString()
    {
        var root = Parse("""{"active":false}""");
        var result = _resolver.Resolve("Active: ${/active}", root, null);
        Assert.Equal("Active: false", result);
    }

    [Fact]
    public void Resolve_ObjectValue_CoercesToJson()
    {
        var root = Parse("""{"nested":{"a":1}}""");
        var result = _resolver.Resolve("Data: ${/nested}", root, null);
        Assert.Equal("""Data: {"a":1}""", result);
    }

    [Fact]
    public void Resolve_ArrayValue_CoercesToJson()
    {
        var root = Parse("""{"items":[1,2,3]}""");
        var result = _resolver.Resolve("Items: ${/items}", root, null);
        Assert.Equal("Items: [1,2,3]", result);
    }

    // ── Edge cases ───────────────────────────────────────────────────

    [Fact]
    public void Resolve_NullTemplate_ReturnsNull()
    {
        Assert.Null(_resolver.Resolve(null, null, null));
    }

    [Fact]
    public void Resolve_EmptyTemplate_ReturnsEmpty()
    {
        Assert.Equal(string.Empty, _resolver.Resolve("", null, null));
    }

    [Fact]
    public void Resolve_DeepNestedPath_Resolves()
    {
        var root = Parse("""{"a":{"b":{"c":"deep"}}}""");
        var result = _resolver.Resolve("${/a/b/c}", root, null);
        Assert.Equal("deep", result);
    }
}
