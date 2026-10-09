#!/usr/bin/env python3
"""Fixed-path watchdog: recovery only; never loads or applies a new candidate."""
import json
import os
from release_common import ReleaseError, require
from staging_host import host_lock, host_profile, recover_interrupted

if __name__ == '__main__':
    try:
        require(os.geteuid() == 0, 'installed_root_recovery_required')
        profile = host_profile('/etc/convy-staging/profile.json')
        with host_lock(profile['sharedLock']):
            recover_interrupted(profile)
        print(json.dumps({'status': 'RECOVERY_CHECKED'}))
    except Exception as error:
        reason = str(error) if isinstance(error, ReleaseError) else 'recovery_failed_output_withheld'
        print(json.dumps({'status': 'BUSY' if reason == 'shared_host_busy_retry_later' else 'FAILED', 'reason': reason}))
        raise SystemExit(0 if reason == 'shared_host_busy_retry_later' else 1)
