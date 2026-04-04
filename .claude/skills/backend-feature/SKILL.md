---
name: backend-feature
description: "End-to-end workflow for implementing a backend feature in Convy. Use when adding new functionality that spans Domain, Application, Infrastructure, and API layers. Covers entity creation, CQRS handlers, validators, repository implementation, API endpoints, and tests."
---
# Backend Feature Workflow

## When to Use
- Adding a new backend feature (e.g., create household, add list item, invite member)
- Implementing a user story that requires changes across multiple Clean Architecture layers

## Prerequisites
- Read `docs/mvp-spec.md` for the feature requirements.
- Read `backend/AGENTS.md` for architecture rules.

## Procedure

### Step 1: Domain Layer (`backend/src/Convy.Domain/`)
1. Create or update **entities** with rich behavior (private setters, guard clauses).
2. Define **value objects** if the feature introduces new concepts.
3. Add **repository interface** in `Domain/Repositories/` if new aggregate.
4. Add **domain events** if the feature triggers cross-aggregate side effects.

### Step 2: Application Layer (`backend/src/Convy.Application/`)
1. Create **Command** (write) or **Query** (read) record in `Features/{Feature}/Commands/` or `Queries/`.
2. Create **Handler** implementing `IRequestHandler<TRequest, TResponse>`.
3. Create **FluentValidation Validator** for the command.
4. Create **DTOs** in `Features/{Feature}/DTOs/`.
5. Add mapping logic in the handler (entity ↔ DTO).

### Step 3: Infrastructure Layer (`backend/src/Convy.Infrastructure/`)
1. Add **EF Core entity configuration** in `Persistence/Configurations/`.
2. Implement **repository** from the Domain interface.
3. Create **migration** if schema changes: `dotnet ef migrations add <Name> --project src/Convy.Infrastructure --startup-project src/Convy.API`.
4. Add **SignalR event** if real-time notification needed.

### Step 4: API Layer (`backend/src/Convy.API/`)
1. Create **endpoint** (Minimal API or Controller method).
2. Wire up `IMediator.Send()` call.
3. Map results to HTTP responses (200/201/400/404/500).
4. Add `[Authorize]` attribute.

### Step 5: Tests
1. **Unit tests** for Domain entity behavior (`Convy.Domain.Tests`).
2. **Unit tests** for Handler logic with mocked repos (`Convy.Application.Tests`).
3. **Integration tests** for repository with real DB (`Convy.Infrastructure.Tests`).
4. **API tests** for endpoint routing and responses (`Convy.API.Tests`).

### Step 6: Verify
```bash
dotnet build backend/Convy.sln
dotnet test backend/
```

## Checklist
- [ ] Domain entity has proper invariants
- [ ] Command/Query follows CQRS naming
- [ ] Validator covers all input constraints
- [ ] Repository interface in Domain, implementation in Infrastructure
- [ ] Migration is reversible (Up + Down)
- [ ] Endpoint follows REST conventions
- [ ] Tests cover happy path + error cases
- [ ] No layer dependency violations
