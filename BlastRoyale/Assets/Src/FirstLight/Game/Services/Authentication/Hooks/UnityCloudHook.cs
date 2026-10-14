using FirstLight.Game.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using FirstLight.FLogger;
using FirstLight.Game.Data;
using FirstLight.Game.Logic.RPC;
using FirstLight.Game.Messages;
using FirstLight.Game.Utils;
using FirstLight.Game.Utils.UCSExtensions;
using GameLovers.Services;
using FirstLight.Server.SDK.Modules;
using FirstLight.Server.SDK.Modules.Commands;
using PlayFab;
using PlayFab.ClientModels;
using Unity.Services.Authentication;
using Unity.Services.CloudSave;
using Unity.Services.Friends;
using Unity.Services.Friends.Options;

namespace FirstLight.Game.Services.Authentication.Hooks
{
	public class UnityCloudHook : IAuthenticationHook
	{
		private IClientDataService _dataService;
		private IGameBackendService _backendService;
		private IGameRemoteConfigProvider _gameRemoteConfigProvider;
		private IMessageBrokerService _msgBroker;

		private HashSet<Func<UniTask>> _postUnityAuthentication = new ();
		internal event Action<string> OnSetPlayfabName;
		private string _canonicalPlayFabName;

		/// <summary>
		///  Called just after the unity authentication finished, services are still not initialized
		/// </summary>
		public event Func<UniTask> PostUnityAuthentication
		{
			add => _postUnityAuthentication.Add(value);
			remove => _postUnityAuthentication.Remove(value);
		}

		public UnityCloudHook(IGameBackendService backendService,
							  IClientDataService dataService,
							  IGameRemoteConfigProvider gameRemoteConfigProvider,
							  IMessageBrokerService msgBroker)
		{
			_backendService = backendService;
			_dataService = dataService;
			_gameRemoteConfigProvider = gameRemoteConfigProvider;
			_msgBroker = msgBroker;
		}

		public UniTask BeforeLogout()
		{
			if (AuthenticationService.Instance.IsSignedIn) // If is already signed sign out, this is used in retries or logging in
			{
				AuthenticationService.Instance.SignOut(true);
			}

			return UniTask.CompletedTask;
		}

		public UniTask BeforeAuthentication(bool previouslyLoggedIn = false)
		{
			return UniTask.CompletedTask;
		}

		public UniTask AfterAuthentication(LoginResult result, bool previouslyLoggedIn)
		{
			return AuthenticateUnityServices(result, previouslyLoggedIn);
		}

		public UniTask AfterFetchedState(LoginResult result)
		{
			return UniTask.CompletedTask;
		}

		private async UniTask AuthenticateUnityServices(LoginResult playfabLoginResult, bool previouslyLoggedIn)
		{
			if (AuthenticationService.Instance.IsSignedIn) AuthenticationService.Instance.SignOut();
			// Cache a separate anonymous UGS player per PlayFab account. This requires no server token exchange.
			// These two identities are only linked by the client; Backend/ must restore verified linking for production.
			AuthenticationService.Instance.SwitchProfile("playfab_" + playfabLoginResult.PlayFabId);
			await AuthenticationService.Instance.SignInAnonymouslyAsync();
			await PostUnityAuth(playfabLoginResult);
		}

		public UniTask FetchTokenAndAuthenticate() => AuthenticationService.Instance.SignInAnonymouslyAsync().AsUniTask();

		private async UniTask PostUnityAuth(LoginResult loginResult)
		{
			var tasks = new List<UniTask>();
			// Add our events
			tasks.AddRange(_postUnityAuthentication.Select(func => func.Invoke()));
			tasks.Add(_gameRemoteConfigProvider
				.Init()); // This should not be here because of manual IoC would require a lo tof changes so fuckit
			tasks.Add(CloudSaveService.Instance.SavePlayfabIDAsync(PlayFabSettings.staticPlayer.PlayFabId));
			tasks.Add(AuthenticationService.Instance.GetPlayerNameAsync().AsUniTask());
			tasks.Add(FriendsService.Instance.InitializeAsync(
					new InitializeOptions()
						.WithEvents(true)
						.WithMemberPresence(true)
						.WithMemberProfile(true))
				.AsUniTask());
			await UniTask.WhenAll(tasks);
			await CheckNamesUpdates(loginResult);
			if (!string.IsNullOrWhiteSpace(_canonicalPlayFabName))
				await CloudSaveService.Instance.SavePlayerNameAsync(_canonicalPlayFabName);
		}

		private async UniTask CheckNamesUpdates(LoginResult loginResult)
		{
			try
			{
				_canonicalPlayFabName = loginResult.InfoResultPayload?.PlayerProfile?.DisplayName;
				var playfabName = AuthServiceNameExtensions.TrimPlayfabDisplayName(_canonicalPlayFabName) ?? "";
				var unityName = AuthServiceNameExtensions.TrimUnityDisplayName(AuthenticationService.Instance.PlayerName) ?? "";

				if (string.IsNullOrWhiteSpace(playfabName))
				{
					FLog.Info("Updating playfab name to auto generated unity one" + unityName);
					var playfabResult = await AsyncPlayfabAPI.UpdateUserTitleDisplayName(new UpdateUserTitleDisplayNameRequest()
					{
						DisplayName = unityName.Length > 25 ? unityName.Substring(0, 25) : unityName
					});
					_canonicalPlayFabName = playfabResult.DisplayName;
					OnSetPlayfabName?.Invoke(playfabResult.DisplayName);
					return;
				}

				// Handle old user accounts
				if (!playfabName.Equals(unityName))
				{
					FLog.Info($"Updating unity name('{unityName}') to '{playfabName}'");
					await AuthenticationService.Instance.UpdatePlayerNameAsync(playfabName);
				}
			}
			catch (Exception ex)
			{
				FLog.Error("Failed to update name", ex);
			}
		}

	}
}
