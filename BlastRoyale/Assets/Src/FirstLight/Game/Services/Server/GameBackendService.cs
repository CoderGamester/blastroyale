using FirstLight.Game.Services;
using IDataProvider = FirstLight.Server.SDK.Models.IDataProvider;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using Cysharp.Threading.Tasks;
using FirstLight.FLogger;
using FirstLight.Game.Configs;
using FirstLight.Game.Configs.Remote;
using FirstLight.Game.Data;
using FirstLight.Game.Logic;
using FirstLight.Game.Logic.RPC;
using FirstLight.Game.Messages;
using FirstLight.Game.Utils;
using GameLovers.MobileServices.NativeUi;
using GameLovers.Services;
using FirstLight.Server.SDK.Models;
using FirstLight.Server.SDK.Modules;
using FirstLight.Game.Localization;
using PlayFab;
using PlayFab.ClientModels;
using PlayFab.CloudScriptModels;
using FirstLight.Game.Commands;
using PlayFab.SharedModels;
using FirstLight.Server.SDK.Modules.Commands;
using Unity.Services.Authentication;
using Unity.Services.CloudSave;
using FirstLight.Game.Utils.UCSExtensions;
using UnityEngine;

namespace FirstLight.Game.Services
{
	/// <summary>
	/// This service handles general interaction with playfab that are not needed by the server
	/// </summary>
	public interface IGameBackendService
	{
		/// <summary>
		/// Sets up the backend with the correct cloud environment, per platform
		/// </summary>
		UniTask SetupBackendEnvironment();

		/// <summary>
		/// Execute a function using the generic endcpoint, to see current values look at <see cref="FirstLight.Server.SDK.Modules.Commands"/>
		/// </summary>
		public void CallGenericFunction(string functionName, Action<ExecuteFunctionResult> onSuccess, Action<PlayFabError> onError,
										Dictionary<string, string> data = null);

		/// <summary>
		/// Same as <see cref="CallGenericFunction"/>
		/// </summary>
		public UniTask<ExecuteFunctionResult> CallGenericFunction(string functionName, Dictionary<string, string> data = null);

		/// <summary>
		/// Obtains the server state of the logged in player
		/// </summary>
		void FetchServerState(Action<ServerState> onSuccess, Action<PlayFabError> onError);

		/// <summary>
		/// Obtains the server state of the logged in player
		/// </summary>
		UniTask<ServerState> FetchServerState();

		/// <summary>
		/// Compare server with client rewards to check if they match.
		/// Introduced as a workaround due to requiring two synchronous commands
		/// from two different services (Logic Service & Quantum Server)
		/// </summary>
		UniTask<bool> CheckIfRewardsMatch();

		/// <summary>
		/// Requests to check if the game is currently in maintenance
		/// </summary>
		bool IsGameInMaintenance(out string message);

		/// <summary>
		/// Requests to check if the current game version is outdated
		/// </summary>
		bool IsGameOutdated(out string message);

		/// <summary>
		/// Requests the title version downloaded at authentication time
		/// </summary>
		string GetTitleVersion();

		/// <summary>
		/// Handles an incoming error. Sends outgoing messages, analytics and calls back
		/// </summary>
		void HandleError(PlayFabError error, Action<PlayFabError> callback);

		/// <summary>
		/// Will handle a recoverable exception, making sure it will get to all analytics services
		/// </summary>
		void HandleRecoverableException(Exception ex);

		/// <summary>
		/// Returns if the game is running on dev env. On dev things can be different.
		/// </summary>
		bool IsDev();

		/// <summary>
		/// Returns true for environments that run server-side simulation
		/// </summary>
		bool RunsSimulationOnServer();

		/// <summary>
		/// If the environment was forced by a redirect
		/// </summary>
		public bool ForcedEnvironment { get; }

		/// <summary>
		/// Return true if the game is outdated or in maintanance
		/// </summary>
		public bool IsGameInMaintenanceOrOutdated(bool openPopups);

		/// <see cref="UnityRemoteConfigProvider.UpdateConfigWhenExpired"/>
		public UniTask<bool> UpdateConfigs(params Type[] types);
	}

	/// <inheritdoc cref="IGameBackendService" />
	public interface IInternalGameBackendService : IGameBackendService
	{
	}

	/// <inheritdoc cref="IGameBackendService" />
	public class GameBackendService : IInternalGameBackendService, IClientAuthorityBackend
	{
		private readonly IMessageBrokerService _messageBrokerService;
		private readonly IGameDataProvider _dataProvider;
		private readonly IClientDataService _dataService;
		private readonly IGameServices _services;

		public bool ForcedEnvironment { get; private set; }

		private readonly string _leaderboardLadderName;
		private readonly IPlayerStateBackend _playerBackend;
		private (string PlayerId, uint Trophies)? _publishedTrophies;

		public GameBackendService(IMessageBrokerService msgBroker, IGameDataProvider dataProvider, IGameServices services, IClientDataService dataService,
								  string leaderboardLadderName, IPlayerStateBackend playerBackend = null)
		{
			_messageBrokerService = msgBroker;
			_dataProvider = dataProvider;
			_dataService = dataService;
			_services = services;
			_leaderboardLadderName = leaderboardLadderName;
			_playerBackend = playerBackend ?? new PlayFabPlayerStateBackend();
		}

		public async UniTask SetupBackendEnvironment()
		{
			FLEnvironment.ValidateClientConfiguration();
			var quantumSettings = _services.ConfigsProvider.GetConfig<QuantumRunnerConfigs>().PhotonServerSettings;
			var appData = _dataService.GetData<AppData>();

			FLog.Info($"Using environment: {FLEnvironment.Current.UCSEnvironmentName}");

			PlayFabSettings.TitleId = FLEnvironment.Current.PlayFabTitleID;
			quantumSettings.AppSettings.AppIdRealtime = FLEnvironment.Current.PhotonAppIDRealtime;

			if (FLEnvironment.Current.Name != appData.LastEnvironmentName)
			{
				FLog.Warn("Erasing APP data due to environment switch");

				// We only unlink if there was a previous environment set (check is here for migration purposes)
				if (!string.IsNullOrEmpty(appData.LastEnvironmentName))
				{
					try
					{
						await _services.AuthService.Logout();
					}
					catch (Exception)
					{
						_services.GenericDialogService.OpenSimpleMessage("Error", "Environment changed, could not logout").Forget();
					}
				}

				appData.LastEnvironmentName = FLEnvironment.Current.Name;

				_dataService.AddData(appData, true);
				_dataService.SaveData<AppData>();
			}
		}

		// Rewards are already computed by the local Quantum command path. There is no independent server to compare.
		public UniTask<bool> CheckIfRewardsMatch() => UniTask.FromResult(true);

		public async UniTask PersistPlayerState(ServerState snapshot)
		{
			await _playerBackend.Save(snapshot);
			if (!AuthenticationService.Instance.IsSignedIn || !snapshot.Has<PlayerData>()) return;
			var player = PlayFabSettings.staticPlayer.PlayFabId;
			var trophies = snapshot.DeserializeModel<PlayerData>().Trophies;
			if (_publishedTrophies.HasValue && _publishedTrophies.Value == (player, trophies)) return;
			await CloudSaveService.Instance.SaveTrophiesAsync(trophies);
			_publishedTrophies = (player, trophies);
		}

		public static ServerState CapturePlayerState(IDataProvider data)
		{
			var snapshot = new ServerState();
			foreach (var type in PlayFabPlayerStateBackend.ModelTypes)
			{
				if (data.TryGetData(type, out var model)) snapshot.UpdateModel(model);
			}
			return snapshot;
		}

		/// <summary>
		/// Compatibility boundary for the existing game services. Executes locally or via public client APIs.
		/// SERVER AUTHORITY: restore validation and purchase grants from Backend/ before a production economy.
		/// </summary>
		public async UniTask<ExecuteFunctionResult> CallGenericFunction(string functionName, Dictionary<string, string> data = null)
		{
			var result = new LogicResult { Command = functionName, PlayFabId = PlayFabSettings.staticPlayer.PlayFabId,
				Data = new Dictionary<string, string>() };
			if (functionName == CommandNames.GET_PLAYER_DATA)
			{
				var logic = (IGameLogic) _dataProvider;
				var context = new CommandExecutionContext(new LogicContainer().Build(logic),
					new ServiceContainer().Build(_services), _dataService);
				await new GiveDefaultCollectionItemsCommand().Execute(context);
				await new InitializeBattlepassSeasonCommand().Execute(context);
				await new RestartPlayerTrackedStoreDataCommand().Execute(context);
				await new CleanupOldDataCommand().Execute(context);
				await PersistPlayerState(CapturePlayerState(_dataService));
			}
			else if (functionName == CommandNames.GET_PLAYER_PROFILE)
			{
				if (data == null || !data.TryGetValue(CommandFields.PlayerId, out var playerId))
					throw new ArgumentException("A player ID is required to read a public profile.");
				ModelSerializer.SerializeToData(result.Data, await _playerBackend.GetPublicProfile(playerId));
			}
			else if (functionName == CommandNames.SYNC_NAME)
			{
				// Both SDK profiles are updated explicitly by AuthService and UnityCloudHook.
			}
			else if (functionName == CommandNames.REMOVE_PLAYER_DATA)
			{
				throw new NotSupportedException("Deleting a PlayFab account requires a trusted backend or PlayFab Game Manager. See HUMAN_ACTIONS.md; client credentials must never contain a developer secret.");
			}
			else if (functionName == CommandNames.CONSUME_VALIDATE_PURCHASE)
			{
				throw new NotSupportedException("Real money purchase grants are disabled until a trusted receipt and inventory service is configured. Virtual currency purchases use BuyFromStoreCommand locally.");
			}
			else
			{
				throw new NotSupportedException("Unsupported client backend operation: " + functionName);
			}
			return new ExecuteFunctionResult
			{
				FunctionName = functionName,
				FunctionResult = ModelSerializer.Serialize(new PlayFabResult<LogicResult> { Result = result }).Value
			};
		}

		public void CallGenericFunction(string functionName, Action<ExecuteFunctionResult> onSuccess, Action<PlayFabError> onError,
			Dictionary<string, string> data = null) => DispatchFunction(functionName, onSuccess, onError, data).Forget();

		private async UniTask DispatchFunction(string functionName, Action<ExecuteFunctionResult> onSuccess,
			Action<PlayFabError> onError, Dictionary<string, string> data)
		{
			try { onSuccess?.Invoke(await CallGenericFunction(functionName, data)); }
			catch (WrappedPlayFabException exception) { HandleError(exception.Error, onError); }
			catch (Exception exception)
			{
				HandleError(new PlayFabError { Error = PlayFabErrorCode.Unknown, ErrorMessage = exception.Message }, onError);
			}
		}

		public void HandleError(PlayFabError error, Action<PlayFabError> callback)
		{
			var descriptiveError = error.GenerateErrorReport();
			FLog.Error(descriptiveError);

			_services.MessageBrokerService?.PublishSafe(new ServerHttpErrorMessage()
				{ErrorCode = (HttpStatusCode) error.HttpCode, Message = descriptiveError});

			callback?.Invoke(error);
		}

		public void HandleRecoverableException(Exception ex)
		{
			// Unfortunately we have to log as an Error to send to crash analytics, and it is impossible to send exceptions manually :(
			FLog.Error("recoverable exception", ex);
		}

		/// <inheritdoc/>
		public void HandleUnrecoverableException(Exception ex)
		{
			FLog.Error("unrecoverable exception", ex);
			var descriptiveError = $" {ex.Message}";
#if UNITY_EDITOR
			FLog.Error(descriptiveError);
			var confirmButton = new GenericDialogButton
			{
				ButtonText = "OK",
				ButtonOnClick = () =>
				{
					_services.QuitGame(descriptiveError);
				}
			};
			_services.GenericDialogService.OpenButtonDialog("Server Error", descriptiveError, false, confirmButton);
#else
		GameLovers.MobileServices.NativeUi.NativeUiService.ShowAlertPopUp(false, "Error", descriptiveError, new GameLovers.MobileServices.NativeUi.AlertButton
			{
				Callback = () =>
				{
					_services.QuitGame(descriptiveError);
				},
					Style = GameLovers.MobileServices.NativeUi.AlertButtonStyle.Cancel,
					Text = FirstLight.Game.Localization.ScriptLocalization.MainMenu.QuitGameButton
			});
#endif
		}

		public bool IsDev()
		{
			return FLEnvironment.Current.Name == FLEnvironment.DEVELOPMENT.Name;
		}

		public bool RunsSimulationOnServer() => false;

		public void FetchServerState(Action<ServerState> onSuccess, Action<PlayFabError> onError) => FetchState(onSuccess, onError).Forget();

		private async UniTask FetchState(Action<ServerState> onSuccess, Action<PlayFabError> onError)
		{
			try { onSuccess?.Invoke(await FetchServerState()); }
			catch (WrappedPlayFabException exception) { HandleError(exception.Error, onError); }
		}

		public UniTask<ServerState> FetchServerState() => _playerBackend.Load();

		public bool IsGameInMaintenance(out string message)
		{
			var cfg = _dataProvider.RemoteConfigProvider.GetConfig<GameMaintenanceConfig>();

			if (cfg.Maintenance)
			{
				message = cfg.MaintenanceMessage;
				return true;
			}

			message = null;
			return false;
		}

		public bool IsGameOutdated(out string message)
		{
			var cfg = _dataProvider.RemoteConfigProvider.GetConfig<GameMaintenanceConfig>();
			message = cfg.VersionBlockMessage;
			return VersionUtils.IsOutdatedVersion(cfg.AllowedVersion);
		}

		public string GetTitleVersion()
		{
			return _dataProvider.RemoteConfigProvider.GetConfig<GameMaintenanceConfig>().AllowedVersion;
		}

		public UniTask<bool> UpdateConfigs(params Type[] types)
		{
			var serv = ((IGameRemoteConfigProvider) _dataProvider.RemoteConfigProvider);
			var secondsTtl = serv.GetConfig<GeneralConfig>().ConfigCacheInSeconds;
			return serv.UpdateConfigWhenExpired(TimeSpan.FromSeconds(secondsTtl), types);
		}

		public bool IsGameInMaintenanceOrOutdated(bool openPopups)
		{
			string msg;
			if (IsGameInMaintenance(out msg))
			{
				OpenMaintenanceDialog(msg);
				return true;
			}

			if (IsGameOutdated(out msg))
			{
				OpenGameUpdateDialog(msg);
				return true;
			}

			return false;
		}

		private void OpenGameUpdateDialog(string msg)
		{
			var message = string.Format(msg, VersionUtils.VersionExternal,
				_services.GameBackendService.GetTitleVersion());

			NativeUiService.ShowAlertPopUp(false, false, ScriptLocalization.General.NewGameUpdate, message,
				new AlertButton { Text = ScriptLocalization.General.Confirm, Style = AlertButtonStyle.Default, Callback = OpenStore });

			void OpenStore()
			{
#if UNITY_IOS
				Application.OpenURL(GameConstants.Links.APP_STORE_IOS);
#elif UNITY_ANDROID
				Application.OpenURL(GameConstants.Links.APP_STORE_GOOGLE_PLAY);
#endif
				_services.QuitGame("Closing game blocked dialog");
			}
		}

		private void OpenMaintenanceDialog(string msg)
		{
			var cfg = _dataProvider.RemoteConfigProvider.GetConfig<GameMaintenanceConfig>();

			NativeUiService.ShowAlertPopUp(false, false, ScriptLocalization.General.Maintenance, msg,
				new AlertButton
				{
					Text = cfg.MaintenanceButtonText,
					Style = AlertButtonStyle.Default,
					Callback = () =>
					{
						Application.OpenURL(cfg.MaintenanceButtonLink);
						_services.QuitGame("Closing game blocked dialog");
						OpenMaintenanceDialog(msg);
					}
				});
		}
	}
}
