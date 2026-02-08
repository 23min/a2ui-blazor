# A2UI Blazor — Session Context 2

> Saved before devcontainer rebuild (adding Python 3.12 + uv).

## What This Is

A .NET 8 implementation of Google's [A2UI (Agent-to-UI)](https://a2ui.org/) protocol
for Blazor. Agents describe UIs as declarative JSON; we render them as native Blazor
components — in WebAssembly or Server mode — against any A2UI-compliant backend.

## What's Built (all complete, 0 warnings, 0 errors)

### Libraries (`src/`)

**A2UI.Blazor** — Core renderer library (the main NuGet deliverable)
- Protocol models: `A2UIMessage`, `A2UIComponentData`, `A2UISurfaceState`, action types
- Services: `JsonlStreamReader` (JSONL/SSE), `MessageDispatcher`, `SurfaceManager`,
  `DataBindingResolver` (RFC 6901 JSON Pointer), `ComponentRegistry`
- 16 standard components: Text, Image, Icon, Divider, Row, Column, Card, List, Tabs,
  Button, TextField, CheckBox, ChoicePicker, DateTimeInput, Slider, Video, AudioPlayer
- `A2UISurface.razor` (top-level renderer), `A2UIComponentRenderer.razor` (DynamicComponent dispatch)
- `A2UIComponentBase` (base class with data binding helpers)
- DI: `AddA2UIBlazor()` with extensible component registry
- CSS: `wwwroot/a2ui.css`

**A2UI.Blazor.Server** — Optional server-side helpers for .NET backends
- Fluent API: `SurfaceBuilder`, `ComponentBuilder`
- `A2UIStreamWriter` (JSONL/SSE serialization)
- `A2UIStreamMiddleware` (ASP.NET Core middleware — GET→stream, POST→action)
- `IA2UIAgent` interface
- DI: `AddA2UIServer()`, `AddA2UIAgent<T>()`, `UseA2UIAgents()`

### Samples (`samples/`)

| Sample | Port | Purpose |
|--------|------|---------|
| `python-server/` | 8000 | Python FastAPI A2UI server — proves protocol agnosticism |
| `dotnet-server/` | 5050 | .NET Minimal API using A2UI.Blazor.Server — .NET server story |
| `blazor-wasm-spa/` | auto | Standalone Blazor WASM SPA — configurable `A2UIServerUrl` |
| `blazor-server-app/` | 5100 | Blazor Server (SSR) — enterprise hosting model |

All three agents (restaurant finder, contacts, gallery) are implemented in both
the Python and .NET servers. Both Blazor clients (WASM SPA and Server) consume them
identically — proving the library works across server languages and hosting models.

### Dev Tooling

- `.vscode/tasks.json` — Build All, Start each server/client, compound Demo tasks
- `.vscode/launch.json` — F5 debugging for all .NET projects, compound configs
- `.devcontainer/devcontainer.json` — .NET 8, Python 3.12, uv, Node 20, GitHub CLI

## Git Log

```
8024f3b Switch Python server to uv, add Python + uv to devcontainer
5a926b3 Add VS Code tasks and launch configs for all samples
1ed30d6 Restructure samples: standalone SPA, Python server, Blazor Server
ecb4153 Add README with project overview, getting started, and architecture
f11e945 Implement A2UI Blazor renderer and server libraries
725a8a8 Initial project setup: research, architecture, and devcontainer
```

## Key Design Decisions

- **`ComponentBuilder.Build()` is public** — needed by sample projects outside the library
- **`TreatWarningsAsErrors` enabled** in `Directory.Build.props` — async without await = build error
- **CORS on servers** — both Python (FastAPI middleware) and .NET (`AddCors`) allow `*` for local dev
- **`uv run` for Python** — no pip, no manual venv; `pyproject.toml` with uv handles everything
- **Configurable server URL** — WASM SPA reads `A2UIServerUrl` from `wwwroot/appsettings.json`

## What's Next

1. **Rebuild devcontainer** — picks up Python 3.12 + uv
2. **Test the Python server** — `uv run uvicorn server:app --port 8000`
3. **End-to-end test** — run a server + client combo, verify surfaces render
4. **A2UIStreamClient as a reusable service** — currently duplicated across samples;
   could be promoted into A2UI.Blazor as a convenience class
5. **Unit tests** — DataBindingResolver, JsonlStreamReader, ComponentRegistry
6. **Theming** — the protocol supports `theme` in `createSurface`; not yet wired
7. **NuGet packaging** — `.nuspec` or `PackageReference` metadata for publishing

## File Tree (key files)

```
src/A2UI.Blazor/
  Protocol/A2UIMessage.cs, A2UISurfaceState.cs
  Services/JsonlStreamReader.cs, MessageDispatcher.cs, SurfaceManager.cs,
           DataBindingResolver.cs, ComponentRegistry.cs
  Components/A2UIComponentBase.cs, A2UISurface.razor, A2UIComponentRenderer.razor
  Components/Display/  (Text, Image, Icon, Divider)
  Components/Layout/   (Row, Column, Card, List, Tabs)
  Components/Input/    (Button, TextField, CheckBox, ChoicePicker, DateTimeInput, Slider)
  Components/Media/    (Video, AudioPlayer)
  wwwroot/a2ui.css
  A2UIBlazorServiceExtensions.cs

src/A2UI.Blazor.Server/
  Agents/IA2UIAgent.cs
  Builders/ComponentBuilder.cs, SurfaceBuilder.cs
  Streaming/A2UIStreamWriter.cs, A2UIStreamMiddleware.cs
  A2UIServerExtensions.cs

samples/python-server/server.py, pyproject.toml
samples/dotnet-server/Program.cs, Agents/*.cs
samples/blazor-wasm-spa/Program.cs, Pages/*.razor, Services/A2UIStreamClient.cs
samples/blazor-server-app/Program.cs, Pages/*.razor, Pages/_Host.cshtml
```
