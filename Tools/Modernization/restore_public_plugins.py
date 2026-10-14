#!/usr/bin/env python3
"""Download checksum-pinned, legitimate public plugin releases into ignored paths."""
import hashlib
import pathlib
import subprocess
import tempfile
import urllib.request
import zipfile

RELEASES = (
    ("https://dotween.demigiant.com/downloads/DOTween_1_3_045.zip",
     "32eba5c48520a541775d00cb396e18975b83587ba79175dbb878d753725ba5d9", "DOTween", "BlastRoyale/Assets/Plugins/Demigiant/DOTween"),
    ("https://github.com/Lofelt/NiceVibrations/releases/download/sdk-1.3.4-nicevibrations-4.1.2/nicevibrations-4.1.2.zip",
     "e2566f6ea47addff6f271fbdd0fe8200fe59c74c9d7975bf51bec264c7cbc8d0", "NiceVibrations", "BlastRoyale/Assets/Libs/NiceVibrations"),
)


def restore(root, url, checksum, source_prefix, output):
    destination = root / output
    with tempfile.TemporaryDirectory() as temporary:
        downloaded = pathlib.Path(temporary) / "release.zip"
        urllib.request.urlretrieve(url, downloaded)
        if hashlib.sha256(downloaded.read_bytes()).hexdigest() != checksum:
            raise ValueError("Plugin checksum changed; review the vendor release before updating the pin")
        count = 0
        with zipfile.ZipFile(downloaded) as archive:
            for entry in archive.infolist():
                source = pathlib.PurePosixPath(entry.filename)
                if source.is_absolute() or ".." in source.parts:
                    raise ValueError("Unsafe plugin ZIP path")
                if not source.parts or source.parts[0] != source_prefix or entry.is_dir():
                    continue
                relative = pathlib.Path(*source.parts[1:])
                # Only runtime, native libraries, editor tooling and licenses are required.
                if relative.parts and relative.parts[0].removesuffix(".meta") in {"Demo", "OlderVersions", "HapticSamples"}:
                    continue
                target = destination / relative
                if not target.resolve().is_relative_to(destination.resolve()):
                    raise ValueError("Plugin path leaves destination")
                if subprocess.run(["git", "check-ignore", "-q", "--no-index", str(target)], cwd=root).returncode:
                    raise ValueError("Add .gitignore rules before restoring plugin source")
                target.parent.mkdir(parents=True, exist_ok=True)
                target.write_bytes(archive.read(entry))
                count += 1
        print(f"Restored {count} files to {output}")


if __name__ == "__main__":
    root = pathlib.Path(__file__).resolve().parents[2]
    for release in RELEASES:
        restore(root, *release)
    from configure_dotween import configure
    configure(root)
