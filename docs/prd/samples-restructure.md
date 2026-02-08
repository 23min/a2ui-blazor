# PRD: Samples Restructuring Plan

## Problem

The current sample structure couples the Blazor WASM client to a single .NET server
that both serves A2UI streams and hosts the WASM files. This hides the core value
proposition: **A2UI.Blazor is a protocol-level client that works with any A2UI server,
on any hosting model.**

A developer evaluating this project today would reasonably assume:
- A2UI.Blazor requires A2UI.Blazor.Server on the backend
- It only works with Blazor WebAssembly (not Blazor Server)
- The client and server must be co-deployed

None of these are true, but the samples don't prove it.

## Goal

Restructure the `samples/` directory so that each sample is a standalone project
that demonstrates a single clear use case. Together, the samples prove:

1. **Protocol agnosticism** — The Blazor client renders A2UI from any server
2. **Hosting model flexibility** — Same library works in WASM and Server
3. **A2UI.Blazor.Server is optional** — A convenience, not a requirement

## Target Structure

```
src/
  A2UI.Blazor/                   ← The library (NuGet deliverable)
  A2UI.Blazor.Server/            ← .NET server helpers (NuGet deliverable)

samples/
  python-server/                 ← Python FastAPI A2UI server (~50 lines)
  dotnet-server/                 ← .NET Minimal API using A2UI.Blazor.Server
  blazor-wasm-spa/               ← Standalone Blazor WASM SPA
  blazor-server-app/             ← Blazor Server app (enterprise SSR model)
```

## Sample Descriptions

### python-server/

A minimal Python FastAPI server that streams A2UI messages over SSE. Mirrors
Google's own demo approach (their ADK uses FastAPI under the hood). Zero .NET
dependency. Serves a restaurant finder and component gallery.

**Purpose:** Proves the Blazor client is truly protocol-agnostic. If it renders
UI from a Python server, the agnosticism is undeniable.

**Files:** `server.py`, `requirements.txt` (~50 lines of Python total)

### dotnet-server/

A .NET Minimal API that uses `A2UI.Blazor.Server` — the fluent `SurfaceBuilder`,
`ComponentBuilder`, `A2UIStreamMiddleware`. Serves the same agents as the Python
server so you can swap backends and see identical rendering.

**Purpose:** Shows the .NET server-side story. Demonstrates that
`A2UI.Blazor.Server` is a well-designed convenience for .NET shops.

**Renamed from:** `A2UI.Blazor.Example.Server` (remove Blazor WASM hosting concern)

### blazor-wasm-spa/

A standalone Blazor WebAssembly SPA. Runs on its own. Configurable server URL
(can point at either the Python or .NET server). No hosted model — pure SPA.

**Purpose:** The canonical "how to use A2UI.Blazor" example. This is what a
developer copies when starting their own project.

**Renamed from:** `A2UI.Blazor.Example.Client` (decouple from server hosting)

### blazor-server-app/

A Blazor Server (SSR) application. References the same `A2UI.Blazor` library,
uses the same `<A2UISurface>` component, same CSS — but runs server-side with
SignalR. No WASM download.

**Purpose:** Enterprise story. Many .NET shops won't deploy WASM (security
policies, load times, IP exposure). This proves A2UI.Blazor works on both
Blazor hosting models with zero code changes to the component library.

## Migration Steps

1. Rename `Example.Server` → `dotnet-server`; remove WASM hosting (`UseBlazorFrameworkFiles`, client project reference)
2. Rename `Example.Client` → `blazor-wasm-spa`; add configurable base URL, self-contained
3. Create `python-server/` with FastAPI SSE server
4. Create `blazor-server-app/` referencing A2UI.Blazor
5. Update solution file
6. Update README and documentation

## CORS Consideration

When the SPA and server run on different origins, the servers must set CORS
headers. Both the Python and .NET servers will include permissive CORS for
local development (`Access-Control-Allow-Origin: *`).

## What This Enables

> "Install A2UI.Blazor. Drop `<A2UISurface>` into your app. Point it at any
> A2UI endpoint — Python, .NET, whatever. Works in Blazor WASM. Works in
> Blazor Server. Same library, same components."
