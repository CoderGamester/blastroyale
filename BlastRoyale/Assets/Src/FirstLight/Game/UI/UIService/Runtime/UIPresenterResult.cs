using Cysharp.Threading.Tasks;

namespace FirstLight.Game.UI
{
	public abstract class UIPresenterResult<T> : UIPresenter
	{
		private UniTaskCompletionSource<T> _resultTask;
		private T _cachedResult;

		internal override async UniTask OnScreenOpenedInternal(bool reload = false, UIService uiService = null)
		{
			// Opening makes controls interactive before its asynchronous animation finishes.
			// Prepare the result first so an early click is valid and is not discarded afterwards.
			ResetResult();
			await base.OnScreenOpenedInternal(reload, uiService);
		}

		public T GetResult()
		{
			return _cachedResult;
		}

		protected void SetResult(T result)
		{
			_cachedResult = result;
			_resultTask.TrySetResult(result);
		}

		public UniTask<T> WaitForResult()
		{
			return _resultTask.Task;
		}

		public void ResetResult()
		{
			_resultTask = new ();
			_cachedResult = default;
		}
	}
}
