using System;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;

namespace FirstLight.Game.Utils
{
	/// <summary>
	/// Plays a clip through Unity's native animation graph and reports its first completion.
	/// The owner advances scaled time and disposes the graph when disabled or destroyed.
	/// </summary>
	public sealed class OneShotAnimationPlayer : IDisposable
	{
		private PlayableGraph _graph;
		private AnimationClipPlayable _playable;
		private Animator _animator;
		private AnimationClip _clip;
		private Action _onComplete;

		public void Play(Animator animator, AnimationClip clip, Action onComplete)
		{
			Stop();
			if (!animator) throw new ArgumentNullException(nameof(animator));
			if (!clip) throw new ArgumentNullException(nameof(clip));

			_animator = animator;
			_clip = clip;
			_onComplete = onComplete;
			_graph = PlayableGraph.Create("Flair animation");
			_graph.SetTimeUpdateMode(DirectorUpdateMode.Manual);
			try
			{
				_playable = AnimationClipPlayable.Create(_graph, clip);
				_playable.SetDuration(clip.length);
				_playable.SetTime(0);
				AnimationPlayableOutput.Create(_graph, "Flair", animator).SetSourcePlayable(_playable);
				_graph.Play();
				_graph.Evaluate(0);
			}
			catch
			{
				Stop();
				throw;
			}
		}

		public void Update(float deltaTime)
		{
			if (!_graph.IsValid()) return;
			if (!_animator || !_clip)
			{
				Stop();
				return;
			}

			_graph.Evaluate(Mathf.Max(0, deltaTime));
			if (_playable.GetTime() < _clip.length) return;

			var onComplete = _onComplete;
			Stop();
			onComplete?.Invoke();
		}

		public void Stop()
		{
			_onComplete = null;
			if (_graph.IsValid()) _graph.Destroy();
			_graph = default;
			_playable = default;
			_animator = null;
			_clip = null;
		}

		public void Dispose() => Stop();
	}
}
