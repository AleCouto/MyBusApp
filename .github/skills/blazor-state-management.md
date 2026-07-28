# SKILL: Blazor State Management (MyBusApp)

## When to Use
Use this skill when managing client-side state across components, sharing data between unrelated pages, or handling user session preferences.

## Context — MyBusApp
This is a client-side Blazor WASM app with **3 service providers** and real-time bus arrival data. State management is needed for:
- Current selected provider/lines/stops between pages
- Cached API responses (lines, routes, stops, patterns)
- User preferences (favourite lines, dark mode)

## Objectives
- Keep state predictable, testable, and memory-safe.
- Share data between unrelated components without cascading parameters.
- Prevent memory leaks from event subscriptions.

## Rules

### 1. State Container Pattern
- Use **scoped or singleton C# classes** as State Containers.
- Keep the container in a dedicated `State/` folder.
- Fire `Action` or `event Action` delegates when state changes:
  ```csharp
  public class BusSearchState
  {
      public BusProvider? SelectedProvider { get; set; }
      public BusLine? SelectedLine { get; set; }
      public event Action? OnChange;
      
      public void SelectLine(BusLine line)
      {
          SelectedLine = line;
          NotifyStateChanged();
      }
      private void NotifyStateChanged() => OnChange?.Invoke();
  }
  ```
- Register as scoped in `Program.cs`:
  ```csharp
  builder.Services.AddScoped<BusSearchState>();
  ```

### 2. Subscription & Disposal
- Components that subscribe to state events **must implement `IDisposable`**.
- Subscribe in `OnInitialized()` and **unsubscribe in `Dispose()`**:
  ```csharp
  @implements IDisposable
  
  protected override void OnInitialized()
  {
      BusSearchState.OnChange += StateHasChanged;
  }
  
  public void Dispose()
  {
      BusSearchState.OnChange -= StateHasChanged;
  }
  ```

### 3. Caching as State
- Service-level in-memory caching (e.g. `_cachedLines` in `CarrisMetropolitanaService`) is valid state.
- Cache reference data (lines, routes, stops) for the session lifetime.
- Never cache real-time arrival data — always fetch fresh.

### 4. Local Persistence
- Use `ILocalStorageService` or `SessionStorage` for user preferences across reloads.
- Do **not** persist large API response caches in local storage.

## What NEVER to do
- Never forget to unsubscribe from events in `Dispose()` — causes memory leaks.
- Never pass cascading parameters through more than 2 component levels.
- Never use `async void` for state change notifications.
- Never store live API data in static variables — use DI scoped containers.

