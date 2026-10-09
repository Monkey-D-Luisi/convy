import io
import os
from pathlib import Path
import sys
import tarfile
import tempfile
import unittest
from unittest.mock import patch

sys.path.insert(0, str(Path(__file__).resolve().parents[1]))
from release_common import ReleaseError, digest
from release_content import android_version, archive_content, catalog, patch_metadata, publish_tree, restore_static, save_static


class ReleaseContentTests(unittest.TestCase):
    def test_recovery_snapshot_copies_hardlinks_and_restores_complete_trees(self):
        with tempfile.TemporaryDirectory() as temporary:
            root = Path(temporary)
            journal = root / 'journal'; journal.mkdir()
            profile = {'current': str(root / 'current')}
            before = {}
            for name, directory in (('legal', 'legal'), ('public-site', 'public')):
                target = root / directory; target.mkdir()
                (target / 'index.html').write_bytes(b'previous public bytes')
                if name == 'legal': os.link(target / 'index.html', target / 'copy.html')
                info = target.stat()
                before[name] = {'files': catalog(target), 'device': info.st_dev, 'inode': info.st_ino}
            save_static(journal, profile)
            with tarfile.open(journal / 'previous-static.tar') as archive:
                self.assertTrue(all(member.isfile() or member.isdir() for member in archive))
            (root / 'legal/index.html').write_bytes(b'changed')
            (root / 'public/new.html').write_bytes(b'added')
            restore_static(journal, profile, {'staticBefore': before})
            self.assertEqual(catalog(root / 'legal'), before['legal']['files'])
            self.assertEqual(catalog(root / 'public'), before['public-site']['files'])
    def test_static_byte_and_file_budgets_fail_closed(self):
        with tempfile.TemporaryDirectory() as temporary:
            root = Path(temporary)
            (root / 'one').write_bytes(b'12')
            with patch('release_content.MAX_STATIC_BYTES', 1), self.assertRaises(ReleaseError): catalog(root)
            (root / 'two').write_bytes(b'3')
            with patch('release_content.MAX_STATIC_FILES', 1), self.assertRaises(ReleaseError): catalog(root)
    def test_add_update_delete_and_file_directory_transitions_preserve_bind_root(self):
        with tempfile.TemporaryDirectory() as temporary:
            root = Path(temporary)
            source, target = root / 'source', root / 'target'
            source.mkdir(); target.mkdir()
            (target / 'old').write_bytes(b'delete')
            (target / 'shape').write_bytes(b'was file')
            (source / 'shape').mkdir()
            (source / 'shape/index.html').write_bytes(b'now directory')
            inode = target.stat().st_ino
            publish_tree(source, target)
            self.assertEqual(catalog(source), catalog(target))
            (source / 'shape/index.html').unlink(); (source / 'shape').rmdir()
            (source / 'shape').write_bytes(b'now file')
            publish_tree(source, target)
            self.assertEqual((target / 'shape').read_bytes(), b'now file')
            self.assertEqual(target.stat().st_ino, inode)

    def test_static_symlinks_rejected_without_touching_destination(self):
        with tempfile.TemporaryDirectory() as temporary:
            root = Path(temporary)
            source, target = root / 'source', root / 'target'
            source.mkdir(); target.mkdir()
            (target / 'index').write_bytes(b'old')
            try:
                (source / 'link').symlink_to(target / 'index')
            except OSError:
                self.skipTest('OS does not permit symlink creation')
            with self.assertRaises(ReleaseError): publish_tree(source, target)
            self.assertEqual((target / 'index').read_bytes(), b'old')

    def test_metadata_updates_only_four_keys_and_preserves_secret_bytes(self):
        original = b'# retained\r\nTOKEN=secret$$literal\r\nBackend__Version=old\nMobile__AndroidVersion=old\n'
        metadata = {'sourceSha': 'a' * 40, 'acceptedAtUtc': '2026-10-09T12:00:00+00:00',
                    'backendVersion': 'b' * 12, 'androidVersion': '1.2.3+42'}
        updated = patch_metadata(original, metadata)
        self.assertTrue(updated.startswith(b'# retained\r\nTOKEN=secret$$literal\r\n'))
        self.assertIn(b'Backend__Version=bbbbbbbbbbbb\n', updated)
        self.assertIn(b'Mobile__AndroidVersion=1.2.3+42\n', updated)
        self.assertEqual(updated, patch_metadata(updated, metadata))
        with self.assertRaises(ReleaseError): patch_metadata(b'Backend__Version=a\nBackend__Version=b\n', metadata)
        with self.assertRaises(ReleaseError): patch_metadata(b'TOKEN=secret-without-newline', metadata)
        with self.assertRaises(ReleaseError): patch_metadata(b'\xef\xbb\xbfBackend__Version=old\n', metadata)

    def test_android_version_is_source_declared_and_unambiguous(self):
        self.assertEqual(android_version(b'versionName = "1.2.3"\nversionCode = 42\n'), '1.2.3+42')
        for data in (b'versionName = "1"\n', b'versionName = "1"\nversionName = "2"\nversionCode = 1\n'):
            with self.assertRaises(ReleaseError): android_version(data)

    def test_archive_catalog_and_unsafe_static_members(self):
        with tempfile.TemporaryDirectory() as temporary:
            path = Path(temporary) / 'source.tar'
            def write(extra):
                with tarfile.open(path, 'w') as archive:
                    for name, data in [('legal/index.html', b'legal'), ('public-site/index.html', b'public'),
                                       ('mobile/androidApp/build.gradle.kts', b'versionName = "1"\nversionCode = 2\n'), *extra]:
                        item = tarfile.TarInfo(name); item.size = len(data)
                        archive.addfile(item, io.BytesIO(data))
            write([])
            files, version = archive_content(path)
            self.assertEqual(files['legal'], {'index.html': digest(b'legal')})
            self.assertEqual(version, '1+2')
            for extra in ([('legal/../escape', b'bad')], [('legal/index.html', b'duplicate')], [('legal//index.html', b'alias')]):
                write(extra)
                with self.assertRaises(ReleaseError): archive_content(path)
