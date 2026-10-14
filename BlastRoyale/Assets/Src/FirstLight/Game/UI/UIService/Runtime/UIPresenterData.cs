namespace FirstLight.Game.UI
{
	public abstract class UIPresenterData<T> : UIPresenter where T : class
	{
		protected new T Data => (T) base.Data;
	}
}