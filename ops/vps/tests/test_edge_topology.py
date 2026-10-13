import copy
from pathlib import Path
import sys
import unittest

sys.path.insert(0, str(Path(__file__).resolve().parents[1]))
from edge_topology import edge_mounts
from release_common import ReleaseError, canonical, digest


class EdgeTopologyTests(unittest.TestCase):
    def fixtures(self):
        live = {}
        pins = {}
        for role in ('proxy', 'content'):
            name = 'shared-traefik-' + role
            container = {'Id': role * 16, 'Image': 'sha256:' + 'b' * 64, 'State': {'Running': True},
                         'Config': {'Labels': {'com.docker.compose.project': 'shared-traefik-edge'}},
                         'HostConfig': {'NetworkMode': 'container:' + 'proxy' * 16} if role == 'content' else {},
                         'NetworkSettings': {'Networks': {}}, 'Mounts': [{'Type': 'bind', 'Source': '/opt/convy/public', 'Destination': '/srv/public', 'RW': False}]}
            live[name] = container
            pins[role] = {'name': name, 'id': container['Id'], 'imageId': container['Image'],
                          'configSha256': digest(canonical(container['Config'])),
                          'hostConfigSha256': digest(canonical(container['HostConfig'])),
                          'networksSha256': digest(canonical(container['NetworkSettings']['Networks'])),
                          'mountsSha256': digest(canonical(container['Mounts']))}
        topology = {'kind': 'traefik-file-v1', 'infrastructureSourceSha': 'a' * 40, 'project': 'shared-traefik-edge', 'containers': pins}
        topology['approvalSha256'] = digest(canonical(topology))
        return {'edgeTopology': topology}, live

    def test_reviewed_external_static_mounts_are_adopted(self):
        profile, live = self.fixtures()
        self.assertEqual(edge_mounts(profile, live), [{'type': 'bind', 'source': '/opt/convy/public', 'target': '/srv/public', 'read_only': True}])

    def test_unreviewed_topology_and_every_runtime_change_fail_closed(self):
        profile, live = self.fixtures()
        changes = [lambda c: c.update(Id='replacement'), lambda c: c.update(Image='sha256:' + 'c' * 64),
                   lambda c: c['State'].update(Running=False), lambda c: c['Config'].update(Env=['changed']),
                   lambda c: c['HostConfig'].update(PortBindings={'5432/tcp': [{'HostIp': '', 'HostPort': '5432'}]}),
                   lambda c: c['Mounts'][0].update(RW=True), lambda c: c['NetworkSettings']['Networks'].update(private={})]
        for change in changes:
            changed = copy.deepcopy(live)
            change(changed['shared-traefik-content'])
            with self.subTest(change=change), self.assertRaises(ReleaseError): edge_mounts(profile, changed)
        for changed in ({}, {'edgeTopology': {}}, {'edgeTopology': {**profile['edgeTopology'], 'infrastructureSourceSha': 'd' * 40}}):
            with self.subTest(profile=changed), self.assertRaises(ReleaseError): edge_mounts(changed, live)

    def test_namespace_and_project_are_enforced_even_when_pins_are_recalculated(self):
        profile, live = self.fixtures()
        for wrong in ('bridge', 'container:other'):
            changed = copy.deepcopy(live)
            changed['shared-traefik-content']['HostConfig']['NetworkMode'] = wrong
            topology = copy.deepcopy(profile['edgeTopology'])
            topology['containers']['content']['hostConfigSha256'] = digest(canonical(changed['shared-traefik-content']['HostConfig']))
            topology.pop('approvalSha256'); topology['approvalSha256'] = digest(canonical(topology))
            with self.assertRaises(ReleaseError): edge_mounts({'edgeTopology': topology}, changed)

if __name__ == '__main__': unittest.main()
