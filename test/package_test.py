"""Exercise release-blocking checks against real, deliberately corrupted VSIXs."""
import importlib.util
import json
from pathlib import Path
import tempfile
import unittest
import sys
import shutil
from zipfile import ZipFile, ZIP_DEFLATED

ROOT = Path(__file__).resolve().parent.parent
spec = importlib.util.spec_from_file_location("verify_package", ROOT / "scripts/verify_package.py")
verifier = importlib.util.module_from_spec(spec)
spec.loader.exec_module(verifier)
METADATA = json.loads((ROOT / "package.json").read_text(encoding="utf-8"))
ARTIFACT = ROOT / "dist" / f"{METADATA['displayName']}-{METADATA['version']}-win32-x64.vsix"
sys.path.insert(0, str(ROOT / "scripts"))
from verify_release import verify_release


class ReleaseChecks(unittest.TestCase):
    def mutate(self, changes):
        with tempfile.TemporaryDirectory() as temporary:
            target = Path(temporary) / "test.vsix"
            with ZipFile(ARTIFACT) as source, ZipFile(target, "w", ZIP_DEFLATED) as dest:
                for name in source.namelist():
                    data = source.read(name)
                    dest.writestr(name, changes.get(name, lambda value: value)(data))
            with self.assertRaises(ValueError):
                verifier.verify(target, METADATA)

    def test_official_package_passes(self):
        self.assertEqual(len(verifier.verify(ARTIFACT, METADATA)), 64)

    def test_wrong_publisher_is_rejected(self):
        def change(raw):
            metadata = json.loads(raw)
            metadata["publisher"] = "local-tools"
            return json.dumps(metadata).encode()
        self.mutate({"extension/package.json": change})

    def test_wrong_display_name_is_rejected(self):
        def change(raw):
            metadata = json.loads(raw)
            metadata['displayName'] = 'ContextIME'
            return json.dumps(metadata).encode()
        self.mutate({'extension/package.json': change})

    def test_wrong_author_is_rejected(self):
        def change(raw):
            metadata = json.loads(raw)
            metadata['author'] = 'wrong-author'
            return json.dumps(metadata).encode()
        self.mutate({'extension/package.json': change})

    def test_wrong_platform_is_rejected(self):
        self.mutate({"extension.vsixmanifest": lambda value: value.replace(b'win32-x64', b'darwin-arm64')})

    def test_oversized_icon_is_rejected(self):
        self.mutate({"extension/" + METADATA["icon"]: lambda value: value + b"x" * (1024 * 1024)})

    def test_changed_runtime_is_rejected(self):
        self.mutate({"extension/extension.js": lambda value: value + b"\n// changed\n"})

    def test_release_checks_accept_the_same_verified_file(self):
        import hashlib
        with tempfile.TemporaryDirectory() as temporary:
            folder = Path(temporary)
            shutil.copyfile(ARTIFACT, folder / ARTIFACT.name)
            checksum = hashlib.sha256(ARTIFACT.read_bytes()).hexdigest()
            (folder / "SHA256SUMS.txt").write_text(f"{checksum}  {ARTIFACT.name}\n", encoding="ascii")
            self.assertEqual(verify_release(ROOT, folder, "v" + METADATA["version"]), folder / ARTIFACT.name)

    def test_release_wrong_checksum_is_rejected(self):
        with tempfile.TemporaryDirectory() as temporary:
            folder = Path(temporary)
            shutil.copyfile(ARTIFACT, folder / ARTIFACT.name)
            (folder / "SHA256SUMS.txt").write_text("0" * 64 + "  " + ARTIFACT.name, encoding="ascii")
            with self.assertRaisesRegex(ValueError, "checksum mismatch"):
                verify_release(ROOT, folder, "v" + METADATA["version"])

    def test_release_wrong_tag_is_rejected(self):
        with self.assertRaisesRegex(ValueError, "version differ"):
            verify_release(ROOT, ARTIFACT.parent, "v99.0.0")


if __name__ == "__main__":
    unittest.main()
