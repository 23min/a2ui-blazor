using A2UI.Blazor.Server.Agents;
using A2UI.Blazor.Server.Builders;
using A2UI.Blazor.Server.Streaming;

namespace dotnet_server.Agents;

public sealed class ContactLookupAgent : IA2UIAgent
{
    public string Route => "/agents/contacts";

    private static readonly object[] AllContacts =
    [
        new { name = "Alice Johnson", email = "alice@example.com", phone = "+1-555-0101", department = "Engineering" },
        new { name = "Bob Smith", email = "bob@example.com", phone = "+1-555-0102", department = "Marketing" },
        new { name = "Carol Williams", email = "carol@example.com", phone = "+1-555-0103", department = "Engineering" },
        new { name = "David Brown", email = "david@example.com", phone = "+1-555-0104", department = "Sales" },
        new { name = "Eve Davis", email = "eve@example.com", phone = "+1-555-0105", department = "Engineering" }
    ];

    public async Task HandleAsync(A2UIStreamWriter writer, CancellationToken cancellationToken)
    {
        await writer.WriteCreateSurfaceAsync("contacts", sendDataModel: true);

        await writer.WriteUpdateDataModelAsync("contacts", "/", new { contacts = AllContacts });

        var components = new List<Dictionary<string, object>>();

        components.Add(new ComponentBuilder("root", "Column").Children("header", "search-row", "divider", "contact-list").Gap("12").Build());
        components.Add(new ComponentBuilder("header", "Text").Text("Contact Directory").UsageHint("h2").Build());
        components.Add(new ComponentBuilder("search-row", "Row").Children("search-input", "search-btn").Gap("8").Alignment("end").Build());
        components.Add(new ComponentBuilder("search-input", "TextField").Placeholder("Search contacts...").Label("Search").Action("search").Build());
        components.Add(new ComponentBuilder("search-btn", "Button").Label("Search").Action("search").Build());
        components.Add(new ComponentBuilder("divider", "Divider").Build());
        components.Add(new ComponentBuilder("contact-list", "List").Data("/contacts").Template("contact-row").Build());
        components.Add(new ComponentBuilder("contact-row", "Row").Children("contact-name", "contact-email", "contact-dept").Distribution("spaceBetween").Build());
        components.Add(new ComponentBuilder("contact-name", "Text").Text("name").UsageHint("body").Build());
        components.Add(new ComponentBuilder("contact-email", "Text").Text("email").UsageHint("caption").Build());
        components.Add(new ComponentBuilder("contact-dept", "Text").Text("department").UsageHint("caption").Build());

        await writer.WriteUpdateComponentsAsync("contacts", components);

        try { await Task.Delay(Timeout.Infinite, cancellationToken); }
        catch (OperationCanceledException) { }
    }

    public async Task HandleActionAsync(A2UIStreamWriter writer, UserActionRequest action, CancellationToken cancellationToken)
    {
        if (action.Name == "search")
        {
            var query = action.Context?.GetValueOrDefault("value")?.ToString() ?? "";

            var filtered = string.IsNullOrWhiteSpace(query)
                ? AllContacts
                : AllContacts.Where(c =>
                {
                    var type = c.GetType();
                    var name = type.GetProperty("name")?.GetValue(c)?.ToString() ?? "";
                    var dept = type.GetProperty("department")?.GetValue(c)?.ToString() ?? "";
                    return name.Contains(query, StringComparison.OrdinalIgnoreCase) ||
                           dept.Contains(query, StringComparison.OrdinalIgnoreCase);
                }).ToArray();

            await writer.WriteUpdateDataModelAsync("contacts", "/contacts", filtered);
        }
    }
}
