# Deleted assets and current restoration

The original public clone stripped licensed libraries, Photon files, commercial art and configuration. This records that history and the modernization replacement path. **Do not reinstall the old Quantum 2 split DLL layout or paid plugin caches into the modern client.** Current acceptance and limits are in [REPORT](REPORT.md); fresh account/setup steps are in [HUMAN_ACTIONS](HUMAN_ACTIONS.md).

## Photon history and current restore

Originally removed Quantum paths included `Quantum/quantum_code/quantum.console.runner`, `quantum.console.spectator`, `BlastRoyale/Assets/Libs/Photon`, `Quantum/assemblies` and `Quantum/tools`. The original runtime was 2.1.9 with a separately compiled simulation DLL. The modernization uses **Quantum 3.0.14 build 2241** and **Bot SDK 3.10.2**, with native `Assets/QuantumUser/Simulation` as the authoritative source and genuine Editor code generation.

Restore legitimately acquired SDK source/binaries/lookup tables into ignored local paths by following [the exact modern procedure](Docs/Modernization/quantum.md) and [guarded canonical layout](Tools/Modernization/QuantumSdkLayout/README.md). Final required source-layout phases are **1, 2 and 5**; experimental phases 3/4 were rolled back. Preserve authored QuantumUser source/settings and restored native GUID metadata. The old Q2 DLL must not coexist as a second simulation implementation. Q2.1.10 and the official migration archive were private intermediate decode/export inputs, not fresh runtime dependencies.

The original Quantum server paths `Quantum/quantum_server/assemblies` and `PhotonServer` required separate enterprise access. They remain unnecessary for client/solo-bot development. `Backend/` stays untouched; no server package, key or paid hosting is needed for the chosen UGS client-authority development scope.

## Stripped integrations and free replacements

| Original stripped dependency | Modernization replacement/retirement |
| --- | --- |
| Bakery | Built-in Unity lighting and native removal of missing components/settings; no original-lighting rebake claim |
| PlayFab SDK | Minimal genuine Apache DTO/serializer closure from current public source in `Packages/PlayFabSDK`: 14 original C# files/99 types. Endpoint/HTTP/settings/factory source is retired; UGS replaces operational transport. No SDK binary restore or PlayFab title. |
| Singular, ParrelSync, Facebook | Client code/config/UI/packages and runtime paths removed |
| Firebase App/Analytics and External Dependency Manager | Client integrations, resolver/dependency declarations and remote push hooks removed; native local notifications retained |
| Animancer Pro | Game-owned real Unity Playables with clip/end callbacks |
| DOTween Pro | Game-owned UI sequences; genuine public DOTween core retained for existing core callers |
| I2 Localization | Game-owned native offline localization/catalog/TMP localizers |
| PlayerPrefs Editor | Native game-owned Editor window over real PlayerPrefs |
| SRDebugger/StompyRobot | Development-only game diagnostics/cheats/log/report panel |
| UITKTimeline | Pinned public MIT UPM source plus actual native tint/layer/GUID migration |
| Odin/Sirenix | Functional native Unity inspectors/annotations and Editor windows; SDK-supported Odin-disabled path |
| Nice Vibrations | Official MIT 4.1.2/Lofelt 1.3.4 source/native runtime |
| VoxelBusters/EssentialKit | Maintained MobileServices alerts/local notifications, native review/store URL, UGS guest/optional hosted accounts |
| Unity Purchasing | Disabled implementation/package retired; earned catalog retained and cash path explicitly unavailable |

Legitimate paid caches were private intermediate migration inputs and are retired from the active runtime. Their licenses are not transferred to another developer. `Tools/Modernization/restore_public_plugins.py` downloads official checksum-pinned DOTween core **1.3.045** and MIT NiceVibrations **4.1.2 / Lofelt 1.3.4** into ignored paths and creates the genuine assemblies. [Pins/provenance](DECISIONS.md), [public restore](Docs/Modernization/ci-plugins.md), [DTO dependency/parity evidence](Docs/Modernization/playfab-dto-compatibility.md).

The integrated DTO subset changes 17 files and retires 190; 191 deletions were prepared, but native Unity regenerated the provenance meta with its original GUID. The unchanged FirstLightServerSDK has no PlayFab assembly dependency. Remaining legacy game-facing DTO names do not constitute a second backend. Historical banned-service references remain only in explicit provenance/untouched Backend scope; no whole-repository zero-reference claim is made.

## Original commercial art inventory

The unacquired packs were stripped from `BlastRoyale/Assets/Art/AssetPacks`:

- PolygonApocalypse, PolygonBattleRoyale, PolygonConstruction, PolygonNature and PolygonSciFiCity.
- SimpleFarm, SimpleMilitary, SimplePort, SimpleProps, SimpleTemple, SimpleTown, SimpleTownLite and SimpleTrains.

Native `Assets/Art/ModernizationPlaceholders` contains clearly marked materials/mesh replacements. The initial 29 materials/one mesh preserve referenced GUID/type identities, and the stump prefab roundtrip repairs native pointers without changing durable object/component IDs. Maze's 681 missing roots from 65 source GUIDs use marked placeholders; four banners have genuine save/reopen proof.

Maze has **708** native scene-authored prototypes, FtueDeck **17**. Eight further native maps are integrated with original scene GUIDs, exact source-field/cache/managed-reference checks and separate-process restart: Battlelands **479**, Cemetery **139**, District **223**, Fortress **140**, IslandOne **2,824**, Islands **943**, Test **80**, TestAssets **10**. Native prototypes are baked from actual available scene sources. [Restoration summary](Docs/Modernization/verification/remaining-map-native-restoration-summary.json), [fresh restart](Docs/Modernization/verification/remaining-map-native-postrestart-proof.json).

Missing licensed geometry and unknown prefab-base transforms remain unit-box approximations. Stripped descendants without durable original IDs receive real Unity-generated IDs recorded in the explicit ledger; this is not a reconstruction of their original hierarchy/local IDs. Collider/navmesh/lighting rebakes were not invoked. Null pure-simulation view slots are legitimate; a non-null view is retained only when the native source supplies one. Actual game flow is proven for FtueDeck/Maze, not separately for every other map. [Detailed visual limits](Docs/Modernization/visual-placeholders.md).

## Configuration and other secrets

Create ignored `BlastRoyale/Assets/Resources/LocalClientConfig.json` from the committed blank `.json.template`, without overwriting an existing configured file. Supply real authorized Photon/Unity public client IDs and development switches. Native Unity project binding must agree with config, and locally linked ProjectSettings account fields remain uncommitted.

Resources is included in player builds: no backend secret, service-account key, server key or CI credential belongs there. Hosted Player Accounts uses its genuine ignored SDK settings asset; GitHub Actions receives restricted restore/client config through documented repository secrets. [Exact fresh-machine steps](HUMAN_ACTIONS.md).


Final orphan reconciliation retired unused PartyDialog, paid Animancer/EssentialKit settings, obsolete UnityCloudPlayerSettings and embedded AssetsImporter settings after zero current source/GUID-consumer checks. Exact recovery hashes are in `Docs/Modernization/verification/final-orphan-settings-retirement.json`. Historical raw missing-GUID rows include preserved original prefab provenance and unused design scenes; playable maps have separate genuine native validation.
