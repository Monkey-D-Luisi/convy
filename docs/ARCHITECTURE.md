# Convy Architecture

Convy is a monorepo with separate runtime services for user-facing mobile features, admin operations, ChatGPT MCP OAuth, and public/legal pages.

```text
Users
  |-- Android app -> api.convyapp.com -> ASP.NET Core API -> PostgreSQL
  |                                      |-- SignalR
  |                                      |-- Firebase Admin / FCM
  |                                      `-- OpenAI voice parsing
  |                                      ^
  |                                      |
  |                         Convy.Worker scheduled jobs
  |                         |-- recurring item rollover
  |                         |-- task reminders
  |                         `-- system metric snapshots
  |
  |-- Admin browser -> admin.convyapp.com -> Next.js dashboard -> API admin endpoints
  |
  `-- ChatGPT -> mcp.convyapp.com -> MCP service -> API MCP-scoped endpoints
                         |
                         `-- auth.convyapp.com -> OAuth consent app -> API OAuth broker
```

## Monorepo Layout

```text
backend/       ASP.NET Core API, worker, Clean Architecture, EF Core, tests
mobile/        Kotlin Multiplatform and Android app
dashboard/     Next.js admin dashboard
auth/          Next.js OAuth consent app for ChatGPT MCP
mcp/           Node/TypeScript MCP service
docker/        Compose and Caddy runtime definitions
ops/           Deploy, secret push, backup, restore scripts
infra/         Terraform roots for Hetzner, OCI, and legacy inactive GCP
legal/         Static privacy and terms pages
public-site/   Static public landing page
docs/          Project documentation
```

## Backend

The backend follows Clean Architecture with CQRS:

```text
Convy.API    -> Convy.Infrastructure -> Convy.Application -> Convy.Domain
Convy.Worker -> Convy.Infrastructure -> Convy.Application -> Convy.Domain
```

- `Convy.Domain`: entities, value objects, invariants; zero external dependencies.
- `Convy.Application`: MediatR commands/queries, handlers, DTOs, validators, application interfaces.
- `Convy.Infrastructure`: EF Core, PostgreSQL, Firebase Admin, OpenAI, push notifications, metrics readers, repository implementations.
- `Convy.API`: Minimal API endpoints, auth policies, health checks, OAuth broker, admin endpoints, SignalR wiring.
- `Convy.Worker`: .NET worker host for recurring items, task reminders, and system metric snapshots.

Key backend patterns:

- CQRS through MediatR.
- FluentValidation pipeline behavior.
- Result pattern for expected application failures.
- EF Core Fluent API configuration only.
- Firebase bearer authentication for app/dashboard users.
- MCP bearer authentication for ChatGPT MCP access tokens.

## Mobile

The mobile app uses Kotlin Multiplatform and Compose Multiplatform with MVI:

```text
Intent -> Store -> State -> Composable UI
             |
             `-> SideEffect
```

Main modules:

- `androidApp`: Android entry point, package identity, Firebase/FCM integration, build flavors.
- `composeApp`: shared Compose UI, screens, navigation, theme.
- `shared`: data repositories, Ktor API client, DTOs, domain models, DI, sync helpers.

Task forms keep due date and reminder as structured local date/time state and convert reminder values to UTC at the repository boundary. Offline sync queues item and task complete, uncomplete, and delete actions for reconnect replay.

Android identity:

```text
namespace = com.convy
applicationId = com.monkeydluisi.convy
```

## Dashboard

The dashboard is a Next.js app served at `admin.convyapp.com`.

Security layers:

1. Caddy Basic Auth.
2. Firebase login in the dashboard.
3. Backend `AdminOnly` policy with email allowlist.

Dashboard views cover:

- overview health
- usage metrics
- OpenAI metrics
- ChatGPT MCP metrics
- backup runs and downloads
- system health

Dashboard API routes proxy authenticated requests to backend `/api/v1/admin/*` endpoints.

## Auth App

The auth app is a Next.js app served at `auth.convyapp.com`. It hosts the user-facing OAuth authorization and consent experience for ChatGPT MCP.

Flow:

1. ChatGPT starts OAuth authorization.
2. User signs in with Firebase email/password or Google Sign-In.
3. User reviews requested Convy scopes.
4. Auth app posts Firebase ID token and OAuth request details to the backend approval endpoint.
5. Backend validates client metadata, redirect URI, resource, scopes, and PKCE requirements.
6. Backend issues an authorization code redirect to ChatGPT.

## ChatGPT MCP Service

The MCP service is a Node/TypeScript resource server served at `mcp.convyapp.com`.

Responsibilities:

- Publish protected resource metadata.
- Return OAuth challenges for missing/invalid authorization.
- Validate RS256 MCP access tokens.
- Register static MCP tools with strict Zod schemas.
- Call Convy API endpoints with MCP access tokens.
- Send compact MCP audit records to the API.

The MCP service never reads PostgreSQL directly.

## OAuth And Token Flow

```text
ChatGPT
  -> mcp.convyapp.com/mcp
  <- 401 with protected resource metadata
  -> auth.convyapp.com/oauth/authorize
  -> Firebase login + consent
  -> API /api/v1/mcp/oauth/authorize/approve
  <- authorization code redirect
  -> auth.convyapp.com/oauth/token
  <- RS256 access token + refresh token
  -> mcp.convyapp.com/mcp with bearer token
```

Access tokens include MCP-specific claims such as `token_use=mcp_access`, `auth_source=mcp`, `client_id`, `sub`, and `scope`.

## Infrastructure And Routing

The active controlled-release/staging environment runs on Hetzner:

```text
Caddy
  |-- convyapp.com -> /srv/public
  |-- legal.convyapp.com -> /srv/legal
  |-- api.convyapp.com -> api:8080
  |-- admin.convyapp.com -> dashboard:3000
  |-- auth.convyapp.com -> auth:3000 plus OAuth API routes to api:8080
  `-- mcp.convyapp.com -> mcp:3001

worker
  `-- scheduled backend jobs, not exposed through Caddy
```

OCI and legacy GCP infrastructure are reference/fallback roots and should not be documented as active production unless the deployment target changes.

## Database

Current EF Core `DbSet` tables:

| Entity | Table purpose |
| --- | --- |
| `users` | Convy users mapped to Firebase identities. |
| `households` | Household containers. |
| `household_memberships` | User membership and roles per household. |
| `invites` | Household invitation codes and status. |
| `household_lists` | Shopping/task lists. |
| `list_items` | Shopping list items, completion, recurrence, normalized titles, creation source. |
| `task_items` | Task list items, completion, assignment, due date, reminder, priority, normalized titles. |
| `activity_logs` | Household activity feed. |
| `device_tokens` | FCM device registrations. |
| `notification_preferences` | User notification settings. |
| `voice_parse_events` | Redacted voice parsing operational metrics. |
| `ai_usage_events` | AI usage, cost, latency, and token metrics. |
| `backup_runs` | Backup metadata, verification state, checksums, and durations. |
| `mcp_oauth_authorization_codes` | Hashed OAuth authorization codes. |
| `mcp_oauth_refresh_tokens` | Hashed/rotated OAuth refresh tokens. |
| `mcp_oauth_consents` | User/client/resource/scope consent records. |
| `mcp_tool_invocations` | MCP audit records with redacted tool invocation metadata. |
| `mcp_idempotency_records` | Hashed MCP write idempotency records. |

Core ownership relationships are enforced by PostgreSQL foreign keys. Structural ownership uses cascade deletes; actor/audit references use restrict deletes. See [ADR 005](adr/005-database-referential-integrity.md).

## Runtime Flows

### Firebase app auth

1. Mobile signs in with Firebase.
2. Mobile sends bearer token to API.
3. API validates with Firebase Admin.
4. API maps Firebase UID to Convy user and enforces membership.

### Realtime

1. Mobile connects to SignalR after authentication.
2. API joins the client to household-scoped groups.
3. Backend handlers publish events after relevant list/task/item changes.
4. Other connected household clients refresh or update state.

### Push notifications

1. Mobile registers FCM device token.
2. API stores token and notification preferences.
3. API events enqueue/send immediate household notifications through Firebase Cloud Messaging.
4. Worker sends scheduled task reminders through Firebase Cloud Messaging.

### Worker jobs

1. `Convy.Worker` starts as a separate Compose service.
2. It uses the same PostgreSQL database and Firebase Admin credentials as the API.
3. It runs recurring item rollover, task reminder fanout, and system metric snapshot jobs outside API request latency.
4. Task reminder processing uses a PostgreSQL advisory lock so duplicate worker instances do not send duplicate reminders.

### Voice parsing

1. Mobile sends voice input to API.
2. API sends audio/content to OpenAI for transcription and parsing.
3. API returns parsed items for user confirmation.
4. API stores redacted voice and AI usage metrics.

### MCP tools

1. ChatGPT invokes a static Convy MCP tool.
2. MCP validates access token and tool scopes.
3. MCP calls API with the same bearer token.
4. API enforces membership and MCP scope policies.
5. MCP returns concise structured content and writes audit metadata.

### Backups

1. Systemd timer runs VPS backup script.
2. Script creates PostgreSQL custom-format dump.
3. Script records checksum, size, duration, and verification status.
4. If restic is configured, the script uploads the dump and metadata to encrypted offsite storage.
5. Weekly restore verification restores the latest dump into a temporary database.
6. Dashboard can download registered successful dumps through admin-only API.

## Dependency Rules

- Domain has no external dependencies.
- Application depends only on Domain.
- Infrastructure depends on Application and Domain.
- API and Worker depend on backend layers for composition.
- Dashboard/auth/MCP call the API; they do not access PostgreSQL directly.
- MCP tokens must not satisfy `AdminOnly`.

## Security Boundaries

- Firebase Auth protects user and admin identity.
- Caddy Basic Auth adds a first gate for dashboard access.
- Backend email allowlist gates admin APIs.
- MCP OAuth grants only explicit Convy scopes.
- MCP writes are idempotent and limited to item/task creation and status changes.
- MCP smart write API endpoints require MCP-only policies and cannot be called with Firebase/mobile tokens.
- Backups are admin-only and resolved under the configured backup root.
- Legal and public pages are static and served by Caddy.
