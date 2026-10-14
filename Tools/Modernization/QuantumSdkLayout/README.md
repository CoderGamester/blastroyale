# Quantum SDK source layout repair

This authored tool relocates selected declarations from the user's legitimate Photon Quantum 3.0.14 Stable 2241 SDK. It contains no vendor implementation or replacement types. Real Unity 6000.3.26f1 import is the warning acceptance check; compiler parity alone is insufficient.

The remaining six warnings had a verified cause: Unity's installed MonoScript source generator keys a declaration by namespace and identifier, omitting generic arity. It marks duplicate generic/non-generic names as partial, even when neither declaration uses `partial`. Three actual Unity object pairs were both declared in `QuantumUnityRuntime.cs`: `QuantumUnityComponentPrototype`, `QuantumEntityViewComponent`, and `QuantumSceneViewComponent`, each alongside its generic form. Moving only the non-generic component prototype reduced native warnings from six to four per import. Moving all three non-generic declarations reduced them to zero at startup and explicit synchronous forced import. Their actual MonoScripts now resolve the intended classes from canonical filenames.

The complete Cecil/Roslyn/native audit found 170 Unity object-derived SDK types, with no assembly-resolution failures. Ten declarations still lived in the phase 4 merged source and all ten were nonpartial. The native generated metadata proves the three duplicate pairs above; the warning text did not describe their C# modifiers accurately. Evidence is in `Docs/Modernization/verification/quantum-sdk-warning-causality.json`, the full object inventory and the before/after generated type maps.

## Fresh legitimate SDK installation

Install the SDK first, close Unity for this project, and use .NET SDK 10. Apply **only phases 1, 2 and 5**, in that order:

```sh
dotnet run --project Tools/Modernization/QuantumSdkLayout/QuantumSdkLayout.csproj -- \
  --project BlastRoyale --manifest Tools/Modernization/QuantumSdkLayout/manifest.json --apply

dotnet run --project Tools/Modernization/QuantumSdkLayout/QuantumSdkLayout.csproj -- \
  --project BlastRoyale --manifest Tools/Modernization/QuantumSdkLayout/manifest-phase2.json --apply

dotnet run --project Tools/Modernization/QuantumSdkLayout/QuantumSdkLayout.csproj -- \
  --project BlastRoyale --manifest Tools/Modernization/QuantumSdkLayout/manifest-phase5.json --apply
```

Omit `--apply` to check the exact inputs and reviewed output hashes. After the final patch, its manifest provides the idempotent verification. Intermediate manifests describe superseded source hashes and are not final-state checks. A different SDK revision or modified source fails explicitly; review that revision instead of bypassing the guard. `--record-output-hashes` is an audit-authoring option.

Phase 1 consolidates eleven real partial types into canonical declarations, retaining nine existing metadata files and adding two private abstract-base source GUIDs. Phase 2 consolidates `QuantumMenuSceneInfo` and generic `QuantumGlobalScriptableObject<T>`; native warning count fell from nine to six. Phase 5 moves only the three non-generic declarations identified above. Every class body, attribute, namespace import and conditional member remains the SDK implementation. Phase 5 explicitly guards the original absence of the `partial` modifier and retains conditional `UnityEditor` imports for player compilation.

## Recover a checkout that already applied phases 3 and 4

Close Unity for this project. The exact recovery sequence is:

```sh
dotnet run --project Tools/Modernization/QuantumSdkLayout/QuantumSdkLayout.csproj -- \
  --project BlastRoyale --manifest Tools/Modernization/QuantumSdkLayout/manifest-phase4.json --rollback

dotnet run --project Tools/Modernization/QuantumSdkLayout/QuantumSdkLayout.csproj -- \
  --project BlastRoyale --manifest Tools/Modernization/QuantumSdkLayout/manifest-phase3.json --rollback

dotnet run --project Tools/Modernization/QuantumSdkLayout/QuantumSdkLayout.csproj -- \
  --project BlastRoyale --manifest Tools/Modernization/QuantumSdkLayout/manifest-phase5.json --apply
```

Rollback verifies every applied source/metadata hash and every original backup before writing. It saves the exact retired layout under the existing ignored backup's `retired-layout` directory, restores the original merged source, and removes only the seven recorded new phase 3/4 files and metadata. Existing canonical metadata remains unchanged. It refuses to overwrite recovery material. This sequence was rehearsed independently against a copy of the current phase 4 SDK and produced byte-identical phase 5 sources and metadata. The integration handoff records exact preconditions and commands.

## Acceptance and retained negative evidence

Phase 5 Editor and macOS player source compilation both pass before and after with warnings treated as errors. Full API, attributes, generic constraints and method IL match for 524 Editor types/2,416 methods and 502 player types/2,231 methods; declared field order also matches for all three relocated types. The parity compiler excludes Unity's generated file-mapping metadata, which is the intended layout change; the real native import separately runs Unity's generators and validates the new mappings. The Editor parity response removes the retired, unused DOTweenUpgradeManager reference from the earlier captured response. Public compile summaries and parity reports retain the distinction.

Phases 3 and 4 were negative probes: six plain-object declarations and seven `QuantumGameGizmos` declarations were moved with successful API/IL parity, but native warnings stayed at six. Their manifests and parity evidence remain for audit and guarded recovery; they are **not installation dependencies**. Further isolated worker, callback and runner relocation probes also produced no warning reduction and were not promoted.

The final native causality probe used the owned full-client clone. It retained two unrelated old BotSDK `AIAction` warnings already resolved independently in Main. Its evidence establishes the six merged SDK warnings and their resolution. Root performs complete Main restart acceptance after guarded integration. No warnings were suppressed, no SDK APIs were replaced, and no Main source was changed by these isolated probes.
