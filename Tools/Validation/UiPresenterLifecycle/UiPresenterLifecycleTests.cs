using System;
using System.Collections;
using System.Reflection;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using FirstLight.Game.Presenters;
using FirstLight.Game.UI;
using FirstLight.Game.Views;
using GameLovers.UiService;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;
using Object = UnityEngine.Object;
using GameUIService = FirstLight.Game.UI.UIService;

namespace FirstLight.Tests.PlayMode.UI
{
    public class UiPresenterLifecycleTests
    {
        private GameUIService _service;
        private PrefabRegistryUiAssetLoader _loader;
        private GameObject _template;
        private PanelSettings _panel;

        [SetUp]
        public void SetUp()
        {
            _service = new GameUIService();
            _loader = new PrefabRegistryUiAssetLoader();
            // The production adapter uses Addressables. Exercise its complete lifecycle with
            // the maintained package's real in-memory prefab loader, without cloud or bundles.
            var lifecycle = typeof(GameUIService).GetField("_lifecycle", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(_service);
            typeof(GameLovers.UiService.UiService).GetField("_assetLoader", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(lifecycle, _loader);
        }

        [TearDown]
        public void TearDown()
        {
            DOTween.KillAll();
            _service?.Dispose();
            if (_template) Object.DestroyImmediate(_template);
            if (_panel) Object.DestroyImmediate(_panel);
        }

        [UnityTest]
        public IEnumerator AuthoredCanvasVfxOpensAndCompletesPooledAnimation() => UniTask.ToCoroutine(async () =>
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/AddressableResources/UI/UIVFXScreen.prefab");
            Assert.That(prefab, Is.Not.Null);
            Assert.That(prefab.GetComponent<UIDocument>().visualTreeAsset, Is.Null);
            Assert.That(prefab.GetComponent<Canvas>(), Is.Not.Null);
            _loader.RegisterPrefab("UI/UIVFXScreen.prefab", prefab);
            var opened = 0;
            _service.OnScreenOpened += (_, _) => opened++;
            var presenter = await _service.OpenScreen<UIVFXScreenPresenter>();
            Assert.That(presenter.IsOpen, Is.True);
            Assert.That(_service.IsScreenOpen<UIVFXScreenPresenter>(), Is.True);
            Assert.That(presenter.Root, Is.Null, "The authored Canvas must not become an empty Toolkit screen.");
            Assert.That(opened, Is.EqualTo(1));
            Canvas.ForceUpdateCanvases();
            Assert.That(presenter.GetComponent<Canvas>().isActiveAndEnabled, Is.True);
            Assert.That(presenter.transform.lossyScale.x, Is.GreaterThan(0), "The screen must actually be visible, not just avoid a null exception.");
            var completions = 0;
            presenter.PlayAnimation(null, 0, Vector3.zero, new Vector3(20, 10, 0), () => completions++);
            // DOTween enables the image only when its first update runs.
            await UniTask.NextFrame();
            Assert.That(Array.Exists(presenter.GetComponentsInChildren<UiVfxImage>(true), item => item.GetComponent<UnityEngine.UI.Image>().isActiveAndEnabled), Is.True);
            await UniTask.Delay(TimeSpan.FromSeconds(1.4));
            Assert.That(completions, Is.EqualTo(1), "The original pooled animation completion must still run exactly once.");
            await _service.CloseScreen<UIVFXScreenPresenter>();
            Assert.That(_service.IsScreenOpen<UIVFXScreenPresenter>(), Is.False);
        });

        private void RegisterResultScreen()
        {
            _template = new GameObject("Result fixture");
            _template.SetActive(false);
            _template.AddComponent<TestResultPresenter>();
            _panel = ScriptableObject.CreateInstance<PanelSettings>();
            var document = _template.GetComponent<UIDocument>();
            document.panelSettings = _panel;
            document.visualTreeAsset = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>("Assets/Tests/ResultScreen.uxml");
            Assert.That(document.visualTreeAsset, Is.Not.Null);
            _loader.RegisterPrefab("UI/TestResult.prefab", _template);
        }

        private static void Click(Button button) => typeof(Clickable)
            .GetMethod("Invoke", BindingFlags.Instance | BindingFlags.NonPublic)
            .Invoke(button.clickable, new object[] { null });

        [UnityTest]
        public IEnumerator ResultClickDuringOpenAnimationCompletesWaiterAndSurvivesOpening() => UniTask.ToCoroutine(async () =>
        {
            RegisterResultScreen();
            var opening = _service.OpenScreen<TestResultPresenter>().Preserve();
            await UniTask.WaitUntil(() => _service.IsScreenOpen<TestResultPresenter>() &&
                _service.GetScreen<TestResultPresenter>().AnimationPending);
            var presenter = _service.GetScreen<TestResultPresenter>();
            Assert.That(opening.Status, Is.EqualTo(UniTaskStatus.Pending));
            Click(presenter.CompleteButton);
            var result = presenter.WaitForResult().Preserve();
            Assert.That(result.Status, Is.EqualTo(UniTaskStatus.Succeeded));
            Assert.That(await result, Is.EqualTo(42));
            presenter.FinishAnimation();
            await opening;
            Assert.That(presenter.GetResult(), Is.EqualTo(42));
            Assert.That(await presenter.WaitForResult(), Is.EqualTo(42));
            await _service.CloseScreen<TestResultPresenter>();
        });

        [UnityTest]
        public IEnumerator ReenabledSameResultPresenterUsesFreshWaiterWithoutPreviousChoice() => UniTask.ToCoroutine(async () =>
        {
            RegisterResultScreen();
            var opening = _service.OpenScreen<TestResultPresenter>().Preserve();
            await UniTask.WaitUntil(() => _service.IsScreenOpen<TestResultPresenter>() &&
                _service.GetScreen<TestResultPresenter>().AnimationPending);
            var presenter = _service.GetScreen<TestResultPresenter>();
            Click(presenter.CompleteButton);
            presenter.FinishAnimation();
            await opening;
            Assert.That(await presenter.WaitForResult(), Is.EqualTo(42));
            // Reuse the exact MonoBehaviour, exercising its real OnEnable/reload lifecycle.
            presenter.gameObject.SetActive(false);
            presenter.NextResult = 17;
            presenter.PrepareNextAnimation();
            presenter.gameObject.SetActive(true);
            await UniTask.WaitUntil(() => presenter.AnimationPending);
            var next = presenter.WaitForResult().Preserve();
            Assert.That(next.Status, Is.EqualTo(UniTaskStatus.Pending));
            Assert.That(presenter.GetResult(), Is.EqualTo(0));
            Click(presenter.CompleteButton);
            presenter.FinishAnimation();
            Assert.That(await next, Is.EqualTo(17));
            await UniTask.NextFrame();
            Assert.That(presenter.GetResult(), Is.EqualTo(17));
            await _service.CloseScreen<TestResultPresenter>();
        });

        private void RegisterToolkitScreen(string treePath, bool assignDocument = false)
        {
            _template = new GameObject("Toolkit fixture");
            _template.SetActive(false);
            var presenter = _template.AddComponent<TestToolkitPresenter>();
            var document = _template.GetComponent<UIDocument>();
            if (treePath != null)
            {
                _panel = ScriptableObject.CreateInstance<PanelSettings>();
                document.panelSettings = _panel;
                document.visualTreeAsset = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(treePath);
                Assert.That(document.visualTreeAsset, Is.Not.Null);
            }
            if (assignDocument)
                typeof(UIPresenter).GetField("_document", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(presenter, document);
            _loader.RegisterPrefab("UI/TestToolkit.prefab", _template);
        }

        [UnityTest]
        public IEnumerator ConfiguredToolkitDocumentBindsRootAndRunsCallbacks() => UniTask.ToCoroutine(async () =>
        {
            RegisterToolkitScreen("Assets/Tests/ValidScreen.uxml");
            var presenter = await _service.OpenScreen<TestToolkitPresenter>();
            Assert.That(presenter.Root, Is.Not.Null);
            Assert.That(presenter.Root.name, Is.EqualTo("root"));
            Assert.That(presenter.Root.ClassListContains(GameUIService.CLASS_HIDDEN), Is.False);
            Assert.That(presenter.Queries, Is.EqualTo(1));
            Assert.That(presenter.Opens, Is.EqualTo(1));
            await _service.CloseScreen<TestToolkitPresenter>();
        });

        [UnityTest]
        public IEnumerator ToolkitMissingRootFailsAndRemovesFailedScreen() => UniTask.ToCoroutine(async () =>
        {
            RegisterToolkitScreen("Assets/Tests/MissingRootScreen.uxml");
            Exception failure = null;
            try { await _service.OpenScreen<TestToolkitPresenter>(); }
            catch (Exception exception) { failure = exception; }
            Assert.That(failure, Is.TypeOf<InvalidOperationException>());
            Assert.That(failure.Message, Does.Contain("TestToolkitPresenter").And.Contain("required 'root'"));
            Assert.That(_service.IsScreenOpen<TestToolkitPresenter>(), Is.False);
        });

        [UnityTest]
        public IEnumerator ExplicitEmptyToolkitDocumentFailsWithoutCanvasFallback() => UniTask.ToCoroutine(async () =>
        {
            RegisterToolkitScreen(null, true);
            Exception failure = null;
            try { await _service.OpenScreen<TestToolkitPresenter>(); }
            catch (Exception exception) { failure = exception; }
            Assert.That(failure, Is.TypeOf<InvalidOperationException>());
            Assert.That(failure.Message, Does.Contain("required 'root'"));
            Assert.That(_service.IsScreenOpen<TestToolkitPresenter>(), Is.False);
        });

        [UnityTest]
        public IEnumerator MissingToolkitAndCanvasFailsWithoutOpeningInvisibleScreen() => UniTask.ToCoroutine(async () =>
        {
            RegisterToolkitScreen(null);
            Exception failure = null;
            try { await _service.OpenScreen<TestToolkitPresenter>(); }
            catch (Exception exception) { failure = exception; }
            Assert.That(failure, Is.TypeOf<InvalidOperationException>());
            Assert.That(failure.Message, Does.Contain("configured UIDocument or a legacy Canvas"));
            Assert.That(_service.IsScreenOpen<TestToolkitPresenter>(), Is.False);
        });
    }
}
