using System;
using UITTimeline;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;

namespace FirstLight.Game.UI.Timeline
{
	/// <summary>Extends the pinned native UITTimeline track with background-image tint.</summary>
	[Serializable]
	[TrackClipType(typeof(BackgroundImageTintClip))]
	public sealed class BackgroundImageTintTrack : UITVisualElementTrack
	{
		public override Playable CreateTrackMixer(PlayableGraph graph, GameObject go, int inputCount)
		{
			// The actual package owns selector parsing, UIDocument binding, usage hints,
			// and its supported clip operations. Its serialized base fields stay intact.
			var nativePlayable = base.CreateTrackMixer(graph, go, inputCount);
			if (!nativePlayable.IsValid()) return Playable.Null;
			var nativeMixer = ((ScriptPlayable<UITMixerBehaviour>) nativePlayable).GetBehaviour();
			var mixer = ScriptPlayable<BackgroundImageTintMixerBehaviour>.Create(graph, inputCount);
			mixer.GetBehaviour().NativeMixer = nativeMixer;
			graph.DestroyPlayable(nativePlayable);
			return mixer;
		}
	}
}
