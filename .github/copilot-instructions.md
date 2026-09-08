# MyBusApp — Agent Baseline

## Project Context
- This repository contains a Blazor WebAssembly application targeting .NET 9.
- The application is primarily client-side, so performance, responsiveness, and user experience are important.
- The project is a bus-information application; keep implementation clear, maintainable, and testable.

## Working Rules
- Treat the source code and project configuration as the source of truth.
- Keep changes scoped to the request; do not introduce dependencies or abstractions without a demonstrated need.
- Consult `minimalist-dev.md` for every implementation. Load only the additional skill(s) that match the task.

## Skill Routing

| Work | Read |
|---|---|
| Pages, components, routing, DI | `skills/blazor-wasm-architecture.md` |
| Remote data, DTOs, caching | `skills/api-integration.md` |
| Shared client state | `skills/blazor-state-management.md` |
| Razor, CSS, accessibility | `skills/blazor-ui-styling.md` |
| C# models, services, DTOs | `skills/csharp-clean-code.md` |
| Routes, stops, schedules | `skills/bus-app-domain.md` |
| Automated tests | `skills/testing.md` |

If an intentional change makes a skill inaccurate, update that skill in the same change.
