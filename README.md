# Blast Royale — Unity 6.6 modernization

Mobile top-down battle royale using Unity **6000.6.6f1**, Photon Quantum **3.0.14 Stable 2241** and one Unity Gaming Services development backend. `Backend/` is retained unchanged as the future authoritative reference.

Current Editor acceptance on Unity **6000.6.6f1**: **318 EditMode and 14 PlayMode tests pass** with zero compiler or `UAC*` analyzer warnings, and bundled Addressables + player script compilation succeed cleanly. Earlier interactive Game view runs also verified movement, Quantum attacks, death/spectating, results, return Home and replay, with zero captured runtime errors. Both Photon SDKs are verified: Quantum **3.0.14 Stable 2241**, Bot SDK **3.10.2**, with Realtime **5.1.20**. Missing commercial map art uses accepted placeholders. [Current builds, exact evidence and unrun checks](REPORT.md).

## Prerequisites

- Unity 6000.6.6f1 with Android and iOS support (`/Applications/Unity/Hub/Editor/6000.6.6f1/` on macOS).
- Free eligible Photon and Unity accounts; actual Quantum3 SDK/Bot SDK download permission. SDK/source/binaries remain local and ignored.
- Git/Python3; .NET10 is used by the guarded SDK source-layout tool. Use the existing host tools; no extra Editor bridge is required.
- No Animancer Pro, DOTween Pro, Odin, Bakery, I2, SRDebugger or EssentialKit purchase is needed. Their surviving behaviors use free replacements.

## Clone to configured Editor

1. Clone the public repository, check out this modernization branch, and open `BlastRoyale/` only with 6000.6.6f1. Allow exact pinned UPM dependencies to resolve. Close this project's Editor before running any batchmode command or local dependency restore. One Unity process owns each project.
2. Restore the legitimate Photon 3.0.14/build 2241 and matching Bot SDK 3.10.2 locally. Follow [the exact SDK/codegen/metadata restoration order](Docs/Modernization/quantum.md). Preserve authored QuantumUser source/settings; never import the old quantum.code DLL into Q3. Apply source-layout phases 1, 2 and 5 in order through [the guarded tool](Tools/Modernization/QuantumSdkLayout/README.md).
3. With Unity closed, run the checksum-pinned public runtime restore from the repository root:

   ```sh
   python3 Tools/Modernization/restore_public_plugins.py
   ```

   It restores actual public DOTween core and MIT NiceVibrations/native runtime into ignored paths. It does not import the paid Pro components. [Dependency provenance](Docs/Modernization/ci-plugins.md).
4. Create the ignored local configuration:

   ```sh
   cp BlastRoyale/Assets/Resources/LocalClientConfig.json.template BlastRoyale/Assets/Resources/LocalClientConfig.json
   ```

   Copy only if no configured local file exists. Fill `PhotonQuantumAppId`, `UnityProjectId`, `UnityEnvironmentName` and the optional actual `UnityEnvironmentId`. Keep bundled shop/remote config enabled for initial development. IDs must be real; never put backend/service-account/server keys in Resources, which ships with the player.
5. Create/select one free [Unity Cloud project](https://cloud.unity.com/) and a **development** environment. Link this checkout to that exact project in Project Settings > Services; the native SDK uses `Application.cloudProjectId`, so JSON alone is insufficient. Enable Authentication, CloudSave, Friends and Lobby. Add the ascending Public string index for `player_name` to support friend lookup. Do not configure a PlayFab title or Azure deployment. [Exact human steps](HUMAN_ACTIONS.md).
6. Create a free **Quantum3** app in [Photon Dashboard](https://dashboard.photonengine.com/app/create). Another developer needs their own authorized App ID; this host's temporary authorized ID remains private.
7. After the first UPM import, close Unity and run `python3 Tools/Modernization/patch_mobile_services_scanner.py --project BlastRoyale` and `python3 Tools/Modernization/patch_ui_service_editor.py --project BlastRoyale`. These guarded repairs fix the pinned capability scanner, keep the unchanged UiService test fixture under its existing Editor folder, and port its camera-query test API. Reapply after deleting Library or resolving a fresh package cache. [Native proof and restore order](Docs/Modernization/mobile-scanner-repair.md).
8. Optional recoverable accounts use hosted Unity Player Accounts. Create native `Resources/UnityPlayerAccountSettings.asset` through Project Settings > Services > Player Accounts, configure its real client ID and allowed desktop/mobile redirects. Guest play does not require it. [Actual account SDK setup](Docs/Modernization/ugs-backend.md).

Keep locally linked project/account IDs out of Git, including Unity's modified ProjectSettings account fields. The blank template is the public setup contract. `DELETED-ASSETS.md` retains original missing-content provenance and current restore instructions.

## Playing and limitations

Open `Assets/Scenes/Boot.unity` using **FLG > Scene > Open Boot Scene** (Option+9 on macOS), then enter Play Mode after native project configuration is complete. Playing an arbitrary map directly is an authoring preview and does not run application initialization. First login runs the FtueDeck onboarding path; the bundled development mode then uses solo Photon rooms with bots. UGS Lobby carries party membership and room reservations for compatible party modes; Custom Games remain available.

Use **WASD** to move, **arrow keys** to aim and **Space** to attack, or the two onscreen joysticks. Aiming must provide a direction; Space alone with no direction does not prove an attack. Actual configured Boot-to-tutorial and normal Maze input, death/spectator/results/Home and another match pass. Physical device controls are not verified. Missing commercial art uses clearly marked unit-box approximations. Native source/prototype/cache/GUID/restart acceptance covers the other maps, without individual rendered-match or original-art equivalence claims. Local trophies and earned virtual purchases persist through Cloud Save. Global rankings and real-money purchases are unavailable. Progression/grants are client authoritative; future trusted behavior belongs at the marked boundaries pointing to `Backend/`.

## Local validation

Use the exact target Editor with this project's GUI closed. Explicit XML/log paths keep nonzero test evidence inspectable:

```sh
/Applications/Unity/Hub/Editor/6000.6.6f1/Unity.app/Contents/MacOS/Unity \
  -batchmode -nographics -projectPath "$PWD/BlastRoyale" \
  -runTests -testPlatform EditMode \
  -testResults "$PWD/artifacts/editmode.xml" -logFile "$PWD/artifacts/editmode.log"
```

Run PlayMode with `-testPlatform PlayMode`. The Main native Quantum fixtures are part of EditMode and use the real SDK; CI also filters the native simulation test assembly and rejects zero/all-skipped results. Addressables uses `-quit -executeMethod FirstLight.Editor.Build.BuilderLocalHelper.BuildAddressablesNoDebug`. Android builds use `FirstLight.Editor.Build.Builder.BatchBuild` and the workflow's explicit development/output parameters. An unsigned native iOS Xcode/IL2CPP export checkpoint passes; Xcode compilation, signing and device tests are not run. Final mobile builds are rerun after the latest content/source fixes.

```sh
actionlint .github/workflows/*.yml
python3 -m unittest discover -s Tools/CI/tests -v
python3 Tools/CI/test_addressable_id_contract.py -q
python3 Tools/CI/validate_addressable_ids.py
```

Actionlint, eight CI helper checks and seven Addressable contract cases pass locally. Main EditMode and PlayMode are rerun after the current package/runtime repairs, including a test run after actual Editor Play. Fresh final mobile outputs and their exact package checkpoint are linked from the report; physical-device acceptance remains unrun. Raw logs/APKs remain ignored; public evidence is under Docs/Modernization/verification.

## GitHub Actions

PR/manual jobs run EditMode, PlayMode and nonzero Quantum tests. Manual/tag Android development builds and optional manual unsigned iOS exports bundle Addressables. No remote run is available in this task.

| Repository secret | Meaning |
| --- | --- |
| UNITY_LICENSE | Legitimately activated Unity license used by GameCI |
| UNITY_EMAIL / UNITY_PASSWORD | Unity account activation credentials |
| CI_DEPENDENCIES_URL | Actual restricted HTTPS archive containing only legitimate ignored SDK/runtime prerequisites |
| CI_DEPENDENCIES_SHA256 | Exact private archive SHA256 |
| CLIENT_CONFIG_JSON | Actual ignored public-client configuration, separate from the SDK archive |

Private restore rejects unsafe paths, symlinks, Backend writes and tracked-source overwrites. Public package caches contain no proprietary SDKs. Fork PRs lack secrets and fail explicitly; maintainers can review and dispatch a trusted branch. Paid signing/store deployment is outside this development setup. [CI/license/restoration details](Docs/Modernization/ci-plugins.md).

## Decisions and evidence

[DECISIONS](DECISIONS.md), [REPORT](REPORT.md), [HUMAN_ACTIONS](HUMAN_ACTIONS.md) and [DELETED-ASSETS](DELETED-ASSETS.md) are the authoritative root deliverables. Package Git pins/provenance are in the manifest and decisions; API/asset/session evidence is linked from the report.
