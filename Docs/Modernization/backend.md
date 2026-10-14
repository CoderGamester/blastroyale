# Free development backend

The operational backend is **Unity Gaming Services only**, with Photon Quantum for deterministic rooms. The user authorized this migration; no PlayFab title or Azure deployment is required. Existing public PlayFab DTOs remain only where unchanged server-SDK contracts require them. No operational PlayFab API/settings calls remain in the project-owned client.

[UGS implementation and authority limits](ugs-backend.md) is the detailed backend contract. It covers cached guest authentication, optional hosted account linking/recovery, twelve private progression models, Public profiles, Friends, Lobby, bundled/hosted config and the earned-currency development shop. Unity Economy does not accept new projects; trusted grants and real-money receipt validation remain future server work at unchanged `Backend/`.

Actual Main target-Editor acceptance passes **310 EditMode cases**, including five native SDK account/queue scheduling regressions and nine native serialized Photon party cases. An isolated actual Authentication3.8 lifecycle fixture also passes5/5, and its two expired-token cases fail before the reviewed fix. [Native tests](verification/ugs-main-tests-summary.json), [auth lifecycle proof](verification/ugs-auth-lifecycle-summary.json).

Live development-only UGS acceptance passes native guest authentication, twelve-model save/load, Public profile readback, real stale-write-lock rejection and a separate Editor restart with cached identity/model recovery. The probe uses its own native Authentication profile and never the game's default guest profile. [Live proof](verification/ugs-live-persistence.json), [restart](verification/ugs-live-persistence-restart.json).

Clean Boot-to-match remains under validation after integration: a legacy Canvas/empty Toolkit document failure is being repaired, and FtueDeck first-login scene/prototypes are being restored. Hosted linking, live account switch/deletion, two-client Friends/Lobby, two-device conflicts and device behavior are not run. Pure/native scheduling checks do not replace those cloud/device scenarios.

The actual user-supplied project and newly created development environment are configured only locally. Native player/dashboard project matching is verified. The credentials file's incomplete environment ID was replaced only in ignored local config with the exact observed development ID; no IDs are published. [Native binding result](verification/unity-cloud-project-link.json), [human setup](../../HUMAN_ACTIONS.md).
