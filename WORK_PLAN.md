# Blast Royale modernization plan

Objective: preserve the existing game and bring the public clone to a free-account setup on Unity 6000.3.26f1 and stable Quantum 3, with separate phase commits and evidence-based status. Backend/ is an immutable reference. No upstream pushes.

## Implementation and ownership

Recommended implementation model: current Codex session (model/effort identity not exposed). Shared-context implementation with one integrator and bounded parallel ownership suits the interdependent Unity, backend, and plugin changes.

1. Integrator: repository baseline, Unity target/packages, Quantum migration, GameLovers migration, missing assets, root documentation and phase commits.
2. Backend agent: backend choice, connected client adapters, auth/data/economy/config/rooms, FLEnvironment and local configuration.
3. Cleanup agent: web3 and removed telemetry/social/development SDKs, callers and assets.
4. CI/plugin agent: GitHub Actions, obsolete pipeline removal and vendor dependency restoration/replacement.

## Acceptance

- Establish local prerequisite inventory and protect every licensed restore and credential before import.
- Verify stable SDK and editor recommendations using official upstream sources; pin reproducible dependencies.
- Compile existing simulation and tests; preserve deterministic behavior and serialization contracts during major migration.
- Upgrade APIs against actual SDK/package source, not guessed compatibility stubs.
- Serialize Unity runs; exact target editor import, clean compilation, EditMode/PlayMode tests, Addressables build and playable match are separate acceptance gates.
- Audit GUIDs and prohibited client references, leave Backend byte-for-byte unchanged.
- Run actionlint and runnable local tests; record unavailable CI/device checks explicitly.
- Review integration changes independently, resolve actionable findings, finish DECISIONS/HUMAN_ACTIONS/REPORT/README.

## Initial facts verified locally

2026-10-14: clean develop baseline; upgrade/codex created. Requested editor 6000.3.26f1 is absent; 6000.3.13f1 and 6000.3.23f1 exist. An unrelated Ignite project is open in 6000.3.13f1. Downloads contains only the 2.1.7 sample, not the specified SDK/migration archives. Credentials file exists and has a Quantum 3 App ID; no values are recorded here. Local Frameworks HEAD is aa70389bd6a0e8080b83b68c2fb617e74bba1fd0.

## Dependencies and unresolved gates

Quantum 3 SDK/migration restoration must precede final compile/API/serialization validation. Exact editor installation must precede target editor acceptance. Missing proprietary packages must either be restored locally or replaced with complete call-site ports. Backend choice must reflect current free-tier limits and must connect to the original game bootstrap. Remote test/build execution needs a user-owned GitHub repository and Unity licensing secrets.
