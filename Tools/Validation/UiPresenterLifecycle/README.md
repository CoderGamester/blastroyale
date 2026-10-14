# Native UI presenter lifecycle regression

This isolated Unity 6000.3.26f1 PlayMode fixture stages the original `UIVFXScreen.prefab`, its actual non-code asset dependencies, the game UI adapter/presenters, and the same pinned public packages as the main project. Only `UIPresenter.cs` comes from the reviewed overlay. Script metadata and all copied assets retain their original GUIDs.

The production adapter normally loads Addressables. Tests substitute the maintained GameLovers package's **real `PrefabRegistryUiAssetLoader`** through the existing loader field, then exercise the complete game `UIService.OpenScreen`/close path and native package lifecycle. There are no replacement Unity, UI Toolkit, pool, tween, or package types. The three VFX extension methods used by this subset are extracted verbatim from the game source; the unrelated gameplay helpers are not compiled in this probe.

Run from the repository root:

```sh
python3 Tools/Validation/UiPresenterLifecycle/prepare_native_ui_project.py
/Applications/Unity/Hub/Editor/6000.3.26f1/Unity.app/Contents/MacOS/Unity \
  -batchmode -projectPath "$PWD/.local/ui-boot-project" \
  -runTests -testPlatform PlayMode \
  -testFilter FirstLight.Tests.PlayMode.UI.UiPresenterLifecycleTests \
  -testResults "$PWD/artifacts/ui-presenter-lifecycle-tests.xml" \
  -logFile "$PWD/artifacts/ui-presenter-lifecycle-tests.log"
```

The seven cases cover the authored Canvas VFX screen opening, actual visible Canvas scale, pooled native animation completion and close; a configured Toolkit document's root/query/open callbacks; and explicit failures with cleanup for a missing Toolkit root, an explicitly assigned empty Toolkit document, or no configured Toolkit/Canvas UI. The fixture uses only synthetic Toolkit test screens and local assets, without cloud requests. Two result-presenter cases use real native Clickable dispatch during a controlled asynchronous open hook, verifying an early result survives opening and reuse of the same MonoBehaviour resets the waiter before another choice.

This is a local lifecycle/asset regression. It does not claim a complete main-project boot, rendered appearance comparison, device validation, or remote Addressables bundle validation. The root agent owns the main-project integration and boot rerun. The overlay changes no prefab or asset payload.
