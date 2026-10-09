"""Launch Linux fixtures against the local Docker daemon, never a VPS."""
import json
from pathlib import Path
import subprocess
import uuid

source = Path(__file__).resolve().parents[3]
token = 'convy-release-test-' + uuid.uuid4().hex[:10]
image = token + ':controller'
volume = token + '-files'

def run(args):
    subprocess.run(args, check=True)

try:
    run(['docker', 'build', '-t', image, '-f', str(source / 'ops/vps/tests/fixture.Dockerfile'), str(source)])
    run(['docker', 'volume', 'create', volume])
    root = json.loads(subprocess.check_output(['docker', 'volume', 'inspect', volume]))[0]['Mountpoint']
    run(['docker', 'run', '--rm', '-v', volume + ':' + root, '-v', str(source) + ':/source:ro',
         '-v', '/var/run/docker.sock:/var/run/docker.sock', image,
         '/source/ops/vps/tests/linux_release_fixture.py', '--root', root, '--project', token])
finally:
    subprocess.run(['docker', 'volume', 'rm', volume], check=False)
    subprocess.run(['docker', 'image', 'rm', image], check=False)
