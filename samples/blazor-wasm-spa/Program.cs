using A2UI.Blazor;
using blazor_wasm_spa;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

// Server URL is configurable — point this at any A2UI-compliant server.
// Default: the .NET server sample on port 5050.
// Change to http://localhost:8000 to use the Python server instead.
var serverUrl = builder.Configuration["A2UIServerUrl"] ?? "http://localhost:5050";

builder.Services.AddScoped(sp =>
    new HttpClient { BaseAddress = new Uri(serverUrl) });

builder.Services.AddA2UIBlazor();

await builder.Build().RunAsync();
