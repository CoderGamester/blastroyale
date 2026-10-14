using System;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using FirstLight.FLogger;
using FirstLight.Game.Commands;
using FirstLight.Game.Configs.Remote;
using FirstLight.Game.Data;
using FirstLight.Game.Logic;
using FirstLight.Game.Messages;
using FirstLight.Game.Presenters;
using FirstLight.Game.Services;
using FirstLight.Game.Services.Authentication;
using FirstLight.Game.Services.Authentication.Hooks;
using FirstLight.Game.Utils;
using GameLovers.MobileServices.NativeUi;
using FirstLight.Server.SDK.Modules;
using FirstLight.Server.SDK.Modules.GameConfiguration;
using GameLovers.Services;
using GameLovers.StatechartMachine;
using FirstLight.Game.UI;
using FirstLightServerSDK.Services;
using FirstLight.Game.Localization;
using PlayFab.ClientModels;
using Unity.Services.UserReporting;
using Unity.Services.UserReporting.Client;
using UnityEngine;
using UnityEngine.Analytics;
using UnityEngine.Networking;

namespace FirstLight.Game.StateMachines
{
	/// <summary>
	/// This object contains the behaviour logic for player's authentication in the game in the <seealso cref="GameStateMachine"/>
	/// </summary>
	public class AuthenticationState
	{
		private const string VERSION_FILE_TEMPLATE = "https://cdn.blastroyale.com/versions/{0}.json";
		public static readonly int MaxAuthenticationRetries = 3;

		private readonly IStatechartEvent _authSuccessEvent = new StatechartEvent("Authentication Success Event");
		private readonly IStatechartEvent _authFailEvent = new StatechartEvent("Authentication Fail Generic Event");

		private readonly IStatechartEvent _authFailAccountDeletedEvent =
			new StatechartEvent("Authentication Fail Account Deleted Event");

		private readonly GameLogic _gameLogic;
		private readonly IGameServices _services;
		private readonly IClientDataService _dataService;
		private readonly Action<IStatechartEvent> _statechartTrigger;
		private IConfigsAdder _configsAdder;
		private UniTask _asyncLogin;
		private bool _usingAsyncLogin;

		public class InnerState
		{
			public bool Success;
		}

		public InnerState State;

		public AuthenticationState(GameLogic gameLogic, IGameServices services, IClientDataService dataService, Action<IStatechartEvent> statechartTrigger)
		{
			_gameLogic = gameLogic;
			_services = services;
			_dataService = dataService;
			_statechartTrigger = statechartTrigger;
			State = new InnerState();
		}

		/// <summary>
		/// Setups the Initial Loading state
		/// </summary>
		public void Setup(IStateFactory stateFactory)
		{
			var initial = stateFactory.Initial("Initial");
			var final = stateFactory.Final("Final");
			var authFail = stateFactory.State("Authentication Fail Dialog");
			var postAuthCheck = stateFactory.Choice("Post Authentication Checks");
			var gameMaintenance = stateFactory.State("Game Blocked Dialog");
			var asyncLoginWait = stateFactory.TaskWait("Async Login Wait");

			initial.Transition().Target(asyncLoginWait);
			asyncLoginWait.WaitingFor(LoginFlow).Target(postAuthCheck);
			postAuthCheck.Transition().Condition(() => !State.Success).Target(authFail);
			postAuthCheck.Transition().Condition(IsGameOutdatedOrMaintenance).Target(gameMaintenance);
			postAuthCheck.Transition().Target(final);

			gameMaintenance.OnEnter(OpenMaintenanceDialog);

			final.OnEnter(UnsubscribeEvents);
		}

		private async UniTask LoginFlow()
		{
			await _services.GameBackendService.SetupBackendEnvironment();

			if (!_services.LocalPrefsService.AcceptedPrivacyTerms)
			{
				LoadingScreenPresenter.Hide();
				GlobalAnimatedBackground.Instance.SetDefault();
				// Wait for accepting terms
				await OpenPrivacyDialogAndWaitForAccept();
				_services.LocalPrefsService.AcceptedPrivacyTerms.Value = true;
			}

			// user already logged iin into the game and can automatically login again with the device id
			var loggedIn = false;
			if (_services.AuthService.HasDeviceLinked())
			{
				await AuthenticateAndInitializeLogic(_services.AuthService.LoginWithDeviceID);
				FLog.Info("Logged in using linked device id");
				if (await _services.AuthService.CanLoginWithNativeGamesService())
				{
					FLog.Info("Linking account with native service");
					_services.AuthService.TryToLinkNativeAccount().Forget();
				}
				loggedIn = true;
			}
			// Try to login with native game service
			else if (await _services.AuthService.CanLoginWithNativeGamesService())
			{
				FLog.Info("Can login with native game service");
				await AuthenticateAndInitializeLogic(_services.AuthService.LoginWithNativeGamesService);
				loggedIn = true;
			}

			await WaitForSkipTutorialInput(loggedIn);
			GlobalAnimatedBackground.Instance.Disable();
		}

		private async Task WaitForSkipTutorialInput(bool loggedIn)
		{
			var loggedInWithUnityAccount = false;
			while (true)
			{
				if (loggedIn) // user already finished the tutorial
				{
					FLog.Info("User already logged in");
					if (_services.TutorialService.HasCompletedTutorialSection(TutorialSection.FIRST_GUIDE_MATCH))
					{
						FLog.Info("User already completed tutorial");
						if (_services.UIService.IsScreenOpen<SkipTutorialPopupPresenter>())
						{
							await _services.UIService.CloseScreen<SkipTutorialPopupPresenter>();
						}

						return;
					}
				}

				GlobalAnimatedBackground.Instance.SetDefault();
				LoadingScreenPresenter.Hide();

				// First time user opening the game lets see if he wants to do the tutorial. I surely don't want to do it again
				var skipTutorialScreen = await _services.UIService.OpenScreen<SkipTutorialPopupPresenter>();
				if (loggedInWithUnityAccount) // User may have logged in and not finished tutorial so lets remove
				{
					skipTutorialScreen.DisableLoginOption();
				}

				var result = await skipTutorialScreen.WaitForResult();
				if (result == SkipTutorialPopupPresenter.AllowedOptions.Login)
				{
					if (loggedIn)
					{
						await _services.AuthService.Logout();
						loggedIn = false;
					}

					var wantsAccountLogin = await LoginScreenFlow();
					if (!wantsAccountLogin) // User continues with the guest path
					{
						FLog.Info("User did not login and closed screen");
						continue;
					}

					LoadingScreenPresenter.Show();
					try
					{
						await _services.AuthService.LoginWithUnityPlayerAccount(false);
						InitializeGameLogic();
						State.Success = true;
					}
					catch (OperationCanceledException)
					{
						LoadingScreenPresenter.Hide();
						continue;
					}
					catch (Exception exception)
					{
						FLog.Warn("Hosted Unity Account sign-in did not complete", exception);
						_services.InGameNotificationService.QueueNotification(GameLocalization.GetTranslation("UITAccount/failed"));
						LoadingScreenPresenter.Hide();
						continue;
					}
					_services.AuthService.CanLoginWithNativeGamesService().ContinueWith(can =>
					{
						if(can) _services.AuthService.TryToLinkNativeAccount().Forget();
					}).Forget();
					loggedInWithUnityAccount = true;
					loggedIn = true;
				}

				else if (result is SkipTutorialPopupPresenter.AllowedOptions.SkipTutorial or SkipTutorialPopupPresenter.AllowedOptions.Tutorial)
				{
					await skipTutorialScreen.Close();
					LoadingScreenPresenter.Show();

					if (!loggedIn) // Users may already have logged in automatically with native games services
					{
						await AuthenticateAndInitializeLogic(_services.AuthService.LoginWithGuestAccount);
					}

					if (result == SkipTutorialPopupPresenter.AllowedOptions.SkipTutorial)
					{
						_services.TutorialService.SkipTutorial();
					}

					return;
				}
				else
				{
					throw new Exception("I don't know this yet!");
				}
			}
		}

		private async Task OpenPrivacyDialogAndWaitForAccept()
		{
			var privacyAcceptedSrc = new UniTaskCompletionSource();
			await _services.UIService.OpenScreen<PrivacyDialogPresenter>(new PrivacyDialogPresenter.StateData()
			{
				OnAccept = () => privacyAcceptedSrc.TrySetResult()
			});
			await privacyAcceptedSrc.Task;
			await _services.UIService.CloseScreen<PrivacyDialogPresenter>();
		}

		private async UniTask<bool> LoginScreenFlow()
		{
			var screen = await _services.UIService.OpenScreen<EmailPasswordLoginPopupPresenter>();
			var result = await screen.WaitForResult();
			await screen.Close();
			return result.ResultActionType == EmailPasswordLoginPopupPresenter.Result.ResultType.Login;
		}

		private async UniTask AuthenticateAndInitializeLogic(Func<UniTask> loginMethod)
		{
			FLog.Info("Starting async login");
			var retries = 0;
			int maxRetries = 3;
			while (retries < maxRetries)
			{
				try
				{
					await loginMethod();
					InitializeGameLogic();
					State.Success = true;
					return;
				}
				catch (Exception ex)
				{
					retries++;
					if (retries >= maxRetries)
					{
						State.Success = false;
						OnLoginError(ex);
						throw;
					}

					FLog.Warn("Auth exception, retrying!", ex);
					await UniTask.Delay((int) (2000 * Math.Pow(2, retries - 1)));
				}
			}
		}


		private void OnLoginError(Exception exception)
		{
			bool recoverable = true;
			var message = exception.Message;

			FLog.Error("Login Error", exception);

			if (exception is AuthenticationException authenticationException)
			{
				message = authenticationException.Message;
				recoverable = authenticationException.Recoverable;
			}

			var account = _dataService.GetData<AccountData>();
			Analytics.SendEvent("loginError", new Dictionary<string, object>
			{
				{"report", exception.ToString()},
				{"device_id", account.DeviceId ?? "null"},
				{"recoverable", recoverable},
			});

			if (!recoverable)
			{
				_services.AuthService.Logout().Forget();
			}

			var confirmButton = new GenericDialogButton
			{
				ButtonText = ScriptLocalization.General.OK,
				ButtonOnClick = () =>
				{
					_services.QuitGame("OnLoginError");
				}
			};

			if (!recoverable)
			{
				message = $"<color=red>You will be logged out of this account!</color>\n\n<size=70%>{message}</size>";
			}
			else
			{
				message =
					$"{message}\n\nIf this keeps happening please contact us at <color=blue><u><a href=\"{GameConstants.Links.DISCORD_SERVER}\">Discord</a></u></color>";
			}

			_services.GenericDialogService.OpenButtonDialog(ScriptLocalization.UITShared.error, message,
				false, confirmButton);
		}

		private void UnsubscribeEvents()
		{
			_services.MessageBrokerService?.UnsubscribeAll(this);
		}

		private bool IsGameOutdatedOrMaintenance()
		{
			return _services.GameBackendService.IsGameInMaintenance(out _) || _services.GameBackendService.IsGameOutdated(out _);
		}

		private void OpenMaintenanceDialog()
		{
			_services.GameBackendService.IsGameInMaintenanceOrOutdated(true);
		}

		private void InitializeGameLogic()
		{
			// AuthService initializes/rebinds logic before client initialization commands and migration hooks run.
			_services.MessageBrokerService.PublishSafe(new LogicInitializedMessage());
			InitUserReporting(_services, _gameLogic.RemoteConfigProvider).Forget(); // TODO: Move this to Startup when we can await for auth there
		}

		private static async UniTaskVoid InitUserReporting(IGameServices services, IRemoteConfigProvider remoteConfig)
		{
			if (!remoteConfig.GetConfig<GeneralConfig>().ShowBugReportButton) return;

			var customConfig = new UserReportingClientConfiguration(100, 300, 60, 1);
			UserReportingService.Instance.Configure(customConfig);

			await services.UIService.OpenScreen<UserReportScreenPresenter>();
		}
	}
}
