#!/usr/bin/env python3
"""Forced SSH command: bounded stdin upload, verified CI, installed controller only."""
import hashlib
import json
import os
from pathlib import Path
import signal
import sys
import tarfile
import uuid

from release_common import DIGEST, ReleaseError, json_read, require
from staging_common import verify_ci
from staging_host import automatic, capacity, host_lock, host_profile, maintenance, recover_interrupted

PROFILE = '/etc/convy-staging/profile.json'
FILES = {'release.json', 'release.sha256', 'images.tar', 'source.tar', 'attestation.json'}


def read_exact(stream, size):
    value = bytearray()
    while len(value) < size:
        chunk = stream.read(size - len(value))
        require(bool(chunk), 'partial_upload')
        value.extend(chunk)
    return bytes(value)


def receive(stream, profile, header):
    files = header['files']
    require(set(files) == FILES, 'unexpected_upload_files')
    total = 0
    for name, value in files.items():
        require(isinstance(value['size'], int) and value['size'] > 0 and DIGEST.fullmatch(value['sha256']) is not None, 'invalid_upload_header')
        require(name in ('images.tar', 'source.tar') or value['size'] <= 4 * 1024**2, 'metadata_too_large')
        total += value['size']
    try:
        capacity(profile, total, image_bytes=files['images.tar']['size'] * 2, rollback_bytes=profile['maximumRollbackArchiveBytes'], source_bytes=files['source.tar']['size'] * 2)
    except ReleaseError as failure:
        if str(failure) not in ('insufficient_disk_capacity', 'release_retention_budget_exceeded'):
            raise
        maintenance(profile, pressure=True)
        capacity(profile, total, image_bytes=files['images.tar']['size'] * 2, rollback_bytes=profile['maximumRollbackArchiveBytes'], source_bytes=files['source.tar']['size'] * 2)
    incoming = Path(profile['incomingRoot'])
    incoming.mkdir(mode=0o700, parents=True, exist_ok=True)
    require(not incoming.is_symlink() and incoming.stat().st_uid == os.geteuid() and incoming.stat().st_mode & 0o077 == 0, 'incoming_root_not_private')
    bundle = incoming / ('incoming-' + uuid.uuid4().hex)
    bundle.mkdir(mode=0o700)
    received = set()
    archive_bytes = 0
    for _ in FILES:
        # Parse only one 512-byte USTAR header. Generic tar readers consume PAX/
        # sparse metadata before yielding an entry, which can allocate host RAM.
        entry = tarfile.TarInfo.frombuf(read_exact(stream, 512), 'utf-8', 'strict')
        require(entry.name in FILES and entry.name not in received and entry.type in (tarfile.REGTYPE, tarfile.AREGTYPE) and
                not entry.linkname and entry.size == files[entry.name]['size'], 'unsafe_upload_archive')
        checksum = hashlib.sha256()
        remaining = entry.size
        fd = os.open(bundle / entry.name, os.O_WRONLY | os.O_CREAT | os.O_EXCL | os.O_NOFOLLOW, 0o600)
        with os.fdopen(fd, 'wb') as output:
            while remaining:
                data = read_exact(stream, min(1024 * 1024, remaining))
                output.write(data)
                checksum.update(data)
                remaining -= len(data)
            output.flush()
            os.fsync(output.fileno())
        padding = (-entry.size) % 512
        require(read_exact(stream, padding) == b'\0' * padding, 'invalid_upload_padding')
        archive_bytes += 512 + entry.size + padding
        require(checksum.hexdigest() == files[entry.name]['sha256'], 'upload_checksum_mismatch')
        received.add(entry.name)
    # Two end blocks followed by bounded record padding; no hidden extra entries.
    footer = ((archive_bytes + 1024 + 10239) // 10240) * 10240 - archive_bytes
    require(read_exact(stream, footer) == b'\0' * footer and stream.read(1) == b'', 'trailing_upload_data')
    require(received == FILES, 'upload_files_missing')
    return bundle


def main():
    require(os.geteuid() == 0 and len(sys.argv) == 1, 'installed_root_broker_required')
    profile = host_profile(PROFILE)
    # Entire receive/restore/load/apply/cleanup shares one cross-repository lease.
    with host_lock(profile['sharedLock']):
        signal.alarm(1800)
        header_bytes = sys.stdin.buffer.readline(8193)
        require(len(header_bytes) <= 8192 and header_bytes.endswith(b'\n'), 'invalid_broker_header')
        header = json.loads(header_bytes)
        token = Path(profile['githubTokenFile']).read_text().strip()
        active_path = Path(profile['stateRoot']) / 'cd-active.json'
        active = json_read(active_path) if active_path.exists() else None
        status = verify_ci(header['ci'], token, active)
        if status == 'ALREADY_ACCEPTED':
            return {'status': status, 'sourceSha': header['ci']['sourceSha']}
        recover_interrupted(profile)
        maintenance(profile)
        bundle = receive(sys.stdin.buffer, profile, header)
        try:
            return automatic(PROFILE, bundle, header['ci'])
        finally:
            # Candidate archive is hard linked in the accepted/failed transaction.
            import shutil
            shutil.rmtree(bundle, ignore_errors=False) if bundle.exists() else None


if __name__ == '__main__':
    try:
        print(json.dumps(main()))
    except Exception as error:
        print(json.dumps({'status': 'FAILED', 'reason': str(error) if isinstance(error, ReleaseError) else 'broker_failed_output_withheld'}))
        raise SystemExit(1)
