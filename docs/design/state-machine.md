# Design: Live State Machine Demo

## Context

The roadmap (v0.2.0-preview) includes a **Live State Machine** custom component — an SVG visualization where nodes light up as an agent progresses through pipeline steps. This proves the custom component + real-time streaming story and serves as a compelling demo for the project.

The agent simulates an order processing pipeline (Received -> Validating -> Processing -> Billing -> Shipping -> Delivered). It auto-advances through states every ~2 seconds via `updateDataModel`, and the SVG component re-renders each time to highlight the active node, mark completed nodes, and animate the transition.

---

## Design

### Data Model (pushed by server)

```json
{
  "title": "Order Processing Pipeline",
  "states": [
    { "id": "received",   "label": "Received",   "status": "completed" },
    { "id": "validating",  "label": "Validating",  "status": "active" },
    { "id": "processing",  "label": "Processing",  "status": "pending" },
    { "id": "billing",     "label": "Billing",     "status": "pending" },
    { "id": "shipping",    "label": "Shipping",    "status": "pending" },
    { "id": "delivered",   "label": "Delivered",   "status": "pending" }
  ]
}
```

Server auto-advances: every ~2s, marks the current `active` node as `completed` and the next as `active`. When all are `completed`, resets after a pause.

### Component Properties

```json
{
  "id": "pipeline",
  "component": "StateMachine",
  "data": "/pipeline",
  "title": "/pipeline/title"
}
```

- `data`: binding path to the data model object containing `states` array
- `title`: optional title string (supports data binding)

### SVG Layout

Horizontal pipeline: circles (nodes) connected by lines (edges). Responsive via `viewBox`.

```
  O---------O---------O---------O---------O---------O
Received  Validating  Processing  Billing  Shipping  Delivered
```

Visual states:
- **pending**: gray fill, gray border
- **active**: primary blue fill + pulsing glow animation
- **completed**: green fill + checkmark

### Implementation Notes

- Component inherits from `A2UIComponentBase`
- Reads `data` property as raw string (same pattern as `A2UIList`) to get the binding path
- Calls `SurfaceManager.ResolveBinding()` to get the actual data model object
- Parses `states` array from the resolved JSON
- CSS animations for the active node pulse (no JS interop needed)
- Registered in `ComponentRegistry` as `"StateMachine"`

### Files

| File | Action |
|------|--------|
| `src/A2UI.Blazor/Components/Visualization/A2UIStateMachine.razor` | New |
| `src/A2UI.Blazor/wwwroot/a2ui.css` | Append styles |
| `src/A2UI.Blazor/Services/ComponentRegistry.cs` | Register component |
| `samples/python-server/server.py` | Add `/agents/state-machine` endpoint |
| `samples/blazor-wasm-spa/Pages/StateMachinePage.razor` | New page |
| `samples/blazor-wasm-spa/Layout/MainLayout.razor` | Add nav link |
| `samples/blazor-wasm-spa/Pages/Home.razor` | Add card |
| `tests/A2UI.Blazor.Tests/Components/Visualization/StateMachineTests.cs` | New bUnit tests |
| `tests/A2UI.Blazor.Playwright/StateMachinePageTests.cs` | New E2E tests |
