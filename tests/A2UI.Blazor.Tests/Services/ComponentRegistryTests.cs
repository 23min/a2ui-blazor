using A2UI.Blazor.Components.Display;
using A2UI.Blazor.Components.Input;
using A2UI.Blazor.Components.Layout;
using A2UI.Blazor.Components.Media;
using A2UI.Blazor.Components.Visualization;
using A2UI.Blazor.Services;
using Microsoft.Extensions.Logging.Abstractions;

namespace A2UI.Blazor.Tests.Services;

public class ComponentRegistryTests
{
    private readonly ComponentRegistry _registry;

    public ComponentRegistryTests()
    {
        _registry = new ComponentRegistry(NullLogger<ComponentRegistry>.Instance);
        _registry.RegisterStandardComponents();
    }

    [Theory]
    [InlineData("Text", typeof(A2UIText))]
    [InlineData("Image", typeof(A2UIImage))]
    [InlineData("Icon", typeof(A2UIIcon))]
    [InlineData("Divider", typeof(A2UIDivider))]
    [InlineData("Row", typeof(A2UIRow))]
    [InlineData("Column", typeof(A2UIColumn))]
    [InlineData("Card", typeof(A2UICard))]
    [InlineData("List", typeof(A2UIList))]
    [InlineData("Tabs", typeof(A2UITabs))]
    [InlineData("Button", typeof(A2UIButton))]
    [InlineData("TextField", typeof(A2UITextField))]
    [InlineData("CheckBox", typeof(A2UICheckBox))]
    [InlineData("ChoicePicker", typeof(A2UIChoicePicker))]
    [InlineData("DateTimeInput", typeof(A2UIDateTimeInput))]
    [InlineData("Slider", typeof(A2UISlider))]
    [InlineData("Video", typeof(A2UIVideo))]
    [InlineData("AudioPlayer", typeof(A2UIAudioPlayer))]
    [InlineData("StateMachine", typeof(A2UIStateMachine))]
    public void Resolve_StandardComponent_ReturnsCorrectType(string name, Type expected)
    {
        Assert.Equal(expected, _registry.Resolve(name));
    }

    [Theory]
    [InlineData("text")]
    [InlineData("TEXT")]
    [InlineData("tExT")]
    [InlineData("button")]
    [InlineData("SLIDER")]
    public void Resolve_CaseInsensitive_ReturnsType(string name)
    {
        Assert.NotNull(_registry.Resolve(name));
    }

    [Fact]
    public void Resolve_UnregisteredComponent_ReturnsNull()
    {
        Assert.Null(_registry.Resolve("NonExistent"));
    }

    [Fact]
    public void Register_CustomComponent_CanBeResolved()
    {
        _registry.Register("MyWidget", typeof(A2UIText));
        Assert.Equal(typeof(A2UIText), _registry.Resolve("MyWidget"));
    }

    [Fact]
    public void Register_OverridesExisting_UsesNewType()
    {
        _registry.Register("Text", typeof(A2UIButton));
        Assert.Equal(typeof(A2UIButton), _registry.Resolve("Text"));
    }
}
