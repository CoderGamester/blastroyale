# GameLovers Mobile Services migration

Status: **Implemented source migration; Unity/runtime validation pending**.

The pinned public package is `com.gamelovers.mobileservices` from `https://github.com/CoderGamester/Unity-MobileServices.git#8ce2d7295afd200895179c981e5e43e60ce11d18` (package version 1.2.0, MIT). Its current runtime namespaces and signatures were read from the local clone at `.local/packages/MobileServices`; no compatibility API was invented or copied into the game.

## Native UI

All ten game files that imported or fully qualified `FirstLight.NativeUi` now use `GameLovers.MobileServices.NativeUi`. Existing button descriptors port from `Positive` to `Default` and `Negative` to `Cancel`, matching the current package's actual OS-style enum. The two old `(title, message, text, callback)` convenience calls in `GameBackendService` now pass explicit `AlertButton` values into the package's real overload; maintenance/update blocking dialogs specify `isDismissible=false`.

The old `Modules/Native Ui` source/native plugin and assembly were deleted. iOS native UI source comes from the pinned MIT package, avoiding duplicate native symbols. Rate/review retains the updated project behavior: Unity's iOS review API and Android store URL; it does not use the package's optional Google Play review bridge.

`Assets/Editor/MobileServicesConfig.asset` is persisted with the package's actual config script GUID. Mobile Services owns the declared generated native build settings. Only Android notifications and vibration permissions are enabled because existing game services use them. No camera, microphone, location, media access, tracking, push entitlement or deep-link capability is added by this config. The Play review dependency is disabled because the project review route does not call it. The config is editor-only and contains no secrets.

## Input

The embedded Input Extensions code is dead sample infrastructure, not the game's controls. A source scan found no consumers of its `Input.Controls`, `PointerInputManager`, `GamepadInputManager`, `GestureController`, `SwipeInput`, `TapInput` or `PointerInput` outside that module. A scan of every module `.meta` GUID against all non-meta files under `Assets` found no serialized references.

The entire old `Modules/Input Extensions` module, generated sample action wrappers and sample `.inputactions` files were therefore deleted. No unused copied implementation was moved into game code. The real `Game/Input/InputActions.cs`/`LocalInput` and `Assets/Design/InputSystem/LocalInput.inputactions` remain at their original paths with their metadata. Current gesture support is supplied by `GameLovers.MobileServices.Gestures` if the game later needs it; there is currently no game gesture call site or test to port.

## Assembly integration

Root integration owns the manifest and final assembly mapping:

- Retired `FirstLight.NativeUi`: GUID `113cff88732a84c2680c9dbbdbefc792`.
- Retired `FirstLight.InputExtensions`: GUID `7be4a88d5c82b46bab6801636cca9ed1`.
- Current `GameLovers.MobileServices`: GUID `cbe822915a7aa48eba069762603eb81d`.

The game references the current Mobile Services runtime assembly; its UPM package declares real Unity Input System and mobile-notifications dependencies. Root pins Unity 6.3-compatible versions of those dependencies at the host level.

## Evidence and limits

- Inspected the actual pinned package `package.json`, NativeUi/Gestures runtime source, config schema, build pipeline docs and assembly metadata.
- Ran the complete module-GUID serialized-reference scan before deletion: no consuming assets found.
- Ran `dotnet run --project /tmp/blast-csharp-parse/parse.csproj -- BlastRoyale/Assets/Src/FirstLight/Game`: 745 C# files parsed, **0 syntax errors** at that point. This does not verify game type resolution.
- Scanned source for old native/input namespaces and old `Positive`/`Negative` alert styles: no remaining callers found.
- `git diff --check` for the changed backend native calls/config: clean.
- Unity config deserialization, import/assembly resolution, Game view native mock interaction, device alerts, local-notification delivery, vibration, generated Android/iOS manifests and physical-device input: **Not run**. No Unity process was started by this subtask.

After root's complete dependency integration, verify startup alerts in the Editor (including both callbacks and non-dismissible error screens), then verify one iOS and Android build. Native alert interaction and OS notification/haptic behavior require physical-device acceptance; there is no new account or paid plugin prerequisite for this migration.

An isolated compilation of the package's `Runtime/NativeUi` source against the installed Unity 6000.3 managed reference assemblies succeeded with **0 errors and 3 CS0649 warnings**. The warnings are the package's three editor override delegate fields, assigned by its separate Editor assembly. This used `/tmp/blast-mobile-compile/nativeui.csproj` with Editor symbols and .NET analyzers disabled; the fields' compiler warnings were not suppressed in the reported run. Full Unity compilation remains pending, and those upstream warnings have been reported to the integrator rather than hidden.
