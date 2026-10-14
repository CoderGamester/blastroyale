# Isolated native Maze acceptance helpers

These game-specific helpers use real Unity6000.3.26f1 and actual Quantum3 APIs. They refuse to run outside `.local/maze-native-probe`. No SDK source or binaries are included here. Restore the legitimate private SDK and game-generated types first, copy the current project's Assets/Packages/ProjectSettings into that ignored isolated directory, and copy these two own-source helpers into its Assets/Editor. Coordinate Unity ownership; never bypass a project lock or run an automatic Main apply.

Methods, in order:

1. `ModernizationMazeProbe.RunFull`: native visual preservation with all41 payload hashes unchanged; retire the exact3 approved missing Bakery components with full ignored recovery data; save a separate scene and four prefab copies. It reports exclusions/errors rather than fabricating old source hierarchies/IDs.
2. `ModernizationMazePrototypeBake.Run`: actual native prototype-only bake into the isolated map and export native JSON. This is explicitly separate from the preservation phase.
3. `ModernizationMazePrototypeBake.PersistAndReopen`: persist native map view references; verify scene save/reload; finalize banner mesh/material fallbacks.
4. Start a **new Unity process** for `ModernizationMazePrototypeBake.VerifyPersistent`: verify final map/scene references, native asset types and four banners after restart.

Use batchmode, nographics, quit and an explicit log file. The helpers intentionally validate the known708 source prototype count for this audited scene. They are acceptance tools for this migration, not a generic missing-asset recovery framework. Inspect generated JSON/proof and source/destination SHAs before manual integration. Import only MapEntities into newer converted maps, preserving their triangle/navmesh payloads.

Exact evidence and remaining limits: [native restoration report](../../../Docs/Modernization/maze-native-restoration.md).
