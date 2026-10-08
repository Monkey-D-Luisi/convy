"""Real Docker state transitions, shared Caddy/PostgreSQL and failure injection."""
import argparse
import copy
import json
import os
import re
from pathlib import Path
import shutil
import subprocess
import sys
import tarfile
import time
from datetime import datetime, timezone

sys.path.insert(0, '/source/ops/vps')
from release_common import canonical, file_digest, digest

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
               'healthTimeoutSeconds': 5, 'minimumFreeBytes': 0,
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
    record('health_failure_automatic_rollback', lambda: require(apply(bad, expected=1)['reason'] == 'release_failed_previous_version_restored', 'Health rollback failed'))
    require(inspect('api')['Image'] == old_image and inspect('worker')['Image'] == old_image, 'Health rollback image mismatch')
    profile['acceptance'].append(['docker', 'exec', project + '-api', 'python3', '-c', 'import os; assert os.environ["VERSION"] != "2"'])
    write_profile()
    record('acceptance_failure_automatic_rollback', lambda: require(apply(good, expected=1)['reason'] == 'release_failed_previous_version_restored', 'Acceptance rollback failed'))
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
    print(json.dumps({'status': 'PASS', 'scenarios': results, 'sharedContainersPreserved': True, 'secretLogsAbsent': True}), flush=True)
finally:
    # Fixture cleanup is scoped to this generated project; it is never part of release code.
    subprocess.run(['docker', 'compose', '--project-name', project, '-f', str(root / 'base.json'), '-f', str(root / 'override.json'), 'down', '--volumes'], stdout=subprocess.DEVNULL, stderr=subprocess.DEVNULL)
    for image in images:
        subprocess.run(['docker', 'image', 'rm', image], stdout=subprocess.DEVNULL, stderr=subprocess.DEVNULL)
