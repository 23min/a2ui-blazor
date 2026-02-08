# A2UI Blazor — Session Context

> Saved so we can resume after devcontainer rebuild.

## What We're Building

A complete A2UI Blazor WebAssembly renderer ecosystem — a set of .NET 8 projects
enabling Blazor WASM apps to render agent-driven UIs using the A2UI v0.9 protocol.

## Completed

1. **Feasibility research** captured in [docs/research/feasibility-assessment.md](docs/research/feasibility-assessment.md)
2. **Architecture PRD** written at [docs/prd/architecture.md](docs/prd/architecture.md)
3. **A2UI v0.9 spec researched** — message types: `createSurface`, `updateComponents`, `updateDataModel`, `deleteSurface`
4. **Devcontainer** configured with .NET 8 SDK

## What's Next — In Order

3. **Create solution file and project scaffolding** (was in progress)
   - `A2UI.Blazor.sln`
   - `Directory.Build.props`
   - `.gitignore`
   - `global.json`
   - Four project files:
     - `src/A2UI.Blazor/` — Core renderer library (NuGet package)
     - `src/A2UI.Blazor.Server/` — Server provider library (NuGet package)
     - `samples/A2UI.Blazor.Example.Server/` — Example ASP.NET Core API + host
     - `samples/A2UI.Blazor.Example.Client/` — Example Blazor WASM app

4. **Build core renderer library** (A2UI.Blazor)
   - Protocol models (C# classes for A2UI messages)
   - `JsonlStreamReader` — parses HTTP JSONL streams
   - `MessageDispatcher` — routes by message type
   - `SurfaceManager` — maintains surfaces, components, data model
   - `DataBindingResolver` — JSON Pointer (RFC 6901) → value resolution
   - `ComponentRegistry` — maps A2UI type strings → Blazor component Types
   - Blazor component catalog: Text, Image, Icon, Divider, Row, Column, Card,
     List, Tabs, Button, TextField, CheckBox, ChoicePicker, DateTimeInput, Slider,
     Video, AudioPlayer
   - `A2UISurface.razor` — top-level surface renderer
   - `A2UIComponentRenderer.razor` — dynamic component dispatch
   - DI extensions (`AddA2UIBlazor()`)
   - CSS stylesheet

5. **Build server provider** (A2UI.Blazor.Server)
   - `SurfaceBuilder` / `ComponentBuilder` — fluent API
   - `A2UIStreamWriter` — serializes to JSONL
   - `A2UIStreamMiddleware` — ASP.NET Core middleware for SSE/JSONL streaming
   - `IA2UIAgent` — agent interface
   - DI extensions (`AddA2UIServer()`, `MapA2UIAgent()`)

6. **Build example application**
   - Server: Restaurant finder agent, contact lookup agent, component gallery agent
   - Client: Pages that connect to each agent, render surfaces

7. **Write PRDs** for core renderer, server provider, and example app

8. **Write README.md** — compelling story of A2UI Blazor

9. **Write integration docs**

## Key A2UI v0.9 Spec Details

### Message Types (Server → Client)
- `createSurface`: `{ surfaceId, catalogId, theme?, sendDataModel? }`
- `updateComponents`: `{ surfaceId, components[] }`
- `updateDataModel`: `{ surfaceId, path?, value? }`
- `deleteSurface`: `{ surfaceId }`

### Message Types (Client → Server)
- `action`: `{ name, surfaceId, sourceComponentId, timestamp, context }`
- `error`: `{ code: "VALIDATION_FAILED", surfaceId, path, message }`

### Component Object
- `id` (required) — unique within surface, one must be "root"
- `component` (required) — type string from catalog
- Additional properties per component type

### Data Binding
- JSON Pointer (RFC 6901) paths
- Absolute: `/user/profile/name`
- Relative: `firstName` (within collection scope)
- Dynamic types: `DynamicString`, `DynamicNumber`, `DynamicBoolean`, `DynamicStringList`

### Standard Components
| Category | Components |
|----------|-----------|
| Display | Text, Image, Icon, Divider |
| Layout | Row, Column, Card, List, Tabs |
| Input | Button, TextField, CheckBox, ChoicePicker, DateTimeInput, Slider |
| Media | Video, AudioPlayer |

### Actions
- Server action: `{ event: { name, context? } }`
- Local action: `{ functionCall: { call, args } }`

### UI Composition
- Adjacency list: flat array of components with parent-child via ID references
- One component must have `id: "root"`
