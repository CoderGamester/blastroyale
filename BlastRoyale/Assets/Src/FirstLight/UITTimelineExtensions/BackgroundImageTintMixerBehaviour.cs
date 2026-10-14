using System.Collections.Generic;
using UITTimeline;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.UIElements;

namespace FirstLight.Game.UI.Timeline
{
	public sealed class BackgroundImageTintMixerBehaviour : PlayableBehaviour
	{
		public UITMixerBehaviour NativeMixer;
		private readonly Dictionary<VisualElement, StyleColor> _originalStyles = new Dictionary<VisualElement, StyleColor>();
		private readonly Dictionary<VisualElement, Color> _originalColors = new Dictionary<VisualElement, Color>();

		public override void OnGraphStart(Playable playable)
		{
			NativeMixer.OnGraphStart(playable);
			foreach (var element in NativeMixer.Elements)
			{
				// Editor evaluation/seeking can restart a graph without destroying its
				// playable. Keep the baseline from before its first tint contribution.
				if (_originalStyles.ContainsKey(element)) continue;
				_originalStyles.Add(element, element.style.unityBackgroundImageTintColor);
				_originalColors.Add(element, element.resolvedStyle.unityBackgroundImageTintColor);
			}
		}

		public override void ProcessFrame(Playable playable, FrameData info, object playerData)
		{
			NativeMixer.ProcessFrame(playable, info, playerData);
			var weightedColor = Color.clear;
			var totalWeight = 0f;
			for (var index = 0; index < playable.GetInputCount(); index++)
			{
				var input = (ScriptPlayable<UITBehaviour>) playable.GetInput(index);
				if (!(input.GetBehaviour() is BackgroundImageTintBehaviour tint)) continue;
				var weight = playable.GetInputWeight(index);
				weightedColor += tint.Color * weight;
				totalWeight += weight;
			}
			foreach (var element in NativeMixer.Elements)
			{
				if (totalWeight <= 0f) element.style.unityBackgroundImageTintColor = _originalStyles[element];
				else element.style.unityBackgroundImageTintColor = totalWeight > 1f
					? weightedColor / totalWeight
					: weightedColor + _originalColors[element] * (1f - totalWeight);
			}
		}

		public override void OnGraphStop(Playable playable)
		{
			NativeMixer.OnGraphStop(playable);
			RestoreOriginalStyles();
		}

		public override void OnPlayableDestroy(Playable playable)
		{
			NativeMixer.OnPlayableDestroy(playable);
			RestoreOriginalStyles();
		}

		private void RestoreOriginalStyles()
		{
			foreach (var pair in _originalStyles) pair.Key.style.unityBackgroundImageTintColor = pair.Value;
		}
	}
}
