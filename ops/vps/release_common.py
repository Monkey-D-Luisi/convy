"""Shared immutable artifact and secret-safe subprocess boundaries."""
import hashlib
import gzip
import json
import os
from pathlib import Path
import re
import subprocess
import tarfile

SERVICES = ('api', 'worker', 'dashboard', 'auth', 'mcp')
SHA = re.compile(r'[a-f0-9]{40}')
DIGEST = re.compile(r'[a-f0-9]{64}')
IMAGE = re.compile(r'sha256:[a-f0-9]{64}')
MODEL_PATCH = {
    'OpenAI__ParsingModel': 'gpt-6-luna',
    'OpenAI__Costs__ParsingInputMicrosPer1KTokens': '100',
    'OpenAI__Costs__ParsingCachedInputMicrosPer1KTokens': '10',
    'OpenAI__Costs__ParsingCacheWriteMicrosPer1KTokens': '125',
    'OpenAI__Costs__ParsingOutputMicrosPer1KTokens': '500',
}
LEGACY_PRICE = 'OpenAI__Costs__ParsingReasoningMicrosPer1KTokens'

class ReleaseError(Exception):
    pass

def require(condition, code):
    if not condition:
        raise ReleaseError(code)

def run(args, data=None, timeout=600):
    try:
        if isinstance(data, Path):
            with data.open('rb') as stream:
                result = subprocess.run([str(a) for a in args], stdin=stream, capture_output=True, timeout=timeout)
        else:
            result = subprocess.run([str(a) for a in args], input=data, capture_output=True, timeout=timeout)
    except (OSError, subprocess.TimeoutExpired):
        raise ReleaseError('command_unavailable_or_timed_out') from None
    require(result.returncode == 0, 'command_failed_output_withheld')
    return result.stdout

def canonical(value):
    return json.dumps(value, sort_keys=True, separators=(',', ':')).encode()

def digest(value):
    return hashlib.sha256(value).hexdigest()

def file_digest(path):
    value = hashlib.sha256()
    with Path(path).open('rb') as stream:
        for chunk in iter(lambda: stream.read(1024 * 1024), b''):
            value.update(chunk)
    return value.hexdigest()

def private_write(path, content):
    path = Path(path)
    temporary = path.with_name(path.name + '.tmp')
    fd = os.open(temporary, os.O_WRONLY | os.O_CREAT | os.O_EXCL, 0o600)
    try:
        with os.fdopen(fd, 'wb') as stream:
            stream.write(content)
            stream.flush()
            os.fsync(stream.fileno())
        if path.exists():
            info = path.stat()
            os.chmod(temporary, info.st_mode & 0o777)
            if hasattr(os, 'chown'):
                os.chown(temporary, info.st_uid, info.st_gid)
        os.replace(temporary, path)
    finally:
        temporary.unlink(missing_ok=True)
    if hasattr(os, 'O_DIRECTORY'):
        fd = os.open(path.parent, os.O_RDONLY | os.O_DIRECTORY)
        try:
            os.fsync(fd)
        finally:
            os.close(fd)

def json_read(path):
    try:
        return json.loads(Path(path).read_bytes())
    except (ValueError, OSError):
        raise ReleaseError('invalid_or_missing_json') from None

def migration_digest(root):
    paths = sorted(Path(root).glob('backend/src/Convy.Infrastructure/Migrations/*.cs'))
    require(bool(paths), 'migration_catalog_missing')
    return digest(canonical({p.name: file_digest(p) for p in paths}))

def migration_ids(root):
    return sorted(p.stem for p in Path(root).glob('backend/src/Convy.Infrastructure/Migrations/*.cs')
                  if re.fullmatch(r'[0-9]{14}_[A-Za-z0-9_]+', p.stem))

def extract_source(archive, destination):
    with tarfile.open(archive) as source:
        for member in source.getmembers():
            path = Path(member.name)
            require(not path.is_absolute() and '..' not in path.parts and
                    (member.isfile() or member.isdir()), 'unsafe_source_archive')
        source.extractall(destination, filter='data')

def verify_bundle(bundle, expected):
    bundle = Path(bundle)
    require(DIGEST.fullmatch(expected) is not None, 'invalid_manifest_digest')
    require(file_digest(bundle / 'release.json') == expected, 'manifest_digest_mismatch')
    manifest = json_read(bundle / 'release.json')
    require(manifest.get('format') == 1 and manifest.get('platform') == 'linux/amd64', 'unsupported_artifact')
    require(SHA.fullmatch(manifest.get('sourceSha', '')) is not None, 'invalid_source_sha')
    require(SHA.fullmatch(manifest.get('baselineSha', '')) is not None, 'invalid_baseline_sha')
    require(manifest.get('schemaPolicy') == 'unchanged' and DIGEST.fullmatch(manifest.get('migrationSha256', '')) is not None, 'migration_not_supported')
    require(isinstance(manifest.get('migrationIds'), list) and bool(manifest['migrationIds']) and
            all(re.fullmatch(r'[0-9]{14}_[A-Za-z0-9_]+', value) for value in manifest['migrationIds']), 'migration_ids_missing')
    require(bool(manifest.get('images')) and set(manifest['images']) <= set(SERVICES), 'invalid_service_selection')
    for name in ('source.tar', 'images.tar'):
        require(file_digest(bundle / name) == manifest['files'].get(name), 'artifact_digest_mismatch')
    from release_content import archive_content
    static_files, android = archive_content(bundle / 'source.tar')
    require(manifest.get('staticFiles') == static_files and manifest.get('mobileAndroidVersion') == android,
            'static_or_android_manifest_mismatch')
    expected_ids = set()
    for service, image in manifest['images'].items():
        require(IMAGE.fullmatch(image) is not None, 'invalid_image_id')
        expected_ids.add(image)
    with tarfile.open(bundle / 'images.tar') as archive:
        metadata = json.load(archive.extractfile('manifest.json'))
        actual_ids = set()
        for item in metadata:
            require(not item.get('RepoTags'), 'mutable_image_tags_not_allowed')
            data = archive.extractfile(item['Config']).read()
            image = json.loads(data)
            require(image.get('os') == 'linux' and image.get('architecture') == 'amd64', 'wrong_image_platform')
            require(image.get('config', {}).get('Labels', {}).get('org.opencontainers.image.revision') == manifest['sourceSha'], 'image_source_mismatch')
            actual_ids.add('sha256:' + digest(data))
        identities = {value: value for value in actual_ids}
        def resolve(descriptor):
            identifier = descriptor['digest']
            raw = archive.extractfile('blobs/sha256/' + identifier.removeprefix('sha256:')).read()
            require('sha256:' + digest(raw) == identifier, 'oci_digest_mismatch')
            value = json.loads(raw)
            if 'manifests' in value:
                configs = set().union(*(resolve(item) for item in value['manifests']))
            else:
                configs = {value['config']['digest']} & actual_ids
            if len(configs) == 1:
                identities[identifier] = next(iter(configs))
            return configs
        if 'index.json' in archive.getnames():
            for descriptor in json.load(archive.extractfile('index.json'))['manifests']:
                resolve(descriptor)
        require(expected_ids <= set(identities) and {identities[i] for i in expected_ids} == actual_ids, 'image_archive_mismatch')
        if 'configIds' in manifest:
            require(manifest['configIds'] == {s: identities[i] for s, i in manifest['images'].items()}, 'config_identity_mismatch')
        manifest['configIds'] = {s: identities[i] for s, i in manifest['images'].items()}
        # Containerd may retain compressed content and unpacked snapshots. Inspect
        # unique layer bytes rather than assuming Engine's Size is expanded usage.
        expanded = 0
        stored = 0
        for name in sorted({layer for item in metadata for layer in item['Layers']}):
            member = archive.getmember(name)
            require(member.isfile(), 'image_layer_not_regular_file')
            stored += member.size
            stream = archive.extractfile(member)
            magic = stream.read(2)
            stream.seek(0)
            reader = gzip.GzipFile(fileobj=stream) if magic == b'\x1f\x8b' else stream
            while chunk := reader.read(1024 * 1024):
                expanded += len(chunk)
                require(expanded <= 16 * 1024**3, 'image_expansion_exceeds_host_envelope')
            reader.close()
        storage = stored + 2 * expanded
        if 'imageStorageBytes' in manifest:
            require(manifest['imageStorageBytes'] == storage, 'image_storage_estimate_mismatch')
        manifest['imageStorageBytes'] = storage
    return manifest

def patch_model(content):
    require(b'\r' not in content, 'environment_must_use_lf')
    lines = content.decode('utf-8').splitlines(keepends=True)
    changed = set()
    result = []
    for line in lines:
        key = line.split('=', 1)[0]
        if key in MODEL_PATCH or key == LEGACY_PRICE:
            require(key not in changed, 'duplicate_model_parameter')
            changed.add(key)
            if key in MODEL_PATCH:
                result.append(key + '=' + MODEL_PATCH[key] + '\n')
        else:
            result.append(line)
    if result and not result[-1].endswith('\n'):
        result[-1] += '\n'
    for key, value in MODEL_PATCH.items():
        if key not in changed:
            result.append(key + '=' + value + '\n')
    return ''.join(result).encode()
