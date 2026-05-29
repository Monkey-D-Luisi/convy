# Development

This guide covers local setup for the repository. It does not replace service-specific runbooks in [operations](operations/).

## Prerequisites

- .NET 10 SDK
- Node.js 22 and npm
- JDK 17+
- Android SDK API 35
- Docker and Docker Compose
- Firebase project access for authenticated flows
- Optional: Maestro CLI 2.4.0+ for Android E2E tests

## Local Database

```bash
cd docker
docker compose up -d db
docker compose ps
```

The local Compose file starts PostgreSQL. PgAdmin is available through the tools profile when needed:

```bash
cd docker
docker compose --profile tools up -d pgadmin
```

## Backend

```bash
dotnet restore backend/Convy.slnx
dotnet user-secrets set --project backend/src/Convy.API "ConnectionStrings:DefaultConnection" "<local-postgres-connection-string>"
dotnet user-secrets set --project backend/src/Convy.API "Firebase:ProjectId" "<firebase-project-id>"
dotnet user-secrets set --project backend/src/Convy.API "OpenAI:ApiKey" "<openai-api-key>"
dotnet build backend/Convy.slnx
dotnet run --project backend/src/Convy.API --launch-profile http
```

Use placeholders in docs and examples. Do not commit real Firebase admin JSON, OpenAI keys, MCP keys, Caddy hashes, or deployment secrets.

## Dashboard

```bash
cd dashboard
npm ci
npm run dev
```

Important environment variables:

- `CONVY_API_BASE_URL`
- `NEXT_PUBLIC_FIREBASE_API_KEY`
- `NEXT_PUBLIC_FIREBASE_AUTH_DOMAIN`
- `NEXT_PUBLIC_FIREBASE_PROJECT_ID`
- `NEXT_PUBLIC_FIREBASE_APP_ID`

The dashboard is also protected in hosted environments by Caddy Basic Auth and backend `AdminOnly` authorization.

## Auth App

```bash
cd auth
npm ci
npm run dev
```

The auth app hosts Firebase login and OAuth consent for ChatGPT MCP. It forwards authorization approval to:

```text
/api/v1/mcp/oauth/authorize/approve
```

Hosted metadata and token endpoints are routed through Caddy to the backend API.

## MCP Service

```bash
cd mcp
npm ci
npm run dev
```

Required local/runtime values:

- `CONVY_API_BASE_URL`
- `MCP_PUBLIC_URL`
- `AUTH_PUBLIC_URL`
- `MCP_JWT_ISSUER`
- `MCP_JWT_AUDIENCE`
- `MCP_JWT_PUBLIC_KEY`, `MCP_JWT_PUBLIC_KEY_BASE64`, or `MCP_JWT_PUBLIC_KEY_PATH`
- `CONVY_MCP_AUDIT_API_KEY` in production

The MCP service calls the Convy API. It never connects directly to PostgreSQL.

## Mobile

```bash
cd mobile
./gradlew :shared:testDebugUnitTest
./gradlew :composeApp:testDebugUnitTest
./gradlew :androidApp:assembleLocalDebug
```

Local Android builds point to the host backend through the emulator alias:

```text
http://10.0.2.2:5062
```

Staging builds default to:

```text
https://api.convyapp.com
```

Do not change the published Android identity:

```text
namespace = com.convy
applicationId = com.monkeydluisi.convy
```

## Documentation Maintenance

Any PR that adds or changes a service, public domain, database table, workflow, environment variable, OAuth/MCP behavior, backup behavior, deployment script, or public integration must update the relevant docs in the same PR.

At minimum, check:

- `README.md`
- `docs/OVERVIEW.md`
- `docs/ARCHITECTURE.md`
- `docs/TESTING.md`
- `docs/DEPLOYMENT.md`
- `docs/OPERATIONS.md`
- `docs/SECURITY.md`
- `docs/mcp/*` when ChatGPT MCP behavior changes
- `docs/operations/*` when deploy or runbook behavior changes

## Troubleshooting

| Symptom | Check |
| --- | --- |
| Backend cannot connect to PostgreSQL | Confirm `docker compose ps db`, connection string, and port 5432. |
| Firebase token validation fails | Confirm `Firebase:ProjectId` and authorized Firebase domains. |
| Dashboard or auth app shows missing Firebase config | Confirm `NEXT_PUBLIC_FIREBASE_*` environment values. |
| MCP returns 401 | Confirm bearer token, issuer, audience, and public key configuration. |
| MCP writes return 400 | MCP write API calls require `Idempotency-Key`; the MCP service generates one for ChatGPT calls. |
| Android emulator cannot reach API | Use `10.0.2.2` for host localhost and confirm backend launch profile port 5062. |
