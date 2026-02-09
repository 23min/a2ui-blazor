# Contributing to A2UI Blazor

Thanks for your interest in contributing! Here's how to get started.

## Getting Started

1. Fork the repository
2. Clone your fork and create a branch: `git checkout -b dev/your-feature`
3. Make your changes
4. Run the tests: `dotnet test`
5. Push and open a pull request against `main`

## Development Setup

### Prerequisites

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- [uv](https://docs.astral.sh/uv/) + Python 3.10+ (for the Python server sample)
- [Playwright browsers](https://playwright.dev/dotnet/docs/intro#installing-playwright): `pwsh tests/A2UI.Blazor.Playwright/bin/Debug/net8.0/playwright.ps1 install`

### Build and Test

```bash
dotnet build
dotnet test tests/A2UI.Blazor.Tests          # 115 bUnit component tests
dotnet test tests/A2UI.Blazor.Playwright     # 19 Playwright E2E tests
```

Playwright tests start their own servers on ports 15050/15200 and won't interfere with any manually-started dev servers.

## Branch Conventions

- `main` — protected, requires PR with review
- `dev/*` — feature and fix branches

## Pull Requests

- Keep PRs focused — one feature or fix per PR
- Include tests for new functionality
- Update the README or ROADMAP if your change affects the public API or project direction
- PRs are squash-merged; write a clear title and description

## What to Work On

Check the [Roadmap](ROADMAP.md) for planned work. Items in v0.2.0 and v0.3.0 are good candidates. Open an issue first to discuss your approach for larger changes.

## Code Style

- Follow existing patterns in the codebase
- No third-party dependencies in the core library (`A2UI.Blazor`)
- Prefix CSS classes with `a2ui-`
- XML doc comments on public APIs
