using System.Text.Json;
using A2UI.Blazor.Services;

namespace A2UI.Blazor.Tests.Services;

public class DataBindingResolverTests
{
    private readonly DataBindingResolver _resolver = new();

    private static JsonElement Parse(string json) =>
        JsonDocument.Parse(json).RootElement;

    // ── Resolve absolute paths ──────────────────────────────────────

    [Fact]
    public void Resolve_SimplePath_ReturnsValue()
    {
        var root = Parse("""{"name":"Alice"}""");
        var result = _resolver.Resolve(root, "/name");
        Assert.Equal("Alice", result?.GetString());
    }

    [Fact]
    public void Resolve_NestedPath_ReturnsValue()
    {
        var root = Parse("""{"user":{"profile":{"name":"Bob"}}}""");
        var result = _resolver.Resolve(root, "/user/profile/name");
        Assert.Equal("Bob", result?.GetString());
    }

    [Fact]
    public void Resolve_ArrayIndex_ReturnsElement()
    {
        var root = Parse("""{"items":["a","b","c"]}""");
        var result = _resolver.Resolve(root, "/items/1");
        Assert.Equal("b", result?.GetString());
    }

    [Fact]
    public void Resolve_NestedArrayObject_ReturnsValue()
    {
        var root = Parse("""{"items":[{"name":"first"},{"name":"second"}]}""");
        var result = _resolver.Resolve(root, "/items/0/name");
        Assert.Equal("first", result?.GetString());
    }

    [Fact]
    public void Resolve_Rfc6901Escape_TildeZero_ReturnsTilde()
    {
        // ~0 should unescape to ~
        var root = Parse("""{"a~b":"found"}""");
        var result = _resolver.Resolve(root, "/a~0b");
        Assert.Equal("found", result?.GetString());
    }

    [Fact]
    public void Resolve_Rfc6901Escape_TildeOne_ReturnsSlash()
    {
        // ~1 should unescape to /
        var root = Parse("""{"a/b":"found"}""");
        var result = _resolver.Resolve(root, "/a~1b");
        Assert.Equal("found", result?.GetString());
    }

    // ── Edge cases ──────────────────────────────────────────────────

    [Fact]
    public void Resolve_EmptyPath_ReturnsRoot()
    {
        var root = Parse("""{"x":1}""");
        var result = _resolver.Resolve(root, "");
        Assert.Equal(JsonValueKind.Object, result?.ValueKind);
    }

    [Fact]
    public void Resolve_NullPath_ReturnsRoot()
    {
        var root = Parse("""{"x":1}""");
        var result = _resolver.Resolve(root, null!);
        Assert.Equal(JsonValueKind.Object, result?.ValueKind);
    }

    [Fact]
    public void Resolve_NonExistentProperty_ReturnsNull()
    {
        var root = Parse("""{"name":"Alice"}""");
        var result = _resolver.Resolve(root, "/missing");
        Assert.Null(result);
    }

    [Fact]
    public void Resolve_OutOfBoundsIndex_ReturnsNull()
    {
        var root = Parse("""{"items":["a"]}""");
        var result = _resolver.Resolve(root, "/items/5");
        Assert.Null(result);
    }

    [Fact]
    public void Resolve_NegativeIndex_ReturnsNull()
    {
        var root = Parse("""{"items":["a"]}""");
        var result = _resolver.Resolve(root, "/items/-1");
        Assert.Null(result);
    }

    [Fact]
    public void Resolve_ThroughScalar_ReturnsNull()
    {
        var root = Parse("""{"name":"Alice"}""");
        var result = _resolver.Resolve(root, "/name/sub");
        Assert.Null(result);
    }

    [Fact]
    public void Resolve_NonNumericArrayIndex_ReturnsNull()
    {
        var root = Parse("""{"items":["a","b"]}""");
        var result = _resolver.Resolve(root, "/items/notanumber");
        Assert.Null(result);
    }

    // ── Relative resolution ─────────────────────────────────────────

    [Fact]
    public void ResolveRelative_SimpleProperty_ReturnsValue()
    {
        var scope = Parse("""{"name":"Alice","age":30}""");
        var result = _resolver.ResolveRelative(scope, "name");
        Assert.Equal("Alice", result?.GetString());
    }

    // ── SetValueAtPath ──────────────────────────────────────────────

    [Fact]
    public void SetValueAtPath_ExistingProperty_ReplacesValue()
    {
        var root = Parse("""{"name":"old"}""");
        var newVal = JsonSerializer.SerializeToElement("new");
        var result = DataBindingResolver.SetValueAtPath(root, "/name", newVal);
        Assert.Equal("new", result.GetProperty("name").GetString());
    }

    [Fact]
    public void SetValueAtPath_NewProperty_CreatesIt()
    {
        var root = Parse("""{"a":1}""");
        var newVal = Parse("42");
        var result = DataBindingResolver.SetValueAtPath(root, "/b", newVal);
        Assert.Equal(1, result.GetProperty("a").GetInt32());
        Assert.Equal(42, result.GetProperty("b").GetInt32());
    }

    [Fact]
    public void SetValueAtPath_DeepPath_CreatesIntermediateObjects()
    {
        var root = Parse("""{}""");
        var newVal = JsonSerializer.SerializeToElement("deep");
        var result = DataBindingResolver.SetValueAtPath(root, "/a/b/c", newVal);
        Assert.Equal("deep", result.GetProperty("a").GetProperty("b").GetProperty("c").GetString());
    }

    [Fact]
    public void SetValueAtPath_EmptySegments_ReplacesRoot()
    {
        var root = Parse("""{"old":true}""");
        var newVal = Parse("""{"new":true}""");
        var result = DataBindingResolver.SetValueAtPath(root, "/", newVal);
        Assert.True(result.GetProperty("new").GetBoolean());
    }

    [Fact]
    public void SetValueAtPath_NullRoot_CreatesFromScratch()
    {
        var newVal = JsonSerializer.SerializeToElement("hello");
        var result = DataBindingResolver.SetValueAtPath(null, "/greeting", newVal);
        Assert.Equal("hello", result.GetProperty("greeting").GetString());
    }
}
