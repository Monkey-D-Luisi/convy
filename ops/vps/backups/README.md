# VPS PostgreSQL Backups

Backups run locally on the Hetzner VPS and write PostgreSQL custom-format dumps under `/opt/convy/backups/postgres`.

Install timers after the first successful deploy:

```bash
sudo /opt/convy/current/ops/vps/backups/install-backup-timers.sh
```

Manual backup:

```bash
sudo BACKUP_TYPE=Manual /opt/convy/current/ops/vps/backups/backup-postgres.sh
```

Verify a dump catalog:

```bash
sudo /opt/convy/current/ops/vps/backups/verify-backup.sh /opt/convy/backups/postgres/daily/<file>.dump
```

Restore into a temporary database:

```bash
sudo /opt/convy/current/ops/vps/backups/restore-postgres.sh /opt/convy/backups/postgres/daily/<file>.dump convy_restore_manual
```

The weekly `convy-restore-verify.timer` performs a real restore into a temporary database and drops it after a basic query succeeds.
