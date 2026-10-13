"""Explicit root-reviewed transition to an independently managed Traefik edge."""
from pathlib import Path
from release_common import DIGEST, SHA, IMAGE, canonical, digest, file_digest, require


def edge_mounts(profile, live):
    topology = profile.get('edgeTopology')
    require(isinstance(topology, dict) and topology.get('kind') == 'traefik-file-v1', 'edge_topology_not_reviewed')
    require(SHA.fullmatch(topology.get('infrastructureSourceSha', '')) is not None, 'edge_source_not_pinned')
    containers = topology.get('containers', {})
    require(set(containers) == {'proxy', 'content'}, 'edge_container_pins_incomplete')
    require(DIGEST.fullmatch(topology.get('approvalSha256', '')) is not None and
            digest(canonical({k: v for k, v in topology.items() if k != 'approvalSha256'})) == topology['approvalSha256'],
            'edge_topology_approval_mismatch')
    values = {}
    for role, pin in containers.items():
        require(pin.get('name') in live and IMAGE.fullmatch(pin.get('imageId', '')) is not None,
                'reviewed_edge_container_missing')
        container = live[pin['name']]
        require(container['State']['Running'] and container['Id'] == pin['id'] and container['Image'] == pin['imageId'] and
                digest(canonical(container['Config'])) == pin['configSha256'] and
                digest(canonical(container['HostConfig'])) == pin['hostConfigSha256'] and
                digest(canonical(sorted(container['Mounts'], key=lambda m: m['Destination']))) == pin['mountsSha256'] and
                digest(canonical(container['NetworkSettings']['Networks'])) == pin['networksSha256'],
                'reviewed_edge_runtime_drift')
        require(container['Config'].get('Labels', {}).get('com.docker.compose.project') == topology['project'],
                'edge_project_ownership_mismatch')
        values[role] = container
    require(values['content']['HostConfig']['NetworkMode'] == 'container:' + values['proxy']['Id'],
            'edge_content_namespace_mismatch')
    for path, expected in topology.get('protectedConfigurationHashes', {}).items():
        require(Path(path).is_absolute() and Path(path).is_file() and not Path(path).is_symlink() and
                file_digest(path) == expected, 'reviewed_edge_configuration_changed')
    return [{'type': m['Type'], 'source': m['Source'], 'target': m['Destination'], 'read_only': not m['RW']}
            for m in values['content']['Mounts']]
