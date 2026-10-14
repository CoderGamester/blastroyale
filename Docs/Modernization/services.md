# Maintained Services migration

## Decision and ownership

Use the actual public `com.gamelovers.services` 2.1.2 package pinned to `dd2b87a6398288e13201330be065a4e3a3429bb3`. The current remote HEAD was verified with `git ls-remote https://github.com/CoderGamester/Unity-Services.git HEAD`; the package's actual manifest targets Unity 6000.3. Root owns the final UPM manifest and assembly references. [Source](https://github.com/CoderGamester/Unity-Services/tree/dd2b87a6398288e13201330be065a4e3a3429bb3).

Deleted the embedded `Modules/Services` implementations and their old assemblies. Current TimeService, TickService, CoroutineService, Installer, CommandService and object/gameobject pools are supplied by the maintained package. Existing service tests moved to the main game's EditorMode / PlayMode test assemblies and were ported to current APIs.

Preserved only game-specific ThreadService, RemoteTextureService, MaterialVfxService and AsyncGameObjectPool under `Game/Services/Infrastructure`, with a game-owned namespace and their original script GUIDs. These implementations supply project behavior not offered by the package; no old generic Services module was retained under another folder. Root owns the separate Addressables/AssetImporter integration; backend owns MobileServices.

## Player-state adapter

`ClientDataService` composes real `GameLovers.Services.DataService` instances. Its small registry supplies the runtime `Type` lookup/enumeration expected by this game's legacy server command/state contract, while typed local persistence stays in the maintained implementation.

- Local data retains `PlayerPrefs` keys based on each type name, preserving existing local saves.
- Remote player state remains in memory and is not accidentally written when saving all client state.
- Registry entries expose the current object reference, so game logic and command context see the same state.
- `AddData(Type, object)` validates the declared reference type and routes into the real typed maintained store. It uses preserved reference-type generic code for the dynamic server contract. Device IL2CPP validation is still required.
- Replacing an entry can change its local/remote policy; saving all data uses current registry save delegates, rather than saving stale entries left in the local store.

The client contract is now `IClientDataService` / `IClientDataSaver`. It still implements `FirstLight.Server.SDK.Models.IDataProvider` so backend state capture and custom commands retain their dynamic contract. All client callers and fixtures were ported. `Backend/` was not edited.

## Message and pool contracts

Client messages and message-broker interfaces now use maintained `GameLovers.Services`. Every game publish call uses the package's actual `PublishSafe` delegate snapshot. That implementation permits listeners to subscribe/unsubscribe or cause nested state/UI transitions during delivery; its normal `Publish` would reject those mutations. The six explicit unsubscription callers were ported from delegate arguments to their proper subscriber targets. The test helper uses its captured delegate's target, preserving per-wait cleanup.

A static scan found no distinct duplicate member callbacks registered for the same target and message in a source file. Runtime callback ownership and nested transitions still need Unity tests. The game-specific MainInstaller convenience API wraps the real maintained Installer; explicit aliases resolve the new package's same-named MainInstaller class.

Pool interfaces now import `GameLovers.Services.Pooling`; minimap ObjectRefPool usages were replaced by the maintained `GameObjectPool<T>` with its real instantiator. Old pool-existence tests use `TryGetPool`. The generic command test uses the maintained `(gameLogic, messageBroker)` constructor and two-argument command execution API. Custom server-context commands remain game logic, not a duplicated generic service.

## Evidence and remaining validation

- **Pass, static:** no `FirstLight.Services`, `FirstLight.SDK.Services`, `FirstLight.Modules.Services` namespaces remain under client source, and `Modules/Services` is deleted.
- **Pass, static:** no old ObjectRefPool / generic HasPool or delegate-style message Unsubscribe calls remain in client source; no unsafe Publish calls remain.
- **Added, not run:** five new NUnit cases cover local-only persistence, dynamic reference identity, replacing local state with remote state, legacy save keys and subscription mutations during publication.
- **Not run by this agent:** Unity compilation, service/pool/coroutine test execution, player-state integration tests and IL2CPP device validation. Root owns serialized Editor runs and must record their actual results.

Package source was inspected directly at the pinned commit, including its migration guide and interface/implementation files. Claims about adapters and preserved behavior are implementation/static evidence until the relevant Unity tests run.

## SDK-restored import integration followup

The root's actual target-Editor import with restored Quantum 2 migration prerequisites reported concrete namespace collisions in `artifacts/unity-q2-restored-import.log`. The two config/presenter `Required` annotations now explicitly select the existing Odin RequiredAttribute; the import of current GameData for config interfaces is retained. Startup, StartupLoadingHelper, CoreLoopState and MatchState explicitly select the game-owned IAssetAdderService adapter rather than the maintained generic interface. StatusBarsView retains UnityEngine.Pool.ObjectPool: its create/get/release/destroy callbacks and `(false, 3)` constructor match Unity's pool, and the unused current-package Pooling import was removed. GameSocialService's serialized FriendActivity uses UnityEngine.Scripting.Preserve, removing an obsolete Best.HTTP attribute dependency while retaining the existing class/property annotations.

Scoped whitespace checks passed and Roslyn syntax parsing reported no syntax errors. These changes address the listed import diagnostics; a fresh full Unity compilation is owned by the integrator and remains separate evidence. No Unity process was started for this followup.
