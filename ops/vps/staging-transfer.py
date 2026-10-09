#!/usr/bin/env python3
"""Stream the signed bundle to a constrained principal using the pinned SSH host."""
import argparse
import json
import os
from pathlib import Path
import re
import subprocess
import tarfile
import tempfile

from release_common import DIGEST, ReleaseError, canonical, file_digest, require, verify_bundle
from staging_common import runner_request

FILES = ('release.json', 'release.sha256', 'images.tar', 'source.tar', 'attestation.json')


def transfer(bundle, host, user, key, known, known_digest):
    request = runner_request()
    require(user == 'convy-cd' and re.fullmatch(r'[A-Za-z0-9.-]+', host) is not None, 'constrained_ssh_identity_required')
    require(DIGEST.fullmatch(known_digest) is not None and file_digest(known) == known_digest, 'ssh_host_pin_mismatch')
    manifest = verify_bundle(bundle, Path(bundle, 'release.sha256').read_text().strip())
    require(manifest.get('ciReceipt') == request, 'release_ci_receipt_mismatch')
    header = {'ci': request, 'files': {n: {'size': Path(bundle, n).stat().st_size, 'sha256': file_digest(Path(bundle, n))} for n in FILES}}
    options = ['ssh', '-T', '-i', key, '-o', 'BatchMode=yes', '-o', 'IdentitiesOnly=yes', '-o', 'StrictHostKeyChecking=yes',
               '-o', 'UpdateHostKeys=no', '-o', 'HostKeyAlgorithms=ssh-ed25519', '-o', 'UserKnownHostsFile=' + str(Path(known).resolve()),
               '-o', 'ConnectTimeout=15', '-o', 'ServerAliveInterval=15', '-o', 'ServerAliveCountMax=3', user + '@' + host,
               'sudo -n /usr/local/libexec/convy-staging-broker']
    # Disk-backed stream keeps runner memory bounded; it is removed even on failure.
    with tempfile.TemporaryFile() as data:
        data.write(canonical(header) + b'\n')
        with tarfile.open(fileobj=data, mode='w|') as archive:
            for name in FILES:
                archive.add(Path(bundle) / name, arcname=name, recursive=False)
        data.seek(0)
        try:
            result = subprocess.run(options, stdin=data, capture_output=True, timeout=1800)
            require(file_digest(known) == known_digest, 'ssh_host_pin_changed')
            response = json.loads(result.stdout)
            require(result.returncode == 0, 'staging_broker_rejected:' + response.get('reason', 'failure'))
            require(response.get('status') in ('ACCEPTED', 'ALREADY_APPLIED', 'ALREADY_ACCEPTED') and response.get('sourceSha') == request['sourceSha'], 'staging_acceptance_receipt_missing')
            print(json.dumps(response))
        except (OSError, ValueError, subprocess.TimeoutExpired):
            raise ReleaseError('staging_transfer_failed_output_withheld') from None


if __name__ == '__main__':
    parser = argparse.ArgumentParser()
    parser.add_argument('--bundle', required=True)
    parser.add_argument('--host', required=True)
    parser.add_argument('--user', default='convy-cd')
    parser.add_argument('--key', required=True)
    parser.add_argument('--known-hosts', required=True)
    parser.add_argument('--known-hosts-sha256', required=True)
    args = parser.parse_args()
    try:
        transfer(args.bundle, args.host, args.user, args.key, args.known_hosts, args.known_hosts_sha256)
    except Exception as error:
        print(json.dumps({'status': 'FAILED', 'reason': str(error) if isinstance(error, ReleaseError) else 'transfer_failed_output_withheld'}))
        raise SystemExit(1)
