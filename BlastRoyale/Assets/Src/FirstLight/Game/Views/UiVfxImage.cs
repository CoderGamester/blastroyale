using System;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

namespace FirstLight.Game.Views
{
	/// <summary>
	/// Handles / animates a single animatable UI VFX floating image.
	/// </summary>
	public class UiVfxImage : MonoBehaviour
	{
		[SerializeField] private Image _image = null;
		[SerializeField, Min(0)] private float _moveDuration = 1;
		[SerializeField] private AnimationCurve _moveEase = AnimationCurve.EaseInOut(0, 0, 1, 1);
		[SerializeField, Min(0)] private float _scaleDuration = 1;
		[SerializeField] private AnimationCurve _scaleEase = AnimationCurve.EaseInOut(0, 0, 1, 1);
		[SerializeField] private float _endScale = 2.5f;
		private Tween _moveTween;
		private Tween _scaleTween;

		private void OnDisable()
		{
			StopTweens();
		}

		private void OnDestroy()
		{
			StopTweens();
		}

		private void StopTweens()
		{
			_moveTween?.Kill();
			_scaleTween?.Kill();
			_moveTween = null;
			_scaleTween = null;
		}

		public void Play(Sprite sprite, float delay, Vector3 originWorldPosition, Vector3 targetWorldPosition,
						 Action onComplete)
		{
			StopTweens();
			_image.sprite = sprite;
			_image.enabled = false;

			var t = transform;
			t.position = originWorldPosition;
			t.localScale = Vector3.zero;

			// The prefab's authored curves replace its former visual tween components.
			_moveTween = t.DOMove(targetWorldPosition, _moveDuration)
				.SetEase(_moveEase).SetDelay(delay).SetTarget(gameObject)
				.SetLoops(1, LoopType.Restart).SetAutoKill(true).SetUpdate(UpdateType.Normal, false)
				.OnKill(() => _moveTween = null)
				.OnStart(() => _image.enabled = true)
				.OnComplete(() => onComplete?.Invoke());
			_scaleTween = t.DOScale(_endScale, _scaleDuration)
				.SetEase(_scaleEase).SetDelay(delay).SetTarget(gameObject)
				.SetLoops(1, LoopType.Restart).SetAutoKill(true).SetUpdate(UpdateType.Normal, false)
				.OnKill(() => _scaleTween = null);
			_moveTween.Play();
			_scaleTween.Play();
		}
	}
}
