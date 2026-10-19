#!/usr/bin/env python3
"""Repair the pinned UiService Editor fixture layout and Unity 6 camera query."""
import argparse
import hashlib
import json
from pathlib import Path

PIN = "aa5fa65457b53601de647bcd6c925e435485698e"
SOURCE = Path("Tests/EditMode/Loaders/Resources")
DESTINATION = Path("Editor/Resources")
HASHES = {
    "UiServiceTestPrefab.prefab": "13673596614cdcac5b61f076db2ad02201216a287f506f3ef4ce7765be0af3e2",
    "UiServiceTestPrefab.prefab.meta": "fbb3a462d0c62fcdc5a82e1d370636f49aac2521ca89e0515380dc3e6068145c",
}
FOLDER_HASH = "220e04f619b626ce69872ec5c52f843c21a68576c9b6536a80b0e5cfca43ad77"
CAMERA_TEST = Path("Tests/PlayMode/Integration/UiCameraStackFeatureTests.cs")
CAMERA_BEFORE = "e758c2d78d08e1e2f8f322662df2e67845ecfd73725bf17ec286ed60c3aaba06"
CAMERA_AFTER = "76d9560e278139f12c83cc3f3c64178e87ab7511008cc913e416c2fc63866c0d"
OLD_QUERY = b"Object.FindObjectsByType<Camera>()"
NEW_QUERY = b"Object.FindObjectsByType<Camera>(FindObjectsSortMode.None)"
TEST_HASH = "b373b3881ea9be28682f444860d71e35f3f08b3da927a616cd4219c436de21be"


def sha(path):
    return hashlib.sha256(path.read_bytes()).hexdigest()


def patch(project, rollback=False):
    manifest = json.loads((project / "Packages/manifest.json").read_text())
    lock = json.loads((project / "Packages/packages-lock.json").read_text())
    package_id = "com.gamelovers.uiservice"
    expected_url = f"https://github.com/CoderGamester/Unity-UiService.git#{PIN}"
    if manifest["dependencies"].get(package_id) != expected_url or lock["dependencies"][package_id].get("hash") != PIN:
        raise ValueError("UiService pin changed; review the package before applying this repair.")
    candidates = list((project / "Library/PackageCache").glob(f"{package_id}@{PIN[:12]}*"))
    if len(candidates) != 1:
        raise ValueError("Resolve the pinned UPM package before applying this repair.")
    package = candidates[0]
    test = package / "Tests/EditMode/Loaders/ResourcesUiAssetLoaderTests.cs"
    if sha(test) != TEST_HASH or not (package / "Editor.meta").is_file():
        raise ValueError("The reviewed test or existing Editor folder changed.")
    camera = package / CAMERA_TEST
    if sha(camera) not in {CAMERA_BEFORE, CAMERA_AFTER}:
        raise ValueError("The reviewed camera fixture changed; no files moved.")
    source, destination = (package / SOURCE, package / DESTINATION)
    if source.exists() == destination.exists():
        raise ValueError("Ambiguous Resources layout; expected exactly one original or repaired folder.")
    current = source if source.exists() else destination
    if {p.name for p in current.iterdir()} != set(HASHES):
        raise ValueError("Unexpected fixture payload; no files moved.")
    if any(sha(current / name) != digest for name, digest in HASHES.items()) or sha(current.with_suffix(".meta")) != FOLDER_HASH:
        raise ValueError("Fixture or metadata differs from the reviewed package; no files moved.")
    wanted = source if rollback else destination
    wanted_meta = wanted.with_suffix(".meta")
    if current != wanted and (wanted_meta.exists() or wanted_meta.is_symlink()):
        raise ValueError("Orphan destination metadata exists; no files changed.")
    camera_bytes = camera.read_bytes()
    repaired_camera = camera_bytes.replace(NEW_QUERY, OLD_QUERY) if rollback else camera_bytes.replace(OLD_QUERY, NEW_QUERY)
    expected_camera_hash = CAMERA_BEFORE if rollback else CAMERA_AFTER
    if hashlib.sha256(repaired_camera).hexdigest() != expected_camera_hash:
        raise ValueError("Camera query repair differs from reviewed output; no files moved.")
    if camera_bytes != repaired_camera:
        camera.write_bytes(repaired_camera)
    if current == wanted:
        return "Verified original layout." if rollback else "Verified Editor-only fixture layout and Unity 6 camera query."
    current.rename(wanted)
    try:
        current.with_suffix(".meta").rename(wanted.with_suffix(".meta"))
    except OSError:
        wanted.rename(current)
        raise
    return "Restored original fixture layout." if rollback else "Verified Unity 6 camera query and moved the unchanged fixture beneath the existing Editor folder."


if __name__ == "__main__":
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--project", type=Path, default=Path("BlastRoyale"))
    parser.add_argument("--rollback", action="store_true")
    arguments = parser.parse_args()
    print(patch(arguments.project.resolve(), arguments.rollback))
