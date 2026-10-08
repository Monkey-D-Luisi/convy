#!/usr/bin/env python3
"""Pinned SSH transfer only; never loads images or activates a release."""
import argparse
import json
from pathlib import Path
import re
import shlex
import uuid

from release_common import ReleaseError, file_digest, require, run, verify_bundle

def transfer(bundle, checksum, host, key, known_hosts, pin, remote_root):
    manifest = verify_bundle(bundle, checksum)
    require(file_digest(known_hosts) == pin, 'known_hosts_pin_changed')
    require(re.fullmatch(r'[a-z_][a-z0-9_-]*@[a-zA-Z0-9.-]+', host) is not None, 'invalid_ssh_target')
    require(re.fullmatch(r'/[a-zA-Z0-9/_-]+', remote_root) is not None and '..' not in remote_root.split('/'), 'invalid_remote_root')
    destination = remote_root.rstrip('/') + '/' + manifest['sourceSha'] + '-' + checksum[:12]
    incoming = destination + '.incoming-' + uuid.uuid4().hex
    options = ['-o', 'BatchMode=yes', '-o', 'StrictHostKeyChecking=yes', '-o', 'UpdateHostKeys=no',
               '-o', 'IdentitiesOnly=yes', '-o', 'HostKeyAlgorithms=ssh-ed25519',
               '-o', 'UserKnownHostsFile=' + str(Path(known_hosts).resolve()), '-i', str(Path(key).resolve())]
    command = 'umask 077; test ! -e ' + shlex.quote(destination) + ' && mkdir -m 700 -p ' + shlex.quote(incoming)
    run(['ssh', *options, host, command])
    scripts = Path(__file__).parent
    run(['scp', *options, *[str(Path(bundle) / name) for name in ('release.json', 'images.tar', 'source.tar')],
         str(scripts / 'release_common.py'), str(scripts / 'verify-release.py'), host + ':' + incoming + '/'], timeout=1200)
    verified = json.loads(run(['ssh', *options, host, 'python3 ' + incoming + '/verify-release.py --bundle ' + incoming + ' --manifest ' + checksum], timeout=900))
    require(verified.get('status') == 'VERIFIED' and verified.get('manifestSha256') == checksum, 'remote_verification_failed')
    run(['ssh', *options, host, 'test ! -e ' + destination + ' && mv -T ' + incoming + ' ' + destination])
    require(file_digest(known_hosts) == pin, 'known_hosts_pin_changed')
    return {'status': 'TRANSFER_VERIFIED', 'sourceSha': manifest['sourceSha'], 'manifestSha256': checksum, 'remoteBundle': destination}

if __name__ == '__main__':
    parser = argparse.ArgumentParser()
    for name in ('bundle', 'manifest', 'host', 'key', 'known-hosts', 'pin', 'remote-root'):
        parser.add_argument('--' + name, required=True)
    args = parser.parse_args()
    try:
        print(json.dumps(transfer(args.bundle, args.manifest, args.host, args.key, args.known_hosts, args.pin, args.remote_root)))
    except Exception as error:
        print(json.dumps({'status': 'FAILED', 'reason': str(error) if isinstance(error, ReleaseError) else 'transfer_failed_output_withheld'}))
        raise SystemExit(1)
