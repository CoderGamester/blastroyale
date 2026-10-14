# PlayFab Unity SDK source

Source: https://github.com/PlayFab/UnitySDK/tree/04e73b3a78ce1afbba9f3e003f3fe51ba77bd8bd/ExampleTestProject/Assets/PlayFabSDK

Upstream build identifier: `adobuild_unitysdk_167`, SDK `2.244.260922`. Apache-2.0 source and original `.meta` files were copied from this exact commit. No proprietary binary or Asset Store package is included.

Local reductions and Unity 6 fixes:

- Omit the sample Resources/PlayFabSharedSettings asset and directory metadata; the game owns one blank settings asset in Assets/Resources.
- Remove the unused Facebook/Facebook Instant Games endpoints, DTOs, fields, event dispatch branches and provider/error enum symbols. No game caller used these surfaces. A Roslyn syntax-tree rewrite removed 227 members/branches. Every surviving numeric value in the 16 affected enums was checked against the original source and is explicit, so provider/error wire IDs do not shift.
- Omit the legacy Matchmaker folder: its upstream API methods are marked `Obsolete("No longer available", true)`, none is called by the game, and the dead event declarations cause eight unused-event compiler warnings. The game uses Photon rooms and Unity Lobby for matchmaking.
- Replace the SDK singleton search with Unity 6 `FindFirstObjectByType<T>()`, preserving the existing first-object behavior while removing the deprecated API.
- Change one generic documentation adjective from “singular” to “single” to keep automated removed-service scans unambiguous.
- Add the local UPM wrapper (`package.json`), this provenance file and `LICENSE.md`.

Validation: syntax parse succeeded. A standalone .NET Standard 2.1 compilation against the installed Unity 6000.3.26f1 managed assemblies enabled the Client, Admin, Server, Entity, Authentication, Addon, Economy, Multiplayer and Progression source paths. It is a source compilation check, not an Editor import, network test or player build.

When upgrading the SDK, repeat these reductions from upstream source and re-check the numeric enums, game call sites and Unity compilation. Do not replace this locally reduced package with an unreviewed copy from upstream.
