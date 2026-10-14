using Cysharp.Threading.Tasks;
using FirstLight.Game.UI;
using UnityEngine.UIElements;

namespace FirstLight.Tests.PlayMode.UI
{
    public class TestResultPresenter : UIPresenterResult<int>
    {
        private UniTaskCompletionSource _animation = new UniTaskCompletionSource();
        public Button CompleteButton { get; private set; }
        public bool AnimationPending { get; private set; }
        public int NextResult = 42;
        protected override void QueryElements()
        {
            CompleteButton = Root.Q<Button>("Complete");
            CompleteButton.clicked += () => SetResult(NextResult);
        }
        protected override async UniTask OnScreenOpen(bool reload)
        {
            AnimationPending = true;
            await _animation.Task;
            AnimationPending = false;
        }
        public void FinishAnimation() => _animation.TrySetResult();
        public void PrepareNextAnimation()
        {
            _animation = new UniTaskCompletionSource();
            AnimationPending = false;
        }
    }
}
