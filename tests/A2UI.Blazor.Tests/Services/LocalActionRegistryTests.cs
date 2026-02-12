using System.Text.Json;
using A2UI.Blazor.Services;

namespace A2UI.Blazor.Tests.Services;

public class LocalActionRegistryTests
{
    private readonly LocalActionRegistry _registry = new();

    [Fact]
    public void Register_AndExecute_ReturnsResult()
    {
        _registry.Register("greet", args =>
        {
            var name = args?["name"].GetString();
            return $"Hello, {name}!";
        });

        var result = _registry.Execute("greet", new Dictionary<string, JsonElement>
        {
            ["name"] = JsonSerializer.SerializeToElement("Alice")
        });

        Assert.Equal("Hello, Alice!", result);
    }

    [Fact]
    public void Register_VoidAction_ExecutesSuccessfully()
    {
        var called = false;
        _registry.Register("doSomething", _ => { called = true; });

        _registry.Execute("doSomething", null);

        Assert.True(called);
    }

    [Fact]
    public void Execute_UnregisteredAction_ReturnsNull()
    {
        var result = _registry.Execute("nonexistent", null);

        Assert.Null(result);
    }

    [Fact]
    public void IsRegistered_ReturnsTrueForRegistered()
    {
        _registry.Register("myAction", _ => null);

        Assert.True(_registry.IsRegistered("myAction"));
    }

    [Fact]
    public void IsRegistered_ReturnsFalseForUnregistered()
    {
        Assert.False(_registry.IsRegistered("nonexistent"));
    }

    [Fact]
    public void Execute_WithNullArgs_PassesNullToHandler()
    {
        Dictionary<string, JsonElement>? receivedArgs = null;
        _registry.Register("test", args =>
        {
            receivedArgs = args;
            return null;
        });

        _registry.Execute("test", null);

        Assert.Null(receivedArgs);
    }

    [Fact]
    public void Register_OverwritesPreviousHandler()
    {
        _registry.Register("action", _ => "first");
        _registry.Register("action", _ => "second");

        var result = _registry.Execute("action", null);

        Assert.Equal("second", result);
    }
}
