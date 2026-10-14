# Native USS/UXML importer verification

Both helpers require the owned `.local/maze-native-probe` at Unity6000.3.26f1.

- `NativeUitkAssetInventory.Run` creates the marked native diagonal mask only when absent, guards its previously missing GUID, imports it through actual TextureImporter and records real GUID/type/local IDs for six repair targets.
- `NativeUitkWarningProbe.Run` force-imports the paths in `uitk-warning-paths.txt`, requires each actual imported asset to load, and logs explicit Begin/PASS markers. Set `BLAST_UITK_WARNING_STAGE=Original` or `Patched`; count native importer messages between these markers. The isolated project contains the actual game UI type declarations; no type/serialization stubs are used.

See `Docs/Modernization/ui-importer-repairs.md` for exact proof scope and guarded handoff. This does not enter Play or verify a rendered device layout.
