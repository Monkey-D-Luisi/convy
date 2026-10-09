#!/usr/bin/env python3
"""Encrypted restic upload AND retrieval of this exact fresh database dump."""
import argparse
import json
import os
from pathlib import Path
import subprocess
from datetime import datetime, timezone

from release_common import ReleaseError, file_digest, json_read, require


def export(config_path, backup):
    config_path = Path(config_path)
    require(not config_path.is_symlink() and config_path.stat().st_uid == os.geteuid() and config_path.stat().st_mode & 0o077 == 0, 'offsite_config_not_private')
    config = json_read(config_path)
    backup = Path(backup)
    require(backup.is_file() and not backup.is_symlink() and backup.parent == Path(config['backupDirectory']), 'offsite_backup_outside_scope')
    require(config.get('offHost') is True, 'offsite_endpoint_not_reviewed')
    environment = {**os.environ, **config['environment']}
    require(bool(environment.get('RESTIC_REPOSITORY')) and bool(environment.get('RESTIC_PASSWORD_FILE')), 'encrypted_repository_required')
    password = Path(environment['RESTIC_PASSWORD_FILE'])
    require(not password.is_symlink() and password.stat().st_uid == os.geteuid() and password.stat().st_mode & 0o077 == 0, 'restic_password_not_private')
    try:
        result = subprocess.run(['restic', 'backup', '--json', '--tag', 'convy-cd', str(backup)], capture_output=True, env=environment, timeout=90)
        require(result.returncode == 0, 'offsite_upload_failed')
        summaries = [json.loads(line) for line in result.stdout.splitlines() if json.loads(line).get('message_type') == 'summary']
        snapshot = summaries[-1]['snapshot_id']
        output = backup.with_suffix('.retrieved')
        try:
            fd = os.open(output, os.O_WRONLY | os.O_CREAT | os.O_EXCL, 0o600)
            with os.fdopen(fd, 'wb') as stream:
                restored = subprocess.run(['restic', 'dump', snapshot, str(backup)], stdout=stream, stderr=subprocess.PIPE, env=environment, timeout=60)
            require(restored.returncode == 0 and file_digest(output) == file_digest(backup), 'offsite_retrieval_checksum_mismatch')
        finally:
            output.unlink(missing_ok=True)
        # This tag/path family only. The existing daily/weekly/monthly policy remains separate.
        retention = subprocess.run(['restic', 'forget', '--tag', 'convy-cd', '--group-by', 'host,tags', '--keep-last', '2', '--keep-daily', '7',
                                    '--keep-weekly', '5', '--keep-monthly', '4', '--prune'], capture_output=True, env=environment, timeout=90)
        require(retention.returncode == 0, 'offsite_retention_failed')
        return {'backupSha256': file_digest(backup), 'verifiedEncryptedRestore': True,
                'snapshotId': snapshot, 'verifiedAtUtc': datetime.now(timezone.utc).isoformat()}
    except (OSError, ValueError, IndexError, KeyError, subprocess.TimeoutExpired):
        raise ReleaseError('offsite_export_failed_output_withheld') from None


if __name__ == '__main__':
    parser = argparse.ArgumentParser()
    parser.add_argument('--config', required=True)
    parser.add_argument('backup')
    args = parser.parse_args()
    try:
        print(json.dumps(export(args.config, args.backup)))
    except Exception as error:
        print(json.dumps({'status': 'FAILED', 'reason': str(error) if isinstance(error, ReleaseError) else 'offsite_export_failed_output_withheld'}))
        raise SystemExit(1)
