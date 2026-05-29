# Backup And Restore Runbook

Backups currently run on the Hetzner VPS and store PostgreSQL custom-format dumps locally under `/opt/convy/backups/postgres`.

Offsite encrypted backups are a required future hardening step before broader public onboarding.

## Files

| File | Purpose |
| --- | --- |
| `ops/vps/backups/install-backup-timers.sh` | Installs daily backup and weekly restore-verification timers. |
| `ops/vps/backups/backup-postgres.sh` | Creates a PostgreSQL custom-format dump and records metadata. |
| `ops/vps/backups/verify-backup.sh` | Runs `pg_restore --list` against a dump. |
| `ops/vps/backups/restore-postgres.sh` | Restores a dump into a target database. |
| `ops/vps/backups/restore-verify-postgres.sh` | Restores latest dump into a temporary database and checks it. |
| `ops/vps/backups/prune-backups.sh` | Removes old backup files according to retention policy. |

## Install Timers

Run after the first healthy deploy:

```bash
sudo /opt/convy/current/ops/vps/backups/install-backup-timers.sh
systemctl list-timers convy-backup.timer convy-restore-verify.timer
```

## Manual Backup

```bash
sudo BACKUP_TYPE=Manual /opt/convy/current/ops/vps/backups/backup-postgres.sh
```

Expected:

- dump file under `/opt/convy/backups/postgres`
- checksum metadata
- `pg_restore --list` verification
- `backup_runs` row in PostgreSQL

## Verify Dump Catalog

```bash
sudo /opt/convy/current/ops/vps/backups/verify-backup.sh /opt/convy/backups/postgres/daily/<file>.dump
```

## Restore Into Temporary Database

```bash
sudo /opt/convy/current/ops/vps/backups/restore-postgres.sh /opt/convy/backups/postgres/daily/<file>.dump convy_restore_manual
```

After inspection, drop the temporary database if it is no longer needed.

## Scheduled Restore Verification

```bash
sudo systemctl status convy-restore-verify.timer
sudo systemctl status convy-restore-verify.service
sudo journalctl -u convy-restore-verify.service -n 100 --no-pager
```

The restore verification job restores the latest dump into a temporary database, runs a basic query, and drops the temporary database.

## Dashboard Download

Admin users can download successful registered dumps from the dashboard. The backend only serves files:

- recorded in `backup_runs`
- marked as successful
- resolved under the configured backup root

## Restore During Incident

1. Stop writes or take the API offline.
2. Identify the latest verified backup.
3. Copy the dump to a safe restore location if needed.
4. Restore into a temporary database first.
5. Compare row counts for core tables.
6. Restore into the production database only after confirming the target state and downtime window.
7. Run API, auth, MCP, dashboard, mobile, and backup smoke checks.

Core row-count tables:

- `users`
- `households`
- `household_memberships`
- `household_lists`
- `list_items`
- `task_items`
- `invites`
- `activity_logs`
- `device_tokens`
- `notification_preferences`
- `voice_parse_events`
- `ai_usage_events`
- `backup_runs`
- `mcp_oauth_refresh_tokens`
- `mcp_oauth_consents`
- `mcp_tool_invocations`
- `mcp_idempotency_records`

## Risks

- Local-only backups do not protect against VPS loss.
- Backup files may contain household data and must be handled as sensitive data.
- Schema rollback may not be safe after migrations.
- Admin backup download must remain guarded by Basic Auth, Firebase login, and `AdminOnly`.
