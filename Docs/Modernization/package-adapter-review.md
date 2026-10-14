# Package adapter review

Reviewed the live game callers against the actual pinned package sources in `.local/packages`, without adding compatibility implementations to stand in for package APIs.

## Bounded production corrections

- `PlayerLogic`, `BattlePassLogic`, and `ContentCreatorLogic` now declare the six getter/setter-backed fields as native `ObservableResolverField<T>`. The native `ObservableField<T>.Rebind(T)` overload cannot rebind getter/setter delegates.
- The UI adapter explicitly names `GameLovers.UiService.UiService`, avoiding the legacy sibling namespace collision. `UIPresenter.OnInitialized` obtains its required `UIDocument` if the serialized reference is absent.
- Screen closure now invokes the real package close lifecycle and awaits `CloseTransitionTask` before unloading. Opening awaits `OpenTransitionTask` as well: the package's `OpenUiAsync` starts transitions and returns the presenter without waiting for them.
- Duplicate loads now check the game screen dictionary after the native asynchronous load. The package returns the same presenter if two loads finish together, so the second request returns that presenter instead of adding the same key again.
- `GetScreen<T>` preserves the original `InvalidOperationException` when the screen is absent.
- The five game dictionaries in `CurrencyLogic`, `EquipmentLogic` (loadout and inventory), `ResourceLogic`, and `UniqueIdLogic` explicitly select `ObservableUpdateFlag.Both`. The removed implementation notified keyed and global listeners; the native package defaults to `KeyUpdateOnly`. Without this change global inventory/currency observers would silently stop receiving updates.
- Guid and Vfx enum-selector drawers use the native `GameLoversEditor.GameData` namespace; an unused `FirstLightEditor` import was removed from `ConfigUtils`. The root migration separately corrected asset-importer/editor namespace prefixes.

## Preserved test migration

The original DataExtensions, Statechart, and GoogleSheetImporter test families remain under `Tests/EditorMode/PackageMigration`.

Removed legacy lazy list/dictionary wrappers do not share the new resolver projection contract. Those fixtures now exercise the real ordinary collections with native `Rebind`, and three new tests prove that rebinding preserves the reader instance and subscriptions. The dictionary rebind case verifies keyed and global subscriptions independently.

The original dictionary notification assertions still require four Added/Updated/Removed notifications across two collections and two subscriber forms. Both flags are explicit. The operation case uses real `Dictionary` backing storage: the former substituted `IDictionary.Remove` returned false by default, and the real native collection correctly suppresses a Removed notification when removal fails. Expectations were not weakened to accommodate missing notifications.

Five validation assertions were updated from `MissingMemberException`/`MissingMethodException` to the native `InvalidOperationException`, preserving each invalid state graph and its rejection check.

## Task-wait semantic change

The old TaskWait state exposed event transitions and allowed a parent event to exit a running task immediately, ignoring its later completion. Native `ITaskWaitState` has no event transition API. Native nested/split states queue the first parent event while an inner task runs and execute it after that task completes.

The task tests explicitly describe and verify this changed contract: an event does not interrupt a direct task wait; a parent event causes no exit or transition before completion, and causes exactly one parent exit/transition afterwards. A teardown releases a pending fixture task after failed assertions, preventing work from leaking into the next test.

The current game graph was checked before accepting this change:

- Its ten task waits are in AuthenticationState, CoreLoopState, NetworkState, MatchState, and BaseTutorialState. No game code configures a TaskWait event.
- Their enclosing Nest/Split chains have no Event transitions. MatchState's task waits are enclosed by CoreLoopState's `match.Nest`, which has no parent event transitions.
- MatchState's `gameSimulation` parent handles MatchQuit, MatchEnded, and critical disconnection, but GameSimulationState and BattleRoyaleState contain no task waits.
- Authentication's parent nest and the root Core split likewise have no event transitions; the tutorial task wait is in the Core split chain.

The queued task-wait behavior therefore is not exercised by the current parent-event leave/reconnect/quit paths. No production caller adaptation was required by this source graph audit. A future parent event surrounding an asynchronous wait must allow that wait to complete or explicitly implement caller cancellation. This audit does not prove an online match or reconnect on a device.

## Verification

The UI adapter's complete Runtime source compiled against actual imported GameLovers.UiService, UniTask, Addressables, QuickEye, UI and Unity 6000.3.26f1 libraries with **0 warnings and 0 errors**. No substitute game or UI types were used. Evidence: `verification/ui-adapter-build.log`.

An isolated Unity project at `.local/package-migration-project` used Unity **6000.3.26f1**, the real pinned packages, NUnit/Unity Test Framework **1.6.0**, and the existing NSubstitute test dependency. It contains the three migrated pure test families and no game/Quantum substitute types.

| Package | Actual checked-out commit |
| --- | --- |
| GameData | `3c6147829ba6154d92a0dd257c69bf167b20c3b7` |
| Statechart | `21aabc10111ffe4adbe5d4a7e4843a648994ce39` |
| GoogleSheetImporter | `c04b1e89c740d3e18db523a0fdfacd8299d028e1` |
| UniTask | `a77a1b4a89824fb6df293099de3060daf2a432c1` |

The final EditMode run exited **0**, executed **124 tests**, and reported **124 passed, 0 failed, 0 skipped**. It includes all fourteen fixtures: EnumSelector, ObservableDictionary/Field/List, Choice/Leave/Nest/Split/State/TaskWait/Statechart/Transition/Wait, and CsvParser. The earlier failed run exposed the dictionary flag regression and package contract differences above; those were reviewed and corrected before the final run.

Durable evidence: `verification/package-migration-tests.xml`, `verification/package-migration-unity.log`, and `verification/package-migration-test-source-build.log` (standalone source compile also 0 warnings/errors).

These are package adapter and pure package tests. They do not establish full game compilation, Addressables availability for game screens, physical-device behavior, or a playable Quantum match.

## Concurrent layer admission followup

The original migrated UI adapter could start loading two different screen types after both observed an empty layer. The correction serializes admission/loading/native opening per layer. Close and open game callbacks are awaited outside that gate, because a callback may itself await another screen on the same layer. Holding a semaphore through that callback would deadlock.

Each open uses the maintained UiService's actual instance-address API, with a unique instance identity separate from its configured Addressables prefab path. An old close callback can therefore reopen its own type without the eventual old-instance close/unload affecting the new instance. The game dictionary tracks only the current screen on a layer; completion/error cleanup checks exact entry identity before removing it. A replaced in-flight open reports cancellation instead of announcing that it opened. The presenter's initial frame wait uses its close token, and closing a not-yet-initialized presenter tolerates absent token/root state. Closing-transition objects may remain visible temporarily during their callbacks/animations; one current admitted screen remains after operations settle.

Fresh compilation of the complete actual adapter Runtime against real imported GameLovers.UiService, UniTask, Addressables, QuickEye and Unity 6000.3.26f1 references succeeded: **0 warnings, 0 errors**. The updated log is `verification/ui-adapter-build.log`. This is source/type acceptance only. Actual concurrent opens of different types, same-type reopen from a close callback, and nested awaited opens from open/close callbacks still require Unity runtime tests with screen assets. No runtime concurrency result is claimed.
