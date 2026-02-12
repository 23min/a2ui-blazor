# Design: Advanced Protocol Features (v0.5.0)

**Status**: Implemented
**Date**: 2026-02-12

## Overview

v0.5.0 adds four protocol-level features to the A2UI Blazor renderer: surface lifecycle events, local action execution (functionCall), optimistic data model updates, and server-sent validation error rendering. These features close gaps between the renderer and the A2UI v0.9 specification.

---

## Feature 1: Surface Lifecycle Events

### Problem

`SurfaceManager` only exposed `OnSurfaceChanged`. Host apps had no way to distinguish surface creation from deletion or react at those specific moments (e.g., logging, analytics, cleanup).

### Design

Two new events on `SurfaceManager`:

```csharp
public event Action<string>? OnSurfaceCreated;   // fires after CreateSurface
public event Action<string>? OnSurfaceDeleted;    // fires after DeleteSurface
```

**Ordering**: `OnSurfaceCreated` fires at the end of `CreateSurface()`, before any `OnSurfaceChanged` (which doesn't fire on create). `OnSurfaceDeleted` fires before `OnSurfaceChanged` in `DeleteSurface()`, giving subscribers a chance to react to the deletion event before the general change notification.

**A2UISurface integration**: The `A2UISurface.razor` component exposes corresponding `EventCallback<string>` parameters so host pages can bind to lifecycle events declaratively:

```razor
<A2UISurface SurfaceId="demo"
             OnSurfaceCreated="HandleCreated"
             OnSurfaceDeleted="HandleDeleted" />
```

### Files Changed

| File | Change |
|------|--------|
| [SurfaceManager.cs](../../src/A2UI.Blazor/Services/SurfaceManager.cs) | Added events, fire in Create/Delete, protected notification helpers |
| [A2UISurface.razor](../../src/A2UI.Blazor/Components/A2UISurface.razor) | Added EventCallback parameters, subscribe/unsubscribe in lifecycle |
| [SurfaceManagerTests.cs](../../tests/A2UI.Blazor.Tests/Services/SurfaceManagerTests.cs) | 5 new tests |

---

## Feature 2: Local Actions (functionCall)

### Problem

The A2UI protocol defines two action types: `action.event` (server round-trip) and `action.functionCall` (client-side execution). Only `event` was implemented — `functionCall` was parsed but never executed.

### Design

A new `LocalActionRegistry` service allows host apps to register client-side action handlers:

```csharp
services.AddA2UIBlazor(configureLocalActions: registry =>
{
    registry.Register("navigate", args =>
        NavigationManager.NavigateTo(args["url"].GetString()!));

    registry.Register("showDialog", args => { /* open modal */ });
});
```

**Dispatch rule**: Components check `action.Event` first. If present, fire a server round-trip. If null, check `action.FunctionCall` — if the call name is registered, execute locally with no server round-trip. If not registered, the action is silently ignored.

**Base class helper**: `A2UIComponentBase.ExecuteLocalAction(A2UILocalAction)` encapsulates the registry lookup and execution.

### Service Registration

`LocalActionRegistry` is registered as a singleton in `AddA2UIBlazor()` with an optional `Action<LocalActionRegistry>?` configuration callback:

```csharp
public static IServiceCollection AddA2UIBlazor(
    this IServiceCollection services,
    Action<ComponentRegistry>? configureComponents = null,
    Action<LocalActionRegistry>? configureLocalActions = null)
```

### Files Changed

| File | Change |
|------|--------|
| [LocalActionRegistry.cs](../../src/A2UI.Blazor/Services/LocalActionRegistry.cs) | New service |
| [A2UIBlazorServiceExtensions.cs](../../src/A2UI.Blazor/A2UIBlazorServiceExtensions.cs) | Register in DI, add configure callback |
| [A2UIComponentBase.cs](../../src/A2UI.Blazor/Components/A2UIComponentBase.cs) | `[Inject] LocalActionRegistry`, `ExecuteLocalAction()` helper |
| All input components + Button | Added `else if (action?.FunctionCall)` fallback in handlers |
| [LocalActionRegistryTests.cs](../../tests/A2UI.Blazor.Tests/Services/LocalActionRegistryTests.cs) | 7 new tests |
| [ButtonTests.cs](../../tests/A2UI.Blazor.Tests/Components/Input/ButtonTests.cs) | 2 new integration tests |

---

## Feature 3: Optimistic Updates

### Problem

When a user types in a bound TextField, the value only updates in the data model when the server responds. Over slow connections, this causes visible lag — the input appears unresponsive.

### Design

Input components update the local data model immediately when the user interacts, before sending the action to the server. The server's subsequent `updateDataModel` message naturally overwrites the optimistic value.

**Key insight**: The data model is already mutable and components already re-render on `OnSurfaceChanged`. We just need to call `SurfaceManager.UpdateDataModel()` locally in the component handler before sending the action.

**Base class helper**: `A2UIComponentBase.ApplyOptimisticUpdate(propertyName, value)`:

```csharp
protected void ApplyOptimisticUpdate(string propertyName, object? value)
{
    // Only applies when the property value is a data binding path (starts with '/')
    // Literal values are ignored — no path to update in the data model
    if (path is null || !path.StartsWith('/')) return;

    var jsonValue = JsonSerializer.SerializeToElement(value);
    SurfaceManager.UpdateDataModel(Surface.SurfaceId, path, jsonValue);
}
```

**Applied to all input components**:

| Component | Property | Trigger |
|-----------|----------|---------|
| TextField | `value` | `@oninput` |
| ChoicePicker | `selected` | `@onchange` |
| Slider | `value` | `@oninput` |
| CheckBox | `checked` | `@onchange` |
| DateTimeInput | `value` | `@onchange` |

### Architecture Change: SurfaceManager Injection

As part of this feature, `SurfaceManager` on `A2UIComponentBase` was changed from `[CascadingParameter]` to `[Inject]`. This is the correct pattern because:

1. `SurfaceManager` is a DI singleton — there's only one instance, so cascading adds no value over injection.
2. `[Inject]` is testable via `Services.AddSingleton()` in bUnit, while `[CascadingParameter]` requires wrapping components in `<CascadingValue>`.
3. The `A2UISurface` cascading wrapper was removed as it was redundant.

### Files Changed

| File | Change |
|------|--------|
| [A2UIComponentBase.cs](../../src/A2UI.Blazor/Components/A2UIComponentBase.cs) | `ApplyOptimisticUpdate()`, `[Inject] SurfaceManager` |
| [A2UISurface.razor](../../src/A2UI.Blazor/Components/A2UISurface.razor) | Removed `<CascadingValue>` wrapper |
| All 5 input components | Added `ApplyOptimisticUpdate()` call in event handlers |
| [TextFieldTests.cs](../../tests/A2UI.Blazor.Tests/Components/Input/TextFieldTests.cs) | 2 new tests (bound + literal) |

---

## Feature 4: Validation Error Rendering

### Problem

Servers can set component-level `error` properties via `updateComponents`, but there was no way to send field-level validation errors via the `error` message type. The protocol defines server-to-client `error` messages, but the renderer only handled client-to-server errors.

### Design

**Message handling**: `MessageDispatcher` now handles `"error"` messages from the server:

```json
{"type": "error", "surfaceId": "s1", "path": "/email", "message": "Invalid email format"}
```

**Storage**: `A2UISurfaceState` gets a `ValidationErrors` dictionary keyed by data model path:

```csharp
public Dictionary<string, string> ValidationErrors { get; } = new();
```

**SurfaceManager API**:

```csharp
public void SetValidationError(string surfaceId, string path, string message);
public void ClearValidationError(string surfaceId, string path);
```

Both fire `OnSurfaceChanged` to trigger re-renders.

**Component rendering**: Input components check validation errors as a fallback when no component-level `error` property is set:

```csharp
var error = _interacted ? null : (GetString("error") ?? GetValidationError("value"));
```

**Precedence**: Component `error` property > Validation error > No error. User interaction clears both (via `_interacted` flag).

**Base class helper**: `A2UIComponentBase.GetValidationError(propertyName)` looks up the property's binding path in `Surface.ValidationErrors`.

### Message Schema

Added two properties to `A2UIMessage`:

```csharp
[JsonPropertyName("message")]
public string? ErrorMessage { get; set; }

[JsonPropertyName("code")]
public string? ErrorCode { get; set; }
```

### Files Changed

| File | Change |
|------|--------|
| [A2UISurfaceState.cs](../../src/A2UI.Blazor/Protocol/A2UISurfaceState.cs) | `ValidationErrors` dictionary |
| [A2UIMessage.cs](../../src/A2UI.Blazor/Protocol/A2UIMessage.cs) | `ErrorMessage`, `ErrorCode` properties |
| [SurfaceManager.cs](../../src/A2UI.Blazor/Services/SurfaceManager.cs) | `SetValidationError`, `ClearValidationError` |
| [MessageDispatcher.cs](../../src/A2UI.Blazor/Services/MessageDispatcher.cs) | Handle `"error"` message type |
| [A2UIComponentBase.cs](../../src/A2UI.Blazor/Components/A2UIComponentBase.cs) | `GetValidationError()` helper |
| All 5 input components | Validation error fallback in error display |
| [SurfaceManagerTests.cs](../../tests/A2UI.Blazor.Tests/Services/SurfaceManagerTests.cs) | 4 new tests |
| [MessageDispatcherTests.cs](../../tests/A2UI.Blazor.Tests/Services/MessageDispatcherTests.cs) | 2 new tests |
| [TextFieldTests.cs](../../tests/A2UI.Blazor.Tests/Components/Input/TextFieldTests.cs) | 2 new tests |

---

## Test Summary

| Area | New Tests |
|------|-----------|
| SurfaceManager lifecycle events | 5 |
| LocalActionRegistry | 7 |
| Button functionCall integration | 2 |
| TextField optimistic updates | 2 |
| TextField validation errors | 2 |
| SurfaceManager validation errors | 4 |
| MessageDispatcher error handling | 2 |
| **Total** | **24** |

Final count: 273 unit tests (up from 249), 46 E2E tests (no regressions).

## Related Documentation

- [Error Handling + Logging](error-handling-logging.md) — v0.2.0 error architecture
- [State Machine Design](state-machine.md) — Custom component example
- [ROADMAP](../../ROADMAP.md) — Version milestones
