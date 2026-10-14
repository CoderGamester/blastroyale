# Native inspector source migration

The game and custom Quantum 3 simulation sources now use real Unity inspector fields plus a small FirstLight.Inspector assembly. The native property drawer, fallback editors and explicit editor windows are maintained by the editor migration. No Sirenix namespace, compatibility facade or expression evaluator was introduced.

The grammar-aware Roslyn conversion covered 334 attribute uses (280 game, 54 custom simulation) across 88 files with old imports, including files with unused imports. Five additional aliased ReadOnlyOdin uses were converted after alias review. Twenty Required annotations on verified value types were removed using actual Roslyn semantic types; required references, strings and collections remain meaningful. Existing SerializeField members and .meta identities were preserved. The two private CharacterTester selection fields now use native SerializeField so the real native object dropdown can expose and persist them.

Decorative tabs/foldouts/table layouts were flattened into native Header and Tooltip metadata. Numeric minimums use actual Unity Min. Functional field intent uses one composite InspectorField attribute so visibility, required checks, options, callbacks, validation, readonly and child restrictions can coexist with one native drawer. The attribute has order -1000 so localization/sprite pickers can be consumed alongside it. The editor implementation handles those concrete behaviors.

Concrete callback/condition contracts:

| Source | Native member |
| --- | --- |
| CharacterTester | GetAllWeapons/GetAllGliders return named InspectorOption values; GiveWeapons/GiveGliders execute after selection changes. |
| DurationConfig | Existing static IsValidDate(string) validates the actual field value, retaining its error text. |
| MiniMapView | IsMinimapCameraDisabled(Camera) replaces the old expression with a concrete native-camera predicate. |
| LocalizableString | _useLocalization controls the term field; text uses its inverse. |
| QuantumGameModeConfig | AllowBots controls bot fields; UsesKillCount() controls kill-count completion; GetOptionalSystems() supplies actual optional system type names. |
| FLEnvironmentAsset | Native readonly field retains its build-time warning as a tooltip. |
| NavmeshQuantumCollidersBaking | GetInspectorIssues() reports missing collider sources and cleaner walkable points through IInspectorValidator. Nested GroupConfig.Destination keeps the child-only restriction relative to the root component. |

Fourteen action buttons retain their original method calls and labels. RenderMinimap remains available only in play mode. All actions are parameterless except BakeUnityNavMesh(bool applyCleaner), whose native editor supplies a real bool toggle. Decorative button sizes/icons were omitted; method behavior was retained.

Validation: game/metadata/custom-simulation syntax parse covered 950 files with zero syntax errors. The complete actual Quantum 3 simulation source and separate FirstLight.Inspector assembly compiled with warnings-as-errors, zero warnings/errors and no Sirenix DLL references using .local/odin-source-compile/Simulation.csproj. Evidence: artifacts/odin-free-simulation-source-compile.log and verification/native-inspector-source-conversion.json. Whole-client compilation and native editor interaction tests remain root/editor-agent integration checks; syntax/API compilation does not establish their outcome.

Root added FirstLight.Inspector references to Game and Editor. The scratch Quantum.Simulation assembly now references FirstLight.Inspector and no longer lists Sirenix.Serialization or Sirenix.OdinInspector.Attributes precompiled references. Optional vendor editor integrations continue to use the real QUANTUM_ODIN_DISABLED guard. No Photon vendor source was modified for this source migration. Cleanup received the two changed scratch config files and real metadata assembly for its asset-stage refresh.
