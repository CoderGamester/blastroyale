using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.UIElements.Experimental;

namespace FirstLight.Game.UIElements
{
	/// <summary>
	/// A small widget responsible for displaying a loading spinner that rotates with the set amount of speed
	/// </summary>
	[UxmlElement]
	public partial class AnimatedImageElement : VisualElement
	{
		private float _uxmlrotationsPerSecond = 0;
		[UxmlAttribute("rotations-per-second")]
		public float rotationsPerSecond
		{
			get => _uxmlrotationsPerSecond;
			set { _uxmlrotationsPerSecond = value; ScheduleAnimations(); }
		}
		private float _uxmlrandPosMinX = 0;
		[UxmlAttribute("rand-pos-min-x")]
		private float randPosMinX
		{
			get => _uxmlrandPosMinX;
			set { _uxmlrandPosMinX = value; ScheduleAnimations(); }
		}
		private float _uxmlrandPosMinY = 0;
		[UxmlAttribute("rand-pos-min-y")]
		private float randPosMinY
		{
			get => _uxmlrandPosMinY;
			set { _uxmlrandPosMinY = value; ScheduleAnimations(); }
		}
		private float _uxmlrandPosMaxX = 0;
		[UxmlAttribute("rand-pos-max-x")]
		private float randPosMaxX
		{
			get => _uxmlrandPosMaxX;
			set { _uxmlrandPosMaxX = value; ScheduleAnimations(); }
		}
		private float _uxmlrandPosMaxY = 0;
		[UxmlAttribute("rand-pos-max-y")]
		private float randPosMaxY
		{
			get => _uxmlrandPosMaxY;
			set { _uxmlrandPosMaxY = value; ScheduleAnimations(); }
		}
		private int _uxmlrandPosDurationMs = 0;
		[UxmlAttribute("rand-pos-duration-ms")]
		private int randPosDurationMs
		{
			get => _uxmlrandPosDurationMs;
			set { _uxmlrandPosDurationMs = value; ScheduleAnimations(); }
		}
		private float _uxmlrandPosLerpFactor = 0;
		[UxmlAttribute("rand-pos-lerp-factor")]
		private float randPosLerpFactor
		{
			get => _uxmlrandPosLerpFactor;
			set { _uxmlrandPosLerpFactor = value; ScheduleAnimations(); }
		}
		private float _uxmlsineWavePosMinX = 0;
		[UxmlAttribute("sine-wave-pos-min-x")]
		private float sineWavePosMinX
		{
			get => _uxmlsineWavePosMinX;
			set { _uxmlsineWavePosMinX = value; ScheduleAnimations(); }
		}
		private float _uxmlsineWavePosMinY = 0;
		[UxmlAttribute("sine-wave-pos-min-y")]
		private float sineWavePosMinY
		{
			get => _uxmlsineWavePosMinY;
			set { _uxmlsineWavePosMinY = value; ScheduleAnimations(); }
		}
		private float _uxmlsineWavePosMaxX = 0;
		[UxmlAttribute("sine-wave-pos-max-x")]
		private float sineWavePosMaxX
		{
			get => _uxmlsineWavePosMaxX;
			set { _uxmlsineWavePosMaxX = value; ScheduleAnimations(); }
		}
		private float _uxmlsineWavePosMaxY = 0;
		[UxmlAttribute("sine-wave-pos-max-y")]
		private float sineWavePosMaxY
		{
			get => _uxmlsineWavePosMaxY;
			set { _uxmlsineWavePosMaxY = value; ScheduleAnimations(); }
		}
		private float _uxmlsineWavePosXOffset = 0;
		[UxmlAttribute("sine-wave-pos-x-offset")]
		private float sineWavePosXOffset
		{
			get => _uxmlsineWavePosXOffset;
			set { _uxmlsineWavePosXOffset = value; ScheduleAnimations(); }
		}
		private float _uxmlsineWavePosYOffset = 0;
		[UxmlAttribute("sine-wave-pos-y-offset")]
		private float sineWavePosYOffset
		{
			get => _uxmlsineWavePosYOffset;
			set { _uxmlsineWavePosYOffset = value; ScheduleAnimations(); }
		}
		private float _uxmlsineWavePosXFrequency = 0;
		[UxmlAttribute("sine-wave-pos-x-frequency")]
		private float sineWavePosXFrequency
		{
			get => _uxmlsineWavePosXFrequency;
			set { _uxmlsineWavePosXFrequency = value; ScheduleAnimations(); }
		}
		private float _uxmlsineWavePosYFrequency = 0;
		[UxmlAttribute("sine-wave-pos-y-frequency")]
		private float sineWavePosYFrequency
		{
			get => _uxmlsineWavePosYFrequency;
			set { _uxmlsineWavePosYFrequency = value; ScheduleAnimations(); }
		}

		private float _uxmlsineWaveScaleMin = 1f;
		[UxmlAttribute("sine-wave-scale-min")]
		private float sineWaveScaleMin
		{
			get => _uxmlsineWaveScaleMin;
			set { _uxmlsineWaveScaleMin = value; ScheduleAnimations(); }
		}
		private float _uxmlsineWaveScaleMax = 1f;
		[UxmlAttribute("sine-wave-scale-max")]
		private float sineWaveScaleMax
		{
			get => _uxmlsineWaveScaleMax;
			set { _uxmlsineWaveScaleMax = value; ScheduleAnimations(); }
		}
		private float _uxmlsineWaveScaleFrequency = 0;
		[UxmlAttribute("sine-wave-scale-frequency")]
		private float sineWaveScaleFrequency
		{
			get => _uxmlsineWaveScaleFrequency;
			set { _uxmlsineWaveScaleFrequency = value; ScheduleAnimations(); }
		}

		private ValueAnimation<float> _rotationTween;
		private ValueAnimation<float> _randPosTween;
		private ValueAnimation<float> _sinewavePosTween;
		private ValueAnimation<float> _sinewaveScaleTween;

		private IVisualElementScheduledItem _animationRefresh;

		public AnimatedImageElement()
		{
			RegisterCallback<AttachToPanelEvent>(_ => ScheduleAnimations());
			RegisterCallback<DetachFromPanelEvent>(_ => StopAnimations());
		}

		private void ScheduleAnimations()
		{
			_animationRefresh?.Pause();
			// The panel scheduler applies the completed set of serialized attributes together.
			_animationRefresh = schedule.Execute(ApplyAnimations);
		}

		private void StopAnimations()
		{
			_rotationTween?.Stop();
			_randPosTween?.Stop();
			_sinewavePosTween?.Stop();
			_sinewaveScaleTween?.Stop();
		}

		private void ApplyAnimations()
		{
			StopAnimations();
			if (rotationsPerSecond != 0) AnimateRotation();
			if (randPosMinX != 0 || randPosMinY != 0 || randPosMaxX != 0 || randPosMaxY != 0)
				AnimateRandomPosition();
			if (sineWavePosMinX != 0 || sineWavePosMinY != 0 || sineWavePosMaxX != 0 || sineWavePosMaxY != 0)
				AnimateSineWavePosition();
			if (sineWaveScaleMin != 1f || sineWaveScaleMax != 1f || sineWaveScaleFrequency != 0)
				AnimateSineWaveScale();
		}

		private void AnimateRotation()
		{
			if (_rotationTween != null && _rotationTween.isRunning)
			{
				_rotationTween.Stop();
				_rotationTween.Recycle();
			}

			_rotationTween = experimental.animation.Start(0f, 1f, Mathf.Max(1, (int) (1000 / Mathf.Abs(rotationsPerSecond))),
														  (ve, percent) =>
														  {
															  ve.style.rotate =
																  new Rotate(Mathf.Sign(rotationsPerSecond) * 360 * percent);
														  });

			_rotationTween.Ease(Easing.Linear);
			_rotationTween.KeepAlive();
			_rotationTween.OnCompleted(() => { _rotationTween.Start(); });
		}

		private void AnimateRandomPosition()
		{
			if (_randPosTween != null && _randPosTween.isRunning)
			{
				_randPosTween.Stop();
				_randPosTween.Recycle();
			}

			var randomPos = new Vector2(Random.Range(randPosMinX, randPosMaxX), Random.Range(randPosMinY, randPosMaxY));

			_randPosTween = experimental.animation.Start(0f, 1f, Mathf.Max(1, randPosDurationMs), (ve, percent) =>
			{
				var lerpNewX = Mathf.Lerp(ve.resolvedStyle.translate.x, randomPos.x, percent / Mathf.Max(Mathf.Abs(randPosLerpFactor), 0.0001f));
				var lerpNewY = Mathf.Lerp(ve.resolvedStyle.translate.y, randomPos.y, percent / Mathf.Max(Mathf.Abs(randPosLerpFactor), 0.0001f));
				ve.style.translate = new Translate(lerpNewX, lerpNewY);
			});

			_randPosTween.Ease(Easing.Linear);
			_randPosTween.KeepAlive();
			_randPosTween.OnCompleted(() =>
			{
				randomPos = new Vector2(Random.Range(randPosMinX, randPosMaxX), Random.Range(randPosMinY, randPosMaxY));
				_randPosTween.Start();
			});
		}

		private void AnimateSineWavePosition()
		{
			if (_sinewavePosTween != null && _sinewavePosTween.isRunning)
			{
				_sinewavePosTween.Stop();
				_sinewavePosTween.Recycle();
			}

			_sinewavePosTween = experimental.animation.Start(0f, 1f, 9999999, (ve, percent) =>
			{
				var sinX = Mathf.Sin((Time.realtimeSinceStartup * sineWavePosXFrequency) + sineWavePosXOffset);
				var sinY = Mathf.Sin((Time.realtimeSinceStartup * sineWavePosYFrequency) + sineWavePosYOffset);
				var normX = (sinX - -1) / (1 - -1);
				var normY = (sinY - -1) / (1 - -1);
				var lerpNewX = Mathf.Lerp(sineWavePosMinX, sineWavePosMaxX, normX);
				var lerpNewY = Mathf.Lerp(sineWavePosMinY, sineWavePosMaxY, normY);

				ve.style.translate = new Translate(lerpNewX, lerpNewY);
			});

			_sinewavePosTween.Ease(Easing.Linear);
			_sinewavePosTween.KeepAlive();
			_sinewavePosTween.OnCompleted(() => { _sinewavePosTween.Start(); });
		}

		private void AnimateSineWaveScale()
		{
			if (_sinewaveScaleTween != null && _sinewaveScaleTween.isRunning)
			{
				_sinewaveScaleTween.Stop();
				_sinewaveScaleTween.Recycle();
			}

			_sinewaveScaleTween = experimental.animation.Start(0f, 1f, 9999999, (ve, percent) =>
			{
				var sinX = Mathf.Sin(Time.realtimeSinceStartup * sineWaveScaleFrequency);
				var normX = (sinX - -1) / (1 - -1); // Wat
				var lerpNewX = Mathf.Lerp(sineWaveScaleMin, sineWaveScaleMax, normX);

				ve.style.scale = new Scale(new Vector3(lerpNewX, lerpNewX, lerpNewX));
			});

			_sinewaveScaleTween.Ease(Easing.Linear);
			_sinewaveScaleTween.KeepAlive();
			_sinewaveScaleTween.OnCompleted(() => { _sinewaveScaleTween.Start(); });
		}
	}
}
