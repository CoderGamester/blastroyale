using System.Collections;
using System.Collections.Generic;
using FirstLight.Game.UIElements;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;

namespace FirstLight.Tests.PlayMode
{
	public class JoystickPointerLifecycleTests
	{
		private static readonly int Owner = PointerId.touchPointerIdBase;
		private static readonly int Other = PointerId.touchPointerIdBase + 1;
		private readonly List<float> _clicks = new ();
		private readonly List<Vector2> _moves = new ();
		private GameObject _documentObject;
		private PanelSettings _panelSettings;
		private VisualElement _host;
		private JoystickElement _joystick;

		[UnitySetUp]
		public IEnumerator SetUp()
		{
			_clicks.Clear();
			_moves.Clear();
			_panelSettings = ScriptableObject.CreateInstance<PanelSettings>();
			_panelSettings.scaleMode = PanelScaleMode.ConstantPixelSize;
			_documentObject = new GameObject(nameof(JoystickPointerLifecycleTests));
			var document = _documentObject.AddComponent<UIDocument>();
			document.panelSettings = _panelSettings;
			_host = new VisualElement();
			_host.style.width = 300;
			_host.style.height = 300;
			_joystick = new JoystickElement();
			_joystick.style.width = 100;
			_joystick.style.height = 100;
			_joystick.OnClick += _clicks.Add;
			_joystick.OnMove += _moves.Add;
			_host.Add(_joystick);
			document.rootVisualElement.Add(_host);
			yield return null;
			yield return null;
			Assert.That(_host.panel, Is.Not.Null);
			Assert.That(_joystick.worldBound.width, Is.GreaterThan(0));
		}

		[UnityTearDown]
		public IEnumerator TearDown()
		{
			Object.Destroy(_documentObject);
			yield return null;
			Object.Destroy(_panelSettings);
			yield return null;
		}

		[UnityTest]
		public IEnumerator Drag_AnotherPointerPressesAndReleases_OwnerKeepsControl()
		{
			var origin = (Vector3)_host.worldBound.center;
			Down(Owner, origin);
			yield return null;
			Assert.That(_host.HasPointerCapture(Owner), Is.True);
			Move(Owner, origin + Vector3.right * 30);
			Assert.That(_moves.Count, Is.GreaterThan(0));
			Assert.That(_moves[_moves.Count - 1].x, Is.GreaterThan(0));
			var count = _moves.Count;
			Down(Other, origin + Vector3.up * 40);
			Up(Other, origin);
			yield return null;
			Assert.That(_host.HasPointerCapture(Owner), Is.True);
			Assert.That(_host.HasPointerCapture(Other), Is.False);
			Assert.That(_clicks, Is.EqualTo(new[] {1f}));
			Assert.That(_moves.Count, Is.EqualTo(count));
			Up(Owner, origin);
			yield return null;
			Assert.That(_clicks, Is.EqualTo(new[] {1f, 0f}));
			Assert.That(_moves[_moves.Count - 1], Is.EqualTo(Vector2.zero));
			Assert.That(_host.HasPointerCapture(Owner), Is.False);
		}

		[UnityTest]
		public IEnumerator Drag_PointerCanceled_ResetsOnceAndAcceptsNextPointer()
		{
			var origin = (Vector3)_host.worldBound.center;
			Down(Owner, origin);
			yield return null;
			Move(Owner, origin + Vector3.right * 30);
			using (var pointerEvent = PointerCancelEvent.GetPooled(new PointerSource(Owner, origin)))
			{
				pointerEvent.target = _host;
				_host.SendEvent(pointerEvent);
			}
			yield return null;
			Assert.That(_clicks, Is.EqualTo(new[] {1f, 0f}));
			Assert.That(_moves[_moves.Count - 1], Is.EqualTo(Vector2.zero));
			Assert.That(_host.HasPointerCapture(Owner), Is.False);
			Up(Owner, origin);
			Assert.That(_clicks, Is.EqualTo(new[] {1f, 0f}));
			Down(Other, origin);
			yield return null;
			Assert.That(_host.HasPointerCapture(Other), Is.True);
			Assert.That(_clicks, Is.EqualTo(new[] {1f, 0f, 1f}));
		}

		[UnityTest]
		public IEnumerator Drag_PointerCaptureReleased_ResetsWithoutPointerUp()
		{
			var origin = (Vector3)_host.worldBound.center;
			Down(Owner, origin);
			yield return null;
			Move(Owner, origin + Vector3.right * 30);
			var captureOutEvents = 0;
			_host.RegisterCallback<PointerCaptureOutEvent>(pointerEvent =>
			{
				if (pointerEvent.pointerId == Owner) captureOutEvents++;
			});
			_host.ReleasePointer(Owner);
			// Unity commits pending capture changes in PointerEventBase.PostDispatch, not on the next frame.
			Move(Owner, origin + Vector3.right * 30);
			yield return null;
			Assert.That(captureOutEvents, Is.EqualTo(1));
			Assert.That(_host.HasPointerCapture(Owner), Is.False);
			Assert.That(_clicks, Is.EqualTo(new[] {1f, 0f}));
			Assert.That(_moves[_moves.Count - 1], Is.EqualTo(Vector2.zero));
		}

		[UnityTest]
		public IEnumerator Drag_JoystickDetached_ResetsAndReattachmentRemainsUsable()
		{
			var origin = (Vector3)_host.worldBound.center;
			Down(Owner, origin);
			yield return null;
			Move(Owner, origin + Vector3.right * 30);
			_joystick.RemoveFromHierarchy();
			yield return null;
			Assert.That(_clicks, Is.EqualTo(new[] {1f, 0f}));
			Assert.That(_moves[_moves.Count - 1], Is.EqualTo(Vector2.zero));
			Assert.That(_host.HasPointerCapture(Owner), Is.False);
			_host.Add(_joystick);
			yield return null;
			Down(Other, origin);
			yield return null;
			Assert.That(_host.HasPointerCapture(Other), Is.True);
			Up(Other, origin);
			yield return null;
			Assert.That(_clicks, Is.EqualTo(new[] {1f, 0f, 1f, 0f}));
		}

		private void Down(int pointer, Vector3 position)
		{
			using var pointerEvent = PointerDownEvent.GetPooled(new PointerSource(pointer, position));
			pointerEvent.target = _host;
			_host.SendEvent(pointerEvent);
		}

		private void Move(int pointer, Vector3 position)
		{
			using var pointerEvent = PointerMoveEvent.GetPooled(new PointerSource(pointer, position));
			pointerEvent.target = _host;
			_host.SendEvent(pointerEvent);
		}

		private void Up(int pointer, Vector3 position)
		{
			using var pointerEvent = PointerUpEvent.GetPooled(new PointerSource(pointer, position));
			pointerEvent.target = _host;
			_host.SendEvent(pointerEvent);
		}

		private readonly struct PointerSource : IPointerEvent
		{
			public int pointerId { get; }
			public Vector3 position { get; }
			public string pointerType => UnityEngine.UIElements.PointerType.touch;
			public bool isPrimary => pointerId == Owner;
			public int button => 0;
			public int pressedButtons => 1;
			public Vector3 localPosition => position;
			public Vector3 deltaPosition => Vector3.zero;
			public float deltaTime => 0;
			public int clickCount => 1;
			public float pressure => 1;
			public float tangentialPressure => 0;
			public float altitudeAngle => 0;
			public float azimuthAngle => 0;
			public float twist => 0;
			public Vector2 tilt => Vector2.zero;
			public PenStatus penStatus => PenStatus.None;
			public Vector2 radius => Vector2.zero;
			public Vector2 radiusVariance => Vector2.zero;
			public EventModifiers modifiers => EventModifiers.None;
			public bool shiftKey => false;
			public bool ctrlKey => false;
			public bool commandKey => false;
			public bool altKey => false;
			public bool actionKey => false;

			public PointerSource(int pointer, Vector3 pointerPosition)
			{
				pointerId = pointer;
				position = pointerPosition;
			}
		}
	}
}
