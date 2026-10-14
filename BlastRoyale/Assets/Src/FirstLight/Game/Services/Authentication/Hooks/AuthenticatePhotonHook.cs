using System;
using Cysharp.Threading.Tasks;
using Photon.Client;
using FirstLight.Game.Utils;
using PlayFab.ClientModels;
using Photon.Realtime;
using Unity.Services.Authentication;

namespace FirstLight.Game.Services.Authentication.Hooks
{
	public class AuthenticatePhotonHook : IAuthenticationHook
	{
		private readonly IInternalGameNetworkService _networkService;
		private int _generation;

		public AuthenticatePhotonHook(IInternalGameNetworkService networkService)
		{
			_networkService = networkService;
		}

		public UniTask BeforeAuthentication(bool previouslyLoggedIn = false) => UniTask.CompletedTask;

		public UniTask AfterAuthentication(LoginResult result, bool previouslyLoggedIn = false)
		{
			return AuthenticatePhoton();
		}

		public UniTask AfterFetchedState(LoginResult result) => UniTask.CompletedTask;

		public UniTask BeforeLogout()
		{
			_generation++;
			UgsAccountSession.Invalidate();
			_networkService.DisconnectPhoton();
			_networkService.QuantumClient.AuthValues = null;
			_networkService.UserId.Value = null;
			return UniTask.CompletedTask;
		}

		public async UniTask AuthenticatePhoton()
		{
			var authentication = AuthenticationService.Instance;
			if (!authentication.IsSignedIn || string.IsNullOrEmpty(authentication.PlayerId))
				throw new InvalidOperationException("Sign in to Unity Authentication before connecting Photon.");
			var account = UgsAccountSession.Capture();
			var playerId = account.PlayerId;
			var generation = ++_generation;
			var client = _networkService.QuantumClient;
			if (client.RealtimePeer.PeerState != PeerStateValue.Disconnected)
			{
				_networkService.DisconnectPhoton();
				await UniTask.WaitUntil(() => client.RealtimePeer.PeerState == PeerStateValue.Disconnected)
					.Timeout(TimeSpan.FromSeconds(10));
			}
			account.ThrowIfChanged();
			if (_generation != generation)
				throw new OperationCanceledException("The Unity account changed before Photon connected.");

			_networkService.UserId.Value = playerId;
			// Photon Cloud anonymous identity uses the real UGS account ID. No custom-auth token is fabricated.
			client.AuthValues = new AuthenticationValues(playerId);
			if (!_networkService.ConnectPhotonServer())
				throw new InvalidOperationException("Photon could not start connecting for the authenticated Unity account.");
		}
	}
}
