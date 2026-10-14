# Canvas and UI Toolkit presenter lifecycle

## Boot failure and bounded fix

The first Unity Cloud boot reached `LoginFlow`, then the independent VFX initialization failed in `UIPresenter.OnScreenOpenedInternal`. `UIVFXScreen.prefab` is an authored Canvas screen: it has a Canvas, CanvasScaler, VFX image/pool source, floating-text reference and animation. Its presenter intentionally serializes `_document` as null. The inherited `RequireComponent(UIDocument)` also leaves an empty document on that prefab, with neither a visual tree nor panel settings.

The maintained-package migration automatically bound any `UIDocument` in `OnInitialized`, including this empty legacy placeholder. Opening then queried its nonexistent Toolkit `root` and dereferenced null. `ServerSelectScreen.prefab` has the same legacy Canvas/document pattern. No missing visual asset or placeholder reconstruction is needed for these screens.

The one-source fix binds an unassigned document only when it has a configured visual-tree asset. A legacy Canvas retains its intended lifecycle. A Toolkit document missing the required `root`, or a presenter with neither a configured document nor Canvas, throws a descriptive `InvalidOperationException`; the UI service removes the failed screen. This preserves error detection instead of silently opening an invisible screen. Original prefab, script identity, animation data and metadata are unchanged.

## Native verification

The isolated `Tools/Validation/UiPresenterLifecycle` fixture uses the exact original VFX prefab and dependency closure, exact game UI source, the same pinned public packages, and native Unity 6000.3.26f1. The complete game `UIService.OpenScreen`/close flow runs with the maintained package's real prefab-registry loader. Unity/UI Toolkit/pools/tweens/package types are not replaced. The game subset's three VFX extension methods are extracted verbatim from their original source.

Five PlayMode cases verify the authored Canvas opens, has an active visible Canvas transform, completes one actual pooled free tween animation, and closes; a configured Toolkit screen binds its root and runs query/open callbacks; missing Toolkit root, explicitly empty Toolkit document and missing both UI systems all fail clearly with failed-screen cleanup. Whole-game source compilation of the overlay against actual native SDK/package/Unity references also passes with zero diagnostics.

Acceptance evidence is recorded in `verification/ui-presenter-lifecycle-summary.json` and native NUnit XML. The original-source negative run uses the same authored prefab and test, demonstrating the boot's null dereference. The final positive run restores the reviewed overlay.

These tests cover local asset and UI lifecycle behavior. A complete main-project boot rerun, remote Addressables bundles, rendered appearance and device behavior remain separate checks owned by the root agent. No main-project process or writes were performed by this isolated task.
