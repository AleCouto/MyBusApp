# Skill: Testing

## When to Use
Use this skill when adding or modifying unit tests, integration tests, or UI component tests.

## Objectives
- Improve confidence in the implementation.
- Prevent regressions during future changes.
- Keep the application reliable as it grows.

## Rules
- The no-new-library policy applies to production code. Test-only dependencies already declared in `MyBusApp.Tests` (xUnit, test SDK, and Coverlet) are permitted.
- Test behavior, not implementation details.
- Keep tests deterministic and isolated.
- Use unit tests for services and domain logic.
- Use component tests for meaningful UI interactions.
- Avoid brittle tests that depend on internal structure.

## Recommended Approach
1. Identify the behavior to verify.
2. Choose the smallest relevant test type.
3. Write clear assertions around expected outcomes.
4. Keep tests simple and maintainable.

## Acceptance Criteria
- Tests are reliable and readable.
- Critical behaviors are covered.
- Changes can be made with lower risk of regressions.
