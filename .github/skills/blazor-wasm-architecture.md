# SKILL: Blazor WebAssembly Architecture (MyBusApp)

## When to Use
Use this skill when creating or modifying pages, components, services, routing, or UI flows in this Blazor WebAssembly (.NET 9) project.

## Context — MyBusApp Architecture
- **Framework:** Blazor WebAssembly standalone (`.NET 9`, `net9.0`)
- **Entry:** `Program.cs` — uses `WebAssemblyHostBuilder`
- **Configuration:** `wwwroot/appsettings.json` — loaded via `builder.Configuration.AddJsonFile`
- **Dependency injection:** `ApiSettings` registered as singleton; services as scoped
- **Multiple providers pattern:** `IBusService` registered 3 times with different implementations
- **No external JS or NuGet packages** beyond the Blazor WASM SDK

## Objectives
- Build maintainable and reusable UI components.
- Keep business logic out of markup and component code.
- Follow Blazor and .NET 9 best practices.

## Rules

### 1. Component Structure
- Prefer small, focused components over large monolithic ones.
- Use **file-scoped namespaces** (`namespace X.Y;`) in all `.razor` and `.cs` files.
- Keep C# logic inside the `@code { }` block at **the end** of the Razor file.
- Never mix database/API calls directly in component code — use injected services.

### 2. Dependency Injection
- Service injection must be via the `@inject` directive in `.razor` files.
- In `Program.cs`, use `builder.Services.AddScoped<TInterface, TImplementation>()`.
- Multiple implementations of `IBusService` are registered — use `IEnumerable<IBusService>` for injection.
- Never use constructor injection in Blazor components.

### 3. State & Data Flow
- Use **State Containers** for state shared across unrelated components.
- Subscribed components must implement `IDisposable` and unsubscribe.
- Call `StateHasChanged()` only on the UI thread.
- Use `CascadingParameter` sparingly (max 2 levels deep).
- **Never** use `async void` except for UI event handlers.

### 4. Routing & Navigation
- Define routes with `@page "/route"` directive.
- Use `NavigationManager` for programmatic navigation.
- Use `FocusOnNavigate` in `App.razor` for accessibility.

### 5. Configuration
- Add new API endpoints in `wwwroot/appsettings.json` under the `"Apis"` section.
- Access via `ApiSettings` singleton — do not hardcode URLs.
- Do **not** expose API secrets or connection strings (WASM runs in the browser).

## Recommended Approach
1. Understand the requirement and identify affected files (page, component, service).
2. Decide if the change is UI, logic, or data access.
3. Implement the smallest change that satisfies the requirement.
4. Keep code easy to test and maintain.

## Acceptance Criteria
- Components are readable, focused, and reusable.
- Business logic is in services, not in UI markup.
- Multiple `IBusService` implementations work transparently via DI.
- The app follows Blazor WASM conventions and .NET 9 best practices.

