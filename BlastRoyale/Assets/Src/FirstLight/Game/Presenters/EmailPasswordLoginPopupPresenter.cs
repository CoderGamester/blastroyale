using Cysharp.Threading.Tasks;
using FirstLight.Game.Services;
using FirstLight.Game.UI;
using FirstLight.Game.UIElements;
using FirstLight.Game.Utils;
using UnityEngine.UIElements;

namespace FirstLight.Game.Presenters
{
	/// <summary>Hosted Unity Account choice; class and asset identities retain existing screen bindings.</summary>
	[UILayer(UILayer.Popup)]
	public class EmailPasswordLoginPopupPresenter : UIPresenterResult<EmailPasswordLoginPopupPresenter.Result>
	{
		public class Result
		{
			public enum ResultType { Login = 0, Close = 2 }
			public ResultType ResultActionType;
		}

		private GenericPopupElement _popup;

		protected override void QueryElements()
		{
			_popup = Root.Q<GenericPopupElement>("Popup").Required();
			_popup.CloseClicked += CloseChoice;
			Root.Q<LocalizedButton>("ContinueGuestButton").Required().clicked += CloseChoice;
			var login = Root.Q<LocalizedButton>("LoginButton").Required();
			login.clicked += () => SetResult(new Result { ResultActionType = Result.ResultType.Login });
			var available = MainInstaller.ResolveServices().AuthService.CanLoginWithUnityPlayerAccount;
			login.SetEnabled(available);
			Root.Q<LocalizedLabel>("Desc").Required().LocalizationKey = available ? "UITAccount/description" : "UITAccount/unavailable";
		}

		private void CloseChoice() => SetResult(new Result { ResultActionType = Result.ResultType.Close });
		protected override UniTask OnScreenOpen(bool reload) => _popup.AnimateOpen();
		protected override UniTask OnScreenClose() => _popup.AnimateClose();
	}
}
