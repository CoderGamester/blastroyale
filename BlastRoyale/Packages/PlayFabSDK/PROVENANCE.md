# PlayFab data-contract compatibility subset

Source: https://github.com/PlayFab/UnitySDK/tree/04e73b3a78ce1afbba9f3e003f3fe51ba77bd8bd/ExampleTestProject/Assets/PlayFabSDK

Upstream build `adobuild_unitysdk_167`, SDK `2.244.260922`, Apache-2.0. The original license, assembly name and retained source-file metadata are preserved. This package is a local source subset rather than the full upstream service SDK.

The game authenticates and persists through Unity Services and uses Photon rooms. It still exposes original PlayFab DTO names at its legacy login, catalog, profile, leaderboard and local command-result boundaries. No PlayFab title, service session, endpoint API, request factory, HTTP implementation, shared settings asset or Editor integration is retained. The server SDK binary is unchanged and has no PlayFab assembly dependency.

The subset contains 14 original source files with 99 top-level types. A native assembly dependency walk started from actual imported game/test references, then retained complete source declarations and their field/base/member dependencies. DTO fields were not replaced, renamed or fabricated. The native JSON serializer and its supporting interfaces/utilities remain original source. The unused private transport factory and its unused interface were removed; requesting the transport plugin now throws a descriptive unsupported-operation error. Existing serializer-plugin registration and `ToJson` behavior remain intact.

Eleven unused Azure error enum symbols were removed only after checking they all had explicit numeric values and no owned caller. Every retained enum integer is unchanged. Obsolete provider references had already been removed in the previous local source reduction; surviving source contains no Azure, Facebook or Firebase reference. Function-result DTO names remain solely as data envelopes for local commands; their obsolete service-specific comments were updated.

Verification: 99 retained top-level types preserve 1,768 field signatures and 177 public/protected method signatures. All 15 retained enum schemas keep their numeric values. Four native original-versus-subset serialization and roundtrip comparisons pass. The subset plus current whole game, client server-plugin assembly, Editor, EditMode test source and PlayMode test source compile against actual Unity/SDK/package assemblies with zero C# warnings/errors. This is isolated source/serialization evidence; Main Editor import, tests and player builds after promotion are separate checks.

Evidence: `Docs/Modernization/playfab-dto-compatibility.md` and `Docs/Modernization/verification/playfab-dto-subset-*.json`. Repeat the source closure, complete-type extraction, enum integer checks and native compilation when changing the compatibility boundary. Do not restore the full upstream endpoint SDK as a routine update.
