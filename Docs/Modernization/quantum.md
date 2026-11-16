# Quantum 3 ownership, restoration and acceptance

The branch uses actual **Quantum 3.0.14 Stable, build 2241**, released2026-10-05 and verified as current stable on2026-10-14. Quantum3.1 was preview; maintained2.x was2.1.10. [Official setup/download](https://doc.photonengine.com/quantum/v3/getting-started/initial-setup), [migration guide](https://doc.photonengine.com/quantum/v3/getting-started/migration-guide), [project structure](https://doc.photonengine.com/quantum/v3/manual/quantum-project).

Choose3.x for the maintained Unity assembly workflow. Updating2.x would reduce immediate API/asset work but retain the split Mono/.NET Framework pipeline and defer the major migration. Planning estimates:2–5 developer days for2.x restoration versus1–3 developer weeks for3.x source, missing content and device acceptance. These are estimates, not measured completion times.

## Source ownership

- Authoritative simulation: `BlastRoyale/Assets/QuantumUser/Simulation`, compiled by native `Quantum.Simulation` through the committed assembly reference. Client uses actual `Quantum.Unity`, `Quantum.Simulation` and Photon Realtime APIs.
- Original `Quantum/quantum_code` is historical Q2 migration input. Do not edit/build it as a second authoritative source or import its old DLL into Q3.
- Licensed SDK/source/libraries below `Assets/Photon`, generated code and SDK default resources remain ignored. Game-owned inspectors and authored data remain public.
- `Backend/` is unchanged. Tests remap old assembly names only in an in-memory fixture copy.

## Fresh-machine SDK restoration

1. Install Unity `6000.6.6f1` with Android/iOS modules. Sign in to your eligible Photon account and download the official 3.0.14 build 2241 SDK and Bot SDK 3.10.2. Q2/migration downloads are needed only to reproduce historical conversion, not to run converted authored data.
2. Import the genuine SDK and Bot SDK locally under ignored `Assets/Photon`. Preserve the branch's authored `Assets/QuantumUser` source and `QuantumEditorSettings.asset`; do not overwrite them with sample user files/settings. Do not import old `quantum.code.dll`.
3. In local `Assets/Photon/Quantum/Simulation/Quantum.Simulation.asmdef`, remove `Sirenix.Serialization.dll` and `Sirenix.OdinInspector.Attributes.dll` from explicit precompiled references, reference game-owned `FirstLight.Inspector`, and add `Quantum.BotSDK.Core.dll`. Retain the actual native engine/deterministic/log references. Sirenix is not required.
4. Keep committed `QUANTUM_ODIN_DISABLED` and `WIRE_EXCLUDE_WEBSOCKETSHARP` platform defines. The first selects vendor-supported ordinary inspectors; the second excludes Wire's unused legacy DLL through its existing constraint. Current Wire uses `UnityWebSocketSharp`; actual Photon WebSocket adapters compile against the bundled Photon version.
5. With Unity closed, apply the exact SDK source-layout manifests for phases 1, 2 and 5 in order as documented in `Tools/Modernization/QuantumSdkLayout/README.md`. This moves genuine partial declarations without changing members/API/IL, preserves existing metadata and contains no proprietary payload. Then run actual SDK QTN codegen with zero legacy flags. `Tools/Modernization/QuantumAssetMigration/ModernizationNativeCodegen.cs` invokes that real API; temporarily install it and its supplied assembly definition in an Editor folder for batch use. Do not regenerate legacy wrappers.
6. Close Unity. Run `python3 Tools/Modernization/restore_quantum_generated_guids.py --project BlastRoyale --apply`. It verifies22 exact generated source hashes/class identities, edits only metadata GUIDs and keeps recovery copies. If hashes differ, review SDK/version/codegen before applying any mapping.
7. Reopen the target Editor and let actual SDK database generation run. Authored editor settings preserve213 original Quantum IDs. `quantum-original-guid-registry.json` and `QuantumOriginalGuidInstaller.cs` provide strict content/type/Unity-ID checks for reproducing that registration; this installer requires the native postprocessor disabled temporarily. Normal setup retains committed authored overrides.
8. Fill ignored local config. A fresh developer needs a real free Quantum 3 App ID stored only in local JSON. Do not copy another project's ID without permission.

| Official download | SHA-256 |
| --- | --- |
|3.0.14 stable2241 unitypackage|`5e12afa93acace8f99a8bd7a396df3a3ea4f02e7177b52d2d7d3c5f2c5d5146c`|
|3.0.14 migration2241 ZIP|`86a5a47f7cb82dfe4d848a73dc798dee376d3b7374c7e00dcc509ced33dfde7d`|

## Actual conversion evidence

Authenticated SDK download resolved the initial sign-in blocker. Q2 official codegen/linking and actual simulation compilation passed using the target Editor's bundled Mono4.8 references, resolving the earlier MSB3644 diagnostic without another runtime/editor bridge.

Official Q3 import copied241 authored source/DSL files. Native codegen and matching Bot SDK compilation passed. All19 custom asset classes explicitly derive from genuine native `AssetObject`; final generation uses no legacy wrappers.

The preparation export recovered213 original assets with original Unity GUID/local IDs and Quantum IDs. Actual native factories preserve17 complete prototype payloads. Twenty formerly lazy prototypes are populated native `.qprototype` assets with recorded canonical path changes. AI conversion preserves108 HFSM subassets and2 blackboards. Strict native post-restart comparisons verify authored data; no fabricated SDK or Frame was used.

Runtime catalog loading exposed further binary format changes. Ten original triangle datasets and9navmeshes were converted with actual Q2 decoders and Q3 serializers/readback, preserving geometry/topology. Simply copying bytes or adding a header is invalid. Strict213asset validation passes with exact source/native hashes and those bounded readback ledgers. The original empty Maze cache was populated through native scene baking and SDK serialization with 708 real authored prototypes. Other Map payloads were verified unchanged. Native SessionRunner tests now pass movement, bots, collider queries and recorded verified replay checksum equality. FtueDeck has 17 restored native prototypes and passes actual Boot-to-tutorial room/start acceptance with five player entities, including four bots, and advancing verified frames. All ten native maps are restored with5563prototype/view slots and strict source/cache/restart evidence; other maps were not individually played.

## Fresh acceptance checkpoint

| Check | Result |
| --- | --- |
| Main target-Editor Q3 import | Pass, exit 0, zero C# errors/warnings |
| Main EditMode | Pass 314/314, zero failed/skipped; [XML](verification/main-editmode-ugs.xml) |
| Main PlayMode | Pass 6/6, zero failed/skipped; [XML](verification/main-playmode-ugs.xml) |
| Native value/command tests | 21 included in Main EditMode |
| Addressables | Final bundled content passes, exit 0; no authored UI/SDK import warnings |
| Main Boot scene | Actual 25-second run: zero errors/missing scripts; live UGS Home Play reaches a started Metal Maze match with 35 bots and 270 verified frames |
| Native Frame/bots/replay | Pass 2/2; real 1,200-tick gameplay and 600-tick checksum replay, included in 314 |
| Android development build | Final UGS APK passes, exit 0; [evidence](verification/android-final.json); device unrun |
| Live backend match | Normal UGS Maze solo passes; two-client scenarios unrun |
| iOS unsigned export | Final Xcode and IL2CPP export passes; signing/Xcode compilation/device unrun |

Commands use installed target Editor with `-batchmode -nographics -projectPath …/BlastRoyale`. Tests use `-runTests -testPlatform EditMode` or `PlayMode` and explicit XML/log paths. Addressables uses `-quit -executeMethod FirstLight.Editor.Build.BuilderLocalHelper.BuildAddressablesNoDebug`. Raw outputs remain in ignored `artifacts/`; public evidence contains no configured App IDs/credentials.

Historical Q2 validation passed272EditMode and6PlayMode cases. It is migration evidence, not final Q3 acceptance. SDK import/compiler passes do not establish a playable match, clean content console or remote CI.
