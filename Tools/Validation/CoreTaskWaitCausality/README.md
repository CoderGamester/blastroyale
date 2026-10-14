# Native Core TaskWait reentrancy fixture

Copy `NativeCoreTaskWaitProbe.cs` into `Assets/Editor` of the owned ignored `.local/maze-native-probe` full-client project, with the genuine pinned Statechart/UniTask/Services packages already installed. The path guard prevents execution in Main. Run Unity 6000.3.26f1 with `-batchmode -nographics -projectPath <owned project> -executeMethod NativeCoreTaskWaitProbe.Run` and a log file. Omit `-quit`: the fixture enters real Play Mode, checks eight actual Unity frames per case, writes `native-core-taskwait-proof.json`, and exits itself with 0/1.

The negative case deliberately logs the actual package's two duplicate-bind/task errors. Both boundary cases must have one bind and no fixture errors. The fixture keeps menu loading pending after the real maintained installer bind and never makes the installer idempotent. Other old-clone import diagnostics are outside its controlled fixture error counter and are documented separately.
