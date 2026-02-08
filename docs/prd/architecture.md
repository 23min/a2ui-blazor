# PRD: A2UI Blazor — System Architecture

## Overview

A2UI Blazor is a .NET implementation of the A2UI (Agent-to-UI) protocol that enables
Blazor WebAssembly applications to render rich, interactive UIs driven by AI agents.
The system consists of four projects that together provide a complete end-to-end
solution for agent-driven UI rendering in the .NET ecosystem.

## Problem Statement

.NET developers building agentic applications lack a native way to render A2UI
protocol messages. Existing renderers target Lit (Web Components), Angular, and
Flutter. Enterprise .NET shops need a first-class Blazor WebAssembly renderer that
integrates with the C# type system and Blazor's component model.

## Solution Architecture

```
┌─────────────────────────────────────────────────────────┐
│                    Agent / LLM                          │
│              (generates A2UI JSON)                      │
└──────────────────────┬──────────────────────────────────┘
                       │ A2UI JSONL Stream
                       ▼
┌─────────────────────────────────────────────────────────┐
│              A2UI.Blazor.Server                         │
│  ┌─────────────────┐  ┌──────────────────────────────┐  │
│  │ SurfaceBuilder  │  │  A2UIStreamMiddleware         │  │
│  │ (Fluent API)    │  │  (SSE/JSONL endpoint)        │  │
│  └────────┬────────┘  └──────────────┬───────────────┘  │
│           │                          │                  │
│  ┌────────▼──────────────────────────▼───────────────┐  │
│  │            A2UIStreamWriter                        │  │
│  │    (serializes messages to JSONL stream)           │  │
│  └───────────────────────────────────────────────────┘  │
└──────────────────────┬──────────────────────────────────┘
                       │ HTTP SSE / JSONL
                       ▼
┌─────────────────────────────────────────────────────────┐
│                  A2UI.Blazor                            │
│  ┌─────────────────┐  ┌──────────────────────────────┐  │
│  │ JsonlStreamReader│  │  MessageDispatcher           │  │
│  │ (parses stream)  │  │  (routes by message type)    │  │
│  └────────┬─────────┘  └──────────────┬──────────────┘  │
│           │                           │                 │
│  ┌────────▼───────────────────────────▼──────────────┐  │
│  │              SurfaceManager                        │  │
│  │  (maintains surfaces, components, data model)      │  │
│  └────────┬───────────────────────────┬──────────────┘  │
│           │                           │                 │
│  ┌────────▼──────────┐  ┌─────────────▼──────────────┐  │
│  │ ComponentRegistry │  │  DataBindingResolver        │  │
│  │ (type → component)│  │  (JSON Pointer → value)     │  │
│  └────────┬──────────┘  └─────────────┬──────────────┘  │
│           │                           │                 │
│  ┌────────▼───────────────────────────▼──────────────┐  │
│  │         Blazor Component Catalog                   │  │
│  │  Text | Image | Button | TextField | Row | ...     │  │
│  │         (native .razor components)                 │  │
│  └───────────────────────────────────────────────────┘  │
└─────────────────────────────────────────────────────────┘
```

## Projects

### 1. A2UI.Blazor (Core Renderer Library)

**Purpose**: The primary NuGet package that Blazor WASM applications reference to
render A2UI protocol messages.

**Responsibilities**:
- Parse JSONL streams from any HTTP endpoint
- Maintain surface state (components + data model)
- Resolve data bindings using JSON Pointer (RFC 6901)
- Render A2UI components as native Blazor components
- Dispatch user actions back to the server
- Provide an extensible component registry

**Key Types**:
- `JsonlStreamReader` — reads HTTP stream line-by-line
- `MessageDispatcher` — routes messages to handlers
- `SurfaceManager` — manages surface lifecycle and state
- `DataBindingResolver` — resolves JSON Pointer paths against data models
- `ComponentRegistry` — maps A2UI type strings to Blazor component types
- `A2UISurface.razor` — top-level Blazor component that renders a surface
- `A2UIComponentRenderer.razor` — dynamic component resolver

### 2. A2UI.Blazor.Server (Server Provider Library)

**Purpose**: NuGet package for ASP.NET Core applications that need to serve A2UI
streams to clients.

**Responsibilities**:
- Provide a fluent API for building A2UI messages programmatically
- Serialize messages to JSONL format
- Provide ASP.NET Core middleware for streaming responses
- Define base classes for agent implementations

**Key Types**:
- `SurfaceBuilder` — fluent API for constructing surfaces
- `ComponentBuilder` — fluent API for building components
- `A2UIStreamWriter` — writes A2UI messages to a stream
- `A2UIStreamMiddleware` — ASP.NET Core middleware
- `IA2UIAgent` — interface for agent implementations

### 3. A2UI.Blazor.Example.Server (Example API)

**Purpose**: A working ASP.NET Core application that demonstrates how to build
A2UI agents and serve streams. Hosts example agents and also serves the Blazor
WASM client.

**Key Features**:
- Restaurant finder agent (mirrors the official A2UI quickstart)
- Contact lookup agent
- Component gallery agent (renders all standard components)
- Hosted Blazor WASM client

### 4. A2UI.Blazor.Example.Client (Example Blazor WASM App)

**Purpose**: A working Blazor WebAssembly application that demonstrates consuming
A2UI streams and rendering them.

**Key Features**:
- Connects to the example server's A2UI endpoints
- Renders agent-driven UIs in real-time
- Demonstrates all standard components
- Shows custom component registration

## Protocol Compliance

Targets A2UI specification v0.9 with the following message types:
- `createSurface` — initialize a new surface
- `updateComponents` — add/update components in a surface
- `updateDataModel` — modify surface data model
- `deleteSurface` — remove a surface

## Standard Component Catalog

| Category | Components |
|----------|-----------|
| Display | Text, Image, Icon, Divider |
| Layout | Row, Column, Card, List, Tabs |
| Input | Button, TextField, CheckBox, ChoicePicker, DateTimeInput, Slider |
| Media | Video, AudioPlayer |

## Technology Stack

- .NET 8 (LTS)
- Blazor WebAssembly
- System.Text.Json
- ASP.NET Core (server)

## Non-Goals (v1)

- Server-side Blazor rendering (future consideration)
- A2A protocol transport integration (future)
- AG-UI protocol transport integration (future)
- NuGet package publishing automation
