using Assert = NUnit.Framework.Assert;
using System;
using System.Collections.Generic;
using FirstLight.Game.Services;
using NUnit.Framework;
using Newtonsoft.Json;
using Quantum;
using Unity.Services.Lobbies.Models;

namespace FirstLight.Tests.EditorMode.Services
{
	public class PhotonPartyReservationTests
	{
		private const string Host = "ugs-host";
		private const string Member = "ugs-member";
		private static Lobby Party(string host = Host, params string[] ids) => new Lobby(id: "party-id", hostId: host,
			players: new List<Player> { new Player(id: Host), new Player(id: ids.Length == 0 ? Member : ids[0]) },
			data: new Dictionary<string, DataObject> { [FLLobbyService.KEY_MATCHMAKING_TICKET] = new DataObject(DataObject.VisibilityOptions.Member, JsonConvert.SerializeObject(Reservation())) });

		private static JoinedMatchmaking Reservation(string map = "MazeMayhem", string setupRoom = null)
		{
			const string room = "ugs-party-native-reservation";
			var setup = new MatchRoomSetup { RoomIdentifier = setupRoom ?? room,
				SimulationConfig = new SimulationMatchConfig { MapId = map, TeamSize = 2 } };
			return new JoinedMatchmaking { Version = 1, TicketId = room, PartyId = "party-id", HostId = Host,
				PlayerIds = new[] { Host, Member }, Region = "eu", CreatedAtUtc = DateTime.UtcNow,
				RoomSetupBase64 = Convert.ToBase64String(setup.ToByteArray()) };
		}

		[Test] public void AcceptsActualSerializedRoomForUnchangedParty() => Assert.DoesNotThrow(() =>
			PhotonRoomMatchmakingService.ValidatePartyReservation(Reservation(), Party(), Member, "eu"));

		[Test] public void RejectsOldBackendQueueTicket()
		{
			var reservation = Reservation(); reservation.Version = 0;
			Assert.Throws<InvalidOperationException>(() => PhotonRoomMatchmakingService.ValidatePartyReservation(reservation, Party(), Member, "eu"));
		}

		[Test] public void RejectsChangedPartyLeader() => Assert.Throws<InvalidOperationException>(() =>
			PhotonRoomMatchmakingService.ValidatePartyReservation(Reservation(), Party(Member), Member, "eu"));

		[Test] public void RejectsChangedPartyMembership() => Assert.Throws<InvalidOperationException>(() =>
			PhotonRoomMatchmakingService.ValidatePartyReservation(Reservation(), Party(Host, "another-member"), Host, "eu"));

		[Test] public void RejectsDifferentPhotonRegion() => Assert.Throws<InvalidOperationException>(() =>
			PhotonRoomMatchmakingService.ValidatePartyReservation(Reservation(), Party(), Member, "us"));

		[Test] public void RejectsExpiredReservation()
		{
			var reservation = Reservation(); reservation.CreatedAtUtc = DateTime.UtcNow.AddMinutes(-6);
			Assert.Throws<InvalidOperationException>(() => PhotonRoomMatchmakingService.ValidatePartyReservation(reservation, Party(), Member, "eu"));
		}

		[Test] public void RejectsUnresolvedRandomMap() => Assert.Throws<InvalidOperationException>(() =>
			PhotonRoomMatchmakingService.ValidatePartyReservation(Reservation(GameId.Any.ToString()), Party(), Member, "eu"));

		[Test] public void RejectsClearedActiveReservation()
		{
			var party = Party(); party.Data[FLLobbyService.KEY_MATCHMAKING_TICKET] = new DataObject(DataObject.VisibilityOptions.Member, null);
			Assert.Throws<InvalidOperationException>(() => PhotonRoomMatchmakingService.ValidatePartyReservation(Reservation(), party, Member, "eu"));
		}

		[Test] public void RejectsRoomIdentifierMismatch() => Assert.Throws<InvalidOperationException>(() =>
			PhotonRoomMatchmakingService.ValidatePartyReservation(Reservation(setupRoom: "another-room"), Party(), Member, "eu"));
	}
}
