using FirstLight.Game.Configs.Remote;
using FirstLight.Game.Services.Authentication;
using FirstLight.Game.Data;
using FirstLight.Game.Services;
using FirstLight.Game.Utils;
using FirstLight.Server.SDK.Modules;
using GameLovers.Services;
using NUnit.Framework;

namespace FirstLight.Tests.EditorMode
{
	public class BackendClientStateTests
	{
		[TestCase("abc", "abc")]
		[TestCase("Miguel", "Miguel")]
		[TestCase("Miguel#1234", "Miguel")]
		[TestCase("Miguel#Clash", "Miguel#Clash")]
		[TestCase(null, null)]
		public void PublicNameSupportsUnnumberedAndLegacyNumberedProfiles(string input, string expected)
		{
			Assert.That(AuthServiceNameExtensions.TrimPlayfabDisplayName(input), Is.EqualTo(expected));
		}

		[Test]
		public void CloudSnapshotExcludesAccountCredentialsAndDeviceSettings()
		{
			var data = new ClientDataService();
			data.AddData(new AccountData { DeviceId = "private-device", LastLoginEmail = "private@example.invalid" });
			data.AddData(new AppData());
			data.AddData(new PlayerData { Trophies = 42 });
			var snapshot = GameBackendService.CapturePlayerState(data);
			Assert.That(snapshot.Keys, Is.EquivalentTo(new[] { typeof(PlayerData).FullName }));
			Assert.That(snapshot.DeserializeModel<PlayerData>().Trophies, Is.EqualTo(42));
		}

		[Test]
		public void QueuedSnapshotDoesNotChangeWhenNextCommandMutatesPlayer()
		{
			var player = new PlayerData { Trophies = 10 };
			var data = new ClientDataService();
			data.AddData(player);
			var snapshot = GameBackendService.CapturePlayerState(data);
			player.Trophies = 999;
			Assert.That(snapshot.DeserializeModel<PlayerData>().Trophies, Is.EqualTo(10));
		}

		[Test]
		public void BundledConfigHasOneSoloModeAndAnExplicitSupportedQueue()
		{
			var modes = (FixedGameModesConfig) BundledRemoteConfig.Create(typeof(FixedGameModesConfig));
			var queues = (MatchmakingQueuesConfig) BundledRemoteConfig.Create(typeof(MatchmakingQueuesConfig));
			Assert.That(modes, Has.Count.EqualTo(1));
			Assert.That(modes[0].MatchConfig.DisableBots, Is.False);
			Assert.That(modes[0].MatchConfig.MinPlayersToStartMatch, Is.EqualTo(1));
			Assert.That(queues.ContainsKey(modes[0].MatchConfig.TeamSize.ToString()), Is.True);
			foreach (var type in RemoteConfigValidator.Configs) Assert.That(BundledRemoteConfig.Create(type), Is.Not.Null);
		}
	}
}
