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
- Use these files as guidance when implementing features, reviewing code, or creating new components.
- Always start with **`minimalist-dev.md`** as the default mindset.
- Consult the most relevant skill for the specific task at hand.
- When a task touches multiple areas (e.g., adding a new API page), consult all relevant skills.

## Required Reading Order (for new agents)
1. `minimalist-dev.md` — Default development philosophy
2. `blazor-wasm-architecture.md` — Project structure and patterns
3. `api-integration.md` — How we consume APIs
4. `blazor-state-management.md` — State patterns
5. `blazor-ui-styling.md` — UI conventions
6. `csharp-clean-code.md` — Code style
7. `bus-app-domain.md` — Domain concepts
8. `testing.md` — Testing approach

> **Note:** `.github/copilot-instructions.md` (if present) contains the global baseline. Skills in this folder are repository-specific and override the baseline for Blazor/bus-app concerns.

