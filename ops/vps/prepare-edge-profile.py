#!/usr/bin/env python3
"""Render an explicit disabled CD profile for a reviewed external Traefik edge."""
import argparse
import copy
import json
from pathlib import Path
import sys

from release_common import SHA, canonical, digest, file_digest, json_read, require, run


def topology(live, source_sha, project='shared-traefik-edge', proxy='shared-traefik-edge', content='shared-traefik-content'):
    require(SHA.fullmatch(source_sha) is not None, 'reviewed_infrastructure_sha_required')
    result = {'kind': 'traefik-file-v1', 'infrastructureSourceSha': source_sha, 'project': project, 'containers': {}}
    for role, name in (('proxy', proxy), ('content', content)):
        c = live[name]
        result['containers'][role] = {'name': name, 'id': c['Id'], 'imageId': c['Image'],
            'configSha256': digest(canonical(c['Config'])), 'hostConfigSha256': digest(canonical(c['HostConfig'])),
            'mountsSha256': digest(canonical(sorted(c['Mounts'], key=lambda m: m['Destination']))),
            'networksSha256': digest(canonical(c['NetworkSettings']['Networks']))}
    config_mounts=[m for m in live[proxy]['Mounts'] if m['Destination']=='/etc/traefik' and m['Type']=='bind' and not m['RW']]
    require(len(config_mounts)==1, 'read_only_traefik_configuration_mount_required')
    directory=Path(config_mounts[0]['Source'])
    require(directory.is_dir() and not directory.is_symlink(), 'protected_traefik_configuration_directory_required')
    # The separately leased certificate worker alone owns this generated TLS state.
    generated_tls=directory/'dynamic/certificates.yml'
    result['protectedConfigurationHashes']={str(p):file_digest(p) for p in directory.rglob('*') if p.is_file() and p!=generated_tls}
    require(result['protectedConfigurationHashes'], 'reviewed_traefik_configuration_pins_missing')
    result['approvalSha256'] = digest(canonical(result))
    from edge_topology import edge_mounts
    edge_mounts({'edgeTopology': result}, live)
    return result


def main():
    parser = argparse.ArgumentParser(description='Write a local review artifact only; never update an installed profile.')
    parser.add_argument('--profile', required=True, type=Path)
    parser.add_argument('--infrastructure-source-sha', required=True)
    parser.add_argument('--applications-override', required=True, type=Path)
    parser.add_argument('--output', required=True, type=Path)
    args = parser.parse_args()
    original = json_read(args.profile)
    require(original.get('automaticEnabled') is False and original.get('modelPolicy') == 'preserve', 'disabled_preserving_profile_required')
    require(args.applications_override.is_absolute() and args.applications_override.is_file(), 'reviewed_application_override_required')
    require(args.output.is_absolute() and not args.output.exists(), 'new_absolute_review_output_required')
    ids = run(['docker', 'ps', '-aq']).decode().split()
    live = {c['Name'].lstrip('/'): c for c in json.loads(run(['docker', 'inspect', *ids]))}
    candidate = copy.deepcopy(original)
    candidate['edgeTopology'] = topology(live, args.infrastructure_source_sha)
    candidate['composeFiles'] = [*original['composeFiles'], str(args.applications_override)]
    candidate['composeHashes'] = {p: file_digest(p) for p in candidate['composeFiles']}
    candidate['preserveFiles'] = sorted(set(original['preserveFiles']) | {str(args.applications_override),str(Path(__file__).with_name('edge_topology.py'))} | set(candidate['edgeTopology']['protectedConfigurationHashes']))
    candidate['bootstrapStatus'] = 'DISABLED_REVIEWED_TRAEFIK_TRANSITION_INSTALLATION_PENDING'
    # Only validate normalized application topology; do not load images or create services.
    command = ['docker', 'compose', '--project-name', candidate['project']]
    for p in candidate['envFiles']: command += ['--env-file', p]
    for p in candidate['composeFiles']: command += ['-f', p]
    config = json.loads(run(command + ['config', '--format', 'json']))
    require('caddy' not in config['services'] and {'api','db'} <= set(config['services']), 'application_override_must_remove_caddy')
    with args.output.open('xb') as stream: stream.write(canonical(candidate))
    args.output.chmod(0o600)
    print(json.dumps({'status':'REVIEW_ARTIFACT_ONLY','automaticEnabled':False,'modelPolicy':'preserve','profileSha256':file_digest(args.output)}))


if __name__ == '__main__': main()
