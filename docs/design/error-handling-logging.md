# Error Handling + Logging Architecture

**Status**: Implemented in v0.2.0-preview
**Date**: 2026-02-10

## Overview

A2UI Blazor implements production-grade error handling and logging throughout the library following Microsoft best practices. This document describes the error boundary strategy, logging architecture, and diagnostic patterns used across all core services.

## Error Handling Strategy

### Three-Tier ErrorBoundary Architecture

Blazor's `ErrorBoundary` component is used at three levels to isolate failures and provide graceful degradation:

```
┌─────────────────────────────────────────────┐
│  App Layout ErrorBoundary                   │  ← Tier 3: Whole page
│  ┌───────────────────────────────────────┐  │
│  │  A2UISurface ErrorBoundary            │  │  ← Tier 2: Per surface
│  │  ┌─────────────────────────────────┐  │  │
│  │  │  A2UIComponentRenderer          │  │  │  ← Tier 1: Per component
│  │  │  ErrorBoundary                  │  │  │
│  │  │  ┌───────────────────────────┐  │  │  │
│  │  │  │  DynamicComponent          │  │  │  │
│  │  │  │  (Button, Text, etc.)      │  │  │  │
│  │  │  └───────────────────────────┘  │  │  │
│  │  └─────────────────────────────────┘  │  │
│  └───────────────────────────────────────┘  │
└─────────────────────────────────────────────┘
```

#### Tier 1: Component-Level ErrorBoundary

**Location**: [A2UIComponentRenderer.razor](../../src/A2UI.Blazor/Components/A2UIComponentRenderer.razor)

**Wraps**: Each `DynamicComponent` rendering an A2UI component

**Error UI**:
- Known component that failed: `.a2ui-component-error` (red dashed border, error message)
- Unknown component type: `.a2ui-component-unknown` (orange dashed border, type name)

**Recovery**: Auto-recovers when `OnParametersSet()` is called with new data (component updates from stream)

**Example**:
```razor
<ErrorBoundary @ref="_errorBoundary">
    <ChildContent>
        <DynamicComponent Type="_componentType" Parameters="_parameters" />
    </ChildContent>
    <ErrorContent Context="ex">
        @(LogComponentError(ex))
        <div class="a2ui-component-error" title="@ex.Message">
            Component "@Data.Component" failed to render
        </div>
    </ErrorContent>
</ErrorBoundary>
```

**Rationale**: If a single button or text field fails to render (due to malformed properties, missing bindings, etc.), only that component shows an error. The rest of the surface continues to work.

#### Tier 2: Surface-Level ErrorBoundary

**Location**: [A2UISurface.razor](../../src/A2UI.Blazor/Components/A2UISurface.razor)

**Wraps**: Entire surface content (root component tree)

**Error UI**: `.a2ui-surface-error` with "Something went wrong" message and Retry button

**Recovery**:
- Auto-recovers when `HandleSurfaceChanged` is called (new data from stream)
- Manual recovery via Retry button that calls `_surfaceErrorBoundary?.Recover()`

**Example**:
```razor
<ErrorBoundary @ref="_surfaceErrorBoundary">
    <ChildContent>
        @if (_surface?.GetRoot() is { } root)
        {
            <A2UIComponentRenderer Data="root" Surface="_surface" OnAction="HandleAction" />
        }
    </ChildContent>
    <ErrorContent Context="ex">
        @(LogSurfaceError(ex))
        <div class="a2ui-surface-error">
            <p>Something went wrong.</p>
            <button class="a2ui-button a2ui-button-secondary"
                    @onclick="() => _surfaceErrorBoundary?.Recover()">
                Retry
            </button>
        </div>
    </ErrorContent>
</ErrorBoundary>
```

**Rationale**: If the entire surface fails (root component error, severe rendering issue), show a recoverable error state. The user can retry or wait for new data from the agent to auto-recover.

#### Tier 3: App-Level ErrorBoundary

**Location**: Sample layouts (`MainLayout.razor` in both WASM and Server samples)

**Wraps**: `@Body` (entire page content)

**Error UI**: Centered error message ("An unexpected error occurred")

**Recovery**: Page reload required (no auto-recovery, as this is outside A2UI's control)

**Example**:
```razor
<main class="app-main">
    <ErrorBoundary>
        <ChildContent>@Body</ChildContent>
        <ErrorContent>
            <div style="padding: 2rem; text-align: center; color: #d32f2f;">
                An unexpected error occurred.
            </div>
        </ErrorContent>
    </ErrorBoundary>
</main>
```

**Rationale**: If an error escapes the surface boundaries (routing errors, page-level component failures), prevent a complete app crash. This is a best practice for production Blazor apps.

### Protected Event Invocations

All public events that consumers can subscribe to are wrapped in try/catch blocks to prevent subscriber exceptions from crashing the library:

**Pattern**:
```csharp
private void NotifySurfaceChanged(string surfaceId)
{
    try
    {
        OnSurfaceChanged?.Invoke(surfaceId);
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Subscriber error in OnSurfaceChanged for surface {SurfaceId}", surfaceId);
    }
}
```

**Applied to**:
- `A2UIStreamClient.OnStateChanged` — protects stream state change notifications
- `SurfaceManager.OnSurfaceChanged` — protects surface update notifications

**Rationale**: Library code should never crash due to consumer code errors. If a page's event handler throws, log it but continue processing the stream.

### Error Handling in Sample Pages

All sample pages wrap `SendActionAsync` calls in try/catch blocks to display user-friendly error messages:

```csharp
private async Task HandleAction(A2UIUserAction action)
{
    try
    {
        await Client.SendActionAsync("/agents/restaurant", action);
    }
    catch (Exception ex)
    {
        _error = $"Failed to send action: {ex.Message}";
        await InvokeAsync(StateHasChanged);
    }
}
```

This pattern is demonstrated in 4 pages:
- `RestaurantPage.razor` (WASM + Server)
- `ContactsPage.razor` (WASM + Server)

Gallery and State Machine pages don't need it as they have no user interactions.

## Logging Architecture

### Microsoft.Extensions.Logging Integration

All core services use `ILogger<T>` for diagnostics. Loggers are injected via constructor DI and registered automatically by ASP.NET Core / Blazor.

### EventId Organization

Event IDs are organized in ranges of 100 per service, defined in [LogEvents.cs](../../src/A2UI.Blazor/Diagnostics/LogEvents.cs):

```csharp
namespace A2UI.Blazor.Diagnostics;

internal static class LogEvents
{
    // A2UIStreamClient (1000-1099)
    public const int Connecting = 1000;
    public const int Connected = 1001;
    public const int Reconnecting = 1002;
    public const int Disconnected = 1003;
    public const int StreamEnded = 1004;
    public const int ClientError = 1005;
    public const int StreamError = 1006;
    public const int SendingAction = 1010;
    public const int ActionFailed = 1011;

    // MessageDispatcher (2000-2099)
    public const int UnknownMessageType = 2001;
    public const int NullSurfaceId = 2002;

    // SurfaceManager (3000-3099)
    public const int SurfaceCreated = 3000;
    public const int SurfaceDeleted = 3001;
    public const int UnknownSurface = 3005;

    // JsonlStreamReader (4000-4099)
    public const int ParseError = 4001;

    // ComponentRegistry (5000-5099)
    public const int ComponentNotFound = 5002;
}
```

**Rationale**: Event IDs make it easy to filter and aggregate logs in production. Each service has a dedicated range to avoid conflicts.

### Log Levels

| Level | Usage | Examples |
|-------|-------|----------|
| **Debug** | Detailed diagnostic information | Surface initialized, component updated, delay before reconnect |
| **Information** | Lifecycle events | Connecting, Connected, Surface created, Surface deleted |
| **Warning** | Recoverable issues | Malformed JSONL line, unknown message type, unknown component, null surfaceId |
| **Error** | Operation failures | Client error (4xx), stream error, action send failed, component render failed |

**Guideline**: Use Information for production-relevant events, Debug for development diagnostics, Warning for issues that don't stop execution, Error for failures that require attention.

### Structured Logging Patterns

All log messages use **message templates** with named parameters (not string interpolation):

✅ **Correct**:
```csharp
_logger.LogInformation(LogEvents.Connected, "Connected to {AgentPath}", agentPath);
```

❌ **Wrong**:
```csharp
_logger.LogInformation($"Connected to {agentPath}");
```

**Rationale**: Message templates enable structured logging. Log aggregation systems (Application Insights, Seq, etc.) can index and query by parameter values.

### Service-by-Service Logging Coverage

#### A2UIStreamClient (11 log points)

| Event | Level | Message Template | When |
|-------|-------|------------------|------|
| Connecting | Information | `"Connecting to {AgentPath}"` | First connection attempt |
| Reconnecting | Information | `"Reconnecting to {AgentPath} (attempt {Attempt})"` | Subsequent attempts after drop |
| Connected | Information | `"Connected to {AgentPath}"` | First message received |
| StreamEnded | Information | `"Stream ended for {AgentPath}, will reconnect"` | Normal stream close |
| Disconnected | Information | `"Disconnected from {AgentPath}"` | Cancellation or final disconnect |
| ClientError | Error | `"Client error {StatusCode} connecting to {AgentPath}"` | 4xx HTTP status (no retry) |
| StreamError | Warning | `"Stream error for {AgentPath}, will retry"` | 5xx, network errors (will retry) |
| SendingAction | Debug | `"Sending action {ActionName} to {AgentPath}"` | User action dispatch |
| ActionFailed | Error | `"Failed to send action {ActionName} to {AgentPath}"` | Action send exception |
| (unnamed) | Debug | `"Delaying {DelayMs}ms before reconnect attempt {Attempt}"` | Backoff delay |
| (unnamed) | Error | `"Subscriber error in OnStateChanged for state {State}"` | Event subscriber crash |

#### MessageDispatcher (6 log points)

| Event | Level | Message Template | When |
|-------|-------|------------------|------|
| (unnamed) | Debug | `"Dispatching message type {MessageType} for surface {SurfaceId}"` | Every message |
| UnknownMessageType | Warning | `"Unknown A2UI message type {MessageType} for surface {SurfaceId}"` | Unrecognized type |
| NullSurfaceId | Warning | `"{MessageType} message missing surfaceId"` | createSurface, updateComponents, etc. with null ID |

#### SurfaceManager (8 log points)

| Event | Level | Message Template | When |
|-------|-------|------------------|------|
| SurfaceCreated | Information | `"Creating surface {SurfaceId} with catalogId {CatalogId}"` | createSurface |
| SurfaceDeleted | Information | `"Deleting surface {SurfaceId}"` | deleteSurface |
| UnknownSurface | Warning | `"Cannot update components for unknown surface {SurfaceId}"` | Update to non-existent surface |
| (unnamed) | Debug | `"Updating {ComponentCount} components for surface {SurfaceId}"` | updateComponents |
| (unnamed) | Debug | `"Replacing entire data model for surface {SurfaceId}"` | updateDataModel at root |
| (unnamed) | Debug | `"Patching data model at path {Path} for surface {SurfaceId}"` | updateDataModel at path |
| (unnamed) | Error | `"Failed to parse data model update for surface {SurfaceId} at path {Path}"` | JSON parse error |
| (unnamed) | Error | `"Subscriber error in OnSurfaceChanged for surface {SurfaceId}"` | Event subscriber crash |

#### JsonlStreamReader (1 log point)

| Event | Level | Message Template | When |
|-------|-------|------------------|------|
| ParseError | Warning | `"Failed to parse JSONL line: {Line}"` | Malformed JSON (previously silent) |

#### ComponentRegistry (1 log point)

| Event | Level | Message Template | When |
|-------|-------|------------------|------|
| ComponentNotFound | Warning | `"Unknown component type: {ComponentType}"` | Resolve returns null |

#### A2UISurface (3 log points)

| Event | Level | Message Template | When |
|-------|-------|------------------|------|
| (unnamed) | Debug | `"A2UISurface {SurfaceId} initialized"` | OnInitialized |
| (unnamed) | Debug | `"A2UISurface {SurfaceId} received update"` | HandleSurfaceChanged |
| (unnamed) | Debug | `"A2UISurface {SurfaceId} disposed"` | Dispose |

#### A2UIComponentRenderer (2 log points)

| Event | Level | Message Template | When |
|-------|-------|------------------|------|
| (unnamed) | Error | `"Component {ComponentType} failed to render"` | ErrorBoundary caught exception |
| ComponentNotFound | Warning | `"Unknown component type {ComponentType}"` | Registry.Resolve returns null |

### Testing with NullLogger

All unit tests use `NullLogger<T>.Instance` to provide loggers without actual output:

```csharp
var logger = NullLogger<A2UIStreamClient>.Instance;
var client = new A2UIStreamClient(http, reader, dispatcher, logger);
```

This keeps test output clean while exercising the same code paths as production.

## CSS Error States

Three error state styles are defined in [a2ui.css](../../src/A2UI.Blazor/wwwroot/a2ui.css):

```css
:root {
    --a2ui-error: #d32f2f;
    --a2ui-error-bg: #fff5f5;
    --a2ui-warning: #f57c00;
}

.a2ui-component-error {
    padding: 0.5rem 0.75rem;
    border: 1px dashed var(--a2ui-error);
    border-radius: var(--a2ui-radius);
    background: var(--a2ui-error-bg);
    color: var(--a2ui-error);
    font-size: 0.85rem;
}

.a2ui-component-unknown {
    padding: 0.5rem 0.75rem;
    border: 1px dashed var(--a2ui-warning);
    border-radius: var(--a2ui-radius);
    color: var(--a2ui-warning);
    font-size: 0.85rem;
}

.a2ui-surface-error {
    padding: 1.5rem;
    text-align: center;
    color: var(--a2ui-error);
}
```

## Debugging Guide

### Filtering Logs by Service

Use EventId ranges to filter logs in production:

```csharp
// appsettings.json
{
  "Logging": {
    "LogLevel": {
      "A2UI.Blazor.Services.A2UIStreamClient": "Information",
      "A2UI.Blazor.Services.MessageDispatcher": "Warning",
      "A2UI.Blazor.Services.SurfaceManager": "Debug"
    }
  }
}
```

Or query by EventId in Application Insights:

```kusto
traces
| where customDimensions.EventId >= 1000 and customDimensions.EventId < 1100
| where severityLevel >= 2  // Warning and above
```

### Common Scenarios

**"Surface not rendering"**:
1. Check for `UnknownSurface` (3005) warnings — surface never created
2. Check for `ParseError` (4001) warnings — malformed JSONL breaking stream
3. Check for `ComponentNotFound` (5002) warnings — unknown component types
4. Look for `StreamError` (1006) — connection issues

**"Actions not working"**:
1. Check for `ActionFailed` (1011) errors — send failures
2. Check for `ClientError` (1005) — 4xx response from agent
3. Verify `SendingAction` (1010) debug logs show correct action name

**"Random crashes"**:
1. Look for "Subscriber error" messages — consumer event handlers throwing
2. Check ErrorBoundary error logs — component render failures
3. Verify no uncaught exceptions in browser console

## Design Decisions

### Why Three ErrorBoundary Tiers?

**Blast radius control**: A failing button shouldn't crash the surface, a failing surface shouldn't crash the page. Each tier isolates failures at the appropriate scope.

### Why Auto-Recovery?

**Self-healing UIs**: When new data arrives from the agent, previous errors may no longer apply (e.g., the agent fixed a malformed property). Auto-recovery tries the render again without user intervention.

### Why Protected Event Invocations?

**Library robustness**: Consumer code should never crash the library. If a page's `OnStateChanged` handler throws, the stream client should continue processing messages.

### Why EventId Ranges?

**Production filtering**: In a large application with many services logging, EventId ranges make it trivial to filter to A2UI-specific issues (1000-5999) or zoom into a specific service (1000-1099 = StreamClient).

### Why Structured Logging?

**Queryable diagnostics**: Production log aggregation systems (App Insights, Datadog, Splunk) can index and query structured parameters. This enables powerful queries like "show all connection failures for agent X" or "count errors by component type."

## Related Documentation

- [State Machine Design](state-machine.md) — Custom component implementation example
- [Architecture](../prd/architecture.md) — Overall system design
- [ROADMAP](../../ROADMAP.md) — v0.2.0 production hardening milestone

## Future Enhancements

**Potential improvements for v0.3.0+**:

1. **Circuit breaker** for repeated action send failures (stop retrying after N failures)
2. **Telemetry events** for `Activity` / OpenTelemetry tracing
3. **ErrorBoundary recovery strategies** (exponential backoff before retry)
4. **Detailed error codes** beyond HTTP status (A2UI-specific error taxonomy)
5. **Error reporting callback** for consumer apps to hook into error flow

---

*This design was implemented in February 2026 as part of the v0.2.0-preview milestone. All 134 existing tests pass with the new error handling and logging infrastructure.*
