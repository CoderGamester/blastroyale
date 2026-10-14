using IConfigsProvider = FirstLight.Server.SDK.Modules.GameConfiguration.IConfigsProvider;
using System;
using System.Linq;
using System.Threading;
using Cysharp.Threading.Tasks;
using FirstLight.FLogger;
using FirstLight.Game.Logic;
using FirstLight.Game.Messages;
using FirstLight.Game.Services.RoomService;
using FirstLight.Game.Utils;
using FirstLight.Game.Utils.UCSExtensions;
using GameLovers.GameData;
using GameLovers.Services;
using Newtonsoft.Json;
using Quantum;
using Unity.Services.Lobbies;
using Unity.Services.Lobbies.Models;

namespace FirstLight.Game.Services
{
	/// <summary>Starts a Photon room for the current player or UGS party; no global queue is configured.</summary>
	public interface IMatchmakingService
	{
		IObservableFieldReader<bool> IsMatchmaking { get; }
		UniTask LeaveMatchmaking();
		UniTask JoinMatchmaking(MatchRoomSetup setup);
	}

	public class GameMatched
	{
		public string MatchIdentifier;
		public string[] ExpectedPlayers;
		public MatchRoomSetup RoomSetup;
		public PlayerJoinRoomProperties PlayerProperties;
	}

	/// <summary>Member-visible UGS lobby metadata for an actual Photon room reservation, not a backend queue ticket.</summary>
	public class JoinedMatchmaking
	{
		public int Version;
		public string TicketId;
		public string RoomSetupBase64;
		public string PartyId;
		public string HostId;
		public string[] PlayerIds;
		public string Region;
		public DateTime CreatedAtUtc;

		public MatchRoomSetup DeserializeRoomSetup()
		{
			var bytes = Convert.FromBase64String(RoomSetupBase64);
			if (!MatchRoomSetup.TryParseMatchRoomSetup(bytes, out var setup))
				throw new InvalidOperationException("Could not deserialize the Photon room setup.");
			return setup;
		}
	}

	public sealed class PhotonRoomMatchmakingService : IMatchmakingService
	{
		private readonly IFLLobbyService _lobbies;
		private readonly IMessageBrokerService _broker;
		private readonly IGameNetworkService _network;
		private readonly LocalPrefsService _prefs;
		private readonly IGameModeService _gameModes;
		private readonly ObservableField<bool> _isMatchmaking = new ObservableField<bool>(false);
		private readonly SemaphoreSlim _mutex = new SemaphoreSlim(1);
		private long _requestVersion;
		private string _acceptedRoom;
		public IObservableFieldReader<bool> IsMatchmaking => _isMatchmaking;

		public PhotonRoomMatchmakingService(IGameDataProvider dataProvider, ICoroutineService coroutines, IFLLobbyService lobbies,
			IMessageBrokerService broker, IGameNetworkService network, IGameBackendService backend,
			IConfigsProvider configs, LocalPrefsService prefs, IGameModeService gameModes)
		{
			_lobbies = lobbies;
			_broker = broker;
			_network = network;
			_prefs = prefs;
			_gameModes = gameModes;
			_lobbies.CurrentPartyCallbacks.PlayerJoined += _ => CancelForPartyChange();
			_lobbies.CurrentPartyCallbacks.PlayerLeft += _ => CancelForPartyChange();
			_lobbies.CurrentPartyCallbacks.LocalLobbyUpdated += OnPartyLobbyChanged;
			_broker.Subscribe<SuccessfullyAuthenticated>(_ => CancelLocal());
		}

		private void CancelForPartyChange()
		{
			if (_isMatchmaking.Value) LeaveMatchmaking().Forget();
		}

		private void OnPartyLobbyChanged(ILobbyChanges changes)
		{
			if (changes == null || changes.PlayerJoined.Changed || changes.PlayerLeft.Changed)
			{
				CancelForPartyChange();
				return;
			}
			var party = _lobbies.CurrentPartyLobby;
			if (party == null || party.IsLocalPlayerHost() || !changes.Data.Changed && !changes.Data.Added && !changes.Data.Removed) return;
			if (!changes.Data.Value.TryGetValue(FLLobbyService.KEY_MATCHMAKING_TICKET, out var change)) return;
			var json = change.Value?.Value;
			if (string.IsNullOrEmpty(json)) { CancelLocal(); return; }
			AcceptPartyRoom(json).Forget();
		}

		private async UniTask AcceptPartyRoom(string json)
		{
			var version = _requestVersion;
			try
			{
				using (await _mutex.AcquireAsync())
				{
					if (version != _requestVersion) return;
					var account = UgsAccountSession.Capture();
					var reservation = JsonConvert.DeserializeObject<JoinedMatchmaking>(json);
					if (reservation?.TicketId == _acceptedRoom) return;
					var party = _lobbies.CurrentPartyLobby;
					ValidatePartyReservation(reservation, party, account.PlayerId, _prefs.ServerRegion.Value);
					account.ThrowIfChanged();
					if (version != _requestVersion) return;
					PublishRoom(reservation);
				}
			}
			catch (OperationCanceledException) { CancelLocal(); }
			catch (Exception exception)
			{
				FLog.Warn("Could not join the party's Photon room.", exception);
				CancelLocal(true, exception.Message);
			}
		}

		public async UniTask JoinMatchmaking(MatchRoomSetup setup)
		{
			using (await _mutex.AcquireAsync())
			{
				var version = ++_requestVersion;
				var account = UgsAccountSession.Capture();
				if (_network.UserId != account.PlayerId)
					throw new InvalidOperationException("Photon must be connected with the current Unity player ID before starting a match.");
				var party = _lobbies.CurrentPartyLobby;
				if (party != null && !party.IsLocalPlayerHost())
					throw new InvalidOperationException("Only the UGS party leader can start its match.");
				if (party != null && !party.IsEveryoneReady())
					throw new InvalidOperationException("All party members must be ready before starting a match.");
				if (setup?.SimulationConfig == null) throw new ArgumentException("A simulation configuration is required.", nameof(setup));
				if (setup.SimulationConfig.MapId == GameId.Any.ToString())
				{
					var maps = _gameModes.ValidMatchmakingMaps;
					if (maps.Count == 0) throw new InvalidOperationException("No matchmaking map is configured.");
					// The leader resolves Any once so every party member loads the same map.
					setup.SimulationConfig.MapId = maps[UnityEngine.Random.Range(0, maps.Count)].ToString();
				}
				var players = party == null ? new[] { account.PlayerId } : party.Players.Select(player => player.Id).OrderBy(id => id, StringComparer.Ordinal).ToArray();
				if (players.Length > Math.Max(1, setup.SimulationConfig.TeamSize))
					throw new InvalidOperationException("This party is larger than the selected game mode's team size. Use a Custom Game or select a larger team mode.");
				var room = new JoinedMatchmaking
				{
					Version = 1,
					TicketId = "ugs-party-" + Guid.NewGuid().ToString("N"),
					PartyId = party?.Id,
					HostId = account.PlayerId,
					PlayerIds = players,
					Region = _prefs.ServerRegion.Value,
					CreatedAtUtc = DateTime.UtcNow
				};
				setup.RoomIdentifier = room.TicketId;
				room.RoomSetupBase64 = Convert.ToBase64String(setup.ToByteArray());
				_isMatchmaking.Value = true;
				try
				{
					if (party != null && !await _lobbies.UpdatePartyMatchmakingTicket(room))
						throw new InvalidOperationException("Could not publish the Photon room to the UGS party.");
					account.ThrowIfChanged();
					if (version != _requestVersion) return;
					if (party != null) ValidatePartyReservation(room, _lobbies.CurrentPartyLobby, account.PlayerId, _prefs.ServerRegion.Value);
					PublishRoom(room);
				}
				catch { CancelLocal(); throw; }
			}
		}

		/// <summary>Rejects legacy queue tickets, stale membership/host state and a different Photon region.</summary>
		public static void ValidatePartyReservation(JoinedMatchmaking room, Lobby party, string playerId, string region)
		{
			if (room == null || room.Version != 1 || string.IsNullOrEmpty(room.TicketId) || !room.TicketId.StartsWith("ugs-party-", StringComparison.Ordinal))
				throw new InvalidOperationException("The party has no compatible Photon room reservation.");
			if (party == null || room.PartyId != party.Id || room.HostId != party.HostId || room.PlayerIds == null || !room.PlayerIds.Contains(playerId))
				throw new InvalidOperationException("The party or its leader changed before this room was joined.");
			if (party.Data == null || !party.Data.TryGetValue(FLLobbyService.KEY_MATCHMAKING_TICKET, out var active) || string.IsNullOrEmpty(active.Value) ||
				JsonConvert.DeserializeObject<JoinedMatchmaking>(active.Value)?.TicketId != room.TicketId)
				throw new InvalidOperationException("The party's active Photon room changed or was cancelled.");
			var current = party.Players.Select(player => player.Id).OrderBy(id => id, StringComparer.Ordinal);
			if (!current.SequenceEqual(room.PlayerIds.OrderBy(id => id, StringComparer.Ordinal)))
				throw new InvalidOperationException("The party membership changed before this room was joined.");
			if (room.Region != region) throw new InvalidOperationException("The party room uses a different Photon region.");
			if (room.CreatedAtUtc == default || DateTime.UtcNow - room.CreatedAtUtc > TimeSpan.FromMinutes(5))
				throw new InvalidOperationException("The party room reservation expired. Ask the leader to start again.");
			var setup = room.DeserializeRoomSetup();
			if (setup.RoomIdentifier != room.TicketId || setup.SimulationConfig.MapId == GameId.Any.ToString())
				throw new InvalidOperationException("The party room setup is incomplete.");
		}

		private void PublishRoom(JoinedMatchmaking reservation)
		{
			if (_acceptedRoom == reservation.TicketId) return;
			_acceptedRoom = reservation.TicketId;
			_isMatchmaking.Value = true;
			_broker.PublishSafe(new MatchmakingJoinedMessage { Config = reservation });
			if (!_isMatchmaking.Value) return; // A synchronous UI handler may cancel the request.
			var playerId = _network.UserId;
			var index = Array.IndexOf(reservation.PlayerIds, playerId);
			if (index < 0) { CancelLocal(true, "Photon is connected as a different player."); return; }
			_isMatchmaking.Value = false;
			_broker.PublishSafe(new MatchmakingMatchFoundMessage
			{
				Game = new GameMatched
				{
					MatchIdentifier = reservation.TicketId,
					ExpectedPlayers = reservation.PlayerIds,
					RoomSetup = reservation.DeserializeRoomSetup(),
					PlayerProperties = new PlayerJoinRoomProperties { Team = "team1", TeamColor = (byte)index }
				}
			});
		}

		private void CancelLocal(bool error = false, string reason = null)
		{
			++_requestVersion;
			var wasActive = _isMatchmaking.Value;
			_isMatchmaking.Value = false;
			if (wasActive || error) _broker.PublishSafe(new MatchmakingLeftMessage { Error = error, Reason = reason });
		}

		public async UniTask LeaveMatchmaking()
		{
			// Invalidate immediately, including a pending lobby update.
			CancelLocal();
			using (await _mutex.AcquireAsync())
			{
				var party = _lobbies.CurrentPartyLobby;
				if (party != null && party.IsLocalPlayerHost()) await _lobbies.UpdatePartyMatchmakingTicket(null);
			}
		}
	}
}
