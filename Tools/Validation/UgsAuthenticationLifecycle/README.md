# Native Authentication lifecycle regression

These five EditMode cases exercise the **imported Authentication3.8 SDK** `AuthenticationServiceInternal`, its native components/JwtDecoder/state/events, and the exact application `UnityCloudLoginProvider`/`UgsAccountSession` source, compiled in the isolated fixture.

Only network, local cache, scheduler and telemetry are explicit test collaborators implementing the imported SDK interfaces. They use synthetic local data and perform no cloud writes. No SDK types, Unity engine types or Quantum Frame types are redefined. The actual SDK TargetingComponent reads native Unity application properties, so these cases must execute in Unity, not a standalone .NET runner.

The test assembly name is `Unity.Services.Authentication.Tests`, which the installed SDK already permits through its native `InternalsVisibleTo` declaration. No SDK source or binaries are patched to grant access. The dedicated ignored project isolates this assembly from any application test assemblies using the same name. Each case restores the SDK singleton and invalidates its temporary account capture during teardown.

Coverage:

- Expired cached device login performs native cached-session sign-in and permits a new state-load capture.
- Expired cached guest retry does the same.
- An already authorized/Refreshing provider avoids a redundant sign-in; the actual SDK scheduled refresh preserves generation.
- Signed-out cached login restores the same real SDK ID and invalidates prior work.
- A fresh signed-out guest makes exactly one native anonymous request.

The expired cases assert the native distinction: `IsSignedIn == true`, `IsExpired == true`, `IsAuthorized == false`. The expiry correction uses `!IsAuthorized`; `IsSignedIn` also includes Expired in the actual SDK. The connected AuthService still routes guest/device calls through its existing full LoginProcess, which fetches all player state before initializing game logic. The fixture does not mock that full game initialization or claim live Cloud Save/recovery/device acceptance.

Local invocation after restoring/importing the native project:

```sh
python3 Tools/Validation/UgsAuthenticationLifecycle/prepare_lifecycle_project.py
/Applications/Unity/Hub/Editor/6000.3.26f1/Unity.app/Contents/MacOS/Unity \
  -batchmode -nographics \
  -projectPath "$PWD/.local/ugs-auth-lifecycle-project" \
  -runTests -testPlatform EditMode \
  -testFilter FirstLight.Tests.EditorMode.Authentication.UgsAuthenticationLifecycleTests \
  -testResults "$PWD/artifacts/ugs-auth-lifecycle-tests.xml" \
  -logFile "$PWD/artifacts/ugs-auth-lifecycle-tests.log"
```

The ignored probe contains the two C# files in Assets/Tests, an Editor test asmdef with native test-friend name, native Test Framework1.6.0/Newtonsoft3.2.2, and actual already-imported managed dependencies, exact provider/guard source and the two exact application interfaces extracted from AuthService.cs. It contains no credential config. Main project mutation/Unity execution remains with the root agent.

The suite now includes deliberate recoverable-account logout versus guest/default sign-out, plus actual SDK scheduled expiry rejecting new cloud snapshots. Scheduled normal refresh continues to preserve a valid same-ID snapshot. The policy and authorization guard have separate guarded manifests and native evidence.
