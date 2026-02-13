# Changelog

All notable changes to this project are documented here. Format follows [Keep a Changelog](https://keepachangelog.com/). Versions follow [SemVer](https://semver.org/).

## [0.5.0-preview] — 2026-02-12

### Added
- **Surface lifecycle events** — `OnSurfaceCreated` / `OnSurfaceDeleted` events on `SurfaceManager`, with `EventCallback` parameters on `A2UISurface` for declarative binding
- **Local actions** (`functionCall`) — `LocalActionRegistry` service for client-side action execution without server round-trip; register handlers via `AddA2UIBlazor(configureLocalActions: ...)`
- **Optimistic updates** — input components update the local data model immediately on user interaction; server reconciles on next `updateDataModel`
- **Validation error rendering** — server `error` messages stored per data-model path; input components render inline validation with component-error-takes-precedence semantics
- NuGet package icon (128×128 PNG)
- Design doc: `docs/design/advanced-protocol-features.md`

### Changed
- `SurfaceManager` on `A2UIComponentBase` changed from `[CascadingParameter]` to `[Inject]` — correct pattern for a DI singleton and improves bUnit testability

### Fixed
- E2E test fixture: use port 5050 for Python server (matching default `appsettings.json`) because .NET 10 Blazor WASM does not load environment-specific config files at runtime
- Safe port kill guidance in `CLAUDE.md`: use `lsof -sTCP:LISTEN` to avoid killing remote sessions

## [0.4.0-preview] — 2026-02-11

### Added
- **Accessible forms** — `<label>` / `for` associations, `aria-describedby`, `aria-invalid`, `helperText` and `error` properties on all input components
- **Keyboard navigation** — `:focus-visible` rings on all interactive components
- **Semantic HTML** — `<ul>` / `<li>` in List, `<article>` for Card, `role="region"` on Surface, `role="status"` / `role="alert"` on error states
- **Dark mode** — `prefers-color-scheme` media query with automatic light/dark switching
- **CSS custom property theming** — `--a2ui-*` variables for consumer overrides
- **`prefers-reduced-motion`** — respect user motion preferences for transitions

## [0.3.0-preview] — 2026-02-10

### Added
- **A2A message envelope** — v0.9 `{version, action}` envelope, `A2UI-Client-Capabilities` header, ISO 8601 timestamps
- **Property name migration** — `usageHint` → `variant`, `distribution` → `justify`, `alignment` → `align`
- **Render buffering** — buffer until root component arrives; single flush event
- **`formatString` interpolation** — `${expression}` syntax in bound values
- **`sendDataModel` sync** — echo data model when `sendDataModel: true`; parse `catalogId` and `theme` from `createSurface`
- **Client error reporting** — send client-side errors to server via `error` message type

## [0.2.0-preview] — 2026-02-08

### Added
- **`A2UIStreamClient`** — promoted from samples into the core library
- **Connection resilience** — automatic reconnection with exponential backoff
- **Reconnecting UI** — visual overlay during stream recovery
- **Error handling** — structured error boundaries around component rendering, stream parsing, and action dispatch
- **Logging** — `ILogger<T>` integration throughout core services
- **NuGet packaging** — complete package metadata; CI produces `.nupkg` artifact
- **GitHub Actions CI** — build, test, pack on every push; publish on version tag
- **Live State Machine demo** — SVG custom component showing real-time agent state transitions

### Changed
- Migrated to .NET 10 (current LTS) with bUnit 2.x

## [0.1.0-preview] — 2026-02-05

### Added
- JSONL/SSE stream reader with cancellation support
- Message dispatcher (`createSurface`, `updateComponents`, `updateDataModel`, `deleteSurface`)
- Surface state management with change notification
- Data binding via JSON Pointer (RFC 6901)
- 17 standard components: Text, Image, Icon, Divider, Row, Column, Card, List, Tabs, Button, TextField, CheckBox, ChoicePicker, DateTimeInput, Slider, Video, AudioPlayer
- Dynamic component rendering via `A2UIComponentRenderer`
- Extensible component registry (`AddA2UIBlazor(registry => ...)`)
- Server-side library: fluent builders, stream writer, ASP.NET Core middleware
- Four working samples (Python server, .NET server, Blazor WASM, Blazor Server)
