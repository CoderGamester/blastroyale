# Editor API ports after runtime compilation

The integrator's actual third target-Editor import compiled the game runtime and reported remaining Editor diagnostics in `artifacts/unity-q2-restored-third.log`. This pass addresses the Editor source only; server message typing and Quantum migration remain with the integrator.

## Config import contracts

The maintained package's row/single-config helpers require value types, while the existing QuantumGameConfig is a class and the project's generic Quantum row importers handle both classes and structs. No game/simulation type was converted to a struct to satisfy that helper constraint.

The small project-owned GameSheetConfigsImporter composes the maintained package's actual GoogleSheetScriptableObjectImportContainer and actual CsvParser. It supplies only the unrestricted game row-list assignment. Native importer discovery, locating/creating the ScriptableObject, dirtying it and the completion hook remain in the pinned package. The existing fixed-point/QuantumGameModePair deserializers and asset-prototype import overrides remain the game's own logic. The single game-config importer similarly uses the native asset container and retains its actual Key/Value class deserialization.

Two duplicate using directives were removed. Source and existing script metadata paths are retained. Sheet URLs were stripped from the supplied clone; remote sheet downloads and actual asset writes were not tested. Existing bundled configs do not require a remote import for first play.

## Free validation and current editor APIs

Removed the unsupported Odin RegisterValidationRule/global-validator integration. FLG > Validation > Compile Player Scripts now invokes the actual free Unity PlayerBuildInterface compiler for the active target, with outputs in ignored Library/Modernization/PlayerValidation. It observes compiler errors and requires nonempty produced assemblies. This catches actual unguarded Editor-only references with the target's real preprocessor/assembly configuration, avoiding the old whole-file text scan's false positives for valid internal editor guards. The menu is implemented/compiled but was not executed in this pass; no player-compilation result is claimed.

The complete source compilation also found and repaired additional real API errors: the Odin inspector processor explicitly creates Odin's ReadOnlyAttribute, the built-in SVG importer selects VectorImage for UI Toolkit, and define toggles use current NamedBuildTarget-based Get/SetScriptingDefineSymbols APIs. Three Unity-serialized MapPreview fields have explicit null initialization, retaining their previous initial values without compiler unassigned-field warnings. No compiler warning suppression was added.

## Verification

- **Pass:** the complete 116-file FirstLight.Editor source compiled as .NET Standard 2.1/C# 9 against the 326 actual references and defines from Unity's third-import Editor response file. No SDK/game substitute types were used; runtime FirstLight.Game and real maintained-package/Unity/vendor assemblies supplied the contracts. Final result: **0 warnings, 0 errors**. [Log](verification/editor-import-compile.log).
- **Pass:** Roslyn syntax parse of all Editor source, zero syntax errors; scoped git diff whitespace checks.
- **Not run:** fresh full Unity import, actual Google Sheet download/asset mutation, player validation menu, SVG rendering/asset import interaction, or Quantum 3/player runtime acceptance. No Unity process started by this pass.

The reproduction project and copied dependency outputs stay ignored under `.local/editor-import-compile`, because they include legitimately restored local references. The public source/diagnostic log contains no vendor binaries or secrets.

Primary API evidence: pinned maintained package source inspected directly; [Unity CompilePlayerScripts](https://docs.unity3d.com/6000.3/Documentation/ScriptReference/Build.Player.PlayerBuildInterface.CompilePlayerScripts.html) and installed target-Editor XML/source contracts. The installed UnityEditor.VectorGraphicsModule documents VectorImage as the UI Toolkit vector asset output.

## Fourth-import test compilation followup

The fourth real project import compiled runtime, Editor and server assemblies, then exposed eight test assembly/type diagnostics. Three fixtures now explicitly select the game's IAssetResolverService/AssetResolverService adapters, and IntegrationTestFixture selects the game's persisted RngData model. The Editor test assembly declares the actual Unity.Services.Multiplayer, PhotonQuantum and PhotonRealtime references required by its existing lobby, UnityDB/native allocator and network fixtures. These are restored Quantum 2 migration-prerequisite references; the final Q3 assembly mapping must be reconciled during the SDK migration.

A source-isolation compilation using the actual fourth-import reference assemblies succeeds for all **53 Editor test files** and all **three PlayMode test files**, each with **0 warnings, 0 errors**. Removed one unused empty-fixture field and initialized the stub's existing party-invite list to an actual empty collection. No test assertion was removed, bypassed, weakened or marked ignored. No Unity process started. Actual test execution is separate and remains with the integrator.

[Editor test source compilation](verification/editor-tests-source-compile.log), [PlayMode test source compilation](verification/play-tests-source-compile.log).

## Quantum 3 importer handoff

Keep GameSheetConfigsImporter in the game's Editor namespace after Q3 migration. It uses the pinned package's native GoogleSheetScriptableObjectImportContainer asset lifecycle and CsvParser without asserting that game config records must be structs. Once final Q3 container/model names are known, update the concrete generic arguments and the game's QuantumDeserializer conversions to the real SDK types; retain class/reference semantics, prototype references and GUIDs. The adapter itself is not a second copy of a vendor parser or generic embedded module, and no maintained package source is modified. GameConfigsImporter separately retains its Key/Value single-class mapping over the same native asset lifecycle. Re-run the actual Editor compilation and asset import acceptance after final container changes.


## Actual test execution and runtime boundary repairs

The first original-project EditMode run (`artifacts/main-editmode-q2.xml`) discovered **272 cases: 243 passed, 29 failed, 0 skipped**. All 29 failures occurred in fixture setup: 20 integration/logic cases instantiated the maintained `CoroutineService`, whose real Unity GameObject requires PlayMode, and nine `PoolServiceTest` cases failed inside Castle/NSubstitute generic interface proxy emission (`ArgumentNullException: localType`).

Pure game-logic fixtures now substitute the actual `ICoroutineService` interface, matching the existing mocked logic fixture; the unused runtime-derived `CoroutineServiceStub` was removed. Runtime coroutine behavior remains exercised separately. Pool-service tests now register the actual maintained `ObjectPool<PoolableEntity>` with observable spawn/despawn counters and assert returned identity, callbacks, and pool contents. Duplicate/missing pool exceptions and removal assertions remain. No assertions were disabled.

The first original-project PlayMode XML reported `Passed` with **zero cases**. This is **not acceptance**. The original coroutine file was entirely commented, the asset-resolver fixture only had setup, and the smoke file was entirely commented. Restored all six original real-Unity coroutine tests, adapting generic data to `StartAsyncCoroutine(routine, data)` and `OnComplete(Action<T>)`. Deleted the setup-only asset fixture and commented smoke source; live match acceptance remains separate and requires valid project/account/content setup. The installed **Test Framework 1.6.0** does not define `RequiresPlayMode`; this experimental 2.0 attribute caused a real source-compile error and was removed. The PlayMode test assembly now has an empty `includePlatforms` list rather than being Editor-only, following [Unity's documented mode discovery](https://docs.unity.cn/Packages/com.unity.test-framework%401.1/manual/edit-mode-vs-play-mode-tests.html). Category labels remain. No replacement test attribute or runner was invented.

Warning-as-error source compilation against actual imported references passed for all remaining EditMode test sources, the restored PlayMode source, and all 116 Editor files: [fixture log](verification/editor-tests-fixture-compile.log), [coroutine log](verification/play-tests-restored-compile.log), [Editor generator log](verification/editor-sprite-generator-compile.log). These source checks preceded the actual Unity reruns recorded below; compilation itself does not establish test success.

`Sprites-Elements.uss` had an invalid CSS identifier containing a space. Both its custom-property/class/reference tokens and shared generator methods now consistently sanitize non-ASCII identifier punctuation/whitespace to `-`; asset URLs keep the original filenames. Duplicate detection compares identifiers rather than declarations including URLs. Audited the 534 current sprite image paths: zero resulting identifier collisions. The next original-project Unity import confirms USS parser acceptance.


Fresh runtime execution after restoring discovery: `artifacts/main-playmode-q2-second.xml` contains **six cases, six passed, zero failed/skipped** under actual original-project Unity PlayMode. These are the restored maintained coroutine tests, not a match smoke test. The second EditMode run contains **272 cases: 252 passed, 20 failed, zero skipped**; the real pool fixture repair is confirmed. The remaining 20 failures exposed the production Choice-state validation issue described in [native Editor/graph evidence](odin-native-editor.md), corrected and confirmed by the third original-project run: **272/272 passed**, zero failed/skipped (`artifacts/main-editmode-q2-third.xml`). Final Q3 import remains a separate acceptance step.
