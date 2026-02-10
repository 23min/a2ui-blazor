using A2UI.Blazor.Server.Agents;
using A2UI.Blazor.Server.Builders;
using A2UI.Blazor.Server.Streaming;

namespace dotnet_server.Agents;

public sealed class ComponentGalleryAgent : IA2UIAgent
{
    public string Route => "/agents/gallery";

    public async Task HandleAsync(A2UIStreamWriter writer, CancellationToken cancellationToken)
    {
        await writer.WriteCreateSurfaceAsync("gallery");

        var components = new List<Dictionary<string, object>>();

        components.Add(new ComponentBuilder("root", "Column")
            .Children("title", "subtitle", "divider-top", "display-section", "divider1", "input-section", "divider2", "layout-section")
            .Gap("16").Build());

        components.Add(new ComponentBuilder("title", "Text").Text("A2UI Component Gallery").Variant("h1").Build());
        components.Add(new ComponentBuilder("subtitle", "Text").Text("All standard A2UI components rendered with Blazor").Variant("caption").Build());
        components.Add(new ComponentBuilder("divider-top", "Divider").Build());

        // Display section
        components.Add(new ComponentBuilder("display-section", "Card").Title("Display Components").Children("display-col").Build());
        components.Add(new ComponentBuilder("display-col", "Column").Children("text-h1", "text-h2", "text-body", "text-caption", "icon1", "divider-sample").Gap("8").Build());
        components.Add(new ComponentBuilder("text-h1", "Text").Text("Heading 1").Variant("h1").Build());
        components.Add(new ComponentBuilder("text-h2", "Text").Text("Heading 2").Variant("h2").Build());
        components.Add(new ComponentBuilder("text-body", "Text").Text("This is body text demonstrating the Text component.").Variant("body").Build());
        components.Add(new ComponentBuilder("text-caption", "Text").Text("This is a caption").Variant("caption").Build());
        components.Add(new ComponentBuilder("icon1", "Icon").Set("icon", "★").Set("size", "32").Build());
        components.Add(new ComponentBuilder("divider-sample", "Divider").Build());

        // Input section
        components.Add(new ComponentBuilder("input-section", "Card").Title("Input Components").Children("input-col").Build());
        components.Add(new ComponentBuilder("input-col", "Column").Children("btn-row", "textfield1", "checkbox1", "choice1", "datetime1", "slider1").Gap("12").Build());
        components.Add(new ComponentBuilder("btn-row", "Row").Children("btn-primary", "btn-secondary", "btn-disabled").Gap("8").Build());
        components.Add(new ComponentBuilder("btn-primary", "Button").Label("Primary").Variant("primary").Action("click").Build());
        components.Add(new ComponentBuilder("btn-secondary", "Button").Label("Secondary").Variant("secondary").Action("click").Build());
        components.Add(new ComponentBuilder("btn-disabled", "Button").Label("Disabled").Disabled().Build());
        components.Add(new ComponentBuilder("textfield1", "TextField").Label("Text Field").Placeholder("Enter text...").Action("input").Build());
        components.Add(new ComponentBuilder("checkbox1", "CheckBox").Label("Check me").Action("toggle").Build());
        components.Add(new ComponentBuilder("choice1", "ChoicePicker").Label("Pick a fruit").Options("Apple", "Banana", "Cherry", "Date").Action("select").Build());
        components.Add(new ComponentBuilder("datetime1", "DateTimeInput").Label("Select a date").Set("inputType", "date").Action("dateChange").Build());
        components.Add(new ComponentBuilder("slider1", "Slider").Label("Volume").Min(0).Max(100).Step(1).Value(50).Action("slide").Build());

        // Layout section
        components.Add(new ComponentBuilder("layout-section", "Card").Title("Layout Components").Children("layout-col").Build());
        components.Add(new ComponentBuilder("layout-col", "Column").Children("row-demo", "tabs-demo").Gap("12").Build());
        components.Add(new ComponentBuilder("row-demo", "Row").Children("row-item1", "row-item2", "row-item3").Justify("spaceEvenly").Build());
        components.Add(new ComponentBuilder("row-item1", "Text").Text("Row Item 1").Build());
        components.Add(new ComponentBuilder("row-item2", "Text").Text("Row Item 2").Build());
        components.Add(new ComponentBuilder("row-item3", "Text").Text("Row Item 3").Build());
        components.Add(new ComponentBuilder("tabs-demo", "Tabs").Tabs(("Tab One", "tab1-content"), ("Tab Two", "tab2-content")).Build());
        components.Add(new ComponentBuilder("tab1-content", "Text").Text("Content of the first tab.").Build());
        components.Add(new ComponentBuilder("tab2-content", "Text").Text("Content of the second tab.").Build());

        await writer.WriteUpdateComponentsAsync("gallery", components);

        try { await Task.Delay(Timeout.Infinite, cancellationToken); }
        catch (OperationCanceledException) { }
    }

    public Task HandleActionAsync(A2UIStreamWriter writer, UserActionRequest action, CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }
}
