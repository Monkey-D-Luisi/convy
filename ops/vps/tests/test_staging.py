from pathlib import Path
import gzip
import io
import json
import tarfile
import tempfile
import sys
import unittest

sys.path.insert(0, str(Path(__file__).resolve().parents[1]))
from release_common import ReleaseError, canonical, digest, file_digest, verify_bundle
from staging_common import CI_WORKFLOW_ID, REPOSITORY, authorize_ci


class AutomaticIdentityTests(unittest.TestCase):
    def setUp(self):
        self.sha = 'a' * 40
        self.ci = {'id': 42, 'run_attempt': 1, 'workflow_id': CI_WORKFLOW_ID, 'path': '.github/workflows/ci.yml',
                   'head_repository': {'full_name': REPOSITORY}, 'event': 'push', 'head_branch': 'master',
                   'status': 'completed', 'conclusion': 'success', 'head_sha': self.sha}

    def test_successful_master_is_authorized_without_per_release_approval(self):
        self.assertEqual(authorize_ci(self.ci, self.sha), 'AUTHORIZED')

    def test_ci_failure_and_incomplete_ci_cannot_deploy(self):
        for key, value in (('conclusion', 'failure'), ('status', 'in_progress')):
            with self.subTest(key=key), self.assertRaises(ReleaseError):
                authorize_ci(self.ci | {key: value}, self.sha)

    def test_pr_fork_and_wrong_workflow_cannot_deploy(self):
        for change in ({'event': 'pull_request'}, {'head_branch': 'feature'}, {'head_repository': {'full_name': 'fork/convy'}}, {'workflow_id': 7}):
            with self.subTest(change=change), self.assertRaises(ReleaseError):
                authorize_ci(self.ci | change, self.sha)

    def test_duplicate_is_noop_and_old_or_superseded_run_cannot_overwrite(self):
        self.assertEqual(authorize_ci(self.ci, self.sha, {'sourceSha': self.sha, 'ciRunId': 42}), 'ALREADY_ACCEPTED')
        with self.assertRaises(ReleaseError):
            authorize_ci(self.ci, self.sha, {'sourceSha': 'b' * 40, 'ciRunId': 43})
        with self.assertRaises(ReleaseError):
            authorize_ci(self.ci, 'b' * 40)

    def test_pages_and_android_are_separate_from_inactive_staging(self):
        repo = Path(__file__).resolve().parents[3]
        workflow = (repo / '.github/workflows/staging-cd.yml').read_text()
        self.assertIn("vars.STAGING_CD_ENABLED == 'true'", workflow)
        self.assertNotIn('workflow_dispatch:', workflow)
        legacy = (repo / '.github/workflows/backend-staging-release.yml').read_text()
        self.assertNotIn('workflow_run:', legacy)
        self.assertNotIn('environment:', legacy)
        android = (repo / '.github/workflows/android-play-internal.yml').read_text()
        for safeguard in ('environment: android-release', 'mobile/androidApp/build.gradle.kts', 'WORKFLOW_RUN_CONCLUSION', 'WORKFLOW_RUN_EVENT', 'WORKFLOW_RUN_HEAD_BRANCH', 'Remove restored secret files'):
            self.assertIn(safeguard, android)
        # This branch does not replace the GitHub-managed Pages deployment workflow.
        self.assertNotIn('pages-build-deployment', workflow)

    def test_unique_compressed_layer_storage_is_bounded_and_declared_estimate_is_verified(self):
        with tempfile.TemporaryDirectory() as directory:
            root = Path(directory)
            config = canonical({'os': 'linux', 'architecture': 'amd64', 'config': {'Labels': {'org.opencontainers.image.revision': self.sha}}})
            image = 'sha256:' + digest(config)
            layer_bytes = b'a' * 100000
            compressed = gzip.compress(layer_bytes)
            metadata = canonical([{'Config': 'config.json', 'Layers': ['layer.tar.gz'], 'RepoTags': None}])
            with tarfile.open(root / 'images.tar', 'w') as archive:
                for name, data in (('config.json', config), ('manifest.json', metadata), ('layer.tar.gz', compressed)):
                    item = tarfile.TarInfo(name)
                    item.size = len(data)
                    archive.addfile(item, io.BytesIO(data))
            (root / 'source.tar').write_bytes(b'fixture source')
            manifest = {'format': 1, 'platform': 'linux/amd64', 'sourceSha': self.sha, 'baselineSha': 'b' * 40,
                        'schemaPolicy': 'unchanged', 'migrationSha256': '0' * 64, 'migrationIds': ['20261008000000_Fixture'],
                        'images': {'api': image, 'worker': image}, 'files': {name: file_digest(root / name) for name in ('source.tar', 'images.tar')}}
            (root / 'release.json').write_bytes(canonical(manifest))
            checked = verify_bundle(root, file_digest(root / 'release.json'))
            self.assertEqual(checked['imageStorageBytes'], len(compressed) + 2 * len(layer_bytes))
            manifest['imageStorageBytes'] = 1
            (root / 'release.json').write_bytes(canonical(manifest))
            with self.assertRaisesRegex(ReleaseError, 'image_storage_estimate_mismatch'):
                verify_bundle(root, file_digest(root / 'release.json'))


if __name__ == '__main__':
    unittest.main()
