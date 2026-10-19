using FirstLight.Game.Configs;
using FirstLight.Game.Services;
using FirstLight.Game.Services.RoomService;
using FirstLight.Server.SDK.Modules.GameConfiguration;
using GameLovers.Services;
using NSubstitute;
using NUnit.Framework;
using Photon.Deterministic;
using Photon.Realtime;
using Quantum;
using UnityEditor;
using Assert = NUnit.Framework.Assert;
using RoomService = FirstLight.Game.Services.RoomService.RoomService;

namespace FirstLight.Tests.EditorMode.Services
{
	public class RoomRunnerShutdownTests
	{
		[SetUp]
		public void SetUp() => FirstLight.FLogger.FLog.Init();

		[TestCase(ClientState.Leaving)]
		[TestCase(ClientState.DisconnectingFromGameServer)]
		[TestCase(ClientState.ConnectedToMasterServer)]
		public void Shutdown_AfterRoomServiceStartedLeaving_PreservesPhotonConnection(ClientState state)
		{
			var client = new RealtimeClient {State = state};
			var room = CreateRoom(client, false);
			var parameters = room.GetDefaultStartParameters();
			Assert.That(parameters.GameMode, Is.EqualTo(DeterministicGameMode.Multiplayer));
			Assert.That(parameters.Communicator, Is.TypeOf<QuantumNetworkCommunicator>());
			var communicator = (QuantumNetworkCommunicator) parameters.Communicator;
			Assert.That(communicator.NetworkClient, Is.SameAs(client));
			communicator.OnDestroy();
			communicator.OnDestroy();
			Assert.That(client.State, Is.EqualTo(state));
			Assert.That(client.DisconnectedCause, Is.EqualTo(DisconnectCause.None));
		}

		[Test]
		public void StartParameters_OfflineRoom_DoesNotCreateNetworkCommunicator()
		{
			var client = new RealtimeClient();
			var room = CreateRoom(client, true);
			var parameters = room.GetDefaultStartParameters();
			Assert.That(parameters.GameMode, Is.EqualTo(DeterministicGameMode.Local));
			Assert.That(parameters.Communicator, Is.Null);
			Assert.That(client.State, Is.EqualTo(ClientState.PeerCreated));
		}

		private static GameRoom CreateRoom(RealtimeClient client, bool offline)
		{
			var config = AssetDatabase.LoadAssetAtPath<QuantumRunnerConfigs>(
				"Assets/AddressableResources/Configs/Settings/QuantumRunnerConfigs.asset");
			Assert.That(config, Is.Not.Null);
			Assert.That(config.QuantumDeterministicSessionConfigAsset, Is.Not.Null);
			var configs = Substitute.For<IConfigsProvider>();
			configs.GetConfig<QuantumRunnerConfigs>().Returns(config);
			var network = Substitute.For<IGameNetworkService>();
			network.QuantumClient.Returns(client);
			var service = new RoomService(network, null, configs, Substitute.For<ICoroutineService>(), null, null, null, null);
			var nativeRoom = new Room("shutdown-lifetime-test", new RoomOptions {MaxPlayers = 36}, offline);
			var room = new GameRoom(service, nativeRoom);
			room.Properties.SimulationMatchConfig.Value = new SimulationMatchConfig
			{
				MatchType = MatchType.Matchmaking,
				GameModeID = "BattleRoyale"
			};
			return room;
		}
	}
}
