# A2UI Blazor

A .NET implementation of Google's [A2UI (Agent-to-UI)](https://a2ui.org/) protocol for Blazor.

Agents describe UIs as declarative JSON. A2UI Blazor renders them as native Blazor components — in WebAssembly or Server mode — against any A2UI-compliant backend, in any language.

## Why This Exists

Google's A2UI protocol lets AI agents generate rich, interactive UIs without sending executable code. The agent sends a component tree; the client renders it with trusted, pre-approved widgets.

Official renderers exist for [Lit](https://github.com/google/A2UI), [Angular](https://github.com/google/A2UI), and [Flutter](https://docs.flutter.dev/ai/genui). Enterprise .NET teams building agentic applications had no first-class option — until now.

**A2UI Blazor brings A2UI to the .NET ecosystem.** Same protocol, same security model, native Blazor components.

## What's In This Repo

### Libraries (the deliverables)

| Package | Description |
|---------|-------------|
| **`A2UI.Blazor`** | Core renderer library. Install this in any Blazor app to render A2UI surfaces. Parses JSONL/SSE streams, manages surface state, resolves data bindings, renders 16 standard components. |
| **`A2UI.Blazor.Server`** | Optional server-side helpers for .NET backends. Fluent builders, stream writer, ASP.NET Core middleware. Not required — the client works with any A2UI server. |

### Samples

| Sample | Purpose |
|--------|---------|
| **`samples/dotnet-server/`** | .NET Minimal API serving A2UI streams using `A2UI.Blazor.Server` |
| **`samples/python-server/`** | Python FastAPI server (~50 lines) proving protocol agnosticism |
| **`samples/blazor-wasm-spa/`** | Standalone Blazor WebAssembly SPA consuming A2UI |
| **`samples/blazor-server-app/`** | Blazor Server app — same library, server-side rendering |

> The Blazor SPA renders identically whether pointed at the Python server or the .NET server. That's the point.

## Standard Component Catalog

| Category | Components |
|----------|-----------|
| Display | Text, Image, Icon, Divider |
| Layout | Row, Column, Card, List, Tabs |
| Input | Button, TextField, CheckBox, ChoicePicker, DateTimeInput, Slider |
| Media | Video, AudioPlayer |

Custom components can be registered at startup:

```csharp
builder.Services.AddA2UIBlazor(registry =>
{
    registry.Register("MyWidget", typeof(MyCustomWidget));
});
```

## Getting Started

### Prerequisites

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- Python 3.10+ (only for the Python server sample)

### Build

```bash
git clone https://github.com/anthropics/a2ui-blazor.git
cd a2ui-blazor
dotnet build
```

### Run the .NET Server + Blazor WASM SPA

```bash
# Terminal 1 — start the .NET A2UI server
dotnet run --project samples/A2UI.Blazor.Example.Server

# Open http://localhost:5000 in your browser
```

### Use A2UI.Blazor in Your Own App

**1. Add the package:**

```bash
dotnet add package A2UI.Blazor
```

**2. Register services in `Program.cs`:**

```csharp
builder.Services.AddA2UIBlazor();
```

**3. Add the CSS to your `index.html` (WASM) or `_Host.cshtml` (Server):**

```html
<link href="_content/A2UI.Blazor/a2ui.css" rel="stylesheet" />
```

**4. Render a surface in any page or component:**

```razor
@using A2UI.Blazor.Components

<A2UISurface SurfaceId="my-surface" OnAction="HandleAction" />
```

**5. Connect to any A2UI endpoint and stream messages:**

```csharp
// In your page's OnInitializedAsync:
var response = await Http.SendAsync(
    new HttpRequestMessage(HttpMethod.Get, "https://my-agent/stream"),
    HttpCompletionOption.ResponseHeadersRead);

var stream = await response.Content.ReadAsStreamAsync();
await foreach (var message in streamReader.ReadMessagesAsync(stream))
{
    dispatcher.Dispatch(message);
}
```

The `SurfaceManager` updates automatically. The `<A2UISurface>` component re-renders.

### Build an A2UI Server in .NET

```csharp
// Program.cs
builder.Services.AddA2UIServer();
builder.Services.AddA2UIAgent<MyAgent>();

app.UseA2UIAgents();
```

```csharp
// MyAgent.cs
public class MyAgent : IA2UIAgent
{
    public string Route => "/agents/my-agent";

    public async Task HandleAsync(A2UIStreamWriter writer, CancellationToken ct)
    {
        await writer.WriteCreateSurfaceAsync("main");

        var components = new List<Dictionary<string, object>>
        {
            new ComponentBuilder("root", "Column")
                .Children("greeting")
                .Build(),
            new ComponentBuilder("greeting", "Text")
                .Text("Hello from A2UI!")
                .UsageHint("h1")
                .Build()
        };

        await writer.WriteUpdateComponentsAsync("main", components);
    }

    public Task HandleActionAsync(A2UIStreamWriter writer,
        UserActionRequest action, CancellationToken ct)
        => Task.CompletedTask;
}
```

## Architecture

```
  Agent (Python, .NET, any language)
    │
    │  A2UI JSONL/SSE stream over HTTP
    ▼
  ┌─────────────────────────────────────┐
  │           A2UI.Blazor               │
  │                                     │
  │  JsonlStreamReader → MessageDispatcher → SurfaceManager
  │                                              │
  │  ComponentRegistry ◄─── DataBindingResolver  │
  │       │                                      │
  │  ┌────▼──────────────────────────────────┐   │
  │  │    Blazor Component Catalog           │   │
  │  │    Text │ Button │ Card │ List │ ...  │   │
  │  └───────────────────────────────────────┘   │
  └─────────────────────────────────────────┘
```

**A2UI.Blazor** is the client library. It:
1. Reads a JSONL/SSE stream from any HTTP endpoint
2. Dispatches messages by type (`createSurface`, `updateComponents`, `updateDataModel`, `deleteSurface`)
3. Maintains surface state (components + data model)
4. Resolves data bindings via JSON Pointer (RFC 6901)
5. Renders components through Blazor's `DynamicComponent`

**A2UI.Blazor.Server** is an optional companion for .NET backends. Fluent builders and ASP.NET Core middleware for producing A2UI streams.

## Protocol Compliance

Targets [A2UI specification v0.9](https://github.com/google/A2UI/blob/main/specification/0.9/docs/a2ui_protocol.md).

| Message Type | Direction | Supported |
|-------------|-----------|-----------|
| `createSurface` | Server → Client | Yes |
| `updateComponents` | Server → Client | Yes |
| `updateDataModel` | Server → Client | Yes |
| `deleteSurface` | Server → Client | Yes |
| `action` | Client → Server | Yes |

## Technology Stack

- **.NET 8** (LTS)
- **Blazor** (WebAssembly and Server)
- **System.Text.Json** (zero third-party dependencies in the core library)
- **ASP.NET Core** (server library only)

## Project Status

This project is under active development. The core renderer and server libraries are functional with all 16 standard components implemented. See [docs/prd/samples-restructure.md](docs/prd/samples-restructure.md) for the planned sample restructuring.

## References

- [A2UI Official Site](https://a2ui.org/)
- [A2UI Protocol Specification (v0.9)](https://github.com/google/A2UI/blob/main/specification/0.9/docs/a2ui_protocol.md)
- [A2UI GitHub Repository](https://github.com/google/A2UI)
- [A2UI Renderer Development Guide](https://a2ui.org/renderers/)
- [A2UI Agent Development Guide](https://a2ui.org/guides/agent-development/)
- [Google Developers Blog — Introducing A2UI](https://developers.googleblog.com/introducing-a2ui-an-open-project-for-agent-driven-interfaces/)

## License

Apache 2.0
