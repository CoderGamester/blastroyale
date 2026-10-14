# Quantum migration contract and blocker

## Verified target and alternative

On 2026-10-14 Photon lists Quantum **3.0.14 Stable, build 2241**, released 2026-10-05, as the current stable SDK and migration package. Quantum 3.1 is a preview. The maintained 2.x download is **2.1.10**, newer than the repository's 2.1.9 build 1529. [3.x download](https://doc.photonengine.com/quantum/v3/getting-started/initial-setup), [2.x download](https://doc.photonengine.com/quantum/v2/getting-started/initial-setup), [migration guide](https://doc.photonengine.com/quantum/v3/getting-started/migration-guide).

Choose 3.0.14 for the long-term project. Retaining/updating 2.1.10 would reduce immediate API and serialized-asset changes but keep the old .NET Framework/Mono codegen/export pipeline and defer the same major migration. The current macOS attempt failed on missing .NET Framework 4.8 reference assemblies. Rough planning estimates, not measured completion times: restoring and validating 2.1.10 is 2-5 developer days; full 3.x source/assets/Bot SDK migration and device validation is 1-3 developer weeks given this project's missing assets and existing custom integration. Actual scope must be reassessed after SDK restoration.

## Current state

**Blocked, not migrated.** Neither Quantum 2 SDK, Quantum 3 SDK nor migration ZIP exists in the supplied Downloads location or the bounded Desktop search. The 2.1.7 top-down sample is present and was not used as a substitute SDK. Opening the official 3.0.14 download in the browser returned a sign-in-required 403. The user has a Photon 3 App ID, copied only to ignored local configuration.

The original simulation under `Quantum/quantum_code/quantum.code` is preserved as the migration input. It has not been copied into a new assembly without running the official serializer migration, nor relabeled as Quantum 3. Existing test assembly references still describe Quantum 2; CI explicitly rejects a missing Quantum 3 simulation and refuses to accept zero simulation tests. Do not connect a Quantum 3 App ID with the still-Quantum-2 runtime.

## Intended source/build ownership

Use Quantum 3's maintained Unity assembly workflow: `Assets/QuantumUser/Simulation` as the authoritative custom simulation source, `Quantum.Simulation` as its assembly, SDK libraries privately restored below `Assets/Photon`, and SDK code generation in the Editor. Generated wrappers live below `QuantumUser/View/Generated` and Editor code generation below `QuantumUser/Editor`. Keep a standalone .NET project only as a companion to the migrated assembly/source, if needed for headless deterministic tests. Do not keep two diverging authoritative simulation copies or import a duplicate simulation DLL into Unity. The old split `quantum.code.sln` is a migration input, not the final Quantum 3 workflow. [Project structure](https://doc.photonengine.com/quantum/v3/manual/quantum-project).

## Required migration sequence after the human sign-in/download

1. Restore/update the 2.x SDK to 2.1.10 using legitimately acquired files. The public Git source stays clean; SDK source/binaries must remain ignored. Keep all original gameplay/asset GUIDs.
2. Open the existing project in the requested 6000.3.26f1 editor, resolve the baseline SDK compile, and import the migration preparation package from the official ZIP.
3. Use the preparation tool to add migration defines, export the Quantum asset database, and remove the old Photon integration. Preserve the export in an ignored recovery folder.
4. Import the official 3.0.14 SDK and its migration package. Import the original simulation project, run initial code generation, then resolve actual SDK/API compilation diagnostics. Use the matching Quantum 3 Bot SDK migration instructions for the committed custom bot source/assets.
5. Run the SDK's asset script checks, transfer legacy asset GUIDs, and upgrade asset objects. Reconfigure the Photon 3 App ID from ignored client config. Replace old `PhotonQuantum`/`PhotonRealtime` assembly references with the installed `Quantum.Unity`, `Quantum.Simulation` and actual Realtime assembly names.
6. Migrate the existing simulation fixtures (the old MovementSystemTest is entirely commented out) to real runnable deterministic tests. Test movement, command serialization, bots and match end rewards; compare repeatable input replays/checksums. A zero-test result must remain a failure.
7. Verify clean Editor compilation/import, Addressables build, Boot-to-match, two-client rooms, bot behavior, deterministic tests and at least one mobile development build. Only then retire the old split source/build helper and mark this section Done.

SDK source/asset migration menus require the real licensed SDK; invented stand-in DLLs, renaming missing type names blindly, or editing asset GUIDs without the preparation export cannot supply equivalent evidence. `Backend/` and the unavailable enterprise Quantum server are unchanged references.

## Evidence

- Official current release/download, compatibility and migration documentation checked.
- Bounded archive searches found only the 2.1.7 sample.
- `dotnet build Quantum/quantum_code/quantum.code/quantum.code.csproj --configuration Release`: Fail, MSB3644 (.NET Framework 4.8 references absent), log `artifacts/quantum-baseline-build.log`.
- Two real Unity 6000.3.26f1 batch imports attempted; both returned compiler failure. First import revealed duplicate assembly references, then source diagnostics showed missing Quantum types plus independent upgrade issues. Those independent issues are being repaired; this is not a passing SDK migration.
- No simulation or playable match was run.
