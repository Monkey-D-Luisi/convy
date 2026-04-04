# Backend — ASP.NET Core Guidelines

## Architecture: Clean Architecture (Onion) + CQRS

### Layer Dependencies (strict inward direction)

```
API → Infrastructure → Application → Domain
```

- **Domain** (`Convy.Domain`): Entities, Value Objects, Domain Events, Repository interfaces, Domain exceptions. **ZERO external NuGet dependencies.**
- **Application** (`Convy.Application`): Commands, Queries, Handlers (MediatR), DTOs, Validators (FluentValidation), Mapping, Application interfaces.
- **Infrastructure** (`Convy.Infrastructure`): EF Core DbContext & configurations, Repository implementations, SignalR hubs, Firebase Auth integration, External service clients.
- **API** (`Convy.API`): Controllers/Endpoints, Middleware, DI registration, Configuration, Health checks.

### NEVER:
- Reference `Infrastructure` from `Domain` or `Application`.
- Put business logic in `API` or `Infrastructure`.
- Use EF Core types (`DbContext`, `DbSet`) in `Domain` or `Application`.

## CQRS with MediatR

- **Commands** mutate state, return `Result<T>` or `Result`.
- **Queries** are read-only, return DTOs.
- One handler per command/query — no god handlers.
- Naming: `CreateItemCommand` → `CreateItemCommandHandler`.
- Validators: `CreateItemCommandValidator` using FluentValidation.

### Command Pattern

```csharp
public record CreateItemCommand(Guid ListId, string Title, string? Note, int? Quantity, string? Unit) : IRequest<Result<Guid>>;

public class CreateItemCommandHandler : IRequestHandler<CreateItemCommand, Result<Guid>>
{
    // Constructor injection, single responsibility
}
```

### Query Pattern

```csharp
public record GetListItemsQuery(Guid ListId) : IRequest<Result<IReadOnlyList<ListItemDto>>>;
```

## Entity Framework Core Conventions

- Fluent API configuration in `Infrastructure/Persistence/Configurations/` — one file per entity.
- No data annotations on Domain entities.
- Use `snake_case` naming convention for tables and columns.
- Always configure max lengths, required fields, and indexes explicitly.
- Migrations must be reversible — always implement `Down()`.

## Result Pattern

Use a Result type for all Application layer returns. No exceptions for expected business failures.

```csharp
public class Result<T>
{
    public bool IsSuccess { get; }
    public T? Value { get; }
    public Error? Error { get; }
}
```

## FluentValidation

- One validator per command.
- Validate at the MediatR pipeline level (via behavior).
- Don't duplicate validation in Domain entities — Domain guards invariants, Application validates input shape.

## SignalR Conventions

- Hub per bounded context if needed, not one giant hub.
- Hub methods: PascalCase verbs (`ItemCreated`, `ItemCompleted`).
- Clients receive DTOs, never domain entities.
- Group by household ID for real-time updates.

## API Conventions

- RESTful routing: `/api/v1/{resource}`.
- Use Minimal APIs or Controllers consistently (not mixed).
- Response envelope for errors; raw DTOs for success.
- Auth: Firebase JWT token validation middleware.
- All endpoints require authentication unless explicitly public.

## Error Handling

- Domain: guard clauses throwing `DomainException` for invariants.
- Application: `Result<T>` for expected failures.
- API: Global exception middleware maps exceptions to HTTP status codes.
- Never expose stack traces or internal details to clients.
