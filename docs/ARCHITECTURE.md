# Convy — Architecture

## Overview

Convy is a shared household coordination app. The architecture follows a monorepo approach with clear boundaries between backend and mobile.

```
convy/
|-- backend/          ASP.NET Core 10 - Clean Architecture (Onion)
|-- mobile/           Kotlin Multiplatform + Compose Multiplatform - MVI
|-- docker/           Docker Compose files for local and hosted environments
|-- docs/             Product, architecture, testing, and governance docs
|-- .github/          Copilot governance, prompts, hooks, and GitHub Actions
|-- .claude/          Claude Code governance and MCP configuration
`-- .agents/          Codex workflow skills
```

## Backend - Clean Architecture

```
Convy.API              -> Presentation layer (Minimal API endpoints, middleware, SignalR hubs)
  depends on
Convy.Application      -> Use cases (commands, queries, handlers via MediatR)
  depends on
Convy.Domain           -> Core entities, value objects, repository interfaces
  implemented by
Convy.Infrastructure   -> EF Core, Firebase Admin, OpenAI, push notifications, external services
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

## Mobile - MVI with KMP

```
androidApp/       -> Android entry point, flavors, Firebase integration, platform services
composeApp/       -> Compose Multiplatform UI (screens, components, theme, navigation)
shared/           -> Shared domain models, repositories, networking, offline sync, DI
```

### MVI Flow

```
User -> Intent -> Store -> State -> Composable
                  |
                  `-> SideEffect -> One-shot navigation or snackbar
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
- Current core tables include `users`, `households`, `household_memberships`, `household_lists`, `list_items`, `task_items`, `invites`, `activity_logs`, `device_tokens`, and `notification_preferences`

## Authentication Flow

1. Mobile authenticates with Firebase Auth and receives a JWT
2. Mobile sends JWT in `Authorization: Bearer <token>` header
3. Backend validates JWT with Firebase Admin SDK
4. Backend extracts `firebase_uid` from claims

## ADRs

See [docs/adr/](adr/) for Architecture Decision Records.
