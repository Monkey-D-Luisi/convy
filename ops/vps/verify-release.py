#!/usr/bin/env python3
import argparse
import json
from release_common import ReleaseError, verify_bundle

parser = argparse.ArgumentParser()
parser.add_argument('--bundle', required=True)
parser.add_argument('--manifest', required=True)
args = parser.parse_args()
try:
    manifest = verify_bundle(args.bundle, args.manifest)
    print(json.dumps({'status': 'VERIFIED', 'sourceSha': manifest['sourceSha'], 'manifestSha256': args.manifest}))
except Exception as error:
    print(json.dumps({'status': 'FAILED', 'reason': str(error) if isinstance(error, ReleaseError) else 'invalid_artifact_output_withheld'}))
    raise SystemExit(1)
