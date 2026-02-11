# A2UI Specification Compliance

**Current Target:** [A2UI v0.9](https://github.com/google/A2UI/tree/main/specification/v0_9)
**Current State:** Partial — v0.9 message names, v0.9 property names for implemented components
**Implementation Version:** 0.2.0-preview
**Last Updated:** 2026-02-10

This document tracks A2UI Blazor's compliance with the official A2UI protocol specification. Full compliance with the target specification is the goal.

---

## Specification Versions

| Version | Status | Description |
|---------|--------|-------------|
| [v0.8](https://a2ui.org/specification/v0.8-a2ui/) | Published | Public Preview — original release |
| [v0.9](https://github.com/google/A2UI/tree/main/specification/v0_9) | Published | Major overhaul — "Prompt First" redesign |
| [v0.10](https://github.com/google/A2UI/tree/main/specification/v0_10) | In Development | Adds Custom Functions & Extensions (evolution guide is TBD) |

### v0.8 → v0.9: Breaking Changes

v0.9 was a fundamental overhaul from "Structured Output First" to "Prompt First" (optimized for LLM system prompt embedding). Key breaking changes:

- **Message renames:** `beginRendering` → `createSurface`, `surfaceUpdate` → `updateComponents`, `dataModelUpdate` → `updateDataModel`
- **Property renames:** `usageHint` → `variant`, `distribution` → `justify`, `alignment` → `align`, `minValue/maxValue` → `min/max`
- **Component renames:** `MultipleChoice` → `ChoicePicker`
- **Schema split:** Single schema → `common_types.json`, `server_to_client.json`, `standard_catalog.json`
- **Data model sync:** New `sendDataModel` flag on `createSurface`
- **Root component:** Explicit attribute → implicit "component with ID `root`"

### v0.9 → v0.10: Additive Changes

v0.10 evolution guide is still TBD, but new documents include:

- **Custom Functions:** Developer-defined catalog extensions with JSON Schema validation
- **Extension Specification:** Framework for extending the standard catalog

---

## Protocol Messages (Server → Client)

| Message | Ours | v0.8 Name | v0.9 Name | v0.10 | Notes |
|---------|------|-----------|-----------|-------|-------|
| Create surface | ✅ | `beginRendering` | `createSurface` | same | We use v0.9 name. v0.9 adds `catalogId`, `theme` |
| Update components | ✅ | `surfaceUpdate` | `updateComponents` | same | We use v0.9 name |
| Update data model | ✅ | `dataModelUpdate` | `updateDataModel` | same | We use v0.9 name. v0.9 changed from array-of-pairs to JSON object |
| Delete surface | ✅ | `deleteSurface` | `deleteSurface` | same | Unchanged across versions |
| Render buffering | ✅ | Required | Required | TBD | Buffer until root component arrives; single flush event |

**Gaps:** We do not implement `catalogId` or `theme` on `createSurface`.

---

## Client-to-Server Communication

| Feature | Ours | v0.8 | v0.9 | v0.10 | Notes |
|---------|------|------|------|-------|-------|
| `userAction` payload | ✅ | ✅ | ✅ | same | v0.9 envelope: `{version, action}` per `client_to_server.json` |
| v0.9 message envelope | ✅ | N/A | ✅ | same | `{version: "v0.9", action: {...}}` with ISO 8601 timestamp |
| `a2uiClientCapabilities` | ✅ | N/A | ✅ | same | Sent via `A2UI-Client-Capabilities` HTTP header |
| `sendDataModel` sync | ❌ | N/A | ❌ | TBD | **Gap** — v0.9 requires sending data model back with actions |
| `error` message type | ❌ | ❌ | ❌ | TBD | **Gap** — client→server error reporting |
| Custom functions | N/A | N/A | N/A | 📋 | v0.10 feature — developer-defined extensions |

**Client-to-server message format** (v0.9 compliant):

```json
{
  "version": "v0.9",
  "action": {
    "name": "submit",
    "surfaceId": "main",
    "sourceComponentId": "submit-btn",
    "timestamp": "2026-02-10T12:00:00.000+00:00",
    "context": {}
  }
}
```

Client capabilities are sent via HTTP header: `A2UI-Client-Capabilities: {"v0.9":{"supportedCatalogIds":[...]}}`

---

## Component Catalog

Standard components from the A2UI catalog. Property names differ between spec versions.

### Display Components

| Component | Ours | v0.8 | v0.9 | v0.10 | Property Gaps |
|-----------|------|------|------|-------|---------------|
| `Text` | ✅ | ✅ | ✅ | same | v0.9 compliant (`variant`) |
| `Image` | ✅ | ✅ | ✅ | same | |
| `Icon` | ✅ | ✅ | ✅ | same | |
| `Divider` | ✅ | ✅ | ✅ | same | |

### Layout Components

| Component | Ours | v0.8 | v0.9 | v0.10 | Property Gaps |
|-----------|------|------|------|-------|---------------|
| `Row` | ✅ | ✅ | ✅ | same | v0.9 compliant (`justify`/`align`) |
| `Column` | ✅ | ✅ | ✅ | same | v0.9 compliant (`align`) |
| `Card` | ✅ | ✅ | ✅ | same | |
| `List` | ✅ | ✅ | ✅ | same | |
| `Tabs` | ✅ | ✅ | ✅ | same | |
| `Modal` | ⚠️ | ✅ | ⚠️ | same | **Gap:** We use `entryPointChild`/`contentChild` (v0.8), v0.9 requires `trigger`/`content` |

### Input Components

| Component | Ours | v0.8 | v0.9 | v0.10 | Property Gaps |
|-----------|------|------|------|-------|---------------|
| `Button` | ✅ | ✅ | ✅ | same | v0.9 compliant (`variant`) |
| `TextField` | ⚠️ | ✅ | ⚠️ | same | **Gap:** v0.9 requires `value` (we use `text`), `variant` (we use `textFieldType`) |
| `CheckBox` | ✅ | ✅ | ✅ | same | |
| `ChoicePicker` | ⚠️ | ✅ | ⚠️ | same | **Gap:** v0.9 requires `value` (we use `selections`) |
| `DateTimeInput` | ✅ | ✅ | ✅ | same | |
| `Slider` | ✅ | ✅ | ✅ | same | v0.9 compliant (`min`/`max`) |

### Media Components

| Component | Ours | v0.8 | v0.9 | v0.10 | Property Gaps |
|-----------|------|------|------|-------|---------------|
| `Video` | ✅ | ✅ | ✅ | same | |
| `AudioPlayer` | ✅ | ✅ | ✅ | same | |

**Total:** 17/17 standard components implemented
**v0.9 property compliance:** 13/17 compliant, 4 components have v0.8 property names or gaps (Modal, TextField, ChoicePicker)

---

## Property Name Migration (v0.8 → v0.9)

This table tracks which property names we use vs what each spec version expects.

| Component | Property | Our Value | v0.9 Required | Status |
|-----------|----------|-----------|---------------|--------|
| `Text` | variant hint | `variant` | `variant` | ✅ Compliant |
| `Row`/`Column` | horizontal distribution | `justify` | `justify` | ✅ Compliant |
| `Row`/`Column` | cross-axis alignment | `align` | `align` | ✅ Compliant |
| `Button` | style variant | `variant` | `variant` | ✅ Compliant |
| `Slider` | range bounds | `min`/`max` | `min`/`max` | ✅ Compliant |
| `Tabs` | tab list | `tabs` | `tabs` | ✅ Compliant |
| `Modal` | child slots | `entryPointChild`/`contentChild` | `trigger`/`content` | ❌ Migrate |
| `TextField` | text value | `text` | `value` | ❌ Migrate |
| `TextField` | field type | `textFieldType` | `variant` | ❌ Migrate |
| `ChoicePicker` | selections | `selections` | `value` | ❌ Migrate |

---

## Data Binding

| Feature | Ours | v0.8 | v0.9 | v0.10 | Notes |
|---------|------|------|------|-------|-------|
| Literal values | ✅ | ✅ | ✅ | same | |
| Path-based binding | ✅ | ✅ | ✅ | same | JSON Pointer (RFC 6901) |
| Combined literal + path | ✅ | ✅ | ✅ | same | |
| List iteration | ✅ | ✅ | ✅ | same | |
| `formatString` interpolation | ✅ | N/A | ✅ | same | `${/path}` and `${relativePath}` expressions; type coercion per spec |

---

## Protocol Features

| Feature | Ours | v0.8 | v0.9 | v0.10 | Notes |
|---------|------|------|------|-------|-------|
| JSONL stream parsing | ✅ | ✅ | ✅ | same | |
| SSE transport | ✅ | ✅ | ✅ | same | |
| Surface state management | ✅ | ✅ | ✅ | same | |
| Component tree rendering | ✅ | ✅ | ✅ | same | |
| Dynamic component registry | ✅ | ✅ | ✅ | same | |
| Action context resolution | ✅ | ✅ | ✅ | same | |
| Catalog ID in `createSurface` | ❌ | N/A | ❌ | same | **Gap** — v0.9 requires parsing `catalogId` |
| Theme support | ❌ | N/A | ❌ | same | **Gap** — v0.9 requires `primaryColor` etc. |
| Custom functions | N/A | N/A | N/A | 📋 | v0.10 feature |
| Extension catalogs | N/A | N/A | N/A | 📋 | v0.10 feature |

---

## Known Gaps & Roadmap

### High Priority (Blocking spec compliance)

1. ~~**A2A Message Envelope**~~ ✅ Done
   - v0.9 envelope format: `{version, action}` per `client_to_server.json`
   - Client capabilities sent via `A2UI-Client-Capabilities` HTTP header
   - ISO 8601 timestamps, required `context` field

2. **Property Name Migration to v0.9** (⚠️ 4 components remaining)
   - ✅ Done: `usageHint` → `variant`, `distribution` → `justify`, `alignment` → `align`
   - Remaining gaps (not renames — features not yet implemented):
     - Modal: `entryPointChild`/`contentChild` → `trigger`/`content`
     - TextField: `textFieldType` → `variant`
     - ChoicePicker: `selections` → `value`

3. ~~**Render Buffering**~~ ✅ Done
   - Buffer messages until root component arrives in `updateComponents`
   - Single `OnSurfaceChanged` event fires when root arrives (flush)
   - Subsequent updates fire immediately (progressive rendering)

### Medium Priority (v0.9 gaps)

4. ~~**`formatString` Interpolation**~~ ✅ Done
   - `${/absolute/path}` and `${relativePath}` expressions in `formatString` FunctionCall
   - Type coercion: null → `""`, numbers/bools → string, objects/arrays → JSON
   - Escape support: `\${` → literal `${`

5. **`sendDataModel` Sync** (❌ v0.9 gap)
   - Echo data model in client→server messages when `sendDataModel: true`

6. **Catalog & Theme Support** (❌ v0.9 gap)
   - Parse `catalogId` from `createSurface`
   - Parse and apply `theme` properties

### Low Priority (v0.10 / Future)

7. **Custom Functions** (📋 v0.10 feature)
   - Developer-defined catalog extensions
   - JSON Schema validation for function calls

8. **Extension Specification** (📋 v0.10 feature)
   - Framework for extending the standard catalog

9. **Error Reporting to Server** (❌ v0.9 gap)
   - Send client-side errors to server via `error` message type

---

## Testing Against Reference Implementations

| Test | Status | Notes |
|------|--------|-------|
| Python sample server | ✅ | All 4 demos working |
| .NET sample server | ✅ | All 4 demos working |
| Official A2UI reference agents | ❓ | Not tested |
| v0.9 strict-mode server | ❓ | Not tested — remaining gaps (Modal, TextField, ChoicePicker) may break |

---

## Contributing

When implementing new protocol features:

1. Update this document first (mark as 📋 Planned)
2. Implement the feature
3. Add tests covering the spec requirements
4. Update status to ✅ Implemented
5. Document any deviations in the "Notes" column

---

## References

- [A2UI v0.8 Specification](https://a2ui.org/specification/v0.8-a2ui/)
- [A2UI v0.9 Specification](https://github.com/google/A2UI/tree/main/specification/v0_9)
- [A2UI v0.10 Specification](https://github.com/google/A2UI/tree/main/specification/v0_10) (in development)
- [A2UI v0.9 Evolution Guide](https://github.com/google/A2UI/blob/main/specification/v0_9/docs/evolution_guide.md)
- [A2UI GitHub Repository](https://github.com/google/A2UI)
- [Renderer Development Guide](https://a2ui.org/guides/renderer-development/)

---

**Legend:**
| Symbol | Meaning |
|--------|---------|
| ✅ | Implemented and tested |
| ⚠️ | Partially implemented (property name mismatch) |
| ❌ | Not implemented |
| 🚨 | Critical gap (required by spec) |
| 📋 | Planned or not yet required |
| ❓ | Unknown or untested |
| N/A | Not applicable for this version |
