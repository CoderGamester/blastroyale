#!/usr/bin/env python3
"""Restore a legitimately acquired vendor package only into Git-ignored paths.

No Unity process is needed. GUIDs and importer metadata are preserved. Shared
parent folders are skipped; assets outside ignored vendor paths are rejected.
"""
import argparse
import pathlib
import re
import subprocess
import tarfile


def import_package(package, project, prefixes):
    project = project.resolve()
    imported = 0
    with tarfile.open(package, "r:gz") as archive:
        members = {entry.name.removeprefix("./"): entry for entry in archive}
        for name, entry in members.items():
            if not name.endswith("/pathname") or not entry.isfile():
                continue
            # Asset Store exports can append padding on additional lines.
            path = archive.extractfile(entry).read().decode("utf-8-sig").splitlines()[0].rstrip("\0")
            if not any(path == prefix or path.startswith(prefix + "/") for prefix in prefixes):
                continue
            relative = pathlib.PurePosixPath(path)
            if relative.is_absolute() or ".." in relative.parts or relative.parts[0] not in ("Assets", "Packages"):
                raise ValueError("Unsafe asset pathname in package")
            destination = project.joinpath(*relative.parts)
            if not destination.resolve().is_relative_to(project):
                raise ValueError("Asset path leaves the Unity project")
            group = name.rsplit("/", 1)[0]
            asset = members.get(group + "/asset")
            meta = members.get(group + "/asset.meta")
            targets = [destination] + ([pathlib.Path(str(destination) + ".meta")] if meta else [])
            for target in targets:
                # Check before writing so licensed source can never appear as untracked code.
                result = subprocess.run(["git", "check-ignore", "-q", "--no-index", str(target)], cwd=project)
                if result.returncode != 0:
                    raise ValueError(f"Add a vendor .gitignore rule before restoring {target.relative_to(project)}")
            if asset and asset.isfile():
                destination.parent.mkdir(parents=True, exist_ok=True)
                destination.write_bytes(archive.extractfile(asset).read())
            else:
                destination.mkdir(parents=True, exist_ok=True)
            if meta:
                metadata = archive.extractfile(meta).read().decode("utf-8-sig")
                # Unity's removed Facebook desktop importer platforms can still
                # exist in old vendor caches. Strip those disabled records so a
                # legitimate restore does not reintroduce removed SDK settings.
                metadata = re.sub(r"(?m)^  - first:\r?\n      Facebook: [^\r\n]*\r?\n(?: {4,}[^\r\n]*\r?\n)*", "", metadata)
                pathlib.Path(str(destination) + ".meta").write_text(metadata)
            imported += 1
    if not imported:
        raise ValueError("No package assets matched the requested vendor prefixes")
    return imported


if __name__ == "__main__":
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("package", type=pathlib.Path)
    parser.add_argument("--project", type=pathlib.Path, default=pathlib.Path("BlastRoyale"))
    parser.add_argument("--prefix", action="append", required=True, help="Original package pathname prefix")
    args = parser.parse_args()
    print(f"Restored {import_package(args.package, args.project, args.prefix)} ignored vendor assets")
