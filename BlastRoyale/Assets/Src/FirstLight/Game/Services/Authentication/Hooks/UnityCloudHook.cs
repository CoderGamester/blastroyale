using System;
using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using FirstLight.Game.Utils;
using FirstLight.Game.Utils.UCSExtensions;
using GameLovers.Services;
using PlayFab.ClientModels;
using Unity.Services.Authentication;
using Unity.Services.CloudSave;
using Unity.Services.Friends;
using Unity.Services.Friends.Options;

namespace FirstLight.Game.Services.Authentication.Hooks
{
	/// <summary>Initialize UGS clients using the identity already authenticated by UnityCloudLoginProvider.</summary>
	public sealed class UnityCloudHook : IAuthenticationHook
	{
		private readonly IGameRemoteConfigProvider _remoteConfig;
		private readonly HashSet<Func<UniTask>> _postAuthentication = new();
		internal event Action<string> OnSetPlayerName;

		public event Func<UniTask> PostUnityAuthentication
		{
			add => _postAuthentication.Add(value);
			remove => _postAuthentication.Remove(value);
		}

		public UnityCloudHook(IGameBackendService backendService, IClientDataService dataService,
			IGameRemoteConfigProvider gameRemoteConfigProvider, IMessageBrokerService msgBroker)
		{
			_remoteConfig = gameRemoteConfigProvider;
		}

		public UniTask BeforeLogout() => UniTask.CompletedTask;
		public UniTask BeforeAuthentication(bool previouslyLoggedIn = false) => UniTask.CompletedTask;
		public UniTask AfterFetchedState(LoginResult result) => UniTask.CompletedTask;

		public async UniTask AfterAuthentication(LoginResult result, bool previouslyLoggedIn)
		{
			var account = UgsAccountSession.Capture();
			var tasks = _postAuthentication.Select(callback => callback()).ToList();
			tasks.Add(_remoteConfig.Init());
			tasks.Add(FriendsService.Instance.InitializeAsync(new InitializeOptions()
				.WithEvents(true).WithMemberPresence(true).WithMemberProfile(true)).AsUniTask());
			await UniTask.WhenAll(tasks);
			account.ThrowIfChanged();
			var name = await AuthenticationService.Instance.GetPlayerNameAsync();
			account.ThrowIfChanged();
			OnSetPlayerName?.Invoke(name);
			await CloudSaveService.Instance.SavePlayerNameAsync(name);
			account.ThrowIfChanged();
		}
	}
}
