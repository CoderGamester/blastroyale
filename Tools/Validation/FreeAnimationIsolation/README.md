# Native free-animation isolation

From the repository root, restore the public free DOTween core/modules using the existing modernization restore tool, then run:

```sh
python3 Tools/Validation/FreeAnimationIsolation/create_project.py
/Applications/Unity/Hub/Editor/6000.3.26f1/Unity.app/Contents/MacOS/Unity \
  -batchmode -nographics \
  -projectPath "$PWD/.local/free-animation-project" \
  -runTests -testPlatform PlayMode \
  -testResults "$PWD/artifacts/free-animation-native-tests.xml" \
  -logFile "$PWD/artifacts/free-animation-native-tests.log"
```

Use this isolated project only when its Unity process ownership is available. The builder copies the actual game sources and unchanged public DOTween core/modules; no paid package or fake Unity/SDK types are included. It extracts the actual VfxSprite subtree while clearing only its external parent and currency sprite. Tests exercise native graphs, transforms, curves and MonoBehaviour lifecycle. AdditionalFlairAnimation's Addressables/character integration and the original full UI prefab remain full-client validation requirements.
