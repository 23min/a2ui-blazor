using A2UI.Blazor.Server.Agents;
using A2UI.Blazor.Server.Builders;
using A2UI.Blazor.Server.Streaming;

namespace dotnet_server.Agents;

public sealed class ErrorDemoAgent : IA2UIAgent
{
    public string Route => "/agents/error-demo";

    private int _errorCount;

    public async Task HandleAsync(A2UIStreamWriter writer, CancellationToken cancellationToken)
    {
        await writer.WriteCreateSurfaceAsync("error-demo", sendDataModel: true);

        await writer.WriteUpdateDataModelAsync("error-demo", "/", new
        {
            lastErrorMessage = "No errors reported yet.",
            errorCount = 0
        });

        var components = new List<Dictionary<string, object>>();

        // Root layout
        components.Add(new ComponentBuilder("root", "Column")
            .Children("header", "description", "divider1", "unknown-section", "divider2", "report-section")
            .Gap("16").Build());

        components.Add(new ComponentBuilder("header", "Text").Text("Error Handling Demo").Variant("h2").Build());
        components.Add(new ComponentBuilder("description", "Text")
            .Text("This demo shows how A2UI handles errors gracefully — unknown components render fallback UI, and errors can be reported back to the server.")
            .Variant("body").Build());
        components.Add(new ComponentBuilder("divider1", "Divider").Build());

        // Unknown component section
        components.Add(new ComponentBuilder("unknown-section", "Card").Title("Unknown Component").Children("unknown-col").Build());
        components.Add(new ComponentBuilder("unknown-col", "Column")
            .Children("unknown-desc", "unknown-component")
            .Gap("8").Build());
        components.Add(new ComponentBuilder("unknown-desc", "Text")
            .Text("The component below uses type 'FancyWidget' which doesn't exist in the standard catalog. The renderer shows a graceful fallback:")
            .Variant("body").Build());
        // Intentionally unknown — will render as orange dashed "Unknown component: FancyWidget"
        components.Add(new ComponentBuilder("unknown-component", "FancyWidget").Build());

        components.Add(new ComponentBuilder("divider2", "Divider").Build());

        // Error reporting section
        components.Add(new ComponentBuilder("report-section", "Card").Title("Error Reporting").Children("report-col").Build());
        components.Add(new ComponentBuilder("report-col", "Column")
            .Children("report-desc", "report-btn", "error-status")
            .Gap("8").Build());
        components.Add(new ComponentBuilder("report-desc", "Text")
            .Text("Click the button to send a VALIDATION_FAILED error report to the server via the v0.9 error envelope. The server will acknowledge receipt.")
            .Variant("body").Build());
        components.Add(new ComponentBuilder("report-btn", "Button")
            .Label("Report Error to Server")
            .Action("report-error").Build());
        components.Add(new ComponentBuilder("error-status", "Text")
            .Text("/lastErrorMessage")
            .Variant("caption").Build());

        await writer.WriteUpdateComponentsAsync("error-demo", components);

        try { await Task.Delay(Timeout.Infinite, cancellationToken); }
        catch (OperationCanceledException) { }
    }

    public Task HandleActionAsync(A2UIStreamWriter writer, UserActionRequest action, CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }

    public async Task HandleErrorAsync(A2UIStreamWriter writer, ClientErrorRequest error, CancellationToken cancellationToken)
    {
        _errorCount++;
        var message = $"Server received error #{_errorCount}: [{error.Code}] {error.Message}";
        if (error.Path is not null)
            message += $" (path: {error.Path})";

        await writer.WriteUpdateDataModelAsync("error-demo", "/", new
        {
            lastErrorMessage = message,
            errorCount = _errorCount
        });
    }
}
