using A2UI.Blazor.Example.Server.Agents;
using A2UI.Blazor.Server;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddA2UIServer();
builder.Services.AddA2UIAgent<RestaurantFinderAgent>();
builder.Services.AddA2UIAgent<ContactLookupAgent>();
builder.Services.AddA2UIAgent<ComponentGalleryAgent>();

var app = builder.Build();

app.UseStaticFiles();
app.UseA2UIAgents();
app.UseBlazorFrameworkFiles();
app.MapFallbackToFile("index.html");

app.Run();
