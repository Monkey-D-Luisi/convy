# Deployment

Convy deploys the active controlled-release/staging environment to a Hetzner VPS. OCI infrastructure remains a fallback/reference path unless explicitly updated to match VPS services.

## Branches And CI

- Pull requests target `master`.
- GitHub Actions CI runs on `master` and `main`.
- CD deploys the exact commit that passed CI through `workflow_run`.
- Staging deployment uses the `staging` GitHub environment.

## Active Staging Stack

The VPS Compose stack runs:

- `db`: PostgreSQL 16
- `api`: ASP.NET Core API
- `worker`: .NET worker for recurring items, task reminders, and system metric snapshots
- `dashboard`: Next.js admin dashboard
- `auth`: Next.js OAuth consent app
- `mcp`: Node/TypeScript MCP service
- `caddy`: TLS termination, static file serving, and reverse proxy

Canonical runbooks:

- [Deployment runbook](operations/deployment-runbook.md)
- [Hetzner VPS runbook](operations/hetzner-vps-runbook.md)
- [MCP runbook](operations/mcp-runbook.md)
- [Backup and restore runbook](operations/backup-restore-runbook.md)

## Domains

| Host | Purpose |
| --- | --- |
| `convyapp.com` | Public landing page |
| `www.convyapp.com` | Public landing alias |
| `api.convyapp.com` | Backend API |
| `admin.convyapp.com` | Admin dashboard |
| `auth.convyapp.com` | ChatGPT MCP OAuth consent app |
| `mcp.convyapp.com` | ChatGPT MCP service |
| `legal.convyapp.com` | Privacy and terms |

Legacy `178.105.70.69.nip.io` hosts remain configured for previously installed staging Android builds and cutover safety.

## GitHub CD Inputs

Repository secrets:

- `STAGING_DEPLOY_HOST`
- `STAGING_SSH_PRIVATE_KEY`
- `STAGING_SSH_KNOWN_HOSTS`
- `STAGING_API_HOSTNAME` or `STAGING_PUBLIC_HOSTNAME`

Repository variables:

- `STAGING_DEPLOY_USER`, default `convy-deploy`
- `STAGING_BOOTSTRAP_DEPLOY_USER`, default `root`
- `STAGING_DEPLOY_SCRIPT`, default `ops/vps/deploy-release.sh`

The workflow can bootstrap the non-root deploy user when `STAGING_DEPLOY_USER` is not set.

## Manual Deploy Outline

```bash
git archive --format=tar.gz -o /tmp/convy-release.tar.gz HEAD
scp /tmp/convy-release.tar.gz <deploy-user>@<server>:/tmp/convy-release.tar.gz
ssh <deploy-user>@<server> "sudo mkdir -p /opt/convy/releases/<sha> && sudo tar -xzf /tmp/convy-release.tar.gz -C /opt/convy/releases/<sha> && sudo bash /opt/convy/releases/<sha>/ops/vps/deploy-release.sh <sha>"
```

Use the detailed [deployment runbook](operations/deployment-runbook.md) for exact commands and smoke checks.

## Health Checks

```bash
curl -fsS https://api.convyapp.com/health
curl -fsS https://api.convyapp.com/health/ready
curl -fsS https://auth.convyapp.com/health
curl -fsS https://mcp.convyapp.com/health
curl -fsS https://mcp.convyapp.com/.well-known/oauth-protected-resource
curl -fsS https://legal.convyapp.com/privacy
curl -fsS https://convyapp.com
curl -I https://admin.convyapp.com
```

`admin.convyapp.com` should return a Basic Auth challenge before the dashboard Firebase login appears.

## Rollback

Rollback is release-directory based:

1. Identify the previous healthy release under `/opt/convy/releases`.
2. Run the previous release's `ops/vps/deploy-release.sh <previous-sha>`.
3. Confirm API, auth, MCP, dashboard, legal, public, and legacy health checks, then confirm the `worker` service is running.
4. If database migrations have already changed schema, treat rollback as an incident and validate compatibility before downgrading application code.

## Android Versioning

Android release rules live in [VERSIONING.md](VERSIONING.md). Never reuse a `versionCode`.

Current identity:

```text
namespace = com.convy
applicationId = com.monkeydluisi.convy
```

Current `origin/master` values:

```text
versionCode = 23
versionName = 0.1.20
```
