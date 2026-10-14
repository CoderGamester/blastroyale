# PlayFab data-contract reduction

The operational client uses Unity Authentication, Cloud Save, Friends, Lobby and Remote Config, with Photon for rooms and simulation. It requires no PlayFab title or service transport. The old game-facing contracts still name original PlayFab login, catalog, profile, news, leaderboard and function-result DTOs. Those names do not create a PlayFab account or network request.

An actual Cecil walk of imported Main assemblies found 19 direct game type roots and nine test roots. The unchanged `FirstLightServerSDK.dll` references seven other assemblies and no PlayFab assembly. Keeping complete original DTO declarations and native serializer support gives 99 top-level source types in 14 original Apache-2.0 files. Retained class fields are neither fabricated nor partially removed.

The staged reduction removes all generated endpoint API classes, authentication/request pipelines, HTTP implementations, automatic transport factory, shared settings asset and PlayFab Editor UI. It retires 191 files including metadata and the now-unused Resources settings asset pair. One test source loses an unused import from the deleted multiplayer endpoint namespace. Existing serializer-plugin registration remains functional; asking it to construct a transport reports that this service path has been retired.

Eleven unused Azure error enum names have explicit numeric values and no owned caller. They are removed without shifting surviving values. Obsolete service wording in the two retained function-result envelopes is updated. The resulting package C# contains zero Azure, Facebook and Firebase references. PlayFab DTO namespace/type names remain as the documented compatibility boundary.

Isolated verification passed:

- 1,768 retained field signatures and 177 public/protected method signatures match the original native assembly across all 99 retained top-level types.
- Fifteen enum schemas preserve every retained integer, including 940 error values.
- Four original-versus-subset native `ToJson` comparisons and native deserialization roundtrips match for login/profile, catalog/bundle/prices, store/marketing and local function-result envelopes.
- The subset, whole game, client server-plugin assembly, Editor, EditMode test source and PlayMode test source compile against actual installed Unity/SDK/package assemblies with zero C# warnings/errors.

Main import, Main tests and player builds after guarded promotion belong to the integrator. No new cloud request, live backend migration or hosted-account result is claimed here. Public machine-readable evidence is in [the summary](verification/playfab-dto-subset-summary.json), [schema parity](verification/playfab-dto-subset-abi.json), [native serializer parity](verification/playfab-dto-subset-serializer.json) and [retained source inventory](verification/playfab-dto-subset-source.json).

The implementation was prepared in `.local/playfab-dto-subset`, with before/after hashes in `patch-manifest.json` and original hashes in `deletions.json`. Extraction and parity use actual Cecil and Roslyn from Unity 6000.3.26f1 in `.local/playfab-closure-audit`; serialization uses separate real-assembly load contexts, without endpoint calls or substitute SDK types. The compiler response files and detailed native logs remain in the local verification artifacts.
