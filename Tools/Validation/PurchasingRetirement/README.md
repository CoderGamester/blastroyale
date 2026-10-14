# Native bundled-shop regression fixture

Purchasing4.15.1 is retired because paid charges/receipt grants are unavailable in this free development flow. The earned-currency shop uses the genuine bundled catalog and existing BuyFromStoreCommand. Catalog readiness completes after its actual loaded event; it does not pretend that a payment SDK initialized. Game-owned string failure reasons preserve earlier local JSON without platform-store types.

Four meaningful EditMode cases exercise the actual application service: bundled catalog readiness; rejecting a cash request without executing an earned-currency command; completing a failure dialog after the genuine dialog interface returns; and reading prior payment-failure JSON. The whole current native game assembly and imported genuine SDK/package dependencies are used. NSubstitute implements only actual application collaborators. No Unity/SDK/Frame type is redefined, no live charge/account/network is involved, and no proprietary implementation is included here.

Restore legitimate dependencies and import Main in6000.3.26f1 first. Close the Editor owning the probe before preparing or running it. From repository root:

```sh
python3 Tools/Validation/PurchasingRetirement/prepare_project.py
/Applications/Unity/Hub/Editor/6000.3.26f1/Unity.app/Contents/MacOS/Unity \
  -batchmode -nographics \
  -projectPath "$PWD/.local/purchasing-retirement-project" \
  -runTests -testPlatform EditMode \
  -testFilter FirstLight.Tests.EditorMode.Services.BundledShopReadinessTests \
  -testResults "$PWD/artifacts/purchasing-retirement-tests.xml" \
  -logFile "$PWD/artifacts/purchasing-retirement-tests.log"
```

The preparer records actual assembly hashes and copies no configured credential Resources/settings. `--game-assembly` can select a reviewed isolated whole-app compile. Explicit native plugin references match this DLL-based fixture; Main runs the same tests in its ordinary Editor test assembly. The source enables the real logger's conditional initializer because the app assembly contains development log calls.

Acceptance is bounded: four native cases and runtime/Editor/test source compilation pass. Final Main import/build and device/store behavior are separate; paid purchases remain unavailable.
