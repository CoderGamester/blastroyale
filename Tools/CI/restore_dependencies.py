#!/usr/bin/env python3
"""Restore a private, checksum-pinned prerequisite ZIP without logging secrets."""
import hashlib
import json
import os
import pathlib
import re
import stat
import subprocess
import sys
import tempfile
import urllib.parse
import urllib.request
import zipfile


def restore(archive_path, root):
    root = root.resolve()
    # Every restored file must already be ignored. Never overwrite tracked
    # gameplay, workflow, or Backend reference source from a dependency bundle.
    with zipfile.ZipFile(archive_path) as archive:
        validated = []
        for entry in archive.infolist():
            path = pathlib.PurePosixPath(entry.filename)
            if path.is_absolute() or ".." in path.parts or "\\" in entry.filename:
                raise ValueError("Unsafe dependency archive path")
            if not path.parts or path.parts[0] not in {"BlastRoyale", "Quantum"}:
                raise ValueError("Dependency ZIP must use repository-relative Unity/Quantum paths")
            if stat.S_ISLNK(entry.external_attr >> 16):
                raise ValueError("Dependency ZIP must not contain symlinks")
            if entry.is_dir():
                continue
            target = root.joinpath(*path.parts)
            if not target.resolve().is_relative_to(root):
                raise ValueError("Dependency path leaves repository")
            ignored = subprocess.run(["git", "check-ignore", "-q", "--no-index", str(target)], cwd=root)
            tracked = subprocess.run(["git", "ls-files", "--error-unmatch", str(target)], cwd=root, stdout=subprocess.DEVNULL, stderr=subprocess.DEVNULL)
            if ignored.returncode or not tracked.returncode:
                raise ValueError("Bundle contains a tracked or non-ignored file; include only licensed restore prerequisites")
            validated.append((entry, target))
        if not validated:
            raise ValueError("Dependency ZIP is empty")
        for entry, target in validated:
            target.parent.mkdir(parents=True, exist_ok=True)
            target.write_bytes(archive.read(entry))
    print(f"Restored {len(validated)} ignored prerequisite files")


def main():
    root = pathlib.Path(__file__).resolve().parents[2]
    url = os.environ.get("CI_DEPENDENCIES_URL", "")
    checksum = os.environ.get("CI_DEPENDENCIES_SHA256", "")
    if not url or not checksum:
        raise ValueError("Configure CI_DEPENDENCIES_URL and CI_DEPENDENCIES_SHA256 from a legitimate private prerequisite bundle. Fork PRs cannot access these secrets; run a trusted branch manually after review.")
    if urllib.parse.urlparse(url).scheme != "https" or not re.fullmatch(r"[0-9a-fA-F]{64}", checksum):
        raise ValueError("Prerequisites need an HTTPS URL and a SHA-256 checksum")
    with tempfile.TemporaryDirectory() as temporary:
        downloaded = pathlib.Path(temporary) / "dependencies.zip"
        try:
            urllib.request.urlretrieve(url, downloaded)
        except Exception:
            raise ValueError("Private prerequisite download failed; refresh the secret URL or its access grant") from None
        if hashlib.sha256(downloaded.read_bytes()).hexdigest().lower() != checksum.lower():
            raise ValueError("Private prerequisite checksum mismatch")
        restore(downloaded, root)
    config = os.environ.get("CLIENT_CONFIG_JSON", "")
    if not config:
        raise ValueError("Set CLIENT_CONFIG_JSON using the committed local client configuration template")
    try:
        if not isinstance(json.loads(config), dict):
            raise ValueError()
    except Exception:
        raise ValueError("CLIENT_CONFIG_JSON must contain a JSON object") from None
    target = root / "BlastRoyale/Assets/Resources/LocalClientConfig.json"
    # Recheck the actual target before writing secrets.
    if subprocess.run(["git", "check-ignore", "-q", str(target)], cwd=root).returncode:
        raise ValueError("Local client config must be ignored before restoration")
    target.parent.mkdir(parents=True, exist_ok=True)
    target.write_text(config, encoding="utf-8")
    # Q2 imports must not make a Quantum 3 migration job look healthy.
    simulation = root / "BlastRoyale/Assets/QuantumUser/Simulation"
    sdk = root / "BlastRoyale/Assets/Photon/Quantum"
    if not simulation.is_dir() or not sdk.is_dir():
        raise ValueError("Quantum 3 migration is incomplete: restore the SDK and port the simulation to Assets/QuantumUser/Simulation before running this CI")


if __name__ == "__main__":
    try:
        main()
    except (ValueError, zipfile.BadZipFile) as error:
        print(f"::error::{error}", file=sys.stderr)
        sys.exit(1)
