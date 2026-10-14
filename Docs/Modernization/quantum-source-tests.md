# Quantum 3 source and native-code regression tests

Status: **Passed: source-port fixtures and two real native movement/bot/physics/replay tests; final gameplay flow is tracked in the root report**.

The scope is the ignored `.local/quantum3-source-project`, imported with the actual licensed Quantum 3.0.14 and Bot SDK 3.10.2 packages. Work here does not alter the old Quantum input tree or the original `Backend/` tree. The integrator owns copying validated game source/assets into the client and the final runtime build. Proprietary SDK binaries and lookup-table data remain local and ignored.

The assigned config/component/command port resolves actual generator/compiler diagnostics: the consumable signal uses valid comma-separated parameters and avoids reserved type names, bot asset references use generic `asset_ref<T>`/`AssetRef<T>`, Stats initializes its real Q3 collection handles, and PlayerCharacter initializes the actual Bot SDK blackboard. Q3's `IAssetSerializer` handles public runtime config/player fields, replacing the old no-op `SerializeUserData` hooks; MatchConfigs has a concrete default. Frame enumeration uses `MaxPlayerCount` and `GetPlayerData`. Native `DeterministicCommandSetup.AddCommandFactoriesUser` retains all 21 game commands, while `DeterministicSystemSetup.AddSystemsUser` preserves the original ordered 2D game systems. Obsolete asset config markers are removed; the fixed-point drop-rate inspector uses the actual SDK `RangeEx` attribute.

The two command/component HFSM triggers use the actual public `AIContext.SetHFSMAgentAndBlackboard` and by-reference context overload of `HFSMManager.TriggerEvent`. Metadata was read from the real Bot SDK assembly to verify these public signatures. The premature respawn path now returns after rejection; the old implementation logged the rejection and still sent the respawn event. The blackboard is initialized manually in the current game spawn path; registering SDK `BotSDKSystem.OnAdded` as well would require moving that initialization to avoid allocating it twice.

This follows the official [Quantum 3 DSL](https://doc.photonengine.com/quantum/v3/manual/quantum-ecs/dsl) and [asset model](https://doc.photonengine.com/quantum/v3/manual/assets/assets-simulation). Public game fields and actual SDK types remain the implementation; no fake SDK, shadow type or conditional compilation bypass is used.

## Native regression fixture scope

Three new fixtures are in scratch `Assets/QuantumUser/Tests/EditMode`, under namespace `FirstLight.Tests.EditorMode.Systems` for the existing CI filter. Their own Editor test assembly references the actual Quantum.Simulation and Quantum.Unity assembly GUIDs, the actual SDK assemblies and Unity's NUnit support. Scratch uses `com.unity.test-framework` 1.6.0.

- **14 command serialization cases:** actual PlayerEmojiCommand, WeaponSlotSwitchCommand, TeamPositionPingCommand and SpecialUsedCommand round-trip through the real Quantum BitStream codec; negative/fractional FP coordinates preserve every raw bit; all four ping types are covered; decoding consumes the complete payload; repeated serialization produces identical bytes. Signed index tests cover the wire payload only, not authorization or execution of invalid slots.
- **6 movement math cases:** actual QuantumHelpers direction/rotation round-trips for four cardinal directions, full steering and zero-weight steering operate on actual SDK Transform2D and fixed-point types. They load native SDK lookup tables through FPMathUtils.LoadLookupTables and fail setup if the real tables are unavailable. They exercise steering math, not physics collision or a simulated match.
- **1 Stats case:** the actual migrated constructor initializes all eight game stat slots, metadata and both real Q3 collection handles, covering the collection-field migration that caused compiler errors.

These are independent assertions against game contracts, not replacement implementations. No mocked Frame or source copies are used. The original entirely commented MovementSystemTest remains excluded from reported counts.

## Verification limits

A separate netstandard2.1 compilation of the fixtures against the freshly imported actual Quantum.Simulation, Quantum.Unity, deterministic/engine/log and NUnit assemblies passes with **0 warnings and 0 errors**, with warnings treated as errors. This is compilation evidence, not execution evidence.

The real SDK installer creates `Assets/QuantumUser/Resources/QuantumLookupTables.asset`; it references the downloaded `Assets/Photon/Quantum/Runtime/RuntimeAssets/LUT/*.bytes`. The exact installer method is `Quantum.Editor.QuantumEditorHubWindow.InstallAllUserFiles` (declared in the SDK's QuantumEditorHubWindowSdk.cs partial file). Native lookup data is never copied into a public fixture.

Actual Unity 6000.3.26f1 execution passed **21/21**, with **0 failed and 0 skipped**. The NUnit XML contains 21 actual test-case elements (14 command, 6 movement math, 1 Stats); the Unity process exited 0 and its log contains 0 C# compiler warnings/errors. See `verification/quantum3-native-tests-summary.json` for fixture/case names, coverage and raw-evidence SHA-256. Local raw XML/log evidence is `artifacts/quantum3-native-tests.xml` and `.log`. The asset-migration writer paused during this run, and the scratch Unity process exited before ownership was released. The integrator's real clean import and source compiler passes are separate evidence. Command execution against a real Frame, movement/physics systems, rollback/replay, bot navigation, asset migration, original menus, cloud sessions, a completed match and mobile player builds remain unverified by these fixtures.
