# Skills Index — MyBusApp

This folder contains repository-specific skills for GitHub Copilot, Blackbox, and other coding agents working on the **MyBusApp** project.

MyBusApp is a **Blazor WebAssembly (.NET 9)** application that aggregates real-time bus information from 3 public transport APIs:
- **Carris Metropolitana**
- **Carris Lisboa**
- **Transitland**

## Available Skills

| Skill | File | When to Use |
|---|---|---|
| 🏗️ Blazor WASM Architecture | `blazor-wasm-architecture.md` | Creating/modifying pages, components, services, routing |
| 🌐 API Integration | `api-integration.md` | HTTP requests, API consumption, data loading from bus APIs |
| 📦 Blazor State Management | `blazor-state-management.md` | Client-side state, shared data between components |
| 🎨 Blazor UI & Styling | `blazor-ui-styling.md` | UI components, Bootstrap 5, responsive design |
| 🧹 C# Clean Code | `csharp-clean-code.md` | Writing/refactoring C# models, services, DTOs |
| 🧘 Minimalist Development | `minimalist-dev.md` | Default mindset — framework-first, no external libs |
| 🚌 Bus App Domain | `bus-app-domain.md` | Domain modeling for routes, stops, schedules |
| 🧪 Testing | `testing.md` | Unit tests, integration tests, component tests |

## How to Use These Skills
- Read `.github/copilot-instructions.md` first, then `minimalist-dev.md`.
- Load only the skill files relevant to the task; use multiple files only when the work crosses those concerns.
- The source code and configuration are authoritative. Update a skill when a deliberate change makes it stale.

`copilot-instructions.md` contains the global baseline and routing table. The files in this folder provide the detailed, repository-specific guidance.
