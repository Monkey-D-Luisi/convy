# Hetzner VPS Hosting Runbook

Hetzner is the active controlled-release/staging hosting target. The VPS runs PostgreSQL, ASP.NET Core API, Next.js dashboard, Next.js auth app, Node MCP service, static public/legal pages, and Caddy through Docker Compose.

OCI files are fallback/reference material unless explicitly updated for parity.

## Infrastructure Layout

- `infra/hetzner`: Hetzner Cloud Terraform stack.
- `ops/hetzner/invoke-terraform.ps1`: Terraform helper that loads the Hetzner token outside the repo.
- `ops/vps`: VPS bootstrap, secret push, release deployment, and backup helpers.
- `docker/docker-compose.vps.yml`: runtime services for `db`, `api`, `dashboard`, `auth`, `mcp`, and `caddy`.
- `docker/Caddyfile.vps`: public routing for root, API, admin dashboard, OAuth auth, MCP, legal pages, and legacy hosts.
- `public-site`: copied to `/opt/convy/public`.
- `legal`: copied to `/opt/convy/legal`.

## Required Hosts

Configure DNS before the first deploy:

- `CONVY_PUBLIC_HOSTNAME`: `convyapp.com`
- `CONVY_WWW_HOSTNAME`: `www.convyapp.com`
- `CONVY_API_HOSTNAME`: `api.convyapp.com`
- `CONVY_ADMIN_HOSTNAME`: `admin.convyapp.com`
- `CONVY_AUTH_HOSTNAME`: `auth.convyapp.com`
- `CONVY_MCP_HOSTNAME`: `mcp.convyapp.com`
- `CONVY_LEGAL_HOSTNAME`: `legal.convyapp.com`

Legacy `178.105.70.69.nip.io` hosts stay configured for already installed staging Android builds and cutover safety.

## Provision

Store the Hetzner token outside the repo:

```powershell
New-Item -ItemType Directory -Force "$env:USERPROFILE\.config\convy\secrets"
Set-Content -Path "$env:USERPROFILE\.config\convy\secrets\hcloud-token.txt" -Value "<token>"
```

Plan and apply:

```powershell
cd infra/hetzner
Copy-Item terraform.tfvars.example terraform.tfvars
..\..\ops\hetzner\invoke-terraform.ps1 init
..\..\ops\hetzner\invoke-terraform.ps1 plan
..\..\ops\hetzner\invoke-terraform.ps1 apply
```

Do not apply until server type, location, SSH key, and expected cost are accepted.

## Bootstrap

```powershell
$ip = terraform output -raw public_ip
scp -i "$env:USERPROFILE\.ssh\convy_vps_deploy" ..\..\ops\vps\bootstrap-server.sh "root@${ip}:/tmp/bootstrap-server.sh"
ssh -i "$env:USERPROFILE\.ssh\convy_vps_deploy" "root@${ip}" "bash /tmp/bootstrap-server.sh"
```

The CD workflow can create/use the non-root deploy user `convy-deploy`.

## Secrets

Generate Caddy Basic Auth hash:

```powershell
docker run --rm caddy:2.10.0-alpine caddy hash-password --plaintext "<admin-password>"
```

Push secrets:

```powershell
$env:OPENAI_API_KEY = "<openai-api-key>"
$env:ADMIN_BASIC_AUTH_USER = "admin"
$env:ADMIN_BASIC_AUTH_HASH = "<caddy-hash>"
$env:ADMIN_ALLOWED_EMAILS = "admin@example.com"
$env:FIREBASE_WEB_API_KEY = "<firebase-web-api-key>"
$env:FIREBASE_AUTH_DOMAIN = "convy-6520d.firebaseapp.com"
$env:FIREBASE_WEB_APP_ID = "<firebase-web-app-id>"

.\ops\vps\push-secrets.ps1 `
  -HostName $ip `
  -ConvyHostname "convyapp.com" `
  -ConvyApiHostname "api.convyapp.com" `
  -ConvyAdminHostname "admin.convyapp.com" `
  -ConvyAuthHostname "auth.convyapp.com" `
  -ConvyMcpHostname "mcp.convyapp.com" `
  -ConvyLegalHostname "legal.convyapp.com"
```

Optional OpenAI cost settings are supported through `OPENAI_COST_*` environment variables. If omitted, cost estimates can be `null` while counts and latency still appear.

## Deploy

Use [deployment-runbook.md](deployment-runbook.md) for GitHub Actions and manual deploy details.

## Backups

Install backup timers after the first healthy deploy:

```bash
sudo /opt/convy/current/ops/vps/backups/install-backup-timers.sh
```

Manual backup and verification:

```bash
sudo BACKUP_TYPE=Manual /opt/convy/current/ops/vps/backups/backup-postgres.sh
sudo /opt/convy/current/ops/vps/backups/verify-backup.sh /opt/convy/backups/postgres/daily/<file>.dump
sudo /opt/convy/current/ops/vps/backups/restore-postgres.sh /opt/convy/backups/postgres/daily/<file>.dump convy_restore_manual
```

See [backup-restore-runbook.md](backup-restore-runbook.md).

## Health Checks

```bash
curl -fsS https://api.convyapp.com/health/ready
curl -fsS https://auth.convyapp.com/health
curl -fsS https://mcp.convyapp.com/health
curl -fsS https://mcp.convyapp.com/.well-known/oauth-protected-resource
curl -fsS https://legal.convyapp.com/privacy
curl -fsS https://convyapp.com
curl -I https://admin.convyapp.com
curl -fsS https://178.105.70.69.nip.io/health/ready
```

## Logs

```bash
cd /opt/convy/current/docker
docker compose --env-file /opt/convy/shared/api.env -f docker-compose.vps.yml ps
docker compose --env-file /opt/convy/shared/api.env -f docker-compose.vps.yml logs --tail=200 api
docker compose --env-file /opt/convy/shared/api.env -f docker-compose.vps.yml logs --tail=200 mcp
docker compose --env-file /opt/convy/shared/api.env -f docker-compose.vps.yml logs --tail=200 caddy
```

## Key Rotation

MCP key rotation:

1. Generate or provide new `MCP_AUTH_PRIVATE_KEY_PEM_BASE64` and `MCP_AUTH_PUBLIC_KEY_PEM_BASE64`.
2. Push secrets.
3. Redeploy API and MCP together.
4. Existing short-lived access tokens stop validating; refresh tokens can mint new access tokens unless revoked.

Audit key rotation:

1. Set a new `CONVY_MCP_AUDIT_API_KEY`.
2. Push secrets.
3. Redeploy API and MCP.

## Emergency Disable MCP

Fastest options:

- stop the `mcp` service
- remove/disable the Caddy MCP route
- rotate MCP RSA keys if private key exposure is suspected
- revoke affected refresh tokens

Keep API/auth online when possible so revocation and diagnostics continue to work.
