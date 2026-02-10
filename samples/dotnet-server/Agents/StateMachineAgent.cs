using A2UI.Blazor.Server.Agents;
using A2UI.Blazor.Server.Builders;
using A2UI.Blazor.Server.Streaming;

namespace dotnet_server.Agents;

public sealed class StateMachineAgent : IA2UIAgent
{
    public string Route => "/agents/state-machine";

    private static readonly List<Dictionary<string, object>> PipelineStates =
    [
        new() { ["id"] = "received", ["label"] = "Received" },
        new() { ["id"] = "validating", ["label"] = "Validating" },
        new() { ["id"] = "processing", ["label"] = "Processing" },
        new() { ["id"] = "billing", ["label"] = "Billing" },
        new() { ["id"] = "shipping", ["label"] = "Shipping" },
        new() { ["id"] = "delivered", ["label"] = "Delivered" }
    ];

    public async Task HandleAsync(A2UIStreamWriter writer, CancellationToken cancellationToken)
    {
        // Create surface with data model enabled
        await writer.WriteCreateSurfaceAsync("state-machine", sendDataModel: true);

        // Initialize data model - all states pending
        var initialStates = PipelineStates.Select(s => new Dictionary<string, object>(s) { ["status"] = "pending" }).ToList();
        await writer.WriteUpdateDataModelAsync("state-machine", "/", new Dictionary<string, object>
        {
            ["pipeline"] = new Dictionary<string, object>
            {
                ["title"] = "Order Processing Pipeline",
                ["states"] = initialStates,
                ["statusMessage"] = "Waiting to start..."
            }
        });

        // Define component tree
        var components = new List<Dictionary<string, object>>
        {
            new ComponentBuilder("root", "Column")
                .Children("header", "pipeline", "status-text")
                .Gap("12")
                .Build(),

            new ComponentBuilder("header", "Text")
                .Text("Live State Machine")
                .Variant("h2")
                .Build(),

            new ComponentBuilder("pipeline", "StateMachine")
                .Set("data", "/pipeline")
                .Set("title", "/pipeline/title")
                .Build(),

            new ComponentBuilder("status-text", "Text")
                .Text("/pipeline/statusMessage")
                .Variant("caption")
                .Build()
        };

        await writer.WriteUpdateComponentsAsync("state-machine", components);

        // Auto-advance through states in a loop
        try
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                // Advance through each step
                for (int step = 0; step < PipelineStates.Count; step++)
                {
                    await Task.Delay(2000, cancellationToken);

                    var states = new List<Dictionary<string, object>>();
                    for (int i = 0; i < PipelineStates.Count; i++)
                    {
                        var state = new Dictionary<string, object>(PipelineStates[i]);
                        if (i < step)
                            state["status"] = "completed";
                        else if (i == step)
                            state["status"] = "active";
                        else
                            state["status"] = "pending";
                        states.Add(state);
                    }

                    var statusMsg = $"Step {step + 1}/{PipelineStates.Count}: {PipelineStates[step]["label"]}";
                    await writer.WriteUpdateDataModelAsync("state-machine", "/pipeline", new Dictionary<string, object>
                    {
                        ["title"] = "Order Processing Pipeline",
                        ["states"] = states,
                        ["statusMessage"] = statusMsg
                    });
                }

                // All completed
                await Task.Delay(2000, cancellationToken);
                var completedStates = PipelineStates.Select(s => new Dictionary<string, object>(s) { ["status"] = "completed" }).ToList();
                await writer.WriteUpdateDataModelAsync("state-machine", "/pipeline", new Dictionary<string, object>
                {
                    ["title"] = "Order Processing Pipeline",
                    ["states"] = completedStates,
                    ["statusMessage"] = "All steps completed! Restarting in 3s..."
                });

                await Task.Delay(3000, cancellationToken);

                // Reset
                var resetStates = PipelineStates.Select(s => new Dictionary<string, object>(s) { ["status"] = "pending" }).ToList();
                await writer.WriteUpdateDataModelAsync("state-machine", "/pipeline", new Dictionary<string, object>
                {
                    ["title"] = "Order Processing Pipeline",
                    ["states"] = resetStates,
                    ["statusMessage"] = "Pipeline reset. Starting..."
                });
            }
        }
        catch (OperationCanceledException)
        {
            // Client disconnected, exit gracefully
        }
    }

    public Task HandleActionAsync(A2UIStreamWriter writer, UserActionRequest action, CancellationToken cancellationToken)
    {
        // State machine has no user actions
        return Task.CompletedTask;
    }
}
