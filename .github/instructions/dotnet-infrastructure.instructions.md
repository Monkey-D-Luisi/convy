---
description: "Use when editing Infrastructure layer code — EF Core DbContext, entity configurations, repository implementations, SignalR hubs, and external service integrations."
applyTo: "backend/src/Convy.Infrastructure/**"
---
# Infrastructure Layer Guidelines

## Entity Framework Core

### DbContext
- Single `ConvyDbContext` for the MVP.
- Register entity configurations via `modelBuilder.ApplyConfigurationsFromAssembly()`.
- Use `snake_case` naming convention: `builder.ToTable("list_items")`.
- Always configure audit fields: `created_at`, `updated_at`.

### Entity Configurations
- One `IEntityTypeConfiguration<T>` per entity in `Persistence/Configurations/`.
- Use Fluent API exclusively — no data annotations.
- Configure: primary key, required fields, max lengths, indexes, relationships.

### Repositories
- Implement interfaces defined in `Convy.Domain`.
- Inject `ConvyDbContext` via constructor.
- Use `AsNoTracking()` for read-only queries.
- Deferred execution: return `IQueryable` only within the repo, materialize before returning.

## SignalR

### Hub Structure
- Hub in `Hubs/` folder.
- Group clients by household ID on connection.
- Broadcast changes to household group only — never to all connected clients.
- Hub methods receive and send DTOs, never domain entities.

### Real-Time Events
- `ItemCreated`, `ItemUpdated`, `ItemCompleted`, `ItemDeleted`
- `ListCreated`, `ListArchived`
- `MemberJoined`

## Firebase Auth Integration
- Validate Firebase JWT tokens via middleware.
- Extract `uid` from token claims.
- Map Firebase `uid` to internal `User.Id`.
- Never store Firebase credentials in code — use configuration/secrets.

## Migrations
- Naming: `YYYYMMDDHHMMSS_DescriptiveAction` (e.g., `20260404120000_AddListItemsTable`).
- Always implement `Down()` for rollback.
- Test both `Up()` and `Down()` before committing.
- No data manipulation in schema migrations — use separate seed scripts.
