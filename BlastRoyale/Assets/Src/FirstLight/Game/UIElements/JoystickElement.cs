using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace FirstLight.Game.UIElements
{
	/// <summary>
	/// Displays an interactive Joystick. Joystick pointer events are captured from it's parent.
	/// </summary>
	[UxmlElement]
	public partial class JoystickElement : VisualElement
	{
		private const string UssBlock = "joystick";
		private const string UssFree = UssBlock + "--free";
		private const string UssBgCircle = UssBlock + "__bg-circle";
		private const string UssStick = UssBlock + "__stick";
		private const string UssStickFree = UssStick + "--free";
		private const string UssDirectionHalo = UssBlock + "__direction-halo";

		private const string UssMatchButton = "match-button";
		private const string UssMatchButtonBgCircle = "match-button__bg-circle";

		private readonly VisualElement _stick;
		private readonly VisualElement _directionHalo;
		private Vector3 _initialPosition;
		private int? _pointerId;
		private VisualElement _inputSurface;

		public event Action<Vector2> OnMove;
		public event Action<float> OnClick; // Float so that we can use it directly with the input system

		public JoystickElement()
		{
			AddToClassList(UssBlock);
			AddToClassList(UssMatchButton);

			var bgCircle = new VisualElement {name = "bg-circle"};
			Add(bgCircle);
			bgCircle.AddToClassList(UssMatchButtonBgCircle);
			bgCircle.AddToClassList(UssBgCircle);

			Add(_directionHalo = new VisualElement {name = "direction-vfx"});
			_directionHalo.AddToClassList(UssDirectionHalo);
			_directionHalo.usageHints =
				UsageHints.DynamicTransform; // TODO: This could be added / removed in  PointerDown / PointerUp

			Add(_stick = new VisualElement {name = "stick"});
			_stick.AddToClassList(UssStick);
			_stick.usageHints = UsageHints.DynamicTransform;

			if (Application.isPlaying)
			{
				RegisterCallback<AttachToPanelEvent>(OnAttachToPanel);
				RegisterCallback<DetachFromPanelEvent>(OnDetachFromPanel);
			}
		}

		private void OnAttachToPanel(AttachToPanelEvent evt)
		{
			_inputSurface = parent;
			_inputSurface.RegisterCallback<PointerDownEvent>(OnPointerDown);
			_inputSurface.RegisterCallback<PointerMoveEvent>(OnPointerMove);
			_inputSurface.RegisterCallback<PointerUpEvent>(OnPointerUp);
			_inputSurface.RegisterCallback<PointerCancelEvent>(OnPointerCancel);
			_inputSurface.RegisterCallback<PointerCaptureOutEvent>(OnPointerCaptureOut);
		}

		private void OnDetachFromPanel(DetachFromPanelEvent evt)
		{
			RemoveListeners();
		}

		public void RemoveListeners()
		{
			if (_inputSurface == null) return;
			ResetPointer();
			_inputSurface.UnregisterCallback<PointerDownEvent>(OnPointerDown);
			_inputSurface.UnregisterCallback<PointerMoveEvent>(OnPointerMove);
			_inputSurface.UnregisterCallback<PointerUpEvent>(OnPointerUp);
			_inputSurface.UnregisterCallback<PointerCancelEvent>(OnPointerCancel);
			_inputSurface.UnregisterCallback<PointerCaptureOutEvent>(OnPointerCaptureOut);
			_inputSurface = null;
		}

		private void OnPointerDown(PointerDownEvent evt)
		{
			if (_pointerId.HasValue) return;
			_pointerId = evt.pointerId;
			_initialPosition = resolvedStyle.translate;
			_inputSurface.CapturePointer(evt.pointerId);

			AddToClassList(UssFree);
			_stick.AddToClassList(UssStickFree);

			var parentPosition = _inputSurface.WorldToLocal(evt.position);
			var offsetPosition = parentPosition - new Vector2(worldBound.width / 2f, worldBound.height / 2f);

			style.translate = new Translate(offsetPosition.x, offsetPosition.y);

			OnClick?.Invoke(1f);
		}

		private void OnPointerMove(PointerMoveEvent evt)
		{
			if (_pointerId != evt.pointerId || !_inputSurface.HasPointerCapture(evt.pointerId)) return;

			var parentPosition = _inputSurface.WorldToLocal(evt.position);
			var offsetPosition = parentPosition - new Vector2(worldBound.width / 2f, worldBound.height / 2f);

			var stickPosition = offsetPosition - (Vector2) resolvedStyle.translate;
			var stickPositionClamped = Vector2.ClampMagnitude(stickPosition, worldBound.width / 2f);
			var stickPositionClampedNormalized = stickPositionClamped.normalized;

			_stick.style.translate = new Translate(stickPositionClamped.x, stickPositionClamped.y);
			_directionHalo.style.rotate = VectorToRotation(stickPositionClampedNormalized);

			_directionHalo.style.opacity = stickPositionClamped.magnitude / (worldBound.width / 2f);

			stickPositionClampedNormalized.y = -stickPositionClampedNormalized.y;
			OnMove?.Invoke(stickPositionClampedNormalized);
		}

		private void OnPointerUp(PointerUpEvent evt)
		{
			if (_pointerId == evt.pointerId) ResetPointer();
		}

		private void OnPointerCancel(PointerCancelEvent evt)
		{
			if (_pointerId == evt.pointerId) ResetPointer();
		}

		private void OnPointerCaptureOut(PointerCaptureOutEvent evt)
		{
			if (_pointerId == evt.pointerId) ResetPointer();
		}

		private void ResetPointer()
		{
			if (!_pointerId.HasValue) return;
			var pointerId = _pointerId.Value;
			_pointerId = null;
			_inputSurface.ReleasePointer(pointerId);

			RemoveFromClassList(UssFree);
			_stick.RemoveFromClassList(UssStickFree);

			style.translate = new Translate(_initialPosition.x, _initialPosition.y, _initialPosition.z);
			_stick.style.translate = new Translate(0, 0);
			_directionHalo.style.opacity = 0f;

			OnMove?.Invoke(Vector3.zero);
			OnClick?.Invoke(0f);
		}

		private static Rotate VectorToRotation(Vector2 direction)
		{
			var angleInRadians = Mathf.Atan2(direction.y, direction.x);
			var angleInDegrees = angleInRadians * Mathf.Rad2Deg;

			// Normalize the angle to be between 0 and 360 degrees
			if (angleInDegrees < 0)
			{
				angleInDegrees += 360;
			}

			return new Rotate(Angle.Degrees(angleInDegrees + 90));
		}

	}
}
