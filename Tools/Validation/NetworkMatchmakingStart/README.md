# Native matchmaking dispatch regression

The actual maintained Statechart commits its current state after transition actions run. Immediately completed bundled configuration/reservation can therefore publish a graph event during a choice action and reenter that choice. These five native PlayMode tests exercise the imported game NetworkState and the real Statechart, MessageBroker, Authentication 3.8 state and Photon RealtimeClient. NSubstitute is confined to game-owned orchestration interfaces; no SDK/Frame type is substituted. They make no socket/cloud request.

With Main closed/imported and genuine dependencies present:

```sh
python3 Tools/Validation/NetworkMatchmakingStart/prepare_network_start_project.py --game-assembly BlastRoyale/Library/ScriptAssemblies/FirstLight.Game.dll
/Applications/Unity/Hub/Editor/6000.3.26f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -projectPath "$PWD/.local/network-start-project" -runTests -testPlatform PlayMode -testFilter FirstLight.Tests.EditorMode.Network -testResults "$PWD/artifacts/network-start-tests.xml" -logFile "$PWD/artifacts/network-start-tests.log"
```

Use PlayMode: the actual UniTask.NextFrame boundary requires an advancing native Time.frameCount. Do not launch another process on that probe while it is open. The real Auth SDK grants internals access to the fixture's native test-friend assembly identity; its SDK types are never redefined. Test inputs contain synthetic local identities only. Native FLog initialization is called through reflection because its Conditional logging attributes otherwise omit the fixture call.

Five tests pass after the fix; source compiles with zero warnings/errors. The public [summary](../../../Docs/Modernization/verification/network-matchmaking-start-summary.json) records the exact tested source/assembly SHA and scope. A real rendered Main flow remains separate acceptance.
