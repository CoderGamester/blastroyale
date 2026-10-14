# Runnable test restoration checkpoint

The original project, using the restored real Quantum 2.1.10 SDK and target Unity 6000.3.26f1, passed 272/272 EditMode tests and 6/6 PlayMode tests, with zero failures or skipped tests and process exit zero. Results are in verification/main-editmode-q2.xml and verification/main-playmode-q2.xml. These are a restoration checkpoint; they must be rerun after the final Quantum 3 and free inspector integration.

The pure model fixtures use an ICoroutineService substitute because they run outside PlayMode. The nine pooling cases exercise the actual maintained ObjectPool and its callbacks instead of an old proxy API. Six actual CoroutineService PlayMode cases now target the current completion API. The entirely commented smoke fixture and an assertion-free asset fixture were deleted.

The run exposed two unconditional nodes incorrectly declared as Statechart choices. AudioState.GameModeCheck and GameSimulationState.StopSimulationInitializationError now use transitions. Other choices retain their conditions and fallback branches.

Commands used the installed Unity executable with -batchmode -nographics -projectPath BlastRoyale -runTests -testPlatform EditMode or PlayMode and explicit -testResults/-logFile paths. Unity runs were serialized for this project. Zero-test results are rejected by CI.
