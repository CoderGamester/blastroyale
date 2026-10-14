# Free native Unity Editor replacement

Status: implemented and source-compiled against actual target Editor/game/package references with warnings as errors. Original-project Quantum 3 import passed with zero C# diagnostics; native inspector/window interaction remains pending. No paid inspector namespace or compatibility facade was created.

## Retained authoring behavior

| Former tool | Native replacement |
| --- | --- |
| Configs menu | Searchable asset list across config folders; actual Unity Editor/SerializedObject inspection; Project selection; save dirty asset; actual maintained sheet importer lookup, CSV conversion, import and save. Failed HTTP requests do not import their response body; requests are disposed. |
| Collections menu | Model/prefab browser; imported ID copy; real icon/texture/skin references; Unity interactive model preview; Project inspection; Addressable group entries and character test-scene action. Missing assets/groups display their actual absence. |
| Map generator | Explicit native inputs for ID/player count/flags/drop size and game-mode allow list; original config generation/save and scene export actions remain accessible. Export checks real prerequisites, refuses existing destinations, saves the scene before resolving its GUID, moves the generated root into that scene, and only writes a nonempty generated enum update. |
| Map preview | Native folder picker, preview-setup and render-target fields; original runtime snapshot preparation/export actions gated by PlayMode and real prerequisites; missing Camera reports an error. |
| Stat debugger | Native read-only live stat fields; original local-player/stat event subscriptions and teardown remain. |
| Localization terms | Actual Unity searchable AdvancedDropdown over the bundled catalog, plus editable existing value. |
| Sprite classes | Actual Unity searchable AdvancedDropdown over generated classes, retaining image-path labels and actual class values. |
| Simulation Match/Meta Item config processors | Native type PropertyDrawers preserve game-mode/map/item lists, automatic bot/player choices, team-size range, enum choice, and read-only unique ID; ordinary nested serialized fields remain editable. |

The two game picker attributes now derive from real Unity `PropertyAttribute`. All five native windows use Unity `EditorWindow`; no decorative no-op attributes stand in for UI. The remaining unused Sirenix imports were removed, the character importer uses `OfType<AnimationClip>()`, and artifact copying no longer requests an Odin DLL.

## Audited game annotation consumer

The shared **FirstLight.Inspector** assembly supplies game-owned `InspectorFieldAttribute`, `InspectorActionAttribute`, `InspectorOption` and `IInspectorValidator`. The native Editor consumer implements concrete behavior for the inventory: required references/strings, read-only values, options from actual named members, field-change callbacks, typed validation, conditional visibility, and child-hierarchy restrictions. It resolves actual nested declaring instances (including list elements), supports static string/Camera validation methods, and reports missing/misconfigured members visibly. It does not interpret arbitrary expression strings.

Native fallback MonoBehaviour/ScriptableObject inspectors retain annotated action buttons and real validator messages. The one original bool-argument action has an editable toggle; runtime-only actions are gated by actual PlayMode and actions are disabled for multi-object selection. Scalar Required markers that could never express a missing reference are removed in the typed inventory conversion. Child restrictions compare against each real root component's hierarchy and reject external references while retaining the prior field value. Localization/sprite pickers compose with visibility through the composite field drawer.

Unity's [target-branch property handler](https://github.com/Unity-Technologies/UnityCsReference/blob/6000.3/Editor/Mono/Inspector/Core/ScriptAttributeGUI/PropertyHandler.cs) maintains nesting for multiple drawers; delegating ordinary fields to real `EditorGUI.PropertyField` preserves Unity's existing type drawers and serialized editing.

## Evidence and limits

- Full Editor source isolation build with actual Unity 6000.3.26 references passed **0 warnings, 0 errors**, with paid Sirenix, Animancer and DOTween Pro compiler references removed: [build log](verification/editor-odin-native-compile.log). Source isolation includes the actual shared annotation source; original-project import confirms assembly identities separately.
- All 13 original Editor files carrying Odin references are replaced/cleaned, plus one new native field/action consumer. `rg` confirms no Sirenix/Odin API names remain in Editor source.
- Changed-file whitespace checks pass. Existing source `.meta` identities remain; the new native consumer has its own script GUID.
- No Unity process or asset-mutating import/export action was run by this owner. Native window interaction, constraints/callback UX and Q3-specific map export acceptance need actual target-Editor verification. Compilation alone does not prove those actions or a match.

## Production graph correction discovered by real tests

The second original EditMode run passed **252/272** cases and exposed the same native graph-construction error in 20 integration/logic setups. The Audio game-mode check had a single unconditional Battle Royale path; the simulation-initialization-error state likewise had one unconditional error callback/final path. Both now use the maintained package's real Transition state. Audited all 27 production Choice declarations: the other 25 have conditional paths and one unconditional fallback. Validation was retained; neither fixtures nor package checks were weakened. The next actual original-project EditMode run (`artifacts/main-editmode-q2-third.xml`) passed **272/272**, zero failed/skipped, confirming both production graph corrections. This is the restored Q2 baseline, separate from final Q3 import acceptance.
