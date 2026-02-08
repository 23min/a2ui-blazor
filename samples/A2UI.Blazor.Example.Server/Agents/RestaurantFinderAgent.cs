using A2UI.Blazor.Server.Agents;
using A2UI.Blazor.Server.Builders;
using A2UI.Blazor.Server.Streaming;

namespace A2UI.Blazor.Example.Server.Agents;

/// <summary>
/// Example agent that demonstrates a restaurant finder UI,
/// mirroring the official A2UI quickstart example.
/// </summary>
public sealed class RestaurantFinderAgent : IA2UIAgent
{
    public string Route => "/agents/restaurant";

    public async Task HandleAsync(A2UIStreamWriter writer, CancellationToken cancellationToken)
    {
        // Create the surface
        await writer.WriteCreateSurfaceAsync("restaurant-finder", sendDataModel: true);

        // Send data model with some restaurants
        await writer.WriteUpdateDataModelAsync("restaurant-finder", "/", new
        {
            query = "",
            restaurants = new[]
            {
                new { name = "The Golden Fork", cuisine = "Italian", rating = 4.5, priceRange = "$$" },
                new { name = "Sushi Zen", cuisine = "Japanese", rating = 4.8, priceRange = "$$$" },
                new { name = "Taco Fiesta", cuisine = "Mexican", rating = 4.2, priceRange = "$" },
                new { name = "Le Petit Bistro", cuisine = "French", rating = 4.7, priceRange = "$$$" }
            }
        });

        // Build the UI components
        var components = new List<Dictionary<string, object>>();

        var root = new ComponentBuilder("root", "Column").Children("header", "search-row", "divider1", "results-list");
        var header = new ComponentBuilder("header", "Text").Text("Restaurant Finder").UsageHint("h2");
        var searchRow = new ComponentBuilder("search-row", "Row")
            .Children("search-field", "search-btn")
            .Gap("8")
            .Alignment("end");
        var searchField = new ComponentBuilder("search-field", "TextField")
            .Placeholder("Search restaurants...")
            .Label("Search")
            .Action("search");
        var searchBtn = new ComponentBuilder("search-btn", "Button")
            .Label("Search")
            .Action("search");
        var divider = new ComponentBuilder("divider1", "Divider");
        var list = new ComponentBuilder("results-list", "List")
            .Data("/restaurants")
            .Template("restaurant-card");
        var card = new ComponentBuilder("restaurant-card", "Card")
            .Title("name")
            .Children("card-body");
        var cardBody = new ComponentBuilder("card-body", "Row")
            .Children("card-cuisine", "card-rating", "card-price")
            .Distribution("spaceBetween");
        var cuisine = new ComponentBuilder("card-cuisine", "Text").Text("cuisine").UsageHint("body");
        var rating = new ComponentBuilder("card-rating", "Text").Text("rating").UsageHint("caption");
        var price = new ComponentBuilder("card-price", "Text").Text("priceRange").UsageHint("caption");

        components.Add(root.Build());
        components.Add(header.Build());
        components.Add(searchRow.Build());
        components.Add(searchField.Build());
        components.Add(searchBtn.Build());
        components.Add(divider.Build());
        components.Add(list.Build());
        components.Add(card.Build());
        components.Add(cardBody.Build());
        components.Add(cuisine.Build());
        components.Add(rating.Build());
        components.Add(price.Build());

        await writer.WriteUpdateComponentsAsync("restaurant-finder", components);

        // Keep connection open for actions
        try { await Task.Delay(Timeout.Infinite, cancellationToken); }
        catch (OperationCanceledException) { }
    }

    public async Task HandleActionAsync(A2UIStreamWriter writer, UserActionRequest action, CancellationToken cancellationToken)
    {
        if (action.Name == "search")
        {
            var query = action.Context?.GetValueOrDefault("value")?.ToString() ?? "";

            // Simulate filtering (in a real app, this would call an LLM or database)
            var allRestaurants = new[]
            {
                new { name = "The Golden Fork", cuisine = "Italian", rating = 4.5, priceRange = "$$" },
                new { name = "Sushi Zen", cuisine = "Japanese", rating = 4.8, priceRange = "$$$" },
                new { name = "Taco Fiesta", cuisine = "Mexican", rating = 4.2, priceRange = "$" },
                new { name = "Le Petit Bistro", cuisine = "French", rating = 4.7, priceRange = "$$$" }
            };

            var filtered = string.IsNullOrWhiteSpace(query)
                ? allRestaurants
                : allRestaurants.Where(r =>
                    r.name.Contains(query, StringComparison.OrdinalIgnoreCase) ||
                    r.cuisine.Contains(query, StringComparison.OrdinalIgnoreCase)).ToArray();

            await writer.WriteUpdateDataModelAsync("restaurant-finder", "/restaurants", filtered);
        }
    }
}
