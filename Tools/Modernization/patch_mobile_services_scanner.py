#!/usr/bin/env python3
"""Patch one exact MIT MobileServices scanner after the pinned Git UPM package resolves.

Run with Unity closed. The patch preserves runtime-source and precompiled player
assemblies while excluding the package's own Editor explorer from permission inference.
"""

import argparse
import hashlib
import json
from pathlib import Path
import shlex
import subprocess

PIN = "8ce2d7295afd200895179c981e5e43e60ce11d18"
DEPENDENCY = "https://github.com/CoderGamester/Unity-MobileServices.git#" + PIN
ORIGINAL_SHA256 = "b5cc297b31c934cd3511c51bb41910f32c137845ef60425f26c6528244037cb2"
RELATIVE = "Editor/Settings/MobileServicesScanner.cs"
OLD = """\t\t\tvar assemblies = System.AppDomain.CurrentDomain.GetAssemblies();
\t\t\tforeach (var assembly in assemblies)
\t\t\t{
\t\t\t\tif (!IsScannableAssembly(assembly)) continue;"""
NEW = """\t\t\t// Only actual player compilation inputs can require shipping permissions.
\t\t\t// The package's Editor device-simulator panel references every permission/ATT
\t\t\t// service for testing and must not configure the game as if it called them.
\t\t\tvar playerAssemblies = new HashSet<string>(StringComparer.Ordinal);
\t\t\tforeach (var compiled in UnityEditor.Compilation.CompilationPipeline.GetAssemblies(UnityEditor.Compilation.AssembliesType.Player))
\t\t\t{
\t\t\t\tplayerAssemblies.Add(compiled.name);
\t\t\t\tforeach (var reference in compiled.compiledAssemblyReferences)
\t\t\t\t\tplayerAssemblies.Add(System.IO.Path.GetFileNameWithoutExtension(reference));
\t\t\t}
\t\t\tvar assemblies = System.AppDomain.CurrentDomain.GetAssemblies();
\t\t\tforeach (var assembly in assemblies)
\t\t\t{
\t\t\t\tif (!IsScannableAssembly(assembly) || !playerAssemblies.Contains(assembly.GetName().Name)) continue;"""


def digest(data):
    return hashlib.sha256(data).hexdigest()


def ensure_unity_closed(project):
    process_list = subprocess.check_output(["ps", "-axo", "pid=,args="], text=True)
    for line in process_list.splitlines():
        try:
            words = shlex.split(line)
        except ValueError:
            continue
        if len(words) < 3 or not words[1].endswith("/Unity.app/Contents/MacOS/Unity"):
            continue
        if "-projectPath" in words:
            index = words.index("-projectPath")
            if index + 1 < len(words) and Path(words[index + 1]).resolve() == project:
                raise RuntimeError("Close this project's Unity process before patching; PID " + words[0])


def patch(project, restore=False):
    project = project.resolve()
    ensure_unity_closed(project)
    manifest = json.loads((project / "Packages/manifest.json").read_text())
    if manifest.get("dependencies", {}).get("com.gamelovers.mobileservices") != DEPENDENCY:
        raise RuntimeError("The exact verified Git UPM pin is required; no unknown version is patched.")
    packages = [p for p in (project / "Library/PackageCache").glob("com.gamelovers.mobileservices@*")
                if json.loads((p / "package.json").read_text()).get("_fingerprint") == PIN]
    if len(packages) != 1:
        raise RuntimeError("Resolve UPM once, close Unity, then patch its exact resolved package cache.")
    target = packages[0] / RELATIVE
    current = target.read_bytes()
    backup = project / "Temp/ModernizationPackagePatches/MobileServicesScanner" / ORIGINAL_SHA256 / "original.cs"
    if restore:
        original = backup.read_bytes()
        if digest(original) != ORIGINAL_SHA256 or original.count(OLD.encode()) != 1:
            raise RuntimeError("Exact original recovery bytes are unavailable.")
        expected = original.replace(OLD.encode(), NEW.encode())
        if current == original:
            status = "already_restored"
        elif current == expected:
            target.write_bytes(original)
            status = "restored"
        else:
            raise RuntimeError("Refusing to overwrite an unrecognized modified scanner.")
    elif NEW.encode() in current:
        # An idempotence claim still verifies the complete file, not just the replacement marker.
        original = current.replace(NEW.encode(), OLD.encode())
        if digest(original) != ORIGINAL_SHA256 or current.count(NEW.encode()) != 1:
            raise RuntimeError("Patched file differs outside the one approved scanner fragment.")
        if backup.exists() and backup.read_bytes() != original:
            raise RuntimeError("Existing recovery bytes differ.")
        backup.parent.mkdir(parents=True, exist_ok=True)
        backup.write_bytes(original)
        status = "already_applied"
    else:
        if digest(current) != ORIGINAL_SHA256 or current.count(OLD.encode()) != 1:
            raise RuntimeError("Refusing to patch an unknown scanner source hash.")
        if backup.exists() and backup.read_bytes() != current:
            raise RuntimeError("Existing recovery bytes differ.")
        backup.parent.mkdir(parents=True, exist_ok=True)
        backup.write_bytes(current)
        target.write_bytes(current.replace(OLD.encode(), NEW.encode()))
        status = "applied"
    return {"status": status, "upstream_git_pin": PIN, "source": RELATIVE,
            "original_sha256": ORIGINAL_SHA256, "current_sha256": digest(target.read_bytes()),
            "scope": "actual player source/precompiled assembly filter; no warning suppression or permission enablement"}


if __name__ == "__main__":
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--project", type=Path, required=True)
    parser.add_argument("--restore", action="store_true")
    args = parser.parse_args()
    print(json.dumps(patch(args.project, args.restore), indent=2))
