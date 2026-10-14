# Native ShaderGraph migration

These authored helpers use the installed ShaderGraph 17.3.0 APIs to migrate five exact graph inputs. They contain no replacement SDK types. Copy the two `.cs` files and the `.asmref` into a temporary `Assets/Editor/NativeShaderMigration` folder in an isolated Unity 6000.3.26f1 project with public URP 17.3.0. The asmref compiles the helper with the real `Unity.ShaderGraph.Editor` assembly so its native graph APIs are accessible. The helper pins the package version and requires the game's Gamma color space.

Copy the graph inputs and their existing metas at the paths in `manifest.json`. Include the unchanged `SGG_SkyboxUVs.shadersubgraph` and its meta, which are real dependencies of the Skybox graph. All source graph content/meta hashes and GUIDs are checked before any graph mutation. `Apply` is for the original input hashes; `ValidateSaved` is for the reviewed output hashes.

Set `BLAST_SHADER_MIGRATION_MANIFEST` to the absolute manifest path and `BLAST_SHADER_EVIDENCE_DIR` to an absolute output folder. Execute these native Editor entry points in separate serialized runs:

1. `FirstLight.Modernization.NativeShaderGraphMigration.Apply`
2. `FirstLight.Modernization.NativeGradientGpuProof.Run` with graphics available
3. `FirstLight.Modernization.NativeShaderGraphMigration.ValidateSaved` after restarting Unity

The View Direction nodes upgrade through native `ChangeVersion`. The three native generated shaders are byte-identical before and after. The legacy Skybox graph is saved through native MultiJson serialization.

Sample Gradient v1 adds a Gamma transfer that changes these authored gradients. The migration adds native RGB-to-Linear, Split and Combine nodes to invert that transfer while preserving the original alpha and shader property values. Shader generation verifies the exact path. A genuine GPU probe extracts the generated original/v1/conversion/Combine functions and unchanged authored gradient declarations; it renders 4,096 samples per graph into a float target. It must span the authored color range and detect the incorrect uncorrected v1 result, so a uniform/error render cannot pass.

On Apple M4 Max/Metal in Gamma space, corrected RGB error is at most `7.5833e-7`, alpha error is exactly zero, and the uncorrected upgrade error is `0.2871359`. A diagnostic run with `BLAST_SHADER_PROBE_HALF=1` compiles the native ShaderLibrary unified-half branch and records errors up to `1.7006e-6`, with alpha unchanged. This is a precision-branch test on Metal, not mobile-device validation; minimum-precision implementations may choose greater precision. The accepted numeric tolerance is `2e-6`.

Actual native graph validation/import and the separate restart produce zero node errors/warnings or shader compiler messages across all five graphs. Every original graph meta/GUID and every natively decoded original node/property is retained. Durable evidence is under `Docs/Modernization/verification/shadergraph-*`; raw generated shaders and successful Unity logs are under `artifacts/shadergraph-migration` and `artifacts/shadergraph-*.log`.

Integration copies only the five reviewed `.shadergraph` outputs. Existing metas remain unchanged. The temporary asmref/helpers and probe shaders are validation tools and are not required in the game project.
