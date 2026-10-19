# Blast Royale modernization report

Checkpoint: **2026-10-19 UTC**. **The reported Editor Play failure is repaired, and the final source is playable in this checkout.** Code commit `77c7da136`, with Services 2.2.0, passes **318 EditMode and 14 PlayMode cases**, zero failures/skips. Real Game view controls moved the local player **7.305673 units** and emitted **three native Quantum attacks** in the first match; death → spectating → standings → progression → Home → another match passed in the same Play session. The second match also accepted movement and emitted three attacks, with **zero captured runtime errors across the run**. The only runtime warning is a negative-scale BoxCollider on accepted placeholder geometry, repeated once per Maze load. The earlier cold-start repair checkpoint has zero console errors/warnings.

`Backend/` is untouched. Proprietary Photon files, downloaded runtime binaries, real client/account IDs and build outputs stay ignored. No upstream push, history rewrite, paid plugin purchase or server deployment was made. Evidence below separates native Editor, isolated fixtures, cloud probes, mobile export and device acceptance.

## Nine-section status

| Section | Status | Evidence and remaining work |
| --- | --- | --- |
| 1. Unity 6.3 | **Pass locally** | Exact Editor; final source passes 318 EditMode/14 PlayMode, clean cold startup and Android APK and unsigned iOS export. Final-source real input/attacks/results/Home/replay pass; device/Xcode/signing remain separate acceptance. |
| 2. Photon Quantum | **Done** | Real stable 3.0.14 source/codegen, 213 original assets, ten native maps, Q2→Q3 binary conversions, two genuine session/replay fixtures and actual tutorial/normal match. Device and two-player acceptance remain outside these proofs. |
| 3. Web3 cleanup | **Done** | Client integrations, transfer flows, UI/config and dependencies removed; ordinary earned currency integer IDs retained. Untouched Backend and historical documentation are explicit exceptions. |
| 4. GameLovers packages | **Done** | Six current public Git pins, old embedded generic modules removed, actual caller/adapter ports, 124 native package cases and Main gameplay pass. Physical mobile behavior is not verified. |
| 5. Plugins | **Done** | Free native replacements and minimal genuine DTO source remove paid/runtime transport dependencies; source, animation, localization, native Timeline and GPU proofs pass. All required replacements are integrated. Physical haptics/store behavior and exhaustive authoring interaction remain separate unrun acceptance. |
| 6. Assets and secrets | **Partial** | Maze, FtueDeck and eight additional native map restorations integrated with exact GUID/field/cache/new-ID ledgers. Missing commercial geometry uses marked approximations; the broad raw GUID audit is reconciled with native ten-map/source checks; unused design scenes and inactive animation log behaviours remain unverified authoring content. Real IDs stay only in ignored configuration/settings. |
| 7. CI/CD | **Done** | Azure/Fastlane/infra pipeline removal and pinned Actions for tests/Quantum/Android/unsigned iOS. Actionlint, eight helper cases and seven Addressable-ID mutation cases pass locally. Remote Actions/license activation not run. |
| 8. Free backend | **Done** | One UGS identity plus Photon, live guest/twelve-model/Public-profile/restart/write-lock proof and actual tutorial/solo game pass. Native lifecycle/account/queue/party guards pass; normal native match input/results/Home/replay pass. Live hosted recovery, two-client Friends/Lobby and device acceptance are not run. |
| 9. Proposals | **Done** | Two architecture and two game-feature proposals below, with problem, benefit, effort and files. None implemented. |

## Editor recovery acceptance

[Final native GUI/input proof](Docs/Modernization/verification/interactive-final-native-input-20261019.json), [current SDK payload audit](Docs/Modernization/verification/sdk-installed-audit-20261019.json), [independent review and referee](Docs/Modernization/verification/editor-repair-review-20261019.md). The observer only listens to the real runner/input/events; it creates no session, supplies no inputs and advances no frames. Controls came through the actual Game view. Holding Space without an aim vector was not counted as an attack; aim plus the shooting joystick emitted native attack events.

The final retest also caught a normal empty spectator-target notification during leaving. The presenter now validates the target before accessing the runner/frame, ignores the legitimate cleared sentinel, and preserves warnings for inconsistent valid entities. Native GUI leaving/results/Home/replay passes with no spectator warning.

Normal match shutdown previously left Photon while the SDK communicator also attempted leave. Its fallback disconnected a client already Leaving; repeated ReconnectAndRejoin then returned false without a callback. RoomService now owns leaving and failed rejoin falls back to Master. Actual result/Home/replay and four native communicator cases verify that correction. Match-start waits are canceled with their state and bound to their captured game. Input callbacks detach before Disable so held specials cannot execute into a stopped runner. The old avatar CDN is replaced with bundled character/default images.

EditMode fixtures now own their Quantum dispatchers and preserve/restore global bindings. This fixes failures specifically observed when tests run after actual Play; all original session/replay assertions remain. No broad claim of live reconnection, malformed-config recovery, every special interruption, physical-device behavior or two-client social testing follows from these results. The older 2026-10-14 cloud/content records below remain historical scoped evidence.

## 1. Unity 6.3

Editor is exactly **6000.3.26f1**, revision `de8167e7440c`. Recommended pins include URP **17.3.0**, Addressables **2.10.3**, Input System **1.20.0**, Test Framework **1.6.0**, mobile notifications **2.5.0** and Multiplayer Services **2.3.3**. Genuine UXML source generation and current runtime/Editor APIs replace deprecated factories, transforms and service calls. The real Render Graph path replaces obsolete URP compatibility mode. [Package record](Docs/Modernization/unity-package-upgrade.json).

| Executed check | Result and scope | Durable evidence |
| --- | --- | --- |
| Current Main EditMode | **318 passed; 0 failed/skipped**, including native Quantum sessions and room shutdown | [XML](Docs/Modernization/verification/main-editmode-editor-repair.xml) |
| Current Main PlayMode | **14 passed; 0 failed/skipped**, including actual pointer and input lifetimes | [XML](Docs/Modernization/verification/main-playmode-editor-repair.xml) |
| Historical Main Boot (2026-10-14) | Actual 25-second native run: **0 runtime errors, 0 missing-script warnings, 0 C# diagnostics, 0 SDK import warnings** | [Boot](Docs/Modernization/verification/main-boot-ugs.json) |
| Historical tutorial/solo checkpoint (2026-10-14) | Actual authored UI → native runner/HUD; tutorial five entities/four bots; normal Maze 36 entities/35 bots, **270 advancing verified frames and 0 errors** | [Tutorial checkpoint](Docs/Modernization/verification/main-tutorial-runner-checkpoint.json), [normal Metal run](Docs/Modernization/verification/main-match-flow-ugs.json) |
| Cold Editor startup (2026-10-19) | **Pass**, zero errors/warnings before tests or Play | [Startup](Docs/Modernization/verification/editor-cold-startup-20261019.json) |
| Final Game view input/results/replay (2026-10-19 UTC) | **Pass on code 77c7da136 / Services 2.2.0**; movement/attacks in two matches, complete result/Home cycle and replay, zero runtime errors; one placeholder collider warning per map load | [Observed runner/input/events](Docs/Modernization/verification/interactive-final-native-input-20261019.json) |
| Final Android development APK | **Pass**, zero native build errors; one intentional Pipeline runtime-server-disabled notice. Actual APK 532,168,390 bytes; both ARM architectures and ZIP CRCs pass; no UiService test prefab among 8,970 packed records | [Current APK](Docs/Modernization/verification/android-editor-repair-20261019.json) |
| Final iOS unsigned export | **Pass**, zero native build errors; the same intentional Pipeline notice. Actual Xcode project, framework/data and 1,439 IL2CPP C++ files verified; test prefab excluded from 8,974 packed records | [Current export](Docs/Modernization/verification/ios-editor-repair-20261019.json) |
| UiService optional package tests | **47 additional cases pass**; expanded run 365/365. Normal manifest keeps package tests optional | [Expanded suite](Docs/Modernization/verification/expanded-ui-editmode-20261019.json), [original Resources fixture 5/5](Docs/Modernization/verification/ui-resource-fixture-20261019.json) |

Both final mobile builds pass through the existing target-correct Addressables/player builder at code commit `77c7da136`, with Services 2.2.0 and Pipeline 0.8.0-exp.1. Pipeline's runtime server intentionally remains disabled in players. The guarded UiService cache repair moves unchanged test resources under the existing Editor directory and ports one camera-query test API; five guard checks pass and the native player report confirms exclusion. [Repair and fresh-cache instructions](Docs/Modernization/ui-service-editor-repair.md).

Version stamping now happens at Play/build boundaries, avoiding import-worker writes during cold startup. ShaderGraph Power bases use their equivalent nonnegative inputs; final mobile reports contain no shader, adaptive-icon or test-prefab warnings; final Android also has no Gradle-template warning. Native ScriptableObject creation replaces remaining Q2 constructor patterns without changing the 35 copied Bot settings. Fixture JSON construction retains the original config rows and serializer settings.

The 2026-10-14 [Android](Docs/Modernization/verification/android-final.json) and [iOS](Docs/Modernization/verification/ios-final.json) records remain historical. Export success does not establish Xcode compilation, signing, device behavior or two-client acceptance. The earlier [pre-Services GUI proof](Docs/Modernization/verification/interactive-editor-native-input-20261019.json) remains historical. Earlier SDK/ShaderGraph/Boot failures remain causal evidence. Canonical SDK phases **1, 2 and 5** resolve native types/GUIDs; experimental phases 3/4 were rolled back. [Guarded SDK tool](Tools/Modernization/QuantumSdkLayout/README.md).

Five native matchmaking regressions pass; the original source genuinely fails with two synchronous reservations before the graph choice commits. The explicit next-player-loop dispatch and version/account guards fix the duplicate Photon join observed in the normal flow. Seven result-screen regressions preserve early clicks and fresh pooled results. [Matchmaking proof](Docs/Modernization/verification/network-matchmaking-start-summary.json), [UI proof](Docs/Modernization/verification/ui-presenter-result-summary.json).

## 2. Photon Quantum

The official stable target rechecked on 2026-10-19 is **3.0.14 build 2241**, released 2026-10-05; 3.1 was a preview. Matching **Bot SDK 3.10.2** and **Realtime 5.1.20** are installed. Bot 3.10.5 targets Quantum 3.1 and is not this release pairing. All nine Photon DLL payloads and 289 Bot payloads match their official packages; 16 layout targets and 22 generated GUID guards pass. Native `Assets/QuantumUser/Simulation` is authoritative. The old split `Quantum/quantum_code` is historical migration input. Real SDK code generation, 21 registered commands, system setup, view/prototype APIs and native SessionRunner are ported; no SDK/Frame stand-ins were used. [Decision and exact restoration](Docs/Modernization/quantum.md).

Conversion preserves **213 original Quantum IDs**, 20 populated standalone prototypes, 108 HFSM subassets and two blackboards. Ten triangle datasets and nine navmeshes use genuine Q2 readers/Q3 writers with geometry/topology readback, rather than treating incompatible bytes as equivalent. The 221-asset native catalog includes eight SDK defaults. Map entity caches are baked from actual scene sources separately from collision/navigation payloads.

Two genuine Main session cases cover actual AddPlayer/input, movement, bots, baked-collider queries, 1,200 fixed ticks and a 600-frame verified-checksum replay. The tests found a real blackboard allocation issue, now fixed. [Session proof](Docs/Modernization/verification/quantum3-native-session-summary.json).

Actual Photon authentication/private ephemeral room and UGS Boot-to-tutorial/normal-room runs pass using authorized local applications. FtueDeck has 17 native prototypes; Maze has 708. The normal rendered Editor run uses Metal and actual Home Play with an active HUD. This older checkpoint proves startup/frame progression. The dated Editor-repair record above adds actual input, attack and result/Home cycles; two-player gameplay remains unrun. [Room smoke](Docs/Modernization/verification/photon-room-q3.json), [normal flow](Docs/Modernization/verification/main-match-flow-ugs.json).

## 3. Web3 cleanup

Owned client wallet/contract/NFT/voucher/blockchain flows, packages, UI/config/localization and native hooks were removed. Ordinary reward/shop balances preserve persisted integer IDs; the renamed `BonusCurrency` group retains **41**. There are no external signing, transfer or redemption paths behind those balances. Removed-service history in untouched `Backend/` and provenance documents is deliberate; it is not a whole-repository zero-reference claim. [Cleanup scope](Docs/Modernization/cleanup.md).

## 4. GameLovers packages

The six maintained public families—GameData, Statechart, GoogleSheetImporter, Services, UiService and MobileServices—are pinned to exact verified commits. Generic embedded services/data/statechart/UI/import/native/input modules are removed. Game-specific adapters compose real current APIs; FLogger remains the project's logging implementation. **124/124 native package cases pass**, and Main gameplay exercises the integration. [Pins and trade-offs](DECISIONS.md), [adapter review](Docs/Modernization/package-adapter-review.md), [Services](Docs/Modernization/services.md), [mobile](Docs/Modernization/frameworks-mobile.md).

Observable `Rebind`/`NotifyAll` preserve subscribers when accounts reload. Native broker `PublishSafe` permits listener changes during UI/state transitions. Actual task-wait semantics and synchronous graph dispatch were reviewed; explicit player-loop boundaries prevent the two identified immediate-completion races. Backend fixtures retain old config type names through an in-memory binder, leaving `Backend/` unchanged. IL2CPP source export passes; physical device behavior remains unverified.

## 5. Plugins

| Dependency | Implemented free choice | Acceptance and limits |
| --- | --- | --- |
| Bakery | Built-in Unity lighting; retired actual missing Bakery components/settings | Native save/reopen and clean Boot; no lighting rebake or original-lighting claim |
| PlayFab | **14 genuine Apache C# files/99 top-level DTO and serializer types** from current source; no endpoints/HTTP/settings/factories | Native serialization parity 4/4 and actual source compile 0/0; a legacy game-facing schema, not an operational backend |
| Animancer | Game-owned native Playables with clip/end callbacks | Ten native animation cases pass; no paid Lite/Pro API façade |
| DOTween Pro | Game-owned UI VFX sequences; genuine public DOTween core **1.3.045** retained for current core callers | Actual Pro serialized values converted and native tests pass; device animation polish not assessed |
| I2 | Native offline catalog/localizers: **2,962 terms, three languages** | Native fixtures pass; original 26 empty English values show keys |
| PlayerPrefs Editor | Game-owned native PlayerPrefs authoring window | Actual compile; complete manual authoring session not run |
| SRDebugger | Development-only diagnostics/cheats/log/report panel | Actual source compile; physical development-player interaction not run |
| UITKTimeline | Pinned public MIT UPM source and native tint repair | Eight authored clips/11 tracks/four assets plus actual runtime blend/reset proof; all 26 assets have layer/curve audit |
| Odin | Native Unity inspectors, functional annotations and thirteen Editor windows; SDK-supported `QUANTUM_ODIN_DISABLED` | Actual compilation without Sirenix; complete authoring workflow not run |
| Nice Vibrations | Official MIT **4.1.2 / Lofelt 1.3.4** runtime | Checksum-pinned legitimate source/native restore; physical haptics not run |
| VoxelBusters | Maintained MobileServices alerts/local notifications, Unity iOS review/Android store URL, UGS guest/optional hosted accounts | Paid EssentialKit removed; reviews/listing/notifications on devices not run |
| Unity Purchasing | Retired disabled implementation, package and assembly refs | Earned catalog remains; cash purchases reject before charging or granting |

Singular, ParrelSync, Facebook, Firebase and unused EDM dependencies are removed. Paid caches were private intermediate migration inputs; fresh setup needs no paid plugin license. Original Apache DTO fields/methods and explicit enum integers are retained; unused Azure constants and outbound/generated endpoint surfaces are deleted. **17 files changed and 190 files retired** in the integrated DTO subset (191 deletions were prepared; Unity regenerated the provenance meta with its original GUID). Actual unchanged FirstLightServerSDK has no PlayFab assembly dependency. [DTO closure/parity](Docs/Modernization/playfab-dto-compatibility.md), [free animation](Docs/Modernization/verification/free-animation-summary.json), [Timeline](Docs/Modernization/verification/ui-timeline-tint-authored-native-proof.json), [purchasing](Docs/Modernization/verification/purchasing-retirement-summary.json).

Five ShaderGraphs were migrated natively. Metal GPU sampling verifies the two changed aim/line branches against the original formulas; it does not establish lighting quality or full game/device visual equivalence. [Shader migration](Docs/Modernization/verification/shadergraph-native-migration-summary.json), [GPU proof](Docs/Modernization/verification/shadergraph-native-gradient-gpu-proof.json).

## 6. Assets and secrets

Commercial art remains unacquired. Native materials/mesh and clearly marked unit-box replacements repair missing dependencies while preserving authored overrides and durable identities. Maze replaces 681 missing prefab roots from 65 source GUIDs and has 708 actual native prototypes. FtueDeck has 17 native prototypes and strict native scene save/reopen proof. Four banners and the stump prefab preserve native object/component identities.

The eight additional map restorations are integrated after separate-process reopen with original scene GUIDs. Actual prototype counts are Battlelands **479**, Cemetery **139**, District **223**, Fortress **140**, IslandOne **2,824**, Islands **943**, Test **80** and TestAssets **10**. Exact cache/managed-reference/source-field ledgers distinguish native prototype data from legitimate null pure-simulation view slots. [Native roundtrip/restoration](Docs/Modernization/verification/remaining-map-native-restoration-summary.json), [fresh restart](Docs/Modernization/verification/remaining-map-native-postrestart-proof.json).

Some stripped outer prefabs lack durable original descendant IDs. Unity-generated replacements have an explicit new-ID ledger; no original hierarchy/ID/geometry recovery is claimed for those objects. Unknown commercial geometry/default transforms remain approximations. Collision, navmesh and lighting bakes were not invoked during visual restoration. Maps other than FtueDeck/Maze have native restoration acceptance, not individual rendered match acceptance. The final raw audit still reports 18,777 reference rows and 1,291 GUIDs before last orphan cleanup, mostly retained missing-prefab provenance and unused design scenes; this is not a live gameplay blocker count. Native checks of all ten scenes verify no missing components/prefab connections. Five unused orphan settings/prefab assets and the unsupported virtual Addressables builder are retired; three native Timeline signals rebound. Unknown design-only components and inactive animation logging remain outside validated gameplay. [Reconciled audit](Docs/Modernization/verification/final-asset-audit-summary.json). [Visual limits](Docs/Modernization/visual-placeholders.md), [deleted inventory](DELETED-ASSETS.md).

Real public client IDs and switches are in ignored `Assets/Resources/LocalClientConfig.json` beside a blank committed template. Native Unity project binding agrees with config; the build helper uses the actual native setter and verifies its getter. Locally linked ProjectSettings account fields remain private; Resources ships in players and cannot protect server/service-account secrets. [Native binding](Docs/Modernization/verification/unity-cloud-project-link.json), [build configuration](Docs/Modernization/verification/cloud-build-configuration.json), [human steps](HUMAN_ACTIONS.md).

## 7. CI/CD

Old Azure pipeline, Fastlane and infra scripts/hooks are removed. SHA-pinned GitHub Actions run PR/manual EditMode, PlayMode and a nonzero genuine Quantum filter; tag/manual Android development builds and optional unsigned iOS exports include Addressables. Private restore checks its exact SHA and rejects traversal, symlinks, Backend writes and tracked overwrites. Public caches exclude proprietary files. Fork PRs lack secrets and fail explicitly; no secret-bearing `pull_request_target` bypass is used.

Executed locally: actionlint, **eight CI helper cases** and **seven serialized Addressable-ID mutation cases**, all passing. The Addressable contract preserves integer gaps and global-address bindings. Remote Actions/headless activation are **Not run**. Android debug-keystore and unsigned iOS development outputs need no store deployment or paid signing. [Commands/secrets/restoration](Docs/Modernization/ci-plugins.md).

## 8. Free backend

The user authorized **UGS only plus Photon**, replacing the initial hybrid plan. Actual Authentication, Cloud Save, Friends, Lobby/Multiplayer and Remote Config APIs serve the existing game callers. Photon owns deterministic rooms; UGS Lobby carries party metadata/reservations. Bundled config and catalog are enabled for initial play. No PlayFab title, Azure deployment, Cloud Code or custom server is required. [Decision and current free limits](DECISIONS.md), [implementation](Docs/Modernization/ugs-backend.md).

Twelve whitelisted progression models use Cloud Save Default access; searchable name/avatar/trophies use Public access. Genuine existing-key write locks reject stale updates. Session-generation and native `IsAuthorized` guards reject old or expired requests, queue completion removes its exact captured head, and refreshed authorized tokens preserve valid work. Cached guests keep their sole recovery credential; explicit hosted-account logout clears credentials only for a real linked Unity identity. Guest linking preserves identity/models; the obsolete cross-account tutorial merge is retired.

Existing local progression/reward/earned-currency commands remain client authoritative behind `IPlayerStateBackend`/`IShopCatalogBackend`. Cash purchase paths are disabled and global rankings explicitly unavailable. Unity Economy stopped new-project signups on **2026-09-08**, so it is not a fresh-setup dependency. No trusted grants, cross-device absent-key transaction, durable offline queue or old PlayFab-player migration is claimed. [Economy notice](https://docs.unity.com/en-us/economy/index).

Live native guest authentication, all twelve models, separate Editor restart, Public profile readback and a genuine stale-write-lock rejection pass in an isolated native profile. Actual configured Boot→tutorial and Home→normal solo game pass. Native SDK lifecycle **10/10**, account/store isolation **4/4**, expired-switch retry **5/5**, UI result **7/7**, matchmaking dispatch **5/5** and delayed-name account isolation **2/2** have scoped public evidence. These historical isolated counts are separate from the current Main 318-case suite. [Cloud proof](Docs/Modernization/verification/ugs-live-persistence.json), [restart](Docs/Modernization/verification/ugs-live-persistence-restart.json), [lifecycle](Docs/Modernization/verification/ugs-auth-authorization-guard-summary.json), [account policy](Docs/Modernization/verification/runtime-account-policy-summary.json), [name guard](Docs/Modernization/verification/display-name-isolation-summary.json).

Hosted browser linking/recovery, live account deletion, two-device conflicts and two-client Friends/Lobby are **Not run**. Authentication 3.8's own delayed name response can update its vendor cache; the bounded game guard protects the loaded game name, Public profile and notification. Physical Android/iOS acceptance remains pending.

## 9. Follow-up proposals — not implemented

### Architecture 1: provider-neutral backend contracts

- **Problem:** the operational backend is UGS, but auth/profile/catalog callers still expose 99 retained legacy SDK DTO/serializer types. This spreads provider-shaped schema through game logic/UI.
- **Proposal:** define game-owned immutable auth/session/profile/catalog/room results, confine SDK mappings to adapters, and add account-switch/persistence contracts. Keep future trusted authority at the existing backend boundary.
- **Expected benefit:** future provider or server-authority changes become smaller and easier to review; the remaining DTO source can retire without changing persisted integer/JSON contracts accidentally.
- **Rough effort:** 7–12 developer days including caller migration and regression fixtures; production server implementation is separate.
- **Files/systems:** AuthService, GameBackendService, PlayerStateBackend, ShopCatalogBackend, PlayfabStoreService and profile/matchmaking presentation callers/tests.

### Architecture 2: changed-content native validation gate

- **Problem:** raw GUID presence does not prove type/local-ID resolution, usable collision, valid config/prefab mapping or matching Addressables content. Native prefab caches and stripped descendants required real Editor roundtrips in this migration.
- **Proposal:** add a changed-content Editor command/CI gate that loads maps, UI/config/skin assets and validates required type/GUID/prototype/Addressables contracts. Keep explicit reviewed placeholder exceptions and identity ledgers.
- **Expected benefit:** authoring/import regressions fail before startup, shop or match failures; migration identity evidence stays reproducible.
- **Rough effort:** 4–7 developer days for the initial changed-asset gate; broader performance/render coverage follows separately.
- **Files/systems:** Tools/audit-assets.py, Editor/AssetImporters and SheetImporters, AssetResolverService, AddressableAssetsData and QuantumUser migration/validation tools.

### Feature 1: configurable practice session

- **Problem:** solo bots offer little structured control for learning aim, weapons, movement and special timing.
- **Proposal:** add Practice with bot difficulty/count, fixed loadouts and instant restart; show survival time, accuracy and damage using the current simulation/settings.
- **Expected benefit:** useful onboarding/player experimentation and repeatable balance/device checks without live queue setup.
- **Rough effort:** 5–10 developer days for UI/config/session/results.
- **Files/systems:** GameModeService, GameModeScreenPresenter, MatchSettingsView, RoomService, PreGameLoadingScreenPresenter, MatchEndDataService and Quantum bot/match systems.

### Feature 2: rotating gameplay objectives

- **Problem:** earned progression offers little short-term variety after the basic match loop is learned.
- **Proposal:** rotate objectives for weapon-category damage, team support, pickups and survival; version their config, show progress and award ordinary rewards once through the existing command/persistence boundary. Move trusted grants to a server later.
- **Expected benefit:** clear reasons to experiment and return using the existing earned economy.
- **Rough effort:** 8–15 developer days for state, aggregation, rewards and UI; live scheduling/anti-cheat authority is separate.
- **Files/systems:** RewardLogic, BattlePassLogic, MatchEndDataService, progression data/configs, MatchEndScreenPresenter, command queue and persistence interfaces.


Current build evidence: [Android](Docs/Modernization/verification/android-editor-repair-20261019.json), [unsigned iOS](Docs/Modernization/verification/ios-editor-repair-20261019.json). Historical content proof: [213 native assets](Docs/Modernization/verification/quantum-213-with-native-map-caches-validation.json), [all 5,563 native map view slots](Docs/Modernization/verification/accepted-native-map-view-slots.json). The fresh isolated map fixture had unrelated importer exceptions before its read-only validation; its validation phase is clean and final Main suites/import/gameplay provide separate clean-project evidence.
