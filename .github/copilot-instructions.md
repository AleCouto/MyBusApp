# Copilot Instructions for MyBusApp

## Project Context
- This repository contains a Blazor WebAssembly application targeting .NET 9.
- The application is primarily client-side, so performance, responsiveness, and user experience are important.
- The project is expected to evolve into a bus-related application domain, but implementation details should remain clear, maintainable, and testable.

## General Principles
- Keep components small, focused, and reusable.
- Prefer dependency injection for services and HTTP clients.
- Keep business logic out of UI components whenever possible.
- Favor clear naming, simple structure, and explicit intent.
- Avoid unnecessary complexity or premature abstraction.
- Maintain compatibility with .NET 9 and Blazor WebAssembly patterns.

## Coding Conventions
- Use modern C# features supported by .NET 9.
- Keep nullable reference types enabled and handle nulls explicitly.
- Use async/await for I/O operations.
- Prefer typed models, DTOs, and services over ad-hoc structures.
- Handle errors gracefully and provide user-friendly feedback to the user.
- Keep methods concise, readable, and easy to test.

## Architecture Expectations
- Create or update pages and components in a way that is easy to reason about and test.
- Centralize API calls and remote data access in services.
- Keep state management predictable and avoid unnecessary global state.
- Prefer reusable UI primitives over duplicated markup.
- Keep UI logic minimal and delegate domain rules to services or domain classes.

## Testing Expectations
- Add or update unit tests when changing business logic.
- Add component tests where UI behavior is meaningful.
- Keep tests deterministic, isolated, and fast.
- Validate behavior rather than implementation details.
