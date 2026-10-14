using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using FirstLight.Game.UI;
using QuickEye.UIToolkit;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.UIElements;

namespace FirstLight.Game.UI
{
	[RequireComponent(typeof(UIDocument))]
	public abstract class UIPresenter : GameLovers.UiService.UiPresenter
	{
		// TODO: Add Required attribute when no more legacy screens are present
		[SerializeField] private UIDocument _document;

		public VisualElement Root { private set; get; }

		internal UILayer Layer { set; get; }
		internal object Data { set; get; }
		internal Func<bool> IsCurrentScreen { set; get; }

		/// <summary>
		/// Set by the UIService when opening the screen
		/// </summary>
		protected UIService Service;

		private readonly List<UIView> _views = new ();
		private bool _enableTriggered;
		private CancellationTokenSource _cancellationTokenSource;
		private List<AssetReference> _dynamicUsedAssets = new ();
		private bool _closed = false;

		protected override void OnInitialized()
		{
			base.OnInitialized();
			// Legacy Canvas screens deliberately leave this reference unset. RequireComponent
			// also adds an empty UIDocument to those prefabs; it is not a Toolkit screen.
			if (_document == null && TryGetComponent<UIDocument>(out var document) &&
				document.visualTreeAsset != null)
			{
				_document = document;
			}
		}

		private void OnEnable()
		{
			// Support for live reload, we don't trigger on first enable as that is covered by the UIService
			if (Service == null || !_enableTriggered)
			{
				_enableTriggered = true;
				return;
			}

			OnScreenOpenedInternal(true).Forget();
		}

		private void OnValidate()
		{
			if (_document != null)
			{
				// Not really necessary as we set this at runtime but just so it's clear in the editor
				_document.sortingOrder = (int) (GetType().GetAttribute<UILayerAttribute>()?.Layer ?? UILayer.Default);
			}
		}

		/// <summary>
		/// Adds a <see cref="UIView"/> view to the list of views, and handles it's lifecycle events.
		/// </summary>
		public void AddView(VisualElement element, UIView view)
		{
			_views.Add(view);
			view.AttachedInternal(element, this);
		}

		internal virtual async UniTask OnScreenOpenedInternal(bool reload = false, UIService uiService = null)
		{
			if (IsCurrentScreen?.Invoke() == false) return;
			Service = uiService ?? Service;
			_cancellationTokenSource?.Dispose();
			_cancellationTokenSource = new CancellationTokenSource();
			_closed = false;
			// Assert.AreEqual(typeof(T), Data.GetType(), $"Screen opened with incorrect data type {Data.GetType()} instead of {typeof(T)}");

			if (_document == null && !TryGetComponent<Canvas>(out _))
			{
				throw new InvalidOperationException($"Screen {GetType().Name} requires a configured UIDocument or a legacy Canvas.");
			}

			if (_document != null)
			{
				Root = _document.rootVisualElement?.Q(UIService.ID_ROOT) ??
					throw new InvalidOperationException($"Screen {GetType().Name} UIDocument is missing the required '{UIService.ID_ROOT}' element.");
				Root.AssignQueryResults(this);
				Root.AssignQueryViews(this, this);
				QueryElements();

				if (reload)
				{
					// We still skip a frame there so OnScreenOpened is always called on the second frame
					await UniTask.NextFrame(cancellationToken: _cancellationTokenSource.Token);
				}
				else
				{
					Root.EnableInClassList(UIService.CLASS_HIDDEN, true);

					// We need to wait for next frame to trigger the CLASS_HIDDEN transition
					await UniTask.NextFrame(cancellationToken: _cancellationTokenSource.Token);

					if (IsCurrentScreen?.Invoke() == false) return;
					Root.EnableInClassList(UIService.CLASS_HIDDEN, false);
				}

				foreach (var view in _views)
				{
					view.OnScreenOpen(reload);
				}
			}

			await OnScreenOpen(reload);
		}

		internal async UniTask OnScreenClosedInternal()
		{
			// There is concurrency if you open another same layer screen on OnScreenClosed(), when opening the new screen UIService will 
			// try to close this again
			if (_closed) return;
			_closed = true;
			_cancellationTokenSource?.Cancel();
			await OnScreenClose();
			foreach (var dynamicUsedAsset in _dynamicUsedAssets)
			{
				dynamicUsedAsset.ReleaseAsset();
			}

			foreach (var view in _views)
			{
				view.OnScreenClose();
			}

			if (_document != null)
			{
				Root?.EnableInClassList(UIService.CLASS_HIDDEN, true);
			}
		}

		public void AddAutoReleaseAsset(params AssetReference[] assetRef)
		{
			_dynamicUsedAssets.AddRange(assetRef);
		}

		/// <summary>
		/// A cancelation toke with the lifetime of the screen
		/// </summary>
		public CancellationToken GetCancellationTokenOnClose() => _cancellationTokenSource.Token;

		protected abstract void QueryElements();

		protected virtual UniTask OnScreenOpen(bool reload)
		{
			return UniTask.CompletedTask;
		}

		protected virtual UniTask OnScreenClose()
		{
			return UniTask.CompletedTask;
		}

		public UniTask Close()
		{
			return Service.CloseScreen(this);
		}
	}
}
