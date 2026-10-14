using System;
using UITTimeline;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;

namespace FirstLight.Game.UI.Timeline
{
	/// <summary>Retains the game's authored _template.Color Timeline payload.</summary>
	[Serializable]
	public sealed class BackgroundImageTintClip : PlayableAsset, ITimelineClipAsset
	{
		public BackgroundImageTintBehaviour _template = new BackgroundImageTintBehaviour();
		public ClipCaps clipCaps => ClipCaps.All;
		public override Playable CreatePlayable(PlayableGraph graph, GameObject owner) =>
			ScriptPlayable<BackgroundImageTintBehaviour>.Create(graph, _template);
	}

	[Serializable]
	public sealed class BackgroundImageTintBehaviour : UITBehaviour
	{
		public Color Color = Color.white;
	}
}
