# Operations

This page is the operational index for the Convy beta/staging environment.

## Runbooks

- [Deployment runbook](operations/deployment-runbook.md)
- [Hetzner VPS runbook](operations/hetzner-vps-runbook.md)
- [Backup and restore runbook](operations/backup-restore-runbook.md)
- [ChatGPT MCP operations runbook](operations/mcp-runbook.md)
- [Oracle Free Tier fallback runbook](operations/oracle-free-tier-runbook.md)

## Daily Checks

```bash
curl -fsS https://api.convyapp.com/health/ready
curl -fsS https://auth.convyapp.com/health
curl -fsS https://mcp.convyapp.com/health
curl -fsS https://mcp.convyapp.com/.well-known/oauth-protected-resource
curl -fsS https://legal.convyapp.com/privacy
curl -fsS https://convyapp.com
```

Check the dashboard for:

- API and system health
- OpenAI error rate, latency, token usage, and estimated cost
- MCP health, metadata reachability, tool invocations, and success rate
- latest backup status and restore verification

## Logs

On the VPS:

```bash
cd /opt/convy/current/docker
docker compose --env-file /opt/convy/shared/api.env -f docker-compose.vps.yml ps
docker compose --env-file /opt/convy/shared/api.env -f docker-compose.vps.yml logs --tail=200 api
docker compose --env-file /opt/convy/shared/api.env -f docker-compose.vps.yml logs --tail=200 mcp
docker compose --env-file /opt/convy/shared/api.env -f docker-compose.vps.yml logs --tail=200 auth
docker compose --env-file /opt/convy/shared/api.env -f docker-compose.vps.yml logs --tail=200 dashboard
docker compose --env-file /opt/convy/shared/api.env -f docker-compose.vps.yml logs --tail=200 caddy
```

Do not copy logs containing tokens, credentials, Firebase ID tokens, or personal household content into issues or docs.

## Backups

Backups run locally under:

```text
/opt/convy/backups/postgres
```

Primary scripts:

- `ops/vps/backups/backup-postgres.sh`
- `ops/vps/backups/verify-backup.sh`
- `ops/vps/backups/restore-postgres.sh`
- `ops/vps/backups/restore-verify-postgres.sh`
- `ops/vps/backups/prune-backups.sh`
- `ops/vps/backups/install-backup-timers.sh`

See [backup and restore runbook](operations/backup-restore-runbook.md).

## DNS And Caddy

Caddy serves public, API, admin, auth, MCP, legal, and legacy hosts from `docker/Caddyfile.vps`. When changing domains:

1. Update DNS first.
2. Push updated hostname secrets with `ops/vps/push-secrets.ps1`.
3. Redeploy.
4. Verify all public and legacy health checks.
5. Update [DEPLOYMENT.md](DEPLOYMENT.md), [domain-cutover.md](mcp/domain-cutover.md), and legal/public links.

## MCP Emergency Disable

To disable ChatGPT MCP quickly:

1. Remove or disable the `CONVY_MCP_HOSTNAME` Caddy route, or stop the `mcp` service.
2. Keep the API and auth app available long enough for revocation and diagnostics if possible.
3. Rotate `McpAuth__PrivateKeyPemBase64` / `McpAuth__PublicKeyPemBase64` if key exposure is suspected.
4. Rotate `McpAudit__ApiKey` if audit service key exposure is suspected.
5. Review `mcp_tool_invocations` for unexpected tool activity.

## Secret Rotation

Rotate secrets through `ops/vps/push-secrets.ps1` and redeploy. Never edit committed `.env*.example` files with real values.

Common rotations:

- OpenAI API key
- Firebase web config values when required by Firebase project changes
- Caddy Basic Auth hash
- Admin allowed emails
- MCP RSA key pair
- MCP audit API key
- PostgreSQL password, with planned downtime and connection string update

## Monitoring Gaps

The current beta relies on health endpoints, dashboard views, logs, and scheduled backup verification. External alerting and encrypted offsite backups are required before broader public onboarding.
