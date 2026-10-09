#!/usr/bin/env python3
"""Common staging lease; installed independently of mutable product releases."""
from contextlib import contextmanager
from functools import wraps
import os
from pathlib import Path
import stat
import sys

LOCK_PATH = '/run/lock/shared-staging-deployment.lock'
LOCK_FD = 8
MARKER = 'SHARED_STAGING_LOCK_FD'
_active_path = None


class LockError(RuntimeError):
    pass


def _require(condition, reason):
    if not condition:
        raise LockError(reason)


def _identity(info):
    return info.st_dev, info.st_ino


def _protected(info):
    return stat.S_ISREG(info.st_mode) and info.st_uid == 0 and info.st_gid == 0 and stat.S_IMODE(info.st_mode) == 0o600 and info.st_nlink == 1


def _path_info(path):
    _require(os.geteuid() == 0, 'shared_lock_root_required')
    path = Path(path)
    _require(path.is_absolute(), 'shared_lock_absolute_path_required')
    for parent in [path.parent, *path.parent.parents]:
        info = parent.lstat()
        _require(stat.S_ISDIR(info.st_mode) and info.st_uid == 0 and
                 (info.st_mode & 0o022 == 0 or info.st_mode & stat.S_ISVTX), 'shared_lock_unsafe_parent')
    info = path.lstat()
    _require(_protected(info), 'shared_lock_unprotected_inode')
    return info


def inherited_fd(path=None):
    """An environment marker is never proof of ownership: inspect the kernel FD."""
    path = path or _active_path or LOCK_PATH
    _require(os.environ.get(MARKER) == str(LOCK_FD), 'shared_lock_inherited_descriptor_required')
    info = _path_info(path)
    try:
        fd_info = os.fstat(LOCK_FD)
    except OSError:
        raise LockError('shared_lock_inherited_descriptor_invalid') from None
    _require(_protected(fd_info) and _identity(info) == _identity(fd_info), 'shared_lock_inode_mismatch')
    details = Path('/proc/self/fdinfo/' + str(LOCK_FD)).read_text().splitlines()
    # fdinfo describes locks on this open file description, not another opener.
    _require(any(line.startswith('lock:') and ' FLOCK ' in line and ' WRITE ' in line for line in details),
             'shared_lock_exclusive_ownership_required')
    return LOCK_FD


def lease_fds():
    return (inherited_fd(),) if MARKER in os.environ else ()


@contextmanager
def shared_lock(path=None):
    import fcntl
    global _active_path
    path = str(path or _active_path or LOCK_PATH)
    if MARKER in os.environ:
        inherited_fd(path)
        yield
        return
    info = _path_info(path)
    try:
        os.fstat(LOCK_FD)
    except OSError:
        pass
    else:
        raise LockError('shared_lock_descriptor_in_use')
    opened = os.open(path, os.O_RDWR | os.O_NOFOLLOW | os.O_CLOEXEC)
    owner = None
    previous_path = _active_path
    try:
        _require(_protected(os.fstat(opened)) and _identity(info) == _identity(os.fstat(opened)), 'shared_lock_inode_changed')
        try:
            fcntl.flock(opened, fcntl.LOCK_EX | fcntl.LOCK_NB)
        except BlockingIOError:
            raise LockError('shared_host_busy_retry_later') from None
        _require(_identity(_path_info(path)) == _identity(os.fstat(opened)), 'shared_lock_inode_changed')
        if opened != LOCK_FD:
            os.dup2(opened, LOCK_FD, inheritable=True)
            os.close(opened)
        else:
            os.set_inheritable(opened, True)
        owner = LOCK_FD
        _active_path = path
        os.environ[MARKER] = str(LOCK_FD)
        yield
    finally:
        if owner is not None:
            os.environ.pop(MARKER, None)
            _active_path = previous_path
            # Close only our reference. LOCK_UN would release surviving children.
            os.close(owner)
        else:
            os.close(opened)


def writer(function):
    @wraps(function)
    def guarded(*args, **kwargs):
        with shared_lock():
            return function(*args, **kwargs)
    return guarded


def main():
    if sys.argv[1:] == ['--check']:
        inherited_fd()
        return
    _require(len(sys.argv) > 2 and sys.argv[1] == '--', 'shared_lock_command_required')
    with shared_lock():
        os.execvpe(sys.argv[2], sys.argv[2:], os.environ)


if __name__ == '__main__':
    try:
        main()
    except (LockError, OSError):
        reason = sys.exc_info()[1]
        print(str(reason) if isinstance(reason, LockError) else 'shared_lock_unavailable', file=sys.stderr)
        raise SystemExit(75)
