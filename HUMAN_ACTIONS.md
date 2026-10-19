# Human actions

On this host, authorized Photon/Unity project and development-environment configuration passes native binding, live guest/twelve-model/Public-profile/restart/conflict checks and actual Boot-to-tutorial/normal Maze gameplay. The following steps are for a fresh developer; Editor movement, attacks and repeated result/Home/match flow also pass. There is no remaining credential blocker for local guest play. No actual ID or credential is published. The operational backend is **UGS only plus Photon**: no PlayFab title, Azure deployment or custom server is needed.

## Required fresh-machine setup

1. Install **Unity 6000.3.26f1** with Android/iOS support through Unity Hub. Use an eligible free development license. This host already has the exact Editor/modules; do not install another version for this task.
2. Sign in to your own eligible [Photon account](https://dashboard.photonengine.com/) and legitimately obtain **Quantum 3.0.14 build 2241** plus **Bot SDK 3.10.2**. Follow [the exact restore/codegen/GUID/source-layout order](Docs/Modernization/quantum.md), applying final phases **1, 2 and 5**. Proprietary SDK implementation/binaries remain ignored. Enterprise Quantum server access is unnecessary for this client development path.
3. With Unity closed, run `python3 Tools/Modernization/restore_public_plugins.py` from the repository root. It restores checksum-pinned public DOTween core and MIT NiceVibrations/native runtime. Paid Animancer, DOTween Pro, Odin, Bakery, I2, SRDebugger and EssentialKit are not prerequisites. [Missing-asset history/current replacements](DELETED-ASSETS.md).
4. Create/select one free [Unity Cloud project](https://cloud.unity.com/) and a **development** environment. In Editor > Project Settings > Services, link that exact project. The native SDK uses `Application.cloudProjectId`; local JSON alone does not link the Editor.
5. Copy `BlastRoyale/Assets/Resources/LocalClientConfig.json.template` to the ignored `LocalClientConfig.json` only if absent. Fill actual `UnityProjectId`, `UnityEnvironmentName` and `PhotonQuantumAppId`; optional `UnityEnvironmentId` must be the exact environment ID if used. Keep `UseBundledRemoteConfig=true` and `UseBundledShop=true` for initial play. Hosted Remote Config requires its actual environment configuration; no made-up ID is a substitute.
6. Enable Authentication, Cloud Save, Friends and Lobby/Multiplayer in that Unity project. The selected development flow uses the services' published free allowances, without a paid hosting deployment. Monitor usage before exceeding free limits. [Current decision/capacity](DECISIONS.md).
7. For friend name lookup, Cloud Save > Player Data > Indexes: create an ascending string index for `player_name` in **Public** access. Sign in to publish the searchable record. [Official index/query guide](https://docs.unity.com/en-us/cloud-save/concepts/queries).
8. Create a free **Quantum 3** application in [Photon Dashboard](https://dashboard.photonengine.com/app/create), using ordinary Photon authentication, and configure its actual App ID. The temporary application used on this host was explicitly authorized; a fresh developer needs an application they are authorized to use.

Keep real client/account IDs out of Git, including locally linked ProjectSettings account fields. The build helper configures/validates the real native Unity project ID from ignored local config. Resources ships in the player and cannot protect backend keys or service-account credentials; those never belong in this JSON. One Unity process owns a project at a time. After initial UPM resolution, close Unity and apply the two exact pinned package repairs listed in README step 7 before testing/building; CI applies both automatically.

## Optional recoverable accounts

Guest login is sufficient for first play. To use hosted Unity Player Accounts, enable that provider and create genuine `Assets/Resources/UnityPlayerAccountSettings.asset` through Project Settings > Services > Player Accounts. Configure the actual Client ID in that ignored SDK-owned asset; the SDK does not read an invented Client ID field from LocalClientConfig.json.

Add the PC platform with desktop redirect `http://localhost/callback`, and authorize the native mobile redirect `unitydl://com.unityplayeraccounts.<UnityProjectId>` in the hosted configuration. Linking keeps the guest identity/models. Explicit hosted logout clears eligible linked credentials and reaches account choice; unlinked guests keep their sole recovery credential. Native lifecycle fixtures pass; actual browser linking/recovery and device redirects are not run. [Native API/configuration details](Docs/Modernization/ugs-backend.md).

## GitHub Actions, when an owned remote is available

Configure repository secrets `UNITY_LICENSE`, `UNITY_EMAIL`, `UNITY_PASSWORD`, `CI_DEPENDENCIES_URL`, `CI_DEPENDENCIES_SHA256` and `CLIENT_CONFIG_JSON` as described in [CI setup](Docs/Modernization/ci-plugins.md). Confirm actual account/headless-license eligibility; no remote activation was performed here. The restricted archive must contain legitimately acquired ignored prerequisites and no server/account keys. Keep client config separate from SDK archives.

Fork PRs lack secrets and fail clearly; a maintainer may review and dispatch a trusted branch. No upstream push or remote Actions execution occurred. Android development uses Unity's debug keystore. iOS exports an unsigned Xcode project; paid distribution signing is not required for the development export.

## Before distribution or broader acceptance

- Run on physical Android/iOS devices; the current native Editor game and unsigned iOS export do not prove device controls, haptics, notifications, performance or store review.
- Complete hosted browser/account deletion and two-client Friends/Lobby scenarios if those features are used.
- Keep cash purchases disabled until genuine receipt validation/trusted grants exist. Global ranking is unavailable; progression/rewards remain client authoritative.
- Replace marked development legal/privacy text and visual unit-box approximations when appropriate. The original commercial art was not acquired or reconstructed.
