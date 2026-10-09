"""Static content and public release metadata in the application recovery journal."""
import os
from pathlib import Path, PurePosixPath
import re
import tarfile

from release_common import digest, file_digest, private_write, require

STATIC = {'legal': 'legal', 'public-site': 'public'}
MAX_STATIC_BYTES = 64 * 1024**2
MAX_STATIC_FILES = 4096
STATIC_RECOVERY_BYTES = 336 * 1024**2  # prior trees, tar overhead, recovery extraction and atomic replacement
METADATA_KEYS = ('Deploy__ReleaseSha', 'Deploy__LastDeployAt', 'Backend__Version', 'Mobile__AndroidVersion')


def protected_lines(content, managed_keys=METADATA_KEYS):
    keys = {key.encode() for key in managed_keys}
    return b''.join(line for line in content.splitlines(keepends=True) if line.partition(b'=')[0] not in keys)


def android_version(content):
    text = content.decode('utf-8')
    name = re.findall(r'^\s*versionName\s*=\s*"([^"\r\n]+)"\s*$', text, re.M)
    code = re.findall(r'^\s*versionCode\s*=\s*([0-9]+)\s*$', text, re.M)
    require(len(name) == len(code) == 1 and re.fullmatch(r'[A-Za-z0-9.+_-]{1,80}', name[0]), 'android_version_not_reviewed')
    return name[0] + '+' + code[0]


def catalog(root):
    root = Path(root)
    require(root.is_dir() and not root.is_symlink(), 'static_root_missing_or_symlink')
    files = {}
    total = 0
    entries = sorted(root.rglob('*'))
    require(len(entries) <= MAX_STATIC_FILES, 'static_content_exceeds_budget')
    for p in entries:
        require(not p.is_symlink() and (p.is_dir() or p.is_file()), 'unsafe_static_entry')
        if p.is_file():
            total += p.stat().st_size
            files[p.relative_to(root).as_posix()] = file_digest(p)
            require(total <= MAX_STATIC_BYTES and len(files) <= MAX_STATIC_FILES, 'static_content_exceeds_budget')
    return files


def archive_content(path):
    files = {name: {} for name in STATIC}
    total = 0
    seen = set()
    with tarfile.open(path) as archive:
        for member in archive:
            parts = PurePosixPath(member.name).parts
            if not parts or parts[0] not in STATIC:
                continue
            require(not PurePosixPath(member.name).is_absolute() and '..' not in parts and
                    (member.isfile() or member.isdir()) and member.name.rstrip('/') == '/'.join(parts) and
                    '/'.join(parts) not in seen, 'unsafe_static_archive')
            seen.add('/'.join(parts))
            require(len(seen) <= MAX_STATIC_FILES, 'static_content_exceeds_budget')
            if member.isfile():
                require(len(parts) > 1, 'invalid_static_path')
                total += member.size
                require(total <= MAX_STATIC_BYTES and sum(map(len, files.values())) < MAX_STATIC_FILES, 'static_content_exceeds_budget')
                files[parts[0]]['/'.join(parts[1:])] = digest(archive.extractfile(member).read())
        require(all(files.values()), 'static_source_missing')
        mobile = archive.extractfile('mobile/androidApp/build.gradle.kts')
        require(mobile is not None, 'android_version_source_missing')
        version = android_version(mobile.read())
    return files, version


def roots(profile):
    parent = Path(profile['current']).parent
    return {name: parent / destination for name, destination in STATIC.items()}


def metadata_paths(profile):
    parent = Path(profile['current']).parent
    return parent / 'shared/release.env', parent / 'shared/release-metadata/accepted.json'


def static_inputs(profile, manifest, config):
    mounts = config['services']['caddy'].get('volumes', [])
    values = {}
    for name, root in roots(profile).items():
        info = root.stat()
        require(not root.is_symlink() and root.is_dir() and info.st_uid == os.geteuid() and info.st_mode & 0o022 == 0,
                'static_root_not_owned')
        require(any(m.get('type') == 'bind' and m.get('source') == str(root) and
                    m.get('target') == '/srv/' + STATIC[name] and m.get('read_only') is True for m in mounts), 'static_caddy_mount_not_reviewed')
        values[name] = {'files': catalog(root), 'device': info.st_dev, 'inode': info.st_ino}
    changed = [name for name in STATIC if values[name]['files'] != manifest['staticFiles'][name]]
    return values, changed


def public_write(path, content):
    path = Path(path)
    require(not path.is_symlink(), 'public_file_symlink')
    private_write(path, content)
    os.chmod(path, 0o644)


def publish_tree(source, destination):
    source, destination = Path(source), Path(destination)
    wanted = catalog(source)
    current = catalog(destination)
    # Keep the Caddy bind root inode. Each file replacement is atomic; the journal
    # restores the whole release if any file, health check or acceptance step fails.
    for name in sorted(set(current) - set(wanted), reverse=True):
        (destination / name).unlink()
    for path in sorted((p for p in destination.rglob('*') if p.is_dir()), key=lambda p: len(p.parts), reverse=True):
        if not any(path.iterdir()):
            path.rmdir()
    for name in wanted:
        target = destination / name
        for parent in reversed(target.parents):
            if parent == destination or destination in parent.parents:
                require(not parent.is_symlink(), 'static_parent_symlink')
                parent.mkdir(mode=0o755, exist_ok=True)
        require(not target.exists() or target.is_file(), 'static_file_directory_conflict')
        public_write(target, (source / name).read_bytes())
    for path in sorted((p for p in destination.rglob('*') if p.is_dir()), key=lambda p: len(p.parts), reverse=True):
        if not any(path.iterdir()):
            path.rmdir()
    require(catalog(destination) == wanted, 'static_publication_mismatch')


def save_static(path, profile):
    target = Path(path) / 'previous-static.tar'
    with tarfile.open(target, 'w', format=tarfile.USTAR_FORMAT, dereference=True) as archive:
        for name, root in roots(profile).items():
            catalog(root)
            archive.add(root, arcname=name)
    os.chmod(target, 0o600)
    return file_digest(target)


def restore_static(path, profile, state):
    for name, root in roots(profile).items():
        info = root.stat()
        expected = state['staticBefore'][name]
        require(not root.is_symlink() and (info.st_dev, info.st_ino) == (expected['device'], expected['inode']), 'static_bind_root_changed')
    import tempfile
    from release_common import extract_source
    with tempfile.TemporaryDirectory(dir=path, prefix='restore-static-') as temporary:
        restored = Path(temporary)
        extract_source(Path(path) / 'previous-static.tar', restored)
        for name, root in roots(profile).items():
            require(catalog(restored / name) == state['staticBefore'][name]['files'], 'static_recovery_mismatch')
            publish_tree(restored / name, root)


def patch_metadata(content, metadata):
    require(not content or content.endswith(b'\n'), 'release_env_requires_final_newline')
    require(not content.startswith(b'\xef\xbb\xbf'), 'release_env_requires_utf8_without_bom')
    values = dict(zip(METADATA_KEYS, (metadata['sourceSha'], metadata['acceptedAtUtc'], metadata['backendVersion'], metadata['androidVersion'])))
    lines = content.decode('utf-8').splitlines(keepends=True)
    seen = set()
    result = []
    for line in lines:
        key = line.partition('=')[0]
        if key in values:
            require(key not in seen, 'duplicate_release_metadata')
            seen.add(key)
            result.append(key + '=' + values[key] + '\n')
        else:
            result.append(line)
    result.extend(key + '=' + value + '\n' for key, value in values.items() if key not in seen)
    return ''.join(result).encode()


def candidate_metadata(manifest, before, api_changed, at):
    env = before['services']['api'].get('environment', {})
    return {'format': 1, 'sourceSha': manifest['sourceSha'], 'acceptedAtUtc': at,
            'backendSourceSha': manifest['sourceSha'] if api_changed else env.get('Deploy__ReleaseSha'),
            'backendVersion': manifest['sourceSha'][:12] if api_changed else env.get('Backend__Version', env.get('Deploy__ReleaseSha', 'unknown')[:12]),
            'backendDeployedAtUtc': at if api_changed else env.get('Deploy__LastDeployAt'),
            'androidVersion': manifest['mobileAndroidVersion']}
