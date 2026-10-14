#!/usr/bin/env python3
"""Inventory unresolved GUIDs without changing Unity assets or inventing replacements.

Package GUIDs are only resolved when their source is present locally. An unresolved
GUID is evidence to inspect, not proof that a reference is permanently lost.
"""
import argparse
import collections
import csv
import json
import re
from pathlib import Path

GUID = re.compile(r"(?:guid:\s*|GUID:)([0-9a-fA-F]{32})")
SCRIPT = re.compile(r"m_Script:\s*\{[^\n}]*guid:\s*([0-9a-fA-F]{32})")
BUILTIN = {"0" * 32, "0" * 16 + "f" + "0" * 15, "0" * 16 + "e" + "0" * 15}
TEXT_EXTENSIONS = {".unity", ".prefab", ".asset", ".mat", ".controller",
                   ".overridecontroller", ".playable", ".asmdef", ".asmref"}


def inventory(project, extra_roots):
    roots = [project / "Assets", project / "Packages", project / "Library/PackageCache"]
    roots.extend(extra_roots)
    definitions = collections.defaultdict(list)
    for root in roots:
        if not root.exists():
            continue
        for meta in root.rglob("*.meta"):
            asset = meta.with_suffix("")
            if not asset.exists():
                continue
            try:
                match = re.search(r"^guid:\s*([0-9a-fA-F]{32})", meta.read_text(), re.M)
            except (UnicodeError, OSError):
                continue
            if match:
                definitions[match[1].lower()].append(str(asset))
    unresolved = []
    assets_scanned = 0
    for path in sorted((project / "Assets").rglob("*")):
        if path.suffix.lower() not in TEXT_EXTENSIONS or not path.is_file():
            continue
        try:
            source = path.read_text()
        except (UnicodeError, OSError):
            continue
        assets_scanned += 1
        for lineno, line in enumerate(source.splitlines(), 1):
            for match in GUID.finditer(line):
                guid = match[1].lower()
                if guid in definitions or guid in BUILTIN:
                    continue
                unresolved.append({"asset": str(path), "line": lineno, "guid": guid,
                                   "kind": "script" if SCRIPT.search(line) else "asset"})
    return {
        "assets_scanned": assets_scanned,
        "known_guids": len(definitions),
        "unresolved_references": len(unresolved),
        "unresolved_unique_guids": len({row["guid"] for row in unresolved}),
        "unresolved_script_references": sum(row["kind"] == "script" for row in unresolved),
        "duplicate_guid_definitions": {guid: paths for guid, paths in definitions.items() if len(paths) > 1},
        "most_referenced_unresolved": collections.Counter(row["guid"] for row in unresolved).most_common(30),
        "scope": "project assets plus locally restored package definitions; external missing packages remain unresolved",
    }, unresolved


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--project", type=Path, default=Path("BlastRoyale"))
    parser.add_argument("--extra-root", type=Path, action="append", default=[])
    parser.add_argument("--output", type=Path, default=Path("artifacts/asset-audit"))
    args = parser.parse_args()
    if not (args.project / "Assets").is_dir():
        parser.error("project must contain an Assets directory")
    summary, unresolved = inventory(args.project, args.extra_root)
    args.output.mkdir(parents=True, exist_ok=True)
    (args.output / "summary.json").write_text(json.dumps(summary, indent=2) + "\n")
    with (args.output / "unresolved.csv").open("w", newline="") as output:
        writer = csv.DictWriter(output, fieldnames=["asset", "line", "guid", "kind"])
        writer.writeheader()
        writer.writerows(unresolved)
    print(json.dumps({key: value for key, value in summary.items()
                      if key not in {"duplicate_guid_definitions", "most_referenced_unresolved"}}, indent=2))


if __name__ == "__main__":
    main()
