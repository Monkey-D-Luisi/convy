# Convy — Architecture

## Overview

Convy is a shared household coordination app. The architecture follows a monorepo approach with clear boundaries between backend and mobile.

```
convy/
├── backend/          ASP.NET Core 10 — Clean Architecture (Onion)
├── mobile/           KMP + Compose Multiplatform — MVI
├── docker/           Docker Compose (PostgreSQL, API, pgAdmin)
├── docs/             Documentation & ADRs
├── .github/          Copilot governance (instructions, agents, skills, prompts, hooks)
└── .claude/          Claude Code governance (skills, settings)
```

## Backend — Clean Architecture

```
Convy.API              → Presentation layer (Controllers, Middleware, SignalR Hubs)
  ↓ depends on
Convy.Application      → Use cases (Commands, Queries, Handlers via MediatR)
  ↓ depends on
Convy.Domain           → Core entities, value objects, repository interfaces
  ↑ implemented by
Convy.Infrastructure   → EF Core, Firebase Admin, external services
```

### Key Patterns

| Pattern | Implementation |
|---------|---------------|
| CQRS | MediatR (Commands/Queries with separate handlers) |
| Validation | FluentValidation pipeline behavior |
| ORM | Entity Framework Core 10 + Npgsql (PostgreSQL) |
| Auth | Firebase Auth (JWT validation) |
| Real-time | SignalR |
| Error Handling | Result pattern (no exceptions for domain flow) |

### Dependency Rules

- **Domain** has ZERO external dependencies
- **Application** depends only on Domain
- **Infrastructure** implements interfaces from Domain/Application
- **API** wires everything via DI

## Mobile — MVI with KMP

```
androidApp/       → Android entry point (Application, Activity)
composeApp/       → Compose Multiplatform UI (screens, components, theme, navigation)
shared/           → Business logic (data, domain, DI)
```

### MVI Flow

```
User → Intent → Store → State → Composable
                  ↓
              SideEffect → One-shot navigation or snackbar
```

### Key Libraries

| Purpose | Library |
|---------|---------|
| HTTP | Ktor Client |
| DI | Koin |
| Serialization | kotlinx.serialization |
| Async | kotlinx.coroutines |

## Database

- PostgreSQL 16 (via Docker)
- Naming: `snake_case` for tables and columns
- Fluent API only (no data annotations)
- Migrations via EF Core CLI

## Authentication Flow

1. Mobile authenticates with Firebase Auth → gets JWT
2. Mobile sends JWT in `Authorization: Bearer <token>` header
3. Backend validates JWT with Firebase Admin SDK
4. Backend extracts `firebase_uid` from claims

## ADRs

See [docs/adr/](adr/) for Architecture Decision Records.
