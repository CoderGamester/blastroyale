using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using FirstLight.Game.Utils;
using PlayFab.ClientModels;
using Unity.Services.Authentication;
using Unity.Services.Authentication.PlayerAccounts;
using Unity.Services.Core;

namespace FirstLight.Game.Services.Authentication
{
	/// <summary>One UGS identity for authentication, Cloud Save, Friends, Lobby and Photon user IDs.</summary>
	internal sealed class UnityCloudLoginProvider : ILoginProvider
	{
		public bool CanUsePlayerAccounts
		{
			get
			{
				try { return PlayerAccountService.Instance != null; }
				catch (ServicesInitializationException) { return false; }
			}
		}

		public UniTask BeforeAuthentication(bool previouslyLoggedIn) => UniTask.CompletedTask;
		public UniTask AfterAuthentication(LoginResult result, bool previouslyLoggedIn) => UniTask.CompletedTask;
		public UniTask AfterFetchedState(LoginResult result) => UniTask.CompletedTask;
		public UniTask BeforeLogout() => UniTask.CompletedTask;
		public bool CanLoginAutomatically() => AuthenticationService.Instance.SessionTokenExists;

		public async UniTask<LoginResult> LoginWithDevice()
		{
			// IsSignedIn also includes Expired in Auth3.8; cached sign-in is required until authorization is restored.
			if (!AuthenticationService.Instance.IsAuthorized)
				await AuthenticationService.Instance.SignInAnonymouslyAsync();
			return await ProjectLoginResult(false);
		}

		public async UniTask<LoginResult> LoginWithNewGuestAccount()
		{
			// Reuse a cached session on retries/restarts; creating a guest never discards recoverable progression.
			var firstSession = !AuthenticationService.Instance.SessionTokenExists;
			// IsSignedIn also includes Expired in Auth3.8; cached sign-in is required until authorization is restored.
			if (!AuthenticationService.Instance.IsAuthorized)
				await AuthenticationService.Instance.SignInAnonymouslyAsync();
			return await ProjectLoginResult(firstSession);
		}

		public async UniTask<LoginResult> LoginWithUnityPlayerAccount(bool linkCurrentGuest)
		{
			if (!CanUsePlayerAccounts)
				throw new InvalidOperationException("Unity Player Accounts is not configured. Create its settings in Project Settings > Services > Player Accounts.");
			var accounts = PlayerAccountService.Instance;
			if (accounts.IsSignedIn) accounts.SignOut();
			var wasSignedIn = AuthenticationService.Instance.IsSignedIn;
			var current = wasSignedIn || linkCurrentGuest ? UgsAccountSession.Capture() : default;
			var tokenReady = false;
			// StartSignInAsync launches the browser; SignedIn is the actual token-ready completion signal.
			var completion = new UniTaskCompletionSource();
			void SignedIn() => completion.TrySetResult();
			void Failed(RequestFailedException error) => completion.TrySetException(error);
			accounts.SignedIn += SignedIn;
			accounts.SignInFailed += Failed;
			try
			{
				await accounts.StartSignInAsync();
				await completion.Task.Timeout(TimeSpan.FromMinutes(5));
				tokenReady = true;
				if (wasSignedIn) current.ThrowIfChanged();
				if (linkCurrentGuest)
				{
					current.ThrowIfChanged();
					await AuthenticationService.Instance.LinkWithUnityAsync(accounts.AccessToken);
					current.ThrowIfChanged();
				}
				else
				{
					// Only change the game account once the hosted login has returned a real access token.
					UgsAccountSession.Invalidate();
					if (AuthenticationService.Instance.IsSignedIn) AuthenticationService.Instance.SignOut();
					await AuthenticationService.Instance.SignInWithUnityAsync(accounts.AccessToken);
				}
				return await ProjectLoginResult(false);
			}
			finally
			{
				accounts.SignedIn -= SignedIn;
				accounts.SignInFailed -= Failed;
				if (!tokenReady) accounts.SignOut();
			}
		}

		private static async UniTask<LoginResult> ProjectLoginResult(bool newlyCreated)
		{
			var account = UgsAccountSession.Capture();
			var name = await AuthenticationService.Instance.GetPlayerNameAsync();
			account.ThrowIfChanged();
			// Genuine legacy DTO retained solely for existing hook/server-SDK contracts; no PlayFab session is created.
			return new LoginResult
			{
				PlayFabId = account.PlayerId,
				NewlyCreated = newlyCreated,
				InfoResultPayload = new GetPlayerCombinedInfoResultPayload
				{
					PlayerProfile = new PlayerProfileModel { PlayerId = account.PlayerId, DisplayName = name },
					TitleData = new Dictionary<string, string>()
				}
			};
		}

		public UniTask Logout(bool clearRecoverableCredentials = false)
		{
			UgsAccountSession.Invalidate();
			var authentication = AuthenticationService.Instance;
			// Only the explicit Settings logout requests credential clearing. The native
			// linked Unity identity proves this account can be recovered through hosted login.
			// Anonymous or unknown identities must keep their sole recovery credential.
			var clearCredentials = clearRecoverableCredentials && authentication.PlayerInfo != null &&
				!string.IsNullOrEmpty(authentication.PlayerInfo.GetUnityId());
			if (authentication.IsSignedIn) authentication.SignOut(clearCredentials);
			if (CanUsePlayerAccounts && PlayerAccountService.Instance.IsSignedIn) PlayerAccountService.Instance.SignOut();
			return UniTask.CompletedTask;
		}

		private static NotSupportedException HostedAccountRequired() => new("Use the hosted Unity Player Accounts sign-in/link flow for recoverable accounts.");
		public UniTask<LoginResult> LoginWithEmailPassword(string email, string password) => throw HostedAccountRequired();
		public UniTask AddUserNamePassword(string email, string username, string password) => throw HostedAccountRequired();
		public UniTask SendAccountRecoveryEmail(string email) => throw HostedAccountRequired();
	}
}
