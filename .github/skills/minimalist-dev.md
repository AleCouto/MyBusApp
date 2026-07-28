# SKILL: Minimalist & Framework-First Development (MyBusApp)

## When to Use
Use this skill as the **default mindset** for all development work. This skill overrides any suggestion to add external libraries or change architecture unnecessarily.

## Context — MyBusApp
- **Only NuGet dependencies:** `Microsoft.AspNetCore.Components.WebAssembly` + `Microsoft.AspNetCore.Components.WebAssembly.DevServer`
- **No external libraries:** No AutoMapper, MediatR, Fluxor, MudBlazor, Radzen, or any third-party package
- **All features** are implemented with native .NET 9 / Blazor WASM / Bootstrap 5

## Core Principles

### Principle 1: Framework-First, No External Libraries
- Always use **native .NET 9** and **Blazor WebAssembly** capabilities first.
- Use **standard C# logic** before considering any abstraction.
- Use **Bootstrap 5 utility classes** before custom CSS.
- Do NOT introduce external NuGet packages unless explicitly requested or absolutely unavoidable.
- Never suggest AutoMapper — use manual mapping in service classes.
- Never suggest MediatR — use direct DI service calls.
- Never suggest Fluxor — use plain State Containers with `event Action`.

### Principle 2: Surgical Code Modification
- When fixing bugs or adding features, alter **only the essential code** required.
- Preserve existing code structure, variable naming, formatting, and architecture.
- Do **NOT** rewrite working code, refactor adjacent methods, or redesign patterns.
- Keep changes **as small and precise as possible**.

### Principle 3: No Premature Abstractions
- Don't create interfaces, factories, or base classes unless there are at least 2 concrete implementations.
- Don't abstract what you don't yet understand — wait for the pattern to emerge.
- Simple `switch` or `if/else` is fine for 3-4 cases (like provider routing).

### Principle 4: Configuration over Hardcoding
- API URLs go in `wwwroot/appsettings.json`, not in code.
- Feature flags go in configuration, not in conditional compilation.
- Use `ApiSettings` for all endpoint configuration.

## What NEVER to do
- Never add NuGet packages that duplicate built-in .NET functionality.
- Never refactor unaffected methods or rewrite whole files.
- Never remove existing logic or comments unless they directly cause a bug.
- Never add third-party UI suites (MudBlazor, Radzen, Syncfusion, Telerik, AntDesign).
- Never introduce JavaScript interop for things achievable in Blazor C#.

## Decision Checklist
Before adding any new dependency or abstraction, ask:
1. Can I do this with native .NET 9? → **YES: Do it**
2. Can I do this with Bootstrap 5 classes? → **YES: Do it**
3. Can I do this with a simple helper method? → **YES: Do it**
4. Is there an existing pattern in MyBusApp I can follow? → **YES: Follow it**
5. Is the answer to all above "no"? → Then and ONLY then consider a library.

