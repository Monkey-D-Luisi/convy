# Convy

Convy is a household coordination app for shared shopping lists, tasks, activity, voice-assisted item capture, realtime updates, push notifications, and a private beta ChatGPT MCP integration.

The repository is a monorepo. The mobile app is Android-first, the backend is ASP.NET Core, the admin/auth/MCP surfaces are separate web services, and the active hosted path is a Hetzner VPS behind Caddy.

## Current Status

- Active beta/staging domain: `convyapp.com`
- API host: `api.convyapp.com`
- Admin dashboard: `admin.convyapp.com`
- OAuth consent app: `auth.convyapp.com`
- ChatGPT MCP service: `mcp.convyapp.com`
- Legal host: `legal.convyapp.com`
- Legacy `178.105.70.69.nip.io` hosts remain configured for already installed staging Android builds.

## Tech Stack

| Area | Technology |
| --- | --- |
| Backend | ASP.NET Core 10, Clean Architecture, CQRS, MediatR, FluentValidation |
| Database | PostgreSQL 16, EF Core 10, Npgsql |
| Mobile | Kotlin Multiplatform, Compose Multiplatform, Android first |
| Auth | Firebase Auth for app/dashboard users, Convy OAuth broker for ChatGPT MCP |
| Realtime and push | SignalR, Firebase Cloud Messaging |
| AI | OpenAI voice transcription/parsing with operational metrics |
| Dashboard | Next.js 16, React 19, Firebase login, backend admin APIs |
| OAuth app | Next.js 16 authorization and consent UI for ChatGPT MCP |
| MCP | Node.js, TypeScript, Express, MCP SDK, Zod, JOSE |
| Hosting | Docker Compose, Caddy, Hetzner VPS; OCI files are fallback/reference |
| CI/CD | GitHub Actions for backend, dashboard, auth, MCP, infra, and staging deploy |

## Repository Structure

```text
convy/
|-- backend/       ASP.NET Core solution: Domain, Application, Infrastructure, API, tests
|-- mobile/        Kotlin Multiplatform app and Android package
|-- dashboard/     Next.js admin dashboard for health, usage, MCP, backups, and system views
|-- auth/          Next.js OAuth authorization and Firebase consent app for ChatGPT MCP
|-- mcp/           ChatGPT MCP resource server and Convy API tool adapter
|-- public-site/   Static public landing page served at convyapp.com
|-- legal/         Static privacy and terms pages served at legal.convyapp.com
|-- docker/        Local, VPS, and OCI Compose/Caddy files
|-- ops/           VPS, OCI, deployment, secret push, and backup scripts
|-- infra/         Terraform roots for Hetzner, OCI, and legacy inactive GCP
|-- docs/          Product, architecture, development, testing, deployment, operations docs
|-- AGENTS.md      Cross-editor agent governance
`-- CLAUDE.md      Claude Code project instructions
```

## Quick Start

Prerequisites:

- .NET 10 SDK
- Node.js 22
- JDK 17+
- Android SDK API 35
- Docker and Docker Compose
- Firebase project/configuration for authenticated flows

```bash
# PostgreSQL for local backend development
cd docker
docker compose up -d db
cd ..

# Backend
dotnet restore backend/Convy.slnx
dotnet build backend/Convy.slnx
dotnet run --project backend/src/Convy.API --launch-profile http

# Dashboard
cd dashboard
npm ci
npm run dev

# Auth app
cd ../auth
npm ci
npm run dev

# MCP service
cd ../mcp
npm ci
npm run dev

# Android app and shared tests
cd ../mobile
./gradlew :shared:testDebugUnitTest :composeApp:testDebugUnitTest :androidApp:assembleLocalDebug
```

See [docs/DEVELOPMENT.md](docs/DEVELOPMENT.md) for environment variables, user-secrets, service ports, and troubleshooting.

## Core Commands

```bash
dotnet restore backend/Convy.slnx
dotnet build backend/Convy.slnx --no-restore -c Release
dotnet test backend/Convy.slnx --no-build -c Release --verbosity normal

cd dashboard && npm ci && npm test && npm run lint && npm run build
cd ../auth && npm ci && npm test && npm run lint && npm run build
cd ../mcp && npm ci && npm test && npm run lint && npm run build

cd ../mobile
./gradlew :shared:testDebugUnitTest :composeApp:testDebugUnitTest :androidApp:assembleLocalDebug

cd ../docker
docker compose -f docker-compose.yml config --quiet
docker compose -f docker-compose.vps.yml config --quiet
docker compose -f docker-compose.oci.yml config --quiet
```

## Android Package Identity

Do not change the published Android application ID.

```text
android namespace = com.convy
android applicationId = com.monkeydluisi.convy
```

Kotlin source namespaces are internal implementation details. The Play Store package identity remains `com.monkeydluisi.convy`.

## Documentation

- [Overview](docs/OVERVIEW.md) - product, modules, users, and current capabilities
- [Architecture](docs/ARCHITECTURE.md) - system structure, data model, flows, and boundaries
- [Development](docs/DEVELOPMENT.md) - local setup, environment variables, and troubleshooting
- [Testing](docs/TESTING.md) - unit, integration, E2E, CI, infra, and smoke checks
- [Deployment](docs/DEPLOYMENT.md) - branches, staging deploy, Android versioning, rollback
- [Operations](docs/OPERATIONS.md) - health checks, backups, logs, Caddy, DNS, MCP operations
- [Security](docs/SECURITY.md) - auth, authorization, MCP OAuth/JWT, audit, secrets, backups
- [ChatGPT MCP](docs/mcp/README.md) - Convy MCP integration for ChatGPT
- [AI Tooling](docs/ai-tooling/mcp-setup.md) - MCP servers used by development agents
- [ADRs](docs/adr/) - architecture decisions
- [Versioning](docs/VERSIONING.md) - Android version history and release rules

## Git Workflow

- Branch naming: `feature/<short-description>`, `fix/<short-description>`, or `chore/<short-description>`
- Commits: Conventional Commits (`feat:`, `fix:`, `chore:`, `docs:`, `test:`, `refactor:`)
- Pull requests target `master`. CI is configured for both `master` and `main` while repository defaults are normalized.

## License And Distribution

Convy is a private beta project. Do not distribute binaries, credentials, datasets, or hosted access outside the approved beta/testing scope.
