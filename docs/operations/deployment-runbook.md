# Deployment Runbook

This runbook covers the active Hetzner staging deploy path. See [oracle-free-tier-runbook.md](oracle-free-tier-runbook.md) for OCI fallback/reference notes.

## Preconditions

- DNS points to the staging VPS.
- Firebase authorized domains include `admin.convyapp.com` and `auth.convyapp.com`.
- `/opt/convy/shared/firebase-admin.json` exists on the VPS.
- `/opt/convy/shared/api.env` exists and contains service secrets.
- `Continuous Integration` passed for the commit being deployed.

## GitHub Actions Deploy

The `Backend Staging Release` workflow:

1. Runs after successful `Continuous Integration` on a `master` push.
2. Checks out the CI commit.
3. Configures SSH from `STAGING_SSH_PRIVATE_KEY` and pinned `STAGING_SSH_KNOWN_HOSTS`.
4. Ensures a non-root deploy user if needed.
5. Packages the repository, excluding build artifacts and Terraform state.
6. Uploads to `/tmp/convy-release.tar.gz`.
7. Extracts under `/opt/convy/releases/<sha>`.
8. Runs `ops/vps/deploy-release.sh <sha>`.
9. Checks `https://<staging-api-host>/health/ready`.

Workflow file:

```text
.github/workflows/backend-staging-release.yml
```

The `staging` GitHub environment uses a selected-branch deployment policy for `master` only. Do not add `main` or broad wildcard branch policies unless the repository default branch changes and the workflows are updated in the same change.

Required secrets:

- `STAGING_DEPLOY_HOST`
- `STAGING_SSH_PRIVATE_KEY`
- `STAGING_SSH_KNOWN_HOSTS`
- `STAGING_API_HOSTNAME` or `STAGING_PUBLIC_HOSTNAME`

Optional variables:

- `STAGING_DEPLOY_USER`, default `convy-deploy`
- `STAGING_BOOTSTRAP_DEPLOY_USER`, default `root`
- `STAGING_DEPLOY_SCRIPT`, default `ops/vps/deploy-release.sh`

## Push Secrets

Use placeholders here; real values must come from the operator environment or local secret files.

```powershell
$env:OPENAI_API_KEY = "<openai-api-key>"
$env:ADMIN_BASIC_AUTH_USER = "admin"
$env:ADMIN_BASIC_AUTH_HASH = "<caddy-hash>"
$env:ADMIN_ALLOWED_EMAILS = "admin@example.com"
$env:FIREBASE_WEB_API_KEY = "<firebase-web-api-key>"
$env:FIREBASE_AUTH_DOMAIN = "convy-6520d.firebaseapp.com"
$env:FIREBASE_WEB_APP_ID = "<firebase-web-app-id>"

.\ops\vps\push-secrets.ps1 `
  -HostName "<server-ip-or-host>" `
  -ConvyHostname "convyapp.com" `
  -ConvyApiHostname "api.convyapp.com" `
  -ConvyAdminHostname "admin.convyapp.com" `
  -ConvyAuthHostname "auth.convyapp.com" `
  -ConvyMcpHostname "mcp.convyapp.com" `
  -ConvyLegalHostname "legal.convyapp.com"
```

`push-secrets.ps1` preserves existing PostgreSQL password, MCP RSA keys, and MCP audit key when present. If both MCP key values are missing, it generates a new RSA key pair.

The pushed API environment sets `VOICE_PARSING_ENABLED=true`, `McpAuth__AllowedClientMetadataHosts__0=chat.openai.com`, and `McpAuth__AllowedClientMetadataHosts__1=chatgpt.com`. If voice parsing is enabled outside Development, `OPENAI_API_KEY` must be present.

## Manual Deploy

```powershell
$sha = git rev-parse HEAD
git archive --format=tar.gz -o "$env:TEMP\convy-release.tar.gz" HEAD
scp -i "$env:USERPROFILE\.ssh\convy_vps_deploy" "$env:TEMP\convy-release.tar.gz" "convy-deploy@<server>:/tmp/convy-release.tar.gz"
ssh -i "$env:USERPROFILE\.ssh\convy_vps_deploy" "convy-deploy@<server>" "sudo mkdir -p /opt/convy/releases/$sha && sudo tar -xzf /tmp/convy-release.tar.gz -C /opt/convy/releases/$sha && sudo bash /opt/convy/releases/$sha/ops/vps/deploy-release.sh $sha"
```

## What Deploy Does

`ops/vps/deploy-release.sh`:

- preserves static bind mount directories
- writes `/opt/convy/shared/release.env`
- copies `legal/` to `/opt/convy/legal`
- copies `public-site/` to `/opt/convy/public`
- builds `api`, `worker`, `dashboard`, `auth`, and `mcp`
- starts Compose services
- checks API, auth, and MCP health
- prunes Docker image and BuildKit cache after a healthy deploy. BuildKit cache is capped by `DOCKER_BUILD_CACHE_MAX_USED_SPACE`, default `4GB`.

## Smoke Checks

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

## Rollback

1. Identify previous release:

```bash
ls -1 /opt/convy/releases
```

2. Redeploy previous release:

```bash
sudo bash /opt/convy/releases/<previous-sha>/ops/vps/deploy-release.sh <previous-sha>
```

3. Re-run all smoke checks.
4. If migrations changed schema, validate backwards compatibility before treating rollback as complete.

## Post-Deploy Checklist

- Dashboard Basic Auth challenge appears.
- Firebase dashboard login works for an admin allowlisted email.
- API admin endpoints return `401` without token and `403` for non-admin users.
- ChatGPT MCP metadata is reachable.
- MCP Developer Mode authorization works.
- `docker compose --env-file /opt/convy/shared/api.env -f /opt/convy/current/docker/docker-compose.vps.yml ps worker` shows the worker running.
- Latest backup timer is still enabled.
- `release.env` shows the expected release SHA and Android version.
