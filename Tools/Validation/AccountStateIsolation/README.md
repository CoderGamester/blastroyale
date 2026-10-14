# Local account-state regression

Four native Unity 6000.3.26f1 EditMode cases exercise the real compiled `ClientDataService`, `PlayerStoreLogic`, `PlayerLogic`, `AuthService` model-replacement method, authentication hooks, serializers, SDK command contracts and maintained observables. They check account A's exhausted purchase policy does not carry into account B; observers survive the real backing-list rebind; and both an existing different account and a new guest retain their own tutorial state.

The two tutorial cases reproduce the actual local login ordering: `BeforeAuthentication`, replacement of all twelve serialized models and logic initialization/rebinding, then `AfterFetchedState`. Small game-only orchestration collaborators route through actual game logic and commands. Authentication/network requests are not run by this fixture. No UGS, Unity, observable, SDK or Frame type is redefined.

After restoring/importing the real SDK and dependencies, stage the native imported Main game assembly:

```sh
python3 Tools/Validation/AccountStateIsolation/prepare_account_state_project.py
```

To validate the reviewed overlay before Main integration, pass its whole-game compiler output with `--game-assembly .local/runtime-account-compile/FirstLight.Game.dll`. Run the isolated project with Unity `-batchmode -runTests -testPlatform EditMode -testFilter FirstLight.Tests.EditorMode.Authentication.AccountStateIsolationTests`, specifying `-projectPath "$PWD/.local/runtime-account-project"`, `-testResults` and `-logFile` paths.

This fixture validates local model/account separation. The separate `UgsAuthenticationLifecycle` fixture executes real native auth state, scheduled refresh and provider logout policy with local synthetic network collaborators. Neither fixture performs cloud requests. Parent-owned Main boot, live cloud persistence and match tests remain separate evidence.

A fifth case, `ExpiredAccountSwitchTests`, executes actual `AuthService.LoginProcess` against the native Auth3.8 scheduled expiry state. It cancels an account switch, verifies the login gate releases, then calls the actual cached Unity provider through a subsequent login attempt. The test deliberately stops before backend model loading. Its SDK network/cache/scheduler collaborators come from `UgsAuthenticationLifecycle`; imported SDK state and game types remain unchanged. Run the complete namespace filter `FirstLight.Tests.EditorMode.Authentication` to include all five cases. The test assembly uses the real SDK's existing `Unity.Services.Authentication.Tests` friend identity. Current native staging contains 89 real assemblies after the retired Purchasing dependencies were proved absent from surviving native references.

Public positive and prior-source negative evidence is recorded in `Docs/Modernization/verification/runtime-account-expired-switch-summary.json`. No cloud request or full hosted-login claim is made.
