# Persisted AddressableId contract

The checked-in game enum has **65 allocated integers**, **64 active configurations** and the permanently retired **58** (`Configs_Settings_AssetResources`). Removing that enum member would reinterpret every later serialized integer. The root fixed runtime `GetConfig` to find `AddressableConfig.Id` instead of indexing the shortened list; retired IDs fail clearly. Its real NUnit regression fixture checks every active configuration and the runner address.

`Tools/CI/addressable-id-contract.json` records the existing name → integer and integer → address contract. `python3 Tools/CI/validate_addressable_ids.py` rejects renumbering, deleted tombstones, integer reuse, rebinding existing IDs to another address, duplicate/conflicting global configuration rows, and the upstream generator’s known positional lookup. Unsupported constructor expressions fail clearly rather than escaping the scan. The native NUnit fixture remains the authority for actual lookup behavior; this static check does not interpret arbitrary C# method bodies. New enum members can use unused integers above the highest allocated value; older active-address retirement requires an intentional reviewed contract update that reserves its integer. Both GitHub workflows run the guard. The public test job additionally runs seven mutation checks without Unity or secrets. Local guard, mutation checks and actionlint passed; remote Actions has not run.

## Maintained generator limitation

The pinned Services commit `dd2b87a6398288e13201330be065a4e3a3429bb3` has a positional generator at `Editor/AddressableIds/AddressableIdsGeneratorUtils.cs`:

- `AppendAddressEnumMembers` emits implicit sequential values in current asset-list order.
- `GenerateConfigs` constructs global rows with the loop index.
- `GenerateLoopUpMethods` emits `_addressableConfigs[(int) addressable]`.
- `GenerateLabelMap` gives each label row its local index; it does not use the global configuration ID. Existing `GenerateIds` label rows already differ from global IDs (CharacterSkins is label row 0, global ID 64).

Consequently, using the upstream generator directly on this migrated game would undo the persisted-ID repair. The maintained package is left unchanged. The checked-in output is authoritative until a game-specific stable-ID generation policy is implemented. After any generation, retain all existing enum members with their original integers (including tombstones), assign new IDs above the allocated maximum, make global and label rows share the global ID for each address, retain ID-based runtime lookup, and run the guard plus the NUnit fixture before accepting the result. This guard detects corruption; it does **not** make the upstream generator stable automatically. Do not refresh the contract simply to accept a positional regeneration failure.
