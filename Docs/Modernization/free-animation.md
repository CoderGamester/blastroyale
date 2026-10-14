# Free runtime animation replacements

The client now keeps the unchanged public DOTween core 1.3.045 and its free modules, while the two paid visual tween components in UIVFXScreen are replaced by the existing game-owned UiVfxImage. The sole Animancer API caller, AdditionalFlairAnimation, uses Unity's native Playables through OneShotAnimationPlayer. No vendor implementation was copied or replaced with compatibility types.

## Preserved behavior and data

UiVfxImage moves the image in world space, starts at zero scale, reveals the image after the requested delay and invokes the existing presenter callback when movement finishes. The callback still returns the object to its pool. Both former DOTweenAnimation components had a one-second duration, a single normal scaled-time loop, auto-kill and distinct custom curves. Those curves were transferred exactly, including key times/values, tangents, weights and wrap modes. Scale remains 2.5 relative to the reset zero scale. Retrigger, disable and destruction kill the two active tweens without completing the cancelled request.

Only the two Pro component blocks and their entries in VfxSprite's component list were removed from UIVFXScreen.prefab. The Image, RectTransform, UiVfxImage, presenter, text and all existing object/script identities remain. The existing UiVfxImage and AdditionalFlairAnimation .meta files are unchanged; OneShotAnimationPlayer has a new normal Unity script GUID.

AdditionalFlairAnimation keeps the character's flair event and addressable prefab/clip fields. It finds the instantiated prefab's Animator, plays the real AnimationClip with an AnimationClipPlayable/AnimationPlayableOutput graph and advances the graph from LateUpdate using Time.deltaTime. The completion callback hides the flair object once. Retrigger destroys the prior graph and starts at time zero. Disable cancels playback; destruction unsubscribes and releases the instantiated addressable object. An addressable instantiation finishing after its owner is destroyed is also released.

The current owned text-serialized asset scan found no Animancer component GUID references and no prefab references to AdditionalFlairAnimation's own GUID. Missing, external or binary assets are outside that conclusion. The maintained caller was ported; the original flair asset set was not available for a full feature smoke test.

## Validation

- **Pass:** All three changed/new sources compile against the installed real Unity 6000.3.26 managed assemblies, free DOTween core and actual Addressables/UniTask APIs with zero warnings/errors. The isolated source compile references the existing FirstLight.Game assembly only to resolve the real CharacterSkinMonoComponent; it is not a whole-client clean-room build. Log: artifacts/free-animation-source-compile.log.
- **Pass:** A separate .local/free-animation-project contains the exact UiVfxImage and OneShotAnimationPlayer sources, the unchanged public free DOTween DLL/modules, Unity's native animation/UI modules and Unity Test Framework 1.6.0. The installed editor ran ten actual PlayMode tests: ten passed, zero failed/skipped, process exit zero. Logs/results: artifacts/free-animation-native-tests.log and .xml.
- Native tests cover authored clip sampling and one completion, retrigger restarting the clip/replacing its completion, cancellation/replay, zero/negative delta, destroyed animator, exact imported VFX curve/timing data, delayed reveal and real curve sampling, VFX retrigger cancellation, real OnDisable cancellation and completion returning an object to a pool.
- The isolated VFX prefab is the actual VfxSprite object with its four remaining native/game components extracted from the main prefab. Only its external parent and currency sprite reference are cleared for isolation. No substitute SDK/game types are used. The unrelated presenter/text siblings remain in the main asset and require the full-client import.
- **Not run in this scope:** original full UI prefab rendering, addressable flair load in a match, Android/iOS rendering and a player build. Root owns the serialized main-project validation. The isolated tests make no full-match or device claim.

Tools/Validation/FreeAnimationIsolation contains the reproducible project builder and native test fixture. Run the builder after the normal public-core restore, then use Unity 6000.3.26 with -runTests -testPlatform PlayMode against its returned unique project path.

## Root integration

The integrator removed Kybernetik.Animancer, DOTweenPro.Runtime and DOTweenPro.Editor assembly references and retired their ignored local package directories after the isolated tests passed. DOTween.Modules and UniTask.DOTween remain. The native replacement requires no animation package. Full client validation follows the Quantum 3 integration.

The official [DOTween license](https://dotween.demigiant.com/license.php) permits commercial/noncommercial use of its standard core; Pro licenses are separate. [Animancer's feature comparison](https://kybernetik.com.au/animancer/docs/introduction/features) lists its custom event system as a player-build Pro feature. The replacement uses the actual [Unity 6.3 AnimationClipPlayable](https://docs.unity3d.com/6000.3/Documentation/ScriptReference/Animations.AnimationClipPlayable.html) and [PlayableGraph.Evaluate](https://docs.unity3d.com/6000.3/Documentation/ScriptReference/Playables.PlayableGraph.Evaluate.html) APIs.
