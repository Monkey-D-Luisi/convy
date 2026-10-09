"""Real Docker state transitions, shared Caddy/PostgreSQL and failure injection."""
import argparse
import copy
import importlib.util
import io
import json
import os
import re
from pathlib import Path
import shutil
import subprocess
import sys
import tarfile
import tempfile
import time
from datetime import datetime, timezone

sys.path.insert(0, '/source/ops/vps')
from release_common import canonical, file_digest, digest
from staging_common import CI_WORKFLOW_ID, REPOSITORY, authorize_ci
from staging_host import automatic, capacity, host_lock, maintenance, recover_interrupted, release as auto_release
broker_spec = importlib.util.spec_from_file_location('broker', '/source/ops/vps/staging-broker.py')
broker = importlib.util.module_from_spec(broker_spec)
broker_spec.loader.exec_module(broker)

parser = argparse.ArgumentParser()
parser.add_argument('--root', required=True)
parser.add_argument('--project', required=True)
args = parser.parse_args()
root = Path(args.root)
project = args.project
if re.fullmatch(r'convy-release-test-[a-f0-9]{10}', project) is None:
    raise SystemExit('Only a generated local fixture project is allowed')
old_sha, new_sha, bad_sha = '1' * 40, '2' * 40, '3' * 40
schema = digest(b'fixture-compatible-schema')
secret = 'fixture-secret-dollar$literal$that-must-never-appear'
controller = '/source/ops/vps/safe-release.py'
results = []
images = []

def command(args, data=None):
    r = subprocess.run(args, input=data, capture_output=True)
    if r.returncode:
        raise RuntimeError('Fixture command failed: ' + args[0] + ' ' + r.stderr.decode()[-1000:])
    return r.stdout

def compose(*extra):
    return command(['docker', 'compose', '--project-name', project, '--env-file', str(root / 'api.env'),
                    '-f', str(root / 'base.json'), '-f', str(root / 'override.json'), *extra])

def inspect(name):
    return json.loads(command(['docker', 'inspect', project + '-' + name]))[0]

def shared_state():
    result = {}
    for n in ('caddy', 'db', 'converso'):
        c = inspect(n)
        result[n] = {k: c[k] for k in ('Id', 'Image', 'NetworkSettings', 'RestartCount')}
        result[n]['Mounts'] = sorted(c['Mounts'], key=lambda m: m['Destination'])
    return result

def invoke(action, bundle=None, checksum=None, approve=None, state=None, expected=0, environment=None):
    rejection = expected == 1 and (action == 'plan' or approve == '0' * 64)
    if rejection:
        unchanged_ids = command(['docker', 'ps', '-aq']).split()
        unchanged_images = sorted(command(['docker', 'image', 'ls', '-q', '--no-trunc']).split())
        unchanged_env = (root / 'api.env').read_bytes()
        unchanged_states = sorted((root / 'transactions').glob('*/state.json'))
    argv = ['python3', controller, action, '--profile', str(root / 'profile.json')]
    if bundle:
        argv += ['--bundle', str(bundle), '--manifest', checksum]
    if approve:
        argv += ['--approve', approve]
    if state:
        argv += ['--state', state]
    r = subprocess.run(argv, capture_output=True, env=environment)
    if secret.encode() in r.stdout + r.stderr:
        raise AssertionError('Secret leaked')
    require(r.returncode == expected, action + ' expected ' + str(expected) + ': ' + r.stdout.decode() + r.stderr.decode())
    value = json.loads(r.stdout)
    if rejection:
        require(command(['docker', 'ps', '-aq']).split() == unchanged_ids, 'Rejected plan mutated containers')
        require(sorted(command(['docker', 'image', 'ls', '-q', '--no-trunc']).split()) == unchanged_images, 'Rejected plan loaded images')
        require((root / 'api.env').read_bytes() == unchanged_env, 'Rejected plan changed configuration')
        require(sorted((root / 'transactions').glob('*/state.json')) == unchanged_states, 'Rejected plan created a transaction')
    print(json.dumps({'action': action, **value}), flush=True)
    return value

def require(condition, reason):
    if not condition:
        raise AssertionError(reason)

def record(name, operation):
    baseline = shared_state()
    operation()
    require(shared_state() == baseline, name + ': shared container changed')
    require((root / 'certificate.fixture').read_bytes() == b'preserved-certificate-bytes', 'Certificate changed')
    results.append(name)
    print('PASS ' + name, flush=True)

def build(version, sha, healthy):
    tag = project + ':v' + version
    command(['docker', 'build', '--platform', 'linux/amd64', '-t', tag,
             '--build-arg', 'SOURCE_SHA=' + sha, '--build-arg', 'VERSION=' + version,
             '--build-arg', 'HEALTHY=' + str(healthy).lower(),
             '-f', '/source/ops/vps/tests/application.Dockerfile', '/source/ops/vps/tests'])
    image = json.loads(command(['docker', 'image', 'inspect', tag]))[0]['Id']
    images.append(tag)
    images.append(image)
    return image

def bundle(name, sha, image):
    path = root / name
    path.mkdir(mode=0o700)
    with tarfile.open(path / 'source.tar', 'w') as archive:
        archive.add('/source/ops/vps/tests/application.py', arcname='application.py')
    command(['docker', 'image', 'save', '-o', str(path / 'images.tar'), image])
    manifest = {'format': 1, 'sourceSha': sha, 'baselineSha': old_sha, 'platform': 'linux/amd64',
                'schemaPolicy': 'unchanged', 'migrationSha256': schema,
                'migrationIds': ['20261008000000_Fixture'],
                'composeSha256': file_digest(root / 'base.json'), 'images': {'api': image, 'worker': image},
                'files': {n: file_digest(path / n) for n in ('source.tar', 'images.tar')}}
    size = json.loads(command(['docker', 'image', 'inspect', image]))[0]['Size']
    manifest['imageSizes'] = {'api': size, 'worker': size}
    (path / 'release.json').write_bytes(canonical(manifest))
    return path, file_digest(path / 'release.json')

def apply(bundle_info, **kwargs):
    path, checksum = bundle_info
    planned = invoke('plan', path, checksum)
    return invoke('apply', path, checksum, approve=planned['approvalDigest'], **kwargs)

def rollback(accepted):
    state = accepted['statePath']
    planned = invoke('rollback-plan', state=state)
    invoke('rollback', approve=planned['approvalDigest'], state=state)
    previous = json.loads(Path(state, 'state.json').read_bytes())['previousImages']
    if previous:
        require(inspect('api')['Image'] == old_image, 'Old API image not restored')
    else:
        require(not command(['docker', 'ps', '-aq', '--filter', 'name=^' + project + '-api$']).strip(), 'Initial API was not removed')
    require((root / 'api.env').read_bytes() == original_env, 'Protected environment not restored')

try:
    old_image = build('1', old_sha, True)
    new_image = build('2', new_sha, True)
    bad_image = build('3', bad_sha, False)
    (root / 'api.env').write_text("OPENAI_API_KEY='" + secret + "'\nOpenAI__ParsingModel=gpt-5.4-nano\n"
                               'OpenAI__TranscriptionModel=gpt-4o-mini-transcribe\n'
                               'OpenAI__Costs__ParsingInputMicrosPer1KTokens=\n'
                               'OpenAI__Costs__ParsingReasoningMicrosPer1KTokens=\n')
    os.chmod(root / 'api.env', 0o600)
    original_env = (root / 'api.env').read_bytes()
    (root / 'Caddyfile').write_text(':8080 {\n respond "shared edge"\n}\n')
    (root / 'certificate.fixture').write_bytes(b'preserved-certificate-bytes')
    healthcheck = {'test': ['CMD', 'python3', '-c', 'import urllib.request; urllib.request.urlopen("http://127.0.0.1:8080")'],
                   'interval': '1s', 'timeout': '1s', 'retries': 2, 'start_period': '1s'}
    app = {'image': old_image, 'env_file': [str(root / 'api.env')],
           'environment': {'Database__MigrateOnStartup': 'true', 'Deploy__ReleaseSha': old_sha},
           'networks': ['private'], 'healthcheck': healthcheck}
    base = {'name': project, 'services': {
        'api': {**app, 'container_name': project + '-api'},
        'worker': {**app, 'container_name': project + '-worker'},
        'db': {'image': 'postgres:16-alpine', 'container_name': project + '-db',
               'environment': {'POSTGRES_PASSWORD': 'fixture-only', 'POSTGRES_USER': 'fixture', 'POSTGRES_DB': 'fixture'}, 'networks': ['private']},
        'caddy': {'image': 'caddy:2.10.0-alpine', 'container_name': project + '-caddy',
                  'volumes': [str(root / 'Caddyfile') + ':/etc/caddy/Caddyfile:ro',
                              str(root / 'certificate.fixture') + ':/fixture/certificate:ro'], 'networks': ['private']},
        'converso': {'image': old_image, 'container_name': project + '-converso', 'networks': ['edge']}},
        'networks': {'private': {'name': project + '-private'}, 'edge': {'name': project + '-edge'}}}
    (root / 'base.json').write_bytes(canonical(base))
    override = {'services': {'caddy': {'environment': {'OPS_HASH': 'literal$$retained'}, 'networks': ['edge']}}}
    (root / 'override.json').write_bytes(canonical(override))
    profile = {'format': 1, 'project': project, 'composeFiles': [str(root / 'base.json'), str(root / 'override.json')],
               'envFiles': [str(root / 'api.env')], 'modelEnv': str(root / 'api.env'),
               'preserveFiles': [str(root / 'Caddyfile'), str(root / 'certificate.fixture')],
               'current': str(root / 'current'), 'stateRoot': str(root / 'transactions'),
               'migrationSha256': schema, 'schemaBaselineSha': old_sha, 'composeSha256': file_digest(root / 'base.json'),
               'composeHashes': {str(root / p): file_digest(root / p) for p in ('base.json', 'override.json')},
               'healthTimeoutSeconds': 15, 'minimumFreeBytes': 0,
               'acceptance': [['docker', 'exec', project + '-api', 'python3', '-c',
                               'import urllib.request; urllib.request.urlopen("http://127.0.0.1:8080")']]}
    def write_profile():
        (root / 'profile.json').write_bytes(canonical(profile))
        os.chmod(root / 'profile.json', 0o600)
    write_profile()
    compose('up', '-d', '--no-deps', 'caddy', 'db', 'converso')
    deadline = time.monotonic() + 30
    while subprocess.run(['docker', 'exec', project + '-db', 'pg_isready', '-h', '127.0.0.1', '-U', 'fixture'], capture_output=True).returncode:
        require(time.monotonic() < deadline, 'Fixture PostgreSQL failed to start')
        time.sleep(0.2)
    command(['docker', 'exec', project + '-db', 'psql', '-U', 'fixture', '-d', 'fixture', '-c',
             'CREATE TABLE "__EFMigrationsHistory" ("MigrationId" text PRIMARY KEY, "ProductVersion" text); INSERT INTO "__EFMigrationsHistory" VALUES (\'20261008000000_Fixture\', \'fixture\');'])
    dump = command(['docker', 'exec', project + '-db', 'pg_dump', '-U', 'fixture', '-d', 'fixture', '-Fc'])
    (root / 'fresh.dump').write_bytes(dump)
    command(['docker', 'exec', project + '-db', 'createdb', '-U', 'fixture', 'isolated_restore'])
    command(['docker', 'exec', '-i', project + '-db', 'pg_restore', '-U', 'fixture', '-d', 'isolated_restore', '--exit-on-error'], dump)
    restored = command(['docker', 'exec', project + '-db', 'psql', '-U', 'fixture', '-d', 'isolated_restore', '-At', '-c', 'SELECT COUNT(*) FROM "__EFMigrationsHistory";'])
    require(restored.strip() == b'1', 'Isolated fixture restore did not recover schema')
    command(['docker', 'exec', project + '-db', 'dropdb', '-U', 'fixture', 'isolated_restore'])
    (root / 'restore-proof.json').write_bytes(canonical({'backupSha256': file_digest(root / 'fresh.dump'), 'isolatedRestoreSucceeded': True, 'verifiedAtUtc': datetime.now(timezone.utc).isoformat()}))
    os.chmod(root / 'restore-proof.json', 0o600)
    profile.update({'backupFile': str(root / 'fresh.dump'), 'backupSha256': file_digest(root / 'fresh.dump'), 'restoreProofFile': str(root / 'restore-proof.json')})
    write_profile()
    good = bundle('good', new_sha, new_image)
    bad = bundle('bad', bad_sha, bad_image)
    first = {}
    record('first_application_deployment', lambda: first.update(apply(good)))
    record('idempotent_repeat', lambda: require(invoke('plan', *good)['status'] == 'ALREADY_APPLIED', 'Repeat is not idempotent'))
    os.utime(root / 'fresh.dump', (time.time() - 7200, time.time() - 7200))
    record('idempotent_repeat_requires_no_new_backup', lambda: require(invoke('plan', *good)['status'] == 'ALREADY_APPLIED', 'No-op requires a backup'))
    os.utime(root / 'fresh.dump', None)
    record('normal_rollback', lambda: rollback(first))
    # An initial application deployment has no previous app; rollback removes only those new app containers.
    compose('up', '-d', '--no-deps', 'api', 'worker')
    (root / 'old-source').mkdir()
    (root / 'current').symlink_to(root / 'old-source')
    accepted = {}
    record('successful_update', lambda: accepted.update(apply(good)))
    record('repeat_preserves_container_ids', lambda: require(apply(good)['status'] == 'ALREADY_APPLIED', 'Repeat mutated'))
    record('normal_update_rollback', lambda: rollback(accepted))
    record('wrong_manifest_sha', lambda: invoke('plan', good[0], '0' * 64, expected=1))
    missing = root / 'missing'
    shutil.copytree(good[0], missing)
    manifest = json.loads((missing / 'release.json').read_bytes())
    manifest['images']['api'] = 'sha256:' + '0' * 64
    (missing / 'release.json').write_bytes(canonical(manifest))
    record('missing_image', lambda: invoke('plan', missing, file_digest(missing / 'release.json'), expected=1))
    wrong_source = root / 'wrong-source'
    shutil.copytree(good[0], wrong_source)
    manifest = json.loads((wrong_source / 'release.json').read_bytes())
    manifest['sourceSha'] = '4' * 40
    (wrong_source / 'release.json').write_bytes(canonical(manifest))
    record('wrong_source_sha', lambda: invoke('plan', wrong_source, file_digest(wrong_source / 'release.json'), expected=1))
    corrupt = root / 'corrupt'
    shutil.copytree(good[0], corrupt)
    (corrupt / 'images.tar').write_bytes(b'partial-transfer')
    record('partial_transfer_checksum', lambda: invoke('plan', corrupt, good[1], expected=1))
    planned = invoke('plan', *good)
    record('missing_approval', lambda: invoke('apply', *good, approve='0' * 64, expected=1))
    profile['backupSha256'] = '0' * 64
    write_profile()
    record('backup_checksum_mismatch', lambda: invoke('plan', *good, expected=1))
    profile['backupSha256'] = file_digest(root / 'fresh.dump')
    write_profile()
    os.utime(root / 'fresh.dump', (time.time() - 7200, time.time() - 7200))
    record('expired_backup', lambda: invoke('plan', *good, expected=1))
    os.utime(root / 'fresh.dump', None)
    command(['docker', 'exec', project + '-db', 'psql', '-U', 'fixture', '-d', 'fixture', '-c', 'INSERT INTO "__EFMigrationsHistory" VALUES (\'20261009000000_Unknown\', \'fixture\');'])
    record('unknown_live_migration', lambda: invoke('plan', *good, expected=1))
    command(['docker', 'exec', project + '-db', 'psql', '-U', 'fixture', '-d', 'fixture', '-c', 'DELETE FROM "__EFMigrationsHistory" WHERE "MigrationId" = \'20261009000000_Unknown\';'])
    (root / 'override.json').write_text('invalid: [')
    profile['composeHashes'][str(root / 'override.json')] = file_digest(root / 'override.json')
    write_profile()
    record('invalid_compose', lambda: invoke('plan', *good, expected=1))
    (root / 'override.json').write_bytes(canonical(override))
    profile['composeHashes'][str(root / 'override.json')] = file_digest(root / 'override.json')
    write_profile()
    profile['migrationSha256'] = '0' * 64
    write_profile()
    record('incompatible_schema', lambda: invoke('plan', *good, expected=1))
    profile['migrationSha256'] = schema
    write_profile()
    nonreversible = root / 'nonreversible'
    shutil.copytree(good[0], nonreversible)
    manifest = json.loads((nonreversible / 'release.json').read_bytes())
    manifest['schemaPolicy'] = 'nonreversible'
    (nonreversible / 'release.json').write_bytes(canonical(manifest))
    record('nonreversible_migration', lambda: invoke('plan', nonreversible, file_digest(nonreversible / 'release.json'), expected=1))
    record('health_failure_automatic_rollback', lambda: require(apply(bad, expected=1)['reason'].startswith('release_failed_previous_version_restored:'), 'Health rollback failed'))
    require(inspect('api')['Image'] == old_image and inspect('worker')['Image'] == old_image, 'Health rollback image mismatch')
    profile['acceptance'].append(['docker', 'exec', project + '-api', 'python3', '-c', 'import os; assert os.environ["VERSION"] != "2"'])
    write_profile()
    record('acceptance_failure_automatic_rollback', lambda: require(apply(good, expected=1)['reason'].startswith('release_failed_previous_version_restored:'), 'Acceptance rollback failed'))
    profile['acceptance'].pop()
    write_profile()
    shim = root / 'shim'
    shim.mkdir()
    marker = root / 'fail-once'
    def inject(match):
        marker.unlink(missing_ok=True)
        (shim / 'docker').write_text('#!/bin/sh\ncase " $* " in\n *"' + match + '"*) if [ ! -e "' + str(marker) + '" ]; then touch "' + str(marker) + '"; echo \'' + secret + '\' >&2; exit 1; fi;;\nesac\nexec /usr/local/bin/docker "$@"\n')
        os.chmod(shim / 'docker', 0o755)
        return {**os.environ, 'PATH': str(shim) + ':' + os.environ['PATH']}
    record('load_failure_automatic_rollback', lambda: apply(good, expected=1, environment=inject('image load')))
    record('partial_start_failure_automatic_rollback', lambda: apply(good, expected=1, environment=inject('never worker')))
    require(inspect('api')['Image'] == old_image and inspect('worker')['Image'] == old_image, 'Partial rollback mismatch')
    require((root / 'api.env').read_bytes() == original_env, 'Secret config mismatch')
    runtime_env = dict(value.split('=', 1) for value in inspect('api')['Config']['Env'])
    require(runtime_env['OPENAI_API_KEY'] == secret, 'Secret value changed')
    require(runtime_env['Database__MigrateOnStartup'] == 'false', 'Rollback enabled startup migrations')
    require((root / 'current').resolve() == root / 'old-source', 'Current pointer mismatch')
    # Automatic CD uses the same real application/database/edge containers.
    profile.update({'automaticEnabled': True, 'modelPolicy': 'preserve', 'githubTokenFile': str(root / 'github-token'),
                    'incomingRoot': str(root / 'cd-incoming'), 'backupRoot': str(root / 'backups'),
                    'offsiteReceiptFile': str(root / 'offsite-receipt.json'),
                    'offsiteExportCommand': ['python3', '/source/ops/vps/offsite-export.py', '--config', str(root / 'offsite-config.json')],
                    'maximumIncomingBytes': 2 * 1024**3, 'maximumReleaseBytes': 3 * 1024**3,
                    'maximumRollbackArchiveBytes': 1024**3,
                    'applicationCaps': {'api': {'memoryBytes': 512 * 1024**2, 'cpus': 1.0}, 'worker': {'memoryBytes': 256 * 1024**2, 'cpus': 0.5}},
                    'applicationLogOptions': {'max-size': '10m', 'max-file': '3'},
                    'minimumAvailableMemoryBytes': 0, 'failedReleaseCount': 2, 'failedReleaseMaxAgeSeconds': 86400})
    (root / 'github-token').write_text('fixture-no-network-token')
    os.chmod(root / 'github-token', 0o600)
    (root / 'backups').mkdir()
    (root / 'restic-password').write_text(secret)
    os.chmod(root / 'restic-password', 0o600)
    restic_env = {'RESTIC_REPOSITORY': str(root / 'encrypted-offsite-fixture'), 'RESTIC_PASSWORD_FILE': str(root / 'restic-password')}
    command(['restic', '-r', restic_env['RESTIC_REPOSITORY'], '--password-file', restic_env['RESTIC_PASSWORD_FILE'], 'init'])
    (root / 'offsite-config.json').write_bytes(canonical({'offHost': True, 'backupDirectory': str(root / 'backups/cd'), 'environment': restic_env}))
    os.chmod(root / 'offsite-config.json', 0o600)
    write_profile()
    counter = 100
    def request_for(sha, run_id):
        return {'sourceSha': sha, 'ciRunId': run_id, 'ciRunAttempt': 1, 'cdRunId': run_id + 1000}
    def fixture_ci(request, token=None, active=None):
        ci = {'id': request['ciRunId'], 'run_attempt': 1, 'workflow_id': CI_WORKFLOW_ID, 'path': '.github/workflows/ci.yml',
              'head_repository': {'full_name': REPOSITORY}, 'event': 'push', 'head_branch': 'master',
              'status': 'completed', 'conclusion': 'success', 'head_sha': request['sourceSha']}
        return authorize_ci(ci, request['sourceSha'], active)
    def fixture_attestation(path, request, profile):
        # Only the external GitHub identity/signature boundary is substituted here.
        manifest = auto_release.verify_bundle(path, file_digest(Path(path) / 'release.json'))
        require(manifest['ciReceipt'] == request, 'Fixture signed identity mismatch')
        return manifest
    def signed(info, request):
        path = info[0]
        manifest = json.loads((path / 'release.json').read_bytes())
        manifest['ciReceipt'] = request
        (path / 'release.json').write_bytes(canonical(manifest))
        return path
    def auto(info, request):
        with host_lock(root / 'shared.lock'):
            maintenance(profile, info[0])
            return automatic(root / 'profile.json', signed(info, request), request, verify=fixture_ci, attest=fixture_attestation)
    def denied(change):
        baseline_ids = command(['docker', 'ps', '-aq'])
        ci = {'id': 1, 'run_attempt': 1, 'workflow_id': CI_WORKFLOW_ID, 'path': '.github/workflows/ci.yml', 'head_repository': {'full_name': REPOSITORY},
              'event': 'push', 'head_branch': 'master', 'status': 'completed', 'conclusion': 'success', 'head_sha': new_sha} | change
        try:
            authorize_ci(ci, new_sha)
            raise AssertionError('Unsafe event accepted')
        except auto_release.ReleaseError:
            pass
        require(command(['docker', 'ps', '-aq']) == baseline_ids, 'Rejected CI changed Docker')
    record('automatic_failed_ci_does_not_deploy', lambda: denied({'conclusion': 'failure'}))
    record('automatic_pr_event_denied', lambda: denied({'event': 'pull_request'}))
    record('automatic_fork_event_denied', lambda: denied({'head_repository': {'full_name': 'fork/convy'}}))
    request = request_for(new_sha, counter)
    signed(good, request)
    (good[0] / 'release.sha256').write_text(file_digest(good[0] / 'release.json') + '\n')
    (good[0] / 'attestation.json').write_text('{}')
    transfer_files = {n: {'size': (good[0] / n).stat().st_size, 'sha256': file_digest(good[0] / n)} for n in broker.FILES}
    def invalid_stream():
        unchanged_images = set(command(['docker', 'image', 'ls', '-q', '--no-trunc']).split())
        stream = io.BytesIO()
        with tarfile.open(fileobj=stream, mode='w') as tar:
            entry = tarfile.TarInfo('images.tar')
            entry.type = tarfile.SYMTYPE
            entry.linkname = '/outside'
            tar.addfile(entry)
        stream.seek(0)
        try:
            broker.receive(stream, profile, {'files': transfer_files})
            raise AssertionError('Broker accepted an archive link')
        except auto_release.ReleaseError as error:
            require(str(error) == 'unsafe_upload_archive', 'Wrong broker rejection')
        require(set(command(['docker', 'image', 'ls', '-q', '--no-trunc']).split()) == unchanged_images, 'Receiver loaded a rejected image')
        with host_lock(root / 'shared.lock'):
            maintenance(profile)
    record('broker_archive_link_denied_without_image_load', invalid_stream)
    with tempfile.TemporaryFile() as transfer:
        with tarfile.open(fileobj=transfer, mode='w') as tar:
            for name in broker.FILES:
                tar.add(good[0] / name, arcname=name, recursive=False)
        transfer.seek(0)
        with host_lock(root / 'shared.lock'):
            received = broker.receive(transfer, profile, {'files': transfer_files})
    received_info = (received, file_digest(received / 'release.json'))
    auto_accepted = {}
    record('automatic_master_success_with_encrypted_backup_restore', lambda: auto_accepted.update(auto(received_info, request)))
    require(received.exists(), 'Maintenance deleted the live broker upload')
    shutil.rmtree(received)
    auto_release.recovery(auto_accepted['statePath'])
    require((root / 'api.env').read_bytes() == original_env, 'Automatic CD patched model/credentials')
    record('automatic_duplicate_noop', lambda: require(auto(good, request)['status'] == 'ALREADY_ACCEPTED', 'Duplicate changed accepted release'))
    def older():
        baseline_ids = command(['docker', 'ps', '-aq'])
        try:
            with host_lock(root / 'shared.lock'):
                automatic(root / 'profile.json', good[0], request_for(old_sha, 99), verify=fixture_ci, attest=fixture_attestation)
            raise AssertionError('Older accepted source overwrote current')
        except auto_release.ReleaseError as failure:
            require(str(failure) == 'older_ci_run_denied', 'Older run rejection mismatch')
        require(command(['docker', 'ps', '-aq']) == baseline_ids, 'Older run changed containers')
    record('automatic_older_run_denied', older)
    def competing():
        with host_lock(root / 'shared.lock'):
            result = subprocess.run(['python3', '-c', 'import sys; sys.path.insert(0,"/source/ops/vps"); from staging_host import host_lock;\nwith host_lock(sys.argv[1]): print("converso entered")', str(root / 'shared.lock')], capture_output=True)
            require(result.returncode != 0 and b'shared_host_busy_retry_later' in result.stderr, 'Converso bypassed host lock')
        with host_lock(root / 'shared.lock'):
            pass
    record('competing_convy_converso_host_lock', competing)
    def low(key, reason):
        old = profile[key]
        profile[key] = 2**63
        try:
            capacity(profile, 0)
            raise AssertionError('Resource gate bypassed')
        except auto_release.ReleaseError as failure:
            require(str(failure) == reason, 'Wrong resource rejection')
        finally:
            profile[key] = old
    record('insufficient_ram_fail_closed', lambda: low('minimumAvailableMemoryBytes', 'insufficient_ram_capacity'))
    record('insufficient_disk_fail_closed', lambda: low('minimumFreeBytes', 'insufficient_disk_capacity'))
    def interrupt():
        state_path = Path(auto_accepted['statePath'])
        state = auto_release.recovery(state_path)
        state['status'] = 'ACTIVATING'
        auto_release.save_state(state_path, state)
        (root / 'current.next').symlink_to(state_path / 'source')
        (state_path / 'state.json.tmp').write_bytes(b'partial atomic write')
        os.chmod(state_path / 'state.json.tmp', 0o600)
        with host_lock(root / 'shared.lock'):
            recover_interrupted(profile)
        require(inspect('api')['Image'] == old_image, 'Interrupted release did not recover prior image')
        (root / 'transactions/cd-active.json').unlink(missing_ok=True)
    record('interrupted_automatic_activation_restores_journal', interrupt)
    last = None
    for number in range(4, 10):
        sha = format(number, 'x') * 40
        image = build(str(number), sha, True)
        info = bundle('auto-' + str(number), sha, image)
        context_manifest = json.loads((info[0] / 'release.json').read_bytes())
        context_manifest['contextHashes'] = {'api': digest(str(number).encode()), 'worker': digest(str(number).encode())}
        (info[0] / 'release.json').write_bytes(canonical(context_manifest))
        last = auto(info, request_for(sha, counter + number))
        with host_lock(root / 'shared.lock'):
            cleanup = maintenance(profile)
        state_count = len(list((root / 'transactions').glob('*/state.json')))
        require(state_count <= 4, 'Release archives accumulated without bound')
        require(len(list((root / 'backups/cd').glob('backup-*.dump'))) == 2, 'CD backups accumulated without bound')
        require(command(['docker', 'image', 'inspect', old_image]).strip(), 'Unrelated running image removed')
    record('repeated_releases_bound_archives_images_and_backups', lambda: require(len(set(command(['docker', 'image', 'ls', project, '-q', '--no-trunc']).split())) <= 7, 'Owned images accumulated'))
    def unchanged_context():
        before_ids = [inspect(s)['Id'] for s in ('api', 'worker')]
        sha = 'a' * 40
        info = bundle('docs-only', sha, build('9', sha, True))
        manifest = json.loads((info[0] / 'release.json').read_bytes())
        manifest['contextHashes'] = {'api': digest(b'9'), 'worker': digest(b'9')}
        (info[0] / 'release.json').write_bytes(canonical(manifest))
        result = auto(info, request_for(sha, 150))
        require(result['status'] == 'ALREADY_APPLIED' and [inspect(s)['Id'] for s in ('api', 'worker')] == before_ids, 'Unchanged contexts recreated apps')
    record('documentation_commit_contexts_preserve_application_ids', unchanged_context)
    def automatic_health_failure():
        before_images = [inspect(s)['Image'] for s in ('api', 'worker')]
        try:
            auto(bad, request_for(bad_sha, 160))
            raise AssertionError('Bad automatic release accepted')
        except auto_release.ReleaseError as error:
            require(str(error).startswith('release_failed_previous_version_restored:'), 'Automatic health rollback failed')
        require([inspect(s)['Image'] for s in ('api', 'worker')] == before_images, 'Automatic rollback image mismatch')
        require(json.loads((root / 'transactions/cd-active.json').read_bytes())['ciRunId'] == 150, 'Failed release advanced CI ledger')
    record('automatic_health_failure_restores_images_and_ci_ledger', automatic_health_failure)
    def restore_after_cleanup():
        path = Path(last['statePath'])
        state = auto_release.recovery(path)
        previous = next(iter(state['previousImages'].values()))
        run_tags = json.loads(command(['docker', 'image', 'inspect', previous]))[0].get('RepoTags') or []
        for tag in run_tags:
            command(['docker', 'image', 'rm', tag])
        if subprocess.run(['docker', 'image', 'inspect', previous], capture_output=True).returncode == 0:
            command(['docker', 'image', 'rm', previous])
        with host_lock(root / 'shared.lock'):
            auto_release.restore(path, profile)
        require(inspect('api')['Image'] == previous, 'Previous image could not be restored after cleanup')
        with host_lock(root / 'shared.lock'):
            maintenance(profile)
        require((root / 'current').is_dir(), 'Rollback source pointer is missing')
    record('protected_previous_image_restore_after_housekeeping', restore_after_cleanup)
    record('unrelated_running_image_preserved', lambda: require(inspect('converso')['Image'] == old_image, 'Converso image changed'))
    require((root / 'api.env').read_bytes() == original_env, 'Protected bytes changed')
    print(json.dumps({'status': 'PASS', 'scenarios': results, 'sharedContainersPreserved': True, 'secretLogsAbsent': True,
                      'backupRestore': 'actual bounded isolated PostgreSQL and encrypted restic retrieval; local endpoint fixture'}), flush=True)
finally:
    # Fixture cleanup is scoped to this generated project; it is never part of release code.
    subprocess.run(['docker', 'compose', '--project-name', project, '-f', str(root / 'base.json'), '-f', str(root / 'override.json'), 'down', '--volumes'], stdout=subprocess.DEVNULL, stderr=subprocess.DEVNULL)
    for image in images:
        subprocess.run(['docker', 'image', 'rm', image], stdout=subprocess.DEVNULL, stderr=subprocess.DEVNULL)
