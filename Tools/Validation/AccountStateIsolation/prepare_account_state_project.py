#!/usr/bin/env python3
"""Stage imported native game/package/SDK assemblies for local account-state regression."""
from pathlib import Path
import argparse, hashlib, json, re, shutil

REPO = Path(__file__).resolve().parents[3]
MAIN = REPO/'BlastRoyale'
DEST = REPO/'.local/runtime-account-project'
parser=argparse.ArgumentParser()
parser.add_argument('--game-assembly',type=Path,default=MAIN/'Library/ScriptAssemblies/FirstLight.Game.dll')
args=parser.parse_args()
for directory in ['Assets/Plugins','Assets/Tests','Packages','ProjectSettings']:
    (DEST/directory).mkdir(parents=True,exist_ok=True)
names=json.loads(Path(__file__).with_name('native-assembly-names.json').read_text())
assemblies={p.name:p for p in (MAIN/'Assets').rglob('*.dll')}
assemblies.update({p.name:p for p in (MAIN/'Library/ScriptAssemblies').glob('*.dll')})
# Keep exactly the required staged assembly inventory; native package source supplies the rest.
for target in (DEST/'Assets/Plugins').glob('*.dll'):
    if target.name not in names: target.unlink()
proof=[]
for name in names:
    source=args.game_assembly if name=='FirstLight.Game.dll' else assemblies.get(name)
    if source is None or not source.exists():raise FileNotFoundError('Restore/import the genuine dependency first: '+name)
    target=DEST/'Assets/Plugins'/name
    shutil.copy2(source,target)
    if source.with_suffix('.dll.meta').exists():shutil.copy2(source.with_suffix('.dll.meta'),target.with_suffix('.dll.meta'))
    proof.append({'Source':str(source.resolve().relative_to(REPO)),'Name':name,'SHA256':hashlib.sha256(source.read_bytes()).hexdigest()})
# The DebugUI package is explicitly referenced in Main. Preserve that assembly isolation
# when its genuine imported DLL is staged, so it cannot shadow URP's own DebugUI type.
target=DEST/'Assets/Plugins/DebugUI.dll.meta'
guid=re.search(r'guid: ([a-f0-9]+)',target.read_text()).group(1) if target.exists() else '9ca20f761d014040bd50a71329835c5b'
template=(MAIN/'Assets/Photon/Quantum/Assemblies/Quantum.Engine.dll.meta').read_text()
target.write_text(re.sub(r'guid: [a-f0-9]+','guid: '+guid,template).replace('isExplicitlyReferenced: 0','isExplicitlyReferenced: 1'))
manifest=json.loads((MAIN/'Packages/manifest.json').read_text())['dependencies']
packages=['com.unity.test-framework','com.unity.nuget.newtonsoft-json','com.unity.ugui',
          'com.unity.addressables','com.unity.render-pipelines.universal','com.unity.inputsystem',
          'com.cysharp.unitask','com.unity.ai.navigation','com.unity.timeline']
deps={name:manifest[name] for name in packages}
deps.update({name:value for name,value in manifest.items() if name.startswith('com.unity.modules.')})
(DEST/'Packages/manifest.json').write_text(json.dumps({'dependencies':deps},indent=2)+'\n')
(DEST/'ProjectSettings/ProjectVersion.txt').write_text('m_EditorVersion: 6000.3.26f1\n')
shutil.copy2(Path(__file__).with_name('AccountStateIsolationTests.cs'),DEST/'Assets/Tests/AccountStateIsolationTests.cs')
shutil.copy2(Path(__file__).with_name('ExpiredAccountSwitchTests.cs'),DEST/'Assets/Tests/ExpiredAccountSwitchTests.cs')
native_lifecycle=Path(__file__).parent.parent/'UgsAuthenticationLifecycle'
shutil.copy2(native_lifecycle/'Collaborators.cs',DEST/'Assets/Tests/Collaborators.cs')
# The real Auth3.8 SDK already grants this assembly access to its native state/scheduler contracts.
fixture=(native_lifecycle/'UgsAuthenticationLifecycleTests.cs').read_text()
usings=fixture[:fixture.index('namespace FirstLight.Tests')]
(DEST/'Assets/Tests/NativeFixture.cs').write_text(usings+fixture[fixture.index('sealed class NativeFixture'):])
(DEST/'Assets/Tests/AccountStateTests.asmdef').write_text(json.dumps({'name':'Unity.Services.Authentication.Tests',
'references':['UnityEngine.TestRunner','UnityEditor.TestRunner','UniTask'],'includePlatforms':['Editor'],
'overrideReferences':False,'defineConstraints':['UNITY_INCLUDE_TESTS']},indent=2)+'\n')
(DEST/'native-inputs.json').write_text(json.dumps(proof,indent=2)+'\n')
print('Staged',len(proof),'genuine native assembly inputs and local regression fixture.')
