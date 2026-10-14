#!/usr/bin/env python3
"""Rebind old UITTimeline identities to the pinned public package without changing animation data."""
import argparse
import hashlib
import json
import re
from pathlib import Path

PIN = "68e9114b94ba"
MAPPING = {
    "aa3563c4c1576499f8f4740733a3495e": "UITScaleClip",
    "fa3678c8e5d8241d8a144107daae27f3": "UITPositionClip",
    "cc53655b7c6ad47fe82d3435fd9e98c9": "UITOpacityClip",
    "aa0453509f1a04d22ab32566f2dabfff": "UITRotationClip",
    "0711fd2f2a50a4e80a194230787b64a8": "UITDisplayClip",
    "d42d320888fca4d4aa04ce5f462925ff": "UITVisibilityClip",
    "597970daea4104996808b5e9505d5b58": "UITVisualElementTrack",
    "0b70f5de3afb146aaa3619b2751db448": "DelayedPlayOnAwake",
}


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--project", type=Path, default=Path("BlastRoyale"))
    parser.add_argument("--apply", action="store_true")
    parser.add_argument("--evidence", type=Path, default=Path("artifacts/uitimeline-guid-port.json"))
    args = parser.parse_args()
    package = args.project / "Library/PackageCache" / ("dev.krajnc.uitoolkittimeline@" + PIN)
    manifest = json.loads((package / "package.json").read_text())
    assert manifest["name"] == "dev.krajnc.uitoolkittimeline"
    mapping = {}
    for old, name in MAPPING.items():
        source = package / "Runtime" / (name + ".cs")
        assert source.exists(), source
        assert "class " + name in source.read_text(), source
        guid = re.search(r"^guid: ([a-f0-9]{32})$", source.with_suffix(".cs.meta").read_text(), re.M)[1]
        mapping[old] = guid
    rows = []
    sha = lambda b: hashlib.sha256(b).hexdigest()
    for path in sorted((args.project / "Assets").rglob("*")):
        if path.suffix not in (".playable", ".prefab", ".unity", ".asset"):
            continue
        before = path.read_bytes()
        try:
            text = before.decode("utf-8")
        except UnicodeError:
            continue
        counts = {old: text.count(old) for old in mapping if old in text}
        if not counts:
            continue
        for old in counts:
            text = text.replace(old, mapping[old])
        after = text.encode("utf-8")
        # The complete original file is recoverable by reversing only these exact GUID substitutions.
        reverse = text
        for old in counts:
            reverse = reverse.replace(mapping[old], old)
        assert reverse.encode("utf-8") == before, path
        meta = path.with_suffix(path.suffix + ".meta")
        row = {"Path": str(path), "BeforeSha256": sha(before), "AfterSha256": sha(after),
               "UnchangedMetaSha256": sha(meta.read_bytes()), "ReferenceCounts": counts,
               "AllOtherBytesUnchanged": True}
        rows.append(row)
        if args.apply:
            backup = Path(".local/main-before-uitimeline") / path
            backup.parent.mkdir(parents=True, exist_ok=True)
            if backup.exists():
                assert backup.read_bytes() == before, backup
            else:
                backup.write_bytes(before)
            path.write_bytes(after)
    result = {"PinnedPublicPackage": "68e9114b94ba4a9123a4612d6fd6ce9fc98a7868", "Applied": args.apply,
              "Mappings": [{"Type": MAPPING[old], "OldGuid": old, "NativeGuid": new} for old, new in mapping.items()],
              "Files": rows, "FileCount": len(rows), "ReferenceCount": sum(sum(r["ReferenceCounts"].values()) for r in rows),
              "NativePlaybackAcceptance": "Separate native Editor run required"}
    args.evidence.parent.mkdir(parents=True, exist_ok=True)
    args.evidence.write_text(json.dumps(result, indent=2) + "\n")
    print("UITTimeline GUID port:", len(rows), "files;", result["ReferenceCount"], "references; applied:", args.apply)


if __name__ == "__main__":
    main()
