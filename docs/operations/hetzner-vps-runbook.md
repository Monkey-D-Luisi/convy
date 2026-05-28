# Hetzner VPS Hosting Runbook

Hetzner is the active staging target for the current beta deployment. The VPS runs PostgreSQL, the ASP.NET Core API, the Next.js admin dashboard, legal static pages, and Caddy in Docker Compose.

OCI files are intentionally not updated for the admin dashboard, legal host, or new backup workflow in this change. Treat `docker-compose.oci.yml`, `Caddyfile.oci`, and `ops/oci` as reference material until a dedicated OCI follow-up updates them.

## Infrastructure Layout

- `infra/hetzner`: Hetzner Cloud Terraform stack.
- `ops/vps`: VPS bootstrap, secret push, release deployment, and backup helpers.
- `docker/docker-compose.vps.yml`: runtime services for `db`, `api`, `dashboard`, `auth`, `mcp`, and `caddy`.
- `docker/Caddyfile.vps`: public routing for root, API, admin dashboard, OAuth auth, MCP, and legal pages.
- `public-site`: minimal public Convy landing page copied to `/opt/convy/public` during deploy.
- `legal`: static legal documents copied to `/opt/convy/legal` during deploy.
- `dashboard`: Next.js admin dashboard served behind Caddy Basic Auth.

## Required Hosts

Configure DNS before the first staging deploy:

- `CONVY_PUBLIC_HOSTNAME`, currently `convyapp.com`
- `CONVY_WWW_HOSTNAME`, currently `www.convyapp.com`
- `CONVY_API_HOSTNAME`, currently `api.convyapp.com`
- `CONVY_ADMIN_HOSTNAME`, currently `admin.convyapp.com`
- `CONVY_AUTH_HOSTNAME`, currently `auth.convyapp.com`
- `CONVY_MCP_HOSTNAME`, currently `mcp.convyapp.com`
- `CONVY_LEGAL_HOSTNAME`, currently `legal.convyapp.com`
- `CONVY_LEGACY_API_HOSTNAME`, currently `178.105.70.69.nip.io` for previously installed Android staging builds
- `CONVY_LEGACY_ADMIN_HOSTNAME`, currently `admin.178.105.70.69.nip.io`
- `CONVY_LEGACY_AUTH_HOSTNAME`, currently `auth.178.105.70.69.nip.io`
- `CONVY_LEGACY_MCP_HOSTNAME`, currently `mcp.178.105.70.69.nip.io`
- `CONVY_LEGACY_LEGAL_HOSTNAME`, currently `legal.178.105.70.69.nip.io`

Caddy obtains and renews TLS certificates for the public `convyapp.com` hosts and the temporary `nip.io` fallback hosts.

## Secrets

Store the Hetzner API token outside the repo:

```powershell
New-Item -ItemType Directory -Force "$env:USERPROFILE\.config\convy\secrets"
Set-Content -Path "$env:USERPROFILE\.config\convy\secrets\hcloud-token.txt" -Value "<token>"
```

Set these environment variables before running `ops/vps/push-secrets.ps1`:

```powershell
$env:OPENAI_API_KEY = "<openai-api-key>"
$env:ADMIN_BASIC_AUTH_USER = "admin"
$env:ADMIN_BASIC_AUTH_HASH = "<caddy-hash>"
$env:ADMIN_ALLOWED_EMAILS = "admin@example.com"
$env:FIREBASE_WEB_API_KEY = "<firebase-web-api-key>"
$env:FIREBASE_WEB_APP_ID = "<firebase-web-app-id>"
```

Generate the Caddy password hash with:

```powershell
docker run --rm caddy:2.10.0-alpine caddy hash-password --plaintext "<admin-password>"
```

OpenAI price settings are optional. If they are not set, aggregate OpenAI cost estimates are returned as `null` while request and token counts still appear:

```powershell
$env:OPENAI_COST_TRANSCRIPTION_AUDIO_MICROS_PER_SECOND = "<micros>"
$env:OPENAI_COST_PARSING_INPUT_MICROS_PER_1K_TOKENS = "<micros>"
$env:OPENAI_COST_PARSING_CACHED_INPUT_MICROS_PER_1K_TOKENS = "<micros>"
$env:OPENAI_COST_PARSING_OUTPUT_MICROS_PER_1K_TOKENS = "<micros>"
$env:OPENAI_COST_PARSING_REASONING_MICROS_PER_1K_TOKENS = "<micros>"
```

## Provision

```powershell
cd infra/hetzner
Copy-Item terraform.tfvars.example terraform.tfvars
..\..\ops\hetzner\invoke-terraform.ps1 init
..\..\ops\hetzner\invoke-terraform.ps1 plan
```

Only run `apply` after confirming the planned server type:

```powershell
..\..\ops\hetzner\invoke-terraform.ps1 apply
```

## First Bootstrap

```powershell
$ip = terraform output -raw public_ip
$rootHost = "convyapp.com"
$apiHost = "api.convyapp.com"
$adminHost = "admin.convyapp.com"
$authHost = "auth.convyapp.com"
$mcpHost = "mcp.convyapp.com"
$legalHost = "legal.convyapp.com"

scp -i "$env:USERPROFILE\.ssh\convy_vps_deploy" ..\..\ops\vps\bootstrap-server.sh "root@${ip}:/tmp/bootstrap-server.sh"
ssh -i "$env:USERPROFILE\.ssh\convy_vps_deploy" "root@${ip}" "bash /tmp/bootstrap-server.sh"
..\..\ops\vps\push-secrets.ps1 -HostName $ip -ConvyHostname $rootHost -ConvyApiHostname $apiHost -ConvyAdminHostname $adminHost -ConvyAuthHostname $authHost -ConvyMcpHostname $mcpHost -ConvyLegalHostname $legalHost
```

## First Deploy

```powershell
git archive --format=tar.gz -o "$env:TEMP\convy-release.tar.gz" HEAD
scp -i "$env:USERPROFILE\.ssh\convy_vps_deploy" "$env:TEMP\convy-release.tar.gz" "root@${ip}:/tmp/convy-release.tar.gz"
ssh -i "$env:USERPROFILE\.ssh\convy_vps_deploy" "root@${ip}" "mkdir -p /opt/convy/releases/manual && tar -xzf /tmp/convy-release.tar.gz -C /opt/convy/releases/manual && /opt/convy/releases/manual/ops/vps/deploy-release.sh manual"
```

The deploy script copies `legal/` to `/opt/convy/legal`, copies `public-site/` to `/opt/convy/public`, rebuilds `api`, `dashboard`, `auth`, and `mcp`, starts Compose, and checks API, Auth, and MCP health endpoints.
It also writes `/opt/convy/shared/release.env` with the release SHA, deploy timestamp, backend release label, and Android version parsed from `mobile/androidApp/build.gradle.kts`.

## Backups

Install backup timers after the first healthy deploy:

```bash
sudo /opt/convy/current/ops/vps/backups/install-backup-timers.sh
```

The daily backup timer creates PostgreSQL custom-format dumps with `pg_dump`, checksum metadata, `pg_restore --list` verification, and a `backup_runs` database row. The weekly restore verification timer restores the latest dump into a temporary database, runs a basic query, and drops the temporary database.
Admin users can download successful registered dumps from the dashboard. The backend only serves files recorded in `backup_runs` and resolved under `/opt/convy/backups/postgres`.

Manual commands:

```bash
sudo BACKUP_TYPE=Manual /opt/convy/current/ops/vps/backups/backup-postgres.sh
sudo /opt/convy/current/ops/vps/backups/verify-backup.sh /opt/convy/backups/postgres/daily/<file>.dump
sudo /opt/convy/current/ops/vps/backups/restore-postgres.sh /opt/convy/backups/postgres/daily/<file>.dump convy_restore_manual
```

Before non-internal users are onboarded, add encrypted offsite backup storage.

## GitHub CD

The CD workflow is provider-neutral and deploys the commit that passed CI.

Set these repository secrets:

- `STAGING_DEPLOY_HOST`: public IPv4 address or hostname of the VPS.
- `STAGING_PUBLIC_HOSTNAME`: API HTTPS hostname.
- `STAGING_SSH_PRIVATE_KEY`: private deploy key matching the public key provisioned in Hetzner.

Set these repository variables:

- `STAGING_DEPLOY_USER`: `root` for Hetzner.
- `STAGING_DEPLOY_SCRIPT`: `ops/vps/deploy-release.sh` for Hetzner.

## Smoke Checks

```powershell
curl.exe -fsS "https://$apiHost/health"
curl.exe -fsS "https://$apiHost/health/ready"
curl.exe -fsS "https://$authHost/health"
curl.exe -fsS "https://$mcpHost/health"
curl.exe -fsS "https://$mcpHost/.well-known/oauth-protected-resource"
curl.exe -fsS "https://$legalHost/privacy"
curl.exe -fsS "https://$rootHost"
curl.exe -I "https://$adminHost"
curl.exe -fsS "https://178.105.70.69.nip.io/health/ready"
```

Expected admin behavior:

- Caddy prompts for Basic Auth before the dashboard loads.
- The dashboard then requires Firebase login.
- The backend admin API returns `401` without a token and `403` for authenticated non-admin users.
- Dashboard views may display aggregate OpenAI request, token, latency, and estimated cost metrics, but must not display prompts, transcripts, audio, or parsed product names.

## Data Migration

Use a short write-freeze process:

1. Confirm the Hetzner deployment is healthy with an empty database.
2. Disable writes or scale down the previous live service.
3. Export the previous PostgreSQL database with `pg_dump`.
4. Copy the dump to the VPS.
5. Restore into `convy-db`.
6. Compare row counts for `users`, `households`, `household_memberships`, `household_lists`, `list_items`, `task_items`, `invites`, `activity_logs`, `device_tokens`, and `notification_preferences`.
7. Validate login, household loading, item creation, task creation, notification preferences, SignalR updates, FCM push, and voice parsing.
8. Keep the previous environment for rollback until the mobile app has used Hetzner successfully.
