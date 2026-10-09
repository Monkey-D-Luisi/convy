#!/usr/bin/env python3
"""Linux host transaction: plan, apply, recovery plan, and isolated rollback."""
import argparse
import copy
import fcntl
import json
import os
from pathlib import Path
import shutil
import time
from datetime import datetime, timezone

from release_common import (LEGACY_PRICE, MODEL_PATCH, SERVICES, ReleaseError, canonical,
                            digest, extract_source, file_digest, json_read, patch_model,
                            private_write, require, run, verify_bundle)

def dollars(value, encode):
    if isinstance(value, str):
        return value.replace('$', '$$') if encode else value.replace('$$', '$')
    if isinstance(value, list):
        return [dollars(v, encode) for v in value]
    if isinstance(value, dict):
        return {k: dollars(v, encode) for k, v in value.items()}
    return value

def compose(config, *args):
    return run(['docker', 'compose', '--project-name', config['name'], '-f', '-', *args], canonical(dollars(config, True)))

def containers():
    ids = run(['docker', 'ps', '-aq']).decode().split()
    values = json.loads(run(['docker', 'inspect', *ids])) if ids else []
    return {c['Name'].lstrip('/'): c for c in values}

def fingerprint(container):
    networks = copy.deepcopy(container['NetworkSettings'])
    for value in networks.get('Networks', {}).values():
        for key in ('Aliases', 'DNSNames'):
            if value.get(key):
                value[key] = sorted(value[key])
    return {k: container[k] for k in ('Id', 'Image', 'RestartCount')} | {
        'Mounts': sorted(container['Mounts'], key=lambda m: m['Destination']), 'NetworkSettings': networks,
        'startedAt': container['State']['StartedAt'], 'oom': container['State']['OOMKilled'],
        'status': container['State']['Status'], 'environment': digest(canonical(container['Config']['Env']))}

def protected(path):
    path = Path(path)
    require(path.is_file() and not path.is_symlink(), 'protected_file_missing_or_symlink')
    info = path.stat()
    require(info.st_uid == os.geteuid() and info.st_mode & 0o077 == 0, 'protected_file_permissions')

def read_profile(path):
    protected(path)
    profile = json_read(path)
    require(profile.get('format') == 1, 'unsupported_profile')
    require(Path(profile['current']).name == 'current' and
            Path(profile['stateRoot']).parent == Path(profile['current']).parent and
            Path(profile['modelEnv']).is_relative_to(Path(profile['current']).parent) and
            profile['modelEnv'] not in profile['preserveFiles'], 'write_paths_outside_application')
    for key in ('composeFiles', 'envFiles', 'preserveFiles', 'acceptance'):
        require(bool(profile.get(key)), 'incomplete_profile')
    for key in ('current', 'stateRoot', 'modelEnv'):
        require(Path(profile[key]).is_absolute(), 'absolute_profile_paths_required')
    require(profile['modelEnv'] in profile['envFiles'], 'model_env_not_in_profile')
    for source in profile['envFiles']:
        protected(source)
    for source in profile['composeFiles'] + profile['preserveFiles']:
        require(Path(source).is_absolute() and Path(source).is_file(), 'profile_source_missing')
    require(profile.get('composeHashes') == {p: file_digest(p) for p in profile['composeFiles']}, 'compose_sources_not_reviewed')
    require(not Path(profile['current']).exists() or Path(profile['current']).is_symlink(), 'current_must_be_symlink')
    require(1 <= profile.get('healthTimeoutSeconds', 120) <= 600, 'invalid_health_timeout')
    require(profile.get('minimumFreeBytes', 8 * 1024**3) >= 0, 'invalid_capacity_gate')
    return profile

def effective(profile):
    args = ['docker', 'compose', '--project-name', profile['project']]
    for path in profile['envFiles']:
        args += ['--env-file', path]
    for path in profile['composeFiles']:
        args += ['-f', path]
    config = dollars(json.loads(run(args + ['config', '--format', 'json'])), False)
    require(config['name'] == profile['project'] and {'api', 'db', 'caddy'} <= set(config['services']), 'unexpected_compose_project')
    require(file_digest(profile['composeFiles'][0]) == profile['composeSha256'], 'base_compose_not_reviewed')
    for network in config.get('networks', {}).values():
        run(['docker', 'network', 'inspect', network['name']])
        network['external'] = True
        network['ipam'] = {}
        network.pop('driver', None)
        network.pop('driver_opts', None)
    for volume in config.get('volumes', {}).values():
        run(['docker', 'volume', 'inspect', volume['name']])
        volume['external'] = True
        volume.pop('driver', None)
        volume.pop('driver_opts', None)
    return config

def adopt(config, live, profile):
    config = copy.deepcopy(config)
    active = Path(profile['stateRoot']) / 'active.json'
    known = json_read(active) if active.exists() else None
    for service, value in config['services'].items():
        value.pop('build', None)
        value.pop('pull_policy', None)
        name = value.get('container_name', profile['project'] + '-' + service + '-1')
        if name not in live:
            require(service in SERVICES, 'shared_service_must_exist')
            continue
        container = live[name]
        labels = container['Config'].get('Labels', {})
        require(labels.get('com.docker.compose.project') == profile['project'] and
                labels.get('com.docker.compose.service') == service, 'container_ownership_mismatch')
        require(container['State']['Running'], 'baseline_container_not_running')
        actual = dict(e.split('=', 1) for e in container['Config']['Env'] if '=' in e)
        environment = value.get('environment', {})
        for key, expected in environment.items():
            allowed = key in MODEL_PATCH or key == LEGACY_PRICE
            allowed |= service == 'api' and key == 'Database__MigrateOnStartup' and actual.get(key) == 'false'
            allowed |= service == 'api' and known is not None and key == 'Deploy__ReleaseSha'
            require(allowed or actual.get(key) == str(expected), 'effective_environment_differs_from_runtime')
            require(allowed or key in actual, 'runtime_environment_key_missing')
        if environment:
            value['environment'] = {key: actual[key] for key in environment if key in actual}
        for mount in value.get('volumes', []):
            require(any(m['Source'] == mount.get('source') and m['Destination'] == mount['target'] and
                        m['RW'] == (not mount.get('read_only', False)) for m in container['Mounts']), 'runtime_mount_mismatch')
        expected_networks = {config['networks'][n]['name'] for n in value.get('networks', {})}
        require(expected_networks == set(container['NetworkSettings']['Networks']), 'runtime_network_mismatch')
        value['image'] = container['Image']
    return config

def database_history(config):
    name = config['services']['db']['container_name']
    sql = 'SELECT "MigrationId" FROM "__EFMigrationsHistory" ORDER BY "MigrationId";'
    output = run(['docker', 'exec', name, 'sh', '-c',
                  'exec psql -U "$POSTGRES_USER" -d "$POSTGRES_DB" -At -c "$1"', 'sh', sql], timeout=30)
    return output.decode().splitlines()

def backup_gate(profile, config):
    backup = Path(profile['backupFile'])
    require(backup.is_file() and not backup.is_symlink(), 'backup_missing')
    require(file_digest(backup) == profile['backupSha256'], 'backup_checksum_mismatch')
    protected(profile['restoreProofFile'])
    proof = json_read(profile['restoreProofFile'])
    require(proof.get('backupSha256') == profile['backupSha256'] and proof.get('isolatedRestoreSucceeded') is True, 'isolated_restore_not_verified')
    verified = datetime.fromisoformat(proof['verifiedAtUtc'])
    require(verified.tzinfo is not None and 0 <= (datetime.now(timezone.utc) - verified).total_seconds() <= 3600, 'backup_restore_proof_expired')
    require(0 <= time.time() - backup.stat().st_mtime <= 3600, 'backup_expired')
    run(['docker', 'exec', '-i', config['services']['db']['container_name'], 'pg_restore', '--list'], backup, timeout=60)
    return {'backupSha256': file_digest(backup), 'restoreProofSha256': file_digest(profile['restoreProofFile'])}

def source_hashes(profile):
    return {p: file_digest(p) for p in profile['composeFiles'] + profile['envFiles'] + profile['preserveFiles']}

def unchanged(snapshot, affected, profile):
    live = containers()
    excluded = set(affected)
    require({n: fingerprint(c) for n, c in live.items() if n not in excluded} == snapshot, 'unrelated_container_changed')
    return live

def plan(profile_path, bundle, manifest_digest):
    profile = read_profile(profile_path)
    artifact = Path(bundle)
    require(artifact.is_dir() and not artifact.is_symlink() and artifact.stat().st_uid == os.geteuid() and artifact.stat().st_mode & 0o077 == 0, 'artifact_directory_not_private')
    for name in ('release.json', 'images.tar', 'source.tar'):
        path = artifact / name
        require(path.is_file() and not path.is_symlink() and path.stat().st_uid == os.geteuid() and path.stat().st_mode & 0o022 == 0, 'artifact_file_not_immutable')
    manifest = verify_bundle(bundle, manifest_digest)
    require(manifest['migrationSha256'] == profile['migrationSha256'] and manifest['baselineSha'] == profile['schemaBaselineSha'], 'incompatible_schema')
    require(manifest['composeSha256'] == profile['composeSha256'], 'candidate_compose_not_equivalent')
    require(shutil.disk_usage(Path(profile['current']).parent).free >= profile.get('minimumFreeBytes', 8 * 1024**3) + 2 * Path(bundle, 'images.tar').stat().st_size, 'insufficient_disk_capacity')
    live = containers()
    before = adopt(effective(profile), live, profile)
    history = database_history(before)
    require(history == manifest['migrationIds'], 'pending_or_unknown_database_migration')
    after = copy.deepcopy(before)
    affected = []
    for service in SERVICES:
        if service not in manifest['images']:
            continue
        require(service in after['services'], 'candidate_service_missing')
        value = after['services'][service]
        name = value['container_name']
        image = manifest['images'][service]
        try:
            info = json.loads(run(['docker', 'image', 'inspect', image]))[0]
        except ReleaseError:
            try:
                info = json.loads(run(['docker', 'image', 'inspect', manifest['configIds'][service]]))[0]
                image = info['Id']
            except ReleaseError:
                pass
        value['image'] = image
        if service == 'api':
            env = value.setdefault('environment', {})
            require(env.get('OpenAI__TranscriptionModel') == 'gpt-4o-mini-transcribe', 'transcription_not_reviewed')
            env.update(MODEL_PATCH)
            env.pop(LEGACY_PRICE, None)
            env['Database__MigrateOnStartup'] = 'false'
            env['Deploy__ReleaseSha'] = manifest['sourceSha']
        model_change = service == 'api' and patch_model(Path(profile['modelEnv']).read_bytes()) != Path(profile['modelEnv']).read_bytes()
        if name not in live or value != before['services'][service] or model_change:
            affected.append(service)
    backup = backup_gate(profile, before) if affected else {'required': False}
    require('api' in manifest['images'] or patch_model(Path(profile['modelEnv']).read_bytes()) == Path(profile['modelEnv']).read_bytes(), 'model_update_requires_api_image')
    roundtrip = dollars(json.loads(compose(after, 'config', '--format', 'json')), False)
    differences = [s + '.' + k for s in after['services'] for k in set(after['services'][s]) | set(roundtrip['services'].get(s, {}))
                   if after['services'][s].get(k) != roundtrip['services'].get(s, {}).get(k)]
    require(roundtrip == after, 'compose_roundtrip_not_equivalent_fields:' + ','.join(sorted(differences)))
    names = [after['services'][s]['container_name'] for s in affected]
    unaffected = {n: fingerprint(c) for n, c in live.items() if n not in names}
    current = Path(profile['current'])
    inputs = {'profileSha256': file_digest(profile_path), 'manifestSha256': manifest_digest,
              'sources': source_hashes(profile), 'containers': {n: fingerprint(c) for n, c in live.items()},
              'before': digest(canonical(before)), 'after': digest(canonical(after)),
              'currentTarget': os.readlink(current) if current.is_symlink() else None,
              'toolSha256': file_digest(__file__), 'commonSha256': file_digest(Path(__file__).with_name('release_common.py'))}
    inputs['databaseHistorySha256'] = digest(canonical(history))
    inputs['backup'] = backup
    return profile, manifest, before, after, affected, unaffected, inputs

def summary(planned):
    profile, manifest, before, after, affected, unaffected, inputs = planned
    return {'status': 'PLAN_READY' if affected else 'ALREADY_APPLIED', 'sourceSha': manifest['sourceSha'],
            'services': affected, 'approvalDigest': digest(canonical(inputs)),
            'inputDigests': {key: digest(canonical(value)) for key, value in inputs.items()},
            'manifestSha256': inputs['manifestSha256'], 'startupMigrations': False,
            'project': profile['project'], 'composeFiles': profile['composeFiles'],
            'environmentFiles': profile['envFiles'], 'modelEnv': profile['modelEnv'], 'current': profile['current'],
            'modelPatch': MODEL_PATCH, 'removedPriceKey': LEGACY_PRICE,
            'unrelatedContainers': len(unaffected)}

def health(config, services, profile):
    deadline = time.monotonic() + profile.get('healthTimeoutSeconds', 120)
    while True:
        live = containers()
        ready = True
        for service in services:
            name = config['services'][service]['container_name']
            c = live.get(name)
            ready &= bool(c and c['State']['Running'] and not c['State']['OOMKilled'] and c['Image'] == config['services'][service]['image'])
            if c and 'Health' in c['State']:
                ready &= c['State']['Health']['Status'] == 'healthy'
        if ready:
            try:
                for command in profile['acceptance']:
                    require(isinstance(command, list) and command and all(isinstance(a, str) for a in command), 'invalid_acceptance_command')
                    run(command, timeout=20)
                return
            except ReleaseError:
                pass
        require(time.monotonic() < deadline, 'health_or_acceptance_failed')
        time.sleep(1)

def activate(config, services):
    for service in services:
        compose(config, 'up', '-d', '--no-deps', '--no-build', '--pull', 'never', service)

def loaded_image(manifest, service):
    for identifier in (manifest['images'][service], manifest['configIds'][service]):
        try:
            info = json.loads(run(['docker', 'image', 'inspect', identifier]))[0]
            require(info['Os'] == 'linux' and info['Architecture'] == 'amd64' and
                    info['Config']['Labels']['org.opencontainers.image.revision'] == manifest['sourceSha'], 'loaded_image_identity_mismatch')
            return info['Id']
        except ReleaseError:
            pass
    raise ReleaseError('loaded_image_unavailable')

def save_state(path, data):
    private_write(Path(path) / 'state.json', canonical(data))

def recovery(path):
    path = Path(path)
    protected(path / 'state.json')
    state = json_read(path / 'state.json')
    for name, checksum in state['recoveryFiles'].items():
        require(file_digest(path / name) == checksum, 'recovery_checksum_mismatch')
    return state

def restore(path, profile):
    path = Path(path)
    state = recovery(path)
    before = json_read(path / 'rollback-compose.json')
    require(digest(canonical(database_history(before))) == state['databaseHistorySha256'], 'database_changed_since_release')
    names = [before['services'][s]['container_name'] for s in state['services']]
    unchanged(state['unaffected'], names, profile)
    for service, image in state['previousImages'].items():
        try:
            run(['docker', 'image', 'inspect', image])
        except ReleaseError:
            require('previous-images.tar' in state['recoveryFiles'], 'previous_image_unavailable')
            run(['docker', 'image', 'load', '-i', path / 'previous-images.tar'], timeout=900)
            run(['docker', 'image', 'inspect', image])
    if Path(profile['modelEnv']).read_bytes() != (path / 'previous-model.env').read_bytes():
        private_write(profile['modelEnv'], (path / 'previous-model.env').read_bytes())
    if state['status'] in ('ACTIVATING', 'ACCEPTED', 'RECOVERY_REQUIRED'):
        activate(before, [s for s in state['services'] if s in state['previousImages']])
    for service in state['services']:
        if service not in state['previousImages']:
            name = before['services'][service]['container_name']
            live = containers()
            if name in live:
                c = live[name]
                require(c['Config']['Labels'].get('com.docker.compose.project') == profile['project'], 'rollback_container_ownership')
                run(['docker', 'container', 'rm', '-f', c['Id']])
    if state['previousImages']:
        health(before, list(state['previousImages']), profile)
    current = Path(profile['current'])
    if state['previousCurrent'] is None:
        current.unlink(missing_ok=True)
    elif not current.is_symlink() or os.readlink(current) != state['previousCurrent']:
        set_current(current, state['previousCurrent'])
    active = Path(profile['stateRoot']) / 'active.json'
    if state['previousActive'] is None:
        active.unlink(missing_ok=True)
    else:
        private_write(active, canonical(state['previousActive']))
    unchanged(state['unaffected'], names, profile)
    require({p: file_digest(p) for p in profile['preserveFiles']} == state['preservedFiles'], 'preserved_file_changed')
    state['status'] = 'ROLLED_BACK'
    save_state(path, state)

def set_current(path, target):
    temporary = Path(str(path) + '.next')
    require(not temporary.exists() and not temporary.is_symlink(), 'current_transaction_conflict')
    temporary.symlink_to(target)
    os.replace(temporary, path)

def apply(profile_path, bundle, manifest_digest, approval):
    planned = plan(profile_path, bundle, manifest_digest)
    require(summary(planned)['approvalDigest'] == approval, 'approval_digest_mismatch:' + json.dumps(summary(planned)['inputDigests'], sort_keys=True))
    profile = planned[0]
    initial_inputs = planned[-1]
    root = Path(profile['stateRoot'])
    root.mkdir(mode=0o700, parents=True, exist_ok=True)
    require(root.stat().st_uid == os.geteuid() and root.stat().st_mode & 0o077 == 0, 'state_directory_not_private')
    with (root / 'lock').open('a') as lock:
        fcntl.flock(lock, fcntl.LOCK_EX | fcntl.LOCK_NB)
        planned = plan(profile_path, bundle, manifest_digest)
        changed_inputs = sorted(k for k in initial_inputs if initial_inputs[k] != planned[-1][k])
        require(summary(planned)['approvalDigest'] == approval, 'host_changed_since_approval:' + ','.join(changed_inputs))
        profile, manifest, before, after, services, unaffected, inputs = planned
        if not services:
            return summary(planned)
        path = root / (manifest['sourceSha'] + '-' + manifest_digest[:12] + '-' + str(time.time_ns()))
        path.mkdir(mode=0o700)
        names = [after['services'][s]['container_name'] for s in services]
        live = containers()
        previous = {s: live[before['services'][s]['container_name']]['Image'] for s in services if before['services'][s]['container_name'] in live}
        private_write(path / 'previous-compose.json', canonical(before))
        rollback_config = copy.deepcopy(before)
        rollback_config['services']['api'].setdefault('environment', {})['Database__MigrateOnStartup'] = 'false'
        private_write(path / 'rollback-compose.json', canonical(rollback_config))
        private_write(path / 'candidate-compose.json', canonical(after))
        private_write(path / 'previous-model.env', Path(profile['modelEnv']).read_bytes())
        private_write(path / 'release.json', Path(bundle, 'release.json').read_bytes())
        state = {'format': 1, 'sourceSha': manifest['sourceSha'], 'manifestSha256': manifest_digest, 'status': 'PREPARING',
                 'services': services, 'previousImages': previous, 'previousCurrent': inputs['currentTarget'], 'unaffected': unaffected,
                 'previousActive': json_read(root / 'active.json') if (root / 'active.json').exists() else None,
                 'databaseHistorySha256': inputs['databaseHistorySha256'], 'backup': inputs['backup'],
                 'preservedFiles': {p: file_digest(p) for p in profile['preserveFiles']},
                 'recoveryFiles': {p: file_digest(path / p) for p in ('previous-compose.json', 'rollback-compose.json', 'candidate-compose.json', 'previous-model.env', 'release.json')}}
        save_state(path, state)
        if previous:
            run(['docker', 'image', 'save', '-o', path / 'previous-images.tar', *sorted(set(previous.values()))], timeout=900)
            os.chmod(path / 'previous-images.tar', 0o600)
            state['recoveryFiles']['previous-images.tar'] = file_digest(path / 'previous-images.tar')
        source = path / 'source'
        source.mkdir(mode=0o700)
        extract_source(Path(bundle) / 'source.tar', source)
        state['status'] = 'PREPARED'
        save_state(path, state)
        try:
            verify_bundle(bundle, manifest_digest)
            run(['docker', 'image', 'load', '-i', Path(bundle) / 'images.tar'], timeout=900)
            for service in manifest['images']:
                after['services'][service]['image'] = loaded_image(manifest, service)
            private_write(path / 'candidate-compose.json', canonical(after))
            state['recoveryFiles']['candidate-compose.json'] = file_digest(path / 'candidate-compose.json')
            save_state(path, state)
            unchanged(unaffected, names, profile)
            require(source_hashes(profile) == inputs['sources'], 'source_changed_during_prepare')
            require(digest(canonical(database_history(before))) == inputs['databaseHistorySha256'], 'database_changed_during_prepare')
            state['status'] = 'ACTIVATING'
            save_state(path, state)
            private_write(profile['modelEnv'], patch_model(Path(profile['modelEnv']).read_bytes()))
            activate(after, services)
            health(after, services, profile)
            require(digest(canonical(database_history(after))) == inputs['databaseHistorySha256'], 'database_changed_during_release')
            unchanged(unaffected, names, profile)
            require({p: file_digest(p) for p in profile['preserveFiles']} == state['preservedFiles'], 'preserved_file_changed')
            set_current(Path(profile['current']), str(source))
            private_write(root / 'active.json', canonical({'sourceSha': manifest['sourceSha'], 'statePath': str(path)}))
            state['status'] = 'ACCEPTED'
            save_state(path, state)
            return {'status': 'ACCEPTED', 'sourceSha': manifest['sourceSha'], 'services': services, 'statePath': str(path)}
        except Exception as failure:
            reason = str(failure) if isinstance(failure, ReleaseError) else 'unexpected_error_output_withheld'
            state['failureReason'] = reason
            save_state(path, state)
            try:
                restore(path, profile)
            except Exception:
                state['status'] = 'RECOVERY_REQUIRED'
                save_state(path, state)
                raise ReleaseError('automatic_rollback_failed_manual_recovery_required') from None
            raise ReleaseError('release_failed_previous_version_restored:' + reason) from None

def rollback_plan(profile_path, state_path):
    profile = read_profile(profile_path)
    state = recovery(state_path)
    require(Path(state_path).resolve().parent == Path(profile['stateRoot']).resolve(), 'state_outside_profile')
    require(state['status'] in ('ACCEPTED', 'PREPARING', 'PREPARED', 'ACTIVATING', 'RECOVERY_REQUIRED'), 'state_not_recoverable')
    value = {'profileSha256': file_digest(profile_path), 'recovery': state['recoveryFiles'],
             'containers': {n: fingerprint(c) for n, c in containers().items()}, 'sources': source_hashes(profile),
             'current': os.readlink(profile['current']) if Path(profile['current']).is_symlink() else None}
    return profile, {'status': 'ROLLBACK_PLAN_READY', 'services': state['services'], 'approvalDigest': digest(canonical(value))}

if __name__ == '__main__':
    parser = argparse.ArgumentParser()
    parser.add_argument('action', choices=('plan', 'apply', 'rollback-plan', 'rollback'))
    parser.add_argument('--profile', required=True)
    parser.add_argument('--bundle')
    parser.add_argument('--manifest')
    parser.add_argument('--state')
    parser.add_argument('--approve')
    args = parser.parse_args()
    try:
        if args.action in ('plan', 'apply'):
            require(args.bundle is not None and args.manifest is not None, 'bundle_and_manifest_required')
            result = summary(plan(args.profile, args.bundle, args.manifest)) if args.action == 'plan' else apply(args.profile, args.bundle, args.manifest, args.approve)
        else:
            require(args.state is not None, 'state_required')
            profile, result = rollback_plan(args.profile, args.state)
            if args.action == 'rollback':
                require(result['approvalDigest'] == args.approve, 'rollback_approval_mismatch')
                with (Path(profile['stateRoot']) / 'lock').open('a') as lock:
                    fcntl.flock(lock, fcntl.LOCK_EX | fcntl.LOCK_NB)
                    require(rollback_plan(args.profile, args.state)[1] == result, 'host_changed_since_rollback_approval')
                    restore(args.state, profile)
                    result = {'status': 'ROLLED_BACK', 'services': result['services']}
        print(json.dumps(result))
    except Exception as error:
        print(json.dumps({'status': 'FAILED', 'reason': str(error) if isinstance(error, ReleaseError) else 'unexpected_error_output_withheld'}))
        raise SystemExit(1)
