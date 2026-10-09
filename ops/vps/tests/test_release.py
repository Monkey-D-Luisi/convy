import importlib.util
import io
import json
from pathlib import Path
import sys
import tarfile
import tempfile
import unittest
from unittest.mock import patch

ROOT = Path(__file__).resolve().parents[1]
sys.path.insert(0, str(ROOT))
from release_common import (MODEL_PATCH, LEGACY_PRICE, ReleaseError, canonical, digest,
                            extract_source, patch_model)

spec = importlib.util.spec_from_file_location('transfer', ROOT / 'transfer-release.py')
transfer = importlib.util.module_from_spec(spec)
spec.loader.exec_module(transfer)

class ConfigurationTests(unittest.TestCase):
    def test_only_reviewed_parameters_change_and_secrets_are_byte_preserved(self):
        original = ('# retained comment\nOPENAI_API_KEY=secret$$value\n'
                    'Database__MigrateOnStartup=true\n'
                    'OpenAI__TranscriptionModel=gpt-4o-mini-transcribe\n'
                    'OpenAI__Costs__TranscriptionAudioInputMicrosPerSecond=\n'
                    'OpenAI__ParsingModel=gpt-5.4-nano\n' + LEGACY_PRICE + '=\n').encode()
        updated = patch_model(original)
        before = [l for l in original.splitlines() if l.split(b'=')[0].decode() not in {*MODEL_PATCH, LEGACY_PRICE}]
        after = [l for l in updated.splitlines() if l.split(b'=')[0].decode() not in {*MODEL_PATCH, LEGACY_PRICE}]
        self.assertEqual(before, after)
        self.assertNotIn(LEGACY_PRICE.encode(), updated)
        self.assertEqual(updated, patch_model(updated))
        for key, value in MODEL_PATCH.items():
            self.assertIn((key + '=' + value).encode(), updated)

    def test_duplicate_and_crlf_fail_closed(self):
        for content in (b'OpenAI__ParsingModel=a\nOpenAI__ParsingModel=b\n', b'KEY=value\r\n'):
            with self.assertRaises(ReleaseError):
                patch_model(content)

    def test_source_traversal_and_symlinks_are_rejected(self):
        for name, kind in (('../escaped', tarfile.REGTYPE), ('link', tarfile.SYMTYPE)):
            with tempfile.TemporaryDirectory() as temporary:
                archive = Path(temporary) / 'source.tar'
                with tarfile.open(archive, 'w') as tar:
                    item = tarfile.TarInfo(name)
                    item.type = kind
                    item.linkname = '/etc/shadow'
                    tar.addfile(item, io.BytesIO(b''))
                with self.assertRaises(ReleaseError):
                    extract_source(archive, Path(temporary) / 'output')
                self.assertFalse((Path(temporary) / 'output').exists())

    def test_transfer_failure_cannot_load_or_activate_and_ssh_is_pinned(self):
        with tempfile.TemporaryDirectory() as temporary:
            known = Path(temporary) / 'known_hosts'
            known.write_text('fixture ed25519 key')
            from release_common import file_digest
            calls = []
            def execute(args, **kwargs):
                calls.append(args)
                if args[0] == 'scp':
                    raise ReleaseError('command_failed_output_withheld')
                return b''
            with patch.object(transfer, 'verify_bundle', return_value={'sourceSha': 'a' * 40}), patch.object(transfer, 'run', side_effect=execute):
                with self.assertRaises(ReleaseError):
                    transfer.transfer(temporary, 'b' * 64, 'root@fixture', 'fixture-key', known, file_digest(known), '/fixture/artifacts')
            self.assertEqual([a[0] for a in calls], ['ssh', 'scp'])
            for args in calls:
                self.assertIn('StrictHostKeyChecking=yes', args)
                self.assertIn('UpdateHostKeys=no', args)
                self.assertNotIn('load', args)
                self.assertNotIn('up', args)

    def test_workflows_have_no_automatic_production_path(self):
        source = (ROOT.parents[1] / '.github/workflows/backend-staging-release.yml').read_text()
        for forbidden in ('workflow_run:', 'secrets.', 'environment:', 'ssh ', 'scp '):
            self.assertNotIn(forbidden, source)
        self.assertIn('workflow_dispatch:', source)

if __name__ == '__main__':
    unittest.main()
