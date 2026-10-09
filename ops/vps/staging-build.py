#!/usr/bin/env python3
"""Credential-free builder. Never accepts a PR artifact or a mutable source ref."""
import argparse
import importlib.util
import json
import os
from pathlib import Path
import uuid

from release_common import SERVICES, canonical, require, run
from staging_common import runner_request


if __name__ == '__main__':
    parser = argparse.ArgumentParser()
    parser.add_argument('--baseline', required=True)
    parser.add_argument('--output', required=True)
    args = parser.parse_args()
    request = runner_request()
    # Build every application from the tested tree. Host context hashes select which
    # services change; this also covers a skipped/superseded prior master run.
    services = list(SERVICES)
    spec = importlib.util.spec_from_file_location('builder', Path(__file__).with_name('build-release.py'))
    builder = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(builder)
    name = 'convy-ci-' + uuid.uuid4().hex[:12]
    run(['docker', 'buildx', 'create', '--name', name, '--driver', 'docker-container'])
    try:
        builder.build('.', request['sourceSha'], args.baseline, args.output, services, builder=name, receipt=request)
    finally:
        # Only this run's dedicated builder. No cache or images on the shared VPS.
        run(['docker', 'buildx', 'rm', name], timeout=120)
