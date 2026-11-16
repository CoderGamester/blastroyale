# Modernization decisions

Decision snapshot: **2026-10-19**. Unity Gaming Services owns the development backend and Photon owns deterministic rooms. The reported Editor failure is repaired; native input, attack, result/Home and subsequent matches pass. Current host suites pass 318 EditMode and 14 PlayMode cases; final Services 2.2.0 code also passes native movement/attacks in two matches, results/Home/replay, with zero captured runtime errors. Mobile export, device and two-client acceptance remain separately reported. [Current evidence](REPORT.md).

## Unity 6.6 and project APIs

Target **Unity 6000.6.6f1** (`d20c9ac777da`), upgrading from the earlier `6000.3.26f1` (`de8167e7440c`) checkpoint with `6000.6.6f1`'s built-in and recommended package versions: URP **17.6.0**, Cinemachine **6.6.0** (Cinemachine 3 API and scene/prefab camera conversion), Timeline **6.6.0**, uGUI **2.6.0**, Test Framework **1.8.0**, Addressables **2.11.2**, Input System **1.20.1**, AI Navigation **2.0.16**, ProBuilder **6.1.3**, 2D Animation **16.0.1**, Cloud Save **3.4.2**, Multiplayer Services **2.4.1**, Wire **1.6.1**, Android min SDK **26** and target SDK **36**. Unity 6.6 removed legacy `UxmlFactory`/`UxmlTraits` and `VisualElement.transform`: MIT `com.annulusgames.debug-ui`, `com.quickeye.ui-toolkit-plus` and `dev.krajnc.uitoolkittimeline` are embedded under `Packages/` with `[UxmlElement]` and style-transform ports while preserving all Timeline script GUIDs. Static TextCore/TMP UI font assets were converted in place to Dynamic for Unity 6.6's Advanced Text Generator, `VersionUtils` writes to `game-version-data` so it never collides with `GameLovers.Services` 2.2's `version-data`, and all Unity 6.6 `CS0618`/`CS0619`/`UAC*` diagnostics are resolved (`0` errors, `0` warnings). [Historical 6.3 package record](Docs/Modernization/unity-package-upgrade.json).

Retaining Unity 6000.0.39f1 would postpone API work but fail the requested LTS target. Installing another editor or suppressing obsolete warnings would not validate this version. UI Toolkit custom controls now use real `UxmlElement`/`UxmlAttribute` source generation with partial classes, retaining existing names, attribute defaults and script GUIDs. Legacy VisualElement transforms are ported to style translate/rotate/scale and resolved-style reads. Source generation/type checks and a full-project clean Console are separate gates.

## Photon Quantum architecture

Choose **Quantum 3.0.14 Stable, build 2241** (released 2026-10-05), which was the latest stable in official Photon documentation checked on this date. Quantum 3.1 was a preview. The maintained 2.x branch was **2.1.10**, newer than this repository's 2.1.9/build 1529. [Official setup/download](https://doc.photonengine.com/quantum/v3/getting-started/initial-setup), [2.x alternative](https://doc.photonengine.com/quantum/v2/getting-started/initial-setup), [migration guide](https://doc.photonengine.com/quantum/v3/getting-started/migration-guide).

| Option | Effort/risk estimate | Decision |
| --- | --- | --- |
| Restore/update Quantum 2.1.10 | Roughly 2–5 developer days for baseline restoration/validation; lower immediate API and serialization risk. Keeps the old split .NET Framework/Mono codegen/export path and requires this major migration later. The first dotnet build failed on missing .NET Framework 4.8 references; a later direct compile using the target Editor's bundled Mono references and the real 2.1.10 SDK succeeded. | Rejected as the final architecture; may be needed temporarily by official preparation/export tooling. |
| Migrate to Quantum 3.0.14 | Roughly 1–3 developer weeks for source, assets, custom Bot SDK and device validation; higher one-time serialized-asset/API risk, increased by missing art and SDK integration. | Chosen for the maintained long-term project. Estimates are planning ranges, not measured completion time. |

The intended single source of truth is `Assets/QuantumUser/Simulation` in `Quantum.Simulation`; SDK code generation occurs in the Editor, and generated view/editor wrappers follow the SDK layout. A standalone .NET project may be a companion using the same source, but the old split simulation DLL must not coexist as a second authoritative implementation. Proprietary Photon SDK/server files stay ignored. No fake DLL/type stubs or inferred GUID substitutions are acceptable migration evidence. [Quantum project structure](https://doc.photonengine.com/quantum/v3/manual/quantum-project), [current migration evidence](Docs/Modernization/quantum.md).

The actual Quantum 3 source, native assets and code generation are integrated. Latest Main EditMode passes 318 cases, including two real native session/replay cases. Original triangle/navmesh data uses real Q2 decoding and Q3 serialization with bounded identity/payload proofs. Proprietary files stay ignored; restoration tools contain no SDK implementation. [Restoration and acceptance](Docs/Modernization/quantum.md).

## Editor recovery and package ownership

Keep the user's installed AI Desktop package. Upgrade its existing official Unity Pipeline dependency from **0.6.0-exp.1 to 0.8.0-exp.1**, rather than editing its Roslyn/vendor implementation. The old version failed compile through an Unsafe reference collision; the official update compiles and runs native tests. No additional bridge was installed.

**RoomService owns leaving Photon rooms.** Configure the Quantum communicator with `ShutdownConnectionOptions.None`; the existing explicit room leave and captured-room shutdown fallback handle cleanup. Native SDK shutdown otherwise tries to leave a client already Leaving and falls through to disconnect. Failed ReconnectAndRejoin must fall back to Master instead of waiting for a callback that never comes. Four native communicator cases and repeated real result/Home/replay flows verify this change.

Use native listener ownership for manual Quantum subscriptions, detach input callbacks before disabling held controls, and cancel game-start waits with the state that owns them. UI Toolkit pointer cancellation/capture-loss/detach resets the actual input values. Keep deliberate authoring previews; document the existing Boot entry instead of globally forcing every Play action to replace the open scene.

The retired avatar CDN cannot serve a new developer. Serve its avatar paths from bundled character artwork with the bundled default avatar for unavailable variants; preserve ordinary remote URL behavior. These are already-public project images, not newly acquired commercial art.

The installed SDK audit reconfirms **Quantum 3.0.14 Stable 2241 + Bot SDK 3.10.2 + Realtime 5.1.20**. Bot 3.10.5 explicitly targets Quantum 3.1; choosing it by version number would create an incompatible pairing. [Exact payload and guard audit](Docs/Modernization/verification/sdk-installed-audit-20261019.json).

The two remaining runtime Q2 asset constructions now use the official platform-safe `AssetObject.Create<T>()` factory. All35 bot navigation assignments remain identical. Native integration fixtures create ScriptableObject configs through a local Newtonsoft contract resolver while preserving the original typed data/version; `Backend/` remains unchanged. Version stamping runs only at Play/build entry, with synchronous import, so startup import workers never race writes to the generated resource.

## Removed client integrations and ordinary currency

Delete the complete unused client integrations for blockchain wallets/contracts/vouchers/transfers, Singular, ParrelSync, Facebook, Firebase App/Analytics and their UI/config/localization/native dependency paths. Retaining disconnected initialization or invisible menus would leave runtime/build dependencies that the game no longer needs.

Unity remote Push Notifications depended on Firebase and was removed with its registration/settings/declarations. Existing **local** mobile notifications remain. VoxelBusters removal leaves no EDM dependency XML consumer, so External Dependency Manager and resolver settings/binaries were removed. No substitute component pretends that removed services connect successfully.

Keep numeric IDs for the ordinary NOOB/partner/seasonal balances because rewards, saved balances, shop prices, artwork and simulation configuration still use them. Rename their old crypto grouping to `BonusCurrency` with group ID 41 preserved; external transfer/redemption/signing has been removed. A wholesale numeric-ID rewrite would break persistence and serialized game assets without benefiting removal. `Backend/` is deliberately untouched, so historical removed-service references remain there and in decision/provenance documentation. [Cleanup scope and scan evidence](Docs/Modernization/cleanup.md).

## Maintained GameLovers package family

Use the current public package family linked by [Frameworks](https://github.com/CoderGamester/Frameworks), pinned to verified Git commits. Delete the old generic embedded modules and port callers/tests to the actual current APIs.

| Package/repository | Pin | Port decision |
| --- | --- | --- |
| [GameData](https://github.com/CoderGamester/Unity-GameData) | `3c6147829ba6154d92a0dd257c69bf167b20c3b7` | Native observable fields/collections and config support replace old Data Extensions; explicit keyed/global notification flags and rebind helpers preserve actual game observers. |
| [Statechart](https://github.com/CoderGamester/Statechart-HFSM) | `21aabc10111ffe4adbe5d4a7e4843a648994ce39` | Native state graph and fixtures; task-wait event differences were checked against current game state nesting. |
| [GoogleSheetImporter](https://github.com/CoderGamester/Unity-GoogleSheet-Importer) | `c04b1e89c740d3e18db523a0fdfacd8299d028e1` | Current importer/parser APIs and retained original CSV fixtures. |
| [Services](https://github.com/CoderGamester/Unity-Services) | `12af946385ab192b6c29c62e2c33ccc33455a265` | Real Time/Tick/Coroutine/Installer/DataService/Pool/MessageBroker; game-specific client adapter supplies dynamic legacy state contracts. |
| [UiService](https://github.com/CoderGamester/Unity-UiService) | `aa5fa65457b53601de647bcd6c925e435485698e` | Real package loading/lifecycle with a game UI Toolkit adapter; await transitions before unload. |
| [MobileServices](https://github.com/CoderGamester/Unity-MobileServices) | `8ce2d7295afd200895179c981e5e43e60ce11d18` | Native UI, notifications/gestures package; old unused sample Input Extensions removed after consumer/GUID scans. |

**Services 2.2.0** was checked against the live public repository on2026-10-19. Its parameterless store still uses the same PlayerPrefs keys; retain that storage choice. The other five pins match their public HEADs. [Source comparison](Docs/Modernization/services-2.2-update.md).

`ClientDataService` composes real maintained typed DataService stores, retaining Type.Name local save keys and the server SDK's dynamic IDataProvider boundary. Remote snapshots are not accidentally written to local PlayerPrefs. Maintain only four custom project implementations—ThreadService, RemoteTextureService, MaterialVfxService and AsyncGameObjectPool—in game-owned infrastructure; the generic Services module is deleted. Device IL2CPP/AOT behavior remains to verify.

Use the package's actual `PublishSafe` snapshot behavior because listeners can subscribe/unsubscribe and open screens/state transitions during publication. Normal `Publish` rejects these mutations; changing that behavior would be a game regression. Port unsubscription to subscriber targets. Addressables/asset importing uses the maintained package namespaces; project adapters retain only game behavior. [Services](Docs/Modernization/services.md), [UI/data/statechart review](Docs/Modernization/package-adapter-review.md), [mobile migration](Docs/Modernization/frameworks-mobile.md).

The native task-wait contract queues a parent transition until the task completes rather than interrupting the task. Original fixtures were updated to test that exact change. A source graph audit found no enclosing game Event transition using that path; future cancellation needs an explicit caller contract. The original pure package fixtures ran in real Unity 6000.3.26f1: **124 passed, 0 failed, 0 skipped**. This validates those package families, not the full game.

## Plugin choices

| Dependency | Choice, alternatives and reason | Limit/acceptance |
| --- | --- | --- |
| Bakery | Remove obsolete settings/components and use Unity's built-in lightmapper. Paid bake source is not required for the development runtime. | Actual Boot missing-script retirement and native scene roundtrips pass. No lighting rebake or original-lighting equivalence is claimed. |
| PlayFab SDK | Retain only the genuine Apache DTO/serializer dependency closure from **2.244.260922**, commit `04e73b3a78ce1afbba9f3e003f3fe51ba77bd8bd`, as `file:PlayFabSDK`. Remove outbound endpoint/HTTP/settings/factory code. | 14 original C# files/99 top-level types retain native fields and explicit enum values; four native parity cases and source compile pass. 17 files changed/190 retired after native integration. No operational PlayFab backend. |
| Animancer | Replace Pro with game-owned native Unity Playables and port animation callers/serialized references. | Ten isolated animation checks and main compilation pass; rendered/device behavior remains to test. |
| DOTween Pro | Replace Pro animation components with game-owned sequences using public DOTween core **1.3.045**. | No paid Pro package is required; ten native animation cases pass. Device visual quality remains unverified. |
| I2 Localization | Replace with game-owned offline localization and the actual exported catalog: **2,962 terms**, English/Russian/Portuguese. Port every used translation/list surface and TMP component. | No unused remote spreadsheet synchronization retained. English fallback/unknown-key detection exist; 26 originally empty English values visibly show keys. Native locale fixtures pass in the Main suite. |
| PlayerPrefs Editor | Implement a small Editor window over real PlayerPrefs, known LocalPrefsService keys plus explicit key entry. | Does not invent portable enumeration of all OS preferences; Editor interaction remains to test. |
| SRDebugger | Replace unavailable paid SDK with development-only game diagnostics: categorized options/cheats, captured logs, local report export. | Independent source compile passes; runtime touch interaction/release exclusion remains to test. |
| UITKTimeline | Install public MIT UPM source at `68e9114b94ba4a9123a4612d6fd6ce9fc98a7868`. | Native authored tint playback covers eight clips/11 tracks/four assets; runtime graph blend/reset and all 26 layer/curve audits pass. This is not full animation/art/device acceptance. |
| Odin | Replace runtime annotations and thirteen Editor windows with game-owned inspectors and native Unity serialization. Use the SDK-supported QUANTUM_ODIN_DISABLED path. | Main C# compilation passes without Sirenix; editor authoring interaction remains to test. |
| Nice Vibrations | Use official public MIT **4.1.2 / Lofelt SDK 1.3.4** source and real native libraries, excluding demos/obsolete packs. | Repository is archived; physical Android/iOS haptics need testing. Checksum pin makes restoration reproducible. |
| VoxelBusters | Remove Essential Kit. Use the explicit unavailable native-login provider, UGS guest auth and optional hosted Unity Player Accounts; use Unity iOS review request and Android store URL. Native alerts use maintained MobileServices. | Store login is unavailable explicitly; reviews require a real listing and device behavior is unverified. No paid package needed for first play. |

Paid caches were legitimate intermediate migration inputs and remain private. The final runtime source uses free replacements; developers do not need Animancer Pro, DOTween Pro or Odin. Public restore downloads are checksum pinned. [Plugin restoration](Docs/Modernization/ci-plugins.md).

## Free development backend and authority

Choose **UGS as the single operational backend**, with Photon Quantum rooms. The user authorized this migration after questioning the original hybrid setup. One real UGS identity owns Authentication, private Cloud Save progression, Public profiles, Friends and Lobby. Remote Config may use bundled development defaults. No PlayFab title, Azure deployment or custom server is required.

| Option | Setup/capacity | Decision |
| --- | --- | --- |
| UGS only | Authentication/Remote Config free; Cloud Save 5 GiB plus 1 million reads/writes monthly; Lobby 10 GiB monthly per regional group; Friends 50,000 active users/month. | Chosen: existing UGS packages, one identity and one project. Finite free allowances require usage monitoring. |
| PlayFab plus UGS | Existing DTOs reduce the first source port, but two accounts need linking and recovery across providers. The separate new Foundation preview has account/onboarding prerequisites; this is not a claim that every legacy PlayFab Development account requires them. | Rejected: no existing live player data requires this complexity. |
| Firebase Spark | Free Auth/Firestore quotas; Functions require billing-enabled Blaze. | Rejected: adds another SDK/provider and conflicts with the requested Firebase removal. |
| Nakama/Heroic Cloud | Open-source self-hosting is free; managed hosting is billed and a permanent free hosted tier was not established. | Rejected: requires hosting/server work outside the requested setup. |

Capacity checked on 2026-10-14: [UGS pricing](https://unity.com/products/gaming-services/pricing), [PlayFab Foundation onboarding](https://learn.microsoft.com/en-us/xbox/playfab/get-started/foundation-onboarding), [Firebase plans](https://firebase.google.com/docs/projects/billing/firebase-pricing-plans), [Heroic pricing](https://heroiclabs.com/pricing/). Account eligibility and service enablement must be verified in the actual account.

Unity Economy stopped new-project signups on **2026-09-08**. Original virtual progression/shop commands therefore remain client authoritative and persist through Cloud Save. No Economy SDK or Cloud Code hosting is added. Real-money grants remain disabled without receipt validation. Existing-key write locks reject conflicting saves; absent-key creation is not claimed atomic across devices. Failed snapshots remain queued for the next command; app-termination durability and conflict-merge UI are not implemented. [Official Economy status](https://docs.unity.com/en-us/economy/index), [implementation and limitations](Docs/Modernization/ugs-backend.md).

Guest login uses the SDK's cached identity. Optional hosted Unity Player Accounts provides account linking and recovery. Native username/password recovery requires a custom admin server, so the old email/reset UI is retired. Linking preserves the guest ID and loaded state. Explicit hosted logout clears cached credentials only for a real native linked Unity identity; guests retain their sole recovery credential. Real IsAuthorized/session-generation guards reject expiry and old completions while native authorized refresh preserves valid work. Legacy public PlayFab DTOs remain for game-facing schema compatibility; the unchanged FirstLightServerSDK binary has no PlayFab assembly dependency. The retained source contains no endpoints or transport. [Closure/parity evidence](Docs/Modernization/playfab-dto-compatibility.md). [Hosted accounts](https://docs.unity.com/en-us/authentication/unity-player-accounts), [username/password limitation](https://docs.unity.com/en-us/authentication/platform-signin/username-password).

Solo/bot matches use Photon rooms; UGS Lobby carries party metadata/reservations. Native room metadata/version tests pass, but live two-client parties are not tested. Global rankings are explicitly unavailable. Trusted grants, anti-cheat, server arbitration and authoritative progression belong at the explicitly marked boundaries pointing to unchanged `Backend/`.

## Missing visual content and secrets

Restore only legitimate content or native Unity-created placeholders with original referenced identity. This phase restored **29 materials and one mesh**, all clearly named PLACEHOLDER, under `Assets/Art/ModernizationPlaceholders`. Unity validated actual asset types/GUID/local file IDs and roundtripped the original stump MeshFilter/Collider references. The necessary imported-model pointer type changed through real Unity PrefabUtility serialization; no arbitrary prefab hierarchy or YAML schema was invented.

The initial material/mesh subset removed **87 unresolved rows/30 GUIDs**; it was not a final whole-project result. Subsequent native restoration preserves Maze's 708 and FtueDeck's 17 scene-authored prototypes, then integrates eight additional maps after exact source-field/cache/managed-reference/GUID and separate-restart checks. Native Unity-generated IDs are recorded for stripped descendants lacking durable source IDs. Unknown commercial geometry/default transforms remain marked unit-box approximations; no collider/navmesh/lighting rebake is performed. Actual tutorial runner/frame progression is proven for FtueDeck; the graphics-enabled started match is proven for Maze. [Visual limits](Docs/Modernization/visual-placeholders.md), [additional maps](Docs/Modernization/verification/remaining-map-native-restoration-summary.json).

Keep public client IDs and development switches in ignored `Assets/Resources/LocalClientConfig.json`, with a committed template. Required account choices are documented, never invented. Resources JSON ships in the player, so it must contain no backend secret key or service account. Editor-linked UGS project identity is checked against local config because UGS Core reads `Application.cloudProjectId`. Credentials were copied locally without publishing their values.

## GitHub Actions and build distribution

Delete Azure pipeline/Fastlane/infra scripts and their dead Cloud Build hooks. Use pinned GitHub Actions for PR/manual EditMode, PlayMode and Quantum tests; Android tag/manual development APKs; optional manual unsigned iOS Xcode exports. Cache only public Unity registry source. Proprietary restores use a real owner-supplied private HTTPS archive and SHA-256, checked before any write; reject traversal/symlinks/Backend/tracked overwrites. Client JSON is a separate secret. [Workflow and restore details](Docs/Modernization/ci-plugins.md).

Fork PRs cannot access repository secrets and fail clearly. A maintainer reviews code and dispatches a trusted branch. Running secret-bearing `pull_request_target` against untrusted changes was rejected. Test XML must contain actual passed tests and no failures; missing/zero/all-skipped runs cannot claim success.

Android development builds use Unity's debug keystore; iOS exports an unsigned Xcode project. Store deployment, paid signing and hardcoded signing secrets are unnecessary for this development scope and remain separate human actions.

**Verified:** Main **318 EditMode/14 PlayMode**, actual native movement/bots/physics/checksum replay, live UGS guest/twelve-model/Public-profile/restart/write-lock conflict, clean Boot, authored tutorial and actual normal Home Play→Maze with Metal/HUD/270 verified frames/35 bots/zero errors, native map source/cache/GUID/fresh-restart acceptance, canonical SDK import/API/IL parity, native shader GPU/Timeline checks, UGS Addressables checkpoint, earlier Android APK and unsigned iOS Xcode/IL2CPP export, package/backend/UI regression fixtures, actionlint and CI helper checks. **Current exports:** final Services 2.2.0 Android APK and unsigned iOS Xcode/IL2CPP export pass with zero build errors; one intentional Pipeline runtime-server-disabled notice each. See the dated Editor-repair records in REPORT.md. The earlier mobile records remain historical. **Not run:** remote Actions/headless activation, Xcode compilation/signing, physical device tests, live hosted-account recovery/deletion and two-client social scenarios. Individual rendered matches on the eight additional maps and original paid-art equivalence are not claimed.


The final native mobile scanner patch preserves the required upstream Git pin and excludes only assemblies outside the actual Player compilation inputs; an actual runtime permission positive control still works. The exact source hash guard and closed-Editor restore order are documented in [scanner repair](Docs/Modernization/mobile-scanner-repair.md). Final Android/unsigned iOS builds pass; physical-device and remote CI checks remain unrun.
