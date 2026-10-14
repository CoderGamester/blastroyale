using System;
using System.Reflection;
using DG.Tweening;
using FirstLight.Game.Utils;
using FirstLight.Game.Views;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace FirstLight.Tests.EditorMode.FreeAnimation
{
	public class FreeAnimationTests
	{
		private GameObject _root;
		private AnimationClip _clip;
		private OneShotAnimationPlayer _player;

		[TearDown]
		public void TearDown()
		{
			_player?.Dispose();
			DOTween.KillAll();
			if (_root) Object.DestroyImmediate(_root);
			if (_clip) Object.DestroyImmediate(_clip);
		}

		private Transform CreateAnimation()
		{
			_root = new GameObject("Flair root", typeof(Animator));
			var animated = new GameObject("Animated").transform;
			animated.SetParent(_root.transform);
			_clip = new AnimationClip();
			AnimationUtility.SetEditorCurve(_clip, EditorCurveBinding.FloatCurve("Animated", typeof(Transform), "m_LocalPosition.x"), AnimationCurve.Linear(0, 0, 1, 2));
			_player = new OneShotAnimationPlayer();
			return animated;
		}

		[Test]
		public void NativeGraphSamplesAuthoredClipAndCompletesOnce()
		{
			var animated = CreateAnimation();
			var count = 0;
			_player.Play(_root.GetComponent<Animator>(), _clip, () => count++);
			_player.Update(0.25f);
			Assert.That(animated.localPosition.x, Is.EqualTo(0.5f).Within(0.001f));
			Assert.That(count, Is.Zero);
			_player.Update(0.8f);
			_player.Update(1);
			Assert.That(count, Is.EqualTo(1));
		}

		[Test]
		public void RetriggerRestartsClipAndReplacesPreviousCompletion()
		{
			var animated = CreateAnimation();
			var original = 0;
			var retrigger = 0;
			_player.Play(_root.GetComponent<Animator>(), _clip, () => original++);
			_player.Update(0.75f);
			_player.Play(_root.GetComponent<Animator>(), _clip, () => retrigger++);
			Assert.That(animated.localPosition.x, Is.Zero.Within(0.001f));
			_player.Update(0.5f);
			Assert.That(animated.localPosition.x, Is.EqualTo(1).Within(0.001f));
			_player.Update(0.6f);
			Assert.That(original, Is.Zero);
			Assert.That(retrigger, Is.EqualTo(1));
		}

		[Test]
		public void StopCancelsCompletionAndCanPlayAgain()
		{
			CreateAnimation();
			var count = 0;
			_player.Play(_root.GetComponent<Animator>(), _clip, () => count++);
			_player.Update(0.5f);
			_player.Stop();
			_player.Update(1);
			Assert.That(count, Is.Zero);
			_player.Play(_root.GetComponent<Animator>(), _clip, () => count++);
			_player.Update(1.1f);
			Assert.That(count, Is.EqualTo(1));
		}

		[Test]
		public void ZeroAndNegativeDeltaDoNotAdvanceAnimation()
		{
			var animated = CreateAnimation();
			_player.Play(_root.GetComponent<Animator>(), _clip, null);
			_player.Update(0);
			_player.Update(-1);
			Assert.That(animated.localPosition.x, Is.Zero.Within(0.001f));
		}

		[Test]
		public void DestroyedAnimatorCancelsCompletion()
		{
			CreateAnimation();
			var count = 0;
			_player.Play(_root.GetComponent<Animator>(), _clip, () => count++);
			Object.DestroyImmediate(_root);
			_player.Update(2);
			Assert.That(count, Is.Zero);
		}

		private UiVfxImage CreateVfx()
		{
			var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/VfxSprite.prefab");
			Assert.That(prefab, Is.Not.Null);
			_root = Object.Instantiate(prefab);
			return _root.GetComponent<UiVfxImage>();
		}

		private static T Field<T>(UiVfxImage image, string name)
			=> (T) typeof(UiVfxImage).GetField(name, BindingFlags.Instance | BindingFlags.NonPublic).GetValue(image);

		private static void ManualUpdate(UiVfxImage image, float time)
		{
			Field<Tween>(image, "_moveTween")?.SetUpdate(UpdateType.Manual);
			Field<Tween>(image, "_scaleTween")?.SetUpdate(UpdateType.Manual);
			DOTween.ManualUpdate(time, time);
		}

		[Test]
		public void PrefabRetainsDistinctAuthoredCurvesAndTiming()
		{
			var image = CreateVfx();
			Assert.That(_root.GetComponents<MonoBehaviour>().Length, Is.EqualTo(2)); // Image + game VFX
			Assert.That(Field<float>(image, "_moveDuration"), Is.EqualTo(1));
			Assert.That(Field<float>(image, "_scaleDuration"), Is.EqualTo(1));
			Assert.That(Field<float>(image, "_endScale"), Is.EqualTo(2.5f));
			var move = Field<AnimationCurve>(image, "_moveEase");
			var scale = Field<AnimationCurve>(image, "_scaleEase");
			Assert.That(move.keys.Length, Is.EqualTo(3));
			Assert.That(move.keys[1].time, Is.EqualTo(0.787243f));
			Assert.That(scale.keys.Length, Is.EqualTo(4));
			Assert.That(scale.keys[1].time, Is.EqualTo(0.3738148f));
		}

		[Test]
		public void VfxWaitsForDelayThenUsesAuthoredMovementAndScale()
		{
			var image = CreateVfx();
			var count = 0;
			image.Play(null, 0.2f, Vector3.zero, new Vector3(10, 5, 0), () => count++);
			Assert.That(_root.GetComponent<Image>().enabled, Is.False);
			ManualUpdate(image, 0.1f);
			Assert.That(_root.GetComponent<Image>().enabled, Is.False);
			ManualUpdate(image, 0.6f);
			Assert.That(_root.GetComponent<Image>().enabled, Is.True);
			Assert.That(_root.transform.position.x, Is.EqualTo(10 * DOVirtual.EasedValue(0, 1, 0.5f, Field<AnimationCurve>(image, "_moveEase"))).Within(0.001f));
			Assert.That(_root.transform.localScale.x, Is.EqualTo(2.5f * DOVirtual.EasedValue(0, 1, 0.5f, Field<AnimationCurve>(image, "_scaleEase"))).Within(0.001f));
			Assert.That(count, Is.Zero);
			ManualUpdate(image, 0.6f);
			DOTween.ManualUpdate(2, 2);
			Assert.That(count, Is.EqualTo(1));
		}

		[Test]
		public void VfxRetriggerCancelsPreviousCallbackAndResetsOrigin()
		{
			var image = CreateVfx();
			var original = 0;
			var retrigger = 0;
			image.Play(null, 0, Vector3.zero, Vector3.one, () => original++);
			ManualUpdate(image, 0.5f);
			Assert.That(original, Is.Zero, "Original completed during first half");
			image.Play(null, 0, new Vector3(4, 3, 0), new Vector3(8, 6, 0), () => retrigger++);
			Assert.That(DOTween.TotalActiveTweens(), Is.EqualTo(2), "Retrigger must replace both previous tweens");
			Assert.That(original, Is.Zero, "Original completed while retrigger killed previous tween");
			Assert.That(_root.transform.position, Is.EqualTo(new Vector3(4, 3, 0)));
			Assert.That(_root.transform.localScale, Is.EqualTo(Vector3.zero));
			ManualUpdate(image, 1.1f);
			Assert.That(original, Is.Zero);
			Assert.That(retrigger, Is.EqualTo(1));
		}

		[Test]
		public void VfxDisableCancelsBothTweensWithoutCompletion()
		{
			var image = CreateVfx();
			var count = 0;
			image.Play(null, 0, Vector3.zero, Vector3.one, () => count++);
			ManualUpdate(image, 0.5f);
			_root.SetActive(false);
			DOTween.ManualUpdate(2, 2);
			Assert.That(count, Is.Zero);
			Assert.That(Field<Tween>(image, "_moveTween"), Is.Null);
			Assert.That(Field<Tween>(image, "_scaleTween"), Is.Null);
		}

		[Test]
		public void VfxCompletionCanReturnObjectToPoolWithoutLiveScaleTween()
		{
			var image = CreateVfx();
			var count = 0;
			image.Play(null, 0, Vector3.zero, Vector3.one, () => { count++; _root.SetActive(false); });
			ManualUpdate(image, 1.1f);
			DOTween.ManualUpdate(1, 1);
			Assert.That(count, Is.EqualTo(1));
			Assert.That(_root.activeSelf, Is.False);
			Assert.That(Field<Tween>(image, "_moveTween"), Is.Null);
			Assert.That(Field<Tween>(image, "_scaleTween"), Is.Null);
		}
	}
}
