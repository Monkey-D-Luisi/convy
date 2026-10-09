# Deployment

Convy deploys the active controlled-release/staging environment to a Hetzner VPS. OCI infrastructure remains a fallback/reference path unless explicitly updated to match VPS services.

## Branches And CI

- Pull requests target `master`.
- GitHub Actions `Continuous Integration` runs on `master`.
- The old `Backend Staging Release` workflow remains disabled. New automatic staging CD source is inactive pending separately approved first activation.
- After activation, exact successful master CI triggers isolated automatic application deployment; see [the staging CD runbook](operations/staging-cd.md).
- Staging deployment uses the `staging` GitHub environment, restricted to the `master` deployment branch.

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
- [Android Play Internal Release runbook](operations/android-play-internal-release.md)
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

## Automatic shared staging CD

Use [the staging CD runbook](operations/staging-cd.md) for constrained credentials, pinned artifacts, first activation, shared locking, resource/backup policy and independent rollback. Ordinary compatible master releases after activation need no individual approval. Production does not exist yet.

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

Rollback uses the fixed installed controller and checksummed application journal. It restores exact previous image/configuration bytes and preserves shared services; see [isolated release and rollback](operations/safe-release.md). Do not rebuild an old source release on the host.

## Android Versioning

Android release rules live in [VERSIONING.md](VERSIONING.md). Never reuse a `versionCode`.
Android Play Internal Testing publication is automated by the protected `Android Play Internal Release` workflow after CI succeeds on a `master` push that changes `mobile/androidApp/build.gradle.kts`. See the [Android Play Internal Release runbook](operations/android-play-internal-release.md) for environment secrets, Play service account permissions, and the no-public-artifacts policy.

Current identity:

```text
namespace = com.convy
applicationId = com.monkeydluisi.convy
```

Current `origin/master` values:

```text
versionCode = 29
versionName = 0.1.25
```
