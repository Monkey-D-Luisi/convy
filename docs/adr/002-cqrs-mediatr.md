# ADR-002: CQRS with MediatR

## Status
Accepted

## Context
We need a pattern to separate read and write operations, keep handlers focused, and enable cross-cutting concerns via pipeline behaviors.

## Decision
Use CQRS with MediatR:
- **Commands**: Write operations (`IRequest<Result<T>>`)
- **Queries**: Read operations (`IRequest<T>`)
- **Pipeline Behaviors**: Validation (FluentValidation), logging

## Consequences
- Each handler has a single responsibility
- Validation is automatic via pipeline
- Easy to add new cross-cutting concerns (caching, authorization)
- Slight overhead per operation vs. direct service calls
