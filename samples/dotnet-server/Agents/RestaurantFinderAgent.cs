using A2UI.Blazor.Server.Agents;
using A2UI.Blazor.Server.Builders;
using A2UI.Blazor.Server.Streaming;

namespace dotnet_server.Agents;

public sealed class RestaurantFinderAgent : IA2UIAgent
{
    public string Route => "/agents/restaurant";

    public async Task HandleAsync(A2UIStreamWriter writer, CancellationToken cancellationToken)
    {
        await writer.WriteCreateSurfaceAsync("restaurant-finder", sendDataModel: true);

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

        var components = new List<Dictionary<string, object>>();

        components.Add(new ComponentBuilder("root", "Column").Children("header", "search-row", "divider1", "results-list").Build());
        components.Add(new ComponentBuilder("header", "Text").Text("Restaurant Finder").Variant("h2").Build());
        components.Add(new ComponentBuilder("search-row", "Row").Children("search-field", "search-btn").Gap("8").Align("end").Build());
        components.Add(new ComponentBuilder("search-field", "TextField").Placeholder("Search restaurants...").Label("Search").Action("search").Build());
        components.Add(new ComponentBuilder("search-btn", "Button").Label("Search").Action("search").Build());
        components.Add(new ComponentBuilder("divider1", "Divider").Build());
        components.Add(new ComponentBuilder("results-list", "List").Data("/restaurants").Template("restaurant-card").Build());
        components.Add(new ComponentBuilder("restaurant-card", "Card").Title("name").Children("card-body").Build());
        components.Add(new ComponentBuilder("card-body", "Row").Children("card-cuisine", "card-rating", "card-price").Justify("spaceBetween").Build());
        components.Add(new ComponentBuilder("card-cuisine", "Text").Text("cuisine").Variant("body").Build());
        components.Add(new ComponentBuilder("card-rating", "Text").Text("rating").Variant("caption").Build());
        components.Add(new ComponentBuilder("card-price", "Text").Text("priceRange").Variant("caption").Build());

        await writer.WriteUpdateComponentsAsync("restaurant-finder", components);

        try { await Task.Delay(Timeout.Infinite, cancellationToken); }
        catch (OperationCanceledException) { }
    }

    public async Task HandleActionAsync(A2UIStreamWriter writer, UserActionRequest action, CancellationToken cancellationToken)
    {
        if (action.Name == "search")
        {
            var query = action.Context?.GetValueOrDefault("value")?.ToString() ?? "";

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
