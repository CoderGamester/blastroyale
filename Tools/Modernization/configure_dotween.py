#!/usr/bin/env python3
"""Generate the assembly definition for the free DOTween modules."""
import json
import pathlib
import subprocess
import uuid


def configure(root):
    vendor = root / "BlastRoyale/Assets/Plugins/Demigiant"
    if not (vendor / "DOTween/DOTween.dll").exists():
        raise ValueError("Restore DOTween before generating its assembly definitions")
    assemblies = [
        ("DOTween/Modules", "DOTween.Modules", ["UnityEngine.UI"], []),
    ]
    for directory, name, references, platforms in assemblies:
        path = vendor / directory / (name + ".asmdef")
        if subprocess.run(["git", "check-ignore", "-q", "--no-index", str(path)], cwd=root).returncode:
            raise ValueError("Vendor assembly definitions must remain ignored")
        data = {"name": name, "references": references, "includePlatforms": platforms, "autoReferenced": True}
        path.write_text(json.dumps(data, indent=2) + "\n")
        guid = uuid.uuid5(uuid.NAMESPACE_URL, "blastroyale-local-vendor/" + name).hex
        pathlib.Path(str(path) + ".meta").write_text("fileFormatVersion: 2\nguid: " + guid + "\nAssemblyDefinitionImporter:\n  externalObjects: {}\n  userData: \n  assetBundleName: \n  assetBundleVariant: \n")
    print(f"Generated {len(assemblies)} ignored DOTween assembly definitions")


if __name__ == "__main__":
    configure(pathlib.Path(__file__).resolve().parents[2])
