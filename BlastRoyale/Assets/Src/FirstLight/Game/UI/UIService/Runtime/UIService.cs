using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using GameLovers.UiService;
using UnityEngine;
using UnityEngine.UIElements;

namespace FirstLight.Game.UI
{
    /// <summary>Toolkit screen adapter; UPM owns loading, visibility and unloading.</summary>
    public sealed class UIService : IDisposable
    {
        public const string ID_ROOT = "root";
        public const string CLASS_HIDDEN = "hidden";
        public const string SFX_CLICK_FORWARDS = "sfx-click-forwards";
        public const string SFX_CLICK_BACKWARDS = "sfx-click-backwards";
        public const string USS_PLAYER_LABEL = "player-name";
        public delegate void OnScreenOpenedDelegate(string screenName, string layerName);
        public event OnScreenOpenedDelegate OnScreenOpened;

        private readonly GameLovers.UiService.UiService _lifecycle = new GameLovers.UiService.UiService();
        private readonly AddressablesUiConfigs _configs;
        private readonly Dictionary<Type, Screen> _screens = new();
        private readonly Dictionary<UILayer, SemaphoreSlim> _layerGates = new();
        private int _nextInstance;
        private bool _disposed;

        private sealed class Screen
        {
            public Type Type;
            public UIPresenter Presenter;
            public string Address;
        }
        private readonly HashSet<Type> _registered = new();

        public UIService()
        {
            _configs = ScriptableObject.CreateInstance<AddressablesUiConfigs>();
            _configs.Configs = new List<UiConfig>();
            _lifecycle.Init(_configs);
        }

        public async UniTask<UIPresenter> OpenScreen(Type type, object data = null)
        {
            var layer = type.GetAttribute<UILayerAttribute>()?.Layer ?? UILayer.Default;
            var gate = GetLayerGate(layer);
            var closing = new List<UniTask>();
            Screen screen = null;
            await gate.WaitAsync().AsUniTask();
            try
            {
                ThrowIfDisposed();
                if (_screens.TryGetValue(type, out var existing)) return existing.Presenter;
                // Admission and native loading are serialized per layer. Game callbacks are
                // started here but awaited only after releasing the gate: they may open a screen.
                BeginCloseLayer(layer, false, closing);
                if (_registered.Add(type))
                {
                    _lifecycle.AddUiConfig(new UiConfig
                    {
                        UiType = type,
                        Address = $"UI/{type.Name.Replace("Presenter", "")}.prefab",
                        Layer = (int)layer,
                        LoadSynchronously = false
                    });
                }
                // Native instance identity must differ from its prefab address. A close callback
                // can reopen the same type while its previous instance finishes closing.
                var address = $"game-screen-{++_nextInstance}";
                var presenter = (UIPresenter)await _lifecycle.LoadUiAsync(type, address);
                screen = new Screen { Type = type, Presenter = presenter, Address = address };
                presenter.Layer = layer;
                presenter.Data = data;
                presenter.IsCurrentScreen = () => IsCurrent(screen);
                if (presenter.TryGetComponent<UIDocument>(out var document))
                    document.sortingOrder = (int)layer;
                _screens.Add(type, screen);
                await _lifecycle.OpenUiAsync(type, address);
                await presenter.OpenTransitionTask;
            }
            catch
            {
                if (screen != null && IsCurrent(screen))
                {
                    _screens.Remove(type);
                    _lifecycle.UnloadUi(type, screen.Address);
                }
                // Started close operations still need their errors observed when loading fails.
                foreach (var close in closing) close.Forget();
                throw;
            }
            finally { gate.Release(); }

            try
            {
                foreach (var close in closing) await close;
                if (!IsCurrent(screen)) throw new OperationCanceledException("Screen was replaced while opening.");
                await screen.Presenter.OnScreenOpenedInternal(uiService: this);
                if (!IsCurrent(screen)) throw new OperationCanceledException("Screen was replaced by its open callback.");
                OnScreenOpened?.Invoke(type.Name.Replace("Presenter", ""), layer.ToString());
                return screen.Presenter;
            }
            catch
            {
                // A newer same-type instance must never be removed by this older completion.
                if (IsCurrent(screen)) await CloseScreen(type, false);
                throw;
            }
        }

        public UniTask CloseScreen(Type type, bool checkOpened = true) => CloseScreenInternal(type, null);

        internal UniTask CloseScreen(UIPresenter presenter) => CloseScreenInternal(presenter.GetType(), presenter);

        private async UniTask CloseScreenInternal(Type type, UIPresenter expectedPresenter)
        {
            var layer = type.GetAttribute<UILayerAttribute>()?.Layer ?? UILayer.Default;
            var gate = GetLayerGate(layer);
            var close = UniTask.CompletedTask;
            await gate.WaitAsync().AsUniTask();
            try
            {
                ThrowIfDisposed();
                if (_screens.TryGetValue(type, out var screen) &&
                    (expectedPresenter == null || ReferenceEquals(expectedPresenter, screen.Presenter)))
                    close = BeginClose(screen);
            }
            finally { gate.Release(); }
            await close;
        }

        public async UniTask CloseLayer(UILayer layer, bool useAutoClose = false)
        {
            var gate = GetLayerGate(layer);
            var closing = new List<UniTask>();
            await gate.WaitAsync().AsUniTask();
            try
            {
                ThrowIfDisposed();
                BeginCloseLayer(layer, useAutoClose, closing);
            }
            finally { gate.Release(); }
            foreach (var close in closing) await close;
        }

        private void BeginCloseLayer(UILayer layer, bool useAutoClose, List<UniTask> closing)
        {
            var close = new List<Screen>();
            foreach (var screen in _screens.Values)
            {
                if (screen.Presenter.Layer != layer) continue;
                if (useAutoClose && screen.Type.GetAttribute<UILayerAttribute>()?.AutoClose == false) continue;
                close.Add(screen);
            }
            foreach (var screen in close) closing.Add(BeginClose(screen));
        }

        private UniTask BeginClose(Screen screen)
        {
            _screens.Remove(screen.Type);
            return FinishClose(screen);
        }

        private async UniTask FinishClose(Screen screen)
        {
            try
            {
                await screen.Presenter.OnScreenClosedInternal();
                if (_disposed) return;
                _lifecycle.CloseUi(screen.Type, screen.Address);
                await screen.Presenter.CloseTransitionTask;
            }
            finally
            {
                if (!_disposed) _lifecycle.UnloadUi(screen.Type, screen.Address);
            }
        }

        private bool IsCurrent(Screen screen) => !_disposed && screen != null &&
            _screens.TryGetValue(screen.Type, out var current) && ReferenceEquals(current, screen);

        private SemaphoreSlim GetLayerGate(UILayer layer)
        {
            if (!_layerGates.TryGetValue(layer, out var gate))
                _layerGates.Add(layer, gate = new SemaphoreSlim(1, 1));
            return gate;
        }

        private void ThrowIfDisposed()
        {
            if (_disposed) throw new ObjectDisposedException(nameof(UIService));
        }

        public bool HasUIPresenterOpenOnLayer(UILayer layer)
        {
            foreach (var presenter in _screens.Values)
                if (presenter.Presenter.Layer == layer) return true;
            return false;
        }

        public T GetScreen<T>() where T : UIPresenter
        {
            if (_screens.TryGetValue(typeof(T), out var presenter)) return (T)presenter.Presenter;
            throw new InvalidOperationException($"Screen {typeof(T).Name} is not opened!");
        }
        public bool IsScreenOpen<T>() where T : UIPresenter => _screens.ContainsKey(typeof(T));
        public async UniTask<T> OpenScreen<T>(object data = null) where T : UIPresenter =>
            (T)await OpenScreen(typeof(T), data);
        public UniTask CloseScreen<T>(bool checkOpened = true) where T : UIPresenter => CloseScreen(typeof(T), checkOpened);

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            _screens.Clear();
            _lifecycle.Dispose();
            UnityEngine.Object.Destroy(_configs);
        }
    }
}
