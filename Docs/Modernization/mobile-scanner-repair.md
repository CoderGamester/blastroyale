# Native mobile capability scanner repair

The pinned MIT MobileServices Editor scanner walked every loaded assembly, including its own Editor device simulator. That panel has permission and tracking-service fields, so the scanner incorrectly inferred seven permission categories and tracking usage. Actual native negative control reported eight configuration warnings. The bounded patch admits only actual Player compilation inputs and their precompiled references; it preserves the original runtime type inspection. Native patched control reports zero warnings, while a genuine player permission field still produces all seven categories and six missing-description warnings. No permission or tracking capability is enabled to silence a scanner.

The package remains the exact Git dependency `8ce2d7295afd200895179c981e5e43e60ce11d18`. After Unity resolves it, close that project and run:

```sh
python3 Tools/Modernization/patch_mobile_services_scanner.py --project BlastRoyale
```

The tool checks the Git fingerprint and complete original SHA before changing one source fragment. It verifies complete-file idempotence, keeps exact local recovery bytes, refuses unknown changes and refuses to patch an open project. It contains no SDK binaries. Deleting Library or changing this Git pin requires restoration again. `--restore` restores only the exact recognized original source.

GitHub workflows first use the pinned Unity Builder action with `PackageRestore.Resolve`, which verifies the package after Unity's normal UPM import and exits. The host applies this exact patch, then the normal test/build action compiles it. Both workflows pass actionlint; remote activation/container execution has not run. [Official pinned Builder implementation](https://raw.githubusercontent.com/game-ci/unity-builder/4423ee75828dff56037d1d1cfdac6b68e3a6ba00/dist/platforms/ubuntu/steps/build.sh).

The native project configuration keeps the authored `blastroyale` iOS scheme and Android `blastroyale://friend` deep link. It enables local notification/vibration declarations already used by the game; unused camera, microphone, location, media and tracking permissions remain disabled. The final Android build reports zero scanner warnings and one configured Android deep link. Physical-device notification/link behavior remains unrun.

Evidence: `verification/mobile-scanner-native-summary.json` and original/patched/positive native records. The isolated older clone retains unrelated import diagnostics; the scanner's own cases have zero C# diagnostics and native exceptions. Final Main build acceptance is reported separately.
