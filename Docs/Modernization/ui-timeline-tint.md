# Preserved native Timeline background-image tint

Eight authored `UITBackgroundTintClip` subassets retained their original script GUID, `_template.Color` payload, names, timings, easing and blend curves. The pinned MIT UITTimeline package lacks this operation: its mixer ignores an unknown `UITBehaviour`. The game-owned extension adds actual weighted `unityBackgroundImageTintColor` playback while composing the package's real UIDocument selector factory, mixer and supported clip behaviours.

Three new sources and a small assembly provide the clip, track and mixer. The clip's metadata keeps the original `d7b44b5154d04b2787b81cc4970c12d8` GUID so every existing tint object resolves without rewriting its payload. The track inherits the native package track, preserving its serialized `automaticUsageHints` field. Its factory uses the actual upstream mixer/element selection; known scale, opacity, position, rotation and class/display/visibility operations remain upstream behaviour. Tint blends active native input weights with the original resolved tint, normalizing weights above one, and restores the saved inline style when inactive or when the runtime graph stops/destroys.

The four affected Timeline assets are SeasonBannerAnimation, Rewards-Equipment, ProductBundleBannerAnimation and GameModeNewEvent. Exactly eleven track-family script GUIDs are changed. Parent/child names, IDs and all other bytes stay unchanged. Entire affected families share the new track type because native `TrackAsset.isSubTrack` requires exact parent/child type equality. All eight clip records and every asset metadata file remain byte-identical.

The full authored inventory contains 363 current native UI tracks. None have animated track fields or clip-property AnimationClips. Native Timeline 1.8.13 merges descendant clips into a single root mixer when `CreateLayerMixer` returns null; those ordinary layers remain supported. The package's warning about ignored animated track fields does not apply to the current authored set. Other track families remain on the pinned upstream type.

Real Unity 6000.3.26f1 acceptance passed two complementary checks:

- Four original Timeline assets load all eight native tint subassets and eleven track-family nodes. Twenty-four native graph evaluations at original clip timings match actual input-weight tint readback, including layered descendants and the authored easing/blending.
- A real attached runtime UIDocument panel compares the upstream negative control (tint ignored) with the extension. Half/full/zero weights, native scale contribution and restoration after graph destruction pass with zero runtime errors/exceptions and zero C# diagnostics.

Manual Editor Timeline evaluation does not issue the runtime destruction callbacks; restoration is therefore verified in actual Play Mode separately. The minimal fixture primes Unity's real default Search index before entering Play and uses the actual native-created default theme; no warnings or errors are suppressed. Failed preparatory fixture runs remain in ignored artifacts for traceability.

The guarded thirteen-file promotion manifest is `.local/tint-timeline-patches/manifest.json`; public handoff and results are in [the verification folder](verification/ui-timeline-tint-integration-handoff.json). Runnable native fixture sources are in `Tools/Validation/UITTimelineTint`. Main is changed only by root's guarded integration, and original screen/prefab playback remains part of complete Main acceptance.
