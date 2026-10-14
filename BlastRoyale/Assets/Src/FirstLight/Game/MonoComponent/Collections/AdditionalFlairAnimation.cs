using System;
using Cysharp.Threading.Tasks;
using FirstLight.Game.MonoComponent.Collections;
using FirstLight.Game.Utils;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace FirstLight.Game.Configs.Collection
{
	/// <summary>
	/// Spawns a flair object and plays its clip alongside the character's animation.
	/// </summary>
	public class AdditionalFlairAnimation : MonoBehaviour
	{
		[SerializeField] private AssetReferenceGameObject _assetReference = null;
		[SerializeField] public AnimationClip _animationClip;
		private readonly OneShotAnimationPlayer _clipPlayer = new();
		private CharacterSkinMonoComponent _characterSkin;
		private Animator _animator;
		private GameObject _animationObject;

		private async void Start()
		{
			_characterSkin = GetComponent<CharacterSkinMonoComponent>();
			if (_characterSkin) _characterSkin.OnTriggerFlair += OnOnTriggerFlair;
			try
			{
				var animationObject = await _assetReference.InstantiateAsync(transform, false).ToUniTask();
				if (!this)
				{
					Addressables.ReleaseInstance(animationObject);
					return;
				}
				_animationObject = animationObject;
				_animator = animationObject.GetComponentInChildren<Animator>(true);
				_animationObject.SetActive(false);
				if (!_animator) Debug.LogError("Flair animation prefab requires an Animator.", this);
			}
			catch (Exception exception)
			{
				if (this) Debug.LogException(exception, this);
			}
		}

		private void LateUpdate()
		{
			_clipPlayer.Update(Time.deltaTime);
		}

		private void OnDisable()
		{
			_clipPlayer.Stop();
			if (_animationObject) _animationObject.SetActive(false);
		}

		private void OnDestroy()
		{
			if (_characterSkin) _characterSkin.OnTriggerFlair -= OnOnTriggerFlair;
			_clipPlayer.Dispose();
			if (_animationObject) Addressables.ReleaseInstance(_animationObject);
		}

		private void OnOnTriggerFlair()
		{
			TriggerAnimationAndDisable();
		}

		public void TriggerAnimationAndDisable()
		{
			if (!isActiveAndEnabled || !_animationObject || !_animator || !_animationClip) return;
			_animationObject.SetActive(true);
			_clipPlayer.Play(_animator, _animationClip, () =>
			{
				if (_animationObject) _animationObject.SetActive(false);
			});
		}
	}
}
