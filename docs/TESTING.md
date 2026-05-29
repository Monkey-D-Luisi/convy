# Testing

This document covers local verification, CI coverage, manual smoke checks, and live MCP validation.

## Backend

Projects:

| Project | Focus |
| --- | --- |
| `Convy.Domain.Tests` | Entity invariants and value objects. |
| `Convy.Application.Tests` | CQRS handlers, validators, services, user-facing normalization, smart batch behavior. |
| `Convy.Infrastructure.Tests` | EF Core repositories, metrics readers, PostgreSQL integration through Testcontainers. |
| `Convy.API.Tests` | Minimal API contracts, auth/authorization behavior, OAuth/MCP contracts, ops contracts. |

Commands:

```bash
dotnet restore backend/Convy.slnx
dotnet build backend/Convy.slnx --no-restore -c Release
dotnet test backend/Convy.slnx --no-build -c Release --verbosity normal
```

Use `ConnectionStrings__DefaultConnection` when a test run needs a specific PostgreSQL endpoint.

## Dashboard

The dashboard uses Node's test runner for contract tests and TypeScript for lint/build validation.

```bash
cd dashboard
npm ci
npm test
npm run lint
npm run build
```

Coverage includes:

- admin API proxy behavior
- Firebase token forwarding
- non-cacheable admin responses
- overview/usage/OpenAI/MCP/backup/system view contracts
- typed admin payload shape

## Auth App

```bash
cd auth
npm ci
npm test
npm run lint
npm run build
```

Coverage includes:

- OAuth request validation
- Firebase config path
- approval route to backend OAuth endpoint
- Google Sign-In presence for consent
- supported scope display and rejection of unsupported scopes

## MCP Service

```bash
cd mcp
npm ci
npm test
npm run lint
npm run build
```

Coverage includes:

- metadata generation
- bearer token challenges
- issuer/audience/token-use validation
- scope enforcement
- tool definitions and schemas
- API client behavior and redacted audit logging

## Mobile Unit And Build Checks

```bash
cd mobile
./gradlew :shared:testDebugUnitTest
./gradlew :composeApp:testDebugUnitTest
./gradlew :androidApp:assembleLocalDebug
```

The local Android flavor points to `http://10.0.2.2:5062`. Staging defaults to `https://api.convyapp.com`.

## Mobile E2E With Maestro

Prerequisites:

- Android emulator running and visible in `adb devices`
- API running locally on port 5062
- PostgreSQL running through Docker
- Maestro CLI 2.4.0+
- App installed with `./gradlew :androidApp:installLocalDebug`

Run the suite:

```bash
cd mobile
powershell -File e2e/run-e2e.ps1
```

Manual equivalent:

```bash
maestro test -e EMAIL="e2e_<timestamp>@test.com" -e JOIN_EMAIL="e2e_join_<timestamp>@test.com" -e APP_VERSION="<current-versionName>" e2e/
```

Important conventions:

- Every flow declares `appId: com.monkeydluisi.convy`.
- `config.yaml` uses `flows: ["*"]` so `pending/` is excluded.
- `flowsOrder` entries do not include `.yaml`.
- Use `id:` selectors from Compose test tags where possible.
- Completed items are hidden until the completed section is expanded.

## Infrastructure Validation

```bash
docker compose -f docker/docker-compose.yml config --quiet
docker compose -f docker/docker-compose.vps.yml config --quiet
docker compose -f docker/docker-compose.oci.yml config --quiet
```

Script validation used by CI:

```powershell
Get-ChildItem ops -Recurse -Filter *.sh | ForEach-Object { bash -n $_.FullName }
Get-ChildItem ops -Recurse -Filter *.ps1 | ForEach-Object {
  $errors = $null
  [System.Management.Automation.PSParser]::Tokenize((Get-Content -Raw $_.FullName), [ref]$errors) | Out-Null
  if ($errors) { $errors | ForEach-Object { Write-Error "$($_.Message) in $($_.Token.Content)" }; exit 1 }
}
```

Docker build validation:

```bash
docker build -f docker/backend/Dockerfile backend
docker build -f dashboard/Dockerfile dashboard
docker build -f auth/Dockerfile auth
docker build -f mcp/Dockerfile mcp
```

## GitHub Actions CI

CI jobs:

- `backend`: restore, build, test with PostgreSQL service.
- `dashboard`: install, test, lint, build.
- `auth`: install, test, lint, build.
- `mcp`: install, test, lint, build.
- `infra`: Compose config validation, script syntax validation, Docker builds.

CD runs only after CI completes successfully and performs an external staging health check against `STAGING_API_HOSTNAME` or `STAGING_PUBLIC_HOSTNAME`.

## Staging Smoke Checks

```bash
curl -fsS https://api.convyapp.com/health
curl -fsS https://api.convyapp.com/health/ready
curl -fsS https://auth.convyapp.com/health
curl -fsS https://mcp.convyapp.com/health
curl -fsS https://mcp.convyapp.com/.well-known/oauth-protected-resource
curl -fsS https://auth.convyapp.com/.well-known/oauth-authorization-server
curl -fsS https://legal.convyapp.com/privacy
curl -fsS https://legal.convyapp.com/terms
curl -fsS https://convyapp.com
curl -I https://admin.convyapp.com
curl -fsS https://178.105.70.69.nip.io/health/ready
```

Expected dashboard behavior:

- Caddy prompts for Basic Auth.
- Firebase login is required after Basic Auth.
- Backend admin APIs return `401` without a token.
- Authenticated non-admin users receive `403`.

## Live ChatGPT MCP Manual Tests

Use [manual-chatgpt-test-plan.md](mcp/manual-chatgpt-test-plan.md). Required coverage:

- connect MCP server in ChatGPT Developer Mode
- complete OAuth authorization
- query context, shopping lists, task lists, and recent activity
- add shopping items
- avoid duplicate pending items
- return completed items/tasks to pending through smart writes where applicable
- mark items/tasks completed and pending
- revoke access
- confirm audit records are created without prompts/full arguments

## Repository Scans

Run before merging documentation or deployment changes:

```bash
rg -n "convy\\.app|com\\.combi|com\\.combi\\.app|com\\.convy\\.app" .
rg -n "main-only PR wording|main as the sole target|default branch is main" README.md docs .github
rg -n "BEGIN (RSA |EC |OPENSSH )?PRIVATE KEY|PRIVATE KEY-----|ghp_[A-Za-z0-9_]+|sk-proj-[A-Za-z0-9_]+|McpAuth__PrivateKeyPemBase64=[A-Za-z0-9+/=]{80,}" .
```

Review `178.105.70.69.nip.io` hits manually. They are valid only for legacy staging host documentation, examples, and Caddy configuration.

## Markdown Link Validation

Use a local Markdown link checker or a scripted path scan. Internal links should resolve after docs are moved or renamed, especially:

- `docs/MCP-SETUP.md` compatibility stub
- `docs/ai-tooling/mcp-setup.md`
- `docs/mcp/smart-tool-behavior.md`
- `docs/operations/*`
