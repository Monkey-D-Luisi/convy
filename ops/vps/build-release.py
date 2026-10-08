#!/usr/bin/env python3
"""Build only on a trusted workstation/CI, using immutable Git archives."""
import argparse
import json
from pathlib import Path
import tempfile

from release_common import (SERVICES, SHA, ReleaseError, canonical, extract_source,
                            file_digest, migration_digest, migration_ids, require, run, verify_bundle)

BUILDS = {
    'api': ('backend', 'docker/backend/Dockerfile'),
    'worker': ('backend', 'docker/worker/Dockerfile'),
    'dashboard': ('dashboard', 'dashboard/Dockerfile'),
    'auth': ('auth', 'auth/Dockerfile'),
    'mcp': ('mcp', 'mcp/Dockerfile'),
}

def build(repo, source, baseline, output, services):
    require(SHA.fullmatch(source) is not None and SHA.fullmatch(baseline) is not None, 'exact_commit_required')
    require(bool(services) and len(set(services)) == len(services) and set(services) <= set(SERVICES), 'invalid_services')
    output = Path(output)
    require(not output.exists(), 'immutable_output_already_exists')
    for sha in (source, baseline):
        require(run(['git', '-C', repo, 'rev-parse', sha + '^{commit}']).decode().strip() == sha, 'commit_not_available')
    with tempfile.TemporaryDirectory() as temporary:
        temp = Path(temporary)
        tree = temp / 'source'
        old = temp / 'baseline'
        tree.mkdir()
        old.mkdir()
        for sha, path in ((source, tree), (baseline, old)):
            archive = temp / (sha + '.tar')
            archive.write_bytes(run(['git', '-C', repo, 'archive', '--format=tar', sha]))
            extract_source(archive, path)
        migration = migration_digest(tree)
        require(migration == migration_digest(old), 'schema_change_requires_separate_recovery_review')
        compose_path = 'docker/docker-compose.vps.yml'
        require(file_digest(tree / compose_path) == file_digest(old / compose_path), 'compose_change_requires_separate_review')
        images = {}
        for service in services:
            context, dockerfile = BUILDS[service]
            tag = 'convy-reviewed-' + service + ':' + source
            run(['docker', 'build', '--platform', 'linux/amd64', '--label',
                 'org.opencontainers.image.revision=' + source, '-t', tag,
                 '-f', tree / dockerfile, tree / context], timeout=1800)
            info = json.loads(run(['docker', 'image', 'inspect', tag]))[0]
            require(info['Os'] == 'linux' and info['Architecture'] == 'amd64', 'wrong_build_platform')
            images[service] = info['Id']
        output.mkdir(parents=True)
        (output / 'source.tar').write_bytes((temp / (source + '.tar')).read_bytes())
        run(['docker', 'image', 'save', '-o', output / 'images.tar', *sorted(set(images.values()))], timeout=900)
        manifest = {'format': 1, 'sourceSha': source, 'baselineSha': baseline, 'platform': 'linux/amd64',
                    'schemaPolicy': 'unchanged', 'migrationSha256': migration, 'migrationIds': migration_ids(tree), 'composeSha256': file_digest(tree / compose_path),
                    'images': images, 'files': {p: file_digest(output / p) for p in ('source.tar', 'images.tar')}}
        (output / 'release.json').write_bytes(canonical(manifest))
        checksum = file_digest(output / 'release.json')
        checked = verify_bundle(output, checksum)
        manifest['configIds'] = checked['configIds']
        (output / 'release.json').write_bytes(canonical(manifest))
        checksum = file_digest(output / 'release.json')
        verify_bundle(output, checksum)
        (output / 'release.sha256').write_text(checksum + '\n')
        print(json.dumps({'status': 'ARTIFACT_READY', 'sourceSha': source, 'manifestSha256': checksum, 'images': images}))

if __name__ == '__main__':
    parser = argparse.ArgumentParser()
    parser.add_argument('--repo', default='.')
    parser.add_argument('--source', required=True)
    parser.add_argument('--baseline', required=True)
    parser.add_argument('--output', required=True)
    parser.add_argument('--services', nargs='+', choices=SERVICES, default=list(SERVICES))
    args = parser.parse_args()
    try:
        build(args.repo, args.source, args.baseline, args.output, args.services)
    except (ReleaseError, OSError, ValueError) as error:
        print(json.dumps({'status': 'FAILED', 'reason': str(error) if isinstance(error, ReleaseError) else 'build_input_failed'}))
        raise SystemExit(1)
