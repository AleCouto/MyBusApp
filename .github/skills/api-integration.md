# SKILL: API Integration (MyBusApp)

## When to Use
Use this skill when the work involves HTTP requests, remote services, API consumption, or data loading from external bus data endpoints.

## Context — MyBusApp
This application integrates **3 public transport APIs**:
1. **Carris Metropolitana** — `https://api.carrismetropolitana.pt/v2/` (lines, routes, stops, patterns, arrivals)
2. **Carris Lisboa** — `https://api.carris.pt/v2.7/` (routes, variants, stops, estimations)
3. **Transitland** — `https://api.transit.land/api/v2/` (routes, route_stop_patterns, stops, schedules)

Each API has its own:
- **Service class** implementing `IBusService` in `Services/`
- **DTO model** in `Models/DTOs/{Provider}/`
- **Domain model** mapping in `Models/Domain/BusModels.cs`

## Objectives
- Integrate APIs safely, predictably, and resiliently.
- Keep network logic centralized in typed service classes.
- Provide good UX during loading, error, and empty states.

## Rules

### 1. Service Architecture
- All API services **must implement** `IBusService` interface.
- Inject `ApiSettings` (from `Configuration/ApiSettings.cs`) for `BaseUrl`.
- Use typed `HttpClient` — created via `new HttpClient { BaseAddress = ... }` in the service constructor.
- Never instantiate `HttpClient` directly inside components or pages.
- Never expose API keys or secrets in Blazor WASM (they run on the browser).

### 2. DTOs and Domain Models
- Use `System.Text.Json` for all JSON serialization/deserialization.
- Use `record` types with `[property: JsonPropertyName(...)]` for DTOs.
- Define `JsonSerializerOptions` once with `PropertyNameCaseInsensitive = true`.
- **Map DTOs to domain models** (`BusLine`, `BusDirection`, `BusStop`, `BusArrival`) before returning from services.
- Never leak DTO types into UI components.

### 3. Error Handling
- Wrap all HTTP calls in `try-catch` handling `HttpRequestException` and general exceptions.
- Return empty lists / null instead of throwing exceptions to the UI.
- Log errors with `Console.WriteLine` (WASM-compatible).
- Never let API failures crash the application.

### 4. Loading & Empty States
- Show `spinner-border` or skeleton UI while awaiting API responses.
- Handle empty response payloads gracefully — return `[]` not null.
- Show meaningful "no results" messages to the user.

### 5. Caching (Important for MyBusApp)
- Cache reference data (lines, routes, stops) in-memory for the session.
- Use `_cached*` fields with null-coalescing assignment (`??=`) pattern.
- Example from `CarrisMetropolitanaService`:
  ```csharp
  _cachedLines ??= await GetLinesFromApiAsync();
  ```

## Recommended Approach
1. Identify which provider's API to use.
2. Add/update the service in `Services/` implementing `IBusService`.
3. Create/update DTOs in `Models/DTOs/{Provider}/`.
4. Map responses to domain models before returning.
5. Handle loading, success, and error states in the UI.

## Acceptance Criteria
- API access is centralized in typed services — no raw HTTP in components.
- Error handling is robust: failures shown gracefully, not crashes.
- The UI shows spinners during load and messages when no data.
- DTOs are never used directly in `.razor` files (only domain models).

