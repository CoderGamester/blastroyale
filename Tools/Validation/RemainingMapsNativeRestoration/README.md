# Remaining map native restoration

All eight additional map scenes were recovered in Unity6000.3.26f1 with the legitimately restored Quantum3.0.14 SDK. The SDK's `QuantumMapDataBaker.BakePrototypes(data,true)` populated originally empty entity caches. Every existing authored object/component field was compared before recovery and after baking; only the exact scene entity-reference cache and generated visual renderer-cache rows changed. Original scene GUIDs/metas and all other map fields stayed intact. Separate native restart verified the complete fingerprints.

The missing art is represented by clearly named orange native unit boxes. They have no added colliders. No collider, navmesh or lighting bake ran. These placeholders do not recover licensed geometry or missing prefab base transforms. District had235 old reference slots (109 null) and223 actual active native prototype sources; the difference is not attributed to inactive filtering or claimed source loss. The explicit new-ID ledger records retained Unity-loaded objects/components that had no original durable serialized IDs.

A scene reference slot exists for each prototype. Only a source `QuantumEntityView` becomes the SDK's `selfView`; asset-spawned views and pure simulation prototypes can legitimately have null slots. The validator calls the genuine SDK sibling sorter and `SerializeImplicitComponents` and checks every slot against its actual source `selfView`.

After following the root SDK/package setup, prepare a separate host:

```sh
python3 Tools/Validation/RemainingMapsNativeRestoration/prepare_project.py
"$UNITY_PATH" -batchmode -nographics -quit \
  -projectPath "$PWD/.local/remaining-map-validation-project" \
  -executeMethod ModernizationAcceptedMapsValidation.Run \
  -logFile "$PWD/.local/remaining-map-validation-project/native-map-validation.log"
```

Use the exact editor from `BlastRoyale/ProjectSettings/ProjectVersion.txt`. The preparer copies the locally restored real SDK and current public game assets, excludes account credentials/native project linkage and does not redistribute SDK source or binaries. It refuses to replace an existing host by default.

Public acceptance fingerprints and native identity provenance are in `Docs/Modernization/verification/native-map-entity-cache-ledger.json`, `remaining-map-native-restoration-summary.json`, `remaining-map-native-postrestart-proof.json` and `remaining-map-native-generated-identities.json`. Before/after source hashes identify the original records in Git history; the ignored recovery savepoints retain the exact raw scenes and retired Bakery records. The bounded Quantum asset validator requires exact ten-map cache/scene/meta/other-field hashes, native source counts and original Quantum GUIDs; it never ignores arbitrary map fields.

The game-owned `ModernizationMapRecovery.cs` and `ModernizationRemainingMaps.cs` preserve the original recovery implementation. They are guarded to the separate `.local/remaining-map-probe` host and require the exact pre-recovery native inventory/source hashes. They are not imported by the default read-only preparer and do not recreate missing licensed base art. To reproduce recovery, restore the exact before-hash scene/map inputs from Git or the ignored raw recovery savepoints into that isolated host, then run native `ModernizationRemainingMaps.Inventory`, `ModernizationMapRecovery.RunAll`, `VerifySourceAndOriginalGuid` and, in a new process, `VerifyRestart`. Never invoke recovery against Main or replace already accepted nonempty entity caches. The read-only validation above is the normal fresh-clone check.
