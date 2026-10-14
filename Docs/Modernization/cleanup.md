# Client service cleanup and free diagnostics

## Source changes

Removed the complete client integration for Singular, ParrelSync, Facebook, Firebase App/Analytics and blockchain services. This includes initialization, tracking events, SDK-specific analytics parameter serializers, editor postprocessors, device testing harnesses, wallet/contract/voucher logic, service interfaces, player/private data, commands, cheats, settings UI, remote avatar imports, badges, localization terms and Addressables entries. Deleted assets keep no replacement components that pretend to connect or succeed.

Firebase was also a transitive dependency of Unity Push Notifications. Remote push registration, its settings/Android declarations and embedded Push Notifications assets were removed. The existing local mobile notification permission/scheduling path remains. After removal of VoxelBusters and the remote push SDK there is no remaining dependency XML consumer, so External Dependency Manager and its resolver libraries/settings were removed. Android manifests, dependency declarations and stale Gradle backups were cleaned.

The old vendor device-test integration was replaced with `TestResultWriter`, which writes actual test results and benchmark measurements to `Application.persistentDataPath/play-test-results.csv`. Existing unit/play test scenarios remain; the removed platform intent/device harness no longer controls a test run. Test results now report failure if captured errors exist. No upload happens.

NFT equipment data, inventory filters, cooldowns, protections and source-specific metadata have been deleted. Equipment costs use a single ordinary inventory. NFT-only tests were removed; ordinary inventory, upgrade, scrap and serialization tests remain. Legacy equipment conversion keeps its actual ordinary-item-to-Blast-Bucks behavior. The game’s existing constant durability result was not newly introduced by this work; a functional durability redesign remains outside this removal phase.

NOOB, partner and seasonal currency numeric IDs remain because match rewards, collected bonus calculations, saved player balances, shop prices, icons and character assets use them. They are ordinary in-game balances with no connection, signing, redemption or transfer to an external service. The `CryptoCurrency` grouping was renamed `BonusCurrency` while keeping numeric group ID 41. Gas-ticket rewards and the dead source ID row were removed. Root also owns the matching Quantum-generated source cleanup, preserving remaining numeric IDs.

Removed 82 obsolete localization terms. Ordinary skin acquisition descriptions now refer to gameplay rewards/the shop, and the match reward tooltip describes item bonuses. Existing non-English strings for changed acquisition descriptions were cleared to the catalog’s English fallback; translated replacements are not claimed. The purple loading-gradient artwork was renamed to remove an obsolete purpose-specific name, preserving its GUID.

The old production legal text contained clauses for deleted financial-product features and a former website fetch. It was replaced with clearly marked development privacy/terms placeholders. The operator must supply reviewed policies before distributing a build; these placeholders do not claim to provide approved production policies.

## SRDebugger replacement

The unavailable paid debug SDK is replaced with the project-owned `RuntimeDebugPanel` and `DebugOptions` sources. In the Editor and development players, the Diagnostics button opens a Unity IMGUI panel with categorized game cheats, currency and Quantum commands, reflected option properties, performance modes, bounded captured logs, build/device information and local report export. Numeric inputs are converted to their actual property types; invalid edits/actions display the real exception message.

Reports are written under `Application.persistentDataPath/Diagnostics/`. The old vendor reporter, private registry/services, sample option demonstration file, SDK-specific build hooks and assembly references have been deleted. The obsolete multi-client cheat was also deleted: its flag had no consumers and its old string-based handler did not exist. Release players do not create or expose the diagnostics component. The two SRF transform helpers were ported directly to Unity transform APIs; no SRF dependency remains. Reflection preservation targets the actual `FirstLight.Game` assembly.

## PlayFab source reduction

The backend owner selected the current Apache-2.0 public PlayFab SDK source at commit `04e73b3a78ce1afbba9f3e003f3fe51ba77bd8bd`. To satisfy literal removal of unused social SDK schemas as well as runtime integrations, a Roslyn syntax-tree rewrite removed 227 Facebook/Facebook Instant Games members, model types, event declarations and dispatch branches from the vendored SDK. Every surviving numeric value in 16 affected enums was checked against the original downloaded source and remains explicit. See `playfab-prune-evidence.json` and the package’s `PROVENANCE.md`.

The unused legacy Matchmaker SDK folder was omitted because upstream marks its API methods `Obsolete("No longer available", true)`; there were no game callers. Photon rooms and Unity Lobby remain the matchmaking path. The SDK singleton lookup was upgraded to Unity 6 `FindFirstObjectByType<T>()`.

## Package changes handed to root

Remove the following direct dependencies and their lock entries/transitive dependencies: `com.google.firebase.analytics`, `com.google.firebase.app`, `com.nethereum.unity`, `xyz.0xsequence.waas-unity`, `com.singular`, `com.veriorpies.parrelsync`, `com.unity.services.push-notifications`. Remove the corresponding OpenUPM scopes/registry if it becomes empty. SDK-specific assembly references must not remain after root finishes the complete assembly/package migration.

## Verification

- **Pass:** broad content scan of `BlastRoyale/Assets`, `ProjectSettings` and `Packages` found no removed-service or NFT references in code, settings or assets after excluding provenance/license documentation. The scan includes metadata; Unity asset-import history `sequenceNumber` is unrelated to the removed wallet SDK.
- **Pass:** no tracked files changed under `Backend/`. Its historical blockchain code remains as explicitly requested; this is the deliberate exception to a repository-wide zero-reference assertion. A source scan at this phase found 130 matching lines in that reference backend.
- **Pass:** Roslyn syntax parsing of client and vendored SDK sources reported zero syntax errors. This does not resolve Unity assemblies or prove a full game compilation.
- **Pass:** the reduced PlayFab SDK compiled independently against Unity `6000.3.26f1` managed assemblies as .NET Standard 2.1, C# 9, with Client/Admin/Server/Entity/Authentication/Addon/Economy/Multiplayer/Progression source paths enabled: **0 warnings, 0 errors**. Command: `dotnet build /tmp/blast-playfab-compile/compile.csproj --nologo`; output: `verification/playfab-sdk-compile.log`. This was a compilation only; no backend API request was made.
- **Pass:** the actual RuntimeDebugPanel source compiled independently against the same Unity assemblies with temporary fixture definitions only for version metadata/option ordering: **0 warnings, 0 errors**. Command: `dotnet build /tmp/blast-debug-compile/compile.csproj --nologo`; output: `verification/debug-panel-source-compile.log`. This validates Unity API/type usage in the panel, not the complete game or runtime UI behavior.
- **Pass:** Android manifest and the three edited Settings/Store/Rewards UXML documents parsed with Python ElementTree.
- **Pass:** `git diff --check` was clean at handoff.
- **Pass:** a later independent review ran the preserved pure package tests in an isolated real Unity `6000.3.26f1` project: **124 passed, 0 failed, 0 skipped**, exit 0. See `package-adapter-review.md` and the durable XML/log there. Unity execution was serialized with the other workers.
- **Not run by this worker:** full game Unity import, game EditMode/PlayMode tests, Addressables build, device builds, diagnostic-panel interaction and a live playable match. Root records those separate results in the main report.

## Remaining integration checks

Run the complete Unity compile and existing tests after root’s GameLovers/Quantum/package migration. Check the removed component references in scenes/prefabs via the Editor, inspect the Settings/Store/Rewards screens, and exercise ordinary currency purchases and reward collection. Inspect the development diagnostics on desktop and touch devices, then confirm no Diagnostics button appears in a non-development player. A whole-repository text scan will intentionally still find historical references in `Backend/` and decision/provenance documentation.
