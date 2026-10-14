using System;
using Cysharp.Threading.Tasks;
using FirstLight.Game.Localization;
using FirstLight.Game.Services;
using FirstLight.Game.UI;
using FirstLight.Game.UIElements;
using FirstLight.Game.Utils;
using UnityEngine.UIElements;
using MainInstaller = FirstLight.Game.Utils.MainInstaller;

namespace FirstLight.Game.Presenters
{
	/// <summary>Optional hosted Unity Account linking/sign-in; guest play remains available.</summary>
	public class ConnectFlgIdScreenPresenter : UIPresenterData<ConnectFlgIdScreenPresenter.StateData>
	{
		public class StateData
		{
			public Action AuthLoginSuccess;
			public Action AuthRegisterSuccess;
			public Action CloseClicked;
		}

		private IGameServices _services;
		private LocalizedButton _linkButton;
		private LocalizedButton _loginButton;
		private ImageButton _closeButton;
		private bool _busy;

		private void Awake() => _services = MainInstaller.Resolve<IGameServices>();

		protected override void QueryElements()
		{
			_linkButton = Root.Q<LocalizedButton>("LinkAccountButton").Required();
			_loginButton = Root.Q<LocalizedButton>("LoginButton").Required();
			_closeButton = Root.Q<ImageButton>("CloseButton").Required();
			_closeButton.clicked += () => { if (!_busy) Data.CloseClicked?.Invoke(); };
			_linkButton.clicked += () => Authenticate(true).Forget();
			_loginButton.clicked += () => Authenticate(false).Forget();
			var available = _services.AuthService.CanLoginWithUnityPlayerAccount;
			_linkButton.SetEnabled(available);
			_loginButton.SetEnabled(available);
			Root.Q<LocalizedLabel>("Desc").Required().LocalizationKey = available ? "UITAccount/description" : "UITAccount/unavailable";
			Root.SetupClicks(_services);
		}

		private async UniTask Authenticate(bool linkCurrentGuest)
		{
			if (_busy || !_services.AuthService.CanLoginWithUnityPlayerAccount) return;
			_busy = true;
			_linkButton.SetEnabled(false);
			_loginButton.SetEnabled(false);
			try
			{
				await _services.UIService.OpenScreen<LoadingSpinnerScreenPresenter>();
				await _services.AuthService.LoginWithUnityPlayerAccount(linkCurrentGuest);
				if (linkCurrentGuest) Data.AuthRegisterSuccess?.Invoke();
				else Data.AuthLoginSuccess?.Invoke();
			}
			catch (OperationCanceledException) { }
			catch (Exception exception)
			{
				var button = new GenericDialogButton { ButtonText = ScriptLocalization.General.OK, ButtonOnClick = _services.GenericDialogService.CloseDialog };
				await _services.GenericDialogService.OpenButtonDialog(ScriptLocalization.UITShared.error,
					GameLocalization.GetTranslation("UITAccount/failed") + "\n" + exception.Message, false, button);
			}
			finally
			{
				await _services.UIService.CloseScreen<LoadingSpinnerScreenPresenter>();
				_busy = false;
				_linkButton.SetEnabled(_services.AuthService.CanLoginWithUnityPlayerAccount);
				_loginButton.SetEnabled(_services.AuthService.CanLoginWithUnityPlayerAccount);
			}
		}
	}
}
