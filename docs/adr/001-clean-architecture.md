# ADR-001: Clean Architecture (Onion) for Backend

## Status
Accepted

## Context
We need a backend architecture that enforces separation of concerns, testability, and independence from frameworks/infrastructure.

## Decision
Use Clean Architecture (Onion Architecture) with 4 layers:
- **Domain**: Core entities, value objects, repository interfaces. Zero dependencies.
- **Application**: Use cases via CQRS (MediatR). Depends only on Domain.
- **Infrastructure**: EF Core, Firebase, external services. Implements Domain interfaces.
- **API**: ASP.NET Core presentation. Wires DI.

## Consequences
- Clear dependency rules enforced by project references and hooks
- Domain logic is fully testable without infrastructure
- New infrastructure providers can be swapped without domain changes
- More initial boilerplate vs. a simpler layered approach
