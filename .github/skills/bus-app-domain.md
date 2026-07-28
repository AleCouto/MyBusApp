# Skill: Bus App Domain

## When to Use
Use this skill when modeling features related to routes, stops, schedules, trips, passengers, or any other domain concept in the bus application.

## Objectives
- Define domain concepts clearly and consistently.
- Keep business rules explicit and maintainable.
- Avoid business logic leaking into UI components.

## Rules
- Use clear domain models for entities and concepts.
- Keep business rules in services or domain classes rather than in components.
- Separate domain models from transport/DTO models where needed.
- Prefer explicit names for domain concepts.
- Avoid hard-coded business rules scattered across the UI.

## Recommended Approach
1. Identify the real business concept to model.
2. Define a clear domain object or service.
3. Keep validation and rules centralized.
4. Use the domain model consistently across the application.

## Acceptance Criteria
- Domain concepts are clear and understandable.
- Business rules are centralized and coherent.
- The implementation is easier to evolve as requirements change.
