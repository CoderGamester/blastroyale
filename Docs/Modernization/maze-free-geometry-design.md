# Maze missing-prefab geometry: proposed bounded restoration

Status: the approved native isolated probe is complete and passed, including the full681-root rehearsal, four banners and prototype/reference persistence after restart. Main integration remains root-owned. [Results and limits](maze-native-restoration.md).

## Evidence

`MazeMayhemScene.unity` contains **681 missing PrefabInstance records across 65 missing source GUIDs**. All 681 have explicit position XYZ and quaternion XYZW override values. Only **195** contain complete local-scale XYZ overrides; missing source-prefab base scale is not recoverable from those scene records. **662** have added component overrides and **six** have added GameObject overrides. Additional child transform overrides exist, so flattening records or replacing all instances with arbitrary prefab roots would lose authored data.

The missing GUID audit also includes non-prefab references; those are excluded. Missing source hierarchies, original mesh dimensions, material assignments and original child fileIDs are unavailable. They must not be invented or assigned to newly generated objects.

## Recommendation

Use real Unity **6000.3.26f1** authoring in an isolated copy to probe whether `PrefabUtility.UnpackPrefabInstance(..., Completely, ...)` retains the currently loaded missing-instance objects, added components/children, scene references and recorded overrides. [Target-version API](https://docs.unity3d.com/6000.3/Documentation/ScriptReference/PrefabUtility.UnpackPrefabInstance.html) describes preservation of overrides, but does not guarantee recovery of absent source content. The [target source](https://github.com/Unity-Technologies/UnityCsReference/blob/6000.3/Editor/Mono/Prefabs/PrefabUtility.cs) delegates actual unpacking to native code; this requires a real roundtrip probe, not an assumption.

For roots that pass the probe, retain every existing GameObject/component and its available transform/parent relationship. Add a clearly marked game-owned **visual-only** child using real Unity primitives/materials. Unity generates all new object/file IDs; new placeholder prefabs, if used, receive fresh GUIDs. Original names remain available in a marker/manifest beside the original source GUID. No original missing GUID is repurposed as a fabricated prefab, and no invented child hierarchy or source local ID is used.

Prefer geometry derived from retained real Quantum box-collider authoring fields when unambiguous. Unit boxes/cylinders are acceptable clearly labeled visual approximations for roots without recoverable dimensions; inherited source scale remains unknown and is recorded as such. Existing authored position/rotation/scale overrides are preserved exactly. A visual fallback must not claim recovery of the original mesh, source base scale or collisions.

Keep actual retained Unity/Quantum collision components and the already recovered/migrated native map collision payload unchanged. Generated primitive physics colliders are removed immediately. Do not rebake the map/navmesh while source collision inheritance is incomplete. This preserves existing deterministic collision data without adding guessed physics.

## Probe and acceptance

1. Snapshot each native root's parent, transforms, active/tag/layer/static flags, added child/component types, serialized component values and scene references before unpacking. Associate it with the actual PrefabInstance/source GUID from the audit.
2. Try a small representative set: ordinary missing root, added Quantum collider, added child, explicit scale and omitted scale. If Unity cannot unpack a missing asset, or loses/rebinds any known authored component/reference, leave that instance unchanged and record it excluded. Do not delete/recreate it as an unproved replacement.
3. For passing roots add only marked native visual children, save a separate scene copy, close/reopen it, and compare the snapshots and deterministic map payload hashes. Existing references must still resolve to the retained authored objects/components; all newly generated mesh/material/object IDs must resolve to their actual native types.
4. Record repaired vs excluded counts, missing-prefab-instance reduction and remaining unresolved GUIDs separately. Assert zero missing newly introduced references. Only apply the proven subset to the original scene after root review of the roundtrip evidence, with its serialized Unity ownership reserved first.

A failure to recover missing native instances is a concrete source-content limitation. The useful alternative is a separate native collision-derived visual preview scene while leaving original scene data untouched; that is a distinct artifact and does not satisfy an original-scene repair claim.
