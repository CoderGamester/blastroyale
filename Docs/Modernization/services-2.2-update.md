# GameLovers Services current release review — 2026-10-19

Read-only public-source/API comparison. No project/package cache edits, no Unity calls.

**Decision:** update only Services pin from `dd2b87a6398288e13201330be065a4e3a3429bb3` (2.1.2) to `12af946385ab192b6c29c62e2c33ccc33455a265` (2.2.0) using normal UPM resolution, followed by native compile/host suites and mobile builds. No game call-site port is required by inspected source.

Public GitHub compare API reports 14 commits ahead, 0 behind. 2.2.0 released 2026-10-17. Changes:

- DataService adds IDataStorage, PlayerPrefsDataStorage, FileDataStorage, BrowserLocalDataStorage and optional key prefix. Parameterless DataService constructor remains and delegates to PlayerPrefsDataStorage. Default keys remain typeof(T).Name, JSON uses the same Newtonsoft serializer, missing data still creates T, public typed APIs unchanged; protected OnDataSaved hook remains after each persisted value.
- DataService.SaveAllData now persists each PlayerPrefs entry separately via backend.Write, which calls PlayerPrefs.Save. This changes flush cadence from once after the loop to once per item. Host ClientDataService already saves local entries individually and does not call package SaveAllData for remote entries, so no migration/port needed.
- Host ClientDataService.cs:37–38 uses two parameterless stores, only public Get/Load/AddOrReplace/Save APIs; never opts into new backend or prefix. Existing ClientStateMigrationTests exercise local roundtrip, remote-only memory, dynamic registry, replacing local with remote, and legacy stored key. These should be rerun against 2.2.0.
- MessageBroker, AssetResolver, MainInstaller and Tick changes are documentation only. Time private static variable and RNG private constants were renamed without numerical/algorithm change. No private package reflection found in host for those symbols.
- Services Explorer Data tab reads/deletes using bound storage; compatible with old default PlayerPrefs and optional future backends.
- AssetResolver sample assigns default InputSystemUIInputModule actions. This affects Samples~; it is not itself a fix for this game's joystick/input lifecycle.
- Dependencies unchanged: GameData 1.0.0, Addressables 1.21.20, UniTask 2.5.10, PerformanceTests 3.5.0. Host already has explicit pinned UniTask and newer Addressables. Unity minimum lowers 6000.3 to 6000.0, compatible with target 6000.3.26f1.
- New BrowserLocalDataStorage native exports are conditional UNITY_WEBGL && !UNITY_EDITOR; jslib importer is WebGL-only. There is no new Android/iOS native dependency.

Do not switch this game's stores to FileDataStorage/BrowserLocalDataStorage or add key prefixes as part of the version bump: that would change existing save lookup locations without a requested migration. Keep remote progression behind existing UGS boundary.

Verification status: public source compatibility reviewed; current native acceptance is recorded in REPORT.md, separately from earlier package results.

## Exact immutable sources

- https://api.github.com/repos/CoderGamester/Unity-Services/compare/dd2b87a6398288e13201330be065a4e3a3429bb3...12af946385ab192b6c29c62e2c33ccc33455a265
- https://github.com/CoderGamester/Unity-Services/compare/dd2b87a6398288e13201330be065a4e3a3429bb3...12af946385ab192b6c29c62e2c33ccc33455a265
- https://github.com/CoderGamester/Unity-Services/blob/12af946385ab192b6c29c62e2c33ccc33455a265/CHANGELOG.md
- https://github.com/CoderGamester/Unity-Services/blob/12af946385ab192b6c29c62e2c33ccc33455a265/Runtime/DataService.cs
- https://github.com/CoderGamester/Unity-Services/blob/12af946385ab192b6c29c62e2c33ccc33455a265/Runtime/PlayerPrefsDataStorage.cs
- https://github.com/CoderGamester/Unity-Services/blob/12af946385ab192b6c29c62e2c33ccc33455a265/Runtime/BrowserLocalDataStorage.cs
- https://github.com/CoderGamester/Unity-Services/blob/12af946385ab192b6c29c62e2c33ccc33455a265/Runtime/Plugins/WebGL/BrowserLocalDataStorage.jslib.meta
- https://github.com/CoderGamester/Unity-Services/blob/12af946385ab192b6c29c62e2c33ccc33455a265/package.json
