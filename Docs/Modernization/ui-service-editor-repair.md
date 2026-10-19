# Pinned UiService Editor repair

The public UiService Git pin is retained. Its test prefab lived in an ordinary Resources folder, so Unity included an asset whose helper script is test-only in Android players. The guarded repair moves that unchanged folder and its existing metadata to the package's existing `Editor/Resources` directory. No runtime loader or test assertion changes. The original Resources loader fixture passes all five cases, including real first-load lookup, cached reuse and distinct inactive instances.

Enabling optional package tests also exposed a Unity6 camera query without its required sort-mode argument. The repair adds `FindObjectsSortMode.None` to that one test call; the test's camera checks and teardown remain intact. A temporary testable-package run passed 47 additional EditMode cases. The project's normal manifest retains its own 318 EditMode and 14 PlayMode cases.

After initial UPM resolution, close Unity and run:

```sh
python3 Tools/Modernization/patch_ui_service_editor.py --project BlastRoyale
```

The script checks the exact Git pin, lock hash, all fixture bytes, metadata and both camera-source hashes before changing anything. Repeated application verifies the resulting layout. `--rollback` restores the original reviewed layout/API line. Unknown inputs, ambiguous layouts and orphan destination metadata are refused. No SDK implementation or new metadata is generated. Five actual scratch guard checks verify idempotency, rollback, reapply and negative cases. Reapply after clearing Library; both workflows do this after package resolution and before tests/build.

[Original native fixture](verification/ui-resource-fixture-20261019.json), [repair guards](verification/ui-editor-repair-guard-20261019.json). Fresh player exclusion and final build warnings are recorded in REPORT.md. Pipeline's intentional disabled-runtime-server warning is separate; enabling that server to hide a diagnostic is not an acceptable fix.
