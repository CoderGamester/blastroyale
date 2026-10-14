"""Safety and honest-result checks for CI's dependency restore and NUnit guard."""
import importlib.util
import pathlib
import subprocess
import tempfile
import unittest
import zipfile


def load(name):
    path = pathlib.Path(__file__).resolve().parents[1] / (name + ".py")
    spec = importlib.util.spec_from_file_location(name, path)
    module = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(module)
    return module


restore = load("restore_dependencies").restore
verify = load("verify_test_results").verify


class DependencyRestoreTests(unittest.TestCase):
    def setUp(self):
        self.temporary = tempfile.TemporaryDirectory()
        self.root = pathlib.Path(self.temporary.name)
        subprocess.run(["git", "init", "-q", str(self.root)], check=True)
        (self.root / ".gitignore").write_text("BlastRoyale/Assets/Vendor/\nQuantum/assemblies/\n")
        (self.root / "tracked.txt").write_text("keep")
        subprocess.run(["git", "add", "."], cwd=self.root, check=True)

    def tearDown(self):
        self.temporary.cleanup()

    def archive(self, entries):
        path = self.root / "bundle.zip"
        with zipfile.ZipFile(path, "w") as archive:
            for name, content in entries:
                archive.writestr(name, content)
        return path

    def test_restores_ignored_prerequisite(self):
        path = "BlastRoyale/Assets/Vendor/sdk.dll"
        restore(self.archive([(path, b"test-package-content")]), self.root)
        self.assertEqual((self.root / path).read_bytes(), b"test-package-content")

    def test_rejects_non_ignored_file_before_any_write(self):
        entries = [("BlastRoyale/Assets/Vendor/sdk.dll", b"vendor"), ("BlastRoyale/Assets/game.cs", b"overwrite")]
        with self.assertRaises(ValueError):
            restore(self.archive(entries), self.root)
        self.assertFalse((self.root / entries[0][0]).exists())

    def test_rejects_traversal_and_backend_content(self):
        for path in ["../escape", "BlastRoyale/../escape", "Backend/reference.cs", "/absolute", "BlastRoyale\\Assets\\escape"]:
            with self.subTest(path=path), self.assertRaises(ValueError):
                restore(self.archive([(path, b"invalid")]), self.root)

    def test_rejects_symlink(self):
        entry = zipfile.ZipInfo("BlastRoyale/Assets/Vendor/link")
        entry.create_system = 3
        entry.external_attr = 0o120777 << 16
        with self.assertRaises(ValueError):
            restore(self.archive([(entry, b"../../game.cs")]), self.root)


class TestResultGuardTests(unittest.TestCase):
    def setUp(self):
        self.temporary = tempfile.TemporaryDirectory()
        self.root = pathlib.Path(self.temporary.name)

    def tearDown(self):
        self.temporary.cleanup()

    def write(self, xml):
        (self.root / "results.xml").write_text(xml)

    def test_accepts_real_pass(self):
        self.write('<test-run><test-case result="Passed" /></test-run>')
        verify(self.root)

    def test_missing_results_and_zero_tests_fail(self):
        with self.assertRaises(ValueError): verify(self.root)
        self.write('<test-run testcasecount="0" />')
        with self.assertRaises(ValueError): verify(self.root)

    def test_failure_or_all_skipped_fail(self):
        for cases in ['<test-case result="Skipped" />', '<test-case result="Passed" /><test-case result="Failed" />']:
            self.write('<test-run>' + cases + '</test-run>')
            with self.assertRaises(ValueError): verify(self.root)

    def test_supports_legacy_nunit2(self):
        self.write('<test-results><test-case executed="True" success="True" result="Success" /></test-results>')
        verify(self.root)


if __name__ == "__main__":
    unittest.main()
