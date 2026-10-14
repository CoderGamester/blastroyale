#!/usr/bin/env python3
"""Rehearse bundled-shop app contracts with genuine imported native dependencies."""
from pathlib import Path
import argparse
import hashlib
import json
import re
import shutil
import uuid

REPO = Path(__file__).resolve().parents[3]
MAIN = REPO / "BlastRoyale"
DEST = REPO / ".local/purchasing-retirement-project"
parser = argparse.ArgumentParser()
parser.add_argument("--game-assembly", type=Path, default=MAIN / "Library/ScriptAssemblies/FirstLight.Game.dll")
args = parser.parse_args()
if (DEST / "Temp/UnityLockfile").exists():
    raise RuntimeError("Close the Editor owning this isolated project before preparing it.")
for directory in ["Assets/Plugins", "Assets/Tests", "Assets/Resources", "Packages", "ProjectSettings"]:
    (DEST / directory).mkdir(parents=True, exist_ok=True)
# This is an assembly-name inventory, not a distribution of licensed implementation.
names = json.loads(Path(__file__).parents[1].joinpath("AccountStateIsolation/native-assembly-names.json").read_text())
names = [name for name in names if "Purchasing" not in name and name != "IAPResolver.dll"] + ["NSubstitute.dll"]
assemblies = {p.name: p for p in (MAIN / "Assets").rglob("*.dll")}
assemblies.update({p.name: p for p in (MAIN / "Library/ScriptAssemblies").glob("*.dll")})
metadata_source = MAIN / "Assets/Photon/Quantum/Assemblies/Quantum.Engine.dll.meta"
if not metadata_source.exists():
    raise FileNotFoundError("Restore/import the genuine licensed Quantum SDK first.")
metadata = metadata_source.read_text()
proof = []
for name in names:
    source = args.game_assembly if name == "FirstLight.Game.dll" else assemblies.get(name)
    if source is None or not source.is_file():
        raise FileNotFoundError("Restore/import the existing genuine dependency first: " + name)
    destination = DEST / "Assets/Plugins" / name
    shutil.copy2(source, destination)
    settings = re.sub(r"guid: [a-f0-9]+", "guid: " + uuid.uuid4().hex, metadata)
    if name == "DebugUI.dll":
        settings = settings.replace("isExplicitlyReferenced: 0", "isExplicitlyReferenced: 1")
    destination.with_suffix(".dll.meta").write_text(settings)
    proof.append({"Name": name, "SHA256": hashlib.sha256(source.read_bytes()).hexdigest()})
manifest = json.loads((MAIN / "Packages/manifest.json").read_text())["dependencies"]
packages = ["com.unity.test-framework", "com.unity.nuget.newtonsoft-json", "com.unity.ugui",
            "com.unity.addressables", "com.unity.render-pipelines.universal", "com.unity.inputsystem",
            "com.cysharp.unitask", "com.unity.ai.navigation", "com.unity.timeline"]
dependencies = {name: manifest[name] for name in packages}
dependencies.update({name: value for name, value in manifest.items() if name.startswith("com.unity.modules.")})
(DEST / "Packages/manifest.json").write_text(json.dumps({"dependencies": dependencies}, indent=2) + "\n")
(DEST / "ProjectSettings/ProjectVersion.txt").write_text("m_EditorVersion: 6000.3.26f1\n")
shutil.copy2(Path(__file__).with_name("BundledShopReadinessTests.cs"), DEST / "Assets/Tests/BundledShopReadinessTests.cs")
shutil.copy2(MAIN / "Assets/Resources/LocalizationCatalog.json", DEST / "Assets/Resources/LocalizationCatalog.json")
# Explicit precompiled references are necessary for this genuine DLL-based fixture.
precompiled = [name for name in names if name != "DebugUI.dll"] + ["Newtonsoft.Json.dll", "nunit.framework.dll"]
(DEST / "Assets/Tests/BundledShop.Tests.asmdef").write_text(json.dumps({
    "name": "BlastRoyale.BundledShop.Tests", "includePlatforms": ["Editor"],
    "optionalUnityReferences": ["TestAssemblies"], "references": ["UniTask"],
    "overrideReferences": True, "precompiledReferences": precompiled, "autoReferenced": False
}, indent=2) + "\n")
(DEST / "native-inputs.json").write_text(json.dumps({"NativeDependencies": proof,
    "NoPurchasingPackageOrAssemblies": True, "CredentialsCopied": False}, indent=2) + "\n")
print("Prepared", len(proof), "genuine imported dependencies and the exact app-service fixture.")
