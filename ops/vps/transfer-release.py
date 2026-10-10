#!/usr/bin/env python3
"""Retired unlocked transfer entry point; validates the host pin and fails closed."""
import argparse
import json
from pathlib import Path
import re

from release_common import ReleaseError, file_digest, require

def transfer(bundle, checksum, host, key, known_hosts, pin, remote_root):
    require(file_digest(known_hosts) == pin, 'known_hosts_pin_changed')
    require(re.fullmatch(r'[a-z_][a-z0-9_-]*@[a-zA-Z0-9.-]+', host) is not None, 'invalid_ssh_target')
    require(re.fullmatch(r'/[a-zA-Z0-9/_-]+', remote_root) is not None and '..' not in remote_root.split('/'), 'invalid_remote_root')
    raise ReleaseError('legacy_transfer_disabled_use_staging_broker_or_locked_admin_transaction')

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
