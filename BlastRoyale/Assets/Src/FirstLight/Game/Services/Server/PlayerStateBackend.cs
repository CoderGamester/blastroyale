using System;
using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using FirstLight.Game.Data;
using FirstLight.Game.Utils;
using FirstLight.Server.SDK.Models;
using FirstLight.Server.SDK.Modules;
using PlayFab;
using PlayFab.ClientModels;

namespace FirstLight.Game.Services
{
	/// <summary>
	/// Replace this persistence boundary when restoring server authority from Backend/.
	/// Progression, currencies and virtual shop purchases are currently computed by the client.
	/// </summary>
	public interface IPlayerStateBackend
	{
		UniTask<ServerState> Load();
		UniTask Save(ServerState state);
		UniTask<PublicPlayerProfile> GetPublicProfile(string playerId);
	}

	/// <summary>Free PlayFab Development Mode APIs; no Azure Functions, developer secret or hosted server.</summary>
	public sealed class PlayFabPlayerStateBackend : IPlayerStateBackend
	{
		// Only account progression belongs in the cloud. Never upload AccountData, device IDs or local settings.
		public static readonly Type[] ModelTypes =
		{
			typeof(PlayerData), typeof(LiveopsData), typeof(CollectionData), typeof(IdData), typeof(RngData),
			typeof(EquipmentData), typeof(BattlePassData), typeof(TutorialData), typeof(ContentCreatorData),
			typeof(PlayerStoreData), typeof(EventsData), typeof(BundlePurchaseData)
		};
		private Dictionary<string, string> _lastSaved = new ();
		private string _playerId;
		private int _generation;

		public async UniTask<ServerState> Load()
		{
			_playerId = PlayFabSettings.staticPlayer.PlayFabId;
			_generation++;
			_lastSaved.Clear();
			var generation = _generation;
			var playerId = _playerId;
			var authentication = new PlayFabAuthenticationContext { ClientSessionTicket = PlayFabSettings.staticPlayer.ClientSessionTicket };
			var result = await AsyncPlayfabAPI.ClientAPI.GetUserData(new GetUserDataRequest { AuthenticationContext = authentication });
			if (_generation != generation || playerId != PlayFabSettings.staticPlayer.PlayFabId)
				throw new InvalidOperationException("Player changed while progression was loading.");
			var state = new ServerState();
			foreach (var type in ModelTypes)
			{
				if (result.Data != null && result.Data.TryGetValue(Key(type), out var value))
				{
					state[type.FullName] = value.Value;
					_lastSaved[Key(type)] = value.Value;
				}
				else
				{
					var model = Activator.CreateInstance(type);
					if (model is RngData rng)
					{
						// Stable seed across platforms, unlike string.GetHashCode().
						rng.Seed = StableSeed(_playerId);
						rng.State = RngUtils.GenerateRngState(rng.Seed);
					}
					state.UpdateModel(model);
				}
			}
			return state;
		}

		public async UniTask Save(ServerState state)
		{
			if (string.IsNullOrEmpty(_playerId) || _playerId != PlayFabSettings.staticPlayer.PlayFabId)
				throw new InvalidOperationException("Player changed during cloud save; sign in again before persisting progression.");
			var generation = _generation;
			var authentication = new PlayFabAuthenticationContext { ClientSessionTicket = PlayFabSettings.staticPlayer.ClientSessionTicket };
			var updates = new Dictionary<string, string>();
			foreach (var type in ModelTypes)
			{
				if (!state.TryGetValue(type.FullName, out var json)) continue;
				var key = Key(type);
				if (!_lastSaved.TryGetValue(key, out var old) || old != json) updates[key] = json;
			}
			// Small batches also avoid uploading unchanged inventories after every command.
			foreach (var batch in updates.Select((value, index) => (value, index)).GroupBy(x => x.index / 10))
			{
				var data = batch.ToDictionary(x => x.value.Key, x => x.value.Value);
				await AsyncPlayfabAPI.ClientAPI.UpdateUserData(new UpdateUserDataRequest
				{
					Data = data,
					AuthenticationContext = authentication,
					Permission = UserDataPermission.Private
				});
				if (_generation != generation || _playerId != PlayFabSettings.staticPlayer.PlayFabId) throw new InvalidOperationException("Player changed while progression was saving.");
				foreach (var entry in data) _lastSaved[entry.Key] = entry.Value;
			}
		}

		public async UniTask<PublicPlayerProfile> GetPublicProfile(string playerId)
		{
			var source = new UniTaskCompletionSource<GetPlayerProfileResult>();
			PlayFabClientAPI.GetPlayerProfile(new GetPlayerProfileRequest
			{
				PlayFabId = playerId,
				ProfileConstraints = new PlayerProfileViewConstraints
				{
					ShowDisplayName = true, ShowAvatarUrl = true, ShowStatistics = !FLEnvironment.Configuration.UseBundledRemoteConfig
				}
			}, r => source.TrySetResult(r), e => source.TrySetException(e.AsException()));
			var profile = (await source.Task).PlayerProfile;
			return new PublicPlayerProfile
			{
				Name = profile.DisplayName,
				AvatarUrl = profile.AvatarUrl,
				Statistics = profile.Statistics?.Select(s => new Statistic
				{
					Name = s.Name, Value = s.Value, Version = s.Version
				}).ToList() ?? new List<Statistic>()
			};
		}

		private static string Key(Type type) => "model_" + type.Name;
		private static int StableSeed(string value)
		{
			unchecked
			{
				uint hash = 2166136261;
				foreach (var character in value ?? "") hash = (hash ^ character) * 16777619;
				return (int) hash;
			}
		}
	}

	public interface IClientAuthorityBackend
	{
		UniTask PersistPlayerState(ServerState snapshot);
	}
}
