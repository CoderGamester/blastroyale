# Quantum asset recovery and native migration

All SDK code/binaries stay ignored. `Tools/Modernization/QuantumAssetMigration` contains game-owned checks and identity recovery code, with no copied SDK implementation. The actual downloaded Q2/Q3 SDKs, real simulation assembly and Unity6000.3.26f1 performed the migration in isolated projects. Main receives the reviewed mirror, never the temporary dependency/reference project.

The Q2 exporter produced exactly213 assets and matches every original cached Quantum GUID/path. Thirty-nine missing asset wrapper GUIDs were recovered by actual typed graph joins and unique real field schemas. Twenty missing component GUIDs matched unique native schemas; the remaining two empty markers use explicitly qualified authoring-role inference approved by root: all287 StormAnchor placements/consumer agree, and all387 AirDropSpawner placements/consumer agree. These two identities are not byte/schema proof. Actual native Q2 and Q3 factories built all674 markers.

The official Q3 script check, GUID transfer and restore all pass. Source filenames match concrete asset types. Eighteen native Unity config containers and nineteen explicit simulation `AssetObject` bases replace the generated legacy contract. Native codegen runs with zero legacy flags; private input wrappers, migration defines and paid attribute references are retired. A clean native restart has zero C# warnings/errors and zero exceptions.

All213 original Quantum GUIDs remain. The SDK canonical standalone storage changes exactly20 prototype paths from `Prefab|EntityPrototype` to `PrefabEntityPrototype`; all other193 original paths remain. Twenty actual native standalone prototypes have populated components. Every one of the seventeen explicitly authored custom prefab component payloads equals the real prepared Q2 data, including the player's HFSM/blackboard refs, three weapon slots, inventory specials and revive fields. The original prefab has no Stats component; runtime initializes it.

The actual native database has221 assets:213 originals plus eight actual SDK defaults (six default configs, sample map and its binary mesh). VoidMetadata is not counted. Two original dangling asset refs remain diagnosed rather than invented: destructible GameId109/AssetRef125091833024730613 and test navmesh DataAsset1757640690739227388.

Semantic schema checks preserve all active fields. Q3 moves map integer TriangleMeshCellSize4 to FP SceneMeshCellSize262144. Only empty/default retired metadata is dropped: ten empty navmesh file paths; ten obsolete empty triangle file paths and optional-format flags;5761 empty embedded polygon identifiers; three default immutable collider flags; two disabled steering debug flags. Shape geometry, binary IDs, map entities and gameplay fields are compared. Two exact optional shrinking-circle system strings must move from `quantum.code` to the actual native simulation `typeof(...).AssemblyQualifiedName`; original exports are immutable and the mapping is recorded.

## Reproduce a fresh native clone

1. Restore the legitimately acquired pinned Photon Quantum3.0.14 Stable2241 SDK and Bot SDK, then run the real SDK `InstallAllUserFiles`. Keep licensed SDK sources/binaries and SDK default user-resource templates ignored.
2. Disable the actual SDK postprocessor before importing authored assets with `QUANTUM_DISABLE_ASSET_OBJECT_POSTPROCESSOR`. The spelling matters; `QUANTUM_DISABLE_ASSETOBJECT_POSTPROCESSOR` is invalid. Main runtime uses no migration/legacy defines.
3. Run the actual native QTN generator with zero legacy flags. Game source classes explicitly derive native AssetObject; all SDK-backed code remains real.
4. Run `python3 Tools/Modernization/restore_quantum_generated_guids.py --project BlastRoyale` for the checksum/class guard. Run again with `--apply` to restore exactly22 original component script GUIDs, preserving other metadata and a recovery copy. Twenty mappings have unique schema evidence and two have the qualified role inference above.
5. Copy the game-owned `QuantumOriginalGuidInstaller.cs` into a temporary Editor folder in the real Q3 project and invoke `QuantumOriginalGuidInstaller.Apply` with `-blastQuantumGuidRegistry` pointing to the published213-entry registry. It checks every authored file checksum, Unity GUID/local ID and native type before the first write, then calls real SDK SetAssetGuidOverride. Recreate the real SDK settings rather than relying on a private local cache.
6. Register the actual Addressables settings through `AddressableAssetSettingsDefaultObject.Settings`/the persisted EditorBuildSettings configuration object. Native prototype synchronization requires it. Re-enable the SDK postprocessor and automatic native codegen; reimport and refresh the actual QuantumUnityDB.
7. Verify all213 original IDs, the explicit20 path changes, all20 populated native prototypes and actual eight SDK defaults. Run the actual offline session tests separately; importing/type checks are not runtime, device, cloud or playable-client proof.

## Repeat the original Q2 to Q3 recovery only in scratch

The public recovery helpers intentionally guard `.local/quantum3-source-project`. Retain `.local/quantum-migration/Q2AssetExport` and original pre-transfer savepoints. SDK preparation's `DeletePhoton` hardcodes `Assets/Photon`; this repo's Q2 SDK lived under `Assets/Libs/Photon`, so do not run it unchanged on the main layout. Never rerun ImportSimulationProject over already ported sources.

Set exact original search paths (`Assets/AddressableResources`, `Assets/Resources`) while the official restore runs, so new SDK defaults do not falsely require Q2 exports. Disable postprocessing before the first import. Check scripts, transfer GUIDs and restore through the actual vendor tool. Unity6.3 did not persist imported nested prefab ScriptableObject changes: the guarded game-owned helper resolves target MonoScript GUID/fileIDs from live SDK instances and persists only forty exact original prefab subasset script/identity records with proven parent/view relationships. It never renames exports or fabricates data. Native view Prefab points to the proven root GameObject, not the former Q2 component wrapper.

Upgrade the twenty lazy prototypes through the real SDK `.qprototype` importer and SetAssetGuidOverride. Re-enable only after native codegen and migration wrapper removal. Keep the full pre-finalization savepoint and verify after a clean restart.

Four original IslandOne object IDs contain literal redaction tokens. Scratch recovery replaces four tokens consistently with unused valid numeric IDs at each reference; the original YAML and mapping remain available. Root mirrors that explicit repair; the recovery does not claim the original scene imported without it.

## Evidence and mirror boundary

`.local/quantum-migration/q3-main-mirror-manifest.json` classifies exact source/asset/meta hashes, generated files, private SDK subtree, SDK-installed resource templates and the free inspector asmdef patch. Game-owned FirstLight.Inspector source maps to Main's existing inspector folder; do not duplicate it. Do not copy temporary Backend reference source, reference DLL folders, local client config or temporary editor helper assemblies.

Actual phase logs are in `.local/quantum2-exportproject` and `.local/quantum3-source-project`; exact mapping, semantic checks, original savepoints and real runtime JSON exports are in `.local/quantum-migration`. A successful import is distinguished from the native session smoke and replay test results.
