using A2UI.Blazor.Server;
using dotnet_server.Agents;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
        policy.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod());
});

builder.Services.AddA2UIServer();
builder.Services.AddA2UIAgent<RestaurantFinderAgent>();
builder.Services.AddA2UIAgent<ContactLookupAgent>();
builder.Services.AddA2UIAgent<ComponentGalleryAgent>();

var app = builder.Build();

app.UseCors();
app.UseA2UIAgents();

app.Run();
