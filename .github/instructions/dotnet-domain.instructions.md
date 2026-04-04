---
description: "Use when editing Domain layer code — entities, value objects, domain events, domain exceptions, and repository interfaces."
applyTo: "backend/src/Convy.Domain/**"
---
# Domain Layer Guidelines

## Core Rule
**ZERO external NuGet dependencies.** Domain must be pure C# with no framework references.

## Entities
- Rich domain models with behavior, not anemic data bags.
- Private setters — mutate only through methods that enforce invariants.
- Guard clauses at construction and state transitions.
- Entity base class with `Id` (Guid) and equality by ID.

## Value Objects
- Immutable, equality by value.
- Use `record` or override `Equals`/`GetHashCode`.
- Example: `Quantity`, `ItemStatus`, `HouseholdRole`.

## Repository Interfaces
- Define in Domain: `IHouseholdRepository`, `IListRepository`, etc.
- Return domain entities, not DTOs.
- Keep interface methods focused — no generic `IRepository<T>` unless it truly fits.

## Domain Events
- Use for cross-aggregate side effects.
- Name as past tense: `ItemCompletedEvent`, `MemberJoinedEvent`.
- Raise from entity methods.

## Domain Exceptions
- `DomainException` base class for invariant violations.
- Specific exceptions: `ItemAlreadyCompletedException`, `HouseholdFullException`.
- Only for truly exceptional invariant violations, not for validation of input shape.
