"""Installed Linux broker policy; never imported from an uploaded release."""
from contextlib import contextmanager
import fcntl
import importlib.util
import json
import os
from pathlib import Path
import shutil
import subprocess
import time
import uuid
from datetime import datetime, timezone

from release_common import ReleaseError, canonical, file_digest, json_read, private_write, require, run, verify_bundle
from staging_common import REPOSITORY, SIGNER, verify_ci

spec = importlib.util.spec_from_file_location('release', Path(__file__).with_name('safe-release.py'))
release = importlib.util.module_from_spec(spec)
spec.loader.exec_module(release)


@contextmanager
def host_lock(path):
    path = Path(path)
    require(path.is_absolute() and path.parent.is_dir() and not path.is_symlink(), 'invalid_shared_lock')
    fd = os.open(path, os.O_RDWR | os.O_CREAT | os.O_NOFOLLOW, 0o600)
    with os.fdopen(fd, 'a') as lock:
        require(os.fstat(lock.fileno()).st_uid == os.geteuid() and os.fstat(lock.fileno()).st_mode & 0o077 == 0, 'unprotected_shared_lock')
        try:
            fcntl.flock(lock, fcntl.LOCK_EX | fcntl.LOCK_NB)
        except BlockingIOError:
            raise ReleaseError('shared_host_busy_retry_later') from None
        yield


def tree_bytes(path):
    path = Path(path)
    if not path.exists():
        return 0
    require(not path.is_symlink(), 'managed_path_symlink')
    # Count allocated bytes once per inode (candidate archive is hard linked).
    seen = set()
    size = 0
    for item in path.rglob('*'):
        require(not item.is_symlink(), 'managed_tree_symlink')
        if item.is_file():
            stat = item.stat()
            key = (stat.st_dev, stat.st_ino)
            if key not in seen:
                size += stat.st_blocks * 512
                seen.add(key)
    return size


def resources(profile):
    usage = shutil.disk_usage(Path(profile['current']).parent)
    memory = {line.split(':')[0]: int(line.split()[1]) * 1024 for line in Path('/proc/meminfo').read_text().splitlines() if line.startswith(('MemAvailable:', 'MemTotal:'))}
    live = release.containers()
    logs = sum(Path(c['LogPath']).stat().st_size for c in live.values() if c.get('LogPath') and Path(c['LogPath']).is_file())
    return {'freeDiskBytes': usage.free, 'totalDiskBytes': usage.total, 'memory': memory,
            'dockerUsage': run(['docker', 'system', 'df', '--format', '{{json .}}']).decode().splitlines(),
            'releaseBytes': tree_bytes(profile['stateRoot']), 'incomingBytes': tree_bytes(profile['incomingRoot']),
            'containerLogBytes': logs, 'backupBytes': tree_bytes(profile['backupRoot'])}


def capacity(profile, incoming, image_bytes=0, rollback_bytes=0, source_bytes=0, backup_bytes=0, retained_candidate_bytes=0):
    measurements = resources(profile)
    require(incoming <= profile['maximumIncomingBytes'], 'incoming_artifact_exceeds_budget')
    required = profile['minimumFreeBytes'] + incoming + image_bytes + rollback_bytes + source_bytes + backup_bytes
    require(measurements['freeDiskBytes'] >= required, 'insufficient_disk_capacity')
    require(measurements['memory']['MemAvailable'] >= profile['minimumAvailableMemoryBytes'], 'insufficient_ram_capacity')
    require(measurements['releaseBytes'] + incoming + retained_candidate_bytes + rollback_bytes + source_bytes <= profile['maximumReleaseBytes'], 'release_retention_budget_exceeded')
    return measurements | {'requiredFreeBytes': required}


def protected_states(profile):
    root = Path(profile['stateRoot'])
    active = json_read(root / 'active.json') if (root / 'active.json').exists() else None
    keep = set()
    if active:
        path = Path(active['statePath'])
        require(path.parent == root and not path.is_symlink(), 'active_state_outside_root')
        keep.add(path)
        state = release.recovery(path)
        if state['previousActive']:
            previous_path = Path(state['previousActive']['statePath'])
            if previous_path.is_dir():
                keep.add(previous_path)
            else:
                require('previous-source.tar' in state['recoveryFiles'], 'designated_rollback_source_missing')
    states = []
    for path in root.iterdir() if root.exists() else []:
        if not path.is_dir():
            continue
        require(not path.is_symlink() and path.parent == root, 'unsafe_retention_path')
        state = release.recovery(path)
        states.append((path, state))
        if state['status'] not in ('ACCEPTED', 'ROLLED_BACK'):
            keep.add(path)
    failed = sorted((p for p, s in states if s['status'] == 'ROLLED_BACK'), key=lambda p: p.stat().st_mtime, reverse=True)
    keep.update(p for p in failed[:profile['failedReleaseCount']] if time.time() - p.stat().st_mtime <= profile['failedReleaseMaxAgeSeconds'])
    for path in keep:
        require(path.parent == root and path.is_dir(), 'protected_rollback_state_missing')
        release.recovery(path)
    return states, keep


def maintenance(profile, live_bundle=None, pressure=False):
    """Called only under the shared lock. Never prune a daemon, volume or backup."""
    states, keep = protected_states(profile)
    if pressure:
        # Failed, successfully rolled-back debug journals are optional. Current,
        # designated rollback and unresolved recovery remain mandatory.
        keep = {p for p in keep if next(s for path, s in states if path == p)['status'] != 'ROLLED_BACK'}
        active_file = Path(profile['stateRoot']) / 'active.json'
        if active_file.exists():
            active_path = Path(json_read(active_file)['statePath'])
            keep.add(active_path)
            previous = release.recovery(active_path)['previousActive']
            if previous and Path(previous['statePath']).is_dir():
                keep.add(Path(previous['statePath']))
    protected_images = {c['Image'] for c in release.containers().values()}  # all products, stopped containers too
    def image_ids(path, state):
        config = json_read(path / 'candidate-compose.json')
        manifest = json_read(path / 'release.json')
        return (set(state['previousImages'].values()) | {config['services'][s]['image'] for s in state['services']} |
                set(manifest.get('configIds', {}).values()) | set(manifest['images'].values()))
    for path, state in states:
        if path in keep:
            protected_images.update(image_ids(path, state))
    ledger = Path(profile['stateRoot']) / 'cd-owned-images.json'
    if ledger.with_name(ledger.name + '.tmp').exists():
        release.protected(ledger.with_name(ledger.name + '.tmp'))
        ledger.with_name(ledger.name + '.tmp').unlink()
    candidates = set(json_read(ledger)) if ledger.exists() else set()
    removed = []
    for path, state in states:
        candidates.update(image_ids(path, state))
    private_write(ledger, canonical(sorted(candidates)))
    for path, state in states:
        if path not in keep:
            shutil.rmtree(path)
            removed.append(path.name)
    image_results = []
    remaining = set(candidates)
    for image in sorted(candidates - protected_images):
        # Recheck immediately before removing. Unknown/unowned daemon images are never enumerated for deletion.
        require(image.startswith('sha256:') and len(image) == 71, 'invalid_cleanup_image')
        if image in {c['Image'] for c in release.containers().values()}:
            continue
        result = subprocess.run(['docker', 'image', 'rm', image], capture_output=True, timeout=60)
        image_results.append({'image': image, 'removed': result.returncode == 0})
        if result.returncode == 0 or subprocess.run(['docker', 'image', 'inspect', image], capture_output=True).returncode != 0:
            remaining.discard(image)
    private_write(ledger, canonical(sorted(remaining)))
    incoming = Path(profile['incomingRoot'])
    if incoming.exists():
        for path in incoming.iterdir():
            if live_bundle is not None and path == Path(live_bundle):
                continue
            require(path.is_dir() and not path.is_symlink() and path.name.startswith('incoming-'), 'unknown_incoming_entry')
            # Live receiver holds this same lock, so these are interrupted transfers.
            tree_bytes(path)
            shutil.rmtree(path)
            removed.append(path.name)
    return {'removedArchives': removed, 'images': image_results, 'protectedStates': len(keep)}


def recover_interrupted(profile):
    root = Path(profile['stateRoot'])
    for path in sorted(root.glob('*/state.json')):
        state = release.recovery(path.parent)
        if state['status'] in ('PREPARING', 'PREPARED', 'ACTIVATING', 'RECOVERY_REQUIRED'):
            # A partial archive write before PREPARED has not changed app containers.
            require(state['status'] != 'PREPARING' or not state['previousImages'] or 'previous-images.tar' in state['recoveryFiles'], 'incomplete_preparation_requires_operator_recovery')
            release.restore(path.parent, profile)


def host_profile(path):
    profile = release.read_profile(path)
    require(profile.get('automaticEnabled') is True and profile.get('modelPolicy') == 'preserve', 'first_activation_required')
    require(profile.get('sharedLock') == '/run/lock/shared-staging-deployment.lock', 'shared_lock_contract_required')
    root = Path(profile['current']).parent
    require(Path(profile['incomingRoot']) == root / 'cd-incoming', 'invalid_incoming_root')
    require(Path(profile['backupRoot']) == root / 'backups', 'invalid_backup_root')
    require(profile['minimumFreeBytes'] >= 8 * 1024**3 and profile['minimumAvailableMemoryBytes'] >= 1024**3, 'resource_reserve_not_reviewed')
    require(profile['failedReleaseCount'] == 2 and profile['failedReleaseMaxAgeSeconds'] == 86400, 'retention_policy_not_reviewed')
    require(profile['maximumIncomingBytes'] <= 2 * 1024**3 and profile['maximumReleaseBytes'] <= 3 * 1024**3, 'artifact_budget_not_reviewed')
    require(0 < profile['maximumRollbackArchiveBytes'] <= 1024**3, 'rollback_archive_budget_not_reviewed')
    require(set(profile.get('applicationCaps', {})) == set(release.SERVICES) and
            all(0 < v['memoryBytes'] <= 512 * 1024**2 and 0 < v['cpus'] <= 1 for v in profile['applicationCaps'].values()), 'application_resource_caps_required')
    require(profile.get('applicationLogOptions') == {'max-size': '10m', 'max-file': '3'}, 'application_log_rotation_required')
    for key in ('githubTokenFile',):
        release.protected(profile[key])
    return profile


def provenance(bundle, request, profile):
    token = Path(profile['githubTokenFile']).read_text().strip()
    environment = {**os.environ, 'GH_TOKEN': token}
    argv = ['gh', 'attestation', 'verify', str(Path(bundle) / 'release.json'), '--repo', REPOSITORY,
            '--bundle', str(Path(bundle) / 'attestation.json'),
            '--cert-identity', 'https://github.com/' + SIGNER + '@refs/heads/master',
            '--signer-digest', request['sourceSha'], '--source-digest', request['sourceSha'],
            '--source-ref', 'refs/heads/master', '--deny-self-hosted-runners', '--format', 'json']
    try:
        result = subprocess.run(argv, capture_output=True, timeout=60, env=environment)
        require(result.returncode == 0, 'artifact_provenance_failed')
        verified = json.loads(result.stdout)
        require(bool(verified), 'artifact_attestation_missing')
    except (OSError, ValueError, subprocess.TimeoutExpired):
        raise ReleaseError('artifact_provenance_unavailable') from None
    manifest = verify_bundle(bundle, Path(bundle, 'release.sha256').read_text().strip())
    require(manifest.get('ciReceipt') == request, 'signed_ci_receipt_mismatch')
    require(manifest['sourceSha'] == request['sourceSha'], 'artifact_source_mismatch')
    return manifest


def refresh_recovery(profile):
    """Fresh pg_dump plus a real restore in a separate bounded, networkless container."""
    config = release.effective(profile)
    name = config['services']['db']['container_name']
    database_bytes = int(run(['docker', 'exec', name, 'sh', '-c', 'exec psql -U "$POSTGRES_USER" -d "$POSTGRES_DB" -At -c "SELECT pg_database_size(current_database());"'], timeout=20).strip())
    require(database_bytes <= 64 * 1024**2, 'database_exceeds_reviewed_restore_envelope')
    capacity(profile, 0, backup_bytes=database_bytes * 4)
    db = release.containers()[name]
    image = db['Image']
    prefix = Path(profile['backupRoot']) / 'cd'
    prefix.mkdir(mode=0o700, parents=True, exist_ok=True)
    # Two rotating verified copies; publish the proof only after restore and offsite checks.
    for pending in prefix.glob('pending-*.dump'):
        require(not pending.is_symlink(), 'pending_backup_symlink')
        pending.unlink()
    target = prefix / ('pending-' + str(time.time_ns()) + '.dump')
    fd = os.open(target, os.O_WRONLY | os.O_CREAT | os.O_EXCL, 0o600)
    with os.fdopen(fd, 'wb') as output:
        result = subprocess.run(['docker', 'exec', name, 'sh', '-c', 'exec pg_dump -U "$POSTGRES_USER" -d "$POSTGRES_DB" -Fc --no-owner --no-acl'], stdout=output, stderr=subprocess.PIPE, timeout=120)
    require(result.returncode == 0, 'fresh_backup_failed')
    checksum = file_digest(target)
    restore_name = 'convy-cd-restore-' + uuid.uuid4().hex[:12]
    try:
        run(['docker', 'run', '-d', '--name', restore_name, '--network', 'none', '--memory', '256m', '--cpus', '0.5', '--pids-limit', '128',
             '--read-only', '--tmpfs', '/var/lib/postgresql/data:rw,size=256m', '--tmpfs', '/var/run/postgresql:rw,size=16m',
             '-e', 'POSTGRES_HOST_AUTH_METHOD=trust', image], timeout=60)
        deadline = time.monotonic() + 30
        while True:
            try:
                run(['docker', 'exec', restore_name, 'pg_isready', '-h', '127.0.0.1', '-U', 'postgres'], timeout=5)
                break
            except ReleaseError:
                require(time.monotonic() < deadline, 'isolated_restore_start_failed')
                time.sleep(1)
        run(['docker', 'exec', '-i', restore_name, 'pg_restore', '-U', 'postgres', '-d', 'postgres', '--no-owner', '--no-acl', '--exit-on-error'], target, timeout=120)
        restored = run(['docker', 'exec', restore_name, 'psql', '-U', 'postgres', '-d', 'postgres', '-At', '-c', 'SELECT "MigrationId" FROM "__EFMigrationsHistory" ORDER BY "MigrationId";'], timeout=20).decode().splitlines()
        require(restored == release.database_history(config), 'isolated_restore_schema_mismatch')
    finally:
        run(['docker', 'container', 'rm', '-f', restore_name], timeout=30)
    # Fixed, root-reviewed export command; cannot originate in the incoming source.
    receipt = json.loads(run(profile['offsiteExportCommand'] + [str(target)], timeout=120))
    require(receipt.get('verifiedEncryptedRestore') is True and receipt.get('backupSha256') == checksum, 'offsite_restore_not_verified')
    private_write(profile['offsiteReceiptFile'], canonical(receipt))
    accepted = prefix / target.name.replace('pending-', 'backup-')
    os.replace(target, accepted)
    target = accepted
    proof = prefix / 'restore-proof.json'
    private_write(proof, canonical({'backupSha256': checksum, 'isolatedRestoreSucceeded': True, 'verifiedAtUtc': datetime.now(timezone.utc).isoformat(),
                                   'offsiteReceiptSha256': file_digest(profile['offsiteReceiptFile'])}))
    profile.update({'backupFile': str(target), 'backupSha256': checksum, 'restoreProofFile': str(proof)})
    # Backup retention is separate from image maintenance, only these CD-owned files.
    for old in sorted(prefix.glob('backup-*.dump'), key=lambda p: p.stat().st_mtime, reverse=True)[2:]:
        require(not old.is_symlink(), 'backup_symlink')
        old.unlink()


def automatic(profile_path, bundle, request, verify=verify_ci, attest=provenance, backup=refresh_recovery):
    """Caller holds shared lock, including transfer. Injectable boundaries are fixture-only."""
    profile = release.read_profile(profile_path)
    require(profile.get('automaticEnabled') is True and profile.get('modelPolicy') == 'preserve', 'first_activation_required')
    token = Path(profile['githubTokenFile']).read_text().strip()
    recover_interrupted(profile)
    active_file = Path(profile['stateRoot']) / 'cd-active.json'
    active = json_read(active_file) if active_file.exists() else None
    status = verify(request, token, active)
    if status == 'ALREADY_ACCEPTED':
        return {'status': status, 'sourceSha': request['sourceSha']}
    manifest = attest(bundle, request, profile)
    require(set(manifest.get('imageSizes', {})) == set(manifest['images']) and all(isinstance(s, int) and s > 0 for s in manifest['imageSizes'].values()), 'image_expansion_sizes_missing')
    def check_capacity():
        return capacity(profile, 0, manifest['imageStorageBytes'], profile['maximumRollbackArchiveBytes'],
                        Path(bundle, 'source.tar').stat().st_size * 2, retained_candidate_bytes=Path(bundle, 'images.tar').stat().st_size)
    try:
        measurements = check_capacity()
    except ReleaseError as failure:
        if str(failure) not in ('insufficient_disk_capacity', 'release_retention_budget_exceeded'):
            raise
        maintenance(profile, bundle, pressure=True)
        measurements = check_capacity()
    # The host profile pins compatibility/topology before taking a database backup.
    require(manifest['migrationSha256'] == profile['migrationSha256'] and manifest['baselineSha'] == profile['schemaBaselineSha'], 'incompatible_schema')
    require(manifest['composeSha256'] == profile['composeSha256'], 'candidate_compose_not_equivalent')
    backup(profile)
    temporary = Path(profile['current']).parent / 'cd-effective-profile.json'
    private_write(temporary, canonical(profile))
    try:
        planned = release.plan(temporary, bundle, file_digest(Path(bundle) / 'release.json'))
        verify(request, token, active)  # latest master/CI check immediately before activation
        capacity(profile, 0, manifest['imageStorageBytes'])
        result = release.apply(temporary, bundle, file_digest(Path(bundle) / 'release.json'), release.summary(planned)['approvalDigest'])
        accepted_images = {s: release.containers()[planned[3]['services'][s]['container_name']]['Image'] for s in manifest['images']}
        state_path = result.get('statePath') or json_read(Path(profile['stateRoot']) / 'active.json')['statePath']
        private_write(active_file, canonical(request | {'images': accepted_images, 'statePath': state_path}))
        maintenance_result = maintenance(profile, bundle)
        after = resources(profile)
        record = result | {'ci': request, 'images': accepted_images, 'builtImages': manifest['images'], 'before': measurements, 'after': after,
                           'recovery': {'backupSha256': profile['backupSha256'], 'restoreProofSha256': file_digest(profile['restoreProofFile'])}, 'maintenance': maintenance_result}
        private_write(Path(profile['stateRoot']) / 'cd-last-result.json', canonical(record))
        return record
    except Exception as error:
        reason = str(error) if isinstance(error, ReleaseError) else 'automatic_release_failed_output_withheld'
        record = {'status': 'FAILED', 'reason': reason, 'ci': request, 'before': measurements,
                  'after': resources(profile), 'journals': [{'state': p.parent.name, 'status': json_read(p)['status']}
                   for p in Path(profile['stateRoot']).glob('*/state.json')]}
        private_write(Path(profile['stateRoot']) / 'cd-last-result.json', canonical(record))
        raise
    finally:
        temporary.unlink(missing_ok=True)
