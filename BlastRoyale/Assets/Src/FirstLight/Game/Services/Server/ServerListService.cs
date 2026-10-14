using FirstLight.Game.Services.Infrastructure;
using System;
using System.Collections;
using System.Collections.Generic;
using FirstLight.FLogger;
using FirstLight.Game.Messages;
using FirstLight.Game.Utils;
using GameLovers.Services;
using Photon.Realtime;
using UnityEngine;

namespace FirstLight.Game.Services
{
	public interface IServerListService
	{
		public void FetchServersAndPing();
		public IReadOnlyDictionary<string, ServerPing> PingsByServer { get; }
		public ServerListState State { get; }

		public event Action OnRegionsPinged;
		public event Action OnRegionsFetched;

		public enum ServerListState
		{
			Uninitialized,
			Connecting,
			FetchedRegions,
			FetchedPings,
			Failed,
		}

		public class ServerPing
		{
			public string ServerCode { get; }
			public bool ReceivedPing { get; }
			public string ServerIp { get; }
			public long Ping { get; }

			public ServerPing(string serverCode, bool receivedPing, long ping, string serverIp)
			{
				ServerCode = serverCode;
				ReceivedPing = receivedPing;
				Ping = ping;
				ServerIp = serverIp;
			}
		}
	}


	public class ServerListService : IServerListService, IConnectionCallbacks
	{
		private IThreadService _threadService;
		private ICoroutineService _coroutineService;
		private readonly IGameBackendService _backendService;
		private RealtimeClient _client;
		private Dictionary<string, IServerListService.ServerPing> Pings = new ();

		private bool _fetching;
		private bool _regionResultReceived;

		public ServerListService(IThreadService threadService, ICoroutineService coroutineService, IGameBackendService backendService, IMessageBrokerService _messageBrokerService)
		{
			_threadService = threadService;
			_coroutineService = coroutineService;
			_backendService = backendService;
			_messageBrokerService.Subscribe<SuccessfullyAuthenticated>(OnAuthentication);
		}

		private void OnAuthentication(SuccessfullyAuthenticated msg)
		{
			if (msg.PreviouslyLoggedIn) return;
			FetchServersAndPing();
		}

		public IEnumerator TickQuantum()
		{
			while (_fetching)
			{
				yield return new WaitForSecondsRealtime(0.1f);
				_client?.Service();
			}
		}


		public IReadOnlyDictionary<string, IServerListService.ServerPing> PingsByServer => Pings;
		public IServerListService.ServerListState State { get; private set; }


		public event Action OnRegionsPinged;
		public event Action OnRegionsFetched;


		public void OnConnected()
		{
			FLog.Info("OnConnected");
		}

		public void OnConnectedToMaster()
		{
		}

		public void OnDisconnected(DisconnectCause cause)
		{
			FLog.Info("Disconnected" + cause);
			if (State == IServerListService.ServerListState.Connecting && !_regionResultReceived)
				State = IServerListService.ServerListState.Failed;
			Disconnect();
		}

		public void FetchServersAndPing()
		{
			if (_fetching)
			{
				FLog.Info("Already collecting regions and pings!");
				return;
			}

			FLog.Info("Fetching pings");
			_client = new RealtimeClient {ClientType = ClientAppType.Quantum};
			_client.AddCallbackTarget(this);
			_fetching = true;
			_regionResultReceived = false;
			State = IServerListService.ServerListState.Connecting;
			_client.GetRegions(new AppSettings
			{
				AppIdQuantum = FLEnvironment.Configuration.PhotonQuantumAppId,
				UseNameServer = true
			});
			if (_client.State == ClientState.Disconnected || _client.State == ClientState.PeerCreated)
			{
				State = IServerListService.ServerListState.Failed;
				FLog.Error("Failed to connect to name server");
				Disconnect();
				return;
			}

			_coroutineService.StartCoroutine(TickQuantum());
		}

		public void OnRegionListReceived(RegionHandler regionHandler)
		{
			// Realtime v5 completes its region ping workflow before this callback.
			var client = _client;
			if (!ReferenceEquals(client?.RegionHandler, regionHandler)) return;
			_regionResultReceived = true;
			_threadService.MainThreadDispatcher.Enqueue(() =>
			{
				if (!ReferenceEquals(_client, client)) return;
				UpdateList(regionHandler);
				State = IServerListService.ServerListState.FetchedRegions;
				OnRegionsFetched?.Invoke();
				State = regionHandler.BestRegion == null
					? IServerListService.ServerListState.Failed
					: IServerListService.ServerListState.FetchedPings;
				if (State == IServerListService.ServerListState.FetchedPings) OnRegionsPinged?.Invoke();
				Disconnect();
			});
		}

		private void Disconnect()
		{
			_client?.RemoveCallbackTarget(this);
			_client?.Disconnect();
			_fetching = false;
		}

		private void UpdateList(RegionHandler obj)
		{
			foreach (var objEnabledRegion in obj.EnabledRegions)
			{
				Pings[objEnabledRegion.Code] = new IServerListService.ServerPing(objEnabledRegion.Code, objEnabledRegion.WasPinged, objEnabledRegion.Ping, objEnabledRegion.HostAndPort);
			}
		}


		public void OnCustomAuthenticationResponse(Dictionary<string, object> data)
		{
		}

		public void OnCustomAuthenticationFailed(string debugMessage)
		{
			State = IServerListService.ServerListState.Failed;
			Disconnect();
		}
	}
}
