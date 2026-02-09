# Security Policy

## Supported Versions

| Version | Supported |
|---------|-----------|
| 0.x (latest pre-release) | Yes |

## Reporting a Vulnerability

If you discover a security vulnerability, please report it responsibly:

1. **Do not** open a public issue.
2. Email the maintainers or use [GitHub's private vulnerability reporting](https://github.com/23min/a2ui-blazor/security/advisories/new).
3. Include a description of the vulnerability, steps to reproduce, and potential impact.

We will acknowledge receipt within 48 hours and aim to release a fix within 7 days for critical issues.

## Security Model

A2UI Blazor follows the A2UI protocol's security model:

- **No executable code from agents** — agents send declarative component trees, never JavaScript or executable code.
- **Pre-approved component catalog** — only registered component types are rendered. Unknown types are ignored.
- **Data binding is read-only** — components read from the data model but cannot modify it directly.
- **Actions are explicit** — user interactions are sent to the server as structured action objects, not arbitrary data.
