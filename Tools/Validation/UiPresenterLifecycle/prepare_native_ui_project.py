#!/usr/bin/env python3
"""Stage exact game UI sources/assets and pinned native packages into an isolated Unity project."""
from pathlib import Path
import hashlib, json, re, shutil

REPO = Path(__file__).resolve().parents[3]
MAIN = REPO / 'BlastRoyale'
DEST = REPO / '.local/ui-boot-project'
OVERLAY = REPO / '.local/ui-boot-patches'
GAME = MAIN / 'Assets/Src/FirstLight/Game'
for directory in ['Assets/Runtime', 'Assets/Tests', 'Packages', 'ProjectSettings']:
    (DEST / directory).mkdir(parents=True, exist_ok=True)
proof=[]
def copy(source, target):
    target.parent.mkdir(parents=True, exist_ok=True)
    shutil.copy2(source, target)
    proof.append({'Source':str(source.relative_to(REPO)), 'Target':str(target.relative_to(DEST)), 'SHA256':hashlib.sha256(source.read_bytes()).hexdigest()})
    if source.with_suffix(source.suffix+'.meta').exists():
        shutil.copy2(source.with_suffix(source.suffix+'.meta'),target.with_suffix(target.suffix+'.meta'))

for source in (GAME/'UI/UIService/Runtime').glob('*.cs'):
    patch = OVERLAY / source.relative_to(REPO)
    copy(patch if patch.exists() else source, DEST/'Assets/Runtime/UI'/source.name)
    # Overlay source retains the original Unity script identity.
    if patch.exists():
        shutil.copy2(source.with_suffix('.cs.meta'),DEST/'Assets/Runtime/UI'/source.with_suffix('.cs.meta').name)
for relative in ['Presenters/UIVFXScreenPresenter.cs','Views/UiVfxImage.cs','Domains/HomeScreen/UI/MainMenuFloatingTextView.cs']:
    copy(GAME/relative, DEST/'Assets/Runtime'/Path(relative).name)
for source in (MAIN/'Assets/Src/FirstLight/Inspector').glob('*'):
    if source.is_file(): copy(source,DEST/'Assets/Inspector'/source.name)

# Keep the exact three game extension methods used by UIVFXScreenPresenter;
# the rest of Extensions.cs depends on unrelated gameplay systems.
extensions = GAME/'Utils/Extensions.cs'
text=extensions.read_text()
methods=[]
for name in ['IsDestroyed', 'LateCall', 'LateCallAwaitable']:
    match=re.search(r'\t\tpublic static [^\n]*\b'+name+r'\([^\n]*\)\n\t\t\{',text)
    start=match.start(); opening=text.index('{',match.start()); depth=1; end=opening+1
    while depth:
        depth += (text[end]=='{')-(text[end]=='}');end+=1
    methods.append(text[start:end])
(DEST/'Assets/Runtime/NativeVfxExtensions.cs').write_text('using System;\nusing UnityEngine;\nusing Cysharp.Threading.Tasks;\nnamespace FirstLight.Game.Utils\n{\npublic static class NativeVfxExtensions\n{\n'+'\n'.join(methods)+'\n}\n}\n')
proof.append({'Source':str(extensions.relative_to(REPO)),'ExactMethods':['IsDestroyed','LateCall','LateCallAwaitable'],'SHA256':hashlib.sha256('\n'.join(methods).encode()).hexdigest()})
copy(MAIN/'Assets/Plugins/Demigiant/DOTween/DOTween.dll',DEST/'Assets/Plugins/DOTween.dll')
for module in (MAIN/'Assets/Plugins/Demigiant/DOTween/Modules').iterdir():
    if module.is_file(): copy(module,DEST/'Assets/DOTween.Modules'/module.name)

# Copy the authored VFX prefab and its actual non-code asset dependency closure.
# Package script GUIDs resolve through the same pinned native packages as Main.
index={}
for meta in (MAIN/'Assets').rglob('*.meta'):
    match=re.search(r'^guid: ([a-f0-9]{32})$',meta.read_text(errors='ignore'),re.M)
    if match: index[match.group(1)]=Path(str(meta)[:-5])
pending=[MAIN/'Assets/AddressableResources/UI/UIVFXScreen.prefab'];visited=set();unknown=set()
while pending:
    source=pending.pop()
    if source in visited or not source.is_file(): continue
    visited.add(source)
    if source.suffix in ['.cs','.dll','.asmdef']: continue
    copy(source,DEST/source.relative_to(MAIN))
    if source.stat().st_size>8_000_000: continue
    content=source.read_text(errors='ignore')
    for guid in re.findall(r'guid: ([a-f0-9]{32})',content):
        asset=index.get(guid)
        if asset is not None: pending.append(asset)
        else: unknown.add(guid)

manifest=json.loads((MAIN/'Packages/manifest.json').read_text())['dependencies']
names=['com.cysharp.unitask','com.quickeye.ui-toolkit-plus','com.gamelovers.gamedata',
       'com.gamelovers.services','com.gamelovers.uiservice','com.unity.addressables',
       'com.unity.render-pipelines.universal','com.unity.ugui','com.unity.test-framework',
       'com.unity.test-framework.performance','com.unity.nuget.newtonsoft-json']
deps={name:manifest[name] for name in names}
deps.update({name:value for name,value in manifest.items() if name.startswith('com.unity.modules.')})
(DEST/'Packages/manifest.json').write_text(json.dumps({'dependencies':deps},indent=2)+'\n')
(DEST/'ProjectSettings/ProjectVersion.txt').write_text('m_EditorVersion: 6000.3.26f1\n')
(DEST/'Assets/Runtime/NativeUiRuntime.asmdef').write_text(json.dumps({'name':'FirstLight.NativeUi.Runtime','references':['UniTask','Unity.Addressables','Unity.ResourceManager','GameLovers.UiService','GameLovers.Services','QuickEye.UIToolkit','UnityEngine.UI','Unity.TextMeshPro','FirstLight.Inspector']},indent=2)+'\n')
for name in ['UiPresenterLifecycleTests.cs','TestToolkitPresenter.cs','TestResultPresenter.cs','ResultScreen.uxml','ValidScreen.uxml','MissingRootScreen.uxml']:
    copy(Path(__file__).parent/name,DEST/'Assets/Tests'/name)
(DEST/'Assets/Tests/NativeUiTests.asmdef').write_text(json.dumps({'name':'FirstLight.NativeUi.Tests','references':['FirstLight.NativeUi.Runtime','UniTask','GameLovers.UiService','UnityEngine.UI','Unity.TextMeshPro','UnityEngine.TestRunner','UnityEditor.TestRunner'], 'overrideReferences':True, 'precompiledReferences':['nunit.framework.dll','DOTween.dll'], 'defineConstraints':['UNITY_INCLUDE_TESTS']},indent=2)+'\n')
(DEST/'native-inputs.json').write_text(json.dumps({'Inputs':proof,'ExternalAssetGuids':sorted(unknown),'Packages':deps},indent=2)+'\n')
print('Staged',len(proof),'exact inputs; external package/builtin asset GUIDs',len(unknown))
