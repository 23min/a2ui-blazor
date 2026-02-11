# CLAUDE.md

## Project

A2UI Blazor — an A2UI protocol renderer for Blazor (WebAssembly and Server). Implements the [A2UI v0.9 specification](https://github.com/google/A2UI/tree/main/specification/v0_9).

## Quick Reference

```bash
dotnet build                    # Build entire solution
dotnet test                     # Run all tests (must pass before committing)
dotnet build samples/dotnet-server   # Build .NET server only
dotnet build samples/blazor-wasm-spa # Build WASM client only
```

## Solution Structure

```
src/A2UI.Blazor            # Core library — renderer, components, services
src/A2UI.Blazor.Server     # Server-side helpers for ASP.NET Core agents
samples/blazor-wasm-spa    # Blazor WebAssembly client sample
samples/blazor-server-app  # Blazor Server client sample
samples/dotnet-server      # .NET A2UI agent server sample
samples/python-server      # Python A2UI agent server sample
tests/A2UI.Blazor.Tests    # Unit + component tests (xUnit + bUnit)
tests/A2UI.Blazor.Playwright  # E2E tests (Playwright + NUnit)
```

## Rules

### Development Approach: Red-Green-Refactor

Follow the RED-GREEN-REFACTOR cycle for all code changes:

1. **RED** — Write a failing test that defines the expected behavior
2. **GREEN** — Write the minimum code to make the test pass
3. **REFACTOR** — Clean up the implementation while keeping tests green

Do not skip the red step. Tests come first, implementation follows.

### Specification Compliance

- **SPECIFICATION.md** is the source of truth for protocol compliance
- Target spec is **A2UI v0.9** — full compliance is the goal
- If the spec requires it and we don't have it, it is a **gap** — not "planned", not "nice-to-have"
- No backward compatibility with v0.8 — use v0.9 property names and message structures
- Update SPECIFICATION.md when implementing or discovering protocol features

### Server Parity

The .NET server (`samples/dotnet-server`) and Python server (`samples/python-server`) must always have **identical features**. Every agent endpoint available in one must exist in the other.

### Sample App Parity

The Blazor WASM SPA (`samples/blazor-wasm-spa`) and Blazor Server App (`samples/blazor-server-app`) must have the **same pages and navigation links**. If a demo page is added to one, add it to the other.

### Testing

- All tests must pass before committing (`dotnet test`)
- New functionality requires new tests (red-green-refactor)
- Unit tests use **xUnit** + **bUnit** (for Blazor components)
- Use `NullLogger<T>.Instance` when constructing services in tests
- bUnit tests use `RenderComponent<T>()` API, not Razor syntax in `.cs` files

### Build Configuration

- .NET 10 (current LTS) is the target framework
- `TreatWarningsAsErrors: true` — zero warnings policy
- C# 14, nullable enabled, implicit usings enabled

### Branching

- `main` is the primary branch
- Code changes go through feature branches (`feature/*`) or dev branches (`dev/*`) with PRs to main
- Doc-only changes (ROADMAP, SPECIFICATION, README) can go directly to `main`

### Commits

- Commit messages should summarize the "why", not the "what"
- Run `dotnet test` before committing
- Do not commit unless explicitly asked

### Killing Ports

When asked to kill ports or fix "address already in use" errors, use `lsof -i :<port>` to find the process, then `kill <pid>`. Sample app ports: dotnet-server=5050, blazor-server-app=5100, blazor-wasm-spa=5200.

### Documentation

- `SPECIFICATION.md` — A2UI protocol compliance matrix (root)
- `ROADMAP.md` — version milestones and progress (root)
- `docs/design/` — feature design documents
- `docs/prd/` — architecture and product requirements
- Do not create documentation files unless explicitly asked
