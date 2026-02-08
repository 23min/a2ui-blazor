using A2UI.Blazor;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorPages();
builder.Services.AddServerSideBlazor();

// The same A2UI.Blazor library used in the WASM SPA — works identically in Blazor Server.
builder.Services.AddA2UIBlazor();

// HttpClient for calling the external A2UI server (Python or .NET).
builder.Services.AddScoped(sp =>
{
    var config = sp.GetRequiredService<IConfiguration>();
    var serverUrl = config["A2UIServerUrl"] ?? "http://localhost:5050";
    return new HttpClient { BaseAddress = new Uri(serverUrl) };
});

var app = builder.Build();

app.UseStaticFiles();
app.UseRouting();
app.MapBlazorHub();
app.MapFallbackToPage("/_Host");

app.Run();
