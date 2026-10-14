#!/usr/bin/env python3
"""Stage actual native Authentication SDK/provider source into an isolated Unity test project."""
from pathlib import Path
import hashlib
import json
import shutil

REPO = Path(__file__).resolve().parents[3]
MAIN = REPO / "BlastRoyale"
DEST = REPO / ".local/ugs-auth-lifecycle-project"
TESTS = DEST / "Assets/Tests"
PLUGINS = DEST / "Assets/Plugins"
for path in [TESTS, PLUGINS, DEST / "Packages", DEST / "ProjectSettings"]:
    path.mkdir(parents=True, exist_ok=True)

for name in ["UgsAuthenticationLifecycleTests.cs", "Collaborators.cs"]:
    shutil.copy2(Path(__file__).parent / name, TESTS / name)

source_root = MAIN / "Assets/Src/FirstLight/Game"
source_entries = [
    (source_root / "Services/Authentication/UnityCloudLoginProvider.cs", "NativeUnityCloudLoginProvider.cs"),
    (source_root / "Utils/UgsAccountSession.cs", "NativeUgsAccountSession.cs"),
]
proof = []
for source, target in source_entries:
    if not source.exists():
        source = REPO / ".local/ugs-client-patches" / source.relative_to(REPO)
    candidate = REPO / '.local/runtime-account-patches' / source.relative_to(REPO)
    guard = REPO / '.local/ugs-session-guard-patches' / source.relative_to(REPO)
    if guard.exists(): candidate = guard
    if candidate.exists(): source = candidate
    shutil.copy2(source, TESTS / target)
    proof.append({"Source": str(source.relative_to(REPO)), "Target": target,
                  "SHA256": hashlib.sha256(source.read_bytes()).hexdigest()})

# Extract the two exact application interfaces; no SDK types/contracts are redefined.
auth_source = source_root / "Services/Authentication/AuthService.cs"
candidate = REPO / '.local/runtime-account-patches' / auth_source.relative_to(REPO)
if candidate.exists(): auth_source = candidate
auth = auth_source.read_text()
start = auth.index("\tpublic interface IAuthenticationHook")
end = auth.index("\n\tpublic interface ISessionData", start)
contract = auth[start:end]
(TESTS / "NativeAuthenticationContract.cs").write_text(
    "using Cysharp.Threading.Tasks;\nusing PlayFab.ClientModels;\n"
    "namespace FirstLight.Game.Services.Authentication\n{\n" + contract + "\n}\n")
proof.append({"Source": str(auth_source.relative_to(REPO)), "ExtractedContracts":
              ["IAuthenticationHook", "ILoginProvider"], "SHA256": hashlib.sha256(contract.encode()).hexdigest()})

assemblies = MAIN / "Library/ScriptAssemblies"
required = ["Unity.Services.Authentication.dll", "Unity.Services.Authentication.PlayerAccounts.dll",
            "UniTask.dll", "PlayFab.dll", "UnityEngine.UI.dll"]
required += [p.name for p in assemblies.glob("Unity.Services.Core*.dll") if "Editor" not in p.name]
for name in required:
    source = assemblies / name
    if not source.exists():
        raise FileNotFoundError("Import the main native project before staging: " + str(source))
    shutil.copy2(source, PLUGINS / name)
    proof.append({"NativeAssembly": name, "SHA256": hashlib.sha256(source.read_bytes()).hexdigest()})

(DEST / "Packages/manifest.json").write_text(json.dumps({"dependencies": {
    "com.unity.test-framework": "1.6.0", "com.unity.nuget.newtonsoft-json": "3.2.2"}}, indent=2) + "\n")
(DEST / "ProjectSettings/ProjectVersion.txt").write_text("m_EditorVersion: 6000.3.26f1\n")
(TESTS / "UgsAuthenticationLifecycle.asmdef").write_text(json.dumps({
    "name": "Unity.Services.Authentication.Tests", "references": [], "includePlatforms": ["Editor"],
    "overrideReferences": True, "autoReferenced": False, "defineConstraints": ["UNITY_INCLUDE_TESTS"],
    "optionalUnityReferences": ["TestAssemblies"], "precompiledReferences": required + ["nunit.framework.dll"]
}, indent=2) + "\n")
(DEST / "native-inputs.json").write_text(json.dumps(proof, indent=2) + "\n")
print("Staged native provider/lifecycle fixture and", len(required), "actual imported runtime assemblies.")
