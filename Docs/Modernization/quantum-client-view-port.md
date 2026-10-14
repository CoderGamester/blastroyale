# Quantum 3 client views and prototypes

Target: actual downloaded Quantum **3.0.14 Stable build 2241** and actual generated Quantum.Simulation/Unity sources in the isolated migration project. No SDK/Frame/view stand-in was created. Original-project Q3 import has completed with zero C# diagnostics; visual/match acceptance remains pending.

Grammar-aware type-position edits port MonoComponent and HUD consumers, loaded-view messages, the entity updater, and the FTUE map tutorial to real `QuantumEntityView` and `QuantumEntityViewBindBehaviour`, preserving field/property names such as `EntityView` and all existing `.meta` identities. The updater derives from native `QuantumEntityViewUpdater` and explicitly accepts the simulation asset `Quantum.EntityView`, avoiding the SDK migration shim's obsolete global name. Native `AssetGuid` pool keys and `asset.Guid` replace wrapper lookups. The collectable prototype and `QPrototypeCollectablePlatformSpawner` use the real generated types; map consumers use `QuantumMapData.GetAsset(forEditor)` and culling uses `IsPredictionCulled`.

## Deferred disposal contract

Quantum 3 adds native view-component activation/deactivation to its base destruction path. The game now calls that actual base lifecycle once at deferred disposal rather than manually raising only the Unity event. It temporarily clears/restores `ManualDisposal` for that call, captures the originating QuantumGame when queuing, and preserves the active/pending view lookup during same-frame handlers. Bullet destruction routes through the existing pool; other dynamic/map views use native destruction/disable. No-frame shutdown disposes immediately. Callback-created queue entries remain for the next LateUpdate and duplicate requests are ignored. Destroyed pooled scene objects are skipped on reuse; destroyed hierarchy groups are recreated.

This is a source/API port and lifecycle review. Real native runtime tests for pending view callbacks, pooling/reactivation and shutdown have not yet run. Runtime coroutine acceptance is separate and passed six cases in the original Q2 baseline project.

## Fresh evidence

The root's complete native Q3 client compiler passed with **zero warnings/errors** and emitted `FirstLight.Game.dll`; see `artifacts/quantum3-firstlight.game-compile.log`. The subsequent original-project Unity 6000.3.26f1 import also completed, exit zero with zero C# warnings/errors. Content acceptance still records missing scene/prefab sources. Q3 original-project execution subsequently passed **294 EditMode and six PlayMode cases**, zero failures/skips; see `artifacts/main-editmode-q3-fifth.xml` and `artifacts/main-playmode-q3-first.xml`. No match or visual acceptance is inferred from these suites.

Owned paths: `Game/MonoComponent/**`, `Game/Messages/MatchStateMessages.cs`, `Game/Views/MatchHudViews/**`, `Game/Views/OverlayWorldView.cs`, `Game/Services/Match/EntityViewUpdaterService.cs`, and `Game/StateMachines/Tutorial/FTUEMapState.cs`.
