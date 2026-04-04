---
description: "Use when editing Application layer code — MediatR commands, queries, handlers, DTOs, validators, and mapping logic."
applyTo: "backend/src/Convy.Application/**"
---
# Application Layer Guidelines

## CQRS Pattern

### Commands (write operations)
- Record type: `public record CreateItemCommand(...) : IRequest<Result<Guid>>;`
- One handler per command.
- Handler naming: `{Command}Handler` in the same namespace.
- Return `Result<T>` — never throw exceptions for expected failures.

### Queries (read operations)
- Record type: `public record GetListItemsQuery(...) : IRequest<Result<IReadOnlyList<ListItemDto>>>;`
- Queries MUST NOT mutate state.
- Can use read-optimized paths (raw SQL, read-only DbContext).

## Validators
- One `AbstractValidator<T>` per command.
- Naming: `{Command}Validator`.
- Validate input shape only — business rules live in Domain.
- Integrated via MediatR pipeline behavior.

## DTOs
- Immutable records in `DTOs/` folder.
- Naming: `{Entity}Dto`, `{Entity}SummaryDto`.
- Flat structure — avoid deep nesting.

## Mapping
- Map between Domain entities and DTOs in handlers.
- Use manual mapping or a thin mapping extension — no AutoMapper unless complexity demands it.

## Folder Structure
```
Application/
├── Common/
│   ├── Behaviors/        # MediatR pipeline behaviors
│   ├── Interfaces/       # Application-level interfaces
│   └── Models/           # Result, Error, PagedList
├── Features/
│   ├── Households/
│   │   ├── Commands/
│   │   ├── Queries/
│   │   └── DTOs/
│   ├── Lists/
│   └── Items/
```
