# Native UI Timeline tint acceptance

The two authored fixture sources target the owned ignored `.local/tint-timeline-project`, containing the real pinned UITTimeline/Timeline packages and four preserved game Timeline assets plus the game-owned extension. Their explicit path guards prevent Main execution.

`NativeTintTimelineProbe.Run` is a synchronous Editor graph check. Launch Unity 6000.3.26f1 with `-batchmode -nographics -projectPath <owned project> -executeMethod NativeTintTimelineProbe.Run -quit`; it writes the original 8 clip/11 family/24 timing readback proof.

`NativeTintRuntimeProbe.Run` enters actual Play Mode and waits for a real runtime UIDocument panel. Launch with the same editor flags and `-executeMethod NativeTintRuntimeProbe.Run`, without `-quit`; it exits itself after the actual upstream negative control, tint weights, native scale and graph destruction checks. The fixture initializes the actual native Search default index before Play (verified installed Editor API) and loads the genuine Unity-created default runtime theme. It does not disable diagnostics.

Promotion payloads are frozen in `.local/tint-timeline-patches/manifest.json`; proof and full 363-track layer/curve inventory are in `Docs/Modernization/verification`. This fixture tests native Timeline/API/serialized clip acceptance; root still validates the authored game screen/prefab lifecycle in Main.
