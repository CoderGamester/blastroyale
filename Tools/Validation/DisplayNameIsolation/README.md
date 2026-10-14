# Native delayed name response regression

These two EditMode cases run the actual game `AuthService.SetDisplayName` with imported Authentication 3.8 state, generated name API contracts, player-name component and game message broker. A controlled implementation of the native `IPlayerNamesApi` delays account A's successful response until either a different account B or the same player ID has signed in again and the game loaded-name hook has reloaded its profile. No SDK type is redefined and no cloud request is made.

The fixed code keeps the awaited result local, checks the account before Public Cloud Save, and checks again before changing the game cached name or publishing its notification. Both tests pass; game source compiles with zero warnings/errors. [Exact proof](../../../Docs/Modernization/verification/display-name-isolation-summary.json).

```sh
python3 Tools/Validation/DisplayNameIsolation/prepare_display_name_project.py --game-assembly BlastRoyale/Library/ScriptAssemblies/FirstLight.Game.dll
/Applications/Unity/Hub/Editor/6000.3.26f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -projectPath "$PWD/.local/display-name-project" -runTests -testPlatform EditMode -testFilter FirstLight.Tests.EditorMode.Authentication.DisplayNameIsolationTests -testResults "$PWD/artifacts/display-name-tests.xml" -logFile "$PWD/artifacts/display-name-tests.log"
```

Restore/import legitimate dependencies first; close the probe's Editor before another batchmode process. Its test assembly uses Authentication's existing native test-friend identity. The preparer extracts the shared real native authentication fixture and supplies the actual generated name API implementation; all identities are synthetic.

The stale response is rejected before a Cloud Save call, so these cases prove game cache/notification isolation, not live cloud behavior. Authentication 3.8 itself writes its own `PlayerNameComponent` after its API await without an account-generation check. This bounded game patch does not alter that vendor-owned cache; later genuine name loading replaces it. The minimal probe also reports imported-assembly updater resolution warnings for Newtonsoft; those are distinct from the zero C# diagnostics and passing test results.
