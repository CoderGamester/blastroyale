using FirstLight.Game.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using Cysharp.Threading.Tasks;
using FirstLight.FLogger;
using FirstLight.Game.Data;
using FirstLight.Game.Logic;
using FirstLight.Game.Logic.RPC;
using FirstLight.Game.Messages;
using FirstLight.Game.Services.Authentication.Hooks;
using FirstLight.Game.Utils;
using FirstLight.Game.Utils.UCSExtensions;
using Unity.Services.CloudSave;
using GameLovers.Services;
using FirstLight.Server.SDK.Models;
using FirstLight.Server.SDK.Modules;
using FirstLight.Server.SDK.Modules.Commands;
using FirstLight.Game.Localization;
using PlayFab;
using PlayFab.ClientModels;
using PlayFab.SharedModels;
using Unity.Services.Authentication;
using Unity.Services.Core;

namespace FirstLight.Game.Services.Authentication
{
	public interface INativeGamesLoginProvider
	{
		public UniTask<bool> CanAuthenticate();
		public UniTask<LoginResult> Authenticate();

		public UniTask TryToLinkAccount(LoginResult result);
	}

	public class NullNativeGamesLogin : INativeGamesLoginProvider
	{
		public UniTask<bool> CanAuthenticate()
		{
			return UniTask.FromResult(false);
		}

		public UniTask<LoginResult> Authenticate()
		{
			return UniTask.FromResult<LoginResult>(null);
		}

		public UniTask TryToLinkAccount(LoginResult result)
		{
			return UniTask.CompletedTask;
		}
	}

	public interface IAuthenticationHook
	{
		UniTask BeforeAuthentication(bool previouslyLoggedIn);
		UniTask AfterAuthentication(LoginResult result, bool previouslyLoggedIn);
		UniTask AfterFetchedState(LoginResult result);
		UniTask BeforeLogout();
	}

	internal interface ILoginProvider : IAuthenticationHook
	{
		public UniTask Logout(bool clearRecoverableCredentials = false);
		public UniTask<LoginResult> LoginWithEmailPassword(string email, string password);
		public UniTask<LoginResult> LoginWithDevice();
		public UniTask<LoginResult> LoginWithNewGuestAccount();
		public UniTask AddUserNamePassword(string email, string username, string password);
		public UniTask SendAccountRecoveryEmail(string email);
		public bool CanLoginAutomatically();
		public UniTask<LoginResult> LoginWithUnityPlayerAccount(bool linkCurrentGuest);
	}

	public interface ISessionData
	{
		public bool IsAuthenticated { get; }
		public bool IsFirstSession { get; }
		public bool IsGuest { get; }
		public string Email { get; }
	}

	public interface IAuthService
	{
		/**
		* The local player display name, it has not parsed or escaped spaces and etc
		* If you want pretty display names check the <see cref="AuthServiceNameExtensions"/> class
		*/
		string RawLocalPlayerName { get; }

		/// <summary>
		/// It will go through the whole login process with a guest account, reusing the SDK cached session when present
		/// </summary>
		/// <returns></returns>
		UniTask LoginWithGuestAccount();

		/// <summary>
		/// It will go through the whole login process with the stored/linked device account
		/// </summary>
		/// <returns></returns>
		UniTask LoginWithDeviceID();

		/// <summary>Use the SDK hosted account flow; linking preserves the current guest progression.</summary>
		UniTask LoginWithUnityPlayerAccount(bool linkCurrentGuest);
		bool CanLoginWithUnityPlayerAccount { get; }

		/// <summary>
		/// It will go through the whole login process with the email and password provided, it may fail if the input is incorrect
		/// </summary>
		/// <returns></returns>
		UniTask LoginWithEmailProcess(string email, string password);

		/// <summary>
		/// It will go through the whole login process with the Native games service, Google Play Games or Apple Game Center depending on the platform
		/// </summary>
		/// <returns></returns>
		public UniTask LoginWithNativeGamesService();

		/// <summary>
		/// It will go through the whole login process with a previous processed login result,
		/// this can be useful if you to check if a given account is valid before going through the whole authentication flow
		/// </summary>
		/// <param name="result"></param>
		/// <returns></returns>
		public UniTask LoginWithExistingResult(LoginResult result);

		/// <summary>
		/// It will try to link the current logged in game account services to the account used in game
		/// this is used when user already has an account and wants to link a game center account to it
		/// </summary>
		/// <returns></returns>
		public UniTask TryToLinkNativeAccount();

		/// <summary>
		/// Check if the native game services are enabled and available on the device
		/// </summary>
		/// <returns></returns>
		public UniTask<bool> CanLoginWithNativeGamesService();

		/// <summary>
		/// Fetch the login result for the email and password, this does NOT go through the authentication flow and do not change the auth state
		/// If you want to login with those credentials you must pass the result to <see cref="LoginWithExistingResult"/>
		/// </summary>
		/// <param name="email"></param>
		/// <param name="password"></param>
		/// <returns></returns>
		public UniTask<LoginResult> FetchCredentialsWithEmailPassword(string email, string password);

		/// <summary>
		/// Set the player display name and return a error message if any happens
		/// </summary>
		/// <param name="name"></param>
		/// <returns></returns>
		UniTask<string> SetDisplayName(string name);

		/// <summary>
		/// Register an email and password for the current authenticated account
		/// </summary>
		/// <returns></returns>
		public UniTask AddUserNamePassword(string email, string username, string password);

		/// <summary>
		/// Data for the authenticated account and session
		/// </summary>
		public ISessionData SessionData { get; }

		/// <summary>
		/// Send an recovery account email to the account, this uses hosted Unity Player Accounts
		/// </summary>
		/// <param name="email"></param>
		/// <returns></returns>
		UniTask SendAccountRecoveryEmail(string email);

		/// <summary>
		/// Logout the user and end the current session, preserving a guest's cached recovery token
		/// </summary>
		/// <returns></returns>
		UniTask Logout(bool clearRecoverableCredentials = false);

		/// <summary>
		/// Delete the current account and logout
		/// </summary>
		/// <returns></returns>
		UniTask DeleteAccount();

		/// <summary>
		/// Check if the user has a linked account to the device, this is used for the second time an user opens the game
		/// </summary>
		/// <returns></returns>
		public bool HasDeviceLinked();

		/// <summary>
		/// Registers an authentication hook
		/// </summary>
		public void RegisterHook(IAuthenticationHook hook);

		/// <summary>
		/// Currently linked login types to this account
		/// </summary>
		public IReadOnlyCollection<LoginType> LoginTypes { get; }

		/// <summary>
		/// Returns if user is able to link current account to a native account
		/// </summary>
		public UniTask<bool> CanLinkToNativeAccount();
	}

	public enum LoginType
	{
		Native = 0, LegacyDevice = 1, EmailPass = 2, UnityAccount = 3
	}

	public class SessionData : ISessionData
	{
		public bool IsAuthenticated { get; set; }
		public bool IsFirstSession { get; set; }
		public bool IsGuest { get; set; }
		public string Email { get; set; }

		internal LoginResult LastLoginResult;
	}

	public class AuthService : IAuthService
	{
		private IGameBackendService _backendService;

		private readonly UnityCloudLoginProvider _loginProvider;
		private bool _authenticationInProgress;
		public bool CanLoginWithUnityPlayerAccount => _loginProvider.CanUsePlayerAccounts;

		private INativeGamesLoginProvider _nativeGamesLogin;

		private readonly IMessageBrokerService _msgBroker;
		private readonly IGameLogicInitializer _logicInitializer;
		private readonly IClientDataService _dataService;

		public IReadOnlyCollection<LoginType> LoginTypes { get; private set; }
		public async UniTask<bool> CanLinkToNativeAccount()
		{
			return LoginTypes.Contains(LoginType.EmailPass) && !LoginTypes.Contains(LoginType.Native) && await CanLoginWithNativeGamesService();
		}

		public string RawLocalPlayerName { get; private set; }
		public ISessionData SessionData => _sessionData;
		private SessionData _sessionData;
		private bool _logicInitialized;

		private List<IAuthenticationHook> _authenticationHooks;

		public AuthService(
			IGameLogicInitializer logicInitializer,
			IGameBackendService backendService,
			IClientDataService dataService,
			IGameRemoteConfigProvider gameRemoteConfigProvider,
			IGameCommandService gameCommandService,
			IMessageBrokerService msgBroker, IInternalGameNetworkService networkService)
		{
			_sessionData = new SessionData();
			_logicInitializer = logicInitializer;
			_dataService = dataService;
			_msgBroker = msgBroker;
			_backendService = backendService;
			_msgBroker = msgBroker;
			_loginProvider = new UnityCloudLoginProvider();
			var unityCloudHook = new UnityCloudHook(backendService, dataService, gameRemoteConfigProvider, msgBroker);
			unityCloudHook.OnSetPlayerName += OnUnityAuthSetPlayerName;
			_authenticationHooks = new IAuthenticationHook[]
			{
				_loginProvider,
				unityCloudHook,
				new ParseFeatureFlagsHook(msgBroker),
				new AuthenticatePhotonHook(networkService)
			}.ToList();
			_nativeGamesLogin = new NullNativeGamesLogin();
		}

		/// <summary>
		/// Called after the real UGS player name has been loaded.
		/// </summary>
		private void OnUnityAuthSetPlayerName(string name)
		{
			RawLocalPlayerName = name;
			_msgBroker.PublishSafe(new DisplayNameChangedMessage()
			{
				NewPlayfabDisplayName = name
			});
		}

		public UniTask LoginWithNativeGamesService()
		{
			_sessionData.Email = null;
			_sessionData.IsAuthenticated = false;
			_sessionData.IsFirstSession = true;
			throw new NotSupportedException("Use Unity Player Accounts for recoverable sign-in.");
		}

		public async UniTask TryToLinkNativeAccount()
		{
			try
			{
				await _nativeGamesLogin.TryToLinkAccount(_sessionData.LastLoginResult);
			}
			catch (Exception ex)
			{
				FLog.Warn("Failed to link native account", ex);
			}
		}

		public UniTask<bool> CanLoginWithNativeGamesService()
		{
			return _nativeGamesLogin.CanAuthenticate();
		}

		public UniTask LoginWithGuestAccount() => LoginProcess(_loginProvider.LoginWithNewGuestAccount);

		public async UniTask LoginWithUnityPlayerAccount(bool linkCurrentGuest)
		{
			if (!linkCurrentGuest || !_sessionData.IsAuthenticated)
			{
				await LoginProcess(() => _loginProvider.LoginWithUnityPlayerAccount(linkCurrentGuest), true);
				return;
			}
			if (_authenticationInProgress) throw new InvalidOperationException("Authentication is already in progress.");
			_authenticationInProgress = true;
			try
			{
				var account = UgsAccountSession.Capture();
				var result = await _loginProvider.LoginWithUnityPlayerAccount(true);
				account.ThrowIfChanged();
				var info = await AuthenticationService.Instance.GetPlayerInfoAsync();
				account.ThrowIfChanged();
				_sessionData.LastLoginResult = result;
				_sessionData.IsGuest = false;
				LoginTypes = new HashSet<LoginType> { LoginType.UnityAccount };
				if (string.IsNullOrEmpty(info.GetUnityId())) throw new InvalidOperationException("Unity account linking did not return a linked identity.");
				// Linking preserves this exact UGS ID, loaded models and pending saves. Never reload an older cloud snapshot here.
			}
			finally { _authenticationInProgress = false; }
		}

		public UniTask LoginWithDeviceID()
		{
			_sessionData.Email = null;
			_sessionData.IsAuthenticated = false;
			_sessionData.IsFirstSession = false;
			return LoginProcess(_loginProvider.LoginWithDevice, false);
		}

		public async UniTask LoginWithEmailProcess(string email, string password)
		{
			await LoginProcess(() => _loginProvider.LoginWithEmailPassword(email, password), true);
		}

		public UniTask<LoginResult> FetchCredentialsWithEmailPassword(string email, string password)
		{
			return _loginProvider.LoginWithEmailPassword(email, password);
		}

		public UniTask LoginWithExistingResult(LoginResult result)
		{
			return LoginProcess(() => UniTask.FromResult(result));
		}

		private async UniTask LoginProcess(Func<UniTask<LoginResult>> login, bool previouslyLoggedIn = false)
		{
			if (_authenticationInProgress) throw new InvalidOperationException("Authentication is already in progress.");
			_authenticationInProgress = true;
			var wasAuthenticated = _sessionData.IsAuthenticated;
			var previous = default(UgsAccountSession.Snapshot);
			try
			{
				// Expired SDK sessions remain signed in. Capturing may fail, and must never latch this gate.
				if (wasAuthenticated && AuthenticationService.Instance.IsAuthorized) previous = UgsAccountSession.Capture();
				_sessionData.IsAuthenticated = false;
				await UniTask.WhenAll(_authenticationHooks.Select(a => a.BeforeAuthentication(previouslyLoggedIn)));
				var loginResult = await login();
				var account = UgsAccountSession.Capture();
				if (loginResult == null || loginResult.PlayFabId != account.PlayerId)
					throw new InvalidOperationException("The login result does not belong to the current Unity account.");
				var info = await AuthenticationService.Instance.GetPlayerInfoAsync();
				account.ThrowIfChanged();
				_sessionData.LastLoginResult = loginResult;
				_sessionData.IsFirstSession = loginResult.NewlyCreated;
				_sessionData.IsGuest = (info.Identities == null || info.Identities.Count == 0) && string.IsNullOrEmpty(info.Username);
				LoginTypes = new HashSet<LoginType>
				{
					!string.IsNullOrEmpty(info.GetUnityId()) ? LoginType.UnityAccount : LoginType.LegacyDevice
				};
				_sessionData.Email = null; // Hosted identity claims are not an email/password game login.
				RawLocalPlayerName = AuthenticationService.Instance.PlayerName;
				await UniTask.WhenAll(_authenticationHooks.Select(h => h.AfterAuthentication(loginResult, previouslyLoggedIn)));
				account.ThrowIfChanged();
				var state = await _backendService.FetchServerState();
				account.ThrowIfChanged();
				UpdatePlayerDataAndLogic(state, previouslyLoggedIn);
				await InitializeServerSidePlayer();
				account.ThrowIfChanged();
				if (_dataService.GetData<PlayerData>().Flags.HasFlag(PlayerFlags.Deleted))
					throw new AuthenticationException("This account is deleted!", false);
				await UniTask.WhenAll(_authenticationHooks.Select(a => a.AfterFetchedState(loginResult)));
				account.ThrowIfChanged();
				_sessionData.IsAuthenticated = true;
				_msgBroker.PublishSafe(new SuccessfullyAuthenticated
				{
					SessionData = _sessionData, PreviouslyLoggedIn = previouslyLoggedIn
				});
			}
			catch
			{
				// A canceled hosted link must leave the existing guest usable; switched accounts still require a full reload.
				_sessionData.IsAuthenticated = wasAuthenticated && previous.IsCurrent;
				throw;
			}
			finally { _authenticationInProgress = false; }
		}

		public async UniTask<string> SetDisplayName(string newName)
		{
			var newNameTrimmed = (newName ?? string.Empty).Trim();

			string errorMessage = null;
			if (newNameTrimmed.Length < GameConstants.PlayerName.PLAYER_NAME_MIN_LENGTH)
			{
				errorMessage = string.Format(ScriptLocalization.UITProfileScreen.username_too_short,
					GameConstants.PlayerName.PLAYER_NAME_MIN_LENGTH);
			}
			else if (newNameTrimmed.Length > GameConstants.PlayerName.PLAYER_NAME_MAX_LENGTH)
			{
				errorMessage = string.Format(ScriptLocalization.UITProfileScreen.username_too_long,
					GameConstants.PlayerName.PLAYER_NAME_MAX_LENGTH);
			}
			else if (new Regex("[^a-zA-Z0-9 _-\uA421]+").IsMatch(newNameTrimmed))
			{
				errorMessage = ScriptLocalization.UITProfileScreen.username_invalid_characters;
			}

			if (errorMessage != null)
			{
				return errorMessage;
			}

			newNameTrimmed = AuthServiceNameExtensions.ReplaceSpacesForSpecialChar(newNameTrimmed);

			try
			{
				var account = UgsAccountSession.Capture();
				RawLocalPlayerName = await AuthenticationService.Instance.UpdatePlayerNameAsync(newNameTrimmed);
				account.ThrowIfChanged();
				await CloudSaveService.Instance.SavePlayerNameAsync(RawLocalPlayerName);
				account.ThrowIfChanged();
				_msgBroker.PublishSafe(new DisplayNameChangedMessage { NewPlayfabDisplayName = RawLocalPlayerName });
				return null;
			}
			catch (Exception error) { return $"Error setting player name: {error.Message}"; }
		}

		private void UpdatePlayerDataAndLogic(Dictionary<string, string> state, bool previouslyLoggedIn)
		{
			// Parse every known model first. Corrupt cloud data must not retain a previous account's models and then overwrite the cloud.
			var modelTypes = UnityCloudPlayerStateBackend.ModelTypes.ToDictionary(type => type.FullName);
			var parsed = new Dictionary<Type, object>();
			foreach (var entry in state)
			{
				if (!modelTypes.TryGetValue(entry.Key, out var type)) continue;
				var model = ModelSerializer.DeserializeFromData(type, state);
				if (model == null) throw new InvalidOperationException("Saved player model is empty: " + type.Name);
				parsed[type] = model;
			}
			foreach (var entry in parsed) _dataService.AddData(entry.Key, entry.Value);
			if (_logicInitialized) _logicInitializer.ReInit();
			else { _logicInitializer.Init(); _logicInitialized = true; }
			if (previouslyLoggedIn) _msgBroker.PublishSafe(new ReinitializeMenuViewsMessage());
		}

		/// <summary>
		/// Runs the client initialization commands and persists their resulting state through the selected backend.
		/// </summary>
		private async UniTask InitializeServerSidePlayer()
		{
			var response = await _backendService.CallGenericFunction(CommandNames.GET_PLAYER_DATA);
			var serverResult = ModelSerializer.Deserialize<PlayFabResult<LogicResult>>(response.FunctionResult.ToString());
			var data = serverResult.Result.Data;
			if (data.TryGetValue("BuildNumber", out var buildNumber))
			{
				VersionUtils.ServerBuildNumber = buildNumber;
			}

			if (data.TryGetValue("BuildCommit", out var buildCommit))
			{
				VersionUtils.ServerBuildCommit = buildCommit;
			}

			// Progression is client-authoritative; there is no Azure server build to validate.
		}

		public async UniTask AddUserNamePassword(string email, string username, string password)
		{
			await _loginProvider.AddUserNamePassword(email, username, password);
			_sessionData.IsGuest = false;
			_sessionData.Email = email;
		}

		public UniTask SendAccountRecoveryEmail(string email)
		{
			return _loginProvider.SendAccountRecoveryEmail(email);
		}

		public async UniTask Logout(bool clearRecoverableCredentials = false)
		{
			await UniTask.WhenAll(_authenticationHooks.Select(a => a.BeforeLogout()));
			await _loginProvider.Logout(clearRecoverableCredentials);
			_sessionData.IsAuthenticated = false;
			_sessionData.Email = null;
		}

		public async UniTask DeleteAccount()
		{
			await _backendService.CallGenericFunction(CommandNames.REMOVE_PLAYER_DATA);
			await Logout();
		}

		public bool HasDeviceLinked()
		{
			return _loginProvider.CanLoginAutomatically();
		}

		public void RegisterHook(IAuthenticationHook hook)
		{
			_authenticationHooks.Add(hook);
		}

	}
}
