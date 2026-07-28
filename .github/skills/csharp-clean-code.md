# SKILL: C# Clean Code & Modern Practices (MyBusApp)

## When to Use
Use this skill when writing or refactoring C# code — models, services, DTOs, or configuration.

## Context — MyBusApp
- **Language:** C# 12 (.NET 9)
- **Project type:** Blazor WebAssembly (client-side only)
- **Nullability:** Enabled globally (`<Nullable>enable</Nullable>` in `.csproj`)
- **Implicit usings:** Enabled (`<ImplicitUsings>enable</ImplicitUsings>` in `.csproj`)
- **No Newtonsoft.Json** — use only `System.Text.Json`

## Objectives
- Write clean, idiomatic modern C# that is consistent across the project.
- Use .NET 9 features appropriately.
- Prevent common Blazor WASM pitfalls (threading, sync-over-async).

## Rules

### 1. Modern C# Features
- Use **file-scoped namespaces** everywhere (`namespace MyBusApp.Services;`).
- Use **primary constructors** for simple service classes.
- Use **`record` or `readonly record struct`** for DTOs and immutable domain models.
- Use **pattern matching** where it improves readability.
- Use `var` only when the type is obvious from the right-hand side.

### 2. Async Patterns
- All async methods must return `Task` or `ValueTask` and be suffixed with `Async`.
- **Never** use `.Result` or `.Wait()` on async Tasks — causes deadlocks in Blazor WASM.
- **Never** use `async void` (exception: UI event handlers).
- Use `Task.WhenAll()` for parallel independent API calls.

### 3. Null Safety
- Enable nullable reference types (`.csproj` already has `<Nullable>enable</Nullable>`).
- Use null-conditional operators: `?.`, `??`, `??=`.
- Handle potential null values explicitly — no `NullReferenceException`.
- Return empty collections (`[]` or `new()`) instead of `null` from methods returning lists.

### 4. DTO & Domain Patterns
- DTOs → use `record` with `[property: JsonPropertyName("...")]` for snake_case mapping.
- Domain models → use `record` with simple constructor params (see `BusModels.cs`).
- Mapping logic stays in **service classes**, not in DTOs or components.
- Prefer `JsonSerializerOptions` with `PropertyNameCaseInsensitive = true`.

### 5. Code Organization
- One class/record per file (unless closely related small types).
- Keep files under 300 lines; extract helper methods if longer.
- Use regions sparingly — prefer separate files.

### 6. What NEVER to do
- Never use `dynamic` or `ArrayList` unless absolutely necessary.
- Never suppress warnings with `#pragma warning disable` without a comment.
- Never use `Newtonsoft.Json` — stick to `System.Text.Json`.
- Never use `System.Data`, `System.Data.SqlClient`, or database libraries in Blazor WASM.

