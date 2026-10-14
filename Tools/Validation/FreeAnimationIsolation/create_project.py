#!/usr/bin/env python3
"""Create an ignored Unity project from the real free-animation game sources."""
import json
import re
import shutil
from pathlib import Path

repo = Path(__file__).resolve().parents[3]
project = repo / '.local/free-animation-project'
assets = project / 'Assets'
runtime = assets / 'Runtime'
tests = assets / 'Tests/Editor'
runtime.mkdir(parents=True, exist_ok=True)
tests.mkdir(parents=True, exist_ok=True)
(project / 'Packages').mkdir(exist_ok=True)
(project / 'ProjectSettings').mkdir(exist_ok=True)
(project / 'Packages/manifest.json').write_text(json.dumps({'dependencies': {
    'com.unity.ugui': '2.0.0',
    'com.unity.test-framework': '1.6.0',
    'com.unity.modules.animation': '1.0.0',
    'com.unity.modules.ui': '1.0.0',
    'com.unity.modules.imgui': '1.0.0',
    'com.unity.modules.physics': '1.0.0',
    'com.unity.modules.physics2d': '1.0.0',
    'com.unity.modules.audio': '1.0.0',
}}, indent=2) + '\n')
shutil.copy2(repo / 'BlastRoyale/ProjectSettings/ProjectVersion.txt', project / 'ProjectSettings/ProjectVersion.txt')
for source in ['Views/UiVfxImage.cs', 'Utils/OneShotAnimationPlayer.cs']:
    original = repo / 'BlastRoyale/Assets/Src/FirstLight/Game' / source
    shutil.copy2(original, runtime / original.name)
    shutil.copy2(Path(str(original) + '.meta'), runtime / (original.name + '.meta'))
core = repo / 'BlastRoyale/Assets/Plugins/Demigiant/DOTween'
for name in ['DOTween.dll', 'DOTween.dll.meta', 'readme.txt']:
    original = core / name
    if original.is_file():
        shutil.copy2(original, runtime / name)
assert (runtime / 'DOTween.dll').is_file(), 'Restore the public free DOTween core first.'
shutil.copytree(core / 'Modules', assets / 'DOTween.Modules', dirs_exist_ok=True)
(runtime / 'FirstLight.FreeAnimation.asmdef').write_text(json.dumps({
    'name': 'FirstLight.FreeAnimation', 'references': ['UnityEngine.UI', 'DOTween.Modules'],
}, indent=2))
(tests / 'FirstLight.FreeAnimation.Tests.asmdef').write_text(json.dumps({
    'name': 'FirstLight.FreeAnimation.Tests',
    'references': ['FirstLight.FreeAnimation', 'UnityEngine.UI'],
    'optionalUnityReferences': ['TestAssemblies'],
    'overrideReferences': True, 'precompiledReferences': ['nunit.framework.dll', 'DOTween.dll'],
}, indent=2))
shutil.copy2(Path(__file__).with_name('FreeAnimationTests.cs'), tests / 'FreeAnimationTests.cs')
# Import only the actual VfxSprite object. Its unrelated presenter/text siblings
# require the full game; all four retained native/game component blocks are exact.
source = (repo / 'BlastRoyale/Assets/AddressableResources/UI/UIVFXScreen.prefab').read_text()
ids = ['1857049748784957120', '2715078542509167946', '7328978038156308650',
       '8642474547260884565', '2823971217050829139']
blocks = []
for identity in ids:
    block = re.search(rf'--- !u!\d+ &{identity}\n.*?(?=--- !u!|\Z)', source, re.S).group(0)
    if identity == '2715078542509167946':
        block = re.sub(r'  m_Father: \{fileID: \d+\}', '  m_Father: {fileID: 0}', block)
    if identity == '8642474547260884565':
        # The animation test does not need the source currency texture.
        block = re.sub(r'  m_Sprite: \{[^\n]+\}', '  m_Sprite: {fileID: 0}', block)
    blocks.append(block)
(assets / 'VfxSprite.prefab').write_text('%YAML 1.1\n%TAG !u! tag:unity3d.com,2011:\n' + ''.join(blocks))
print(project)
