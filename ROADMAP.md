# Roadmap

> Versioning follows [SemVer](https://semver.org/). All 0.x releases are pre-release.
> Milestone order is fixed; timelines are not. Contributions welcome at every stage.

---

## v0.1.0-preview — Foundation

**Status: complete**

The core protocol implementation and component catalog.

- [x] JSONL/SSE stream reader with cancellation support
- [x] Message dispatcher (`createSurface`, `updateComponents`, `updateDataModel`, `deleteSurface`)
- [x] Surface state management with change notification
- [x] Data binding via JSON Pointer (RFC 6901)
- [x] 17 standard components: Text, Image, Icon, Divider, Row, Column, Card, List, Tabs, Button, TextField, CheckBox, ChoicePicker, DateTimeInput, Slider, Video, AudioPlayer
- [x] Dynamic component rendering via `A2UIComponentRenderer`
- [x] Extensible component registry (`AddA2UIBlazor(registry => ...)`)
- [x] Server-side library: fluent builders, stream writer, ASP.NET Core middleware
- [x] Four working samples (Python server, .NET server, Blazor WASM, Blazor Server)
- [x] 115 bUnit component tests + 19 Playwright E2E tests

## v0.2.0-preview — Production Hardening

**Status: up next**

Make the library reliable enough for real applications.

- [x] **`A2UIStreamClient` in core library** — promote from samples into `A2UI.Blazor` so consumers don't have to write their own SSE client
- [ ] **Connection resilience** — automatic reconnection with exponential backoff when SSE streams drop
- [ ] **Reconnecting UI** — visual overlay ("Reconnecting...") during stream recovery
- [ ] **Error handling** — structured error boundaries around component rendering, stream parsing, and action dispatch
- [ ] **Logging** — `ILogger<T>` integration throughout core services for diagnostics
- [ ] **Multi-target .NET 8 + .NET 10** — `<TargetFrameworks>net8.0;net10.0</TargetFrameworks>` for both libraries; .NET 8 (LTS until Nov 2026) for existing enterprise consumers, .NET 10 (current LTS) for new projects
- [ ] **NuGet packaging** — complete package metadata, CI build, publish `A2UI.Blazor` and `A2UI.Blazor.Server` to nuget.org
- [ ] **GitHub Actions CI** — build, test, pack on every push; publish on tag
- [x] **Live State Machine demo** — SVG custom component showing real-time agent state transitions; proves the custom component + streaming story

## v0.3.0-preview — Accessibility & Theming

**Status: planned**

Meet WCAG 2.1 AA and support visual customization.

- [ ] **Accessible forms** — proper `<label>` / `for` associations, `aria-describedby`, `aria-invalid`
- [ ] **Keyboard navigation** — visible `:focus-visible` rings on all interactive components
- [ ] **Semantic HTML** — `<ul>` / `<li>` in List, landmark roles where appropriate
- [ ] **Dark mode** — `prefers-color-scheme` media query with automatic light/dark switching
- [ ] **CSS custom property theming** — document how consumers can override `--a2ui-*` variables
- [ ] **`prefers-reduced-motion`** — respect user motion preferences for transitions
- [ ] **Protocol theme support** — wire the `theme` field from `createSurface` to CSS variable injection

## v0.4.0-preview — Advanced Protocol Features

**Status: planned**

Fill in the remaining A2UI protocol capabilities.

- [ ] **Local actions** (`functionCall`) — client-side execution without server round-trip, with a pluggable function registry
- [ ] **Optimistic updates** — update local data model immediately on user action, reconcile when server responds
- [ ] **Surface lifecycle events** — expose `OnSurfaceCreated`, `OnSurfaceDeleted` callbacks for application integration
- [ ] **Validation error rendering** — display inline validation from server `error` messages

## v1.0.0 — Stable Release

**Status: planned**

Public API freeze and long-term support commitment.

- [ ] API review and stabilization (no more breaking changes without major version bump)
- [ ] Comprehensive API documentation (DocFX or similar)
- [ ] Performance benchmarks and optimization pass
- [ ] Migration guide from preview versions
- [ ] Awesome Blazor listing and community outreach

---

## Custom Components (ongoing)

A2UI Blazor's component registry is extensible — any Blazor component can be registered
and driven by an A2UI agent. This is where the protocol gets interesting: agents can
generate rich, domain-specific UIs that go far beyond forms and lists.

Custom components inherit from `A2UIComponentBase` and get data binding, surface state,
and action dispatch for free. The server sends a component type string; the client renders
whatever it wants.

```csharp
builder.Services.AddA2UIBlazor(registry =>
{
    registry.Register("FlowChart", typeof(SvgFlowChart));
    registry.Register("GraphEditor", typeof(InteractiveGraph));
});
```

### Planned custom component demos

#### Editable Graph — "Agent proposes, human corrects"

An SVG-based node-and-edge editor. The agent generates a graph (knowledge graph,
architecture diagram, dependency tree) and the user can drag nodes, add or remove edges,
then send corrections back via A2UI actions. This is the human-in-the-loop pattern:

- Agent sends `updateDataModel` with nodes and edges
- Custom `GraphEditor` component renders them as draggable SVG
- User modifies the layout, adds an edge, deletes a node
- Each edit fires an action back to the agent: `{ name: "graph-edit", context: { ... } }`
- Agent receives the correction and adapts

Use cases: knowledge graph refinement, workflow design, entity relationship modeling.

#### Live State Machine — "Watch the agent think"

An animated flowchart where nodes light up as the agent progresses through steps.
The agent pushes state transitions via `updateDataModel`; the component highlights
the active node and animates the transition edge.

- Pipeline visualization (ETL, CI/CD, multi-agent orchestration)
- Decision tree execution (show which branch the agent took and why)
- Debugging aid (visualize agent reasoning in real time)

#### Data Visualization — Charts from streaming data

SVG-based charts (bar, line, sparkline) driven by the data model. The agent pushes
new data points; the chart updates live without page reload.

- Real-time dashboards (metrics, monitoring, trading)
- Progressive results (search results accumulating as the agent finds them)
- Before/after comparisons (agent shows impact of a proposed change)

#### Interactive Map — Geospatial agent output

Render pins, regions, or routes on a map. The agent sends coordinates; the user
can select, filter, or annotate locations and send feedback.

- Location recommendations (restaurants, properties, service areas)
- Route optimization (agent proposes, user adjusts waypoints)
- Geospatial analysis (heatmaps, clustering results)

#### Component Playground — Build your own

A guide and template for building custom A2UI components:

- Inherit from `A2UIComponentBase`
- Read properties via `GetString()`, `GetElement()`, data binding
- Fire actions via `OnAction.InvokeAsync()`
- Register in `AddA2UIBlazor(registry => ...)`
- SVG, Canvas (via JS interop), or plain HTML — anything Blazor can render

---

## Notes

Open questions and ideas that aren't committed to but worth exploring. Discussion welcome.

### Host app integration

`<A2UISurface>` is a regular Blazor component — drop it anywhere in an existing page.
But interesting questions arise when A2UI is *part* of a larger app rather than the whole thing:

- **Surface to host communication** — Today the only bridge is `OnAction`. The host app
  can intercept actions and react (navigate, show a toast, update its own state). But there's
  no way for the host to observe data model changes *inside* a surface. Something like
  `SurfaceManager.OnDataModelChanged` could let the host react when an agent pushes new data.

- **Host to surface communication** — What if the user logs in (normal Blazor) and the agent
  needs to know? Or the host wants to inject context (user preferences, auth tokens, locale)
  into the surface data model? A `SurfaceManager.InjectData(surfaceId, path, value)` API
  could bridge this gap.

- **Multi-surface coordination** — Two surfaces on one page, driven by different agents.
  One surface's action should affect the other. Today the host would have to manually proxy.
  A cross-surface event bus could make this declarative.

- **Navigation** — An agent wants the host to navigate to a different page. The host would
  check `OnAction` for a navigation-like action and call `NavigationManager.NavigateTo()`.
  Should the library provide a convention for this, or leave it to the consumer?

- **Form integration** — Can an A2UI surface participate in a Blazor `<EditForm>`? Can A2UI
  input components contribute to Blazor validation? Probably out of scope, but worth noting.

### Design system compatibility

A2UI ships its own CSS (`a2ui.css`) with `--a2ui-*` custom properties. When the host app
uses MudBlazor, Tailwind, or another design system, the A2UI surface can look out of place.

Possible approaches (not decided):

- **CSS variable mapping guide** — document how to override `--a2ui-*` to match your
  design system (e.g., set `--a2ui-primary` to your brand color)
- **Headless mode** — render A2UI components with no built-in styles, letting the host's
  CSS take over entirely. High effort, maximum flexibility.
- **Design system adapters** — thin CSS layers that map A2UI variables to MudBlazor/Tailwind
  tokens. Lower effort than headless, good enough for most cases.

### What A2UI is not

Worth being explicit about scope:

- A2UI is not a full app framework — it renders agent-driven surfaces, not entire applications.
- A2UI components don't replace your design system — they're a separate rendering layer for
  agent output.
- Communication between A2UI and normal Blazor uses standard Blazor patterns (callbacks,
  cascading parameters, DI services). The library doesn't try to own the whole page.

---

## Sample & Demo Improvements (ongoing)

Not tied to a specific version — these improve the project's showcase value.

- [ ] Polish sample app aesthetics (responsive layout, transitions, loading states)
- [ ] Interactive component gallery with live property editing
- [ ] Guide: "Build your first A2UI agent in 5 minutes"
- [ ] Guide: "Build a custom A2UI component"
- [ ] Guide: "Using A2UI in an existing Blazor app"

## Contributing

See an item you'd like to work on? Open an issue to discuss the approach, then submit a PR. All contributions are welcome — code, docs, bug reports, and design feedback.
