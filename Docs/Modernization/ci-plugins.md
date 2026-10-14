# CI and plugin modernization evidence

This note covers sections 5 and 7. Unity compilation, Addressables output, device haptics and mobile builds have not been verified by this agent. The root agent owns serialized Unity runs and the final report.

## Choices and implementation

| Dependency | Choice | Evidence and remaining work |
| --- | --- | --- |
| Bakery | Use Unity's built-in lightmapper. Delete the obsolete Bakery project settings. | No Bakery API usages exist in the project C# source. Existing lighting and any missing serialized Bakery components still require the root asset audit and an Editor rebake check; removing settings is not proof that lighting is correct. |
| PlayFab | Owned by the backend migration agent. | No backend files were changed by this agent. |
| Animancer | Restore the available Animancer Pro v8 package locally. | The actual package manifest reports 8.4.0. Restored 823 entries under ignored `BlastRoyale/Packages/com.kybernetik.animancer/`. `AdditionalFlairAnimation` uses real `Play(AnimationClip)` / `Events(owner).OnEnd` APIs. Editor/build API validation remains. |
| DOTween | Restore cached DOTween Pro and update its core from the current public vendor download. | Restored 191 cached entries; public core is 1.3.045, dated 2026-10-12 on the vendor download page. Pro remains the acquired cached package; its exact release number is not established. Generated real `DOTween.Modules`, `DOTweenPro.Runtime` and `DOTweenPro.Editor` assembly definitions in ignored vendor paths. No compatibility stubs. |
| I2 Localization | Replace with a game-owned, bundled translation catalog. | Exported 2,962 text terms and all three actual language columns (English, Russian, Portuguese). Empty `Nickname?` / `File` helper columns are omitted. Ported 94 C# files, including generated accessors, to `FirstLight.Game.Localization`. Every accessor key exists in the catalog. Rebound all three old TMP Localize components in ServerSelectScreen to a real `LocalizedText` component. English fallback and unknown-key detection are implemented. Deleted the old I2 language asset and its remote spreadsheet sync settings. 26 English values were already empty; these display their keys as visible missing-translation placeholders. Six locale/fallback/missing-key NUnit cases were added and have not been run in Unity. |
| PlayerPrefs Editor | Replace with a game-owned Unity Editor window. | `FLG > Tools > Player Preferences` lists known `LocalPrefsService` keys via reflection and supports any explicit key, String/Int/Float editing, saving and confirmed key deletion. Uses Unity's portable PlayerPrefs API; no unsupported enumeration of arbitrary OS preference stores. Editor validation remains. |
| SRDebugger | Replacement owned by the cleanup agent. | Replaced with a development-only game-owned diagnostic panel for settings, cheats, captured logs and local reports; see its evidence note. |
| UITKTimeline | Restore the public MIT UPM package, pinned to official HEAD. | Verified root package name `dev.krajnc.uitoolkittimeline` and commit `68e9114b94ba4a9123a4612d6fd6ce9fc98a7868`; root added the manifest dependency. No project C# API call sites use it; serialized Timeline playback remains to validate. |
| Odin | Restore the legitimately acquired local Asset Store package. | Restored 63 entries under ignored `Assets/Plugins/Sirenix`. Actual `Odin Inspector/Version.txt` reports 4.0.2.3. The acquired editor DLL contains the `Sirenix.OdinInspector.Editor.Validation` namespace; actual validator API compatibility still requires compilation. A fresh developer must own the appropriate license or replace the remaining inspector/serialization dependency. |
| Nice Vibrations | Install the current public MIT release, with the real mobile native libraries. | Downloaded Nice Vibrations 4.1.2 / Lofelt SDK 1.3.4 from the official release; source repository is archived. Restored 64 runtime/editor/native files, excluding demos, older versions and unused sample packs. Existing `HapticPatterns.PlayPreset` call sites use this actual implementation. All source and binaries remain locally ignored; the checksum-pinned restore needs no paid account. Haptic behavior has not been tested on devices. |
| VoxelBusters Essential Kit | Remove the client dependency. | Deleted NativeAuthenticationProvider and assembly references; the backend agent uses the existing unavailable native-login provider and retains free device/email authentication. Replaced RateMyApp with Unity's iOS request-review API and an Android store URL using `Application.identifier`. Store reviews need an actual published listing, but local play does not. No store authentication or review operation was run. |

The bundled localization is deliberately offline and game-owned: it implements every translation lookup/list surface used in this clone. It does not retain I2's unused Google live synchronization or impersonate the I2 namespace. All three migrated TMP components refresh on language change. Existing UI Toolkit elements retain their previous initialization-based lookup behavior.

## Local restore steps

The importer preserves package GUIDs and importer metadata, and checks `.gitignore` before writing each vendor entry. Acquire packages through the vendor/Asset Store under a valid license; do not copy the restored vendor paths into a public commit.

```sh
python3 Tools/Modernization/import_unitypackage.py "$HOME/Library/Unity/Asset Store-5.x/Kybernetik/ScriptingAnimation/Animancer Pro v8.unitypackage" --prefix Packages/com.kybernetik.animancer
python3 Tools/Modernization/import_unitypackage.py "$HOME/Library/Unity/Asset Store-5.x/Demigiant/Editor ExtensionsVisual Scripting/DOTween Pro.unitypackage" --prefix Assets/Plugins/Demigiant
python3 Tools/Modernization/import_unitypackage.py "$HOME/Library/Unity/Asset Store-5.x/Sirenix/Editor ExtensionsSystem/Odin Inspector and Serializer.unitypackage" --prefix Assets/Plugins/Sirenix
python3 Tools/Modernization/restore_public_plugins.py
```

The final command downloads checksum-pinned official DOTween core and Nice Vibrations ZIPs, restores them to ignored paths, and generates the local DOTween assembly definitions. Re-run it after a cached DOTween import so its current core wins. Root owns final assembly-reference mappings. Importing vendor source alone does not establish compile compatibility.

| Public archive | SHA-256 |
| --- | --- |
| DOTween_1_3_045.zip | `32eba5c48520a541775d00cb396e18975b83587ba79175dbb878d753725ba5d9` |
| nicevibrations-4.1.2.zip | `e2566f6ea47addff6f271fbdd0fe8200fe59c74c9d7975bf51bec264c7cbc8d0` |

## GitHub Actions

Deleted `Pipelines/`, `Scripts/`, the dead Unity Cloud Build manifest stub and pre-export hook, and the old postbuild Azure/Fastlane artifact-copy/Xcode modifications. `Backend/` remains untouched. Historical exported documentation is reference material, not executable build infrastructure.

- `tests.yml`: every pull request and manual dispatch; separate EditMode, PlayMode and Quantum-simulation jobs. Activation is part of the actual GameCI test action.
- `build.yml`: manual Android or iOS dispatch, and Android development APK on every tag. Builds bundled Addressables first. iOS exports an unsigned Xcode project; it does not produce a signed IPA or submit to a store.
- Actions are pinned to verified immutable Git commit SHAs. Both use Unity `6000.3.26f1`.
- Cache only public Unity registry package source under `Library/PackageCache/com.unity.*`. Do not place vendor restores, licensed SDKs, the rest of Library, local configuration or Unity licenses in public Actions caches.
- Download proprietary prerequisites from a legitimate private HTTPS bundle supplied by the repository owner. Verify SHA-256 before extraction; accept only ignored, untracked, repository-relative `BlastRoyale/` or `Quantum/` files. Reject symlinks, traversal, Backend content and tracked-source overwrites.
- Fail before Unity if activation secrets, the private restore bundle, client configuration, Quantum 3 SDK or migrated `Assets/QuantumUser/Simulation` source is missing. The legacy Quantum 2 solution is not a substitute for the intended Quantum 3 CI architecture.
- NUnit XML must exist, contain a real passed test and have no failures. Zero-test and all-skipped runs fail. The current `FirstLight.Tests.EditorMode.Systems` movement test is commented out: the Quantum-simulation job remains blocked until real simulation fixtures are ported. The model-level QuantumPropertiesTests are not mislabeled as simulation coverage.
- Fork pull requests do not receive repository secrets and fail the prerequisite check. Review them, then run a trusted branch manually. These workflows use `pull_request`, never `pull_request_target`.

`Builder.BatchBuild` now supports `-FLOutputPath`, uses the active mobile target, bundles Addressables by default and restores each modified debug-group inclusion flag even after an Addressables error. Android development builds use Unity's debug keystore; removed hardcoded store-signing paths/password placeholders. Store distribution is a separate human operation.

## Human setup for CI

1. Create your own GitHub repository/remote for the clone; do not push to upstream. Enable Actions.
2. Activate a free Personal license through Unity Hub if eligible, then follow [GameCI activation](https://game.ci/docs/github/activation/). Set repository Actions secrets `UNITY_LICENSE`, `UNITY_EMAIL`, `UNITY_PASSWORD`. The actions perform activation; supplying credentials is a human step. The workflow source requests no paid signing or license server; successful headless Personal activation must still be verified for the account/editor combination.
3. Complete the Quantum 3 migration and restore all remaining SDK/vendor/assets legitimately. Make a ZIP whose files use repository-relative paths; include only the ignored licensed prerequisite directories, never the repository's gameplay source, Backend files or credentials. Keep this ZIP private under an access grant that covers licensed developers and runners.
4. Set `CI_DEPENDENCIES_URL` to the actual private HTTPS download/signed URL and `CI_DEPENDENCIES_SHA256` to its real SHA-256. No storage provider, account, endpoint or credential is invented in this repository. Refresh an expired signed URL in the secret.
5. Set `CLIENT_CONFIG_JSON` to your filled `Assets/Resources/LocalClientConfig.json.template`. Do not put local configuration in the private vendor bundle or commit it.
6. Dispatch tests on a trusted branch; port and enable the real Quantum simulation tests, then require these checks for merging. Dispatch Android and inspect the APK artifact. Dispatch iOS only when an unsigned Xcode export is useful; a macOS/Xcode device or store signing step remains outside this workflow.

No extra GitHub token is necessary: checkout/artifact actions use the automatically supplied token with read-only repository permissions. No keystore, Google Play publishing key or Apple developer signing credential is required for development APKs and unsigned Xcode exports.

## Checks actually run

- `actionlint .github/workflows/*.yml`: **Pass**, exit 0.
- `python3 -m unittest discover -s Tools/CI/tests -v`: **Pass**, 8 tests. Tests cover ignored-file restore, complete archive validation before writing, traversal/Backend/symlink rejection, NUnit 2/3 success, missing/zero/all-skipped results and test failures. An initial temporary-path resolution failure was fixed by canonicalizing the restore root and the suite was rerun.
- `python3 -m py_compile Tools/CI/*.py Tools/Modernization/*.py`: **Pass**.
- `python3 Tools/CI/restore_dependencies.py` with no secrets: **Expected fail**, exit 1 with a precise missing-prerequisite message.
- All three actual cached package restores and both checksum-pinned public ZIP restores: **Pass**. `git check-ignore -v` confirms the restored runtime binaries/source remain ignored.
- Catalog parsing, all 2,962 accessor-to-term keys and the three prefab replacements: **Pass** as static checks. The 35 retained translations deliberately rewritten by the cleanup agent were preserved; export did not reintroduce removed terminology.
- GitHub remote execution, Unity compilation, runtime translation tests, Addressables/player builds, iOS export, native review and device haptics: **Not run by this agent**. Root's report must distinguish any later Editor checks from this evidence.

## Primary sources checked live

- [UITTimeline source and package metadata](https://github.com/mihakrajnc/UITTimeline/tree/68e9114b94ba4a9123a4612d6fd6ce9fc98a7868).
- [Nice Vibrations public release](https://github.com/Lofelt/NiceVibrations/releases/tag/sdk-1.3.4-nicevibrations-4.1.2) and [MIT source license](https://github.com/Lofelt/NiceVibrations/blob/5e5866386ffd51a938200299cd5d0a4851dbaa0c/LICENSE.md).
- [DOTween current vendor download and setup requirements](https://dotween.demigiant.com/download.php).
- [Unity iOS request-review API](https://docs.unity3d.com/ScriptReference/iOS.Device.RequestStoreReview.html).
- [Odin patch notes](https://odininspector.com/patch-notes); local installation version comes from the acquired package's actual Version.txt.
- [I2 product](https://inter-illusion.com/tools/i2-localization) and [SRDebugger product](https://stompyrobot.uk/tools/srdebugger/) checked before choosing free game-owned replacements.
- [GameCI test runner](https://game.ci/docs/github/test-runner/) and [builder](https://game.ci/docs/github/builder/) configuration and activation.
