# A2UI Blazor WebAssembly Renderer — Feasibility Assessment

> Research conducted prior to project inception. This document captures the analysis
> of whether building an A2UI renderer for Blazor WebAssembly is feasible and what
> the implementation entails.

## Verdict: Feasible — Moderate Complexity

Building an A2UI renderer for Blazor WebAssembly is a well-scoped project. A2UI
explicitly provides a Renderer Development Guide and the spec is designed to be
framework-agnostic — the agent sends an abstract component tree and the client maps
it to native widgets. Existing renderers for Lit, Angular, and Flutter serve as
solid reference implementations.

---

## Architecture Layers

### 1. JSONL Stream Parser (Low Complexity)

A parser that reads a streaming response line by line, decoding each line as a
distinct JSON object, plus a message dispatcher to identify the message type
(`beginRendering`, `surfaceUpdate`, `dataModelUpdate`, `deleteSurface`) and route
it to the correct handler.

In Blazor WASM, this uses `System.Text.Json` and `HttpClient` with streaming
(`GetStreamAsync`). This is straightforward.

### 2. Surface & Component State Management (Moderate Complexity)

A Surface is a named UI canvas owned by the client. Agents do not manipulate the
DOM or widgets directly — they send messages that update a surface's state.

Requirements:
- A `SurfaceManager` service maintaining a dictionary of surfaces
- Each surface contains a flat component buffer keyed by ID
- The UI is represented as a flat list of components with ID references
- This adjacency list model maps naturally to a `Dictionary<string, A2UIComponent>`
  that Blazor can diff and re-render

### 3. Component Catalog & Rendering (Highest Complexity)

Standard components that must map to native widgets:
- Text (with `usageHint` for h1-h5, body, caption)
- Image (with fit and usageHint)
- Icon
- Video
- AudioPlayer
- Divider
- Row (with distribution and alignment)
- And more from the standard catalog

In Blazor, each A2UI component type becomes a `.razor` component. The
`RenderTreeBuilder` or `DynamicComponent` handles the mapping from type strings
to Blazor components at runtime.

Key challenges:
- **Data binding resolution** — A2UI uses JSON Pointer paths to bind component
  properties to a separate data model. Requires a resolver that walks the data
  model and provides values to components reactively.
- **Templated lists** — For each item in the data list, render the component
  specified by `template.componentId`, making the item's data available for
  relative data binding within the template.

### 4. Event/Action System (Moderate Complexity)

When a user interacts with a component that has an action defined:
1. Construct a `userAction` payload
2. Resolve all data bindings within the `action.context` against the data model
3. Send the complete `userAction` object to the server

This means wiring up Blazor `@onclick`, `@onchange` etc. to serialize and POST
structured JSON back to the agent.

---

## Blazor-Specific Considerations

### Advantages
- **Declarative alignment**: Blazor's rendering is declarative via
  `RenderTreeBuilder`, which aligns well with A2UI's declarative philosophy
- **Strong typing**: The C# type system provides better compile-time safety on
  the component catalog than JavaScript renderers
- **DynamicComponent**: Introduced in .NET 6, makes string→Type mapping viable
- **Enterprise appeal**: A Blazor renderer would be a meaningful contribution
  for .NET enterprise shops building agentic applications

### Challenges
- **Streaming in WASM**: `HttpClient` supports streaming, but `ReadLineAsync`
  over JSONL streams needs careful handling. `System.IO.Pipelines` or a simple
  `StreamReader` loop works.
- **Dynamic component rendering**: Mapping a string type → Blazor `Type`
  requires a registry dictionary, not reflection-heavy patterns
- **No DOM access**: Unlike the Lit renderer which directly manipulates the DOM,
  Blazor works through its render tree — actually a benefit here
- **Custom component extensibility**: A2UI's open registry pattern maps to a
  `Dictionary<string, Type>` catalog that users can extend with custom `.razor`
  components

---

## Rough Effort Breakdown

| Layer | Estimated Effort |
|---|---|
| JSONL stream parser + message dispatcher | ~2-3 days |
| Surface state management | ~3-5 days |
| Standard component catalog (15+ components) | ~2-3 weeks |
| Data binding / JSON Pointer resolver | ~1 week |
| User action / event system | ~3-5 days |
| Custom component registry + theming | ~1 week |
| Testing, docs, packaging as NuGet | ~1 week |

**Total: roughly 6-8 weeks** for a single experienced Blazor developer.
A basic proof-of-concept rendering simple surfaces could be done much sooner.

---

## Conclusion

The hardest part isn't any single feature — it's getting data binding and
incremental surface updates right so that streaming feels smooth. The
recommendation is to start by porting the Lit renderer's `MessageProcessor`
logic as the reference architecture.

Blazor WASM pairs well with A2UI's architecture: both are declarative,
component-based, and strongly typed. With React "in progress" and native
iOS/Android planned in the A2UI roadmap, there is clear appetite for more
renderers.
