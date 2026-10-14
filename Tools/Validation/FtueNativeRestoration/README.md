# Native FtueDeck rehearsal helpers

These helpers deliberately require the existing isolated `.local/maze-native-probe` full-client project and the exact target Unity editor/native SDK. They are not runtime compatibility layers.

Run `ModernizationFtueProbe.DiagnoseMissingSceneEntries`, then `ModernizationFtuePrototypeBake.RunAll`, followed by `ModernizationFtuePrototypeBake.VerifyPersistent` in a fresh Unity process. Each stage throws on an unapproved change and writes inspectable JSON evidence. The full visual stage checks the exact known three Bakery owners/records and three ownerless records before preserving/removing them, verifies all original loaded authored objects/references, and permits only explicitly generated unit-box renderer cache rows. Prototype baking validates the MapEntities and corresponding native managed-reference registry while protecting every other Map field. No physics-collider, NavMesh or lighting bake is invoked.

The current accepted counts and source SHA are specific to this restoration: 100 visual roots and 17 native entity/reference slots. The original missing source prefab geometry is unavailable; unit boxes are marked approximations. Main is never changed by the helpers. Root must preserve Main metadata and use the integration handoff's exact SHA preconditions.
